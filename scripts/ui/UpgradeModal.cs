using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Level-Up 3-Choice Epigenetic Mutation Modal.
/// Pauses the game, displays 3 distinct choices (Active/Passive), and applies the selected upgrade.
/// </summary>
public partial class UpgradeModal : Control
{
    [Signal]
    public delegate void ChoiceAppliedEventHandler(Dictionary choice);

    public Label? TitleLabel { get; set; }
    public Label? SubtitleLabel { get; set; }
    public HBoxContainer? CardsContainer { get; set; }

    private Array<Dictionary> _currentChoices = new();
    public Array<Dictionary> CurrentChoices => _currentChoices;
    private Node2D? _playerRef = null;
    private int _pendingLevels = 0;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        TitleLabel = GetNodeOrNull<Label>("CenterContainer/VBox/TitleLabel");
        SubtitleLabel = GetNodeOrNull<Label>("CenterContainer/VBox/SubtitleLabel");
        CardsContainer = GetNodeOrNull<HBoxContainer>("CenterContainer/VBox/CardsContainer");

        SetupCardListeners();
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

        _currentChoices = UpgradeManager.GenerateChoices(_playerRef, 3);
        if (_currentChoices.Count == 0)
            return;

        PopulateCards();
        Visible = true;
        GetTree().Paused = true;
    }

    private void PopulateCards()
    {
        if (TitleLabel != null)
            TitleLabel.Text = Tr("UPGRADE_MODAL_TITLE");
        if (SubtitleLabel != null)
            SubtitleLabel.Text = Tr("UPGRADE_MODAL_SUBTITLE");

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
                var titleLbl = card.GetNodeOrNull<Label>("VBox/TitleLabel");
                var badgeLbl = card.GetNodeOrNull<Label>("VBox/BadgeLabel");
                var descLbl = card.GetNodeOrNull<Label>("VBox/DescLabel");

                if (iconLbl != null)
                    iconLbl.Text = choice.TryGetValue("icon", out var iconVal) ? iconVal.AsString() : "⚡";
                if (titleLbl != null)
                {
                    string nameKey = choice.TryGetValue("name", out var nVal) ? nVal.AsString() : "";
                    titleLbl.Text = Tr(nameKey);
                }
                if (badgeLbl != null)
                {
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
                        string badgeKey = choice.TryGetValue("badge", out var bgVal) ? bgVal.AsString() : "UPGRADE";
                        badgeLbl.Text = "[ " + Tr(badgeKey) + " ]";
                        badgeLbl.Modulate = new Color(0.5f, 0.8f, 1.0f);
                    }
                }
                if (descLbl != null)
                {
                    string descKey = choice.TryGetValue("desc", out var dVal) ? dVal.AsString() : "";
                    descLbl.Text = Tr(descKey);
                }
            }
            else
            {
                card.Visible = false;
            }
        }
    }

    public void OnCardClicked(int idx)
    {
        if (idx < 0 || idx >= _currentChoices.Count)
            return;

        var choice = _currentChoices[idx];
        if (_playerRef != null)
        {
            UpgradeManager.ApplyChoice(_playerRef, choice);
        }
        EmitSignal(SignalName.ChoiceApplied, choice);

        Visible = false;
        GetTree().Paused = false;

        if (_pendingLevels > 0)
        {
            _pendingLevels -= 1;
            CallDeferred(MethodName.ShowNextUpgrade);
        }
    }
}
