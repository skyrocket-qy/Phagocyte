using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestAllCells : SceneTree
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

                    // Check Slot 0 active weapon
                    var slot0 = baseCell.CellSkillManager!.GetActiveSlot(0);
                    AssertThat(slot0).IsNotNull();

                    // Verify cell-specific starting weapon and base body stats
                    switch (cid)
                    {
                        case "macrophage":
                            AssertThat(slot0!.SkillId).IsEqual("ros_torrent");
                            AssertThat(baseCell.MaxHealth).IsEqual(100.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(230.0f);
                            break;
                        case "ctl":
                            AssertThat(slot0!.SkillId).IsEqual("perforin_lance");
                            AssertThat(baseCell.MaxHealth).IsEqual(75.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(280.0f);
                            break;
                        case "neutrophil":
                            AssertThat(slot0!.SkillId).IsEqual("complement_cascade");
                            AssertThat(baseCell.MaxHealth).IsEqual(90.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(240.0f);
                            break;
                        case "b_cell":
                            AssertThat(slot0!.SkillId).IsEqual("antibody_salvo");
                            AssertThat(baseCell.MaxHealth).IsEqual(85.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(220.0f);
                            break;
                        case "dendritic":
                            AssertThat(slot0!.SkillId).IsEqual("pseudopod_lunge");
                            AssertThat(baseCell.MaxHealth).IsEqual(95.0f);
                            AssertThat(baseCell.BaseSpeed).IsEqual(215.0f);
                            break;
                    }

                    // Cells share the same neutral stat baseline (no innate passive bonuses)
                    AssertThat(baseCell.Stats!.GetStat("might")).IsEqualApprox(1.0f, 0.001f);
                    AssertThat(baseCell.Stats!.GetStat("cooldown_reduction")).IsEqualApprox(0.0f, 0.001f);
                    AssertThat(baseCell.Stats!.GetStat("crit_chance")).IsEqualApprox(0.05f, 0.001f);
                    AssertThat(baseCell.Stats!.GetStat("magnet")).IsEqualApprox(150.0f, 0.001f);
                    AssertThat(baseCell.Stats!.GetStat("health_regen")).IsEqualApprox(0.0f, 0.001f);

                    GD.Print($"[PASS] Verified '{cid}': Base Stats, 32-Vertex Morphology, Nucleus, Slot 0 Weapon, Neutral Passives.");
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
