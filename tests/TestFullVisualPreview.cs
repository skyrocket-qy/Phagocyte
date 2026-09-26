using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Comprehensive visual preview and regression harness covering ALL major UI
/// views, modals, notifications, combat HUD, and active skill VFX in Phagocyte.
/// Saves high-resolution viewport PNGs via <see cref="TestHarness.CaptureScreenshot"/>
/// into PHAGOCYTE_CAPTURE_DIR (or tmp/visual_before).
/// </summary>
[TestSuite]
public partial class TestFullVisualPreview : TestHarness
{
    private int _frame = 0;
    private int _stage = 0;

    private MainMenu? _menu;
    private UpgradeModal? _upgradeModal;
    private Node2D? _arena;
    private Macrophage? _player;
    private List<ComplementCascadeSkill.MacAssemblyMine>? _activeMines;

    public override void _Initialize()
    {
        Banner("FULL GAME VISUAL & SKILL PREVIEW HARNESS");
        IsolateSaves("full_preview");
        AchievementManager.ResetAll();
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_stage)
            {
                // =============================================================
                // 1. MAIN MENU & UI SCREENS
                // =============================================================
                case 0:
                    if (!Gate(ref _frame, 2))
                        return false;
                    OpenMainMenu();
                    _frame = 0;
                    _stage = 1;
                    return false;

                case 1:
                    // 1. Title View
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("title_view.png");
                    _menu!.OnStartPressed();
                    _frame = 0;
                    _stage = 2;
                    return false;

                case 2:
                    // 2. Class Selection View
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("class_view.png");
                    _menu!.SelectClass("macrophage");
                    _menu.OnClassConfirmPressed();
                    _frame = 0;
                    _stage = 3;
                    return false;

                case 3:
                    // 3. Gear Loadout Chamber
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("loadout_view.png");
                    _menu!.OnLoadoutConfirmPressed();
                    _frame = 0;
                    _stage = 4;
                    return false;

                case 4:
                    // 4. Passive Talent Tree View
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("passive_view.png");
                    _menu!.OnPassiveConfirmPressed();
                    _frame = 0;
                    _stage = 5;
                    return false;

                case 5:
                    // 5. Map Selection View
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("map_view.png");
                    // Return to title screen
                    _menu!.OnGlobalBackPressed();
                    _menu.OnGlobalBackPressed();
                    _menu.OnGlobalBackPressed();
                    _menu.OnGlobalBackPressed();
                    _frame = 0;
                    _stage = 6;
                    return false;

                case 6:
                    // 6. Open Achievement Gallery
                    if (!Gate(ref _frame, 4))
                        return false;
                    _menu!.AchievementsBtn!.EmitSignal(Button.SignalName.Pressed);
                    AchievementManager.Unlock("engulf_20");
                    AchievementManager.Unlock("first_digestion");
                    _frame = 0;
                    _stage = 7;
                    return false;

                case 7:
                    // 7. Achievement Gallery (All)
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("gallery_all.png");
                    _menu!.AchievementView!.SetFilter(AchievementGalleryView.FilterLocked);
                    _frame = 0;
                    _stage = 8;
                    return false;

                case 8:
                    // 8. Achievement Gallery (Locked)
                    if (!Gate(ref _frame, 4))
                        return false;
                    CaptureScreenshot("gallery_locked.png");
                    _menu!.OnGlobalBackPressed();
                    _menu.EndlessSetupModal!.OpenSetup();
                    _frame = 0;
                    _stage = 9;
                    return false;

                case 9:
                    // 9. Endgame Affliction Setup Modal
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("endgame_setup.png");
                    _menu!.EndlessSetupModal!.CancelBtn!.EmitSignal(Button.SignalName.Pressed);
                    _menu.CellCodexModal!.OpenCodex(0);
                    _frame = 0;
                    _stage = 10;
                    return false;

                case 10:
                    // 10. Codex Modal
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("codex_modal.png");
                    _menu!.CellCodexModal!.CloseBtn!.EmitSignal(Button.SignalName.Pressed);
                    _menu.CellSettingsModal!.OpenSettings(0);
                    _frame = 0;
                    _stage = 11;
                    return false;

                case 11:
                    // 11. Settings Modal
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("settings_modal.png");
                    _menu!.CellSettingsModal!.CloseBtn!.EmitSignal(Button.SignalName.Pressed);
                    _menu.RecordsModal!.OpenHistory();
                    _frame = 0;
                    _stage = 12;
                    return false;

                case 12:
                    // 12. Run Records Modal
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("run_records.png");
                    _menu!.RecordsModal!.CloseBtn!.EmitSignal(Button.SignalName.Pressed);
                    FreeMainMenu();
                    ShowSampleToast();
                    _frame = 0;
                    _stage = 13;
                    return false;

                // =============================================================
                // 2. TOASTS & IN-GAME UPGRADE MODALS
                // =============================================================
                case 13:
                    // 13. Toast Banner
                    if (!Gate(ref _frame, 4))
                        return false;
                    CaptureScreenshot("toast_banner.png");
                    ShowSampleAchievementToast();
                    _frame = 0;
                    _stage = 14;
                    return false;

                case 14:
                    // 14. Achievement Unlock Toast (tween settled)
                    if (!Gate(ref _frame, 30))
                        return false;
                    CaptureScreenshot("achievement_toast.png");
                    ClearRootPopups();
                    ShowSampleUpgradeModal();
                    _frame = 0;
                    _stage = 15;
                    return false;

                case 15:
                    // 15. Upgrade / Mutation Level-Up Modal
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("upgrade_modal.png");
                    ClearRootPopups();
                    SetupCombatArena();
                    _frame = 0;
                    _stage = 16;
                    return false;

                // =============================================================
                // 3. COMBAT HUD & LIVE SKILL VFX
                // =============================================================
                case 16:
                    // 16. Combat HUD & Player Macrophage
                    if (!Gate(ref _frame, 8))
                        return false;
                    CaptureScreenshot("hud_hp.png");

                    // Trigger Skill 1: Complement Cascade (MAC Assembly Mine)
                    ClearArenaForSkillPreview();
                    var comp = new ComplementCascadeSkill();
                    EquipSkillForPreview(comp, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    comp.Trigger();
                    _activeMines = CollectNodes<ComplementCascadeSkill.MacAssemblyMine>(_main!);
                    _frame = 0;
                    _stage = 17;
                    return false;

                case 17:
                    // 17. MAC Assembly Mine
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("skill_mac_mine.png");
                    if (_activeMines != null && _activeMines.Count > 0)
                        _activeMines[0].AssemblyTime = 0.05f; // fast forward fuse
                    _frame = 0;
                    _stage = 18;
                    return false;

                case 18:
                    // 18. MAC Detonation Blast
                    if (!Gate(ref _frame, 15))
                        return false;
                    CaptureScreenshot("skill_mac_blast.png");

                    // Trigger Skill 2: Antibody Salvo
                    ClearArenaForSkillPreview();
                    var ab = new AntibodySalvoSkill();
                    EquipSkillForPreview(ab, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(240, -40));
                    ab.Trigger();
                    _frame = 0;
                    _stage = 19;
                    return false;

                case 19:
                    // 19. Antibody Missile in Flight
                    if (!Gate(ref _frame, 18))
                        return false;
                    CaptureScreenshot("skill_antibody_missile.png");

                    // Trigger Skill 3: Perforin Lance
                    ClearArenaForSkillPreview();
                    var perf = new PerforinLanceSkill();
                    EquipSkillForPreview(perf, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(170, 0));
                    perf.Trigger();
                    _frame = 0;
                    _stage = 20;
                    return false;

                case 20:
                    // 20. Perforin Lance Beam & Pore Decals
                    if (!Gate(ref _frame, 3))
                        return false;
                    CaptureScreenshot("skill_perforin_lance.png");

                    // Trigger Skill 4: Phagocytic Grasp Chain
                    ClearArenaForSkillPreview();
                    var grasp = new PhagocyticGraspSkill();
                    EquipSkillForPreview(grasp, 0);
                    SpawnEnemy<TbEnemy>(new Vector2(140, 0));
                    grasp.Trigger();
                    _frame = 0;
                    _stage = 21;
                    return false;

                case 21:
                    // 21. Phagocytic Grasp Chain
                    if (!Gate(ref _frame, 4))
                        return false;
                    CaptureScreenshot("skill_grasp_chain.png");

                    // Trigger Skill 5: ROS Torrent Jet
                    ClearArenaForSkillPreview();
                    var ros = new RosTorrentSkill();
                    EquipSkillForPreview(ros, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(180, 0));
                    ros.Trigger();
                    _frame = 0;
                    _stage = 22;
                    return false;

                case 22:
                    // 22. ROS Jet Spray
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("skill_ros_torrent.png");

                    // Trigger Granzyme Detonation
                    ClearArenaForSkillPreview();
                    var gran = new GranzymeDetonationSkill();
                    EquipSkillForPreview(gran, 1);
                    var granFoe = SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    gran.DetonateCaspase(granFoe.GlobalPosition);
                    _frame = 0;
                    _stage = 23;
                    return false;

                case 23:
                    // 23. Granzyme Detonation Apoptosis Shockwave
                    if (!Gate(ref _frame, 5))
                        return false;
                    CaptureScreenshot("skill_granzyme_detonation.png");

                    // Trigger Nuclease Blades
                    ClearArenaForSkillPreview();
                    var nuc = new NucleaseBladesSkill();
                    EquipSkillForPreview(nuc, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(150, 0));
                    nuc.Trigger();
                    _frame = 0;
                    _stage = 24;
                    return false;

                case 24:
                    // 24. Nuclease Blades Orbiting Crescent Scythes
                    if (!Gate(ref _frame, 12))
                        return false;
                    CaptureScreenshot("skill_nuclease_blades.png");

                    // Trigger Defensin Barbs
                    ClearArenaForSkillPreview();
                    var def = new DefensinBarbsSkill();
                    EquipSkillForPreview(def, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    def.Trigger();
                    _frame = 0;
                    _stage = 25;
                    return false;

                case 25:
                    // 25. Defensin Barbs Crystalline Needles
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("skill_defensin_barbs.png");

                    // Trigger Pro-Inflammatory Arc
                    ClearArenaForSkillPreview();
                    var arc = new ProInflammatoryArcSkill();
                    EquipSkillForPreview(arc, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(150, 0));
                    SpawnEnemy<TbEnemy>(new Vector2(220, 30));
                    arc.Trigger();
                    _frame = 0;
                    _stage = 26;
                    return false;

                case 26:
                    // 26. Pro-Inflammatory Arc Cytokine Electric Discharge
                    if (!Gate(ref _frame, 5))
                        return false;
                    CaptureScreenshot("skill_pro_inflammatory_arc.png");

                    // Trigger Exosome Singularity
                    ClearArenaForSkillPreview();
                    var exo = new ExosomeSingularitySkill();
                    EquipSkillForPreview(exo, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    exo.Trigger();
                    _frame = 0;
                    _stage = 27;
                    return false;

                case 27:
                    // 27. Exosome Singularity Vesicular Vortex
                    if (!Gate(ref _frame, 10))
                        return false;
                    CaptureScreenshot("skill_exosome_singularity.png");

                    // Trigger Phagolysosome Vent
                    ClearArenaForSkillPreview();
                    var vent = new PhagolysosomeVentSkill();
                    EquipSkillForPreview(vent, 1);
                    vent.Trigger();
                    _frame = 0;
                    _stage = 28;
                    return false;

                case 28:
                    // 28. Phagolysosome Vent Corrosive Enzymatic Puddle
                    if (!Gate(ref _frame, 10))
                        return false;
                    CaptureScreenshot("skill_phagolysosome_vent.png");

                    // Trigger MHC Tracer Beam
                    ClearArenaForSkillPreview();
                    var mhc = new MhcTracerBeamSkill();
                    EquipSkillForPreview(mhc, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    mhc.Trigger();
                    _frame = 0;
                    _stage = 29;
                    return false;

                case 29:
                    // 29. MHC Tracer Beam Confocal Scanner Reticle
                    if (!Gate(ref _frame, 5))
                        return false;
                    CaptureScreenshot("skill_mhc_tracer_beam.png");

                    // Trigger Histamine Surge
                    ClearArenaForSkillPreview();
                    var hist = new HistamineSurgeSkill();
                    EquipSkillForPreview(hist, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(140, 0));
                    hist.Trigger();
                    _frame = 0;
                    _stage = 30;
                    return false;

                case 30:
                    // 30. Histamine Surge Degranulation Wave
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("skill_histamine_surge.png");

                    // Trigger Nitric Oxide Halo
                    ClearArenaForSkillPreview();
                    var no = new NitricOxideHaloSkill();
                    EquipSkillForPreview(no, 1);
                    no.Trigger();
                    _frame = 0;
                    _stage = 31;
                    return false;

                case 31:
                    // 31. Nitric Oxide Halo Cyan Gas Cloud
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("skill_nitric_oxide_halo.png");

                    // Trigger Interferon Wave
                    ClearArenaForSkillPreview();
                    var ifn = new InterferonWaveSkill();
                    EquipSkillForPreview(ifn, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(150, 0));
                    ifn.Trigger();
                    _frame = 0;
                    _stage = 32;
                    return false;

                case 32:
                    // 32. Interferon Wave Acoustic Pressure Ripple
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("skill_interferon_wave.png");

                    // Trigger Lysozyme Ricochet
                    ClearArenaForSkillPreview();
                    var lyso = new LysozymeRicochetSkill();
                    EquipSkillForPreview(lyso, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    lyso.Trigger();
                    _frame = 0;
                    _stage = 33;
                    return false;

                case 33:
                    // 33. Lysozyme Ricochet Globular Protein Capsule
                    if (!Gate(ref _frame, 8))
                        return false;
                    CaptureScreenshot("skill_lysozyme_ricochet.png");

                    // Trigger Pseudopod Lunge
                    ClearArenaForSkillPreview();
                    var lunge = new PseudopodLungeSkill();
                    EquipSkillForPreview(lunge, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(160, 0));
                    lunge.Trigger();
                    _frame = 0;
                    _stage = 34;
                    return false;

                case 34:
                    // 34. Pseudopod Lunge Blunt Fist Strike
                    if (!Gate(ref _frame, 5))
                        return false;
                    CaptureScreenshot("skill_pseudopod_lunge.png");

                    // Trigger Lysosomal Overload
                    ClearArenaForSkillPreview();
                    var lysoOverload = new LysosomalOverloadSkill();
                    EquipSkillForPreview(lysoOverload, 1);
                    SpawnEnemy<TbEnemy>(new Vector2(150, 0));
                    lysoOverload.Trigger();
                    _frame = 0;
                    _stage = 35;
                    return false;

                case 35:
                    // 35. Lysosomal Overload Hydrolytic Acid Pool
                    if (!Gate(ref _frame, 8))
                        return false;
                    CaptureScreenshot("skill_lysosomal_overload.png");

                    Finish(true, "FULL GAME VISUAL PREVIEW & ALL 18 ACTIVE SKILLS");
                    return true;

                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestFullVisualPreview threw: ", ex);
            FreeMainMenu();
            ClearRootPopups();
            Quit(1);
            return true;
        }
    }

    private void OpenMainMenu()
    {
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        _menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(_menu);
    }

    private void FreeMainMenu()
    {
        if (_menu != null && IsInstanceValid(_menu))
        {
            if (_menu.GetParent() != null)
                _menu.GetParent().RemoveChild(_menu);
            _menu.Free();
            _menu = null;
        }
    }

    private void ClearRootPopups()
    {
        if (Root == null)
            return;
        var tree = Root.GetTree();
        if (tree != null)
            PauseManager.Clear(tree);
        foreach (var child in Root.GetChildren())
        {
            if (child is PanelContainer || child is AchievementToast || child is UpgradeModal)
            {
                Root.RemoveChild(child);
                child.Free();
            }
        }
        _upgradeModal = null;
    }

    private void ShowSampleToast()
    {
        var bannerScene = AssetLoader.Load<PackedScene>("res://scenes/ui/toast_banner.tscn");
        var banner = bannerScene.Instantiate<PanelContainer>();
        banner.Name = "PreviewToast";
        Root.AddChild(banner);
        var icon = banner.GetNodeOrNull<TextureRect>("HBox/Icon");
        if (icon != null)
            icon.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        var title = banner.GetNodeOrNull<Label>("HBox/VBox/Title");
        if (title != null)
            title.Text = "Preview toast notification";
        var desc = banner.GetNodeOrNull<Label>("HBox/VBox/Desc");
        if (desc != null)
            desc.Text = "Immunological defense system online";
        banner.Visible = true;
    }

    private void ShowSampleAchievementToast()
    {
        ClearRootPopups();
        var dummyAch = new Godot.Collections.Dictionary
        {
            { "id", "toast_preview" },
            { "image_path", AssetPaths.SkillIcon("actin") },
            { "title_key", "ACH_ENGULF_20_TITLE" },
            { "desc_key", "ACH_ENGULF_20_DESC" },
            { "reward_cell", "ctl" }
        };
        AchievementToast.ShowToast(Root, dummyAch);
    }

    private void ShowSampleUpgradeModal()
    {
        var modalScene = AssetLoader.Load<PackedScene>("res://scenes/ui/upgrade_modal.tscn");
        _upgradeModal = modalScene.Instantiate<UpgradeModal>();
        Root.AddChild(_upgradeModal);

        var choices = new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            new Godot.Collections.Dictionary
            {
                { "type", "active_skill" },
                { "id", "pseudopod_lunge" },
                { "name", "SKILL_LUNGE_NAME" },
                { "description", "SKILL_LUNGE_DESC" },
                { "image_path", AssetPaths.SkillIcon("pseudopod_lunge") }
            },
            new Godot.Collections.Dictionary
            {
                { "type", "active_skill" },
                { "id", "lysosomal_overload" },
                { "name", "SKILL_LYSOSOME_NAME" },
                { "description", "SKILL_LYSOSOME_DESC" },
                { "image_path", AssetPaths.SkillIcon("lysosomal_overload") }
            },
            new Godot.Collections.Dictionary
            {
                { "type", "active_skill" },
                { "id", "ros_torrent" },
                { "name", "SKILL_ROS_NAME" },
                { "description", "SKILL_ROS_DESC" },
                { "image_path", AssetPaths.SkillIcon("ros_torrent") }
            }
        };
        _upgradeModal.ShowChoices(choices);
    }

    private static void SetupCard(Node cardNode, string title, string badge, string desc, string? iconPath = null)
    {
        if (!string.IsNullOrEmpty(iconPath))
        {
            var iconTex = cardNode.GetNodeOrNull<TextureRect>("VBox/IconTexture");
            if (iconTex != null)
                iconTex.Texture = AssetLoader.TryLoad<Texture2D>(iconPath);
        }
        var titleLbl = cardNode.GetNodeOrNull<Label>("VBox/TitleLabel");
        if (titleLbl != null) titleLbl.Text = title;
        var badgeLbl = cardNode.GetNodeOrNull<Label>("VBox/BadgeLabel");
        if (badgeLbl != null) badgeLbl.Text = badge;
        var descLbl = cardNode.GetNodeOrNull<Label>("VBox/DescLabel");
        if (descLbl != null) descLbl.Text = desc;
    }

    private Main? _main;

    private void SetupCombatArena()
    {
        _main = InstantiateMain();
        ClearArenaEntities(_main);

        _player = _main.GetNodeOrNull<Macrophage>("Macrophage");
        if (_player == null)
            return;

        _player.Stats!.SetBase("crit_chance", 0.0f);
        _player.Stats.SetBase("block", 0.0f);
        _player.Stats.SetBase("evasion", 0.0f);
        _player.Stats.SetBase("max_health", 999999.0f);
        _player.Health = 999999.0f;
        _player.ExpToNextLevel = 9999999;

        var hud = _main.GetNodeOrNull<Hud>("HUD");
        if (hud != null)
        {
            hud.ConnectPlayer(_player);
        }
        _player.TakeDamage(25.0f);
    }

    private T SpawnEnemy<T>(Vector2 offset) where T : BaseEnemy, new()
    {
        var enemy = new T
        {
            GlobalPosition = (_player != null ? _player.GlobalPosition : Vector2.Zero) + offset
        };
        if (_main != null)
            _main.AddChild(enemy);
        else
            Root.AddChild(enemy);
        return enemy;
    }

    private void ClearArenaForSkillPreview()
    {
        if (Root != null)
        {
            var tree = Root.GetTree();
            if (tree != null)
                PauseManager.Clear(tree);

            foreach (var child in Root.GetChildren())
            {
                if (child is AchievementToast || child is UpgradeModal)
                {
                    Root.RemoveChild(child);
                    child.QueueFree();
                }
            }
        }
        if (_main == null || _player == null)
            return;

        var toRemove = new List<Node>();
        foreach (var child in _main.GetChildren())
        {
            if (child is BaseEnemy || child is RosJet || child is AntibodyMissile
                || child is PerforinLanceSkill.LanceBeamVisual || child is PerforinLanceSkill.PoreDecal
                || child is LysosomalOverloadSkill.LysosomeAcidPool || child is PhagolysosomeVentSkill.AcidPuddle
                || child is ExosomeSingularitySkill.SingularityVortex || child is NucleaseBladesSkill.BladesCanvas
                || child is MhcTracerBeamSkill.TracerVisual || child is ProInflammatoryArcSkill.ArcLightningVisual
                || child is InterferonWaveSkill.WaveVisual || child is HistamineSurgeSkill.SurgeVisual
                || child is ComplementCascadeSkill.MacAssemblyMine || child is ComplementCascadeSkill.MacDetonationBlast
                || child is GranzymeDetonationSkill.ApoptosisBurstVisual || child is GranzymeDetonationSkill.ApoptosisMarker
                || child is DefensinBarbsSkill.BarbProjectile || child is LysozymeRicochetSkill.RicochetVesicle
                || child is PseudopodChainVisual)
            {
                toRemove.Add(child);
            }
        }
        foreach (var n in toRemove)
        {
            _main.RemoveChild(n);
            n.QueueFree();
        }

        if (_player.CellSkillManager != null)
        {
            for (int i = 0; i < SkillManager.MaxActiveSlots; i++)
            {
                var s = _player.CellSkillManager.ActiveSlots[i];
                if (s != null && GodotObject.IsInstanceValid(s))
                {
                    s.IsInnate = false;
                    s.CooldownTimer = 999999.0f;
                    _player.CellSkillManager.RemoveChild(s);
                    s.QueueFree();
                    _player.CellSkillManager.ActiveSlots[i] = null;
                }
            }
        }

        _player.Velocity = Vector2.Zero;
        _player.ExpToNextLevel = 9999999;
        _player.CurrentExp = 0;
    }

    private void EquipSkillForPreview(BaseSkill skill, int slot = 1)
    {
        skill.IsInnate = false;
        skill.CooldownTimer = 999999.0f;
        _player!.CellSkillManager!.EquipActive(skill, slot);
    }

    private static void ClearArenaEntities(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is BaseEnemy || child is DormantToxinVesicle || child is BioHazardArea)
                child.Free();
            else
                ClearArenaEntities(child);
        }
    }

    private static List<T> CollectNodes<T>(Node root) where T : Node
    {
        var found = new List<T>();
        CollectNodesRecursive(root, found);
        return found;
    }

    private static void CollectNodesRecursive<T>(Node node, List<T> found) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T typed)
                found.Add(typed);
            CollectNodesRecursive(child, found);
        }
    }
}
