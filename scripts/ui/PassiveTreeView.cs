using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Confocal-fluorescence radial bloodweb for the shared passive tree.
/// The pluripotent HSC sits in the nucleus; five lineage sectors grow outward
/// through concentric cytoplasmic rings, wired by glowing microtubule strands.
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
    private const float SectorHalfAngle = 30.0f;
    private const float SectorOuterRadius = 1430.0f;

    private static readonly Color BackgroundColor = new(0.020f, 0.043f, 0.078f, 1.0f);
    private static readonly Color PlasmaCyan = new(0.35f, 0.92f, 1.00f, 1.0f);
    private static readonly Color NecrosisColor = new(0.72f, 0.18f, 0.24f, 1.0f);

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
    private Vector2 _cameraVelocity = Vector2.Zero;
    private Vector2 _lastCamera = Vector2.Zero;
    private bool _cameraInitialized;
    private bool _nodeDragging;
    private string _nodeDragId = "";
    private Vector2 _nodePressPosition = Vector2.Zero;
    private bool _nodeDragMoved;
    private Vector2[]?[] _edgeRoutes = System.Array.Empty<Vector2[]?>();

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
        UpdateCameraVelocity((float)delta);
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
        _cameraVelocity = Vector2.Zero;
        _lastCamera = _camera;
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
            return;
        }

        Vector2 local = GetLocalMousePosition();
        if (local.X < 0.0f || local.Y < 0.0f || local.X > Size.X || local.Y > Size.Y)
        {
            _mouseInside = false;
            SetHoveredNode("");
            return;
        }

        _mouseInside = true;
        _mousePosition = local;
        SetHoveredNode(HitTestVisualPosition(ScreenToWorld(local)) ?? "");
    }

    private string? HitTestVisualPosition(Vector2 worldPosition)
    {
        string? bestId = null;
        float bestScore = float.MaxValue;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            float hitRadius = GetNodeRadius(node) + 22.0f;
            Vector2 position = PassiveTreeManager.IsNucleus(node.Id) ? node.Position : VisualPosition(node);
            float distance = worldPosition.DistanceTo(position);
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

    private void UpdateCameraVelocity(float delta)
    {
        float step = Mathf.Max(delta, 0.0001f);
        if (!_cameraInitialized)
        {
            _lastCamera = _camera;
            _cameraInitialized = true;
        }
        Vector2 instantaneous = (_camera - _lastCamera) / step;
        _lastCamera = _camera;
        _cameraVelocity = _cameraVelocity.Lerp(instantaneous, Mathf.Min(1.0f, 12.0f * step));
        if (_cameraVelocity.Length() > 900.0f)
            _cameraVelocity = _cameraVelocity.Normalized() * 900.0f;
        if (_cameraVelocity.LengthSquared() < 0.01f)
            _cameraVelocity = Vector2.Zero;
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
            button.Position = VisualPosition(node) - size * 0.5f;
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
        bool atrophic = PassiveTreeManager.IsAtrophic(TreeClassKey, node.Id);
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
            : atrophic ? new Color(0.62f, 0.32f, 0.36f, 1.0f)
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

        if (!_mouseInside || string.IsNullOrEmpty(HoveredNodeId))
        {
            _tooltipPanel.Visible = false;
            return;
        }

        string text = PassiveTreeManager.GetNodeTooltipText(TreeClassKey, HoveredNodeId);
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

        var random = new RandomNumberGenerator { Seed = 90210 };
        Vector2 parallax = (_rootPosition - Size * 0.5f) * 0.06f;
        for (int i = 0; i < 120; i++)
        {
            Vector2 basePosition = new Vector2(random.Randf() * Size.X, random.Randf() * Size.Y);
            Vector2 drift = new Vector2(
                Mathf.Sin(_time * 0.40f + i * 1.7f),
                Mathf.Cos(_time * 0.33f + i * 2.3f)) * (5.0f + (i % 5) * 2.0f);
            float twinkle = 0.04f + 0.04f * Mathf.Sin(_time * 1.3f + i * 0.7f);
            Color dust = i % 7 == 0
                ? new Color(1.0f, 0.55f, 0.35f, twinkle)
                : new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, twinkle);
            DrawCircle(basePosition + drift + parallax, i % 11 == 0 ? 2.2f : 1.5f, dust);
        }

        DrawLegend();
        DrawBranchLabels();
    }

    private void DrawBranchLabels()
    {
        Font font = ThemeDB.FallbackFont;
        foreach (string branch in BranchOrder)
        {
            if (!PassiveTreeManager.BranchBaseAngles.TryGetValue(branch, out float baseAngle))
                continue;

            Vector2 world = Polar(SectorOuterRadius + 104.0f, baseAngle);
            Vector2 screen = _rootPosition + world * _zoom;
            if (screen.X < 60.0f || screen.Y < 20.0f || screen.X > Size.X - 60.0f || screen.Y > Size.Y - 20.0f)
                continue;

            string label = GetBranchLabel(branch);
            Vector2 textSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, 14);
            Vector2 origin = screen - new Vector2(textSize.X * 0.5f, 0.0f);
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
            (BranchColors["vitality"], "TREE_RING_MEMBRANE"),
            (NecrosisColor, "TREE_ATROPHY_STATUS")
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
        var atrophic = PassiveTreeManager.GetAtrophicNodes(TreeClassKey);

        DrawSectors(_graphLayer);
        DrawCytoskeleton(_graphLayer);
        DrawEdges(_graphLayer, owned, start, atrophic);
        DrawNucleus(_graphLayer);
        DrawNodes(_graphLayer, owned, start, atrophic);
    }

    private void DrawSectors(Control layer)
    {
        const int steps = 14;
        const int bands = 16;
        foreach (string branch in BranchOrder)
        {
            if (!PassiveTreeManager.BranchBaseAngles.TryGetValue(branch, out float baseAngle))
                continue;

            Color color = GetBranchColor(branch, 1.0f);
            for (int b = 0; b < bands; b++)
            {
                float radius0 = 90.0f + (SectorOuterRadius - 90.0f) * b / bands;
                float radius1 = 90.0f + (SectorOuterRadius - 90.0f) * (b + 1) / bands;
                float alpha = Mathf.Lerp(0.075f, 0.012f, b / (float)(bands - 1));
                var quad = new Vector2[2 * (steps + 1)];
                for (int i = 0; i <= steps; i++)
                {
                    float angle = baseAngle - SectorHalfAngle + 2.0f * SectorHalfAngle * i / steps;
                    quad[i * 2] = Polar(radius0, angle);
                    quad[i * 2 + 1] = Polar(radius1, angle);
                }
                layer.DrawColoredPolygon(quad, new Color(color.R, color.G, color.B, alpha));
            }

            Vector2 previous = Polar(SectorOuterRadius, baseAngle - SectorHalfAngle);
            for (int i = 1; i <= steps; i++)
            {
                float angle = baseAngle - SectorHalfAngle + 2.0f * SectorHalfAngle * i / steps;
                Vector2 next = Polar(SectorOuterRadius, angle);
                layer.DrawLine(previous, next, new Color(color.R, color.G, color.B, 0.18f), 1.6f, true);
                previous = next;
            }
        }
    }

    private void DrawCytoskeleton(Control layer)
    {
        for (int ring = 1; ring <= 5; ring++)
        {
            float radius = PassiveTreeManager.GetRingRadius(ring);
            var points = new Vector2[121];
            for (int i = 0; i <= 120; i++)
            {
                float angle = Mathf.Tau * i / 120.0f;
                float wobble = 1.0f
                    + 0.012f * Mathf.Sin(angle * 6.0f + _time * 0.35f + ring * 1.3f)
                    + 0.006f * Mathf.Sin(angle * 11.0f - _time * 0.22f);
                points[i] = PassiveTreeManager.WorldCenter
                    + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * wobble;
            }
            layer.DrawPolyline(points, new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, ring == 1 ? 0.10f : 0.045f),
                ring == 1 ? 2.0f : 1.2f, true);
        }

        for (int i = 0; i < 5; i++)
        {
            float angle = 54.0f + 72.0f * i;
            layer.DrawLine(Polar(130.0f, angle), Polar(SectorOuterRadius + 46.0f, angle),
                new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.035f), 1.0f, true);
        }
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

    private void DrawEdges(Control layer, Godot.Collections.Dictionary<string, int> owned, string start, HashSet<string> atrophic)
    {
        EnsureEdgeRoutes();
        for (int i = 0; i < PassiveTreeManager.Edges.Length; i++)
        {
            var edge = PassiveTreeManager.Edges[i];
            if (!PassiveTreeManager.TryGetNode(edge.From, out var fromNode) || !PassiveTreeManager.TryGetNode(edge.To, out var toNode))
                continue;

            Vector2 from = VisualPosition(fromNode);
            Vector2 to = VisualPosition(toNode);
            if (from.DistanceSquaredTo(to) < 1.0f)
                continue;

            bool fromActive = owned.ContainsKey(edge.From) || edge.From == start;
            bool toActive = owned.ContainsKey(edge.To) || edge.To == start;
            bool lit = fromActive && toActive;
            bool highlighted = HoveredNodeId == edge.From || HoveredNodeId == edge.To
                || SelectedNodeId == edge.From || SelectedNodeId == edge.To;
            bool starving = !lit && (atrophic.Contains(edge.From) || atrophic.Contains(edge.To));
            Color color = starving
                ? new Color(NecrosisColor.R, NecrosisColor.G, NecrosisColor.B, 0.30f)
                : GetEdgeColor(edge.From, edge.To, lit, highlighted);
            float width = (lit ? 4.4f : 2.0f) + (highlighted ? 1.4f : 0.0f);
            var points = ShiftRoute(_edgeRoutes[i], fromNode, from, toNode, to);

            if (starving)
            {
                float flicker = 0.65f + 0.35f * Mathf.Sin(_time * 2.2f + i);
                for (int k = 0; k + 1 < points.Length; k += 3)
                {
                    layer.DrawLine(points[k], points[k + 1],
                        new Color(color.R, color.G, color.B, color.A * flicker), width * 0.7f, true);
                }
            }
            else
            {
                layer.DrawPolyline(points, new Color(color.R, color.G, color.B, color.A * 0.26f), width * 2.8f, true);
                layer.DrawPolyline(points, color, width, true);
            }

            if (lit)
            {
                float travel = Mathf.PosMod(_time * 0.22f + i * 0.137f, 1.0f);
                layer.DrawCircle(PointAlong(points, travel), 4.2f, Colors.White);
                float echo = Mathf.PosMod(travel + 0.22f, 1.0f);
                layer.DrawCircle(PointAlong(points, echo), 2.6f, new Color(PlasmaCyan.R, PlasmaCyan.G, PlasmaCyan.B, 0.55f));
            }
        }
    }

    private static Vector2[] ShiftRoute(Vector2[]? route, PassiveTreeManager.TreeNode fromNode, Vector2 from, PassiveTreeManager.TreeNode toNode, Vector2 to)
    {
        if (route == null || route.Length < 2)
            return new[] { from, to };

        var points = new Vector2[route.Length];
        Vector2 shiftFrom = from - fromNode.Position;
        Vector2 shiftTo = to - toNode.Position;
        int last = route.Length - 1;
        for (int k = 0; k < route.Length; k++)
            points[k] = route[k] + shiftFrom.Lerp(shiftTo, k / (float)last);
        points[0] = from;
        points[last] = to;
        return points;
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

    /// <summary>
    /// Routes every microtubule once: straight radial lines are kept when clear,
    /// otherwise the strand bows onto a polar arc that slides around other vesicles.
    /// Routes depend only on static node positions, so they are cached.
    /// </summary>
    private void EnsureEdgeRoutes()
    {
        if (_edgeRoutes.Length == PassiveTreeManager.Edges.Length)
            return;

        _edgeRoutes = new Vector2[]?[PassiveTreeManager.Edges.Length];
        for (int i = 0; i < PassiveTreeManager.Edges.Length; i++)
        {
            var edge = PassiveTreeManager.Edges[i];
            if (!PassiveTreeManager.TryGetNode(edge.From, out var from) || !PassiveTreeManager.TryGetNode(edge.To, out var to))
                continue;
            _edgeRoutes[i] = BuildEdgeRoute(from, to);
        }
    }

    private static Vector2[] BuildEdgeRoute(PassiveTreeManager.TreeNode from, PassiveTreeManager.TreeNode to)
    {
        var straight = PolarRoute(from.Position, to.Position, 0.0f);
        float straightClearance = RouteClearance(straight, from.Id, to.Id);
        if (straightClearance >= 4.0f)
            return straight;

        Vector2[] best = straight;
        float bestClearance = straightClearance;
        foreach (float offset in new[] { 16.0f, -16.0f, 30.0f, -30.0f, 46.0f, -46.0f, 64.0f, -64.0f })
        {
            var candidate = PolarRoute(from.Position, to.Position, offset);
            float clearance = RouteClearance(candidate, from.Id, to.Id);
            if (clearance > bestClearance + 0.5f)
            {
                bestClearance = clearance;
                best = candidate;
            }
        }
        return best;
    }

    private static Vector2[] PolarRoute(Vector2 from, Vector2 to, float offset)
    {
        const int samples = 24;
        Vector2 center = PassiveTreeManager.WorldCenter;
        Vector2 relativeFrom = from - center;
        Vector2 relativeTo = to - center;
        float radius0 = relativeFrom.Length();
        float radius1 = relativeTo.Length();
        float angle0 = relativeFrom.Angle();
        float deltaAngle = Mathf.Wrap(relativeTo.Angle() - angle0, -Mathf.Pi, Mathf.Pi);

        var points = new Vector2[samples + 1];
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float eased = t * t * (3.0f - 2.0f * t);
            float radius = Mathf.Lerp(radius0, radius1, eased);
            float angle = angle0 + deltaAngle * eased
                + offset * Mathf.Sin(Mathf.Pi * t) / Mathf.Max(90.0f, radius);
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        points[0] = from;
        points[samples] = to;
        return points;
    }

    private static float RouteClearance(Vector2[] points, string fromId, string toId)
    {
        float clearance = float.MaxValue;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            if (node.Id == fromId || node.Id == toId)
                continue;

            float required = GetNodeRadius(node) + 18.0f;
            float nearest = float.MaxValue;
            for (int i = 1; i < points.Length; i += 2)
                nearest = Mathf.Min(nearest, node.Position.DistanceTo(points[i]));
            clearance = Mathf.Min(clearance, nearest - required);
        }
        return clearance;
    }

    private void DrawNodes(Control layer, Godot.Collections.Dictionary<string, int> owned, string start, HashSet<string> atrophic)
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
            Vector2 position = isNucleus ? node.Position : VisualPosition(node);
            int stacks = owned.TryGetValue(node.Id, out int ownedStacks) ? ownedStacks : 0;
            bool hovered = HoveredNodeId == node.Id;
            bool selected = SelectedNodeId == node.Id;
            bool isStart = node.Id == start;
            bool isAtrophic = atrophic.Contains(node.Id);
            float radius = GetNodeRadius(node);
            float wobblePhase = Hash(node.Id) * Mathf.Tau;
            Color rarityColor = GetRarityColor(node.Rarity);
            Color branchColor = GetBranchColor(node.Branch, 1.0f);

            if (hovered)
                layer.DrawCircle(position, radius + 18.0f, new Color(1, 1, 1, 0.08f));

            if (!isAtrophic)
            {
                float halo = stacks > 0 ? 0.30f : 0.13f;
                layer.DrawCircle(position, radius + 7.0f, new Color(branchColor.R, branchColor.G, branchColor.B, halo));
            }

            Color fill = isAtrophic
                ? new Color(0.09f, 0.035f, 0.05f, 0.92f)
                : stacks > 0
                    ? new Color(branchColor.R, branchColor.G, branchColor.B, 0.34f)
                    : new Color(0.035f, 0.065f, 0.11f, 0.90f);
            Color edge = isAtrophic
                ? NecrosisColor
                : stacks > 0
                    ? rarityColor.Lightened(0.25f)
                    : rarityColor.Darkened(hovered ? -0.05f : 0.18f);

            layer.DrawColoredPolygon(NodeShape(node, radius, wobblePhase), fill);
            layer.DrawPolyline(ClosedShape(node, radius, wobblePhase), edge,
                node.Rarity == PassiveTreeManager.TreeRarity.Unique ? 4.6f : 2.8f, true);

            if (isAtrophic)
            {
                float diagonal = radius * 0.62f;
                layer.DrawLine(position + new Vector2(-diagonal, diagonal), position + new Vector2(diagonal, -diagonal),
                    new Color(NecrosisColor.R, NecrosisColor.G, NecrosisColor.B, 0.60f), 2.0f, true);
                for (int i = 0; i < 6; i++)
                {
                    float startAngle = Mathf.Tau * i / 6.0f + _time * 0.25f;
                    layer.DrawArc(position, radius + 5.0f, startAngle, startAngle + 0.62f, 10,
                        new Color(NecrosisColor.R, NecrosisColor.G, NecrosisColor.B, 0.45f), 2.0f, true);
                }
            }

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

            if (node.MaxStacks > 1)
                DrawStackPips(layer, node, position, stacks, branchColor);
        }
    }

    private void DrawStackPips(Control layer, PassiveTreeManager.TreeNode node, Vector2 position, int stacks, Color branchColor)
    {
        float radius = GetNodeRadius(node) + 15.0f;
        for (int i = 0; i < node.MaxStacks; i++)
        {
            float angle = Mathf.DegToRad(55.0f + (70.0f * i / Mathf.Max(1, node.MaxStacks - 1)));
            Vector2 pip = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (i < stacks)
                layer.DrawCircle(pip, 4.6f, branchColor.Lightened(0.25f));
            else
                layer.DrawArc(pip, 4.6f, 0.0f, Mathf.Tau, 16, new Color(1, 1, 1, 0.30f), 1.5f, true);
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

    private static Vector2 Polar(float radius, float angleDegrees)
    {
        float radians = Mathf.DegToRad(angleDegrees);
        return PassiveTreeManager.WorldCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
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

    private static Vector2 DriftOffset(PassiveTreeManager.TreeNode node, float time)
    {
        if (PassiveTreeManager.IsNucleus(node.Id))
            return Vector2.Zero;

        float seed = Hash(node.Id) * 12.0f;
        float amplitude = 0.7f + node.Ring * 0.45f;
        float speed = 0.26f + Hash(node.Id + "s") * 0.18f;
        return new Vector2(
            Mathf.Sin(time * speed + seed * 7.1f) + 0.35f * Mathf.Sin(time * speed * 2.3f + seed),
            Mathf.Cos(time * speed * 0.83f + seed * 3.7f) + 0.35f * Mathf.Cos(time * speed * 1.9f + seed * 1.7f)) * amplitude;
    }

    private Vector2 VisualPosition(PassiveTreeManager.TreeNode node)
    {
        Vector2 lag = _cameraVelocity * (0.010f + 0.0022f * node.Ring);
        return node.Position + DriftOffset(node, _time) + lag.LimitLength(10.0f);
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
