using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.Enemies;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestSkillSystem : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING 5+5 SKILL SYSTEM AUTOMATED VERIFICATION ---");
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        var main = mainScene.Instantiate();
        Root.AddChild(main);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 4)
            return false;

        _testDone = true;
        var main = Root.GetNodeOrNull<Main>("Main");
        AssertThat(main).IsNotNull();

        var player = main!.GetNodeOrNull<Macrophage>("Macrophage");
        AssertThat(player).IsNotNull();

        var enemyContainer = main.GetNodeOrNull<Node2D>("EnemyContainer");
        AssertThat(enemyContainer).IsNotNull();

        var hud = main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();

        // 1. Verify SkillManager 5 Active + 5 Passive slot counts
        var sm = player!.CellSkillManager;
        AssertThat(sm).IsNotNull();
        AssertThat(sm!.ActiveSlots.Count).IsEqual(5);
        AssertThat(sm.PassiveSlots.Count).IsEqual(5);
        GD.Print("[PASS] SkillManager contains exactly 5 Active slots and 5 Passive slots.");

        // 2. Verify Passive Slot 0: innate Macrophage Deformation (微絲變形)
        var innatePassive = sm.GetPassiveSlot(0);
        AssertThat(innatePassive is MacrophageDeformationSkill).IsTrue();
        AssertThat(innatePassive!.SkillId).IsEqual("macrophage_pseudopods");
        AssertThat(innatePassive.IsInnate).IsTrue();
        GD.Print("[PASS] Passive Slot 0 correctly contains the innate MacrophageDeformationSkill.");

        // 3. Verify Active Slots 0-4 start empty (Macrophage carries no ranged weapon)
        for (int i = 0; i < 5; i++)
        {
            AssertThat(sm.GetActiveSlot(i)).IsNull();
        }
        GD.Print("[PASS] Active Slots 0 to 4 are empty and available.");

        // 4. Verify Passive Slots 1-4 are initially empty
        for (int i = 1; i < 5; i++)
        {
            AssertThat(sm.GetPassiveSlot(i)).IsNull();
        }
        GD.Print("[PASS] Passive Slots 1 to 4 are initially empty and available.");

        // 5. Verify smooth deformation & 32-vertex collision sync
        player.UpdatePseudopodDeformation(0.016f);
        AssertThat(player.Cytoplasm!.Polygon.Length >= 64).IsTrue();
        AssertThat(player.EngulfCollider!.Polygon.Length).IsEqual(32);
        GD.Print($"[PASS] Smooth {player.Cytoplasm!.Polygon.Length}-vertex organic pseudopod deformation & CollisionPolygon2D sync verified.");

        // 6. Test ROS Torrent auto-targeting & firing (drafted into active slot 0)
        var ros = new RosTorrentSkill();
        AssertThat(sm.EquipActive(ros, 0)).IsTrue();
        var staphScene = GD.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");
        var enemy = staphScene.Instantiate<StaphEnemy>();
        enemy.GlobalPosition = player.GlobalPosition + new Vector2(150, 0);
        enemyContainer!.AddChild(enemy);

        ros.Trigger();
        bool projectileFound = false;
        foreach (var child in main.GetChildren())
        {
            if (child is RosJet rj)
            {
                projectileFound = true;
                AssertThat(rj.GlobalPosition.DistanceTo(player.GlobalPosition)).IsLessEqual(60.0f);
                break;
            }
        }
        AssertThat(projectileFound).IsTrue();
        GD.Print("[PASS] ROS Torrent projectile emission & target acquisition verified.");

        // 7. Test equipping a passive trait into passive slot 1 (slot 0 is the innate)
        var actin = new PassiveActinPolymerization();
        float initialArea = player.Stats!.GetStat("area");
        sm.EquipPassive(actin, 1);
        AssertThat(sm.GetPassiveSlot(1)).IsEqual(actin);
        AssertThat(player.Stats.GetStat("area")).IsGreater(initialArea);
        GD.Print("[PASS] Equipping passive trait dynamically modifies player stats.");

        // 8. Verify HUD displays skills
        hud!.UpdateSkillSlots();
        var slotsContainer = hud.SlotsContainer;
        if (slotsContainer != null && slotsContainer.GetChildCount() > 0)
        {
            var card0 = slotsContainer.GetChild(0);
            var iconLbl = card0.GetNodeOrNull<Label>("IconLabel");
            AssertThat(iconLbl).IsNotNull();
            AssertThat(iconLbl!.Text).IsEqual("💨");
            GD.Print("[PASS] HUD Slot 0 correctly displays active weapon icon: " + iconLbl.Text);
        }

        GD.Print("--- ALL 5+5 SKILL SYSTEM AUTOMATED TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
