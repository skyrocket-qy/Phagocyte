using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Level-Up 3-Choice Epigenetic Mutation Modal.
/// Pauses the game, displays 3 distinct choices (Active/Passive/Gear).
/// A gear card that cannot auto-equip opens an in-modal swap step:
/// replace one chamber slot, store the item to the run backpack, or discard
/// it for a small heal.
/// </summary>
public partial class UpgradeModal : ModalBase
{
    [Signal]
    public delegate void ChoiceAppliedEventHandler(Dictionary choice);

    /// <summary>Discarded gear heals this fraction of max HP (Phase 2).</summary>
    public const float DiscardHealRatio = 0.15f;

    public Label? SubtitleLabel { get; set; }
    public HBoxContainer? CardsContainer { get; set; }
    public VBoxContainer? SwapPanel { get; set; }
    public Label? SwapTitleLabel { get; set; }
    public Label? SwapCandidateLabel { get; set; }
    public Label? SwapEnergyLabel { get; set; }
    public EnergyPips? SwapEnergyPips { get; set; }
    public GridContainer? SwapSlotsContainer { get; set; }
    public Label? SwapHintLabel { get; set; }
    public Button? SwapStoreButton { get; set; }
    public Button? SwapDiscardButton { get; set; }
    public Button? SwapCancelButton { get; set; }

    /// <summary>True while the modal is resolving a gear placement.</summary>
    public bool IsSwapMode => _pendingGear != "";

    // Scene-built modal without a header: title lives inside the center VBox.
    protected override string? TitleLabelPath => null;
    protected override string? CloseButtonPath => null;

    private Array<Dictionary> _currentChoices = new();
    public Array<Dictionary> CurrentChoices => _currentChoices;
    private Node2D? _playerRef = null;
    private int _pendingLevels = 0;
    private string _pendingGear = "";
    private readonly System.Collections.Generic.List<GearSlot> _swapCards = new();

    public override void _Ready()
    {
        TitleLabel = GetNodeOrNull<Label>("CenterContainer/VBox/TitleLabel");
        SubtitleLabel = GetNodeOrNull<Label>("CenterContainer/VBox/SubtitleLabel");
        CardsContainer = GetNodeOrNull<HBoxContainer>("CenterContainer/VBox/CardsContainer");
        SwapPanel = GetNodeOrNull<VBoxContainer>("CenterContainer/VBox/SwapPanel");
        SwapTitleLabel = GetNodeOrNull<Label>("CenterContainer/VBox/SwapPanel/SwapTitle");
        SwapCandidateLabel = GetNodeOrNull<Label>("CenterContainer/VBox/SwapPanel/SwapCandidate");
        SwapEnergyLabel = GetNodeOrNull<Label>("CenterContainer/VBox/SwapPanel/SwapEnergyRow/SwapEnergyLabel");
        SwapEnergyPips = GetNodeOrNull<EnergyPips>("CenterContainer/VBox/SwapPanel/SwapEnergyRow/SwapEnergyPips");
        SwapSlotsContainer = GetNodeOrNull<GridContainer>("CenterContainer/VBox/SwapPanel/SwapSlots");
        SwapHintLabel = GetNodeOrNull<Label>("CenterContainer/VBox/SwapPanel/SwapHint");
        SwapStoreButton = GetNodeOrNull<Button>("CenterContainer/VBox/SwapPanel/SwapRow/StoreButton");
        SwapDiscardButton = GetNodeOrNull<Button>("CenterContainer/VBox/SwapPanel/SwapRow/DiscardButton");
        SwapCancelButton = GetNodeOrNull<Button>("CenterContainer/VBox/SwapPanel/SwapRow/CancelButton");

        SetupCardListeners();
        if (SwapStoreButton != null)
            SwapStoreButton.Pressed += OnSwapStorePressed;
        if (SwapDiscardButton != null)
            SwapDiscardButton.Pressed += OnSwapDiscardPressed;
        if (SwapCancelButton != null)
            SwapCancelButton.Pressed += OnSwapCancelPressed;
        InitModal();
    }

    private void SetupCardListeners()
    {
        if (CardsContainer == null)
            return;

        var cards = CardsContainer.GetChildren();
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            var btn = card.GetNodeOrNull<Button>("SelectButton");
            if (btn != null)
            {
                int idx = i;
                btn.Pressed += () => OnCardClicked(idx);
            }
        }
        AudioManager.Instance?.WireClicks(this);
    }

    public void OpenUpgradeModal(Node2D player)
    {
        _playerRef = player;
        if (Visible)
        {
            _pendingLevels += 1;
            return;
        }

        ShowNextUpgrade();
    }

    public void ShowNextUpgrade()
    {
        if (_playerRef == null || !GodotObject.IsInstanceValid(_playerRef))
            return;

        ShowChoices(UpgradeManager.GenerateChoices(_playerRef, 3));
    }

    /// <summary>Displays an explicit set of cards (catalyst cue testing / scripted drafts).</summary>
    public void ShowChoices(Array<Dictionary> choices)
    {
        _currentChoices = choices;
        if (_currentChoices.Count == 0)
            return;

        PopulateCards();
        Visible = true;
        PauseManager.PushHold(GetTree(), PauseManager.UpgradeDraft);
    }

    private void PopulateCards()
    {
        UpdateLocalizedTexts();

        if (CardsContainer == null)
            return;

        var cards = CardsContainer.GetChildren();
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] is not Control card)
                continue;

            if (i < _currentChoices.Count)
            {
                var choice = _currentChoices[i];
                card.Visible = true;

                var iconLbl = card.GetNodeOrNull<Label>("VBox/IconLabel");
                var iconTex = card.GetNodeOrNull<TextureRect>("VBox/IconTexture");
                if (iconTex == null)
                {
                    var vbox = card.GetNodeOrNull<VBoxContainer>("VBox");
                    iconTex = new TextureRect
                    {
                        Name = "IconTexture",
                        CustomMinimumSize = new Vector2(64, 64),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = MouseFilterEnum.Ignore
                    };
                    if (vbox != null)
                    {
                        vbox.AddChild(iconTex);
                        vbox.MoveChild(iconTex, 0);
                    }
                }
                var titleLbl = card.GetNodeOrNull<Label>("VBox/TitleLabel");
                var badgeLbl = card.GetNodeOrNull<Label>("VBox/BadgeLabel");
                var descLbl = card.GetNodeOrNull<Label>("VBox/DescLabel");

                if (iconLbl != null)
                    iconLbl.Visible = false;
                if (iconTex != null)
                {
                    string imagePath = choice.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : "";
                    if (string.IsNullOrEmpty(imagePath) && choice.TryGetValue("id", out var idVal))
                    {
                        string id = idVal.AsString();
                        iconTex.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.SkillIcon(id))
                            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.GearIcon(id))
                            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                    }
                    else
                    {
                        iconTex.Texture = AssetLoader.TryLoad<Texture2D>(imagePath)
                            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                    }
                }
                if (titleLbl != null)
                {
                    string nameKey = choice.TryGetValue("name", out var nVal) ? nVal.AsString() : "";
                    titleLbl.Text = Tr(nameKey);
                }
                if (badgeLbl != null)
                {
                    // Slot templates carry hardcoded badge colors: neutralize so
                    // the type modulate below is the single source of truth.
                    badgeLbl.AddThemeColorOverride("font_color", Colors.White);
                    string bType = choice.TryGetValue("type", out var btVal) ? btVal.AsString() : "";
                    if (bType == "new_active")
                    {
                        badgeLbl.Text = "[ " + Tr("BADGE_NEW_ACTIVE") + " ]";
                        badgeLbl.Modulate = new Color(0.9f, 0.4f, 0.4f);
                    }
                    else if (bType == "new_passive")
                    {
                        badgeLbl.Text = "[ " + Tr("BADGE_NEW_PASSIVE") + " ]";
                        badgeLbl.Modulate = new Color(0.4f, 0.9f, 0.6f);
                    }
                    else if (bType.StartsWith("upgrade"))
                    {
                        int lvl = choice.TryGetValue("level", out var lVal) ? lVal.AsInt32() : 2;
                        badgeLbl.Text = $"[ {Tr("BADGE_UPGRADE")} Lv.{lvl} ]";
                        badgeLbl.Modulate = new Color(1.0f, 0.85f, 0.3f);
                    }
                    else
                    {
                        string badgeKey = choice.TryGetValue("badge", out var bgVal) ? bgVal.AsString() : "BADGE_UPGRADE";
                        badgeLbl.Text = "[ " + Tr(badgeKey) + " ]";
                        badgeLbl.Modulate = new Color(0.5f, 0.8f, 1.0f);
                    }
                }
                if (descLbl != null)
                {
                    string descKey = choice.TryGetValue("desc", out var dVal) ? dVal.AsString() : "";
                    int descLvl = choice.TryGetValue("level", out var dlVal) ? dlVal.AsInt32() : 0;
                    descLbl.Text = descKey == "UPGRADE_TO_LV"
                        ? TextFormatter.Format(Tr(descKey), descLvl)
                        : Tr(descKey);
                    // Gear cards carry their energy cost under the description.
                    if (choice.TryGetValue("energy_cost", out var costVal))
                    {
                        int cost = costVal.AsInt32();
                        descLbl.Text += "\n" + Tr("LOADOUT_COST_LABEL") + ": "
                            + (cost < 0 ? $"+{-cost}" : cost.ToString());
                    }
                }

                // Cue 4 (docs/tutorial.md §2): maxed active + paired passive shows
                // the golden catalyst resonance aura and corner tag.
                bool isCatalyst = choice.TryGetValue("catalyst", out var catVal) && catVal.AsBool();
                ApplyCatalystAura(card, badgeLbl, isCatalyst);
            }
            else
            {
                card.Visible = false;
            }
        }
    }

    /// <summary>Refreshes the static header; card texts refresh on the next open.</summary>
    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (TitleLabel != null)
            TitleLabel.Text = Tr("UPGRADE_MODAL_TITLE");
        if (SubtitleLabel != null)
            SubtitleLabel.Text = Tr("UPGRADE_MODAL_SUBTITLE");

        if (SwapTitleLabel != null)
            SwapTitleLabel.Text = Tr("SWAP_TITLE");
        if (SwapCandidateLabel != null)
            SwapCandidateLabel.Text = SwapCandidateText();
        if (SwapStoreButton != null)
            SwapStoreButton.Text = Tr("SWAP_STORE");
        if (SwapDiscardButton != null)
            SwapDiscardButton.Text = Tr("SWAP_DISCARD");
        if (SwapCancelButton != null)
            SwapCancelButton.Text = Tr("SWAP_CANCEL");
        if (SwapHintLabel != null && string.IsNullOrEmpty(SwapHintLabel.Text))
            SwapHintLabel.Text = Tr("SWAP_HINT");
    }

    private string SwapCandidateText()
    {
        if (string.IsNullOrEmpty(_pendingGear)
            || !GameManager.GearCatalog.TryGetValue(_pendingGear, out var entryVar))
        {
            return "";
        }
        var entry = entryVar.AsGodotDictionary();
        int cost = entry["energy_cost"].AsInt32();
        string costText = cost < 0 ? $"+{-cost}" : cost.ToString();
        return Tr(entry["name_key"].AsString()) + "   " + Tr("LOADOUT_COST_LABEL") + ": " + costText;
    }

    private void ApplyCatalystAura(Control card, Label? badgeLbl, bool isCatalyst)
    {
        var aura = card.GetNodeOrNull<Panel>("CatalystAura");
        if (aura == null)
        {
            aura = new Panel
            {
                Name = "CatalystAura",
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false
            };
            aura.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            aura.AddThemeStyleboxOverride("panel", UiBuilders.PanelStyle(
                new Color(0.0f, 0.0f, 0.0f, 0.0f),
                border: new Color(1.0f, 0.85f, 0.35f, 0.95f),
                borderWidth: 2, cornerRadius: 8));
            card.AddChild(aura);
        }

        aura.Visible = isCatalyst;
    }

    public void OnCardClicked(int idx)
    {
        if (idx < 0 || idx >= _currentChoices.Count)
            return;

        var choice = _currentChoices[idx];
        if (choice.TryGetValue("type", out var typeVal) && typeVal.AsString() == "new_gear")
        {
            OnGearCardClicked(choice);
            return;
        }

        if (_playerRef != null)
        {
            UpgradeManager.ApplyChoice(_playerRef, choice);
        }
        ResolveChoice(choice);
    }

    /// <summary>Shared close path after a choice is fully resolved.</summary>
    private void ResolveChoice(Dictionary choice)
    {
        EmitSignal(SignalName.ChoiceApplied, choice);

        Visible = false;
        if (SwapPanel != null)
            SwapPanel.Visible = false;
        if (CardsContainer != null)
            CardsContainer.Visible = true;
        _pendingGear = "";
        PauseManager.PopHold(GetTree(), PauseManager.UpgradeDraft);

        if (_pendingLevels > 0)
        {
            _pendingLevels -= 1;
            CallDeferred(MethodName.ShowNextUpgrade);
        }
    }

    // ------------------------------------------------------------------
    // Gear swap flow (Phase 2)
    // ------------------------------------------------------------------

    /// <summary>Id pending placement ("" when the modal shows plain cards).</summary>
    public string PendingGear => _pendingGear;

    private GearChamber? ResolveChamber()
    {
        return _playerRef?.GetNodeOrNull<GearChamber>("GearChamber");
    }

    /// <summary>
    /// Gear cards always acquire into the run backpack first. When the new
    /// item auto-equips (free slot + legal energy) the draft resolves exactly
    /// like other choices; otherwise the swap step opens for replace / store /
    /// discard / cancel.
    /// </summary>
    public void OnGearCardClicked(Dictionary choice)
    {
        if (choice.TryGetValue("player", out var playerVal) && playerVal.AsGodotObject() is Node2D explicitPlayer)
            _playerRef = explicitPlayer;
        string id = choice.TryGetValue("id", out var idVal) ? idVal.AsString() : "";
        var chamber = ResolveChamber();
        if (_playerRef == null || chamber == null || string.IsNullOrEmpty(id))
        {
            ResolveChoice(choice);
            return;
        }

        UpgradeManager.ApplyChoice(_playerRef, choice);

        if (chamber.Owns(id) && IsEquipped(chamber, id))
        {
            ResolveChoice(choice);
            return;
        }

        if (!chamber.Owns(id))
        {
            // The item was not even acquired (backpack full of other copies):
            // resolve like any other choice and let the player keep the card.
            ResolveChoice(choice);
            return;
        }

        EnterSwapMode(id);
    }

    private static bool IsEquipped(GearChamber chamber, string id)
    {
        for (int i = 0; i < GearChamber.MaxSlots; i++)
        {
            if (chamber.GetSlot(i) == id)
                return true;
        }
        return false;
    }

    /// <summary>Shows the swap step for a freshly acquired gear.</summary>
    public void EnterSwapMode(string gearId)
    {
        _pendingGear = gearId ?? "";
        if (CardsContainer != null)
            CardsContainer.Visible = false;
        if (SwapPanel != null)
            SwapPanel.Visible = true;
        BuildSwapCards();
        RefreshSwapPanel();
        UpdateLocalizedTexts();
    }

    private void BuildSwapCards()
    {
        if (SwapSlotsContainer == null)
            return;

        _swapCards.Clear();
        for (int i = 0; i < GearChamber.MaxSlots; i++)
        {
            int slot = i;
            var card = SwapSlotsContainer.GetNodeOrNull<GearSlot>($"SwapSlot{slot}");
            if (card == null)
                continue;
            card.Pressed += () => OnSwapSlotPressed(slot);
            _swapCards.Add(card);
        }
        AudioManager.Instance?.WireClicks(SwapSlotsContainer);
    }

    private void RefreshSwapPanel()
    {
        var chamber = ResolveChamber();
        if (chamber == null)
            return;

        for (int i = 0; i < _swapCards.Count; i++)
        {
            string slotId = chamber.GetSlot(i);
            _swapCards[i].ShowGear(slotId, !string.IsNullOrEmpty(slotId));
        }

        int used = chamber.UsedEnergy;
        int max = chamber.MaxEnergy;
        int generators = chamber.GeneratorCount;
        if (SwapEnergyLabel != null)
        {
            SwapEnergyLabel.Text = TextFormatter.Format(Tr("LOADOUT_ENERGY_FMT"), used, max);
            SwapEnergyLabel.Modulate = used > max
                ? new Color(1.0f, 0.45f, 0.45f)
                : new Color(0.45f, 0.92f, 1.0f);
        }
        if (SwapEnergyPips != null)
            SwapEnergyPips.Configure(max, used, generators);
    }

    private void ShowSwapHint(string key)
    {
        if (SwapHintLabel != null)
            SwapHintLabel.Text = Tr(key);
    }

    /// <summary>Latest swap-step hint text (tests / scripted flows).</summary>
    public string SwapHintText => SwapHintLabel?.Text ?? "";

    /// <summary>Replaces one chamber slot with the pending gear.</summary>
    public bool OnSwapSlotPressed(int slot)
    {
        var chamber = ResolveChamber();
        if (chamber == null || string.IsNullOrEmpty(_pendingGear) || !chamber.Owns(_pendingGear))
            return false;

        if (!chamber.Equip(_pendingGear, slot))
        {
            chamber.CanEquip(_pendingGear, slot, out string reason);
            ShowSwapHint(SwapReasonKey(reason));
            RefreshSwapPanel();
            AudioManager.Instance?.PlayError();
            return false;
        }

        AudioManager.Instance?.PlaySocket();
        ResolveChoice(PendingChoice());
        return true;
    }

    /// <summary>Keeps the pending gear in the run backpack.</summary>
    public void OnSwapStorePressed()
    {
        if (string.IsNullOrEmpty(_pendingGear))
            return;
        ResolveChoice(PendingChoice());
    }

    /// <summary>Discards the pending gear for a small heal.</summary>
    public void OnSwapDiscardPressed()
    {
        var chamber = ResolveChamber();
        if (chamber != null && !string.IsNullOrEmpty(_pendingGear))
            chamber.Discard(_pendingGear);

        AudioManager.Instance?.PlayUnequip();

        if (_playerRef != null && _playerRef.HasMethod("Heal"))
        {
            if (_playerRef.Get("stats").AsGodotObject() is CellStats stats)
                _playerRef.Call("Heal", stats.GetStat("max_health") * DiscardHealRatio);
        }
        ResolveChoice(PendingChoice());
    }

    /// <summary>Returns to the 3-choice cards; the gear stays acquired.</summary>
    public void OnSwapCancelPressed()
    {
        if (CardsContainer != null)
            CardsContainer.Visible = true;
        if (SwapPanel != null)
            SwapPanel.Visible = false;
        _pendingGear = "";
    }

    private Dictionary PendingChoice()
    {
        foreach (var candidate in _currentChoices)
        {
            if (candidate.TryGetValue("type", out var typeVal) && typeVal.AsString() == "new_gear"
                && candidate.TryGetValue("id", out var idVal) && idVal.AsString() == _pendingGear)
            {
                return candidate;
            }
        }
        return new Dictionary { { "type", "new_gear" }, { "id", _pendingGear } };
    }

    private static string SwapReasonKey(string reason)
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
