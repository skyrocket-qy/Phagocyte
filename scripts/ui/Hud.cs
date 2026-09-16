using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;
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

    public Label? TitleLabel { get; set; }
    public Label? MapLabel { get; set; }
    public ProgressBar? HpBar { get; set; }
    public Label? HpLabel { get; set; }
    public ProgressBar? AtpBar { get; set; }
    public Label? AtpLabel { get; set; }
    public Label? SizeLabel { get; set; }
    public Label? CountLabel { get; set; }

    public PanelContainer? BurstPanel { get; set; }
    public Label? BurstLabel { get; set; }
    public ProgressBar? BurstBar { get; set; }

    public PanelContainer? PauseModal { get; set; }
    public Label? PauseTitle { get; set; }
    public Button? ResumeBtn { get; set; }
    public Button? SettingsBtn { get; set; }
    public Button? ManualBtn { get; set; }
    public Button? RestartBtn { get; set; }
    public Button? MenuBtn { get; set; }

    public CodexModal? CellCodexModal { get; set; }
    public SettingsModal? CellSettingsModal { get; set; }

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

    public int HoveredSlotIdx { get; set; } = -1;

    // Cached last stats for re-rendering upon language change
    public float LastHealth { get; set; } = 100.0f;
    public float LastMaxHealth { get; set; } = 100.0f;
    public float LastSatiety { get; set; } = 0.0f;
    public float LastMaxSatiety { get; set; } = 100.0f;
    public float LastRadiusRatio { get; set; } = 1.0f;
    public int LastDigestedCount { get; set; } = 0;
    public float LastBurstTimeLeft { get; set; } = 0.0f;

    public Node2D? PlayerRef { get; set; } = null;
    public UpgradeModal? CellUpgradeModal { get; set; } = null;

    private Callable _langCallback;
    private Callable _achUnlockCallback;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        TitleLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/TitleLabel");
        MapLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/MapLabel");
        HpBar = GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPBar");
        HpLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPLabel");
        AtpBar = GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPBar");
        AtpLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPLabel");
        SizeLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/SizeLabel");
        CountLabel = GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/CountLabel");

        BurstPanel = GetNodeOrNull<PanelContainer>("BurstContainer");
        BurstLabel = GetNodeOrNull<Label>("BurstContainer/VBox/BurstLabel");
        BurstBar = GetNodeOrNull<ProgressBar>("BurstContainer/VBox/BurstBar");

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

        if (BurstPanel != null) BurstPanel.Visible = false;
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

    public override void _Process(double delta)
    {
        UpdateSkillSlots();
    }

    public void UpdateLocalizedTexts()
    {
        if (TitleLabel != null) TitleLabel.Text = Tr("HUD_TITLE");

        var mapInfo = GameManager.GetMapInfo(GameManager.SelectedMap);
        if (mapInfo.TryGetValue("name", out var mapName) && MapLabel != null)
        {
            MapLabel.Text = Tr("HUD_BATTLEFIELD") + mapName.AsString();
        }

        if (HpLabel != null) HpLabel.Text = TextFormatter.Format(Tr("HUD_HP"), (int)LastHealth, (int)LastMaxHealth);
        if (AtpLabel != null) AtpLabel.Text = TextFormatter.Format(Tr("HUD_ATP"), (int)((LastSatiety / Mathf.Max(1.0f, LastMaxSatiety)) * 100.0f));
        if (SizeLabel != null) SizeLabel.Text = TextFormatter.Format(Tr("HUD_SIZE"), LastRadiusRatio);
        if (CountLabel != null) CountLabel.Text = TextFormatter.Format(Tr("HUD_DIGESTED"), LastDigestedCount);
        if (SkillTitleLbl != null) SkillTitleLbl.Text = Tr("SKILL_BAR_DUAL_TITLE");

        if (BurstPanel != null && BurstPanel.Visible && BurstLabel != null)
        {
            BurstLabel.Text = TextFormatter.Format(Tr("HUD_BURST_ALERT"), LastBurstTimeLeft);
        }

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

            string badgeText = "";
            Color badgeColor = new Color(1, 1, 1);
            string statsText = "";

            bool isInnate = data.TryGetValue("is_innate", out var innVal) && innVal.AsBool();
            bool isPassive = data.TryGetValue("is_passive", out var passVal) && passVal.AsBool();
            int level = data.TryGetValue("level", out var lvVal) ? lvVal.AsInt32() : 1;
            int maxLevel = data.TryGetValue("max_level", out var mlvVal) ? mlvVal.AsInt32() : 5;

            if (isInnate)
            {
                badgeText = "[ " + Tr("TOOLTIP_TAG_INNATE") + " ]";
                badgeColor = new Color(0.4f, 0.95f, 0.8f);
                statsText = Tr("TOOLTIP_ALWAYS_ACTIVE") + " • " + TextFormatter.Format(Tr("TOOLTIP_LV_FORMAT"), level, maxLevel);
            }
            else if (isPassive)
            {
                badgeText = "[ " + Tr("TOOLTIP_TAG_PASSIVE") + " ]";
                badgeColor = new Color(0.6f, 0.8f, 1.0f);
                statsText = Tr("TOOLTIP_ALWAYS_ACTIVE") + " • " + TextFormatter.Format(Tr("TOOLTIP_LV_FORMAT"), level, maxLevel);
            }
            else
            {
                badgeText = "[ " + Tr("TOOLTIP_TAG_ACTIVE") + " ]";
                badgeColor = new Color(1.0f, 0.85f, 0.3f);
                float maxCd = data.TryGetValue("cooldown_max", out var cdVal) ? cdVal.AsSingle() : 3.2f;
                statsText = TextFormatter.Format(Tr("TOOLTIP_CD"), maxCd) + " • " + TextFormatter.Format(Tr("TOOLTIP_LV_FORMAT"), level, maxLevel);
            }

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
                TooltipStats.Text = slotIdx >= 5 ? "🧬 被动特质槽 (Passive Slot)" : Tr("SKILL_BAR_TITLE");
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
        if (player is BaseCell bc)
        {
            bc.StatsChanged += (h, mh, s, ms, r) => OnPlayerStatsChanged(h, mh, s, ms, r);
            bc.BurstStateChanged += (a, t, m) => OnPlayerBurstStateChanged(a, t, m);
            bc.PathogenDigested += (p, atp) => OnPathogenDigested(p, atp);
            bc.LevelUp += (lvl) => OnPlayerLevelUp((int)lvl);
        }
        else
        {
            if (player.HasSignal("StatsChanged"))
                player.Connect("StatsChanged", Callable.From((float h, float mh, float s, float ms, float r) => OnPlayerStatsChanged(h, mh, s, ms, r)));
            else if (player.HasSignal("stats_changed"))
                player.Connect("stats_changed", Callable.From((float h, float mh, float s, float ms, float r) => OnPlayerStatsChanged(h, mh, s, ms, r)));

            if (player.HasSignal("BurstStateChanged"))
                player.Connect("BurstStateChanged", Callable.From((bool a, float t, float m) => OnPlayerBurstStateChanged(a, t, m)));
            else if (player.HasSignal("burst_state_changed"))
                player.Connect("burst_state_changed", Callable.From((bool a, float t, float m) => OnPlayerBurstStateChanged(a, t, m)));

            if (player.HasSignal("PathogenDigested"))
                player.Connect("PathogenDigested", Callable.From((Node2D p, float atp) => OnPathogenDigested(p, atp)));
            else if (player.HasSignal("pathogen_digested"))
                player.Connect("pathogen_digested", Callable.From((Node2D p, float atp) => OnPathogenDigested(p, atp)));

            if (player.HasSignal("LevelUp"))
                player.Connect("LevelUp", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));
            else if (player.HasSignal("level_up"))
                player.Connect("level_up", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));
        }

        UpdateSkillSlots();
    }

    private void OnPlayerStatsChanged(float health, float maxHealth, float satiety, float maxSatiety, float radiusRatio)
    {
        LastHealth = health;
        LastMaxHealth = maxHealth;
        LastSatiety = satiety;
        LastMaxSatiety = maxSatiety;
        LastRadiusRatio = radiusRatio;

        if (HpBar != null)
        {
            HpBar.MaxValue = maxHealth;
            HpBar.Value = health;
        }
        if (HpLabel != null) HpLabel.Text = TextFormatter.Format(Tr("HUD_HP"), (int)health, (int)maxHealth);

        if (AtpBar != null)
        {
            AtpBar.MaxValue = maxSatiety;
            AtpBar.Value = satiety;
        }
        if (AtpLabel != null) AtpLabel.Text = TextFormatter.Format(Tr("HUD_ATP"), (int)((satiety / maxSatiety) * 100.0f));

        if (SizeLabel != null) SizeLabel.Text = TextFormatter.Format(Tr("HUD_SIZE"), radiusRatio);
    }

    private void OnPlayerBurstStateChanged(bool isActive, float timeLeft, float maxTime)
    {
        if (BurstPanel != null) BurstPanel.Visible = isActive;
        LastBurstTimeLeft = timeLeft;
        if (isActive)
        {
            if (BurstBar != null)
            {
                BurstBar.MaxValue = maxTime;
                BurstBar.Value = timeLeft;
            }
            if (BurstLabel != null) BurstLabel.Text = TextFormatter.Format(Tr("HUD_BURST_ALERT"), timeLeft);
        }
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
                if (CountLabel != null) CountLabel.Text = TextFormatter.Format(Tr("HUD_DIGESTED"), LastDigestedCount);
            }
        }
    }

    private void OnPlayerLevelUp(int _newLevel)
    {
        if (CellUpgradeModal != null && GodotObject.IsInstanceValid(CellUpgradeModal) && PlayerRef != null)
        {
            CellUpgradeModal.OpenUpgradeModal(PlayerRef);
        }
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

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.09f, 0.15f, 0.95f),
            BorderColor = new Color(1.0f, 0.84f, 0.28f, 0.95f),
            ContentMarginLeft = 16.0f,
            ContentMarginRight = 16.0f,
            ContentMarginTop = 8.0f,
            ContentMarginBottom = 8.0f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
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

    private void OnAchievementUnlocked(string _achId, Dictionary achInfo)
    {
        if (AchievementBanner == null)
            return;

        if (AchBannerIcon != null) AchBannerIcon.Text = achInfo.TryGetValue("icon", out var icVal) ? icVal.AsString() : "🏆";
        if (AchBannerTitle != null) AchBannerTitle.Text = Tr("TOAST_ACH_UNLOCKED") + " " + (achInfo.TryGetValue("title", out var ttVal) ? ttVal.AsString() : "");

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

        if (AchBannerDesc != null) AchBannerDesc.Text = subText;

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
