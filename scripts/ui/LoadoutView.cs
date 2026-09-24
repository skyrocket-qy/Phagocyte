using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Pre-run loadout page (TODO Phase 1): the 6-category organelle vault on the
/// left and the 2x2 energy chamber on the right. A fresh cell deploys with no
/// equipment — the default profile is four empty slots. All legality checks run
/// through a scratch <see cref="OrganelleChamber"/>, so the page enforces
/// exactly the same energy/slot contract as the run itself.
/// </summary>
public partial class LoadoutView : Control
{
    [Signal]
    public delegate void ConfirmedEventHandler();

    public static PackedScene SlotScene => AssetLoader.Load<PackedScene>("res://scenes/ui/organelle_slot.tscn");

    /// <summary>Category ids in display order (data-owned values from organelles.json).</summary>
    public static readonly string[] Categories =
    {
        "metabolism", "digestion", "cytoskeleton", "synthesis", "sensing", "symbiosis"
    };

    public Label? HeaderLabel { get; set; }
    public HBoxContainer? ProfileHBox { get; set; }
    public Button? ProfileAddButton { get; set; }
    public Button? ProfileDeleteButton { get; set; }
    public Label? BackpackHeaderLabel { get; set; }
    public GridContainer? CategoryTabBar { get; set; }
    public GridContainer? BackpackGrid { get; set; }
    public Label? HintLabel { get; set; }
    public Label? ChamberHeaderLabel { get; set; }
    public EnergyPips? EnergyBar { get; set; }
    public GridContainer? ChamberGrid { get; set; }
    public Button? ResetButton { get; set; }
    public Button? ConfirmButton { get; set; }
    public StatPreviewPanel? StatPanel { get; set; }

    private OrganelleChamber? _chamber;
    private CharacterBody2D? _scratchHost;
    private string _classKey = "macrophage";
    private int _categoryFilter = -1;
    private ButtonGroup? _profileGroup;
    private readonly List<Button> _profileTabs = new();
    private readonly List<Button> _categoryTabs = new();
    private readonly List<OrganelleSlot> _chamberCards = new();
    private readonly List<OrganelleSlot> _backpackCards = new();
    private readonly List<string> _backpackIds = new();
    private OrganelleTooltip? _tooltip;

    /// <summary>Bio-socket look for the 2x2 chamber: rounded frames with a
    /// glowing cyan rim (symmetric corners so slots never read as tilted).
    /// Empty sockets use the dim variant; filled ones the bright variant.</summary>
    private static readonly StyleBoxFlat SocketNormalStyle = MakeSocketStyle(
        new Color(0.04f, 0.09f, 0.14f, 0.85f), new Color(0.30f, 0.85f, 0.95f, 0.65f), 2.0f, 6.0f);
    private static readonly StyleBoxFlat SocketEmptyNormalStyle = MakeSocketStyle(
        new Color(0.05f, 0.10f, 0.16f, 0.90f), new Color(0.35f, 0.80f, 0.95f, 0.70f), 2.0f, 0.0f);
    private static readonly StyleBoxFlat SocketHoverStyle = MakeSocketStyle(
        new Color(0.06f, 0.14f, 0.22f, 0.92f), new Color(0.45f, 0.98f, 1.0f, 0.95f), 2.0f, 10.0f);
    private static readonly StyleBoxFlat SocketPressedStyle = MakeSocketStyle(
        new Color(0.08f, 0.18f, 0.28f, 1.0f), new Color(0.60f, 1.0f, 1.0f, 1.0f), 2.5f, 12.0f);

    private static StyleBoxFlat MakeSocketStyle(Color bg, Color border, float borderWidth = 2.0f, float shadowSize = 8.0f)
    {
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = (int)borderWidth,
            BorderWidthTop = (int)borderWidth,
            BorderWidthRight = (int)borderWidth,
            BorderWidthBottom = (int)borderWidth,
            ShadowColor = new Color(0.0f, 0.85f, 1.0f, 0.18f),
            ContentMarginLeft = 6.0f,
            ContentMarginTop = 6.0f,
            ContentMarginRight = 6.0f,
            ContentMarginBottom = 6.0f
        };
        style.SetCornerRadiusAll(10);
        if (shadowSize > 0.0f)
            style.ShadowSize = (int)shadowSize;
        return style;
    }

    /// <summary>Capsule profile-tag look: translucent fill with a cyan hairline
    /// and Scheme B asymmetric chamfer.</summary>
    private static readonly StyleBoxFlat ProfileNormalStyle = MakeCapsuleStyle(
        new Color(0.03f, 0.07f, 0.11f, 0.50f), new Color(0.25f, 0.70f, 0.90f, 0.50f));
    private static readonly StyleBoxFlat ProfileHoverStyle = MakeCapsuleStyle(
        new Color(0.06f, 0.14f, 0.20f, 0.75f), new Color(0.35f, 0.90f, 1.0f, 0.85f));
    private static readonly StyleBoxFlat ProfilePressedStyle = MakeCapsuleStyle(
        new Color(0.08f, 0.18f, 0.26f, 0.90f), new Color(0.50f, 0.95f, 1.0f, 0.95f));
    private static readonly Color ProfileInactiveFont = new(0.75f, 0.85f, 0.90f);
    private static readonly Color ProfileActiveFont = new(0.94f, 0.99f, 0.98f);

    private static StyleBoxFlat MakeCapsuleStyle(Color bg, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 3,
            ContentMarginLeft = 14.0f,
            ContentMarginRight = 14.0f
        };
    }

    private static void ApplyCapsuleStyle(Button btn)
    {
        btn.AddThemeStyleboxOverride("normal", ProfileNormalStyle);
        btn.AddThemeStyleboxOverride("hover", ProfileHoverStyle);
        btn.AddThemeStyleboxOverride("pressed", ProfilePressedStyle);
        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        btn.AddThemeColorOverride("font_color", ProfileInactiveFont);
        btn.AddThemeColorOverride("font_hover_color", ProfileActiveFont);
        btn.AddThemeColorOverride("font_pressed_color", ProfileActiveFont);
    }

    public override void _Ready()
    {
        HeaderLabel = GetNodeOrNull<Label>("Margin/VBox/TopBar/TitleVBox/HeaderLabel");
        ProfileHBox = GetNodeOrNull<HBoxContainer>("Margin/VBox/ContentHBox/ChamberSide/ProfileHBox");
        ProfileAddButton = GetNodeOrNull<Button>("Margin/VBox/ContentHBox/ChamberSide/ProfileHBox/ProfileAddButton");
        ProfileDeleteButton = GetNodeOrNull<Button>("Margin/VBox/ContentHBox/ChamberSide/ProfileHBox/ProfileDeleteButton");
        BackpackHeaderLabel = GetNodeOrNull<Label>("Margin/VBox/ContentHBox/VaultSide/BackpackHeader");
        CategoryTabBar = GetNodeOrNull<GridContainer>("Margin/VBox/ContentHBox/VaultSide/CategoryTabBar");
        BackpackGrid = GetNodeOrNull<GridContainer>("Margin/VBox/ContentHBox/VaultSide/VaultScroll/BackpackGrid");
        HintLabel = GetNodeOrNull<Label>("Margin/VBox/ContentHBox/VaultSide/HintLabel");
        ChamberHeaderLabel = GetNodeOrNull<Label>("Margin/VBox/ContentHBox/ChamberSide/ChamberHeader");
        EnergyBar = GetNodeOrNull<EnergyPips>("Margin/VBox/ContentHBox/ChamberSide/EnergyBar");
        ChamberGrid = GetNodeOrNull<GridContainer>("Margin/VBox/ContentHBox/ChamberSide/SocketWell/WellMargin/ChamberGrid");
        ResetButton = GetNodeOrNull<Button>("Margin/VBox/Buttons/ResetButton");
        ConfirmButton = GetNodeOrNull<Button>("Margin/VBox/Buttons/ConfirmButton");
        StatPanel = GetNodeOrNull<StatPreviewPanel>("Margin/VBox/ContentHBox/StatPreview");

        BuildCategoryTabs();
        BuildChamberCards();
        BuildBackpackCards();

        if (ResetButton != null)
            ResetButton.Pressed += OnResetPressed;
        if (ConfirmButton != null)
            ConfirmButton.Pressed += () => EmitSignal(SignalName.Confirmed);
        if (ProfileAddButton != null)
            ProfileAddButton.Pressed += OnProfileAddPressed;
        if (ProfileDeleteButton != null)
            ProfileDeleteButton.Pressed += OnProfileDeletePressed;
        if (ProfileAddButton != null)
            ApplyCapsuleStyle(ProfileAddButton);
        if (ProfileDeleteButton != null)
        {
            ApplyCapsuleStyle(ProfileDeleteButton);
            ProfileDeleteButton.AddThemeColorOverride("font_color", new Color(1.0f, 0.62f, 0.58f));
        }

        // PoE-style cursor tooltip (owns hover info; engine tooltips stay
        // cleared on these cards so the two never double up). Added last so
        // it draws above every card.
        _tooltip = new OrganelleTooltip { Name = "OrganelleTooltip" };
        AddChild(_tooltip);

        UpdateLocalizedTexts();
    }

    // ------------------------------------------------------------------
    // Public API (MainMenu)
    // ------------------------------------------------------------------

    /// <summary>Opens the page for a cell, loading its active profile.</summary>
    public void Open(string classKey)
    {
        _classKey = string.IsNullOrEmpty(classKey) ? "macrophage" : classKey;
        RebuildScratch();
        RefreshAll();
    }

    /// <summary>Currently edited cell id.</summary>
    public string ClassKey => _classKey;

    /// <summary>Chamber snapshot for tests/inspection (never null after _Ready).</summary>
    public OrganelleChamber? Chamber => _chamber;

    public void UpdateLocalizedTexts()
    {
        if (HeaderLabel != null) HeaderLabel.Text = Tr("LOADOUT_HEADER");
        if (ChamberHeaderLabel != null) ChamberHeaderLabel.Text = Tr("LOADOUT_CHAMBER_HEADER");
        if (ResetButton != null) ResetButton.Text = Tr("LOADOUT_RESET");
        if (ConfirmButton != null) ConfirmButton.Text = Tr("LOADOUT_CONFIRM");
        if (ProfileAddButton != null) ProfileAddButton.Text = Tr("TREE_PROFILE_ADD");
        if (ProfileDeleteButton != null) ProfileDeleteButton.Text = Tr("TREE_PROFILE_DELETE");
        StatPanel?.UpdateLocalizedTexts();

        RefreshCategoryTabs();
        RefreshAll();
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    private void BuildCategoryTabs()
    {
        if (CategoryTabBar == null)
            return;

        _categoryTabs.Clear();
        for (int i = -1; i < Categories.Length; i++)
        {
            int filter = i;
            string tabName = filter < 0 ? "CategoryTabAll" : $"CategoryTab_{Categories[filter]}";
            var tab = CategoryTabBar.GetNodeOrNull<Button>(tabName);
            if (tab == null)
                continue;
            ApplyCapsuleStyle(tab);
            tab.AddThemeFontSizeOverride("font_size", 14);
            tab.Pressed += () => OnCategoryTabPressed(filter);
            tab.ButtonPressed = filter == _categoryFilter;
            _categoryTabs.Add(tab);
        }
    }

    private void BuildChamberCards()
    {
        if (ChamberGrid == null)
            return;

        _chamberCards.Clear();
        for (int i = 0; i < OrganelleChamber.MaxSlots; i++)
        {
            int slot = i;
            var card = ChamberGrid.GetNodeOrNull<OrganelleSlot>($"ChamberSlot{slot}");
            if (card == null)
                continue;
            card.AddThemeStyleboxOverride("normal", SocketNormalStyle);
            card.AddThemeStyleboxOverride("hover", SocketHoverStyle);
            card.AddThemeStyleboxOverride("pressed", SocketPressedStyle);
            card.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
            card.Pressed += () => OnChamberCardPressed(slot);
            card.MouseEntered += () => _tooltip?.ShowFor(_chamber?.GetSlot(slot) ?? "");
            card.MouseExited += () => _tooltip?.HideTip();
            _chamberCards.Add(card);
        }
    }

    private void BuildBackpackCards()
    {
        if (BackpackGrid == null)
            return;

        _backpackCards.Clear();
        _backpackIds.Clear();

        // Stable display order: category order, then id.
        var ids = new List<string>();
        foreach (string id in GameManager.OrganelleCatalog.Keys)
            ids.Add(id);
        ids.Sort((a, b) =>
        {
            int catA = CategoryIndex(CategoryOf(a));
            int catB = CategoryIndex(CategoryOf(b));
            return catA != catB ? catA.CompareTo(catB) : string.CompareOrdinal(a, b);
        });

        foreach (string id in ids)
        {
            string localId = id;
            var card = SlotScene.Instantiate<OrganelleSlot>();
            card.Name = $"BackpackSlot_{localId}";
            card.Pressed += () => OnBackpackCardPressed(localId);
            card.MouseEntered += () => _tooltip?.ShowFor(localId);
            card.MouseExited += () => _tooltip?.HideTip();
            BackpackGrid.AddChild(card);
            _backpackCards.Add(card);
            _backpackIds.Add(localId);
        }
    }

    private static int CategoryIndex(string category)
    {
        for (int i = 0; i < Categories.Length; i++)
        {
            if (Categories[i] == category)
                return i;
        }
        return Categories.Length;
    }

    private static string CategoryOf(string id)
    {
        return GameManager.OrganelleCatalog.TryGetValue(id, out var entryVar)
            ? entryVar.AsGodotDictionary()["category"].AsString()
            : "";
    }

    // ------------------------------------------------------------------
    // Scratch chamber (single source of truth for the rules)
    // ------------------------------------------------------------------

    private void RebuildScratch()
    {
        if (_scratchHost != null && IsInstanceValid(_scratchHost))
        {
            RemoveChild(_scratchHost);
            _scratchHost.Free();
        }

        _scratchHost = new CharacterBody2D { Name = "LoadoutScratchHost" };
        _scratchHost.AddChild(new CellStats { Name = "CellStats" });
        _chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        _scratchHost.AddChild(_chamber);
        AddChild(_scratchHost);
        _chamber.Setup(_scratchHost);

        // Only unlocked organelles are owned; the vault shows the rest as locked.
        foreach (string id in GameManager.OrganelleCatalog.Keys)
        {
            if (OrganelleUnlockManager.IsUnlocked(id))
                _chamber.AddToBackpack(id);
        }

        string[] slots = LoadoutManager.GetSlots(_classKey, LoadoutManager.GetActiveProfile(_classKey));
        for (int i = 0; i < slots.Length && i < OrganelleChamber.MaxSlots; i++)
        {
            if (string.IsNullOrEmpty(slots[i]))
                continue;
            _chamber.Equip(slots[i], i);
        }
    }

    // ------------------------------------------------------------------
    // Interactions
    // ------------------------------------------------------------------

    private void OnCategoryTabPressed(int filter)
    {
        _categoryFilter = filter;
        RefreshCategoryTabs();
        RefreshBackpackCards();
    }

    private void OnBackpackCardPressed(string id)
    {
        ToggleOrganelle(id);
    }

    /// <summary>
    /// Equips an organelle into the first free slot, or unequips it when already
    /// equipped. Returns true when the chamber changed; refusals surface through
    /// the hint label (locked / overload / full slots) plus an error sting.
    /// </summary>
    public bool ToggleOrganelle(string id)
    {
        if (_chamber == null || string.IsNullOrEmpty(id))
            return false;

        if (!OrganelleUnlockManager.IsUnlocked(id))
        {
            ShowHint("LOADOUT_HINT_LOCKED");
            FlashEnergy(new Color(1.0f, 0.72f, 0.35f));
            AudioManager.Instance?.PlayError();
            return false;
        }

        // Already equipped: the click unequips it. A generator whose removal
        // would overload is refused with the overload hint (same as equipping).
        for (int i = 0; i < OrganelleChamber.MaxSlots; i++)
        {
            if (_chamber.GetSlot(i) != id)
                continue;
            if (!_chamber.Unequip(i))
            {
                _chamber.CanUnequip(i, out string unequipReason);
                ShowHint(ReasonKey(unequipReason));
                FlashEnergy(new Color(1.0f, 0.35f, 0.35f));
                AudioManager.Instance?.PlayError();
                return false;
            }
            AudioManager.Instance?.PlayUnequip();
            Persist();
            RefreshAll();
            return true;
        }

        int free = -1;
        for (int i = 0; i < OrganelleChamber.MaxSlots; i++)
        {
            if (string.IsNullOrEmpty(_chamber.GetSlot(i)))
            {
                free = i;
                break;
            }
        }
        if (free < 0)
        {
            ShowHint("LOADOUT_HINT_SLOTS_FULL");
            FlashEnergy(new Color(1.0f, 0.55f, 0.45f));
            AudioManager.Instance?.PlayError();
            return false;
        }

        if (!_chamber.Equip(id, free))
        {
            _chamber.CanEquip(id, free, out string reason);
            ShowHint(ReasonKey(reason));
            FlashEnergy(new Color(1.0f, 0.35f, 0.35f));
            AudioManager.Instance?.PlayError();
            return false;
        }

        AudioManager.Instance?.PlaySocket();
        ShowHint("LOADOUT_HINT_DEFAULT");
        Persist();
        RefreshAll();
        return true;
    }

    /// <summary>
    /// Unequips every slot and persists the cleared profile. Generators refuse
    /// while the remaining load would overload, so sweep until a full pass
    /// changes nothing (consumers leave first, generators follow).
    /// </summary>
    public void ResetLoadout()
    {
        if (_chamber == null)
            return;
        for (int pass = 0; pass <= OrganelleChamber.MaxSlots; pass++)
        {
            bool changed = false;
            for (int i = 0; i < OrganelleChamber.MaxSlots; i++)
            {
                if (string.IsNullOrEmpty(_chamber.GetSlot(i)))
                    continue;
                if (_chamber.Unequip(i))
                    changed = true;
            }
            if (!changed)
                break;
        }
        Persist();
        ShowHint("LOADOUT_HINT_DEFAULT");
        RefreshAll();
    }

    private void OnChamberCardPressed(int slot)
    {
        if (_chamber == null || string.IsNullOrEmpty(_chamber.GetSlot(slot)))
            return;
        if (!_chamber.Unequip(slot))
        {
            _chamber.CanUnequip(slot, out string reason);
            ShowHint(ReasonKey(reason));
            FlashEnergy(new Color(1.0f, 0.35f, 0.35f));
            AudioManager.Instance?.PlayError();
            return;
        }
        AudioManager.Instance?.PlayUnequip();
        Persist();
        RefreshAll();
    }

    private void OnResetPressed()
    {
        ResetLoadout();
    }

    private void OnProfileTabPressed(int index)
    {
        LoadoutManager.SetActiveProfile(_classKey, index);
        RebuildScratch();
        RefreshAll();
    }

    private void OnProfileAddPressed()
    {
        if (!LoadoutManager.AddProfile(_classKey))
            return;
        RebuildScratch();
        RefreshAll();
    }

    private void OnProfileDeletePressed()
    {
        int active = LoadoutManager.GetActiveProfile(_classKey);
        if (!LoadoutManager.DeleteProfile(_classKey, active))
            return;
        RebuildScratch();
        RefreshAll();
    }

    private void Persist()
    {
        if (_chamber == null)
            return;

        var slots = new string[OrganelleChamber.MaxSlots];
        for (int i = 0; i < OrganelleChamber.MaxSlots; i++)
            slots[i] = _chamber.GetSlot(i);
        LoadoutManager.SetSlots(_classKey, LoadoutManager.GetActiveProfile(_classKey), slots);
    }

    // ------------------------------------------------------------------
    // Refresh
    // ------------------------------------------------------------------

    private void RefreshAll()
    {
        _tooltip?.HideTip();
        RefreshProfileTabs();
        RefreshChamberCards();
        RefreshEnergy();
        RefreshBackpackCards();
        RefreshCategoryTabs();
        RefreshVaultHeader();
        StatPanel?.Refresh(_classKey);
    }

    /// <summary>Vault header carries the collection progress (unlocked / total).</summary>
    private void RefreshVaultHeader()
    {
        if (BackpackHeaderLabel == null)
            return;
        BackpackHeaderLabel.Text = Tr("LOADOUT_BACKPACK_HEADER") + "   "
            + TextFormatter.Format(Tr("LOADOUT_VAULT_PROGRESS"),
                OrganelleUnlockManager.UnlockedCount, GameManager.OrganelleCatalog.Count);
    }

    private void RefreshProfileTabs()
    {
        if (ProfileHBox == null)
            return;

        _profileGroup ??= new ButtonGroup { AllowUnpress = false };

        foreach (var old in _profileTabs)
        {
            if (IsInstanceValid(old))
            {
                ProfileHBox.RemoveChild(old);
                old.QueueFree();
            }
        }
        _profileTabs.Clear();

        int count = LoadoutManager.GetProfileCount(_classKey);
        int active = LoadoutManager.GetActiveProfile(_classKey);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var tab = new Button
            {
                Name = $"ProfileTab{index}",
                CustomMinimumSize = new Vector2(104, 40),
                ToggleMode = true,
                ButtonGroup = _profileGroup,
                ButtonPressed = index == active,
                MouseDefaultCursorShape = CursorShape.PointingHand
            };
            tab.AddThemeFontSizeOverride("font_size", 14);
            ApplyCapsuleStyle(tab);
            tab.Pressed += () => OnProfileTabPressed(index);
            _profileTabs.Add(tab);
            ProfileHBox.AddChild(tab);
            ProfileHBox.MoveChild(tab, index);
        }

        RefreshProfileTexts();
    }

    private void RefreshProfileTexts()
    {
        int count = LoadoutManager.GetProfileCount(_classKey);
        int active = LoadoutManager.GetActiveProfile(_classKey);
        for (int i = 0; i < _profileTabs.Count && i < count; i++)
        {
            _profileTabs[i].Text = LoadoutManager.GetProfileName(i);
            _profileTabs[i].ButtonPressed = i == active;
        }
        if (ProfileAddButton != null)
            ProfileAddButton.Visible = count < LoadoutManager.MaxProfiles;
        if (ProfileDeleteButton != null)
            ProfileDeleteButton.Disabled = count <= 1;
    }

    private void RefreshChamberCards()
    {
        for (int i = 0; i < _chamberCards.Count; i++)
        {
            string id = _chamber?.GetSlot(i) ?? "";
            bool empty = string.IsNullOrEmpty(id);
            _chamberCards[i].AddThemeStyleboxOverride("normal",
                empty ? SocketEmptyNormalStyle : SocketNormalStyle);
            _chamberCards[i].ShowOrganelle(id, !empty, true);
            _chamberCards[i].TooltipText = "";
        }
    }

    private void RefreshEnergy()
    {
        int used = _chamber?.UsedEnergy ?? 0;
        int max = _chamber?.MaxEnergy ?? OrganelleChamber.BaseEnergy;
        int generators = _chamber?.GeneratorCount ?? 0;

        if (EnergyBar != null)
        {
            // One disc per energy point: consumed discs filled, the trailing
            // generator-granted discs red.
            EnergyBar.Configure(max, used, generators);
        }
    }

    private void RefreshBackpackCards()
    {
        for (int i = 0; i < _backpackCards.Count; i++)
        {
            string id = _backpackIds[i];
            string category = CategoryOf(id);
            bool visible = _categoryFilter < 0 || Categories[_categoryFilter] == category;
            _backpackCards[i].Visible = visible;
            if (!visible)
                continue;

            bool equipped = false;
            if (_chamber != null)
            {
                for (int s = 0; s < OrganelleChamber.MaxSlots; s++)
                {
                    if (_chamber.GetSlot(s) == id)
                    {
                        equipped = true;
                        break;
                    }
                }
            }
            _backpackCards[i].ShowOrganelle(id, equipped, OrganelleUnlockManager.IsUnlocked(id));
            _backpackCards[i].TooltipText = "";
        }
    }

    private void RefreshCategoryTabs()
    {
        for (int i = 0; i < _categoryTabs.Count; i++)
        {
            int filter = i - 1;
            string text = filter < 0 ? Tr("LOADOUT_CATEGORY_ALL") : Tr("ORGANELLE_CAT_" + Categories[filter].ToUpperInvariant());
            _categoryTabs[i].Text = text;
            _categoryTabs[i].ButtonPressed = filter == _categoryFilter;
            if (filter >= 0)
            {
                _categoryTabs[i].AddThemeColorOverride("font_color", OrganelleSlot.CategoryColor(Categories[filter]));
                _categoryTabs[i].AddThemeColorOverride("font_pressed_color", Colors.White);
                _categoryTabs[i].AddThemeColorOverride("font_hover_color", Colors.White);
            }
        }
    }

    private void ShowHint(string key)
    {
        if (HintLabel != null)
            HintLabel.Text = Tr(key);
    }

    private void FlashEnergy(Color color)
    {
        if (EnergyBar == null)
            return;
        EnergyBar.Modulate = color;
        CreateTween().TweenProperty(EnergyBar, "modulate", Colors.White, 0.45f);
    }

    private static string ReasonKey(string reason)
    {
        return reason switch
        {
            "overload" => "LOADOUT_HINT_OVERLOAD",
            "copy_cap" or "already_equipped" => "LOADOUT_HINT_COPY_CAP",
            "unknown" => "LOADOUT_HINT_UNKNOWN",
            "bad_slot" => "LOADOUT_HINT_BAD_SLOT",
            _ => "LOADOUT_HINT_DEFAULT"
        };
    }
}
