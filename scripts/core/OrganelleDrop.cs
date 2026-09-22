using Godot;
using Phagocyte.Core;

namespace Phagocyte.Core;

/// <summary>
/// Transient organelle drop (spawn → drift to the player → collect → free).
/// Lives in code like the other throwaway effect nodes: nothing here is
/// editor-tunable beyond the exported pickup constants.
/// </summary>
public partial class OrganelleDrop : Node2D
{
    /// <summary>Distance at which the drop is collected.</summary>
    public const float PickupRadius = 26.0f;

    /// <summary>Magnet pull speed at the edge of the attraction radius.</summary>
    public const float PullSpeedMin = 90.0f;

    /// <summary>Magnet pull speed once close.</summary>
    public const float PullSpeedMax = 620.0f;

    public string OrganelleId { get; set; } = "";
    public Node2D? Target { get; set; }

    private Sprite2D? _icon;
    private float _age;
    private float _bobPhase;

    public override void _Ready()
    {
        ZIndex = 4;

        string imagePath = GameManager.OrganelleCatalog.TryGetValue(OrganelleId, out var entryVar)
            ? entryVar.AsGodotDictionary()["image_path"].AsString()
            : "";
        _icon = new Sprite2D
        {
            Name = "Icon",
            Texture = AssetLoader.TryLoad<Texture2D>(imagePath)
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon),
            Scale = new Vector2(0.34f, 0.34f)
        };
        AddChild(_icon);
        _bobPhase = GD.Randf() * Mathf.Tau;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _age += dt;
        _bobPhase += dt * 3.0f;

        if (_icon != null)
        {
            _icon.Position = new Vector2(0.0f, Mathf.Sin(_bobPhase) * 3.0f);
            _icon.Modulate = new Color(1, 1, 1, 0.75f + 0.25f * Mathf.Sin(_bobPhase * 1.7f));
        }
        QueueRedraw();

        if (Target == null || !GodotObject.IsInstanceValid(Target))
            return;

        float dist = GlobalPosition.DistanceTo(Target.GlobalPosition);
        if (dist <= PickupRadius)
        {
            Collect();
            return;
        }

        float magnet = Target is Player.BaseCell cell && cell.Stats != null
            ? cell.Stats.GetStat("magnet")
            : 150.0f;
        if (dist <= magnet)
        {
            float pull = Mathf.Lerp(PullSpeedMax, PullSpeedMin, Mathf.Clamp(dist / magnet, 0.0f, 1.0f));
            Vector2 dir = (Target.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += dir * pull * dt;
        }
    }

    public override void _Draw()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(_age * 4.0f);
        DrawArc(Vector2.Zero, 20.0f + pulse * 4.0f, 0.0f, Mathf.Tau, 32,
            new Color(0.45f, 1.0f, 0.72f, 0.55f - pulse * 0.25f), 2.0f, true);
        DrawCircle(Vector2.Zero, 22.0f, new Color(0.2f, 0.9f, 0.6f, 0.10f + pulse * 0.06f));
    }

    /// <summary>Unlocks the organelle (first time only notifies listeners) and frees the drop.</summary>
    public void Collect()
    {
        if (!string.IsNullOrEmpty(OrganelleId))
            OrganelleUnlockManager.Unlock(OrganelleId);
        AudioManager.Instance?.PlayPickup();
        QueueFree();
    }

    /// <summary>Test/scripted hook: collect without a live player target.</summary>
    public void CollectForTest() => Collect();
}
