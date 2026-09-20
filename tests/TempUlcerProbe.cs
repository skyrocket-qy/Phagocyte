using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

public partial class TempUlcerProbe : TestHarness
{
    private int _phase = 0;
    public override bool _Process(double delta)
    {
        if (_phase == 0) { _phase++; return false; }
        try
        {
            EnemySteering.ConfigureArena(new Vector2(2000, 2000));
            HostUlceration.Reset();
            var container = new Node2D { Name = "ProbeContainer" };
            Root.AddChild(container);
            var player = new BaseCell { Name = "NeutralMatterHost", GlobalPosition = new Vector2(500, 500) };
            container.AddChild(player);

            var rbc1 = new SenescentRBC { GlobalPosition = new Vector2(700, 700) };
            container.AddChild(rbc1);
            player.ConsumePathogen(rbc1);
            GD.Print($"PROBE rbc1 valid={GodotObject.IsInstanceValid(rbc1)} inGroup={rbc1.IsInGroup("senescent_rbc")}");

            var invader = new HpyloriEnemy { GlobalPosition = Vector2.Zero };
            container.AddChild(invader);
            var rbc2 = new SenescentRBC { GlobalPosition = new Vector2(220, 0) };
            container.AddChild(rbc2);

            var before = EnemySteering.GetDirection(invader, 0.016f);
            HoldPulses(3);
            var after = EnemySteering.GetDirection(invader, 0.016f);
            GD.Print($"PROBE pulses={HostUlceration.Pulses} beforeDot={before.Dot(Vector2.Right)} after={after} afterDot={after.Dot(Vector2.Right)}");

            foreach (var n in container.GetChildren())
                if (n is Node2D nd)
                    GD.Print($"PROBE child {nd.Name} {nd.GetType().Name} pos={nd.GlobalPosition} valid={GodotObject.IsInstanceValid(nd)}");
            GD.Print("PROBE done");
            Quit(0);
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"PROBE threw: {ex}");
            Quit(1);
            return true;
        }
    }

    private static void HoldPulses(int n)
    {
        for (int i = 0; i < n; i++)
            HostUlceration.RegisterPulse();
    }
}
