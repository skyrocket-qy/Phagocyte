namespace Phagocyte.Player;

using Godot;
using System;

/// <summary>
/// Smooth camera follow system with mouse wheel zoom and map boundary clamping.
/// </summary>
public partial class CameraFollow : Camera2D
{
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
        if (Target == null)
        {
            Target = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (Target == null) return;
        }

        _targetZoomScale = Mathf.Clamp(_targetZoomScale, MinZoomScale, MaxZoomScale);
        _currentZoomScale = Mathf.Lerp(_currentZoomScale, _targetZoomScale, (float)delta * 10.0f);
        Zoom = new Vector2(_currentZoomScale, _currentZoomScale);

        GlobalPosition = Target.GlobalPosition;
    }
}
