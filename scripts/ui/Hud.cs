using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Player;

namespace Phagocyte.UI;

/// <summary>
/// HUD coordinator (mediator). Owns view assembly, player fan-out and the
/// per-frame dispatch; every readout lives in a sub-view below
/// (<see cref="VitalsView"/>, <see cref="SkillBarView"/>,
/// <see cref="WaveTimerView"/>, <see cref="PauseMenuView"/>,
/// <see cref="TutorialCueView"/>, <see cref="ToastView"/>).
/// The pre-split public surface is preserved as thin facades so the
/// test harness keeps working unchanged.
/// </summary>
public partial class Hud : CanvasLayer
{
    public static PackedScene UpgradeModalScene => AssetLoader.Load<PackedScene>("res://scenes/ui/upgrade_modal.tscn");

    // --- First-run micro-cues (docs/tutorial.md §2) ---
    public const float MoveCueSeconds = 5.0f;
    public const float DodgeHintSeconds = 6.0f;
    public const int DodgeHintNearbyThreshold = 15;
    public const float DodgeHintNearbyRadius = 300.0f;

    private VitalsView? _vitals;
    private SkillBarView? _skills;
    private WaveTimerView? _timer;
    private PauseMenuView? _pause;
    private TutorialCueView? _tutorial;
    private ToastView? _toast;

    private Label? _fpsLabel;
    private float _fpsAccum;

    /// <summary>
    /// Fluid-field provider (set by Main). Replaces the old
    /// <c>GetParent() is Main</c> coupling for the tutorial fluid cues.
    /// </summary>
    public Func<Vector2>? FluidVectorProvider { get; set; }

    public Node2D? PlayerRef { get; set; } = null;
    public UpgradeModal? CellUpgradeModal { get; set; } = null;

    private Callable _langCallback;

    // ---- Vitals facades ----
    public Label? LevelLabel { get => _vitals?.LevelLabel; set { if (_vitals != null) _vitals.LevelLabel = value; } }
    public Label? HpTitleLabel { get => _vitals?.HpTitleLabel; set { if (_vitals != null) _vitals.HpTitleLabel = value; } }
    public ProgressBar? HpBar { get => _vitals?.HpBar; set { if (_vitals != null) _vitals.HpBar = value; } }
    public Label? HpLabel { get => _vitals?.HpLabel; set { if (_vitals != null) _vitals.HpLabel = value; } }
    public Label? ExpTitleLabel { get => _vitals?.ExpTitleLabel; set { if (_vitals != null) _vitals.ExpTitleLabel = value; } }
    public ProgressBar? ExpBar { get => _vitals?.ExpBar; set { if (_vitals != null) _vitals.ExpBar = value; } }
    public Label? ExpLabel { get => _vitals?.ExpLabel; set { if (_vitals != null) _vitals.ExpLabel = value; } }
    public Label? SizeLabel { get => _vitals?.SizeLabel; set { if (_vitals != null) _vitals.SizeLabel = value; } }
    public Label? CountLabel { get => _vitals?.CountLabel; set { if (_vitals != null) _vitals.CountLabel = value; } }
    public Label? SpeedLabel { get => _vitals?.SpeedLabel; set { if (_vitals != null) _vitals.SpeedLabel = value; } }
    public Label? KillLabel { get => _vitals?.KillLabel; set { if (_vitals != null) _vitals.KillLabel = value; } }
    public ProgressBar? BottomExpBar { get => _vitals?.BottomExpBar; set { if (_vitals != null) _vitals.BottomExpBar = value; } }
    public ColorRect? VignetteRect { get => _vitals?.VignetteRect; set { if (_vitals != null) _vitals.VignetteRect = value; } }
    public Label? BuffTag { get => _vitals?.BuffTag; set { if (_vitals != null) _vitals.BuffTag = value; } }

    public float LastHealth { get => _vitals?.LastHealth ?? 100.0f; set { if (_vitals != null) _vitals.LastHealth = value; } }
    public float LastMaxHealth { get => _vitals?.LastMaxHealth ?? 100.0f; set { if (_vitals != null) _vitals.LastMaxHealth = value; } }
    public float LastCurrentExp { get => _vitals?.LastCurrentExp ?? 0.0f; set { if (_vitals != null) _vitals.LastCurrentExp = value; } }
    public float LastExpToNext { get => _vitals?.LastExpToNext ?? 30.0f; set { if (_vitals != null) _vitals.LastExpToNext = value; } }
    public int LastLevel { get => _vitals?.LastLevel ?? 1; set { if (_vitals != null) _vitals.LastLevel = value; if (_pause != null) _pause.LastLevel = value; } }
    public float LastRadiusRatio { get => _vitals?.LastRadiusRatio ?? 1.0f; set { if (_vitals != null) _vitals.LastRadiusRatio = value; } }
    public int LastDigestedCount { get => _vitals?.LastDigestedCount ?? 0; set { if (_vitals != null) _vitals.LastDigestedCount = value; } }
    public float LastSpeed { get => _vitals?.LastSpeed ?? 230.0f; set { if (_vitals != null) _vitals.LastSpeed = value; } }

    // ---- Skill bar facades ----
    public PanelContainer? SkillContainer { get => _skills?.SkillContainer; set { if (_skills != null) _skills.SkillContainer = value; } }
    public Label? SkillTitleLbl { get => _skills?.SkillTitleLbl; set { if (_skills != null) _skills.SkillTitleLbl = value; } }
    public GridContainer? SlotsContainer { get => _skills?.SlotsContainer; set { if (_skills != null) _skills.SlotsContainer = value; } }
    public PanelContainer? SkillTooltip { get => _skills?.SkillTooltip; set { if (_skills != null) _skills.SkillTooltip = value; } }
    public Label? TooltipIcon { get => _skills?.TooltipIcon; set { if (_skills != null) _skills.TooltipIcon = value; } }
    public Label? TooltipTitle { get => _skills?.TooltipTitle; set { if (_skills != null) _skills.TooltipTitle = value; } }
    public Label? TooltipBadge { get => _skills?.TooltipBadge; set { if (_skills != null) _skills.TooltipBadge = value; } }
    public Label? TooltipStats { get => _skills?.TooltipStats; set { if (_skills != null) _skills.TooltipStats = value; } }
    public Label? TooltipDesc { get => _skills?.TooltipDesc; set { if (_skills != null) _skills.TooltipDesc = value; } }
    public Label? TooltipBio { get => _skills?.TooltipBio; set { if (_skills != null) _skills.TooltipBio = value; } }
    public int HoveredSlotIdx { get => _skills?.HoveredSlotIdx ?? -1; set { if (_skills != null) _skills.HoveredSlotIdx = value; } }

    // ---- Timer facades ----
    public PanelContainer? TopCenterCapsule { get => _timer?.TopCenterCapsule; set { if (_timer != null) _timer.TopCenterCapsule = value; } }
    public Label? TimerLabel { get => _timer?.TimerLabel; set { if (_timer != null) _timer.TimerLabel = value; } }
    public Label? TitleLabel { get => _timer?.TitleLabel; set { if (_timer != null) _timer.TitleLabel = value; } }
    public Label? MapLabel { get => _timer?.MapLabel; set { if (_timer != null) _timer.MapLabel = value; } }
    public float SurvivalTime { get => _timer?.SurvivalTime ?? 0.0f; set { if (_timer != null) _timer.SurvivalTime = value; } }
    public float GoalSeconds { get => _timer?.GoalSeconds ?? 0.0f; set { if (_timer != null) _timer.GoalSeconds = value; } }
    public bool EndlessMode { get => _timer?.EndlessMode ?? false; set { if (_timer != null) _timer.EndlessMode = value; } }

    // ---- Pause facades ----
    public PanelContainer? PauseModal { get => _pause?.PauseModal; set { if (_pause != null) _pause.PauseModal = value; } }
    public Label? PauseTitle { get => _pause?.PauseTitle; set { if (_pause != null) _pause.PauseTitle = value; } }
    public Button? ResumeBtn { get => _pause?.ResumeBtn; set { if (_pause != null) _pause.ResumeBtn = value; } }
    public Button? SettingsBtn { get => _pause?.SettingsBtn; set { if (_pause != null) _pause.SettingsBtn = value; } }
    public Button? ManualBtn { get => _pause?.ManualBtn; set { if (_pause != null) _pause.ManualBtn = value; } }
    public Button? RestartBtn { get => _pause?.RestartBtn; set { if (_pause != null) _pause.RestartBtn = value; } }
    public Button? MenuBtn { get => _pause?.MenuBtn; set { if (_pause != null) _pause.MenuBtn = value; } }
    public CodexModal? CellCodexModal { get => _pause?.CellCodexModal; set { if (_pause != null) _pause.CellCodexModal = value; } }
    public SettingsModal? CellSettingsModal { get => _pause?.CellSettingsModal; set { if (_pause != null) _pause.CellSettingsModal = value; } }
    public PanelContainer? TreeOverlayPanel { get => _pause?.TreeOverlayPanel; set { if (_pause != null) _pause.TreeOverlayPanel = value; } }
    public RichTextLabel? TreeOverlayText { get => _pause?.TreeOverlayText; set { if (_pause != null) _pause.TreeOverlayText = value; } }
    public bool PauseInputSuppressed { get => _pause?.PauseInputSuppressed ?? false; set { if (_pause != null) _pause.PauseInputSuppressed = value; } }
    public bool IsTreeOverlayVisible => _pause?.IsTreeOverlayVisible ?? false;

    // ---- Tutorial facades ----
    public TutorialOverlay? TutorialOverlayNode { get => _tutorial?.TutorialOverlayNode; set { if (_tutorial != null) _tutorial.TutorialOverlayNode = value; } }
    public bool FirstLevelUpCuePlayed => _tutorial?.FirstLevelUpCuePlayed ?? false;
    public bool IsBulletTimeActive => _tutorial?.IsBulletTimeActive ?? false;
    public bool DodgeHintShownOnce => _tutorial?.DodgeHintShownOnce ?? false;
    public float LevelUpBulletTimeSeconds { get => _tutorial?.LevelUpBulletTimeSeconds ?? 0.5f; set { if (_tutorial != null) _tutorial.LevelUpBulletTimeSeconds = value; } }
    public bool SkipLevelUpBulletTime { get => _tutorial?.SkipLevelUpBulletTime ?? false; set { if (_tutorial != null) _tutorial.SkipLevelUpBulletTime = value; } }

    // ---- Toast facades ----
    public PanelContainer? AchievementBanner { get => _toast?.AchievementBanner; set { if (_toast != null) _toast.AchievementBanner = value; } }
    public Label? AchBannerTitle { get => _toast?.AchBannerTitle; set { if (_toast != null) _toast.AchBannerTitle = value; } }
    public Label? AchBannerDesc { get => _toast?.AchBannerDesc; set { if (_toast != null) _toast.AchBannerDesc = value; } }
    public TextureRect? AchBannerIcon { get => _toast?.AchBannerIcon; set { if (_toast != null) _toast.AchBannerIcon = value; } }
    public Tween? AchTween { get => _toast?.AchTween; set { if (_toast != null) _toast.AchTween = value; } }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _vitals = new VitalsView { Name = "VitalsView" };
        _skills = new SkillBarView { Name = "SkillBarView" };
        _timer = new WaveTimerView { Name = "WaveTimerView" };
        _pause = new PauseMenuView { Name = "PauseMenuView" };
        _tutorial = new TutorialCueView { Name = "TutorialCueView" };
        _toast = new ToastView { Name = "ToastView" };
        AddChild(_vitals);
        AddChild(_skills);
        AddChild(_timer);
        AddChild(_pause);
        AddChild(_tutorial);
        AddChild(_toast);

        _vitals.Bind(this);
        _skills.Bind(this);
        _timer.Bind(this);
        _pause.Bind(this);
        _tutorial.Bind();
        _toast.Bind();

        if (HasNode("UpgradeModal"))
        {
            CellUpgradeModal = GetNodeOrNull<UpgradeModal>("UpgradeModal");
        }
        else
        {
            CellUpgradeModal = UpgradeModalScene.Instantiate<UpgradeModal>();
            AddChild(CellUpgradeModal);
        }
        _tutorial.CellUpgradeModal = CellUpgradeModal;

        // Cross-view wiring (replaces the old direct method calls).
        _vitals.VitalsChanged += () => _pause?.RefreshTreeOverlay();
        _tutorial.LeveledUp += (lvl) =>
        {
            _vitals?.OnPlayerLeveledUp(lvl);
            if (_pause != null) _pause.LastLevel = lvl;
        };
        _skills.LevelUpForwarded += (lvl) => _tutorial?.OnPlayerLevelUp(lvl);
        _skills.SetupSlotHoverSignals();

        if (CellUpgradeModal != null)
            CellUpgradeModal.ChoiceApplied += _ => _tutorial?.RestoreTimeScaleAfterLevelUp();

        _langCallback = Callable.From((string _) => UpdateLocalizedTexts());
        GameManager.AddLanguageListener(_langCallback);

        // Code-built FPS counter (top-right, hidden unless opted in).
        _fpsLabel = new Label
        {
            Name = "FpsLabel",
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = SettingsManager.ShowFps
        };
        _fpsLabel.AnchorLeft = 1.0f;
        _fpsLabel.AnchorRight = 1.0f;
        _fpsLabel.OffsetLeft = -140.0f;
        _fpsLabel.OffsetRight = -10.0f;
        _fpsLabel.OffsetTop = 8.0f;
        _fpsLabel.OffsetBottom = 28.0f;
        _fpsLabel.AddThemeFontSizeOverride("font_size", 13);
        _fpsLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.9f, 0.6f, 0.9f));
        AddChild(_fpsLabel);

        UpdateLocalizedTexts();
    }

    public override void _ExitTree()
    {
        _toast?.Unbind();
        GameManager.RemoveLanguageListener(_langCallback);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _timer?.Tick(dt);
        _vitals?.TickVignette(_timer?.SurvivalTime ?? 0.0f);
        _skills?.TickAlpha(dt, GetTree().Paused);
        _vitals?.UpdateBuffStatus();
        _skills?.TickSkillSlots();
        TickFps(dt);
        Vector2 fluid = FluidVectorProvider?.Invoke() ?? Vector2.Zero;
        _tutorial?.UpdateTutorialCues(dt, fluid);
    }

    public override void _Input(InputEvent @event)
    {
        if (PauseInputSuppressed)
            return;
        _pause?.HandleInput(@event);
    }

    public void ConnectPlayer(Node2D player)
    {
        PlayerRef = player;
        _tutorial?.ResetTutorialCues();
        if (_vitals != null) _vitals.PlayerRef = player;
        if (_skills != null) _skills.PlayerRef = player;
        if (_tutorial != null && player is not BaseCell) _tutorial.PlayerRef = player;
        if (_pause != null)
        {
            _pause.PlayerRef = player;
            _pause.LastLevel = _vitals?.LastLevel ?? 1;
        }

        if (player is BaseCell bc)
        {
            _vitals?.ConnectPlayer(bc);
            _tutorial?.ConnectPlayer(player);
            if (_pause != null) _pause.LastLevel = _vitals?.LastLevel ?? 1;
        }
        else
        {
            _vitals?.ConnectFallback(player);
            _tutorial?.ConnectPlayer(player);
        }

        _vitals?.UpdateBuffStatus();
        _skills?.UpdateSkillSlots();
        _pause?.RefreshTreeOverlay();
    }

    public void UpdateLocalizedTexts()
    {
        _timer?.UpdateLocalizedTexts();
        _vitals?.UpdateLocalizedTexts();
        _skills?.UpdateLocalizedTexts();
        _pause?.UpdateLocalizedTexts();
    }

    public void UpdateExpDisplay()
    {
        _vitals?.UpdateExpDisplay();
    }

    public void UpdateSkillSlots()
    {
        _skills?.UpdateSkillSlots();
    }

    /// <summary>Shows or hides the FPS counter overlay (settings toggle).</summary>
    public void SetFpsVisible(bool visible)
    {
        if (_fpsLabel != null)
        {
            _fpsLabel.Visible = visible;
            _fpsAccum = 1.0f;
        }
    }

    /// <summary>4 Hz text refresh so the counter itself allocates nothing per frame.</summary>
    private void TickFps(float dt)
    {
        if (_fpsLabel == null || !_fpsLabel.Visible)
            return;
        _fpsAccum += dt;
        if (_fpsAccum >= 0.25f)
        {
            _fpsAccum = 0.0f;
            _fpsLabel.Text = Engine.GetFramesPerSecond() + " FPS";
        }
    }

    public void OnSlotMouseEntered(int slotIdx, Control card)
    {
        _skills?.OnSlotMouseEntered(slotIdx, card);
    }

    public void OnSlotMouseExited(int slotIdx)
    {
        _skills?.OnSlotMouseExited(slotIdx);
    }

    public void TogglePause()
    {
        _pause?.TogglePause();
    }

    public void ResumeGame()
    {
        _pause?.ResumeGame();
    }

    public void OnManualPressed()
    {
        _pause?.OnManualPressed();
    }

    public void ToggleTreeOverlay()
    {
        _pause?.ToggleTreeOverlay();
    }

    public void RefreshTreeOverlay()
    {
        _pause?.RefreshTreeOverlay();
    }

    public void ShowDodgeHint()
    {
        _tutorial?.ShowDodgeHint();
    }

    public void ResetTutorialCues()
    {
        _tutorial?.ResetTutorialCues();
    }

    /// <summary>Endless overdrive tier alert reusing the achievement toast (docs/endgame.md §3.2).</summary>
    public void ShowOverdriveAlert(string title, string desc)
    {
        _toast?.ShowOverdriveAlert(title, desc);
    }
}
