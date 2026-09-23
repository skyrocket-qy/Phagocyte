namespace Phagocyte.Player;

using Godot;
using Phagocyte.Core;
using System;

/// <summary>
/// Smooth camera follow system with mouse wheel zoom and map boundary clamping.
/// </summary>
public partial class CameraFollow : Camera2D
{
    public static CameraFollow? Instance { get; private set; }

    [Export] public float MaxShakeOffset { get; set; } = 22.0f;
    [Export] public float TraumaDecay { get; set; } = 1.8f;

    private float _trauma = 0.0f;

    [Export]
    public Node2D? Target { get; set; }

    [Export]
    public float DefaultZoomScale { get; set; } = 1.0f;

    [Export]
    public float MinZoomScale { get; set; } = 0.5f;

    [Export]
    public float MaxZoomScale { get; set; } = 1.6f;

    [Export]
    public float ZoomStep { get; set; } = 0.08f;

    [Export]
    public float MapWidth { get; set; } = 3200f;

    [Export]
    public float MapHeight { get; set; } = 3200f;

    [Export]
    public bool EnableMapLimits { get; set; } = true;

    private float _currentZoomScale = 1.0f;
    private float _targetZoomScale = 1.0f;

    public override void _Ready()
    {
        Instance = this;
        _currentZoomScale = DefaultZoomScale;
        _targetZoomScale = DefaultZoomScale;
        Zoom = new Vector2(_currentZoomScale, _currentZoomScale);

        if (Target == null)
        {
            Target = GetParent()?.GetNodeOrNull<Node2D>("Player") 
                ?? GetParent()?.GetNodeOrNull<Node2D>("BaseCell") 
                ?? GetTree().GetFirstNodeInGroup("player") as Node2D;
        }

        PositionSmoothingEnabled = true;
        PositionSmoothingSpeed = 8.0f;

        if (EnableMapLimits && MapWidth > 0 && MapHeight > 0)
        {
            SetMapBounds(MapWidth, MapHeight);
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    public void AddTrauma(float amount)
    {
        if (!SettingsManager.ScreenShake)
            return;
        _trauma = Mathf.Clamp(_trauma + amount, 0.0f, 1.0f);
    }

    public void SetMapBounds(float width, float height)
    {
        LimitLeft = (int)(-width * 0.5f);
        LimitTop = (int)(-height * 0.5f);
        LimitRight = (int)(width * 0.5f);
        LimitBottom = (int)(height * 0.5f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                _targetZoomScale = Mathf.Clamp(_targetZoomScale + ZoomStep, MinZoomScale, MaxZoomScale);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                _targetZoomScale = Mathf.Clamp(_targetZoomScale - ZoomStep, MinZoomScale, MaxZoomScale);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (Target == null)
        {
            Target = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (Target == null) return;
        }

        _targetZoomScale = Mathf.Clamp(_targetZoomScale, MinZoomScale, MaxZoomScale);
        _currentZoomScale = Mathf.Lerp(_currentZoomScale, _targetZoomScale, dt * 10.0f);
        Zoom = new Vector2(_currentZoomScale, _currentZoomScale);

        GlobalPosition = Target.GlobalPosition;

        // Trauma Shake calculation (gated by user setting, default OFF)
        if (!SettingsManager.ScreenShake)
        {
            _trauma = 0.0f;
            if (Offset != Vector2.Zero)
                Offset = Vector2.Zero;
        }
        else if (_trauma > 0.0f)
        {
            float shake = _trauma * _trauma;
            float offsetX = (float)GD.RandRange(-1.0, 1.0) * MaxShakeOffset * shake;
            float offsetY = (float)GD.RandRange(-1.0, 1.0) * MaxShakeOffset * shake;
            Offset = new Vector2(offsetX, offsetY);
            _trauma = Mathf.Max(0.0f, _trauma - TraumaDecay * dt);
        }
        else if (Offset != Vector2.Zero)
        {
            Offset = Vector2.Zero;
        }
    }
}
