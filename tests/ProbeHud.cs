using Godot;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>Throwaway diagnostics probe (deleted after use).</summary>
public partial class ProbeHud : SceneTree
{
    private int _frame;
    private BaseCell? _player;

    public override void _Initialize()
    {
        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
        var main = mainScene.Instantiate<Main>();
        Root.AddChild(main);
        main.SetPhysicsProcess(false);
        ClearArenaEntities(main);
        GD.Print($"PROBE after clear: enemies={Count<BaseEnemy>(main)} rbc={Count<SenescentRBC>(main)} ves={Count<DormantToxinVesicle>(main)} haz={Count<BioHazardArea>(main)}");
        _player = main.GetNodeOrNull<Macrophage>("Macrophage");
        GD.Print($"PROBE player found: {_player != null}, exp0={_player?.CurrentExp}");
    }

    public override bool _Process(double delta)
    {
        _frame++;
        var main = Root.GetNodeOrNull<Main>("Main");
        GD.Print($"PROBE f{_frame}: exp={_player?.CurrentExp} enemies={Count<BaseEnemy>(main!)} rbc={Count<SenescentRBC>(main!)} ves={Count<DormantToxinVesicle>(main!)} haz={Count<BioHazardArea>(main!)} hp={_player?.Health}");
        if (_frame >= 6)
        {
            Quit(0);
            return true;
        }
        return false;
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

    private static int Count<T>(Node root) where T : Node
    {
        int n = 0;
        foreach (var child in root.GetChildren())
        {
            if (child is T)
                n++;
            n += Count<T>(child);
        }
        return n;
    }
}
