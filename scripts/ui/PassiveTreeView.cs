using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// PoE-style constellation view for the shared passive tree, with pan, zoom,
/// hover tooltips, left-click purchases, and right-click refunds.
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

    private const float MinZoom = 0.22f;
    private const float MaxZoom = 1.75f;
    private const float WorldWidth = 4000.0f;
    private const float WorldHeight = 3000.0f;

    private static readonly Dictionary<string, Color> BranchColors = new()
    {
        { "precision", new Color(1.00f, 0.42f, 0.48f, 1.0f) },
        { "senses", new Color(0.35f, 0.90f, 0.78f, 1.0f) },
        { "motility", new Color(0.42f, 0.72f, 1.00f, 1.0f) },
        { "ballistics", new Color(1.00f, 0.76f, 0.34f, 1.0f) },
        { "vitality", new Color(0.55f, 0.92f, 0.48f, 1.0f) },
        { "core", new Color(0.76f, 0.60f, 1.00f, 1.0f) }
    };

    private static readonly string[] BranchOrder = { "precision", "senses", "motility", "ballistics", "vitality", "core" };

    private Control? _zoomRoot;
    private TreeGraphLayer? _graphLayer;
    private PanelContainer? _tooltipPanel;
    private RichTextLabel? _tooltipText;
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

        _zoomRoot = new Control
        {
            Name = "ZoomRoot",
            MouseFilter = MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(WorldWidth, WorldHeight)
        };
        AddChild(_zoomRoot);

        _graphLayer = new TreeGraphLayer
        {
            Name = "GraphLayer",
            View = this,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(WorldWidth, WorldHeight)
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
            BgColor = new Color(0.04f, 0.07f, 0.12f, 0.96f),
            BorderColor = new Color(0.48f, 0.78f, 1.0f, 0.85f),
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
        UpdateHoverFromMouse();
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

        return new Rect2(min - new Vector2(260, 220), max - min + new Vector2(520, 440));
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
        SetHoveredNode(HitTestWorldPosition(ScreenToWorld(local)) ?? "");
    }

    private void EnsureCameraForClass()
    {
        if (_cameraClass == TreeClassKey)
            return;

        _cameraClass = TreeClassKey;
        if (!FitTree())
            _needsFit = true;
    }

    private void UpdateTransform()
    {
        if (_zoomRoot == null || Size.X <= 0.0f || Size.Y <= 0.0f)
            return;

        ClampCamera();
        _zoomRoot.Position = Size * 0.5f - _camera * _zoom;
        _zoomRoot.Scale = new Vector2(_zoom, _zoom);
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

    private Vector2 ScreenToWorld(Vector2 viewPosition)
    {
        if (_zoomRoot == null)
            return viewPosition;
        return (viewPosition - _zoomRoot.Position) / _zoom;
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
        Color iconColor = stacks > 0 ? new Color(1, 1, 1, 1) : available ? new Color(0.85f, 0.97f, 1.0f, 1.0f) : new Color(0.62f, 0.68f, 0.76f, 1.0f);
        button.AddThemeColorOverride("font_color", iconColor);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", iconColor);
        button.AddThemeConstantOverride("outline_size", 8);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));

        string localId = node.Id;
        button.Pressed += () => ActivateNode(localId);
        button.GuiInput += (@event) => OnNodeGuiInput(button, localId, @event);
        button.MouseEntered += () => SetHoveredNode(localId);
        button.MouseExited += () => SetHoveredNode("");
        return button;
    }

    private void OnNodeGuiInput(Button button, string nodeId, InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Right)
        {
            button.AcceptEvent();
            RefundNode(nodeId);
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

    private void DrawBackground()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.018f, 0.033f, 0.055f, 1.0f));
        if (Size.X <= 0.0f || Size.Y <= 0.0f)
            return;

        float spacing = Mathf.Max(28.0f, 132.0f * _zoom);
        Vector2 offset = Size * 0.5f - _camera * _zoom;
        Color line = new Color(0.28f, 0.48f, 0.70f, 0.10f);
        float startX = spacing - Mathf.PosMod(offset.X, spacing);
        for (float x = startX; x <= Size.X; x += spacing)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), line, 1.0f);
        float startY = spacing - Mathf.PosMod(offset.Y, spacing);
        for (float y = startY; y <= Size.Y; y += spacing)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), line, 1.0f);

        var random = new RandomNumberGenerator { Seed = 424242 };
        for (int i = 0; i < 90; i++)
        {
            Vector2 basePosition = new Vector2(random.Randf() * Size.X, random.Randf() * Size.Y);
            Vector2 drift = new Vector2(Mathf.Sin(_time * 0.35f + i * 1.7f), Mathf.Cos(_time * 0.28f + i * 2.3f)) * 8.0f;
            float twinkle = 0.05f + 0.05f * Mathf.Sin(_time * 1.4f + i);
            DrawCircle(basePosition + drift, 1.6f, new Color(0.55f, 0.80f, 1.0f, twinkle));
        }
    }

    private void DrawGraph()
    {
        if (_graphLayer == null)
            return;

        var owned = PassiveTreeManager.GetAllocation(TreeClassKey);
        string start = PassiveTreeManager.GetStartNode(TreeClassKey);

        DrawBranchRegions(_graphLayer);
        DrawEdges(_graphLayer, owned, start);
        DrawNodes(_graphLayer, owned, start);
    }

    private void DrawBranchRegions(Control layer)
    {
        foreach (string branch in BranchOrder)
        {
            Rect2 bounds = GetBranchBounds(branch);
            if (bounds.Size.X <= 0.0f || bounds.Size.Y <= 0.0f)
                continue;

            Color fill = GetBranchColor(branch, 0.055f);
            Color edge = GetBranchColor(branch, 0.20f);
            layer.DrawRect(bounds, fill);
            layer.DrawRect(bounds, edge, false, 2.0f);
            layer.DrawString(ThemeDB.FallbackFont, bounds.Position + new Vector2(28, 52),
                GetBranchLabel(branch), HorizontalAlignment.Left, -1, 30,
                GetBranchColor(branch, 0.58f));
        }
    }

    private void DrawEdges(Control layer, Godot.Collections.Dictionary<string, int> owned, string start)
    {
        for (int i = 0; i < PassiveTreeManager.Edges.Length; i++)
        {
            var edge = PassiveTreeManager.Edges[i];
            Vector2 from = GetNodePosition(edge.From);
            Vector2 to = GetNodePosition(edge.To);
            if (from == to)
                continue;

            bool fromActive = owned.ContainsKey(edge.From) || edge.From == start;
            bool toActive = owned.ContainsKey(edge.To) || edge.To == start;
            bool lit = fromActive && toActive;
            bool highlighted = HoveredNodeId == edge.From || HoveredNodeId == edge.To
                || SelectedNodeId == edge.From || SelectedNodeId == edge.To;
            Color color = GetEdgeColor(edge.From, edge.To, lit, highlighted);
            float width = (lit ? 4.6f : 2.1f) + (highlighted ? 1.4f : 0.0f);
            var points = CurvePoints(from, to, i);
            layer.DrawPolyline(points, new Color(color.R, color.G, color.B, color.A * 0.28f), width * 2.7f, true);
            layer.DrawPolyline(points, color, width, true);

            if (lit)
            {
                float travel = Mathf.PosMod(_time * 0.22f + i * 0.137f, 1.0f);
                layer.DrawCircle(BezierPoint(from, CurveControl(from, to, i), to, travel), 4.2f, Colors.White);
            }
        }
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
            int stacks = owned.TryGetValue(node.Id, out int ownedStacks) ? ownedStacks : 0;
            bool hovered = HoveredNodeId == node.Id;
            bool selected = SelectedNodeId == node.Id;
            bool isStart = node.Id == start;
            float radius = GetNodeRadius(node);
            Color baseColor = GetRarityColor(node.Rarity);
            Color fill = stacks > 0 ? new Color(baseColor.R, baseColor.G, baseColor.B, 0.38f) : new Color(0.04f, 0.07f, 0.12f, 0.88f);
            Color edge = stacks > 0 ? baseColor.Lightened(0.25f) : baseColor.Darkened(hovered ? -0.05f : 0.18f);
            if (hovered)
                layer.DrawCircle(node.Position, radius + 13.0f, new Color(1, 1, 1, 0.10f));

            layer.DrawCircle(node.Position, radius + 5.0f, new Color(baseColor.R, baseColor.G, baseColor.B, stacks > 0 ? 0.34f : 0.14f));
            layer.DrawColoredPolygon(NodeShape(node, radius), fill);
            layer.DrawPolyline(ClosedShape(node, radius), edge, node.Rarity == PassiveTreeManager.TreeRarity.Unique ? 5.0f : 3.0f, true);

            if (isStart)
            {
                float pulse = 7.0f + 2.4f * Mathf.Sin(_time * 2.6f);
                layer.DrawArc(node.Position, radius + pulse, 0.0f, Mathf.Tau, 64, new Color(0.45f, 0.95f, 1.0f, 0.85f), 3.0f, true);
            }
            if (selected)
                layer.DrawArc(node.Position, radius + 10.0f, 0.0f, Mathf.Tau, 64, new Color(1.0f, 0.90f, 0.42f, 0.95f), 3.5f, true);

            if (node.MaxStacks > 1)
                DrawStackPips(layer, node, stacks);
        }
    }

    private void DrawStackPips(Control layer, PassiveTreeManager.TreeNode node, int stacks)
    {
        float radius = GetNodeRadius(node) + 15.0f;
        for (int i = 0; i < node.MaxStacks; i++)
        {
            float angle = Mathf.DegToRad(55.0f + (70.0f * i / Mathf.Max(1, node.MaxStacks - 1)));
            Vector2 pip = node.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (i < stacks)
                layer.DrawCircle(pip, 4.6f, new Color(1.0f, 0.86f, 0.38f, 1.0f));
            else
                layer.DrawArc(pip, 4.6f, 0.0f, Mathf.Tau, 16, new Color(1, 1, 1, 0.35f), 1.5f, true);
        }
    }

    private static float GetNodeRadius(PassiveTreeManager.TreeNode node)
    {
        return node.Rarity switch
        {
            PassiveTreeManager.TreeRarity.Unique => 54.0f,
            PassiveTreeManager.TreeRarity.Rare => 44.0f,
            PassiveTreeManager.TreeRarity.Magic => 34.0f,
            _ => 26.0f
        };
    }

    private static Vector2[] NodeShape(PassiveTreeManager.TreeNode node, float radius)
    {
        return node.Rarity switch
        {
            PassiveTreeManager.TreeRarity.Magic => new[]
            {
                node.Position + new Vector2(0, -radius),
                node.Position + new Vector2(radius, 0),
                node.Position + new Vector2(0, radius),
                node.Position + new Vector2(-radius, 0)
            },
            PassiveTreeManager.TreeRarity.Rare => PolygonPoints(node.Position, radius, 6, -Mathf.Pi / 2.0f),
            PassiveTreeManager.TreeRarity.Unique => StarPoints(node.Position, radius, radius * 0.46f, 8, -Mathf.Pi / 2.0f),
            _ => PolygonPoints(node.Position, radius, 40, 0.0f)
        };
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

    private static Vector2[] CurvePoints(Vector2 from, Vector2 to, int index)
    {
        Vector2 control = CurveControl(from, to, index);
        var points = new Vector2[19];
        for (int i = 0; i <= 18; i++)
            points[i] = BezierPoint(from, control, to, i / 18.0f);
        return points;
    }

    private static Vector2 CurveControl(Vector2 from, Vector2 to, int index)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= 1.0f)
            return (from + to) * 0.5f;
        Vector2 normal = (delta / length).Rotated(Mathf.Pi * 0.5f);
        float bend = Mathf.Min(110.0f, length * 0.12f) * (index % 2 == 0 ? 1.0f : -1.0f);
        return (from + to) * 0.5f + normal * bend;
    }

    private static Vector2 BezierPoint(Vector2 from, Vector2 control, Vector2 to, float t)
    {
        float u = 1.0f - t;
        return u * u * from + 2.0f * u * t * control + t * t * to;
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
            var dominant = (PassiveTreeManager.TreeRarity)Mathf.Max((int)from.Rarity, (int)to.Rarity);
            rarity = GetRarityColor(dominant);
        }
        if (!lit)
            return new Color(rarity.R, rarity.G, rarity.B, 0.30f);
        Color litColor = highlighted ? Colors.White : rarity.Lightened(0.18f);
        return new Color(litColor.R, litColor.G, litColor.B, highlighted ? 0.98f : 0.82f);
    }

    private static Vector2 GetNodePosition(string nodeId)
    {
        return PassiveTreeManager.TryGetNode(nodeId, out var node) ? node.Position : Vector2.Zero;
    }

    private Rect2 GetBranchBounds(string branch)
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
        return new Rect2(min - new Vector2(150, 130), max - min + new Vector2(300, 260));
    }

    public override void _Draw()
    {
        DrawBackground();
    }
}
