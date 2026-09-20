using Godot;

namespace Phagocyte.Skills;

/// <summary>
/// Transient lamellipodial grasp arm shared by Phagocytic Grasp and
/// Pseudopod Lunge: a broad cytoplasmic sheet extends from the host
/// edge, its tip forks into a phagocytic cup that closes around the
/// prey, then the sheet retracts (prey drag is owned by the skill).
/// </summary>
public partial class PseudopodArmVisual : Node2D
{
    public Node2D? Host { get; set; }
    public Node2D? Target { get; set; }
    public float BaseHalfWidth { get; set; } = 24.0f;
    public float Duration { get; set; } = 0.35f;

    private const float ExtendTime = 0.12f;
    private const float CupTime = 0.20f;

    private float _age = 0.0f;

    public override void _Ready()
    {
        ZIndex = 5;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;

        if (Host == null || !GodotObject.IsInstanceValid(Host))
        {
            QueueFree();
            return;
        }
        GlobalPosition = Host.GlobalPosition;

        if (_age >= Duration)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        Vector2 targetPos = Target != null && GodotObject.IsInstanceValid(Target)
            ? Target.GlobalPosition
            : GlobalPosition + Vector2.Right * 120.0f;
        Vector2 toTarget = targetPos - GlobalPosition;
        if (toTarget.LengthSquared() < 4.0f)
            return;

        float dist = toTarget.Length();
        float ang = toTarget.Angle();

        // Phase envelopes.
        float extendT = Mathf.Clamp(_age / ExtendTime, 0.0f, 1.0f);
        float cupT = Mathf.Clamp((_age - ExtendTime) / (CupTime - ExtendTime), 0.0f, 1.0f);
        float retractT = Mathf.Clamp((_age - CupTime) / (Duration - CupTime), 0.0f, 1.0f);
        float fade = 1.0f - Mathf.Clamp((_age - CupTime) / (Duration - CupTime), 0.0f, 1.0f) * 0.7f;

        float len = dist * (0.2f + 0.8f * extendT) * (1.0f - retractT * 0.75f);
        if (len < 8.0f)
            return;

        float wBase = BaseHalfWidth * (0.6f + 0.4f * extendT);
        float wTip = BaseHalfWidth * (0.25f + 0.55f * cupT);

        // Local frame: +X toward prey.
        Vector2 b = new Vector2(10.0f, 0.0f);
        Vector2 tip = new Vector2(len, 0.0f);
        Vector2 mid = new Vector2(len * 0.55f, 0.0f);
        float bulge = wBase * 0.45f * (1.0f - retractT);
        var sheet = new Vector2[]
        {
            b + new Vector2(0.0f, -wBase),
            mid + new Vector2(0.0f, -wTip - bulge),
            tip,
            mid + new Vector2(0.0f, wTip + bulge),
            b + new Vector2(0.0f, wBase)
        };
        for (int i = 0; i < sheet.Length; i++)
            sheet[i] = sheet[i].Rotated(ang);

        Color fill = new Color(0.20f, 0.36f, 0.58f, 0.55f * fade);
        Color edge = new Color(0.80f, 0.92f, 1.0f, 0.85f * fade);
        DrawColoredPolygon(sheet, fill);

        var outline = new Vector2[sheet.Length + 1];
        System.Array.Copy(sheet, outline, sheet.Length);
        outline[^1] = sheet[0];
        DrawPolyline(outline, edge, 2.5f);

        // Phagocytic cup: twin claws sweep from wide-open to pinched.
        float clawLen = wTip * 2.2f + 8.0f;
        float clawAng = Mathf.Lerp(2.3f, 0.55f, cupT);
        Vector2 tipW = tip.Rotated(ang);
        Vector2 clawA = tipW + new Vector2(Mathf.Cos(ang + clawAng), Mathf.Sin(ang + clawAng)) * clawLen;
        Vector2 clawB = tipW + new Vector2(Mathf.Cos(ang - clawAng), Mathf.Sin(ang - clawAng)) * clawLen;
        DrawLine(tipW, clawA, edge, 3.0f);
        DrawLine(tipW, clawB, edge, 3.0f);
        DrawCircle(clawA, 2.5f, edge);
        DrawCircle(clawB, 2.5f, edge);
    }
}
