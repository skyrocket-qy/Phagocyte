using Godot;
using Godot.Collections;
using Phagocyte.Core;
using Phagocyte.Skills;

namespace Phagocyte.UI;

/// <summary>
/// 10-slot skill bar + cooldown overlays + hover tooltips (former <c>Hud</c>
/// skill block). Owns its node references and hover state; driven explicitly
/// by <see cref="Hud"/> (no <c>_Process</c> of its own).
/// </summary>
public partial class SkillBarView : Node
{
    public PanelContainer? SkillContainer { get; set; }
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

    public Node2D? PlayerRef { get; set; }

    private Callable? _levelUpCallback;

    /// <summary>Wires skill-bar nodes. Call once from Hud._Ready (before hover setup).</summary>
    public void Bind(Node root)
    {
        SkillContainer = root.GetNodeOrNull<PanelContainer>("SkillContainer");
        SkillTitleLbl = root.GetNodeOrNull<Label>("SkillContainer/VBox/TitleLabel");
        SlotsContainer = root.GetNodeOrNull<GridContainer>("SkillContainer/VBox/SlotsContainer");

        SkillTooltip = root.GetNodeOrNull<PanelContainer>("SkillTooltip");
        TooltipIcon = root.GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipIcon");
        TooltipTitle = root.GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipTitle");
        TooltipBadge = root.GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipBadge");
        TooltipStats = root.GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipStats");
        TooltipDesc = root.GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipDesc");
        TooltipBio = root.GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipBio");
    }

    /// <summary>Dynamic transparency: 35% in combat, 100% on hover/pause.</summary>
    public void TickAlpha(float delta, bool paused)
    {
        if (SkillContainer != null)
        {
            bool isInteracting = HoveredSlotIdx >= 0 || paused;
            float targetAlpha = isInteracting ? 1.0f : 0.35f;
            Color c = SkillContainer.Modulate;
            float newA = Mathf.MoveToward(c.A, targetAlpha, delta * 3.0f);
            SkillContainer.Modulate = new Color(c.R, c.G, c.B, newA);
        }
    }

    public void UpdateLocalizedTexts()
    {
        if (SkillTitleLbl != null) SkillTitleLbl.Text = Tr("SKILL_BAR_DUAL_TITLE");

        if (HoveredSlotIdx >= 0 && SkillTooltip != null && GodotObject.IsInstanceValid(SkillTooltip) && SkillTooltip.Visible)
        {
            RefreshTooltipContent(HoveredSlotIdx);
        }

        UpdateSkillSlots();
    }

    /// <summary>Resolves the player's SkillManager UI payload, typed or GDScript.</summary>
    private bool TryGetSkillsData(out Array<Dictionary> skillsData)
    {
        skillsData = new Array<Dictionary>();
        if (PlayerRef == null)
            return false;

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
            return false;
        }

        return true;
    }

    public void UpdateSkillSlots()
    {
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
        {
            PlayerRef = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (PlayerRef == null)
                return;
            if (PlayerRef.HasSignal("level_up"))
            {
                // Reuse one callable so the IsConnected guard actually matches;
                // a fresh lambda per call would stack duplicate handlers.
                _levelUpCallback ??= Callable.From((int lvl) => LevelUpForwarded?.Invoke(lvl));
                if (!PlayerRef.IsConnected("level_up", _levelUpCallback.Value))
                {
                    PlayerRef.Connect("level_up", _levelUpCallback.Value);
                }
            }
        }

        if (!TryGetSkillsData(out var skillsData))
            return;

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
                    iconLbl.Visible = false;
                var iconTex = slotCard.GetNodeOrNull<TextureRect>("IconTexture");
                if (iconTex == null)
                {
                    iconTex = new TextureRect
                    {
                        Name = "IconTexture",
                        CustomMinimumSize = new Vector2(44, 44),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = Control.MouseFilterEnum.Ignore
                    };
                    slotCard.AddChild(iconTex);
                    slotCard.MoveChild(iconTex, 0);
                }
                string slotImagePath = data.TryGetValue("image_path", out var sipVal) ? sipVal.AsString() : "";
                iconTex.Texture = AssetLoader.TryLoad<Texture2D>(slotImagePath)
                    ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                iconTex.Modulate = new Color(1, 1, 1, 1);
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
                // Empty Slot: placeholder texture, no emoji.
                if (iconLbl != null)
                    iconLbl.Visible = false;
                var emptyTex = slotCard.GetNodeOrNull<TextureRect>("IconTexture");
                if (emptyTex == null)
                {
                    emptyTex = new TextureRect
                    {
                        Name = "IconTexture",
                        CustomMinimumSize = new Vector2(44, 44),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = Control.MouseFilterEnum.Ignore
                    };
                    slotCard.AddChild(emptyTex);
                    slotCard.MoveChild(emptyTex, 0);
                }
                emptyTex.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                emptyTex.Modulate = i >= 5
                    ? new Color(0.65f, 0.55f, 0.8f, 0.5f)
                    : new Color(0.4f, 0.5f, 0.6f, 0.6f);
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

    /// <summary>Forwarded GDScript <c>level_up</c> signal (coordinator routes it to the tutorial view).</summary>
    public event System.Action<int>? LevelUpForwarded;

    public void SetupSlotHoverSignals()
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

    public void RefreshTooltipContent(int slotIdx)
    {
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
        {
            PlayerRef = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (PlayerRef == null)
                return;
        }

        if (!TryGetSkillsData(out var skillsData))
            return;

        if (slotIdx < 0 || slotIdx >= skillsData.Count)
            return;

        var data = skillsData[slotIdx];
        string id = data.TryGetValue("id", out var idVal) ? idVal.AsString() : "";

        if (!string.IsNullOrEmpty(id))
        {
            if (TooltipIcon != null) TooltipIcon.Visible = false;
            var tipTex = SkillTooltip?.GetNodeOrNull<TextureRect>("VBox/HeaderHBox/TooltipIconTexture");
            if (tipTex == null)
            {
                var header = SkillTooltip?.GetNodeOrNull<HBoxContainer>("VBox/HeaderHBox");
                if (header != null)
                {
                    tipTex = new TextureRect
                    {
                        Name = "TooltipIconTexture",
                        CustomMinimumSize = new Vector2(48, 48),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = Control.MouseFilterEnum.Ignore
                    };
                    header.AddChild(tipTex);
                    header.MoveChild(tipTex, 0);
                }
            }
            if (tipTex != null)
            {
                string tipImagePath = data.TryGetValue("image_path", out var tipIpVal) ? tipIpVal.AsString() : "";
                tipTex.Texture = AssetLoader.TryLoad<Texture2D>(tipImagePath)
                    ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
            }
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
                TooltipBio.Text = Tr("CODEX_HEADER_BIO") + "\n" + UiBuilders.StripLeadingLabel(bioText);
                TooltipBio.Visible = !string.IsNullOrEmpty(bioText);
            }
        }
        else
        {
            if (TooltipIcon != null) TooltipIcon.Visible = false;
            var emptyTipTex = SkillTooltip?.GetNodeOrNull<TextureRect>("VBox/HeaderHBox/TooltipIconTexture");
            if (emptyTipTex != null)
                emptyTipTex.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
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
                TooltipStats.Text = slotIdx >= 5 ? Tr("TOOLTIP_EMPTY_PASSIVE") : Tr("SKILL_BAR_TITLE");
            }
            if (TooltipDesc != null) TooltipDesc.Text = Tr("TOOLTIP_EMPTY_DESC");
            if (TooltipBio != null)
            {
                TooltipBio.Text = "";
                TooltipBio.Visible = false;
            }
        }
    }
}
