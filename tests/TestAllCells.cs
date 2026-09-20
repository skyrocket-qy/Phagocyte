using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestAllCells : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;
    private Node? _mainInstance = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING FULL IMMUNE CELL ROSTER VERIFICATION (5 CELLS) <<<");
        GD.Print("==================================================================");
    }

    public override bool _Process(double delta)
    {
        switch (_phase)
        {
            case 0:
                // --- Phase 0: Test All 5 Cell Archetypes in Isolation ---
                string[] cellIds = ["macrophage", "ctl", "neutrophil", "b_cell", "dendritic"];
                foreach (var cid in cellIds)
                {
                    var scene = GameManager.GetCellScene(cid);
                    AssertThat(scene).IsNotNull();
                    var cell = scene!.Instantiate();
                    AssertThat(cell is BaseCell).IsTrue();
                    var baseCell = (BaseCell)cell;
                    Root.AddChild(baseCell);

                    AssertThat(baseCell.Stats).IsNotNull();
                    AssertThat(baseCell.CellSkillManager).IsNotNull();

                    // Check smooth organic deformation
                    baseCell.UpdatePseudopodDeformation(0.016f);
                    AssertThat(baseCell.Cytoplasm!.Polygon.Length >= 64).IsTrue();
                    AssertThat(baseCell.EngulfCollider!.Polygon.Length).IsEqual(32);
                    AssertThat(baseCell.Nucleus!.Polygon.Length).IsGreaterEqual(16);

                    // Verify class-exclusive innate skill binding (docs/cell.md Â§2)
                    var sm = baseCell.CellSkillManager!;
                    var slot0 = sm.GetActiveSlot(0);

                    switch (cid)
                    {
                        case "macrophage":
                            AssertThat(slot0).IsNotNull();
                            AssertThat(slot0!.SkillId).IsEqual("phagocytic_grasp");
                            AssertThat(slot0!.IsInnate).IsTrue();
                            AssertThat(sm.GetPassiveSlot(0)).IsNull();
                            AssertThat(baseCell.MaxHealth).IsEqual(140.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(210.0f);
                            break;
                        case "ctl":
                            AssertThat(slot0).IsNotNull();
                            AssertThat(slot0!.SkillId).IsEqual("perforin_lance");
                            AssertThat(slot0!.IsInnate).IsTrue();
                            AssertThat(baseCell.MaxHealth).IsEqual(90.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(260.0f);
                            break;
                        case "neutrophil":
                            AssertThat(slot0).IsNotNull();
                            AssertThat(slot0!.SkillId).IsEqual("granzyme_detonation");
                            AssertThat(slot0!.IsInnate).IsTrue();
                            AssertThat(baseCell.MaxHealth).IsEqual(100.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(230.0f);
                            break;
                        case "b_cell":
                            AssertThat(slot0).IsNotNull();
                            AssertThat(slot0!.SkillId).IsEqual("antibody_salvo");
                            AssertThat(slot0!.IsInnate).IsTrue();
                            AssertThat(baseCell.MaxHealth).IsEqual(95.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(220.0f);
                            break;
                        case "dendritic":
                            AssertThat(slot0).IsNotNull();
                            AssertThat(slot0!.SkillId).IsEqual("mhc_tracer_beam");
                            AssertThat(slot0!.IsInnate).IsTrue();
                            AssertThat(baseCell.MaxHealth).IsEqual(110.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(225.0f);
                            break;
                    }

                    // Verify class-specific Lv.1 base stat matrix (docs/cell.md Â§3)
                    switch (cid)
                    {
                        case "macrophage":
                            AssertThat(baseCell.Stats!.GetStat("armor")).IsEqualApprox(10.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("area")).IsEqualApprox(1.25f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("might")).IsEqualApprox(1.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("block")).IsEqualApprox(0.08f, 0.001f);
                            break;
                        case "ctl":
                            AssertThat(baseCell.Stats!.GetStat("armor")).IsEqualApprox(0.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("crit_chance")).IsEqualApprox(0.15f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("evasion")).IsEqualApprox(0.10f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("pierce")).IsEqualApprox(1.0f, 0.001f);
                            break;
                        case "neutrophil":
                            AssertThat(baseCell.Stats!.GetStat("armor")).IsEqualApprox(5.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("might")).IsEqualApprox(1.2f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("knockback")).IsEqualApprox(1.4f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("health_regen")).IsEqualApprox(0.5f, 0.001f);
                            break;
                        case "b_cell":
                            AssertThat(baseCell.Stats!.GetStat("armor")).IsEqualApprox(0.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("projectile_speed")).IsEqualApprox(1.3f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("cooldown_reduction")).IsEqualApprox(0.10f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("amount")).IsEqualApprox(1.0f, 0.001f);
                            break;
                        case "dendritic":
                            AssertThat(baseCell.Stats!.GetStat("armor")).IsEqualApprox(2.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("magnet")).IsEqualApprox(260.0f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("duration")).IsEqualApprox(1.2f, 0.001f);
                            AssertThat(baseCell.Stats!.GetStat("cooldown_reduction")).IsEqualApprox(0.10f, 0.001f);
                            break;
                    }

                    GD.Print($"[PASS] Verified '{cid}': Base Stats, 32-Vertex Morphology, Nucleus, Innate Binding, Calibrated Stat Matrix.");
                    baseCell.QueueFree();
                }

                GD.Print("[PASS] All 5 cell archetypes verified successfully in isolation.");

                // --- Phase 1: In-Game Runtime Instantiation with CTL selected ---
                GameManager.SelectedClass = "ctl";
                var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
                AssertThat(mainScene).IsNotNull();
                _mainInstance = mainScene!.Instantiate();
                Root.AddChild(_mainInstance);
                _phase = 1;
                return false;

            case 1:
                _frameCount++;
                if (_frameCount < 4)
                    return false;

                var main = Root.GetNodeOrNull<Main>("Main");
                AssertThat(main).IsNotNull();

                // Verify active player is CtlCell
                AssertThat(main!.Player).IsNotNull();
                AssertThat(main.Player is CtlCell).IsTrue();
                AssertThat(main.Player!.IsInGroup("player")).IsTrue();
                AssertThat(main.MainCamera!.GetParent()).IsEqual(main.Player);
                AssertThat(main.HudNode!.PlayerRef).IsEqual(main.Player);

                GD.Print("[PASS] In-game runtime dynamic spawning and integration with non-default cell (CTL) verified.");
                GD.Print("==================================================================");
                GD.Print(">>> ALL 5 IMMUNE CELL ARCHETYPES FULLY VERIFIED! <<<");
                GD.Print("==================================================================");

                // Reset selected class to default
                GameManager.SelectedClass = "macrophage";
                Quit(0);
                return true;
        }

        return false;
    }
}
