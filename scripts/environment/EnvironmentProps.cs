using Godot;
using Phagocyte.Combat;
using Phagocyte.Player;

namespace Phagocyte.Environment;

/// <summary>
/// 01. 皮下創口 fibrin clot (docs/map.md §3): pale fibrin gel on the wound floor.
/// Slows the player by 20% and absorbs enemy projectiles; on Hard it becomes an
/// acidic biofilm that also corrodes the membrane.
/// </summary>
public partial class FibrinClot : BioHazardArea
{
    public const float SlowPercent = 0.20f;

    [Export] public bool HardBiofilm { get; set; } = false;

    /// <summary>Cover radius used by enemy-projectile absorption.</summary>
    public float BlockRadius => Radius;

    public FibrinClot()
    {
        Duration = 14.0f;
        Radius = 95.0f;
        SlowFactor = 1.0f - SlowPercent;
        SlowsTarget = true;
        DealsDamage = false;
        TickInterval = 0.35f;
        CoreColor = new Color(0.86f, 0.80f, 0.58f, 0.30f);
        RimColor = new Color(0.96f, 0.90f, 0.66f, 0.70f);
    }

    public override void _Ready()
    {
        // Cover matter: enemy pellets are absorbed by the clot mesh.
        AddToGroup("neutral_matter");
        base._Ready();

        if (HardBiofilm)
        {
            DealsDamage = true;
            Damage = 4.0f;
            CoreColor = new Color(0.58f, 0.88f, 0.18f, 0.32f);
            RimColor = new Color(0.72f, 1.00f, 0.28f, 0.80f);
        }
    }
}

/// <summary>
/// 02. 肺泡高氧激發力場 hyperoxic pocket (docs/map.md §3): floating cyan bubble.
/// Entering it grants a temporary cooldown-reduction surge.
/// </summary>
public partial class HyperoxicPocket : Node2D
{
    public const float BuffBonus = 0.25f;
    public const float BuffDuration = 6.0f;

    public float Radius { get; set; } = 82.0f;
    public float Lifetime { get; set; } = 22.0f;
    public bool Consumed { get; private set; }

    private float _age;
    private float _buffTimer = -1.0f;
    private BaseCell? _buffTarget;
    private BaseCell? _player;
    private Vector2 _velocity;
    private float _phase;
    private float _redrawAccum;

    public override void _Ready()
    {
        ZIndex = 2;
        _velocity = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(8.0f, 18.0f);
        _phase = GD.Randf() * Mathf.Tau;
    }

    public override void _ExitTree()
    {
        RemoveBuff();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _age += dt;
        _phase += dt * 1.8f;

        if (Consumed)
        {
            _buffTimer -= dt;
            if (_buffTimer <= 0.0f)
                QueueFree();
            return;
        }

        Position += _velocity * dt;

        if (_player == null || !GodotObject.IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as BaseCell;
        if (_player != null
            && GlobalPosition.DistanceTo(_player.GlobalPosition) <= Radius)
        {
            Consume(_player);
        }

        if (_age >= Lifetime)
            QueueFree();

        // Breathe animation needs no physics-rate redraw.
        _redrawAccum += dt;
        if (_redrawAccum >= 1.0f / 30.0f)
        {
            _redrawAccum = 0.0f;
            QueueRedraw();
        }
    }

    private void Consume(BaseCell player)
    {
        Consumed = true;
        _buffTarget = player;
        _buffTimer = BuffDuration;
        ApplyBuff();
        Modulate = new Color(1, 1, 1, 0);
    }

    private void ApplyBuff()
    {
        if (_buffTarget == null || !GodotObject.IsInstanceValid(_buffTarget))
            return;
        // CDR is expressed in flat percentage points (docs/stat.md): a percent
        // modifier would scale a base of 0 and do nothing.
        _buffTarget.Stats?.AddModifier("cooldown_reduction", BuffBonus, 0.0f);
    }

    private void RemoveBuff()
    {
        if (_buffTarget != null && GodotObject.IsInstanceValid(_buffTarget))
            _buffTarget.Stats?.RemoveModifier("cooldown_reduction", BuffBonus, 0.0f);
        _buffTarget = null;
    }

    public override void _Draw()
    {
        if (Consumed)
            return;

        float breathe = 1.0f + 0.05f * Mathf.Sin(_phase);
        DrawCircle(Vector2.Zero, Radius * 0.55f * breathe, new Color(0.35f, 0.95f, 1.0f, 0.10f));
        DrawCircle(Vector2.Zero, Radius * 0.30f * breathe, new Color(0.55f, 0.98f, 1.0f, 0.22f));
        DrawArc(Vector2.Zero, Radius * 0.34f * breathe, 0.0f, Mathf.Tau, 40,
            new Color(0.72f, 1.0f, 1.0f, 0.65f), 2.0f, true);
        DrawCircle(new Vector2(-Radius * 0.12f, -Radius * 0.12f), Radius * 0.06f,
            new Color(1.0f, 1.0f, 1.0f, 0.75f));
    }
}

/// <summary>
/// 03. 肝血竇內皮窗孔篩選 fenestra wall (docs/map.md §3): a narrow endothelial
/// pore that blocks enlarged cells. Only a small-enough cell slips through —
/// dodging never phases terrain.
/// </summary>
public partial class FenestraWall : StaticBody2D
{
    public float GapHalfWidth { get; set; } = 55.0f;
    public float Length { get; set; } = 320.0f;
    public float Thickness { get; set; } = 26.0f;

    /// <summary>Current radius at or below which the cell already fits the pore.</summary>
    public float FitRadius { get; set; } = 34.0f;

    private readonly Godot.Collections.Array<CollisionShape2D> _shapes = new();
    private BaseCell? _player;
    private bool? _lastPassable;

    public override void _Ready()
    {
        float segment = Mathf.Max(10.0f, (Length - GapHalfWidth * 2.0f) * 0.5f);
        float offset = GapHalfWidth + segment * 0.5f;

        for (int side = -1; side <= 1; side += 2)
        {
            var shape = new CollisionShape2D
            {
                Name = side < 0 ? "FenestraLower" : "FenestraUpper",
                Position = new Vector2(0.0f, offset * side),
                Shape = new RectangleShape2D { Size = new Vector2(Thickness, segment) }
            };
            AddChild(shape);
            _shapes.Add(shape);
        }

        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as BaseCell;
        bool passable = _player != null && _player.CurrentRadius <= FitRadius;

        // Static geometry: touch the physics server only on state flips,
        // never every tick; the pore visual only needs a redraw then too.
        if (_lastPassable != passable)
        {
            _lastPassable = passable;
            foreach (var shape in _shapes)
            {
                if (GodotObject.IsInstanceValid(shape))
                    shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, passable);
            }
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var wallColor = new Color(0.55f, 0.85f, 0.95f, 0.55f);
        var wallCore = new Color(0.75f, 0.98f, 1.0f, 0.28f);
        float offset = GapHalfWidth + Mathf.Max(10.0f, (Length - GapHalfWidth * 2.0f) * 0.25f);
        float halfSegment = Mathf.Max(10.0f, (Length - GapHalfWidth * 2.0f) * 0.5f) * 0.5f;

        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 center = new(0.0f, offset * side);
            DrawRect(new Rect2(center - new Vector2(Thickness * 0.5f, halfSegment),
                new Vector2(Thickness, halfSegment * 2.0f)), wallCore);
            DrawLine(center - new Vector2(0.0f, halfSegment), center + new Vector2(0.0f, halfSegment),
                wallColor, 2.4f, true);
        }

        // Highlighted pore mouth
        DrawArc(new Vector2(0.0f, 0.0f), GapHalfWidth, 0.0f, Mathf.Tau, 40,
            new Color(0.45f, 1.0f, 0.95f, 0.45f), 2.0f, true);
    }
}

/// <summary>
/// 04. 胃腔中和保護圈 neutralization zone (docs/map.md §3): alkaline halo
/// generated by H. pylori urea hydrolysis. Acid surges do not corrode inside.
/// </summary>
public partial class NeutralizationZone : Node2D
{
    public static readonly System.Collections.Generic.List<NeutralizationZone> Active = new();

    public float Radius { get; set; } = 120.0f;
    public float Lifetime { get; set; } = 16.0f;

    private float _age;
    private float _phase;
    private float _redrawAccum;

    public override void _Ready()
    {
        Active.Add(this);
        ZIndex = 2;
        _phase = GD.Randf() * Mathf.Tau;
    }

    public override void _ExitTree()
    {
        Active.Remove(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _age += dt;
        _phase += dt * 1.6f;

        if (_age >= Lifetime)
            QueueFree();
        else
        {
            _redrawAccum += dt;
            if (_redrawAccum >= 1.0f / 30.0f)
            {
                _redrawAccum = 0.0f;
                QueueRedraw();
            }
        }
    }

    public bool Contains(Vector2 worldPosition)
    {
        return GlobalPosition.DistanceTo(worldPosition) <= Radius;
    }

    /// <summary>True when any live alkaline zone covers the point.</summary>
    public static bool CoversPoint(Vector2 worldPosition)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var zone = Active[i];
            if (zone == null || !GodotObject.IsInstanceValid(zone))
            {
                Active.RemoveAt(i);
                continue;
            }
            if (zone.Contains(worldPosition))
                return true;
        }
        return false;
    }

    public override void _Draw()
    {
        float breathe = 1.0f + 0.04f * Mathf.Sin(_phase);
        DrawCircle(Vector2.Zero, Radius * breathe, new Color(0.45f, 1.0f, 0.62f, 0.10f));
        DrawArc(Vector2.Zero, Radius * breathe, 0.0f, Mathf.Tau, 56,
            new Color(0.55f, 1.0f, 0.70f, 0.55f), 2.6f, true);
        DrawArc(Vector2.Zero, Radius * 0.62f * breathe, 0.0f, Mathf.Tau, 40,
            new Color(0.70f, 1.0f, 0.80f, 0.22f), 1.4f, true);
    }
}

/// <summary>
/// 04. 胃酸潮湧 acid surge pool (docs/map.md §3): expanding acid wavefront.
/// Damage is applied by the gastric environment so alkaline zones can negate it.
/// </summary>
public partial class AcidSurge : Node2D
{
    public float Radius { get; set; } = 260.0f;
    public float Lifetime { get; set; } = 6.0f;

    private float _age;
    private float _phase;
    private float _redrawAccum;

    public override void _Ready()
    {
        ZIndex = 1;
        _phase = GD.Randf() * Mathf.Tau;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _age += dt;
        _phase += dt * 2.2f;

        if (_age >= Lifetime)
            QueueFree();
        else
        {
            _redrawAccum += dt;
            if (_redrawAccum >= 1.0f / 30.0f)
            {
                _redrawAccum = 0.0f;
                QueueRedraw();
            }
        }
    }

    /// <summary>Current wavefront radius (grows over the surge lifetime).</summary>
    public float CurrentRadius => Radius * Mathf.Clamp(_age / (Lifetime * 0.5f), 0.15f, 1.0f);

    public bool Contains(Vector2 worldPosition)
    {
        return GlobalPosition.DistanceTo(worldPosition) <= CurrentRadius;
    }

    public override void _Draw()
    {
        float radius = CurrentRadius;
        DrawCircle(Vector2.Zero, radius, new Color(0.62f, 0.85f, 0.15f, 0.12f));
        DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 64,
            new Color(0.78f, 0.95f, 0.25f, 0.55f + 0.15f * Mathf.Sin(_phase)), 3.2f, true);
        DrawArc(Vector2.Zero, radius * 0.7f, 0.0f, Mathf.Tau, 48,
            new Color(0.70f, 0.90f, 0.22f, 0.20f), 1.6f, true);
    }
}

/// <summary>
/// 05. 星形膠質腳突 astrocyte foot-process pillar (docs/map.md §3): static maze
/// obstacle in the blood-brain barrier capillary.
/// </summary>
public partial class AstrocytePillar : StaticBody2D
{
    public float Radius { get; set; } = 72.0f;

    private float _phase;
    private float _redrawAccum;

    public override void _Ready()
    {
        AddChild(new CollisionShape2D
        {
            Name = "CollisionShape2D",
            Shape = new CircleShape2D { Radius = Radius }
        });
        _phase = GD.Randf() * Mathf.Tau;
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        _phase += (float)delta * 0.8f;

        // Slow sine pulse on static geometry: 30 Hz redraw is plenty.
        _redrawAccum += (float)delta;
        if (_redrawAccum >= 1.0f / 30.0f)
        {
            _redrawAccum = 0.0f;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var core = new Color(0.85f, 0.72f, 1.0f, 0.20f);
        var rim = new Color(0.92f, 0.80f, 1.0f, 0.55f);
        DrawCircle(Vector2.Zero, Radius, core);
        DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 48, rim, 2.4f, true);

        int arms = 7;
        for (int i = 0; i < arms; i++)
        {
            float angle = i * (Mathf.Tau / arms) + _phase;
            Vector2 from = Vector2.FromAngle(angle) * Radius * 0.85f;
            Vector2 to = Vector2.FromAngle(angle) * (Radius + 22.0f + 6.0f * Mathf.Sin(_phase + i));
            DrawLine(from, to, rim, 3.0f, true);
        }
    }
}
