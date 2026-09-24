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
    public GridContainer? SlotsContainer { get; set; }

    public PanelContainer? SkillTooltip { get; set; }
    public Label? TooltipIcon { get; set; }
    public Label? TooltipTitle { get; set; }
    public Label? TooltipBadge { get; set; }
    public Label? TooltipStats { get; set; }
    public Label? TooltipDesc { get; set; }
    public Label? TooltipBio { get; set; }

    /// <summary>Chamber energy row (built once, only shown while the player engages organelles).</summary>
    public HBoxContainer? ChamberRow { get; set; }
    public Label? ChamberLabel { get; set; }
    public EnergyPips? ChamberPips { get; set; }

    public int HoveredSlotIdx { get; set; } = -1;

    public Node2D? PlayerRef { get; set; }

    /// <summary>Per-slot cached node refs + last rendered state (dirty-only refresh).</summary>
    private sealed class SlotCache
    {
        public Label? IconLbl;
        public Label? BadgeLbl;
        public ProgressBar? CdOverlay;
        public TextureRect? IconTex;
        public string LastId = "\u0000uninit\u0000";
        public string LastImagePath = "\u0000uninit\u0000";
        public string LastBadge = "\u0000uninit\u0000";
        public int LastLevel = int.MinValue;
        public bool LastIsPassive;
        public bool LastHasCd;
        public float LastCdRatio = -1.0f;
    }

    private readonly System.Collections.Generic.List<SlotCache> _slotCache = new();
    private string? _skillLvTemplate;
    private string? _chamberEnergyTemplate;
    private int _tickCounter;
    private int _fallbackTickCounter;

    // Last chamber row state (dirty check for UpdateChamberRow).
    private int _lastChamberUsed = int.MinValue;
    private int _lastChamberMax = int.MinValue;
    private int _lastChamberGens = int.MinValue;
    private bool _lastChamberEngaged;
    private bool _lastChamberOverloaded;
    private Node2D? _lastChamberPlayer;

    /// <summary>Wires skill-bar nodes. Call once from Hud._Ready (before hover setup).</summary>
    public void Bind(Node root)
    {
        SkillContainer = root.GetNodeOrNull<PanelContainer>("SkillContainer");
        SlotsContainer = root.GetNodeOrNull<GridContainer>("SkillContainer/VBox/SlotsContainer");

        SkillTooltip = root.GetNodeOrNull<PanelContainer>("SkillTooltip");
        TooltipIcon = root.GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipIcon");
        TooltipTitle = root.GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipTitle");
        TooltipBadge = root.GetNodeOrNull<Label>("SkillTooltip/VBox/HeaderHBox/TooltipBadge");
        TooltipStats = root.GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipStats");
        TooltipDesc = root.GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipDesc");
        TooltipBio = root.GetNodeOrNull<Label>("SkillTooltip/VBox/TooltipBio");

        // Chamber energy row (Phase 2): code-built so the shared hud.tscn
        // stays untouched; only visible once the player engages organelles.
        ChamberRow = root.GetNodeOrNull<HBoxContainer>("SkillContainer/VBox/ChamberRow");
        if (ChamberRow == null)
        {
            ChamberRow = new HBoxContainer
            {
                Name = "ChamberRow",
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = false
            };
            ChamberRow.AddThemeConstantOverride("separation", 6);
            ChamberLabel = new Label
            {
                Name = "ChamberLabel",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            ChamberLabel.AddThemeFontSizeOverride("font_size", 12);
            ChamberLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.92f, 1.0f));
            ChamberPips = new EnergyPips
            {
                Name = "ChamberPips",
                CustomMinimumSize = new Vector2(92, 16),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                MaxDiameter = 14.0f,
                Gap = 3.0f
            };
            ChamberRow.AddChild(ChamberLabel);
            ChamberRow.AddChild(ChamberPips);
            root.GetNodeOrNull<VBoxContainer>("SkillContainer/VBox")?.AddChild(ChamberRow);
        }
        else
        {
            ChamberLabel = ChamberRow.GetNodeOrNull<Label>("ChamberLabel");
            ChamberPips = ChamberRow.GetNodeOrNull<EnergyPips>("ChamberPips");
        }
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
        _skillLvTemplate = Tr("SKILL_LV");
        _chamberEnergyTemplate = Tr("LOADOUT_ENERGY_FMT");
        InvalidateSlotCache();

        if (HoveredSlotIdx >= 0 && SkillTooltip != null && GodotObject.IsInstanceValid(SkillTooltip) && SkillTooltip.Visible)
        {
            RefreshTooltipContent(HoveredSlotIdx);
        }

        UpdateSkillSlots();
    }

    /// <summary>Forces the next refresh to rewrite every slot (equip/language change).</summary>
    public void InvalidateSlotCache()
    {
        foreach (var entry in _slotCache)
        {
            entry.LastId = "\u0000uninit\u0000";
            entry.LastImagePath = "\u0000uninit\u0000";
            entry.LastBadge = "\u0000uninit\u0000";
            entry.LastLevel = int.MinValue;
            entry.LastCdRatio = -1.0f;
        }
        _lastChamberUsed = int.MinValue;
        _lastChamberMax = int.MinValue;
        _lastChamberGens = int.MinValue;
        _lastChamberPlayer = null;
    }

    private void EnsureSlotCache()
    {
        if (SlotsContainer == null)
            return;
        var children = SlotsContainer.GetChildren();
        if (_slotCache.Count != children.Count)
        {
            _slotCache.Clear();
            for (int i = 0; i < children.Count; i++)
                _slotCache.Add(new SlotCache());
        }
        for (int i = 0; i < children.Count; i++)
        {
            var entry = _slotCache[i];
            var card = children[i];
            entry.IconLbl ??= card.GetNodeOrNull<Label>("IconLabel");
            entry.BadgeLbl ??= card.GetNodeOrNull<Label>("BadgeLabel");
            entry.CdOverlay ??= card.GetNodeOrNull<ProgressBar>("CooldownBar");
            entry.IconTex ??= card.GetNodeOrNull<TextureRect>("IconTexture");
        }
    }

    private bool ResolvePlayer()
    {
        if (PlayerRef != null && GodotObject.IsInstanceValid(PlayerRef))
            return true;
        PlayerRef = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (PlayerRef == null)
            return false;
        return true;
    }

    private bool TryGetSkillManager(out SkillManager? typedSm)
    {
        typedSm = null;
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
            return false;
        var smNode = PlayerRef.GetNodeOrNull<Node>("SkillManager");
        if (smNode is SkillManager csharpSm)
        {
            typedSm = csharpSm;
            return true;
        }
        return false;
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

    /// <summary>
    /// Full structural refresh (equip / level / language change, tests).
    /// Per-frame work moved to <see cref="TickSkillSlots"/>; this stays
    /// synchronous so direct callers see immediate results.
    /// </summary>
    public void UpdateSkillSlots()
    {
        if (!ResolvePlayer())
            return;

        if (!TryGetSkillsData(out var skillsData))
            return;

        if (SlotsContainer == null)
            return;

        EnsureSlotCache();
        _skillLvTemplate ??= Tr("SKILL_LV");

        var slotChildren = SlotsContainer.GetChildren();
        int count = Mathf.Min(slotChildren.Count, skillsData.Count);

        for (int i = 0; i < count; i++)
        {
            var slotCard = slotChildren[i];
            var data = skillsData[i];
            var cache = _slotCache[i];

            string id = data.TryGetValue("id", out var idVal) ? idVal.AsString() : "";
            if (!string.IsNullOrEmpty(id))
            {
                if (cache.IconLbl != null)
                    cache.IconLbl.Visible = false;
                if (cache.IconTex == null)
                {
                    cache.IconTex = new TextureRect
                    {
                        Name = "IconTexture",
                        CustomMinimumSize = new Vector2(44, 44),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = Control.MouseFilterEnum.Ignore
                    };
                    slotCard.AddChild(cache.IconTex);
                    slotCard.MoveChild(cache.IconTex, 0);
                }
                string slotImagePath = data.TryGetValue("image_path", out var sipVal) ? sipVal.AsString() : "";
                if (cache.LastImagePath != slotImagePath || cache.IconTex.Texture == null)
                {
                    cache.IconTex.Texture = AssetLoader.TryLoad<Texture2D>(slotImagePath)
                        ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                    cache.LastImagePath = slotImagePath;
                }
                if (cache.LastId != id)
                    cache.IconTex.Modulate = new Color(1, 1, 1, 1);
                bool isPassive = data.TryGetValue("is_passive", out var passVal) && passVal.AsBool();
                int level = data.TryGetValue("level", out var lvVal) ? lvVal.AsInt32() : 1;

                if (cache.BadgeLbl != null && (cache.LastId != id || cache.LastLevel != level || cache.LastIsPassive != isPassive))
                {
                    string badge = TextFormatter.Format(_skillLvTemplate, level);
                    cache.BadgeLbl.Text = badge;
                    cache.LastBadge = badge;
                    cache.BadgeLbl.Modulate = (isPassive || i >= 5)
                        ? new Color(0.8f, 0.6f, 1.0f)
                        : new Color(1.0f, 0.9f, 0.3f);
                }
                if (cache.CdOverlay != null)
                {
                    float cdRatio = data.TryGetValue("cooldown_ratio", out var cdrVal) ? cdrVal.AsSingle() : 0.0f;
                    bool hasCd = !isPassive && cdRatio > 0.0f;
                    if (cache.LastHasCd != hasCd || cache.LastId != id)
                        cache.CdOverlay.Visible = hasCd;
                    if (hasCd && Mathf.Abs(cache.LastCdRatio - cdRatio) > 0.0005f)
                        cache.CdOverlay.Value = cdRatio;
                    else if (!hasCd && cache.LastHasCd)
                        cache.CdOverlay.Value = 0.0f;
                    cache.LastHasCd = hasCd;
                    cache.LastCdRatio = cdRatio;
                }
                cache.LastId = id;
                cache.LastLevel = level;
                cache.LastIsPassive = isPassive;
            }
            else
            {
                // Empty Slot: placeholder texture, no emoji.
                if (cache.IconLbl != null)
                    cache.IconLbl.Visible = false;
                if (cache.IconTex == null)
                {
                    cache.IconTex = new TextureRect
                    {
                        Name = "IconTexture",
                        CustomMinimumSize = new Vector2(44, 44),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = Control.MouseFilterEnum.Ignore
                    };
                    slotCard.AddChild(cache.IconTex);
                    slotCard.MoveChild(cache.IconTex, 0);
                }
                const string emptyKey = "\u0000empty\u0000";
                if (cache.LastImagePath != emptyKey || cache.IconTex.Texture == null)
                {
                    cache.IconTex.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                    cache.LastImagePath = emptyKey;
                }
                Color emptyMod = i >= 5
                    ? new Color(0.65f, 0.55f, 0.8f, 0.5f)
                    : new Color(0.4f, 0.5f, 0.6f, 0.6f);
                if (cache.LastId != "")
                    cache.IconTex.Modulate = emptyMod;
                if (cache.BadgeLbl != null && cache.LastId != "")
                {
                    cache.BadgeLbl.Text = "";
                    cache.LastBadge = "";
                }
                if (cache.CdOverlay != null && cache.LastHasCd)
                {
                    cache.CdOverlay.Visible = false;
                    cache.CdOverlay.Value = 0.0f;
                }
                cache.LastId = "";
                cache.LastLevel = 0;
                cache.LastIsPassive = i >= 5;
                cache.LastHasCd = false;
                cache.LastCdRatio = 0.0f;
            }
        }

        UpdateChamberRow();
    }

    /// <summary>
    /// Per-frame cheap path for <c>Hud._Process</c>: cooldown values plus
    /// structural fix-ups only for slots whose skill identity changed.
    /// No dictionaries, no translations, no texture loads on steady state.
    /// </summary>
    public void TickSkillSlots()
    {
        _tickCounter++;
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
        {
            // Throttle group lookups: the full refresh resolves on demand.
            if ((_tickCounter % 30) != 0)
                return;
            if (!ResolvePlayer())
                return;
        }
        if (SlotsContainer == null)
            return;
        EnsureSlotCache();
        if (_slotCache.Count == 0)
            return;

        if (!TryGetSkillManager(out var sm) || sm == null)
        {
            // GDScript fallback: throttled full refresh only.
            _fallbackTickCounter++;
            if (_fallbackTickCounter >= 15)
            {
                _fallbackTickCounter = 0;
                UpdateSkillSlots();
            }
            else
            {
                UpdateChamberRow();
            }
            return;
        }

        _skillLvTemplate ??= Tr("SKILL_LV");
        int slots = Mathf.Min(_slotCache.Count, SkillManager.MaxActiveSlots + SkillManager.MaxPassiveSlots);
        for (int i = 0; i < slots; i++)
        {
            bool activeRow = i < SkillManager.MaxActiveSlots;
            BaseSkill? skill = activeRow ? sm.ActiveSlots[i] : sm.PassiveSlots[i - SkillManager.MaxActiveSlots];
            if (skill != null && !GodotObject.IsInstanceValid(skill))
                skill = null;
            var cache = _slotCache[i];

            string curId = skill?.SkillId ?? "";
            int curLevel = skill?.Level ?? 0;
            bool curPassive = skill?.IsPassive ?? !activeRow;
            if (curId != cache.LastId || curLevel != cache.LastLevel || curPassive != cache.LastIsPassive)
            {
                RefreshTickStructure(i, skill, curId, curLevel, curPassive);
            }

            if (cache.CdOverlay == null)
                continue;
            if (skill == null || curPassive)
            {
                if (cache.LastHasCd)
                {
                    cache.CdOverlay.Visible = false;
                    cache.CdOverlay.Value = 0.0f;
                    cache.LastHasCd = false;
                    cache.LastCdRatio = 0.0f;
                }
                continue;
            }
            float effCd = skill.GetCalculatedCooldown();
            float ratio = effCd > 0.0f ? Mathf.Clamp(skill.CooldownTimer / effCd, 0.0f, 1.0f) : 0.0f;
            bool hasCd = ratio > 0.0f;
            if (hasCd != cache.LastHasCd)
                cache.CdOverlay.Visible = hasCd;
            if (hasCd)
            {
                if (Mathf.Abs(cache.LastCdRatio - ratio) > 0.004f)
                {
                    cache.CdOverlay.Value = ratio;
                    cache.LastCdRatio = ratio;
                }
            }
            else if (cache.LastHasCd)
            {
                cache.CdOverlay.Value = 0.0f;
                cache.LastCdRatio = 0.0f;
            }
            cache.LastHasCd = hasCd;
        }

        UpdateChamberRow();
    }

    /// <summary>Structural fix-up for one slot on the tick path (identity changed only).</summary>
    private void RefreshTickStructure(int i, BaseSkill? skill, string curId, int curLevel, bool curPassive)
    {
        var cache = _slotCache[i];
        if (SlotsContainer == null)
            return;
        var children = SlotsContainer.GetChildren();
        if (i < 0 || i >= children.Count)
            return;
        var slotCard = children[i];
        _skillLvTemplate ??= Tr("SKILL_LV");

        if (!string.IsNullOrEmpty(curId) && skill != null)
        {
            if (cache.IconLbl != null)
                cache.IconLbl.Visible = false;
            if (cache.IconTex == null)
            {
                cache.IconTex = new TextureRect
                {
                    Name = "IconTexture",
                    CustomMinimumSize = new Vector2(44, 44),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                slotCard.AddChild(cache.IconTex);
                slotCard.MoveChild(cache.IconTex, 0);
            }
            string path = AssetPaths.SkillIcon(curId);
            if (cache.LastImagePath != path || cache.IconTex.Texture == null)
            {
                cache.IconTex.Texture = AssetLoader.TryLoad<Texture2D>(path)
                    ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                cache.LastImagePath = path;
            }
            cache.IconTex.Modulate = new Color(1, 1, 1, 1);
            if (cache.BadgeLbl != null)
            {
                string badge = TextFormatter.Format(_skillLvTemplate, curLevel);
                cache.BadgeLbl.Text = badge;
                cache.LastBadge = badge;
                cache.BadgeLbl.Modulate = (curPassive || i >= 5)
                    ? new Color(0.8f, 0.6f, 1.0f)
                    : new Color(1.0f, 0.9f, 0.3f);
            }
            cache.LastId = curId;
            cache.LastLevel = curLevel;
            cache.LastIsPassive = curPassive;
        }
        else
        {
            if (cache.IconLbl != null)
                cache.IconLbl.Visible = false;
            if (cache.IconTex == null)
            {
                cache.IconTex = new TextureRect
                {
                    Name = "IconTexture",
                    CustomMinimumSize = new Vector2(44, 44),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                slotCard.AddChild(cache.IconTex);
                slotCard.MoveChild(cache.IconTex, 0);
            }
            const string emptyKey = "\u0000empty\u0000";
            if (cache.LastImagePath != emptyKey || cache.IconTex.Texture == null)
            {
                cache.IconTex.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                cache.LastImagePath = emptyKey;
            }
            cache.IconTex.Modulate = i >= 5
                ? new Color(0.65f, 0.55f, 0.8f, 0.5f)
                : new Color(0.4f, 0.5f, 0.6f, 0.6f);
            if (cache.BadgeLbl != null && cache.LastId != "")
            {
                cache.BadgeLbl.Text = "";
                cache.LastBadge = "";
            }
            if (cache.CdOverlay != null && cache.LastHasCd)
            {
                cache.CdOverlay.Visible = false;
                cache.CdOverlay.Value = 0.0f;
            }
            cache.LastId = "";
            cache.LastLevel = 0;
            cache.LastIsPassive = i >= 5;
            cache.LastHasCd = false;
            cache.LastCdRatio = 0.0f;
        }
    }

    /// <summary>
    /// Refreshes the chamber energy row from the player's chamber (Phase 2).
    /// Shown only once the player engages organelles; overloaded states tint red.
    /// Dirty-checked: translations and pip redraws only run on state change.
    /// </summary>
    public void UpdateChamberRow()
    {
        OrganelleChamber? chamber = null;
        if (PlayerRef != null && GodotObject.IsInstanceValid(PlayerRef))
            chamber = PlayerRef.GetNodeOrNull<OrganelleChamber>("OrganelleChamber");

        int used = chamber?.UsedEnergy ?? 0;
        int max = chamber?.MaxEnergy ?? OrganelleChamber.BaseEnergy;
        int gens = chamber?.GeneratorCount ?? 0;
        bool engaged = (chamber?.EquippedCount ?? 0) > 0 || (chamber?.Backpack.Count ?? 0) > 0;
        bool overloaded = used > max;

        if (ChamberRow != null && ChamberRow.Visible != engaged)
            ChamberRow.Visible = engaged;
        if (!engaged)
        {
            _lastChamberEngaged = false;
            _lastChamberPlayer = PlayerRef;
            return;
        }

        if (PlayerRef == _lastChamberPlayer && engaged == _lastChamberEngaged
            && used == _lastChamberUsed && max == _lastChamberMax && gens == _lastChamberGens
            && overloaded == _lastChamberOverloaded)
            return;

        _lastChamberPlayer = PlayerRef;
        _lastChamberEngaged = engaged;
        _lastChamberUsed = used;
        _lastChamberMax = max;
        _lastChamberGens = gens;
        _lastChamberOverloaded = overloaded;

        _chamberEnergyTemplate ??= Tr("LOADOUT_ENERGY_FMT");
        if (ChamberLabel != null)
        {
            ChamberLabel.Text = "⚡ " + TextFormatter.Format(_chamberEnergyTemplate, used, max);
            ChamberLabel.Modulate = overloaded
                ? new Color(1.0f, 0.45f, 0.45f)
                : new Color(0.45f, 0.92f, 1.0f);
        }
        if (ChamberPips != null)
            ChamberPips.Configure(max, used, gens);
    }

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
