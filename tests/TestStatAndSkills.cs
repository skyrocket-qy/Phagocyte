using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestStatAndSkills : SceneTree
{
    private int _frameCount = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING UNIVERSAL STATS & 5+5 SKILL SUITE VERIFICATION <<<");
        GD.Print("==================================================================");

        // --- Test 1: Stat math formulas ---
        var s = new Stat(10.0f);
        AssertThat(s.GetValue()).IsEqual(10.0f);
        s.AddModifier(5.0f, 0.20f); // (10 + 5) * (1 + 0.20) = 18.0
        AssertThat(s.GetValue()).IsEqualApprox(18.0f, 0.001f);
        s.RemoveModifier(5.0f, 0.20f);
        AssertThat(s.GetValue()).IsEqualApprox(10.0f, 0.001f);
        s.SetBase(20.0f);
        AssertThat(s.GetValue()).IsEqualApprox(20.0f, 0.001f);
        GD.Print("[PASS] Test 1: Stat math calculation (base + flat) * (1 + pct) verified.");

        // --- Test 2: CellStats 18 universal stats & caps ---
        var cs = new CellStats();
        AssertThat(cs.GetStat("might")).IsEqual(1.0f);
        AssertThat(cs.GetStat("area")).IsEqual(1.0f);
        AssertThat(cs.GetStat("cooldown_reduction")).IsEqual(0.0f);
        AssertThat(cs.GetStat("move_speed")).IsEqual(230.0f);
        AssertThat(cs.GetStat("evasion")).IsEqual(0.0f);
        AssertThat(cs.GetStat("block")).IsEqual(0.0f);
        AssertThat(cs.GetStat("life_steal")).IsEqual(0.0f);

        // Test CDR clamp at 75%
        cs.AddModifier("cooldown_reduction", 0.90f, 0.0f);
        AssertThat(cs.GetStat("cooldown_reduction")).IsEqual(0.75f);
        cs.RemoveModifier("cooldown_reduction", 0.90f, 0.0f);
        AssertThat(cs.GetStat("cooldown_reduction")).IsEqual(0.0f);

        // Test Evasion clamp at 60%
        cs.AddModifier("evasion", 0.90f, 0.0f);
        AssertThat(cs.GetStat("evasion")).IsEqual(0.60f);

        // Test Block clamp at 75%
        cs.AddModifier("block", 0.90f, 0.0f);
        AssertThat(cs.GetStat("block")).IsEqual(0.75f);

        // Test Life Steal clamp at 20%
        cs.AddModifier("life_steal", 0.50f, 0.0f);
        AssertThat(cs.GetStat("life_steal")).IsEqual(0.20f);

        // Test Armor percentage formula
        cs.AddModifier("armor", 50.0f, 0.0f);
        AssertThat(cs.GetDamageReductionRatio()).IsEqualApprox(0.50f, 0.001f);
        GD.Print("[PASS] Test 2: CellStats 18 universal stats, clamps (CDR, evasion, block, life steal) & armor formula verified.");

        // --- Test 3: SkillManager 5 Active + 5 Passive Routing ---
        var sm = new SkillManager();
        AssertThat(sm.ActiveSlots.Count).IsEqual(5);
        AssertThat(sm.PassiveSlots.Count).IsEqual(5);

        var ros = new RosTorrentSkill();
        var actin = new PassiveActinPolymerization();

        bool eqActive = sm.EquipSkill(ros);
        AssertThat(eqActive).IsTrue();
        AssertThat(sm.GetActiveSlot(0)).IsEqual(ros);
        AssertThat(sm.GetPassiveSlot(0)).IsNull();

        bool eqPassive = sm.EquipSkill(actin);
        AssertThat(eqPassive).IsTrue();
        AssertThat(sm.GetPassiveSlot(0)).IsEqual(actin);

        var uiData = sm.GetUiData();
        AssertThat(uiData.ContainsKey("actives")).IsTrue();
        AssertThat(uiData.ContainsKey("passives")).IsTrue();
        GD.Print("[PASS] Test 3: SkillManager 5 Active + 5 Passive independent capacity and auto-routing verified.");

        // --- Test 4: Passive Skill Universal Stat Injection & Upgrade ---
        var mockHost = new CharacterBody2D();
        var mockStats = new CellStats { Name = "CellStats" };
        mockHost.AddChild(mockStats);

        var mockSm = new SkillManager();
        mockHost.AddChild(mockSm);
        mockSm.Setup(mockHost);

        AssertThat(mockStats.GetStat("area")).IsEqualApprox(1.0f, 0.001f);
        AssertThat(mockStats.GetStat("move_speed")).IsEqualApprox(230.0f, 0.001f);

        var passiveItem = new PassiveActinPolymerization();
        mockSm.EquipPassive(passiveItem, 0);

        // Level 1 Actin: area +12%, speed +6%
        AssertThat(mockStats.GetStat("area")).IsEqualApprox(1.12f, 0.001f);
        AssertThat(mockStats.GetStat("move_speed")).IsEqualApprox(230.0f * 1.06f, 0.01f);

        // Upgrade to Level 2: area +24%, speed +12%
        passiveItem.Upgrade();
        AssertThat(passiveItem.Level).IsEqual(2);
        AssertThat(mockStats.GetStat("area")).IsEqualApprox(1.24f, 0.001f);
        AssertThat(mockStats.GetStat("move_speed")).IsEqualApprox(230.0f * 1.12f, 0.01f);

        // Equip Lysosome: might +10%, health_regen +0.6
        var lyso = new PassiveLysosomePriming();
        mockSm.EquipPassive(lyso, 1);
        AssertThat(mockStats.GetStat("might")).IsEqualApprox(1.10f, 0.001f);
        AssertThat(mockStats.GetStat("health_regen")).IsEqualApprox(0.60f, 0.001f);

        GD.Print("[PASS] Test 4: Passive traits universal stat injection, leveling and stacking verified.");

        // --- Test 5: Active Skill Stat Consumption ---
        var activeWeapon = new RosTorrentSkill();
        mockSm.EquipActive(activeWeapon, 0);

        // Cooldown with 0% CDR
        AssertThat(activeWeapon.GetCalculatedCooldown()).IsEqualApprox(3.2f, 0.001f);

        // Add 25% CDR
        mockStats.AddModifier("cooldown_reduction", 0.25f, 0.0f);
        AssertThat(activeWeapon.GetCalculatedCooldown()).IsEqualApprox(3.2f * 0.75f, 0.001f);

        // Amount with extra amount
        AssertThat(activeWeapon.GetCalculatedAmount(1)).IsEqual(1);
        mockStats.AddModifier("amount", 2.0f, 0.0f);
        AssertThat(activeWeapon.GetCalculatedAmount(1)).IsEqual(3);

        // Area with current 1.24 area
        AssertThat(activeWeapon.GetCalculatedArea(1.0f)).IsEqualApprox(1.24f, 0.001f);

        // Damage with might 1.10
        var dmgCalc = activeWeapon.GetCalculatedDamage(20.0f);
        float dmgVal = dmgCalc["damage"].AsSingle();
        AssertThat(Mathf.IsEqualApprox(dmgVal, 22.0f) || dmgCalc["is_crit"].AsBool()).IsTrue();

        GD.Print("[PASS] Test 5: Active weapon stat consumption (CDR, Area, Amount, Damage) verified.");

        mockHost.QueueFree();

        // Now load the actual game scene to test complete runtime integration
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        var mainInstance = mainScene!.Instantiate();
        Root.AddChild(mainInstance);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _frameCount++;
        if (_frameCount < 5)
            return false;

        _testDone = true;
        var main = Root.GetNodeOrNull<Main>("Main");
        AssertThat(main).IsNotNull();

        var player = main!.GetNodeOrNull<Macrophage>("Macrophage");
        AssertThat(player).IsNotNull();
        AssertThat(player!.Stats).IsNotNull();

        var sm = player.CellSkillManager;
        AssertThat(sm).IsNotNull();
        AssertThat(sm!.ActiveSlots.Count).IsEqual(5);
        AssertThat(sm.PassiveSlots.Count).IsEqual(5);

        var slot0Active = sm.GetActiveSlot(0);
        AssertThat(slot0Active is RosTorrentSkill).IsTrue();

        // Verify Macrophage movement speed bound to CellStats
        AssertThat(player.CurrentSpeed).IsEqual(player.Stats!.GetStat("move_speed"));

        // Verify HUD exists
        var hud = main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();

        GD.Print("[PASS] Test 6: In-game Main scene integration with Macrophage, CellStats & HUD verified.");
        GD.Print("==================================================================");
        GD.Print(">>> ALL UNIVERSAL STAT & 5+5 SKILL TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }
}
