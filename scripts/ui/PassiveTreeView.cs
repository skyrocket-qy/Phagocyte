using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Confocal-fluorescence grid board for the shared passive tree.
/// The pluripotent HSC sits at the lattice center; five lineage bands and the
/// core metabolism block are wired by orthogonal microtubule traces between
/// adjacent cells, so routes never overlap or cross.
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

    private static readonly string[] BranchOrder = { "precision", "senses", "motility", "ballistics", "vitality" };

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

        _tooltipPanel = new PanelContainer
        {
            Name = "TreeTooltip",
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 20,
            CustomMinimumSize = new Vector2(330, 0)
        };
        var tooltipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.05f, 0.09f, 0.96f),
            BorderColor = new Color(0.35f, 0.92f, 1.0f, 0.55f),
            ContentMarginLeft = 14.0f,
            ContentMarginRight = 14.0f,
            ContentMarginTop = 10.0f,
            ContentMarginBottom = 10.0f
        };
        tooltipStyle.SetBorderWidthAll(1);
        tooltipStyle.SetCornerRadiusAll(8);
        _tooltipPanel.AddThemeStyleboxOverride("panel", tooltipStyle);

        _tooltipText = new RichTextLabel
        {
            Name = "TreeTooltipText",
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(302, 0)
        };
        _tooltipText.AddThemeFontSizeOverride("normal_font_size", 13);
        _tooltipText.AddThemeFontSizeOverride("bold_font_size", 14);
        _tooltipPanel.AddChild(_tooltipText);
        AddChild(_tooltipPanel);

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

        var ordered = new List<PassiveTreeManager.TreeNode>(PassiveTreeManager.Nodes);
        ordered.Sort((a, b) =>
        {
            int rarity = a.Rarity.CompareTo(b.Rarity);
            return rarity != 0 ? rarity : string.Compare(a.Id, b.Id, System.StringComparison.Ordinal);
        });

        foreach (var node in ordered)
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

        _zoom = Mathf.Clamp(Mathf.Min(Size.X / bounds.Size.X, Size.Y / bounds.Size.Y), MinZoom, MaxZoom);
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
        foreach (var node in PassiveTreeManager.Nodes)
        {
            if (!started)
            {
                min = node.Position;
                max = node.Position;
                started = true;
            }
            else
            {
                min = new Vector2(Mathf.Min(min.X, node.Position.X), Mathf.Min(min.Y, node.Position.Y));
                max = new Vector2(Mathf.Max(max.X, node.Position.X), Mathf.Max(max.Y, node.Position.Y));
            }
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
            Text = node.Icon,
            ClipText = true,
            Alignment = HorizontalAlignment.Center
        };

        var transparent = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", transparent);
        button.AddThemeStyleboxOverride("hover", transparent);
        button.AddThemeStyleboxOverride("pressed", transparent);
        button.AddThemeStyleboxOverride("disabled", transparent);
        button.AddThemeStyleboxOverride("focus", transparent);
        button.AddThemeFontOverride("font", ThemeDB.FallbackFont);
        button.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(radius * (node.Rarity == PassiveTreeManager.TreeRarity.Unique ? 0.95f : 0.82f)));
        Color iconColor = stacks > 0 ? new Color(1, 1, 1, 1)
            : available ? new Color(0.88f, 0.98f, 1.0f, 1.0f)
            : new Color(0.60f, 0.68f, 0.78f, 1.0f);
        button.AddThemeColorOverride("font_color", iconColor);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", iconColor);
        button.AddThemeConstantOverride("outline_size", 8);
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

        DrawLegend();
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
        foreach (string branch in BranchOrder)
        {
            Rect2 bounds = GetBranchBounds(branch);
            if (bounds.Size.X <= 0.0f && bounds.Size.Y <= 0.0f)
                continue;

            Vector2 farthest = bounds.GetCenter();
            float bestDistance = -1.0f;
            foreach (var node in PassiveTreeManager.Nodes)
            {
                if (node.Branch != branch)
                    continue;
                float distance = node.Position.DistanceTo(PassiveTreeManager.WorldCenter);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    farthest = node.Position;
                }
            }

            Vector2 direction = farthest - PassiveTreeManager.WorldCenter;
            if (direction.LengthSquared() < 1.0f)
                direction = new Vector2(0, -1);
            Vector2 world = farthest + direction.Normalized() * 44.0f;
            Vector2 screen = WorldToScreen(world);
            if (screen.X < 60.0f || screen.Y < 20.0f || screen.X > Size.X - 60.0f || screen.Y > Size.Y - 20.0f)
                continue;

            string label = GetBranchLabel(branch);
            Vector2 textSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, 14);
            Vector2 origin = screen - new Vector2(textSize.X * 0.5f, textSize.Y * 0.5f);
            DrawString(font, origin + new Vector2(1.0f, 1.0f), label, HorizontalAlignment.Left, -1, 14, new Color(0.0f, 0.0f, 0.0f, 0.55f));
            DrawString(font, origin, label, HorizontalAlignment.Left, -1, 14, GetBranchColor(branch, 0.62f));
        }
    }

    private void DrawLegend()
    {
        Font font = ThemeDB.FallbackFont;
        float y = Size.Y - 74.0f;
        var rows = new (Color Dot, string Key)[]
        {
            (BranchColors["core"], "TREE_RING_NUCLEUS"),
            (PlasmaCyan, "TREE_RING_CYTOPLASM"),
            (BranchColors["vitality"], "TREE_RING_MEMBRANE")
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float rowY = y + i * 17.0f;
            DrawCircle(new Vector2(24.0f, rowY - 4.0f), 4.0f, new Color(rows[i].Dot.R, rows[i].Dot.G, rows[i].Dot.B, 0.85f));
            DrawString(font, new Vector2(36.0f, rowY), TranslationServer.Translate(rows[i].Key),
                HorizontalAlignment.Left, -1, 12, new Color(0.72f, 0.84f, 0.94f, 0.55f));
        }
    }

    private void DrawGraph()
    {
        if (_graphLayer == null)
            return;

        var owned = PassiveTreeManager.GetAllocation(TreeClassKey);
        string start = PassiveTreeManager.GetStartNode(TreeClassKey);

        DrawBands(_graphLayer);
        DrawEdges(_graphLayer, owned, start);
        DrawNucleus(_graphLayer);
        DrawNodes(_graphLayer, owned, start);
    }

    private void DrawBands(Control layer)
    {
        float pad = PassiveTreeManager.GridStep * 0.62f;
        foreach (string branch in BranchOrder)
        {
            Rect2 bounds = GetBranchBounds(branch);
            if (bounds.Size.X <= 0.0f && bounds.Size.Y <= 0.0f)
                continue;

            Color color = GetBranchColor(branch, 1.0f);
            Rect2 rect = ExpandBounds(bounds, pad);
            layer.DrawRect(rect, new Color(color.R, color.G, color.B, 0.040f));
            layer.DrawRect(rect, new Color(color.R, color.G, color.B, 0.14f), false, 1.5f);
        }

        Rect2 core = GetBranchBounds("core");
        if (core.Size.X > 0.0f || core.Size.Y > 0.0f)
        {
            Color coreColor = GetBranchColor("core", 1.0f);
            Rect2 rect = ExpandBounds(core, pad * 1.2f);
            layer.DrawRect(rect, new Color(coreColor.R, coreColor.G, coreColor.B, 0.055f));
            layer.DrawRect(rect, new Color(coreColor.R, coreColor.G, coreColor.B, 0.20f), false, 1.8f);
        }
    }

    private static Rect2 GetBranchBounds(string branch)
    {
        bool started = false;
        Vector2 min = Vector2.Zero;
        Vector2 max = Vector2.Zero;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            if (node.Branch != branch)
                continue;
            if (!started)
            {
                min = node.Position;
                max = node.Position;
                started = true;
            }
            else
            {
                min = new Vector2(Mathf.Min(min.X, node.Position.X), Mathf.Min(min.Y, node.Position.Y));
                max = new Vector2(Mathf.Max(max.X, node.Position.X), Mathf.Max(max.Y, node.Position.Y));
            }
        }
        if (!started)
            return new Rect2(Vector2.Zero, Vector2.Zero);
        return new Rect2(min, max - min);
    }

    private static Rect2 ExpandBounds(Rect2 bounds, float pad)
    {
        return new Rect2(bounds.Position - new Vector2(pad, pad), bounds.Size + new Vector2(pad * 2.0f, pad * 2.0f));
    }

    private void DrawNucleus(Control layer)
    {
        Vector2 center = PassiveTreeManager.WorldCenter;
        float pulse = 1.0f + 0.03f * Mathf.Sin(_time * 1.7f);
        for (int i = 0; i < 9; i++)
        {
            float radius = (168.0f - i * 15.0f) * pulse;
            layer.DrawCircle(center, radius, new Color(0.55f, 0.92f, 1.0f, 0.012f + i * 0.0018f));
        }

        layer.DrawCircle(center, 70.0f * pulse, new Color(0.045f, 0.11f, 0.19f, 0.96f));
        layer.DrawArc(center, 74.0f * pulse, 0.0f, Mathf.Tau, 64, new Color(0.55f, 0.95f, 1.0f, 0.45f), 2.4f, true);
        layer.DrawArc(center, 56.0f * pulse, 0.0f, Mathf.Tau, 64, new Color(0.76f, 0.55f, 1.0f, 0.20f), 1.6f, true);

        for (int i = 0; i < 3; i++)
        {
            float startAngle = _time * (0.45f + i * 0.16f) + i * 2.1f;
            layer.DrawArc(center, 30.0f + i * 12.0f, startAngle, startAngle + 2.1f, 32,
                new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.24f - i * 0.05f), 3.0f, true);
        }
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
            Color color = GetEdgeColor(edge.From, edge.To, lit, highlighted);
            float width = (lit ? 4.4f : 2.0f) + (highlighted ? 1.4f : 0.0f);

            layer.DrawLine(from, to, new Color(color.R, color.G, color.B, color.A * 0.26f), width * 2.8f, true);
            layer.DrawLine(from, to, color, width, true);

            if (lit)
            {
                Vector2[] points = { from, to };
                float travel = Mathf.PosMod(_time * 0.22f + i * 0.137f, 1.0f);

                // ATP bioelectric pulse: fading trail + soft glow behind the head.
                Vector2 head = PointAlong(points, travel);
                layer.DrawCircle(head, 9.0f, new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.16f));
                layer.DrawCircle(head, 4.2f, Colors.White);
                for (int trail = 1; trail <= 4; trail++)
                {
                    float t = Mathf.PosMod(travel - trail * 0.035f, 1.0f);
                    float fade = 1.0f - trail / 5.0f;
                    layer.DrawCircle(PointAlong(points, t), 3.6f * fade,
                        new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.45f * fade));
                }

                float echo = Mathf.PosMod(travel + 0.22f, 1.0f);
                layer.DrawCircle(PointAlong(points, echo), 2.6f, new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.55f));
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
        var ordered = new List<PassiveTreeManager.TreeNode>(PassiveTreeManager.Nodes);
        ordered.Sort((a, b) =>
        {
            int rarity = a.Rarity.CompareTo(b.Rarity);
            return rarity != 0 ? rarity : string.Compare(a.Id, b.Id, System.StringComparison.Ordinal);
        });

        foreach (var node in ordered)
        {
            bool isNucleus = PassiveTreeManager.IsNucleus(node.Id);
            Vector2 position = node.Position;
            int stacks = owned.TryGetValue(node.Id, out int ownedStacks) ? ownedStacks : 0;
            bool hovered = HoveredNodeId == node.Id;
            bool selected = SelectedNodeId == node.Id;
            bool isStart = node.Id == start;
            float radius = GetNodeRadius(node);
            float wobblePhase = Hash(node.Id) * Mathf.Tau;
            Color rarityColor = GetRarityColor(node.Rarity);
            Color branchColor = GetBranchColor(node.Branch, 1.0f);

            if (hovered)
                layer.DrawCircle(position, radius + 18.0f, new Color(1, 1, 1, 0.08f));

            // Breathing vesicle glow: lit nodes pulse stronger than dormant ones.
            float breath = 0.75f + 0.25f * Mathf.Sin(_time * (stacks > 0 ? 2.0f : 1.2f) + wobblePhase);
            float halo = (stacks > 0 ? 0.34f : 0.13f) * breath;
            layer.DrawCircle(position, radius + 7.0f + (stacks > 0 ? 1.6f * breath : 0.0f),
                new Color(branchColor.R, branchColor.G, branchColor.B, halo));

            Color fill = stacks > 0
                ? new Color(branchColor.R, branchColor.G, branchColor.B, 0.34f)
                : new Color(0.035f, 0.065f, 0.11f, 0.90f);
            Color edge = stacks > 0
                ? rarityColor.Lightened(0.25f)
                : rarityColor.Darkened(hovered ? -0.05f : 0.18f);

            layer.DrawColoredPolygon(NodeShape(node, radius, wobblePhase), fill);
            layer.DrawPolyline(ClosedShape(node, radius, wobblePhase), edge,
                node.Rarity == PassiveTreeManager.TreeRarity.Unique ? 4.6f : 2.8f, true);

            if (isNucleus)
            {
                float corePulse = 4.0f + 2.0f * Mathf.Sin(_time * 2.2f);
                layer.DrawArc(position, radius + corePulse, 0.0f, Mathf.Tau, 64, new Color(0.65f, 0.95f, 1.0f, 0.75f), 2.4f, true);
            }
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
        if (PassiveTreeManager.IsNucleus(node.Id))
            return 46.0f;
        return node.Rarity switch
        {
            PassiveTreeManager.TreeRarity.Unique => 52.0f,
            PassiveTreeManager.TreeRarity.Rare => 42.0f,
            PassiveTreeManager.TreeRarity.Magic => 32.0f,
            _ => 25.0f
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

    private static Vector2[] NodeShape(PassiveTreeManager.TreeNode node, float radius, float wobblePhase)
    {
        return node.Rarity switch
        {
            PassiveTreeManager.TreeRarity.Magic => Wobble(new[]
            {
                node.Position + new Vector2(0, -radius),
                node.Position + new Vector2(radius, 0),
                node.Position + new Vector2(0, radius),
                node.Position + new Vector2(-radius, 0)
            }, node.Position, wobblePhase),
            PassiveTreeManager.TreeRarity.Rare => Wobble(PolygonPoints(node.Position, radius, 6, -Mathf.Pi / 2.0f), node.Position, wobblePhase),
            PassiveTreeManager.TreeRarity.Unique => Wobble(StarPoints(node.Position, radius, radius * 0.46f, 8, -Mathf.Pi / 2.0f), node.Position, wobblePhase),
            _ => Wobble(PolygonPoints(node.Position, radius, 40, 0.0f), node.Position, wobblePhase)
        };
    }

    private static Vector2[] Wobble(Vector2[] points, Vector2 center, float phase)
    {
        var result = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 direction = points[i] - center;
            float angle = direction.Angle();
            float scale = 1.0f + 0.045f * Mathf.Sin(angle * 4.0f + phase);
            result[i] = center + direction * scale;
        }
        return result;
    }

    private static Vector2[] ClosedShape(PassiveTreeManager.TreeNode node, float radius, float wobblePhase)
    {
        var shape = NodeShape(node, radius, wobblePhase);
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

    private static Vector2[] StarPoints(Vector2 center, float outer, float inner, int points, float rotation)
    {
        var shape = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float radius = i % 2 == 0 ? outer : inner;
            float angle = rotation + Mathf.Pi * i / points;
            shape[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return shape;
    }

    private static Color GetRarityColor(PassiveTreeManager.TreeRarity rarity)
    {
        return rarity switch
        {
            PassiveTreeManager.TreeRarity.Magic => new Color(0.66f, 0.55f, 1.0f, 1.0f),
            PassiveTreeManager.TreeRarity.Rare => new Color(1.00f, 0.78f, 0.32f, 1.0f),
            PassiveTreeManager.TreeRarity.Unique => new Color(1.00f, 0.48f, 0.22f, 1.0f),
            _ => new Color(0.46f, 0.68f, 0.90f, 1.0f)
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

    private static Color GetEdgeColor(string fromId, string toId, bool lit, bool highlighted)
    {
        Color rarity = new Color(0.34f, 0.48f, 0.66f, 1.0f);
        if (PassiveTreeManager.TryGetNode(fromId, out var from) && PassiveTreeManager.TryGetNode(toId, out var to))
        {
            if (from.Branch == to.Branch)
                rarity = GetBranchColor(from.Branch, 1.0f);
            else
                rarity = GetBranchColor(from.Branch, 1.0f).Lerp(GetBranchColor(to.Branch, 1.0f), 0.5f);
        }
        if (!lit)
            return new Color(rarity.R, rarity.G, rarity.B, 0.26f);
        Color litColor = highlighted ? Colors.White : rarity.Lightened(0.18f);
        return new Color(litColor.R, litColor.G, litColor.B, highlighted ? 0.98f : 0.80f);
    }
}
