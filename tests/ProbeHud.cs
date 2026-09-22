using Godot;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>Throwaway diagnostics probe (deleted after use).</summary>
public partial class ProbeHud : SceneTree
{
    private int _frame;
    private bool _isolated;

    public override void _Initialize()
    {
        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
        var main = mainScene.Instantiate<Main>();
        Root.AddChild(main);
    }

    public override bool _Process(double delta)
    {
        _frame++;
        if (_frame < 3)
            return false;

        var main = Root.GetNodeOrNull<Main>("Main");
        main?.SetPhysicsProcess(false);
        if (main != null)
            ClearArenaEntities(main);
        var hud = main?.GetNodeOrNull<Hud>("HUD");
        GD.Print($"PROBE paused={Paused} hover={hud?.HoveredSlotIdx} startA={hud?.SkillContainer?.Modulate.A}");
        hud!.HoveredSlotIdx = -1;
        for (int i = 0; i < 30; i++)
        {
            hud._Process(0.033);
            if (i % 5 == 4)
                GD.Print($"PROBE step{i + 1}: A={hud.SkillContainer?.Modulate.A:F3} paused={Paused} hover={hud.HoveredSlotIdx}");
        }
        Quit(0);
        return true;
    }

    private static void ClearArenaEntities(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is BaseEnemy || child is SenescentRBC || child is DormantToxinVesicle || child is BioHazardArea)
                child.Free();
            else
                ClearArenaEntities(child);
        }
    }
}
