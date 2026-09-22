using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestAchievementSystem : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;
    private MainMenu? _menuInstance = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING ACHIEVEMENT & IMMUNE CELL PROGRESSION TEST <<<");
        GD.Print("==================================================================");

        // Isolate all persistent manager saves from the player's real files
        IsolateSaves("achievement");
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                // --- Step 1: Initial State (Only Macrophage unlocked, others locked) ---
                AchievementManager.ResetAll();

                AssertThat(GameManager.IsClassUnlocked("macrophage")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("ctl")).IsFalse();
                AssertThat(GameManager.IsClassUnlocked("neutrophil")).IsFalse();
                AssertThat(GameManager.IsClassUnlocked("b_cell")).IsFalse();
                AssertThat(GameManager.IsClassUnlocked("dendritic")).IsFalse();

                // Only the tutorial organ map is unlocked; other maps and all Hard modes are locked
                AssertThat(GameManager.IsMapUnlocked("acute_wound")).IsTrue();
                AssertThat(GameManager.IsMapUnlocked("alveolar_space")).IsFalse();
                AssertThat(GameManager.IsMapUnlocked("hepatic_sinusoid")).IsFalse();
                AssertThat(GameManager.IsMapUnlocked("gastric_lumen")).IsFalse();
                AssertThat(GameManager.IsMapUnlocked("blood_brain_barrier")).IsFalse();
                AssertThat(GameManager.IsMapHardUnlocked("acute_wound")).IsFalse();
                AssertThat(GameManager.IsMapHardUnlocked("alveolar_space")).IsFalse();

                GD.Print("[PASS] Step 1: Default state strictly enforces only Macrophage and Acute Wound unlocked.");

                // --- Step 2: Skill Locking in UpgradeManager Choice Pool ---
                var mockPlayer = new CharacterBody2D();
                var stats = new CellStats { Name = "CellStats" };
                mockPlayer.AddChild(stats);
                var sm = new SkillManager { Name = "SkillManager" };
                mockPlayer.AddChild(sm);
                sm.Setup(mockPlayer);
                sm.EquipActive(new RosTorrentSkill(), 0);

                // While other 4 cells are locked, their active skills must NEVER be offered
                string[] lockedSkillIds = ["perforin_lance", "complement_cascade", "antibody_salvo", "pseudopod_lunge"];
                for (int iter = 0; iter < 15; iter++)
                {
                    var choices = UpgradeManager.GenerateChoices(mockPlayer, 3);
                    foreach (var c in choices)
                    {
                        string id = c["id"].AsString();
                        AssertThat(Array.IndexOf(lockedSkillIds, id) >= 0).IsFalse();
                    }
                }

                GD.Print("[PASS] Step 2: Locked cells' signature skills are completely excluded from upgrade pool.");

                // --- Step 3: Event-Driven Achievement Unlocks & Cell Rewards ---
                // Calibrated thresholds: sub-threshold values must stay locked
                AchievementManager.RecordEvent("pathogen_digested", 199);
                AssertThat(AchievementManager.IsUnlocked("engulf_20")).IsFalse();
                AchievementManager.RecordEvent("level_up", 14);
                AssertThat(AchievementManager.IsUnlocked("reach_level_5")).IsFalse();
                AchievementManager.RecordEvent("survival_time", 479.0f);
                AssertThat(AchievementManager.IsUnlocked("survive_180s")).IsFalse();
                AchievementManager.RecordEvent("active_skills_count", 4);
                AssertThat(AchievementManager.IsUnlocked("full_arsenal")).IsFalse();
                GD.Print("[PASS] Step 3: Calibrated thresholds reject sub-threshold progress.");

                // Test 3a: Digestion 200 -> Unlocks CTL and Perforin Lance
                AchievementManager.RecordEvent("pathogen_digested", 200);
                AssertThat(AchievementManager.IsUnlocked("first_digestion")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("engulf_20")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("ctl")).IsTrue();

                // Now perforin_lance should be allowed in the candidate pool
                bool sawPerforin = false;
                for (int iter = 0; iter < 30; iter++)
                {
                    var choices = UpgradeManager.GenerateChoices(mockPlayer, 3);
                    foreach (var c in choices)
                    {
                        if (c["id"].AsString() == "perforin_lance")
                        {
                            sawPerforin = true;
                            break;
                        }
                    }
                    if (sawPerforin) break;
                }
                AssertThat(sawPerforin).IsTrue();
                GD.Print("[PASS] Step 3a: Engulf 20 unlocks CTL and adds Perforin Lance to upgrade pool.");

                // Test 3b: Devour 500 -> Unlocks Neutrophil
                AchievementManager.RecordEvent("pathogen_digested", 500);
                AssertThat(AchievementManager.IsUnlocked("devour_50")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("neutrophil")).IsTrue();
                GD.Print("[PASS] Step 3b: Devouring 500 pathogens unlocks Neutrophil.");

                // Test 3c: Level 15 -> Unlocks B-Cell
                AchievementManager.RecordEvent("level_up", 15);
                AssertThat(AchievementManager.IsUnlocked("reach_level_5")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("b_cell")).IsTrue();
                GD.Print("[PASS] Step 3c: Level 15 unlocks B-Cell.");

                // Test 3d: Survival 480s -> Unlocks Dendritic Cell
                AchievementManager.RecordEvent("survival_time", 485.0f);
                AssertThat(AchievementManager.IsUnlocked("survive_180s")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("dendritic")).IsTrue();
                GD.Print("[PASS] Step 3d: Survival 480s unlocks Dendritic Cell.");

                // Test 3e: Full 5-active loadout -> Metabolic Arsenal
                AchievementManager.RecordEvent("active_skills_count", 5);
                AssertThat(AchievementManager.IsUnlocked("full_arsenal")).IsTrue();
                GD.Print("[PASS] Step 3e: Five simultaneous active skills unlock Metabolic Arsenal.");

                // Test 3f: First ultimate epigenetic fusion
                AssertThat(AchievementManager.IsUnlocked("first_evolution")).IsFalse();
                AchievementManager.RecordEvent("first_evolution");
                AssertThat(AchievementManager.IsUnlocked("first_evolution")).IsTrue();
                GD.Print("[PASS] Step 3f: First epigenetic evolution fusion recorded.");

                // Test 3g: PrPsc amyloid crystal shattered (non-prion kills must not count)
                AssertThat(AchievementManager.IsUnlocked("prion_cleared")).IsFalse();
                AchievementManager.RecordEvent("pathogen_killed", "tachyzoite");
                AssertThat(AchievementManager.IsUnlocked("prion_cleared")).IsFalse();
                AchievementManager.RecordEvent("pathogen_killed", "prpsc_amyloid_aggregate");
                AssertThat(AchievementManager.IsUnlocked("prion_cleared")).IsTrue();
                GD.Print("[PASS] Step 3g: Shattering a PrPsc amyloid crystal unlocks Protein Scavenger.");

                mockPlayer.QueueFree();

                // --- Step 4: Disk Persistence Verification ---
                AchievementManager.SaveToDisk();
                AchievementManager.UnlockedIds.Clear();
                AchievementManager.ProgressData.Clear();
                AchievementManager.LoadFromDisk();

                AssertThat(AchievementManager.IsUnlocked("engulf_20")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("devour_50")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("reach_level_5")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("survive_180s")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("full_arsenal")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("first_evolution")).IsTrue();
                AssertThat(AchievementManager.IsUnlocked("prion_cleared")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("ctl")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("neutrophil")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("b_cell")).IsTrue();
                AssertThat(GameManager.IsClassUnlocked("dendritic")).IsTrue();
                GD.Print("[PASS] Step 4: Full disk persistence (isolated achievement save) verified.");

                // --- Step 4b: Organ Map Unlock Chain (docs/achievement.md §2) ---
                AchievementManager.RecordMapClear("acute_wound");
                AssertThat(AchievementManager.IsUnlocked("wound_clear")).IsTrue();
                AssertThat(GameManager.IsMapUnlocked("alveolar_space")).IsTrue();
                AssertThat(GameManager.IsMapHardUnlocked("acute_wound")).IsTrue();

                AchievementManager.RecordMapClear("alveolar_space");
                AssertThat(AchievementManager.IsUnlocked("alveolar_clear")).IsTrue();
                AssertThat(GameManager.IsMapUnlocked("hepatic_sinusoid")).IsTrue();
                AssertThat(GameManager.IsMapHardUnlocked("alveolar_space")).IsTrue();

                AchievementManager.RecordMapClear("hepatic_sinusoid");
                AssertThat(GameManager.IsMapUnlocked("gastric_lumen")).IsTrue();
                AssertThat(GameManager.IsMapHardUnlocked("hepatic_sinusoid")).IsTrue();

                AchievementManager.RecordMapClear("gastric_lumen");
                AssertThat(GameManager.IsMapUnlocked("blood_brain_barrier")).IsTrue();
                AssertThat(GameManager.IsMapHardUnlocked("gastric_lumen")).IsTrue();

                int bonusBefore = PassiveTreeManager.BonusPoints;
                AchievementManager.RecordMapClear("blood_brain_barrier");
                AssertThat(AchievementManager.IsUnlocked("bbb_clear")).IsTrue();
                AssertThat(GameManager.IsMapHardUnlocked("blood_brain_barrier")).IsTrue();
                AssertThat(PassiveTreeManager.BonusPoints).IsEqual(bonusBefore + 2);

                AssertThat(AchievementManager.IsEndlessUnlocked()).IsFalse();
                AchievementManager.RecordMapClear("acute_wound", true);
                AssertThat(AchievementManager.IsUnlocked("wound_hard_clear")).IsTrue();
                AssertThat(AchievementManager.IsEndlessUnlocked()).IsTrue();

                // Map lock state is derived from the achievement save and survives a reload
                AchievementManager.SaveToDisk();
                GameManager.ResetMapUnlocks();
                AssertThat(GameManager.IsMapUnlocked("alveolar_space")).IsFalse();
                AchievementManager.LoadFromDisk();
                AssertThat(GameManager.IsMapUnlocked("alveolar_space")).IsTrue();
                AssertThat(GameManager.IsMapHardUnlocked("blood_brain_barrier")).IsTrue();

                GD.Print("[PASS] Step 4b: Organ map unlock chain, +2 talent points and Endless unlock verified.");

                // --- Step 5: MainMenu Achievement Gallery (independent fifth view) ---
                // Achievements no longer live in Codex Tab 4; the gallery owns them.
                var galleryScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
                AssertThat(galleryScene).IsNotNull();
                var galleryMenu = galleryScene!.Instantiate<MainMenu>();
                Root.AddChild(galleryMenu);
                AssertThat(galleryMenu.AchievementsBtn).IsNotNull();
                AssertThat(galleryMenu.AchievementView).IsNotNull();

                // Open via the dedicated MainMenu button (same path as a player click)
                galleryMenu.AchievementsBtn!.EmitSignal(Button.SignalName.Pressed);
                AssertThat(galleryMenu.AchievementView!.Visible).IsTrue();
                AssertThat(galleryMenu.AchievementView.CardCount).IsEqual(AchievementManager.Achievements.Count);
                AssertThat(string.IsNullOrEmpty(galleryMenu.AchievementView.DetailTitle!.Text)).IsFalse();

                // Three-state filter partitions the full set (all == unlocked + locked)
                galleryMenu.AchievementView.SetFilter(AchievementGalleryView.FilterUnlocked);
                int unlockedShown = galleryMenu.AchievementView.CardCount;
                galleryMenu.AchievementView.SetFilter(AchievementGalleryView.FilterLocked);
                int lockedShown = galleryMenu.AchievementView.CardCount;
                AssertThat(unlockedShown + lockedShown).IsEqual(AchievementManager.Achievements.Count);
                galleryMenu.AchievementView.SetFilter(AchievementGalleryView.FilterAll);
                AssertThat(galleryMenu.AchievementView.CardCount).IsEqual(AchievementManager.Achievements.Count);

                // Steam-mirrored artwork fields exist; missing files fall back cleanly
                var galleryInfo = AchievementManager.GetAchievementInfo("engulf_20");
                AssertThat(galleryInfo.ContainsKey("image_path")).IsTrue();
                AssertThat(galleryInfo.ContainsKey("steam_api_name")).IsTrue();
                AssertThat(galleryInfo["steam_api_name"].AsString()).IsEqual("ENGULF_20");
                AssertThat(AssetLoader.TryLoad<Texture2D>(galleryInfo["image_path"].AsString()) == null).IsTrue();
                GD.Print("[PASS] Step 5: AchievementGallery fifth-view rendering, 3-state filter and Steam art fallback verified.");

                // --- Step 6: Main Menu Locked Cell Status & Confirm Button Test ---
                AchievementManager.ResetAll(); // Reset so cells are locked again
                galleryMenu.QueueFree();

                var menuScene = AssetLoader.Load<PackedScene>("scenes/ui/main_menu.tscn");
                AssertThat(menuScene).IsNotNull();
                _menuInstance = menuScene!.Instantiate<MainMenu>();
                Root.AddChild(_menuInstance);
                _phase = 1;
                return false;

            case 1:
                _frameCount++;
                if (_frameCount < 3)
                    return false;

                // Call OnStartPressed
                _menuInstance!.StartBtn!.EmitSignal(Button.SignalName.Pressed);

                // Select locked cell CTL
                _menuInstance.SelectClass("ctl");
                AssertThat(_menuInstance.ClassConfirmBtn!.Disabled).IsTrue();
                AssertThat(_menuInstance.ClassStatusLbl!.Text.Contains("🏆")).IsTrue();

                // Select unlocked cell Macrophage
                _menuInstance.SelectClass("macrophage");
                AssertThat(_menuInstance.ClassConfirmBtn.Disabled).IsFalse();

                // Unlock CTL and refresh
                AchievementManager.Unlock("engulf_20");
                _menuInstance.UpdateAllTexts();
                _menuInstance.SelectClass("ctl");
                AssertThat(_menuInstance.ClassConfirmBtn.Disabled).IsFalse();

                GD.Print("[PASS] Step 6: Main Menu locked condition UI and button disabling verified.");
                GD.Print("==================================================================");
                GD.Print(">>> ACHIEVEMENT & IMMUNE CELL PROGRESSION SUITE PASSED! <<<");
                GD.Print("==================================================================");

                _menuInstance.QueueFree();
                AchievementManager.ResetAll();
                CleanupTestSave();
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[TEST FAILED]: " + ex.ToString());
            CleanupTestSave();
            Quit(1);
            return true;
        }

        return false;
    }

    private static void CleanupTestSave()
    {
        RestoreSaves();
    }
}
