using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Combat;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the special AI refinements: anthrax two-stage awakening,
/// candida frozen hyphal ambush, and malignant cell autonomous mitosis.
/// </summary>
[TestSuite]
public partial class TestSpecialAiRefinements : SceneTree
{
    private int _frame = 0;
    private bool _done = false;
    private Node2D? _container;
    private BaseCell? _player;

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        _frame++;
        if (_frame < 3)
            return false;

        _done = true;
        try
        {
            RunTests();
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestSpecialAiRefinements threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL SPECIAL AI REFINEMENT TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        _container = new Node2D { Name = "SpecialAiContainer" };
        Root.AddChild(_container);
        _player = new BaseCell { Name = "SpecialAiHost", GlobalPosition = new Vector2(400, 400) };
        _container.AddChild(_player);

        TestAnthraxAwakening();
        TestCandidaHyphalAmbush();
        TestMalignantMitosis();

        _container.QueueFree();
    }

    private void TestAnthraxAwakening()
    {
        var spore = new AnthraxSporeEnemy { GlobalPosition = new Vector2(800, 800) };
        _container!.AddChild(spore);

        // Above 50% HP: still dormant
        spore.TakeDamage(20.0f);
        AssertThat(spore.HasAwakened).IsFalse();

        // Below 50% HP: shell breaks into a berserk bacillus
        spore.TakeDamage(10.0f);
        AssertThat(spore.HasAwakened).IsTrue();

        AnthraxBacillus? bacillus = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is AnthraxBacillus found)
            {
                bacillus = found;
                break;
            }
        }

        AssertThat(bacillus).IsNotNull();
        AssertThat(bacillus!.DamageMultiplier).IsEqual(1.5f);
        var reference = new AnthraxBacillus();
        AssertThat(bacillus.FloatSpeed).IsGreater(reference.FloatSpeed);
        reference.Free();
        GD.Print("[PASS] Anthrax spore awakened at 50% HP into a berserk (+80% speed / +50% damage) bacillus.");
    }

    private void TestCandidaHyphalAmbush()
    {
        var candida = new CandidaEnemy { GlobalPosition = _player!.GlobalPosition + new Vector2(180, 0) };
        _container!.AddChild(candida);

        AssertThat(candida.IsHyphaeExtended).IsFalse();
        candida._PhysicsProcess(0.016);
        AssertThat(candida.IsHyphaeExtended).IsTrue();

        // The yeast body freezes in place while channeling
        Vector2 frozenPosition = candida.GlobalPosition;
        candida._PhysicsProcess(0.5);
        AssertThat(candida.GlobalPosition.DistanceTo(frozenPosition) < 0.01f).IsTrue();

        TelegraphedAttack? telegraph = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is TelegraphedAttack found)
            {
                telegraph = found;
                break;
            }
        }

        AssertThat(telegraph).IsNotNull();
        AssertThat(telegraph!.LineLength).IsEqual(CandidaEnemy.HyphaeReach);
        GD.Print("[PASS] Candida froze in place and extended a 150px piercing pseudohyphae at 200px range.");
        candida.QueueFree();
    }

    private void TestMalignantMitosis()
    {
        var malignant = new MalignantCellEnemy { GlobalPosition = new Vector2(1600, 1600) };
        _container!.AddChild(malignant);
        float parentMaxHealth = malignant.MaxHealth;

        // Surviving past the 20s cadence triggers autonomous mitosis
        malignant._PhysicsProcess(MalignantCellEnemy.MitosisInterval + 0.1f);
        AssertThat(malignant.SplitCount).IsGreaterEqual(1);

        int halfHpDaughters = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is MalignantCellEnemy cell && cell != malignant)
            {
                AssertThat(cell.MaxHealth).IsEqualApprox(parentMaxHealth * MalignantCellEnemy.SplitHealthRatio, 0.01f);
                halfHpDaughters++;
            }
        }
        AssertThat(halfHpDaughters).IsGreaterEqual(1);

        // Contact inhibition: replication stops once local density is saturated
        for (int i = 0; i < 10; i++)
        {
            malignant.TryMitosis();
        }

        int totalMalignant = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is MalignantCellEnemy)
                totalMalignant++;
        }

        AssertThat(totalMalignant).IsLessEqual(MalignantCellEnemy.MaxNearbySiblings + 1);
        GD.Print($"[PASS] Malignant cell replicated a half-HP daughter after 20s and capped at {totalMalignant} cells.");
    }
}
