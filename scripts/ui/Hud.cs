using Godot;
using Godot.Collections;
using System;
using System.Text;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.UI;

public partial class Hud : CanvasLayer
{
    private static PackedScene? _upgradeModalScene;
    public static PackedScene UpgradeModalScene => _upgradeModalScene ??= GD.Load<PackedScene>("res://scenes/ui/upgrade_modal.tscn");

    public PanelContainer? AchievementBanner { get; set; }
    public Label? AchBannerTitle { get; set; }
    public Label? AchBannerDesc { get; set; }
    public Label? AchBannerIcon { get; set; }
    public Tween? AchTween { get; set; }

    public Label? LevelLabel { get; set; }
    public Label? TitleLabel { get; set; }
    public Label? TimerLabel { get; set; }
    public Label? MapLabel { get; set; }
    public Label? HpTitleLabel { get; set; }
    public ProgressBar? HpBar { get; set; }
    public Label? HpLabel { get; set; }
    public Label? ExpTitleLabel { get; set; }
    public ProgressBar? ExpBar { get; set; }
    public Label? ExpLabel { get; set; }
    public Label? SizeLabel { get; set; }
    public Label? CountLabel { get; set; }
    public Label? SpeedLabel { get; set; }

    // Backward compatibility aliases
    public ProgressBar? AtpBar { get => ExpBar; set { } }
    public Label? AtpLabel { get => ExpLabel; set { } }

    // Survivor-like Bottom HUD & Vignette Nodes
    public ProgressBar? BottomExpBar { get; set; }
    public ProgressBar? TopExpBar { get => BottomExpBar; set => BottomExpBar = value; }
    public PanelContainer? TopCenterCapsule { get; set; }
    public Label? KillLabel { get; set; }
    public ColorRect? VignetteRect { get; set; }
    public Label? BuffTag { get; set; }
    public PanelContainer? SkillContainer { get; set; }

    public PanelContainer? PauseModal { get; set; }
    public Label? PauseTitle { get; set; }
    public Button? ResumeBtn { get; set; }
    public Button? SettingsBtn { get; set; }
    public Button? ManualBtn { get; set; }
    public Button? RestartBtn { get; set; }
    public Button? MenuBtn { get; set; }

    public CodexModal? CellCodexModal { get; set; }
    public SettingsModal? CellSettingsModal { get; set; }

    // --- First-run micro-cues (docs/tutorial.md §2) ---
    public TutorialOverlay? TutorialOverlayNode { get; set; }
    public const float MoveCueSeconds = 5.0f;
    public const float SqueezeHintSeconds = 6.0f;
    public const int SqueezeHintNearbyThreshold = 15;
    public const float SqueezeHintNearbyRadius = 300.0f;

    private float _moveCueTimer = 0.0f;
    private float _squeezeHintTimer = 0.0f;
    private bool _squeezeHintShown = false;
    private float _nearbyCheckTimer = 0.0f;
    private bool _firstLevelUpCuePlayed = false;
    private Tween? _bulletTimeTween = null;

    /// <summary>True once the first level-up bullet-time cue has fired this run.</summary>
    public bool FirstLevelUpCuePlayed => _firstLevelUpCuePlayed;

    /// <summary>True while the 0.5s bullet-time transition is running.</summary>
    public bool IsBulletTimeActive => _bulletTimeTween != null;

    /// <summary>True once the Squeeze Mode hint has been shown (once per run).</summary>
    public bool SqueezeHintShownOnce => _squeezeHintShown;

    /// <summary>Bullet-time ease duration for the first level-up cue (docs: 0.5s).</summary>
    public float LevelUpBulletTimeSeconds { get; set; } = 0.5f;

    /// <summary>When true the first level-up opens the draft immediately (headless tests).</summary>
    public bool SkipLevelUpBulletTime { get; set; } = false;

    // Skill Bar & Tooltip Nodes
    public Label? SkillTitleLbl { get; set; }
    public GridContainer? SlotsContainer { get; set; }

    public PanelContainer? SkillTooltip { get; set; }
    public Label? TooltipIcon { get; set; }
    public Label? TooltipTitle { get; set; }
    public Label? TooltipBadge { get; set; }
    public Label? TooltipStats { get; set; }
    public Label? TooltipDesc { get; set; }
    public Label? TooltipBio { get; set; }

    public PanelContainer? TreeOverlayPanel { get; set; }
    public RichTextLabel? TreeOverlayText { get; set; }

    public int HoveredSlotIdx { get; set; } = -1;

    // Cached last stats for re-rendering upon language change
    public float LastHealth { get; set; } = 100.0f;
    public float LastMaxHealth { get; set; } = 100.0f;
    public float LastCurrentExp { get; set; } = 0.0f;
    public float LastExpToNext { get; set; } = 30.0f;
    public int LastLevel { get; set; } = 1;
    public float LastRadiusRatio { get; set; } = 1.0f;
    public int LastDigestedCount { get; set; } = 0;
    public float LastSpeed { get; set; } = 230.0f;
    public float SurvivalTime { get; set; } = 0.0f;

    // Survival goal shown next to the timer (0 hides it).
    public float GoalSeconds { get; set; } = 0.0f;

    /// <summary>
    /// Endless overdrive: once the standard goal is crossed the timer turns into a
    /// burning dark-gold fluorescence and counts on without a cap (docs/endgame.md §3.1).
    /// </summary>
    public bool EndlessMode { get; set; } = false;

    // Blocks tree/pause hotkeys once the settlement screen is up.
    public bool PauseInputSuppressed { get; set; } = false;

    public Node2D? PlayerRef { get; set; } = null;
    public UpgradeModal? CellUpgradeModal { get; set; } = null;

    private Callable _langCallback;
    private Callable _achUnlockCallback;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        LevelLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HeaderHBox/LevelBadge/LevelLabel");
        TitleLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HeaderHBox/TitleLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/TitleLabel");
        TopCenterCapsule = GetNodeOrNull<PanelContainer>("TopCenterCapsule");
        TimerLabel = GetNodeOrNull<Label>("TopCenterCapsule/HBox/TimerLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HeaderHBox/TimerLabel");
        KillLabel = GetNodeOrNull<Label>("TopCenterCapsule/HBox/KillLabel");
        MapLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/MapLabel");
        BuffTag = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/BuffContainer/BuffTag");

        BottomExpBar = GetNodeOrNull<ProgressBar>("BottomExpBar")
            ?? GetNodeOrNull<ProgressBar>("TopExpBar");
        VignetteRect = GetNodeOrNull<ColorRect>("CriticalHpVignette");
        SkillContainer = GetNodeOrNull<PanelContainer>("SkillContainer");

        HpTitleLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPTopHBox/HPTitleLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/HPTitleLabel");
        HpBar = GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPBar")
            ?? GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/HPBar");
        HpLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPTopHBox/HPLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/HPLabel");

        ExpTitleLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/EXPContainer/EXPTopHBox/EXPTitleLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/EXPTitleLabel");
        ExpBar = GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/EXPContainer/EXPBar")
            ?? GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPBar")
            ?? GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/EXPBar");
        ExpLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/EXPContainer/EXPTopHBox/EXPLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/EXPLabel");

        SizeLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/FooterHBox/SizeLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/SizeLabel");
        CountLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/FooterHBox/CountLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/CountLabel")
            ?? GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/LegacyBars/CountLabel");
        SpeedLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/FooterHBox/SpeedLabel");

        PauseModal = GetNodeOrNull<PanelContainer>("PauseModal");
        PauseTitle = GetNodeOrNull<Label>("PauseModal/VBox/Title");
        ResumeBtn = GetNodeOrNull<Button>("PauseModal/VBox/ResumeButton");
        SettingsBtn = GetNodeOrNull<Button>("PauseModal/VBox/SettingsButton");
        ManualBtn = GetNodeOrNull<Button>("PauseModal/VBox/ManualButton");
        RestartBtn = GetNodeOrNull<Button>("PauseModal/VBox/RestartButton");
        MenuBtn = GetNodeOrNull<Button>("PauseModal/VBox/MenuButton");

        CellCodexModal = GetNodeOrNull<CodexModal>("CodexModal");
        CellSettingsModal = GetNodeOrNull<SettingsModal>("SettingsModal");

        SkillTitleLbl = GetNodeOrNull<Label>("SkillContainer/VBox/TitleLabel");
        SlotsContainer = GetNodeOrNull<GridContainer>("SkillContainer/VBox/SlotsContainer");

        SkillTooltip = GetNodeOrNull<PanelContainer>("SkillTooltip");
        TooltipIcon = GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipIcon");
        TooltipTitle = GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipTitle");
        TooltipBadge = GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipBadge");
        TooltipStats = GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipStats");
        TooltipDesc = GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipDesc");
        TooltipBio = GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipBio");

        if (PauseModal != null) PauseModal.Visible = false;

        if (HasNode("UpgradeModal"))
        {
            CellUpgradeModal = GetNodeOrNull<UpgradeModal>("UpgradeModal");
        }
        else
        {
            CellUpgradeModal = UpgradeModalScene.Instantiate<UpgradeModal>();
            AddChild(CellUpgradeModal);
        }

        if (ResumeBtn != null) ResumeBtn.Pressed += ResumeGame;
        if (SettingsBtn != null) SettingsBtn.Pressed += OnSettingsPressed;
        if (ManualBtn != null) ManualBtn.Pressed += OnManualPressed;
        if (RestartBtn != null) RestartBtn.Pressed += OnRestartPressed;
        if (MenuBtn != null) MenuBtn.Pressed += OnMenuPressed;

        SetupSlotHoverSignals();
        SetupAchievementBanner();
        SetupTreeOverlay();
        SetupTutorialOverlay();

        if (CellUpgradeModal != null)
            CellUpgradeModal.ChoiceApplied += _ => RestoreTimeScaleAfterLevelUp();

        _achUnlockCallback = Callable.From((string a, Dictionary b) => OnAchievementUnlocked(a, b));
        AchievementManager.AddUnlockListener(_achUnlockCallback);

        _langCallback = Callable.From((string _) => UpdateLocalizedTexts());
        GameManager.AddLanguageListener(_langCallback);

        UpdateLocalizedTexts();
    }

    public override void _ExitTree()
    {
        AchievementManager.RemoveUnlockListener(_achUnlockCallback);
        GameManager.RemoveLanguageListener(_langCallback);
    }

    /// <summary>Burning dark-gold fluorescence used by the endless overdrive timer.</summary>
    private static Color BurningGoldFluorescence(float time)
    {
        float flicker = 0.85f + 0.15f * Mathf.Sin(time * 6.0f);
        float ember = 0.75f + 0.25f * Mathf.Sin(time * 2.3f + 1.7f);
        return new Color(1.0f * flicker, (0.52f + 0.16f * ember) * flicker, 0.06f, 1.0f);
    }

    public override void _Process(double delta)
    {
        if (!GetTree().Paused)
        {
            SurvivalTime += (float)delta;
            int minutes = (int)(SurvivalTime / 60.0f);
            int seconds = (int)(SurvivalTime % 60.0f);
            if (TimerLabel != null)
            {
                string timerText = $"⏱️ {minutes:D2}:{seconds:D2}";
                bool overdrive = EndlessMode && GoalSeconds > 0.0f && SurvivalTime >= GoalSeconds;
                if (GoalSeconds > 0.0f)
                {
                    if (overdrive)
                    {
                        timerText += " / ∞";
                    }
                    else
                    {
                        int goalMinutes = (int)(GoalSeconds / 60.0f);
                        int goalSeconds = (int)(GoalSeconds % 60.0f);
                        timerText += $" / {goalMinutes:D2}:{goalSeconds:D2}";
                    }
                }
                TimerLabel.Text = timerText;
                TimerLabel.Modulate = overdrive
                    ? BurningGoldFluorescence(SurvivalTime)
                    : Colors.White;
            }
        }

        // Low-HP Vignette Pulse (<30% HP)
        if (VignetteRect != null && VignetteRect.Material is ShaderMaterial vignetteMat)
        {
            float hpRatio = LastMaxHealth > 0 ? LastHealth / LastMaxHealth : 1.0f;
            if (hpRatio < 0.30f && LastHealth > 0.0f)
            {
                float dangerFactor = 1.0f - (hpRatio / 0.30f);
                float pulse = 0.45f + 0.35f * Mathf.Sin(SurvivalTime * 7.5f);
                float intensity = Mathf.Clamp(dangerFactor * pulse, 0.0f, 1.0f);
                vignetteMat.SetShaderParameter("pulse_intensity", intensity);
            }
            else
            {
                vignetteMat.SetShaderParameter("pulse_intensity", 0.0f);
            }
        }

        // Dynamic Transparency for Skill Bar (35% in combat, 100% on hover/pause)
        if (SkillContainer != null)
        {
            bool isInteracting = HoveredSlotIdx >= 0 || GetTree().Paused;
            float targetAlpha = isInteracting ? 1.0f : 0.35f;
            Color c = SkillContainer.Modulate;
            float newA = Mathf.MoveToward(c.A, targetAlpha, (float)delta * 3.0f);
            SkillContainer.Modulate = new Color(c.R, c.G, c.B, newA);
        }

        UpdateBuffStatus();
        UpdateSkillSlots();
        UpdateTutorialCues((float)delta);
    }

    /// <summary>Creates the screen-space cue overlay behind the HUD panels.</summary>
    private void SetupTutorialOverlay()
    {
        TutorialOverlayNode = new TutorialOverlay { Name = "TutorialOverlay" };
        AddChild(TutorialOverlayNode);
    }

    /// <summary>
    /// Drives the non-intrusive micro-cues (docs/tutorial.md §2): the opening
    /// WASD ring, the one-shot Squeeze hint and the fluid-shear arrow trails.
    /// </summary>
    private void UpdateTutorialCues(float delta)
    {
        if (TutorialOverlayNode == null)
            return;

        TutorialOverlayNode.PlayerRef = PlayerRef;

        // Cue 1: opening 5 seconds breathing ring
        if (_moveCueTimer > 0.0f)
            _moveCueTimer = Mathf.Max(0.0f, _moveCueTimer - delta);
        TutorialOverlayNode.ShowMoveCue = _moveCueTimer > 0.0f && PlayerRef != null;
        TutorialOverlayNode.MoveCueProgress = _moveCueTimer / MoveCueSeconds;

        // Cue 3: first damage or >15 nearby pathogens -> floating squeeze hint
        if (!_squeezeHintShown)
        {
            _nearbyCheckTimer -= delta;
            if (_nearbyCheckTimer <= 0.0f)
            {
                _nearbyCheckTimer = 0.25f;
                bool hurt = PlayerRef is BaseCell hurtCell && hurtCell.HasTakenDamage;
                if (hurt || CountNearbyPathogens(SqueezeHintNearbyRadius) > SqueezeHintNearbyThreshold)
                    ShowSqueezeHint();
            }
        }

        if (_squeezeHintTimer > 0.0f)
            _squeezeHintTimer = Mathf.Max(0.0f, _squeezeHintTimer - delta);

        TutorialOverlayNode.ShowSqueezeHint = _squeezeHintTimer > 0.0f;
        TutorialOverlayNode.SqueezeHintText = _squeezeHintShown ? LocalizedSqueezeHint() : "";
        TutorialOverlayNode.SqueezeHintAlpha = Mathf.Min(1.0f, _squeezeHintTimer);

        // Cue 5: fluid-shear arrow trails while an environmental field is active
        if (GetParent() is Main main)
        {
            TutorialOverlayNode.FluidVector = main.CurrentFluidVector;
            TutorialOverlayNode.FluidFieldActive = main.CurrentFluidVector.LengthSquared() > 0.01f;
        }
        else
        {
            TutorialOverlayNode.FluidFieldActive = false;
        }
    }

    public void ShowSqueezeHint()
    {
        if (_squeezeHintShown)
            return;

        _squeezeHintShown = true;
        _squeezeHintTimer = SqueezeHintSeconds;
        GD.Print("[Tutorial] Squeeze Mode hint shown.");
    }

    /// <summary>Resets the per-run micro-cue state and restarts the move cue window.</summary>
    public void ResetTutorialCues()
    {
        _moveCueTimer = MoveCueSeconds;
        _squeezeHintTimer = 0.0f;
        _squeezeHintShown = false;
        _nearbyCheckTimer = 0.0f;
        _firstLevelUpCuePlayed = false;
        Engine.TimeScale = 1.0;
    }

    private static string LocalizedSqueezeHint()
    {
        bool gamepad = Input.GetConnectedJoypads().Count > 0;
        return TranslationServer.Translate(gamepad ? "HUD_SQUEEZE_HINT_PAD" : "HUD_SQUEEZE_HINT");
    }

    private int CountNearbyPathogens(float radius)
    {
        if (PlayerRef == null)
            return 0;

        float radiusSq = radius * radius;
        int count = 0;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!GodotObject.IsInstanceValid(enemy))
                continue;
            if (enemy.GlobalPosition.DistanceSquaredTo(PlayerRef.GlobalPosition) <= radiusSq)
                count++;
        }
        return count;
    }

    private void UpdateBuffStatus()
    {
        if (BuffTag == null) return;

        if (PlayerRef is BaseCell bc && bc.TbBurnTimer > 0.0f)
        {
            BuffTag.Visible = true;
            BuffTag.Text = TextFormatter.Format(Tr("HUD_TB_DEBUFF"), bc.TbBurnTimer);
            BuffTag.Modulate = new Color(1.0f, 0.35f, 0.35f, 0.95f);
        }
        else if (PlayerRef is BaseCell bc2 && bc2.InvertControlsTimer > 0.0f)
        {
            BuffTag.Visible = true;
            BuffTag.Text = $"🌀 {Tr("STATUS_CONFUSION")}: {bc2.InvertControlsTimer:F1}s";
            BuffTag.Modulate = new Color(0.85f, 0.45f, 1.0f, 0.95f);
        }
        else if (PlayerRef is BaseCell bc3 && bc3.SlowTimer > 0.0f)
        {
            BuffTag.Visible = true;
            BuffTag.Text = $"🐌 {Tr("STATUS_SLOW")}: {bc3.SlowTimer:F1}s";
            BuffTag.Modulate = new Color(0.4f, 0.8f, 0.5f, 0.95f);
        }
        else
        {
            BuffTag.Visible = false;
            BuffTag.Text = "";
        }
    }

    public void UpdateLocalizedTexts()
    {
        if (TitleLabel != null) TitleLabel.Text = Tr("HUD_TITLE");

        var mapInfo = GameManager.GetMapInfo(GameManager.SelectedMap);
        if (mapInfo.TryGetValue("name", out var mapName) && MapLabel != null)
        {
            MapLabel.Text = "📍 " + Tr("HUD_BATTLEFIELD") + mapName.AsString();
        }

        if (LevelLabel != null) LevelLabel.Text = TextFormatter.Format(Tr("HUD_LEVEL"), LastLevel);
        if (HpTitleLabel != null) HpTitleLabel.Text = Tr("HUD_HP_TITLE");
        if (HpLabel != null) HpLabel.Text = TextFormatter.Format(Tr("HUD_HP_VAL"), (int)LastHealth, (int)LastMaxHealth);
        if (ExpTitleLabel != null) ExpTitleLabel.Text = Tr("HUD_EXP_TITLE");
        UpdateExpDisplay();

        if (CountLabel != null) CountLabel.Text = TextFormatter.Format(Tr("HUD_ELIMINATED"), LastDigestedCount);
        if (KillLabel != null) KillLabel.Text = TextFormatter.Format(Tr("HUD_KILL_COUNT"), LastDigestedCount);
        if (SizeLabel != null) SizeLabel.Text = TextFormatter.Format(Tr("HUD_AREA"), LastRadiusRatio);
        if (SpeedLabel != null) SpeedLabel.Text = TextFormatter.Format(Tr("HUD_SPEED"), (int)LastSpeed);
        if (SkillTitleLbl != null) SkillTitleLbl.Text = Tr("SKILL_BAR_DUAL_TITLE");

        UpdateBuffStatus();
        RefreshTreeOverlay();

        if (PauseTitle != null) PauseTitle.Text = Tr("PAUSE_TITLE");
        if (ResumeBtn != null) ResumeBtn.Text = Tr("PAUSE_RESUME");
        if (SettingsBtn != null) SettingsBtn.Text = Tr("BTN_SETTINGS");
        if (ManualBtn != null) ManualBtn.Text = Tr("PAUSE_MANUAL");
        if (RestartBtn != null) RestartBtn.Text = Tr("PAUSE_RESTART");
        if (MenuBtn != null) MenuBtn.Text = Tr("PAUSE_MENU");

        if (HoveredSlotIdx >= 0 && SkillTooltip != null && GodotObject.IsInstanceValid(SkillTooltip) && SkillTooltip.Visible)
        {
            RefreshTooltipContent(HoveredSlotIdx);
        }

        UpdateSkillSlots();
    }

    public void UpdateExpDisplay()
    {
        if (ExpBar != null)
        {
            ExpBar.MaxValue = Mathf.Max(1.0f, LastExpToNext);
            ExpBar.Value = LastCurrentExp;
        }
        if (BottomExpBar != null)
        {
            BottomExpBar.MaxValue = Mathf.Max(1.0f, LastExpToNext);
            BottomExpBar.Value = LastCurrentExp;
        }
        int percent = (int)((LastCurrentExp / Mathf.Max(1.0f, LastExpToNext)) * 100.0f);
        if (ExpLabel != null)
        {
            ExpLabel.Text = TextFormatter.Format(Tr("HUD_EXP_VAL"), (int)LastCurrentExp, (int)LastExpToNext, percent);
        }
        if (LevelLabel != null)
        {
            LevelLabel.Text = TextFormatter.Format(Tr("HUD_LEVEL"), LastLevel);
        }
    }

    public void UpdateSkillSlots()
    {
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
        {
            PlayerRef = (Node2D)GetTree().GetFirstNodeInGroup("player");
            if (PlayerRef == null)
                return;
            if (PlayerRef.HasSignal("level_up"))
            {
                var callable = Callable.From((int lvl) => OnPlayerLevelUp(lvl));
                if (!PlayerRef.IsConnected("level_up", callable))
                {
                    PlayerRef.Connect("level_up", callable);
                }
            }
        }

        Array<Dictionary> skillsData;
        var smNode = PlayerRef.GetNodeOrNull<Node>("SkillManager");
        if (smNode is SkillManager csharpSm)
        {
            skillsData = csharpSm.GetAllUiData();
        }
        else if (smNode != null && smNode.HasMethod("GetAllUiData"))
        {
            skillsData = smNode.Call("GetAllUiData").AsGodotArray<Dictionary>();
        }
        else if (smNode != null && smNode.HasMethod("get_all_ui_data"))
        {
            skillsData = smNode.Call("get_all_ui_data").AsGodotArray<Dictionary>();
        }
        else
        {
            return;
        }

        if (SlotsContainer == null)
            return;

        var slotChildren = SlotsContainer.GetChildren();
        int count = Mathf.Min(slotChildren.Count, skillsData.Count);

        for (int i = 0; i < count; i++)
        {
            var slotCard = slotChildren[i];
            var data = skillsData[i];

            var iconLbl = slotCard.GetNodeOrNull<Label>("IconLabel");
            var badgeLbl = slotCard.GetNodeOrNull<Label>("BadgeLabel");
            var cdOverlay = slotCard.GetNodeOrNull<ProgressBar>("CooldownBar");

            string id = data.TryGetValue("id", out var idVal) ? idVal.AsString() : "";
            if (!string.IsNullOrEmpty(id))
            {
                if (iconLbl != null)
                {
                    iconLbl.Text = data.TryGetValue("icon", out var icVal) ? icVal.AsString() : "";
                    iconLbl.Modulate = new Color(1, 1, 1, 1);
                }
                if (badgeLbl != null)
                {
                    bool isInnate = data.TryGetValue("is_innate", out var innVal) && innVal.AsBool();
                    bool isPassive = data.TryGetValue("is_passive", out var passVal) && passVal.AsBool();
                    int level = data.TryGetValue("level", out var lvVal) ? lvVal.AsInt32() : 1;

                    if (isInnate)
                    {
                        badgeLbl.Text = Tr("SKILL_INNATE_TAG");
                        badgeLbl.Modulate = new Color(0.4f, 0.95f, 0.8f);
                    }
                    else if (isPassive || i >= 5)
                    {
                        badgeLbl.Text = TextFormatter.Format(Tr("SKILL_LV"), level);
                        badgeLbl.Modulate = new Color(0.8f, 0.6f, 1.0f);
                    }
                    else
                    {
                        badgeLbl.Text = TextFormatter.Format(Tr("SKILL_LV"), level);
                        badgeLbl.Modulate = new Color(1.0f, 0.9f, 0.3f);
                    }
                }
                if (cdOverlay != null)
                {
                    bool isPassive = data.TryGetValue("is_passive", out var passVal) && passVal.AsBool();
                    float cdRatio = data.TryGetValue("cooldown_ratio", out var cdrVal) ? cdrVal.AsSingle() : 0.0f;
                    bool hasCd = !isPassive && cdRatio > 0.0f;
                    cdOverlay.Visible = hasCd;
                    cdOverlay.Value = cdRatio;
                }
            }
            else
            {
                // Empty Slot
                if (iconLbl != null)
                {
                    iconLbl.Text = "+";
                    if (i >= 5)
                        iconLbl.Modulate = new Color(0.65f, 0.55f, 0.8f, 0.5f);
                    else
                        iconLbl.Modulate = new Color(0.4f, 0.5f, 0.6f, 0.6f);
                }
                if (badgeLbl != null)
                {
                    badgeLbl.Text = "";
                }
                if (cdOverlay != null)
                {
                    cdOverlay.Visible = false;
                }
            }
        }
    }

    private void OnRestartPressed()
    {
        GameManager.RestartGame(GetTree());
    }

    private void OnMenuPressed()
    {
        GameManager.GoToMenu(GetTree());
    }

    public void OnManualPressed()
    {
        CellCodexModal?.OpenCodex(0);
    }

    private void OnSettingsPressed()
    {
        CellSettingsModal?.OpenSettings(0);
    }

    private void SetupSlotHoverSignals()
    {
        if (SlotsContainer == null)
            return;

        var slotChildren = SlotsContainer.GetChildren();
        for (int i = 0; i < slotChildren.Count; i++)
        {
            if (slotChildren[i] is not Control card)
                continue;

            card.MouseFilter = Control.MouseFilterEnum.Stop;
            int idx = i;
            card.MouseEntered += () => OnSlotMouseEntered(idx, card);
            card.MouseExited += () => OnSlotMouseExited(idx);
        }
    }

    public void OnSlotMouseEntered(int slotIdx, Control card)
    {
        HoveredSlotIdx = slotIdx;
        RefreshTooltipContent(slotIdx);

        if (SkillTooltip != null)
        {
            Rect2 cardRect = card.GetGlobalRect();
            Vector2 vpSize = GetViewport().GetVisibleRect().Size;
            float targetX = Mathf.Clamp(cardRect.GetCenter().X - 150.0f, 10.0f, vpSize.X - 310.0f);
            float targetY = cardRect.Position.Y - SkillTooltip.Size.Y - 10.0f;
            SkillTooltip.GlobalPosition = new Vector2(targetX, targetY);
            SkillTooltip.Visible = true;
        }
    }

    public void OnSlotMouseExited(int slotIdx)
    {
        if (HoveredSlotIdx == slotIdx)
        {
            HoveredSlotIdx = -1;
            if (SkillTooltip != null)
                SkillTooltip.Visible = false;
        }
    }

    private void RefreshTooltipContent(int slotIdx)
    {
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
        {
            PlayerRef = (Node2D)GetTree().GetFirstNodeInGroup("player");
            if (PlayerRef == null)
                return;
        }

        Array<Dictionary> skillsData;
        var smNode = PlayerRef.GetNodeOrNull<Node>("SkillManager");
        if (smNode is SkillManager csharpSm)
        {
            skillsData = csharpSm.GetAllUiData();
        }
        else if (smNode != null && smNode.HasMethod("GetAllUiData"))
        {
            skillsData = smNode.Call("GetAllUiData").AsGodotArray<Dictionary>();
        }
        else if (smNode != null && smNode.HasMethod("get_all_ui_data"))
        {
            skillsData = smNode.Call("get_all_ui_data").AsGodotArray<Dictionary>();
        }
        else
        {
            return;
        }

        if (slotIdx < 0 || slotIdx >= skillsData.Count)
            return;

        var data = skillsData[slotIdx];
        string id = data.TryGetValue("id", out var idVal) ? idVal.AsString() : "";

        if (!string.IsNullOrEmpty(id))
        {
            if (TooltipIcon != null) TooltipIcon.Text = data.TryGetValue("icon", out var icVal) ? icVal.AsString() : "";
            if (TooltipTitle != null) TooltipTitle.Text = data.TryGetValue("name", out var nmVal) ? nmVal.AsString() : "";

            string badgeText;
            Color badgeColor;
            string statsText;

            bool isInnate = data.TryGetValue("is_innate", out var innVal) && innVal.AsBool();
            bool isPassive = data.TryGetValue("is_passive", out var passVal) && passVal.AsBool();
            int level = data.TryGetValue("level", out var lvVal) ? lvVal.AsInt32() : 1;
            int maxLevel = data.TryGetValue("max_level", out var mlvVal) ? mlvVal.AsInt32() : 5;
            float cooldown = data.TryGetValue("cooldown_max", out var cdVal) ? cdVal.AsSingle() : 3.2f;
            string skillType = isInnate ? "innate" : isPassive ? "passive" : "active";
            UiBuilders.BuildSkillBadge(skillType, cooldown, level, maxLevel,
                out badgeText, out badgeColor, out statsText);

            if (TooltipBadge != null)
            {
                TooltipBadge.Text = badgeText;
                TooltipBadge.Modulate = badgeColor;
            }
            if (TooltipStats != null) TooltipStats.Text = statsText;
            if (TooltipDesc != null)
            {
                string desc = data.TryGetValue("description", out var dsVal) ? dsVal.AsString() : "";
                TooltipDesc.Text = Tr("CODEX_HEADER_TACTICAL") + "\n" + desc;
            }
            if (TooltipBio != null)
            {
                string bioText = data.TryGetValue("biochemistry", out var bioVal) ? bioVal.AsString() : "";
                TooltipBio.Text = Tr("CODEX_HEADER_BIO") + "\n" + bioText;
                TooltipBio.Visible = !string.IsNullOrEmpty(bioText);
            }
        }
        else
        {
            if (TooltipIcon != null) TooltipIcon.Text = "+";
            if (TooltipTitle != null) TooltipTitle.Text = Tr("TOOLTIP_EMPTY_TITLE");
            if (TooltipBadge != null)
            {
                if (slotIdx >= 5)
                {
                    TooltipBadge.Text = "[ " + Tr("TOOLTIP_TAG_PASSIVE") + " ]";
                    TooltipBadge.Modulate = new Color(0.75f, 0.55f, 1.0f);
                }
                else
                {
                    TooltipBadge.Text = "[ " + Tr("SKILL_EMPTY") + " ]";
                    TooltipBadge.Modulate = new Color(0.6f, 0.6f, 0.6f);
                }
            }
            if (TooltipStats != null)
            {
                TooltipStats.Text = slotIdx >= 5 ? "🧬 被动特质槽" : Tr("SKILL_BAR_TITLE");
            }
            if (TooltipDesc != null) TooltipDesc.Text = Tr("TOOLTIP_EMPTY_DESC");
            if (TooltipBio != null)
            {
                TooltipBio.Text = "";
                TooltipBio.Visible = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (PauseInputSuppressed)
            return;

        if (@event.IsActionPressed("toggle_tree") || (@event is InputEventKey treeKey && treeKey.Pressed && treeKey.Keycode == Key.C))
        {
            ToggleTreeOverlay();
            return;
        }

        if (@event.IsActionPressed("toggle_pause") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape))
        {
            if (CellSettingsModal != null && CellSettingsModal.Visible)
            {
                CellSettingsModal.CloseSettings();
                return;
            }
            if (CellCodexModal != null && CellCodexModal.Visible)
            {
                CellCodexModal.CloseCodex();
                return;
            }
            TogglePause();
        }
    }

    public void TogglePause()
    {
        bool paused = !GetTree().Paused;
        GetTree().Paused = paused;
        if (PauseModal != null) PauseModal.Visible = paused;
        if (!paused)
        {
            if (CellCodexModal != null) CellCodexModal.Visible = false;
            if (CellSettingsModal != null) CellSettingsModal.Visible = false;
        }
    }

    public void ResumeGame()
    {
        GetTree().Paused = false;
        if (PauseModal != null) PauseModal.Visible = false;
        if (CellCodexModal != null) CellCodexModal.Visible = false;
        if (CellSettingsModal != null) CellSettingsModal.Visible = false;
    }

    public void ConnectPlayer(Node2D player)
    {
        PlayerRef = player;
        ResetTutorialCues();
        if (player is BaseCell bc)
        {
            bc.StatsChanged += (h, mh, r) => OnPlayerStatsChanged(h, mh, r);
            bc.PathogenDigested += (p, atp) => OnPathogenDigested(p, atp);
            bc.LevelUp += (lvl) => OnPlayerLevelUp((int)lvl);
            bc.ExpChanged += (cur, max, lvl) => OnPlayerExpChanged(cur, max, lvl);

            LastHealth = bc.Health;
            LastMaxHealth = bc.Stats != null ? bc.Stats.GetStat("max_health") : bc.MaxHealth;
            LastCurrentExp = bc.CurrentExp;
            LastExpToNext = bc.ExpToNextLevel;
            LastLevel = bc.CurrentLevel;
            LastSpeed = bc.CurrentSpeed > 0 ? bc.CurrentSpeed : bc.BaseSpeed;
            LastRadiusRatio = bc.CurrentRadius / Mathf.Max(1.0f, bc.BaseRadius);
            LastDigestedCount = bc.DigestedCount;

            if (HpBar != null)
            {
                HpBar.MaxValue = LastMaxHealth;
                HpBar.Value = LastHealth;
            }
            if (HpLabel != null) HpLabel.Text = TextFormatter.Format(Tr("HUD_HP_VAL"), (int)LastHealth, (int)LastMaxHealth);
            UpdateExpDisplay();
            if (SizeLabel != null) SizeLabel.Text = TextFormatter.Format(Tr("HUD_AREA"), LastRadiusRatio);
            if (CountLabel != null) CountLabel.Text = TextFormatter.Format(Tr("HUD_ELIMINATED"), LastDigestedCount);
            if (KillLabel != null) KillLabel.Text = TextFormatter.Format(Tr("HUD_KILL_COUNT"), LastDigestedCount);
            if (SpeedLabel != null) SpeedLabel.Text = TextFormatter.Format(Tr("HUD_SPEED"), (int)LastSpeed);
        }
        else
        {
            if (player.HasSignal("StatsChanged"))
                player.Connect("StatsChanged", Callable.From((float h, float mh, float r) => OnPlayerStatsChanged(h, mh, r)));
            else if (player.HasSignal("stats_changed"))
                player.Connect("stats_changed", Callable.From((float h, float mh, float r) => OnPlayerStatsChanged(h, mh, r)));

            if (player.HasSignal("PathogenDigested"))
                player.Connect("PathogenDigested", Callable.From((Node2D p, float atp) => OnPathogenDigested(p, atp)));
            else if (player.HasSignal("pathogen_digested"))
                player.Connect("pathogen_digested", Callable.From((Node2D p, float atp) => OnPathogenDigested(p, atp)));

            if (player.HasSignal("LevelUp"))
                player.Connect("LevelUp", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));
            else if (player.HasSignal("level_up"))
                player.Connect("level_up", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));

            if (player.HasSignal("ExpChanged"))
                player.Connect("ExpChanged", Callable.From((float c, float m, int l) => OnPlayerExpChanged(c, m, l)));
            else if (player.HasSignal("exp_changed"))
                player.Connect("exp_changed", Callable.From((float c, float m, int l) => OnPlayerExpChanged(c, m, l)));
        }

        UpdateBuffStatus();
        UpdateSkillSlots();
        RefreshTreeOverlay();
    }

    private void OnPlayerExpChanged(float currentExp, float expToNext, int level)
    {
        LastCurrentExp = currentExp;
        LastExpToNext = expToNext;
        LastLevel = level;
        UpdateExpDisplay();
        RefreshTreeOverlay();
    }

    private void OnPlayerStatsChanged(float health, float maxHealth, float radiusRatio)
    {
        LastHealth = health;
        LastMaxHealth = maxHealth;
        LastRadiusRatio = radiusRatio;

        if (PlayerRef is BaseCell bc)
        {
            LastSpeed = bc.CurrentSpeed;
        }

        if (HpBar != null)
        {
            HpBar.MaxValue = maxHealth;
            HpBar.Value = health;
        }
        if (HpLabel != null) HpLabel.Text = TextFormatter.Format(Tr("HUD_HP_VAL"), (int)health, (int)maxHealth);

        if (SizeLabel != null) SizeLabel.Text = TextFormatter.Format(Tr("HUD_AREA"), radiusRatio);
        if (SpeedLabel != null) SpeedLabel.Text = TextFormatter.Format(Tr("HUD_SPEED"), (int)LastSpeed);
        RefreshTreeOverlay();
    }

    private void OnPathogenDigested(Node2D _enemy, float _atp)
    {
        var player = (Node2D)GetTree().GetFirstNodeInGroup("player");
        if (player != null)
        {
            var digProp = player.Get("digested_count");
            if (digProp.VariantType == Variant.Type.Int)
            {
                LastDigestedCount = digProp.AsInt32();
                if (CountLabel != null) CountLabel.Text = TextFormatter.Format(Tr("HUD_ELIMINATED"), LastDigestedCount);
                if (KillLabel != null) KillLabel.Text = TextFormatter.Format(Tr("HUD_KILL_COUNT"), LastDigestedCount);
            }
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        LastLevel = newLevel;
        UpdateExpDisplay();
        if (CellUpgradeModal == null || !GodotObject.IsInstanceValid(CellUpgradeModal) || PlayerRef == null)
            return;

        // Cue 2 (docs/tutorial.md §2): the first level-up eases into 0.5s of
        // bullet time, freezes on the draft, then the 3-choice modal opens.
        if (!_firstLevelUpCuePlayed)
        {
            _firstLevelUpCuePlayed = true;
            if (SkipLevelUpBulletTime)
            {
                CellUpgradeModal.OpenUpgradeModal(PlayerRef);
                return;
            }
            PlayLevelUpBulletTime();
            return;
        }

        CellUpgradeModal.OpenUpgradeModal(PlayerRef);
    }

    private void PlayLevelUpBulletTime()
    {
        _bulletTimeTween?.Kill();
        _bulletTimeTween = CreateTween();
        _bulletTimeTween.SetIgnoreTimeScale(true);
        _bulletTimeTween.TweenMethod(
                Callable.From<float>(v => Engine.TimeScale = v), 1.0f, 0.05f, LevelUpBulletTimeSeconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _bulletTimeTween.TweenCallback(Callable.From(() =>
        {
            _bulletTimeTween = null;
            if (CellUpgradeModal != null && GodotObject.IsInstanceValid(CellUpgradeModal) && PlayerRef != null)
                CellUpgradeModal.OpenUpgradeModal(PlayerRef);
        }));
        GD.Print("[Tutorial] First level-up bullet time engaged.");
    }

    private void RestoreTimeScaleAfterLevelUp()
    {
        _bulletTimeTween?.Kill();
        _bulletTimeTween = null;

        var tween = CreateTween();
        tween.SetIgnoreTimeScale(true);
        tween.TweenMethod(Callable.From<float>(v => Engine.TimeScale = v), Engine.TimeScale, 1.0f, 0.3f);
    }

    public bool IsTreeOverlayVisible => TreeOverlayPanel?.Visible ?? false;

    public void ToggleTreeOverlay()
    {
        if (TreeOverlayPanel == null)
            return;

        bool show = !TreeOverlayPanel.Visible;
        if (show)
            RefreshTreeOverlay();
        TreeOverlayPanel.Visible = show;
    }

    public void RefreshTreeOverlay()
    {
        if (TreeOverlayPanel == null || TreeOverlayText == null)
            return;

        string classId = GameManager.SelectedClass;
        var classInfo = GameManager.GetClassInfo(classId);
        string className = classInfo.TryGetValue("name", out var nameVal) ? nameVal.AsString() : classId;
        var allocation = PassiveTreeManager.GetAllocation(classId);
        var text = new StringBuilder();

        text.AppendLine("[b]" + Tr("TREE_OVERLAY_TITLE") + "[/b]");
        text.AppendLine(className);
        text.AppendLine(TextFormatter.Format(Tr("TREE_OVERLAY_LEVEL"), PassiveTreeManager.GetCellLevel(classId), LastLevel));
        text.AppendLine(TextFormatter.Format(Tr("TREE_POINTS"), PassiveTreeManager.GetPointsAvailable(classId), PassiveTreeManager.GetSpentPoints(classId)));
        text.AppendLine();

        if (allocation.Count == 0)
        {
            text.AppendLine(Tr("TREE_OVERLAY_EMPTY"));
        }
        else
        {
            foreach (var node in PassiveTreeManager.Nodes)
            {
                if (!allocation.TryGetValue(node.Id, out int stacks))
                    continue;

                string icon = PassiveTreeManager.GetNodeIcon(node.Id);
                string nodeName = PassiveTreeManager.GetNodeName(node.Id);
                text.AppendLine(icon + " " + nodeName);
                string description = PassiveTreeManager.GetNodeDescription(node.Id);
                if (!string.IsNullOrEmpty(description))
                {
                    foreach (string line in description.Split("\n"))
                        text.AppendLine("  " + line);
                }
            }
        }

        text.AppendLine();
        text.AppendLine("[b]" + Tr("TREE_OVERLAY_STATS") + "[/b]");
        if (PlayerRef is BaseCell bc && bc.Stats != null)
        {
            string[] statKeys =
            {
                "might", "area", "cooldown_reduction", "projectile_speed", "duration", "amount",
                "pierce", "knockback", "crit_chance", "crit_damage", "ailment_damage",
                "max_health", "health_regen", "armor", "move_speed", "evasion", "block", "life_steal",
                "magnet"
            };
            foreach (string statKey in statKeys)
                text.AppendLine(PassiveTreeManager.GetStatLabel(statKey) + ": " + FormatTreeStat(statKey, bc.Stats.GetStat(statKey)));
        }
        else
        {
            text.AppendLine(Tr("TREE_OVERLAY_NO_PLAYER"));
        }
        text.AppendLine();
        text.AppendLine(Tr("TREE_OVERLAY_HINT"));

        TreeOverlayText.Text = text.ToString();
    }

    private static string FormatTreeStat(string statKey, float value)
    {
        return statKey switch
        {
            "max_health" or "health_regen" or "move_speed" or "magnet" => $"{value:F1}",
            "amount" or "pierce" or "armor" => $"{value:F0}",
            "cooldown_reduction" or "crit_chance" or "evasion" or "block" or "life_steal" => $"{value * 100.0f:F1}%",
            "might" or "area" or "projectile_speed" or "duration" or "knockback" or "crit_damage" or "ailment_damage" => $"{value * 100.0f:F0}%",
            _ => $"{value:F2}"
        };
    }

    private void SetupTreeOverlay()
    {
        TreeOverlayPanel = new PanelContainer
        {
            Name = "TreeOverlayPanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(400, 480),
            AnchorLeft = 1.0f,
            AnchorRight = 1.0f,
            AnchorTop = 0.0f,
            AnchorBottom = 0.0f,
            OffsetLeft = -420.0f,
            OffsetRight = -20.0f,
            OffsetTop = 80.0f,
            OffsetBottom = 620.0f
        };

        var style = UiBuilders.PanelStyle(
            new Color(0.05f, 0.08f, 0.13f, 0.95f),
            border: new Color(0.45f, 0.85f, 0.65f, 0.9f),
            borderWidth: 2, cornerRadius: 10, marginH: 16, marginV: 12);
        TreeOverlayPanel.AddThemeStyleboxOverride("panel", style);

        TreeOverlayText = new RichTextLabel
        {
            Name = "TreeOverlayText",
            BbcodeEnabled = true,
            ScrollActive = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        TreeOverlayText.AddThemeFontSizeOverride("normal_font_size", 13);
        TreeOverlayText.AddThemeFontSizeOverride("bold_font_size", 14);
        TreeOverlayPanel.AddChild(TreeOverlayText);
        AddChild(TreeOverlayPanel);
        RefreshTreeOverlay();
    }

    private void SetupAchievementBanner()
    {
        AchievementBanner = new PanelContainer
        {
            Name = "AchievementBanner",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(400, 60),
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.0f,
            AnchorBottom = 0.0f,
            OffsetLeft = -200.0f,
            OffsetRight = 200.0f,
            OffsetTop = 18.0f,
            OffsetBottom = 78.0f
        };

        var style = UiBuilders.PanelStyle(
            new Color(0.07f, 0.09f, 0.15f, 0.95f),
            border: new Color(1.0f, 0.84f, 0.28f, 0.95f),
            borderWidth: 2, cornerRadius: 8, marginH: 16, marginV: 8);
        AchievementBanner.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 14);
        AchievementBanner.AddChild(hbox);

        AchBannerIcon = new Label
        {
            Text = "🏆"
        };
        AchBannerIcon.AddThemeFontSizeOverride("font_size", 28);
        hbox.AddChild(AchBannerIcon);

        var vbox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        hbox.AddChild(vbox);

        AchBannerTitle = new Label
        {
            Text = Tr("TOAST_ACH_UNLOCKED"),
            Modulate = new Color(1.0f, 0.9f, 0.3f)
        };
        AchBannerTitle.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(AchBannerTitle);

        AchBannerDesc = new Label
        {
            Text = "",
            Modulate = new Color(0.9f, 0.95f, 1.0f)
        };
        AchBannerDesc.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(AchBannerDesc);

        AddChild(AchievementBanner);
    }

    /// <summary>Endless overdrive tier alert reusing the achievement toast (docs/endgame.md §3.2).</summary>
    public void ShowOverdriveAlert(string title, string desc)
    {
        PlayToastBanner("☣️", title, desc, new Color(1.0f, 0.72f, 0.25f));
    }

    private void OnAchievementUnlocked(string _achId, Dictionary achInfo)
    {
        string icon = achInfo.TryGetValue("icon", out var icVal) ? icVal.AsString() : "🏆";
        string title = Tr("TOAST_ACH_UNLOCKED") + " " + (achInfo.TryGetValue("title", out var ttVal) ? ttVal.AsString() : "");

        string subText = achInfo.TryGetValue("desc", out var dsVal) ? dsVal.AsString() : "";
        string rewardCell = achInfo.TryGetValue("reward_cell", out var rcVal) ? rcVal.AsString() : "";
        string reward = achInfo.TryGetValue("reward", out var rwVal) ? rwVal.AsString() : "";

        if (!string.IsNullOrEmpty(rewardCell))
        {
            var cellInfo = GameManager.GetClassInfo(rewardCell);
            string cName = cellInfo.TryGetValue("name", out var cnVal) ? cnVal.AsString() : rewardCell;
            subText = TextFormatter.Format(Tr("TOAST_CELL_UNLOCKED"), cName);
        }
        else if (!string.IsNullOrEmpty(reward))
        {
            subText = reward;
        }

        PlayToastBanner(icon, title, subText, null);
    }

    private void PlayToastBanner(string icon, string title, string desc, Color? titleColor)
    {
        if (AchievementBanner == null)
            return;

        if (AchBannerIcon != null) AchBannerIcon.Text = icon;
        if (AchBannerTitle != null)
        {
            AchBannerTitle.Text = title;
            AchBannerTitle.Modulate = titleColor ?? Colors.White;
        }
        if (AchBannerDesc != null) AchBannerDesc.Text = desc;

        AchievementBanner.Visible = true;
        AchievementBanner.Modulate = new Color(1, 1, 1, 0.0f);
        AchievementBanner.Position = new Vector2(AchievementBanner.Position.X, 0.0f);

        if (AchTween != null && AchTween.IsValid())
        {
            AchTween.Kill();
        }
        AchTween = CreateTween();
        AchTween.SetParallel(true);
        AchTween.TweenProperty(AchievementBanner, "modulate:a", 1.0f, 0.35);
        AchTween.TweenProperty(AchievementBanner, "position:y", 18.0f, 0.35)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        AchTween.Chain().TweenInterval(3.2);
        AchTween.Chain().TweenProperty(AchievementBanner, "modulate:a", 0.0f, 0.5);
        AchTween.Chain().TweenCallback(Callable.From(() => AchievementBanner.Visible = false));
    }
}
