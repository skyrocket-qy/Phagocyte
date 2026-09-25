using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Confocal-fluorescence grid board for the shared passive tree.
/// Five lineage start hubs and the core metabolism block are wired by
/// orthogonal microtubule traces between adjacent cells, so routes never
/// overlap or cross.
/// Pan, zoom, hover tooltips, left-click purchases, right-click refunds.
/// </summary>
public partial class PassiveTreeView : Control
{
    [Signal]
    public delegate void TreeNodeActivatedEventHandler(string nodeId);

    [Signal]
    public delegate void TreeNodeHoveredEventHandler(string nodeId);

    [Signal]
    public delegate void TreeNodeRefundRequestedEventHandler(string nodeId);

    public string TreeClassKey { get; private set; } = "macrophage";
    public string SelectedNodeId { get; private set; } = "";
    public string HoveredNodeId { get; private set; } = "";
    public float ZoomLevel => _zoom;
    public Vector2 CameraPosition => _camera;
    public int RenderedNodeCount => _buttons.Count;

    private const float MinZoom = 0.12f;
    private const float MaxZoom = 1.75f;
    private const float WorldSize = 4800.0f;

    /// <summary>
    /// Initial framing zooms slightly past an exact fit so the board fills
    /// more of the view on open.
    /// </summary>
    private const float FitZoomBoost = 1.15f;

    private static readonly Color BackgroundColor = new(0.020f, 0.043f, 0.078f, 1.0f);
    private static readonly Color PlasmaCyan = new(0.35f, 0.92f, 1.00f, 1.0f);

    private static readonly Dictionary<string, Color> BranchColors = new()
    {
        { "precision", new Color(1.00f, 0.36f, 0.48f, 1.0f) },
        { "senses", new Color(0.24f, 1.00f, 0.70f, 1.0f) },
        { "motility", new Color(0.31f, 0.76f, 1.00f, 1.0f) },
        { "ballistics", new Color(1.00f, 0.82f, 0.29f, 1.0f) },
        { "vitality", new Color(1.00f, 0.54f, 0.29f, 1.0f) },
        { "core", new Color(0.76f, 0.55f, 1.00f, 1.0f) }
    };

    private static readonly string[] BranchOrder = { "precision", "senses", "motility", "ballistics", "vitality", "core" };

    /// <summary>
    /// Catalog-ordered node list, sorted once (rarity, id).
    /// <c>PassiveTreeManager.Nodes</c> is a static catalog, so re-sorting
    /// on every <c>Render</c>/<c>DrawNodes</c> is pure waste.
    /// </summary>
    private static readonly List<PassiveTreeManager.TreeNode> _orderedNodes = BuildOrderedNodes();

    private static List<PassiveTreeManager.TreeNode> BuildOrderedNodes()
    {
        var ordered = new List<PassiveTreeManager.TreeNode>(PassiveTreeManager.Nodes);
        ordered.Sort((a, b) =>
        {
            int rarity = a.Rarity.CompareTo(b.Rarity);
            return rarity != 0 ? rarity : string.Compare(a.Id, b.Id, System.StringComparison.Ordinal);
        });
        return ordered;
    }

    private Control? _zoomRoot;
    private TreeGraphLayer? _graphLayer;
    private PanelContainer? _tooltipPanel;
    private RichTextLabel? _tooltipText;
    private GradientTexture2D? _glowTexture;
    private readonly Dictionary<string, Button> _buttons = new();

    private Vector2 _camera = Vector2.Zero;
    private float _zoom = 0.80f;
    private string _cameraClass = "";
    private bool _needsFit;
    private float _time;
    private Vector2 _mousePosition = Vector2.Zero;
    private bool _mouseInside;
    private bool _panning;
    private MouseButton _panButton = MouseButton.Left;
    private Vector2 _panStartMouse = Vector2.Zero;
    private Vector2 _panStartCamera = Vector2.Zero;
    private bool _panMoved;
    private Vector2 _rootPosition = Vector2.Zero;
    private bool _nodeDragging;
    private string _nodeDragId = "";
    private Vector2 _nodePressPosition = Vector2.Zero;
    private bool _nodeDragMoved;
    private string _tooltipShownId = "\0";

    private partial class TreeGraphLayer : Control
    {
        public PassiveTreeView? View { get; set; }

        public override void _Draw()
        {
            View?.DrawGraph();
        }
    }

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;

        var glowGradient = new Gradient
        {
            Colors = new[]
            {
                new Color(0.30f, 0.82f, 1.00f, 0.15f),
                new Color(0.22f, 0.62f, 1.00f, 0.09f),
                new Color(0.08f, 0.30f, 0.55f, 0.025f),
                new Color(0.0f, 0.0f, 0.0f, 0.0f)
            },
            Offsets = new[] { 0.0f, 0.38f, 0.78f, 1.0f }
        };
        _glowTexture = new GradientTexture2D
        {
            Gradient = glowGradient,
            Width = 256,
            Height = 256,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1.0f, 0.5f)
        };

        _zoomRoot = new Control
        {
            Name = "ZoomRoot",
            MouseFilter = MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(WorldSize, WorldSize)
        };
        AddChild(_zoomRoot);

        _graphLayer = new TreeGraphLayer
        {
            Name = "GraphLayer",
            View = this,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(WorldSize, WorldSize)
        };
        _zoomRoot.AddChild(_graphLayer);

        // Tooltip shell lives in passive_view.tscn under TreeView; only text/position are dynamic.
        _tooltipPanel = GetNodeOrNull<PanelContainer>("TreeTooltip");
        _tooltipText = GetNodeOrNull<RichTextLabel>("TreeTooltip/TreeTooltipText");

        MouseExited += () =>
        {
            _mouseInside = false;
            SetHoveredNode("");
        };

        Render(TreeClassKey);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            UpdateTransform();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        _time += (float)delta;
        if (_needsFit)
            FitTree();
        if (_nodeDragging && !Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _nodeDragging = false;
            _nodeDragId = "";
        }
        UpdateHoverFromMouse();
        UpdateButtonPositions();
        QueueRedraw();
        _graphLayer?.QueueRedraw();
        UpdateTooltipPosition();
    }

    public void Render(string classKey)
    {
        TreeClassKey = string.IsNullOrEmpty(classKey) ? "macrophage" : classKey;
        EnsureCameraForClass();

        foreach (var button in _buttons.Values)
        {
            if (IsInstanceValid(button))
            {
                button.GetParent()?.RemoveChild(button);
                button.QueueFree();
            }
        }
        _buttons.Clear();

        foreach (var node in _orderedNodes)
        {
            var button = CreateNodeButton(node);
            _buttons[node.Id] = button;
            _zoomRoot?.AddChild(button);
        }

        if (!string.IsNullOrEmpty(SelectedNodeId) && !PassiveTreeManager.IsKnownNode(SelectedNodeId))
            SelectedNodeId = "";
        if (!string.IsNullOrEmpty(HoveredNodeId) && !PassiveTreeManager.IsKnownNode(HoveredNodeId))
            SetHoveredNode("");

        UpdateTransform();
        UpdateButtonPositions();
        _tooltipShownId = "\0";
        UpdateTooltipText();
        QueueRedraw();
        _graphLayer?.QueueRedraw();
    }

    public void SelectNode(string nodeId)
    {
        SelectedNodeId = PassiveTreeManager.IsKnownNode(nodeId) ? nodeId : "";
        _graphLayer?.QueueRedraw();
    }

    public void ActivateNode(string nodeId)
    {
        if (!PassiveTreeManager.IsKnownNode(nodeId))
            return;
        SelectedNodeId = nodeId;
        EmitSignal(SignalName.TreeNodeActivated, nodeId);
        _graphLayer?.QueueRedraw();
    }

    public void RefundNode(string nodeId)
    {
        if (!PassiveTreeManager.IsKnownNode(nodeId))
            return;
        SelectedNodeId = nodeId;
        EmitSignal(SignalName.TreeNodeRefundRequested, nodeId);
        _graphLayer?.QueueRedraw();
    }

    public void SetHoveredNode(string nodeId)
    {
        string next = PassiveTreeManager.IsKnownNode(nodeId) ? nodeId : "";
        if (next == HoveredNodeId)
            return;

        HoveredNodeId = next;
        EmitSignal(SignalName.TreeNodeHovered, next);
        UpdateTooltipText();
        _graphLayer?.QueueRedraw();
    }

    public bool FitTree()
    {
        Rect2 bounds = GetTreeBounds();
        if (Size.X <= 0.0f || Size.Y <= 0.0f || bounds.Size.X <= 0.0f || bounds.Size.Y <= 0.0f)
            return false;

        _zoom = Mathf.Clamp(Mathf.Min(Size.X / bounds.Size.X, Size.Y / bounds.Size.Y) * FitZoomBoost, MinZoom, MaxZoom);
        _camera = bounds.GetCenter();
        _needsFit = false;
        ClampCamera();
        UpdateTransform();
        return true;
    }

    public void ZoomStep(float factor)
    {
        if (factor <= 0.0f)
            return;

        _zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
        UpdateTransform();
    }

    public void ZoomAt(Vector2 viewPosition, float newZoom)
    {
        if (Size.X <= 0.0f || Size.Y <= 0.0f)
        {
            ZoomStep(newZoom / Mathf.Max(0.001f, _zoom));
            return;
        }

        Vector2 world = ScreenToWorld(viewPosition);
        _zoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        _camera = world - (viewPosition - Size * 0.5f) / _zoom;
        ClampCamera();
        UpdateTransform();
    }

    public Rect2 GetTreeBounds()
    {
        bool started = false;
        Vector2 min = Vector2.Zero;
        Vector2 max = Vector2.Zero;

        void Include(Vector2 point)
        {
            if (!started)
            {
                min = point;
                max = point;
                started = true;
            }
            else
            {
                min = new Vector2(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
                max = new Vector2(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
            }
        }

        foreach (var node in PassiveTreeManager.Nodes)
            Include(node.Position);

        foreach (string branch in BranchOrder)
        {
            if (!PassiveTreeManager.TryGetRegionCenter(branch, out _))
                continue;
            Rect2 region = PassiveTreeManager.GetRegionBounds(branch);
            Include(region.Position);
            Include(region.End);
        }

        Rect2 board = GetRegionBoardBounds();
        if (board.Size.X > 0.0f || board.Size.Y > 0.0f)
        {
            Include(new Vector2(board.GetCenter().X, board.Position.Y - PassiveTreeManager.GridStep));
            Include(new Vector2(board.GetCenter().X, board.End.Y + PassiveTreeManager.GridStep));
        }

        if (!started)
            return new Rect2(Vector2.Zero, Vector2.Zero);

        return new Rect2(min - new Vector2(200, 170), max - min + new Vector2(400, 340));
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            _mousePosition = motion.Position;
            _mouseInside = true;
            if (_panning && (_panButton == MouseButton.Middle || (_panButton == MouseButton.Left && _panMoved)))
            {
                _camera = _panStartCamera - (motion.Position - _panStartMouse) / _zoom;
                ClampCamera();
                UpdateTransform();
            }
            else if (_panning && _panButton == MouseButton.Left)
            {
                if ((motion.Position - _panStartMouse).Length() > 6.0f)
                {
                    _panMoved = true;
                    _camera = _panStartCamera - (motion.Position - _panStartMouse) / _zoom;
                    ClampCamera();
                    UpdateTransform();
                }
            }
            UpdateTooltipText();
            UpdateTooltipPosition();
            AcceptEvent();
        }
        else if (@event is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.WheelUp && button.Pressed)
            {
                ZoomAt(button.Position, _zoom * 1.15f);
                AcceptEvent();
            }
            else if (button.ButtonIndex == MouseButton.WheelDown && button.Pressed)
            {
                ZoomAt(button.Position, _zoom / 1.15f);
                AcceptEvent();
            }
            else if ((button.ButtonIndex == MouseButton.Left || button.ButtonIndex == MouseButton.Middle) && button.Pressed)
            {
                _panning = true;
                _panButton = button.ButtonIndex;
                _panStartMouse = button.Position;
                _panStartCamera = _camera;
                _panMoved = button.ButtonIndex == MouseButton.Middle;
                AcceptEvent();
            }
            else if ((button.ButtonIndex == MouseButton.Left || button.ButtonIndex == MouseButton.Middle) && !button.Pressed)
            {
                _panning = false;
                _panMoved = false;
                AcceptEvent();
            }
        }
    }

    public static string? HitTestWorldPosition(Vector2 worldPosition)
    {
        string? bestId = null;
        float bestScore = float.MaxValue;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            float hitRadius = GetNodeRadius(node) + 22.0f;
            float distance = worldPosition.DistanceTo(node.Position);
            if (distance > hitRadius)
                continue;

            float score = distance / hitRadius;
            if (score < bestScore)
            {
                bestScore = score;
                bestId = node.Id;
            }
        }
        return bestId;
    }

    private void UpdateHoverFromMouse()
    {
        if (GetViewport() == null || !Visible)
        {
            _mouseInside = false;
            SetHoveredNode("");
            UpdateTooltipText();
            return;
        }

        Vector2 local = GetLocalMousePosition();
        if (local.X < 0.0f || local.Y < 0.0f || local.X > Size.X || local.Y > Size.Y)
        {
            _mouseInside = false;
            SetHoveredNode("");
            UpdateTooltipText();
            return;
        }

        UpdateHoverAt(local);
    }

    /// <summary>
    /// Resolves the hovered node and refreshes the tooltip for a view-local
    /// mouse position. Kept separate from the OS mouse so tests and tools can
    /// drive hover deterministically.
    /// </summary>
    internal void UpdateHoverAt(Vector2 viewPosition)
    {
        _mouseInside = true;
        _mousePosition = viewPosition;
        SetHoveredNode(HitTestVisualPosition(ScreenToWorld(viewPosition)) ?? "");
        UpdateTooltipText();
    }

    private string? HitTestVisualPosition(Vector2 worldPosition)
    {
        string? bestId = null;
        float bestScore = float.MaxValue;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            float half = GetNodeRadius(node) + 17.0f;
            Vector2 delta = worldPosition - node.Position;
            if (Mathf.Abs(delta.X) > half || Mathf.Abs(delta.Y) > half)
                continue;

            float score = delta.Length() / half;
            if (score < bestScore)
            {
                bestScore = score;
                bestId = node.Id;
            }
        }
        return bestId;
    }

    private void EnsureCameraForClass()
    {
        if (_cameraClass == TreeClassKey)
            return;

        _cameraClass = TreeClassKey;
        if (!FitTree())
            _needsFit = true;
    }

    private Vector2 TransformTarget()
    {
        return Size * 0.5f - _camera * _zoom;
    }

    private void UpdateTransform()
    {
        if (_zoomRoot == null || Size.X <= 0.0f || Size.Y <= 0.0f)
            return;

        ClampCamera();
        _rootPosition = TransformTarget();
        _zoomRoot.Position = _rootPosition;
        _zoomRoot.Scale = new Vector2(_zoom, _zoom);
    }

    private void UpdateButtonPositions()
    {
        foreach (var pair in _buttons)
        {
            var button = pair.Value;
            if (!IsInstanceValid(button) || !PassiveTreeManager.TryGetNode(pair.Key, out var node))
                continue;

            Vector2 size = button.Size.X > 1.0f ? button.Size : button.CustomMinimumSize;
            button.Position = node.Position - size * 0.5f;
        }
    }

    private Vector2 ScreenToWorld(Vector2 viewPosition)
    {
        if (_zoomRoot == null)
            return viewPosition;
        return (viewPosition - _rootPosition) / _zoom;
    }

    private void ClampCamera()
    {
        if (Size.X <= 0.0f || Size.Y <= 0.0f)
            return;

        Rect2 bounds = GetTreeBounds();
        Vector2 halfView = Size / (2.0f * _zoom);
        if (bounds.Size.X <= halfView.X * 2.0f)
            _camera.X = bounds.GetCenter().X;
        else
            _camera.X = Mathf.Clamp(_camera.X, bounds.Position.X + halfView.X, bounds.End.X - halfView.X);
        if (bounds.Size.Y <= halfView.Y * 2.0f)
            _camera.Y = bounds.GetCenter().Y;
        else
            _camera.Y = Mathf.Clamp(_camera.Y, bounds.Position.Y + halfView.Y, bounds.End.Y - halfView.Y);
    }

    private Button CreateNodeButton(PassiveTreeManager.TreeNode node)
    {
        float radius = GetNodeRadius(node);
        Vector2 size = new Vector2(radius * 2.0f + 34.0f, radius * 2.0f + 34.0f);
        int stacks = PassiveTreeManager.GetNodeStacks(TreeClassKey, node.Id);
        bool available = PassiveTreeManager.CanPurchase(TreeClassKey, node.Id);
        var button = new Button
        {
            Name = "TreeNode_" + node.Id,
            CustomMinimumSize = size,
            Size = size,
            Position = node.Position - size * 0.5f,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = (stacks > 0 || available) ? CursorShape.PointingHand : CursorShape.Arrow,
            Text = "",
            ClipText = true,
            Alignment = HorizontalAlignment.Center
        };
        // Inset art so the canvas-drawn rarity frame (size/shape/color by
        // trait rarity) stays visible around it instead of being covered.
        float artSide = Mathf.Max(radius * 2.0f - 12.0f, 16.0f);
        var art = new TextureRect
        {
            Name = "NodeArt",
            Position = (size - new Vector2(artSide, artSide)) * 0.5f,
            CustomMinimumSize = new Vector2(artSide, artSide),
            Size = new Vector2(artSide, artSide),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        Texture2D? nodeTex = AssetLoader.TryLoad<Texture2D>(AssetPaths.TraitIcon(node.TraitId))
            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        if (nodeTex != null)
            art.Texture = nodeTex;
        button.AddChild(art);

        var transparent = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", transparent);
        button.AddThemeStyleboxOverride("hover", transparent);
        button.AddThemeStyleboxOverride("pressed", transparent);
        button.AddThemeStyleboxOverride("disabled", transparent);
        button.AddThemeStyleboxOverride("focus", transparent);
        // Locked-state readability rides on the art texture via Modulate.
        button.Modulate = stacks > 0 ? Colors.White
            : available ? Colors.White
            : new Color(0.45f, 0.50f, 0.58f, 1.0f);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));

        string localId = node.Id;
        button.GuiInput += (@event) => OnNodeGuiInput(button, localId, @event);
        button.MouseEntered += () => SetHoveredNode(localId);
        button.MouseExited += () => SetHoveredNode("");
        return button;
    }

    private void OnNodeGuiInput(Button button, string nodeId, InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Right)
            {
                if (mouse.Pressed)
                {
                    button.AcceptEvent();
                    RefundNode(nodeId);
                }
                return;
            }

            if (mouse.ButtonIndex != MouseButton.Left)
                return;

            if (mouse.Pressed)
            {
                _nodeDragging = true;
                _nodeDragId = nodeId;
                _nodePressPosition = GetLocalMousePosition();
                _panStartMouse = _nodePressPosition;
                _panStartCamera = _camera;
                _nodeDragMoved = false;
            }
            else
            {
                bool clicked = _nodeDragging && !_nodeDragMoved && _nodeDragId == nodeId;
                _nodeDragging = false;
                _nodeDragId = "";
                if (clicked)
                    ActivateNode(nodeId);
            }
            button.AcceptEvent();
            return;
        }

        if (@event is InputEventMouseMotion && _nodeDragging)
        {
            Vector2 local = GetLocalMousePosition();
            if ((local - _nodePressPosition).Length() > 6.0f)
                _nodeDragMoved = true;
            if (_nodeDragMoved)
            {
                _camera = _panStartCamera - (local - _panStartMouse) / _zoom;
                ClampCamera();
                UpdateTransform();
            }
            button.AcceptEvent();
        }
    }

    private void UpdateTooltipText()
    {
        if (_tooltipPanel == null || _tooltipText == null)
            return;

        string target = _mouseInside ? HoveredNodeId : "";
        if (target == _tooltipShownId)
            return;

        _tooltipShownId = target;
        if (string.IsNullOrEmpty(target))
        {
            _tooltipPanel.Visible = false;
            return;
        }

        string text = PassiveTreeManager.GetNodeTooltipText(TreeClassKey, target);
        if (string.IsNullOrEmpty(text))
        {
            _tooltipPanel.Visible = false;
            return;
        }

        _tooltipText.Text = text;
        _tooltipPanel.Visible = true;
        _tooltipPanel.ResetSize();
        UpdateTooltipPosition();
    }

    private void UpdateTooltipPosition()
    {
        if (_tooltipPanel == null || !_tooltipPanel.Visible)
            return;

        Vector2 offset = new Vector2(22, 24);
        Vector2 position = _mousePosition + offset;
        Vector2 size = _tooltipPanel.Size;
        if (position.X + size.X > Size.X - 8.0f)
            position.X = Mathf.Max(8.0f, _mousePosition.X - size.X - offset.X);
        if (position.Y + size.Y > Size.Y - 8.0f)
            position.Y = Mathf.Max(8.0f, _mousePosition.Y - size.Y - offset.Y);
        _tooltipPanel.Position = position;
    }

    // ------------------------------------------------------------------
    // Drawing
    // ------------------------------------------------------------------

    public override void _Draw()
    {
        DrawBackground();
    }

    private void DrawBackground()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);
        if (Size.X <= 0.0f || Size.Y <= 0.0f)
            return;

        Vector2 center = _rootPosition + PassiveTreeManager.WorldCenter * _zoom;
        if (_glowTexture != null)
        {
            float glowRadius = Mathf.Max(Size.X, Size.Y) * 1.05f;
            DrawTextureRect(_glowTexture, new Rect2(center - new Vector2(glowRadius, glowRadius), new Vector2(glowRadius * 2.0f, glowRadius * 2.0f)), false);
        }

        DrawGrid();

        var random = new RandomNumberGenerator { Seed = 90210 };
        Vector2 parallax = (_rootPosition - Size * 0.5f) * 0.06f;
        for (int i = 0; i < 120; i++)
        {
            Vector2 basePosition = new Vector2(random.Randf() * Size.X, random.Randf() * Size.Y);

            // Brownian motion: incommensurate multi-frequency jitter instead of a
            // single smooth sine so each dust mote wanders irregularly (docs/tutorial.md §3).
            float phase = i * 1.618f;
            Vector2 drift = new Vector2(
                Mathf.Sin(_time * (0.55f + (i % 3) * 0.17f) + phase)
                    + 0.35f * Mathf.Sin(_time * 2.3f + phase * 2.7f),
                Mathf.Cos(_time * (0.48f + (i % 4) * 0.13f) + phase * 1.31f)
                    + 0.35f * Mathf.Cos(_time * 1.9f + phase * 3.1f)) * (5.0f + (i % 5) * 2.0f);

            float twinkle = 0.04f + 0.04f * Mathf.Sin(_time * 1.3f + i * 0.7f);
            Color dust = i % 7 == 0
                ? new Color(1.0f, 0.55f, 0.35f, twinkle)
                : new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, twinkle);
            DrawCircle(basePosition + drift + parallax, i % 11 == 0 ? 2.2f : 1.5f, dust);
        }

        DrawBranchLabels();
    }

    private void DrawGrid()
    {
        float step = PassiveTreeManager.GridStep;
        Vector2 center = PassiveTreeManager.WorldCenter;
        Rect2 bounds = GetTreeBounds();
        int minCol = Mathf.FloorToInt((bounds.Position.X - center.X) / step) - 1;
        int maxCol = Mathf.CeilToInt((bounds.End.X - center.X) / step) + 1;
        int minRow = Mathf.FloorToInt((center.Y - bounds.End.Y) / step) - 1;
        int maxRow = Mathf.CeilToInt((center.Y - bounds.Position.Y) / step) + 1;
        var fine = new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.045f);
        var strong = new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.085f);
        for (int col = minCol; col <= maxCol; col++)
        {
            float x = center.X + col * step;
            Color color = col % 2 == 0 ? strong : fine;
            DrawLine(WorldToScreen(new Vector2(x, bounds.Position.Y)),
                WorldToScreen(new Vector2(x, bounds.End.Y)), color, 1.0f);
        }
        for (int row = minRow; row <= maxRow; row++)
        {
            float y = center.Y - row * step;
            Color color = row % 2 == 0 ? strong : fine;
            DrawLine(WorldToScreen(new Vector2(bounds.Position.X, y)),
                WorldToScreen(new Vector2(bounds.End.X, y)), color, 1.0f);
        }
    }

    private Vector2 WorldToScreen(Vector2 world)
    {
        return _rootPosition + world * _zoom;
    }

    private void DrawBranchLabels()
    {
        Font font = ThemeDB.FallbackFont;
        Rect2 board = GetRegionBoardBounds();
        foreach (string branch in BranchOrder)
        {
            if (!PassiveTreeManager.TryGetRegionCenter(branch, out _))
                continue;

            // Regions tile edge-to-edge; each label sits in the outer margin of
            // its own board column (left column labels left, right column right).
            Rect2 region = PassiveTreeManager.GetRegionBounds(branch);
            bool leftColumn = region.GetCenter().X <= board.GetCenter().X;
            Vector2 edge = WorldToScreen(new Vector2(leftColumn ? board.Position.X : board.End.X, region.GetCenter().Y));
            string label = GetBranchLabel(branch);
            Vector2 textSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, 14);
            Vector2 origin = new(leftColumn ? edge.X - textSize.X - 14.0f : edge.X + 14.0f, edge.Y - textSize.Y * 0.5f);
            if (origin.X < 4.0f || origin.Y < 8.0f || origin.X + textSize.X > Size.X - 4.0f || origin.Y > Size.Y - 8.0f)
                continue;

            DrawString(font, origin + new Vector2(1.0f, 1.0f), label, HorizontalAlignment.Left, -1, 14, new Color(0.0f, 0.0f, 0.0f, 0.55f));
            DrawString(font, origin, label, HorizontalAlignment.Left, -1, 14, GetBranchColor(branch, 0.62f));
        }
    }

    /// <summary>Union of all region squares: the seamless 15x10 board.</summary>
    private static Rect2 GetRegionBoardBounds()
    {
        bool started = false;
        Rect2 board = new();
        foreach (string branch in BranchOrder)
        {
            if (!PassiveTreeManager.TryGetRegionCenter(branch, out _))
                continue;
            Rect2 region = PassiveTreeManager.GetRegionBounds(branch);
            board = started ? board.Merge(region) : region;
            started = true;
        }
        return board;
    }

    private void DrawGraph()
    {
        if (_graphLayer == null)
            return;

        var owned = PassiveTreeManager.GetAllocation(TreeClassKey);
        string start = PassiveTreeManager.GetStartNode(TreeClassKey);

        DrawBands(_graphLayer);
        DrawEdges(_graphLayer, owned, start);
        DrawNodes(_graphLayer, owned, start);
    }

    private void DrawBands(Control layer)
    {
        foreach (string branch in BranchOrder)
        {
            if (!PassiveTreeManager.TryGetRegionCenter(branch, out Vector2 center))
                continue;

            Color color = GetBranchColor(branch, 1.0f);
            Rect2 rect = PassiveTreeManager.GetRegionBounds(branch);
            bool core = branch == "core";

            // 1. Organic territorial background wash with subtle breathing alpha
            float breath = 0.88f + 0.12f * Mathf.Sin(_time * 1.8f + rect.Position.X * 0.01f);
            layer.DrawRect(rect, new Color(color.R, color.G, color.B, (core ? 0.06f : 0.038f) * breath));

            // 2. Epigenetic Chromatin Fiber Network (sinusoidal micro-filaments weaving across territory)
            DrawChromatinFibers(layer, rect, color, branch);

            // 3. DNA Methylation / Histone Octamer Hubs
            DrawMethylationHubs(layer, rect, center, color);

            // 4. Bio-specimen containment brackets at the 4 corners of each territory
            DrawRegionSpecimenBrackets(layer, rect, color, core);
        }
    }

    private void DrawChromatinFibers(Control layer, Rect2 rect, Color color, string branch)
    {
        int branchSeed = branch.GetHashCode();
        float fiberAlpha = 0.07f + 0.03f * Mathf.Sin(_time * 1.2f + branchSeed);
        Color strandColor = new(color.R, color.G, color.B, fiberAlpha);

        // Sinusoidal chromatin strands weaving horizontally
        for (int s = 0; s < 2; s++)
        {
            float baseY = rect.Position.Y + rect.Size.Y * (0.28f + s * 0.44f);
            int segments = 16;
            var points = new Vector2[segments + 1];
            float stepX = rect.Size.X / segments;
            float freq = 0.012f + s * 0.006f;
            float amp = 14.0f + s * 6.0f;
            float phase = _time * (0.4f + s * 0.2f) + s * 1.8f + branchSeed * 0.01f;

            for (int p = 0; p <= segments; p++)
            {
                float x = rect.Position.X + p * stepX;
                float y = baseY + amp * Mathf.Sin(x * freq + phase);
                points[p] = new Vector2(x, y);
            }
            layer.DrawPolyline(points, strandColor, 1.2f, true);
        }
    }

    private void DrawMethylationHubs(Control layer, Rect2 rect, Vector2 center, Color color)
    {
        // Hub at center and two secondary hubs offset diagonally
        Vector2[] hubs =
        {
            center,
            new(rect.Position.X + rect.Size.X * 0.28f, rect.Position.Y + rect.Size.Y * 0.32f),
            new(rect.Position.X + rect.Size.X * 0.72f, rect.Position.Y + rect.Size.Y * 0.68f)
        };

        for (int h = 0; h < hubs.Length; h++)
        {
            Vector2 pos = hubs[h];
            float hubPulse = 1.0f + 0.15f * Mathf.Sin(_time * 2.2f + h * 1.5f);
            float haloRadius = (h == 0 ? 16.0f : 10.0f) * hubPulse;

            // Outer soft bio-respiration halo
            layer.DrawCircle(pos, haloRadius, new Color(color.R, color.G, color.B, 0.05f));

            // Concentric methylation ring
            layer.DrawArc(pos, (h == 0 ? 9.0f : 6.0f) * hubPulse, 0.0f, Mathf.Tau, 18,
                new Color(color.R, color.G, color.B, 0.22f), 1.0f);

            // Core histone pip
            layer.DrawCircle(pos, h == 0 ? 2.8f : 2.0f, new Color(color.R, color.G, color.B, 0.40f));
            layer.DrawCircle(pos, 1.0f, Colors.White);

            // 4 tiny methylation markers at cardinal offsets
            float markDist = (h == 0 ? 12.0f : 8.0f) * hubPulse;
            Color markColor = new(color.R, color.G, color.B, 0.30f);
            layer.DrawCircle(pos + new Vector2(markDist, 0), 1.2f, markColor);
            layer.DrawCircle(pos - new Vector2(markDist, 0), 1.2f, markColor);
            layer.DrawCircle(pos + new Vector2(0, markDist), 1.2f, markColor);
            layer.DrawCircle(pos - new Vector2(0, markDist), 1.2f, markColor);
        }
    }

    private static void DrawRegionSpecimenBrackets(Control layer, Rect2 rect, Color color, bool core)
    {
        float arm = 22.0f;
        float pad = 4.0f;
        Color bracketColor = new(color.R, color.G, color.B, core ? 0.38f : 0.24f);
        Color dotColor = new(color.R, color.G, color.B, core ? 0.65f : 0.45f);
        float width = core ? 2.0f : 1.5f;

        Vector2 tl = rect.Position + new Vector2(pad, pad);
        Vector2 tr = new(rect.End.X - pad, rect.Position.Y + pad);
        Vector2 bl = new(rect.Position.X + pad, rect.End.Y - pad);
        Vector2 br = rect.End - new Vector2(pad, pad);

        // Top-Left
        layer.DrawLine(tl, tl + new Vector2(arm, 0), bracketColor, width);
        layer.DrawLine(tl, tl + new Vector2(0, arm), bracketColor, width);
        layer.DrawCircle(tl + new Vector2(2, 2), 1.5f, dotColor);

        // Top-Right
        layer.DrawLine(tr, tr - new Vector2(arm, 0), bracketColor, width);
        layer.DrawLine(tr, tr + new Vector2(0, arm), bracketColor, width);
        layer.DrawCircle(tr + new Vector2(-2, 2), 1.5f, dotColor);

        // Bottom-Left
        layer.DrawLine(bl, bl + new Vector2(arm, 0), bracketColor, width);
        layer.DrawLine(bl, bl - new Vector2(0, arm), bracketColor, width);
        layer.DrawCircle(bl + new Vector2(2, -2), 1.5f, dotColor);

        // Bottom-Right
        layer.DrawLine(br, br - new Vector2(arm, 0), bracketColor, width);
        layer.DrawLine(br, br - new Vector2(0, arm), bracketColor, width);
        layer.DrawCircle(br - new Vector2(-2, -2), 1.5f, dotColor);

        // Delicate perimeter border
        layer.DrawRect(rect, new Color(color.R, color.G, color.B, core ? 0.08f : 0.045f), false, 1.0f);
    }

    private void DrawEdges(Control layer, Godot.Collections.Dictionary<string, int> owned, string start)
    {
        for (int i = 0; i < PassiveTreeManager.Edges.Length; i++)
        {
            var edge = PassiveTreeManager.Edges[i];
            if (!PassiveTreeManager.TryGetNode(edge.From, out var fromNode) || !PassiveTreeManager.TryGetNode(edge.To, out var toNode))
                continue;

            Vector2 from = fromNode.Position;
            Vector2 to = toNode.Position;
            if (from.DistanceSquaredTo(to) < 1.0f)
                continue;

            bool fromActive = owned.ContainsKey(edge.From) || edge.From == start;
            bool toActive = owned.ContainsKey(edge.To) || edge.To == start;
            bool lit = fromActive && toActive;
            bool highlighted = HoveredNodeId == edge.From || HoveredNodeId == edge.To
                || SelectedNodeId == edge.From || SelectedNodeId == edge.To;
            Color color = GetEdgeColor(lit, highlighted);
            float width = (lit ? 4.4f : 2.0f) + (highlighted ? 1.4f : 0.0f);

            layer.DrawLine(from, to, new Color(color.R, color.G, color.B, color.A * 0.28f), width * 3.0f, true);
            layer.DrawLine(from, to, color, width, true);

            if (lit)
            {
                Vector2[] points = { from, to };
                float travel = Mathf.PosMod(_time * 0.24f + i * 0.137f, 1.0f);

                // Multi-stage fluorophore excitation pulse: brilliant plasma head with fading phosphor tail
                Vector2 head = PointAlong(points, travel);
                layer.DrawCircle(head, 10.0f, new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.20f));
                layer.DrawCircle(head, 4.5f, Colors.White);

                for (int trail = 1; trail <= 5; trail++)
                {
                    float t = Mathf.PosMod(travel - trail * 0.032f, 1.0f);
                    float fade = 1.0f - trail / 6.0f;
                    layer.DrawCircle(PointAlong(points, t), 3.8f * fade,
                        new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.50f * fade));
                }

                // Secondary harmonic pulse in alternating rhythm
                float echo = Mathf.PosMod(travel + 0.35f, 1.0f);
                layer.DrawCircle(PointAlong(points, echo), 2.8f, new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.60f));
                layer.DrawCircle(PointAlong(points, echo), 1.2f, Colors.White);
            }
        }
    }

    private static Vector2 PointAlong(Vector2[] points, float t)
    {
        if (points.Length < 2)
            return points.Length == 1 ? points[0] : Vector2.Zero;

        float scaled = Mathf.Clamp(t, 0.0f, 1.0f) * (points.Length - 1);
        int index = Mathf.Min((int)scaled, points.Length - 2);
        float local = scaled - index;
        return points[index].Lerp(points[index + 1], local);
    }

    private void DrawNodes(Control layer, Godot.Collections.Dictionary<string, int> owned, string start)
    {
        foreach (var node in _orderedNodes)
        {
            Vector2 position = node.Position;
            int stacks = owned.TryGetValue(node.Id, out int ownedStacks) ? ownedStacks : 0;
            bool hovered = HoveredNodeId == node.Id;
            bool selected = SelectedNodeId == node.Id;
            bool isStart = node.Id == start;
            float radius = GetNodeRadius(node);
            float wobblePhase = Hash(node.Id) * Mathf.Tau;
            Color rarityColor = GetRarityColor(node.Rarity);

            if (hovered)
                layer.DrawCircle(position, radius + 18.0f, new Color(1, 1, 1, 0.08f));

            // Breathing vesicle glow: same-rarity nodes share one glow color;
            // lit nodes pulse stronger than dormant ones.
            float breath = 0.75f + 0.25f * Mathf.Sin(_time * (stacks > 0 ? 2.0f : 1.2f) + wobblePhase);
            float halo = (stacks > 0 ? 0.34f : 0.06f) * breath;
            layer.DrawCircle(position, radius + 7.0f + (stacks > 0 ? 1.6f * breath : 0.0f),
                new Color(rarityColor.R, rarityColor.G, rarityColor.B, halo));

            Color fill = stacks > 0
                ? new Color(rarityColor.R, rarityColor.G, rarityColor.B, 0.34f)
                : new Color(0.035f, 0.065f, 0.11f, 0.90f);
            Color dormantEdge = rarityColor.Darkened(0.45f);
            Color edge = stacks > 0
                ? rarityColor.Lightened(0.25f)
                : new Color(dormantEdge.R, dormantEdge.G, dormantEdge.B, hovered ? 0.75f : 0.5f);

            layer.DrawColoredPolygon(NodeShape(node, radius), fill);
            layer.DrawPolyline(ClosedShape(node, radius), edge,
                node.Rarity == PassiveTreeManager.TreeRarity.Unique ? 7.0f : 5.0f, true);

            if (isStart)
            {
                float pulse = 7.0f + 2.4f * Mathf.Sin(_time * 2.6f);
                layer.DrawArc(position, radius + pulse, 0.0f, Mathf.Tau, 64, new Color(0.45f, 0.95f, 1.0f, 0.85f), 3.0f, true);
            }
            if (selected)
                layer.DrawArc(position, radius + 10.0f, 0.0f, Mathf.Tau, 64, new Color(1.0f, 0.90f, 0.42f, 0.95f), 3.5f, true);
        }
    }

    // ------------------------------------------------------------------
    // Layout helpers
    // ------------------------------------------------------------------

    private static float GetNodeRadius(PassiveTreeManager.TreeNode node)
    {
        return node.Rarity switch
        {
            PassiveTreeManager.TreeRarity.Unique => 52.0f,
            PassiveTreeManager.TreeRarity.Start => 38.0f,
            PassiveTreeManager.TreeRarity.Rare => 38.0f,
            PassiveTreeManager.TreeRarity.Magic => 26.0f,
            _ => 16.0f
        };
    }

    private static float Hash(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return (hash % 997) / 997.0f;
    }

    private static Vector2[] NodeShape(PassiveTreeManager.TreeNode node, float radius)
    {
        // All rarities share one circular frame; rarity reads from size + color.
        // No wobble here: the distortion made circles read as lumpy.
        return PolygonPoints(node.Position, radius, 48, 0.0f);
    }

    private static Vector2[] ClosedShape(PassiveTreeManager.TreeNode node, float radius)
    {
        var shape = NodeShape(node, radius);
        var closed = new Vector2[shape.Length + 1];
        System.Array.Copy(shape, closed, shape.Length);
        if (shape.Length > 0)
            closed[shape.Length] = shape[0];
        return closed;
    }

    private static Vector2[] PolygonPoints(Vector2 center, float radius, int sides, float rotation)
    {
        var points = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = rotation + Mathf.Tau * i / sides;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    private static Color GetRarityColor(PassiveTreeManager.TreeRarity rarity)
    {
        return rarity switch
        {
            PassiveTreeManager.TreeRarity.Magic => new Color(0.32f, 0.72f, 1.00f, 1.0f),
            PassiveTreeManager.TreeRarity.Rare => new Color(1.00f, 0.78f, 0.32f, 1.0f),
            PassiveTreeManager.TreeRarity.Unique => new Color(1.00f, 0.48f, 0.22f, 1.0f),
            PassiveTreeManager.TreeRarity.Start => new Color(0.45f, 0.95f, 1.00f, 1.0f),
            _ => new Color(1.0f, 1.0f, 1.0f, 1.0f)
        };
    }

    private static Color GetBranchColor(string branch, float alpha)
    {
        Color color = BranchColors.TryGetValue(branch, out var branchColor) ? branchColor : new Color(0.55f, 0.68f, 0.85f, 1.0f);
        return new Color(color.R, color.G, color.B, alpha);
    }

    private static string GetBranchLabel(string branch)
    {
        return branch switch
        {
            "precision" => TranslationServer.Translate("TREE_BRANCH_PRECISION"),
            "senses" => TranslationServer.Translate("TREE_BRANCH_SENSES"),
            "motility" => TranslationServer.Translate("TREE_BRANCH_MOTILITY"),
            "ballistics" => TranslationServer.Translate("TREE_BRANCH_BALLISTICS"),
            "vitality" => TranslationServer.Translate("TREE_BRANCH_VITALITY"),
            "core" => TranslationServer.Translate("TREE_BRANCH_CORE"),
            _ => branch
        };
    }

    /// <summary>
    /// Single unified edge color: only lit (both ends owned) vs unlit is
    /// distinguished. Hovered edges flash white; width already encodes lit
    /// state at the call site.
    /// </summary>
    private static readonly Color EdgeBaseColor = new(0.34f, 0.48f, 0.66f, 1.0f);

    private static Color GetEdgeColor(bool lit, bool highlighted)
    {
        if (!lit)
            return new Color(EdgeBaseColor.R, EdgeBaseColor.G, EdgeBaseColor.B, 0.26f);
        Color litColor = highlighted ? Colors.White : EdgeBaseColor.Lightened(0.18f);
        return new Color(litColor.R, litColor.G, litColor.B, highlighted ? 0.98f : 0.80f);
    }
}
