using Godot;
using Godot.Collections;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Achievement / overdrive toast banner (former <c>Hud</c> toast block).
/// Owns the programmatic banner nodes and the achievement-unlock listener;
/// driven explicitly by <see cref="Hud"/>.
/// </summary>
public partial class ToastView : Node
{
    public PanelContainer? AchievementBanner { get; set; }
    public Label? AchBannerTitle { get; set; }
    public Label? AchBannerDesc { get; set; }
    public TextureRect? AchBannerIcon { get; set; }
    public Tween? AchTween { get; set; }

    private Callable _achUnlockCallback;
    private Callable _organelleUnlockCallback;

    /// <summary>Builds the banner and subscribes to achievement unlocks. Call once from Hud._Ready.</summary>
    public void Bind()
    {
        // Banner shell lives in toast_banner.tscn; only text/texture are dynamic.
        AchievementBanner = AssetLoader.Load<PackedScene>("res://scenes/ui/toast_banner.tscn").Instantiate<PanelContainer>();
        AchievementBanner.Name = "AchievementBanner";
        AddChild(AchievementBanner);
        // The shell defaults to visible: hide until the first unlock, or the
        // banner sits on screen from run start and looks permanently stuck.
        AchievementBanner.Visible = false;

        AchBannerIcon = AchievementBanner.GetNodeOrNull<TextureRect>("HBox/Icon");
        AchBannerTitle = AchievementBanner.GetNodeOrNull<Label>("HBox/VBox/Title");
        AchBannerDesc = AchievementBanner.GetNodeOrNull<Label>("HBox/VBox/Desc");
        if (AchBannerTitle != null)
            AchBannerTitle.Text = Tr("TOAST_ACH_UNLOCKED");

        _achUnlockCallback = Callable.From((string a, Dictionary b) => OnAchievementUnlocked(a, b));
        AchievementManager.AddUnlockListener(_achUnlockCallback);

        _organelleUnlockCallback = Callable.From((string id, Dictionary entry) => OnOrganelleUnlocked(id, entry));
        OrganelleUnlockManager.AddUnlockListener(_organelleUnlockCallback);
    }

    /// <summary>Removes the achievement listener. Call from Hud._ExitTree.</summary>
    public void Unbind()
    {
        AchievementManager.RemoveUnlockListener(_achUnlockCallback);
        OrganelleUnlockManager.RemoveUnlockListener(_organelleUnlockCallback);
    }

    /// <summary>Organelle drop collected: same banner, chamber-green accent.</summary>
    public void OnOrganelleUnlocked(string _organelleId, Dictionary entry)
    {
        string imagePath = entry.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : "";
        string nameKey = entry.TryGetValue("name_key", out var nkVal) ? nkVal.AsString() : "";
        string descKey = entry.TryGetValue("desc_key", out var dkVal) ? dkVal.AsString() : "";
        PlayToastBanner(imagePath,
            Tr("TOAST_ORGANELLE_UNLOCKED") + " " + Tr(nameKey),
            Tr(descKey),
            new Color(0.45f, 1.0f, 0.72f));
    }

    /// <summary>Endless overdrive tier alert reusing the achievement toast (docs/endgame.md §3.2).</summary>
    public void ShowOverdriveAlert(string title, string desc)
    {
        PlayToastBanner(AssetPaths.UiIcon("status_overclock"), title, desc, new Color(1.0f, 0.72f, 0.25f));
    }

    public void OnAchievementUnlocked(string _achId, Dictionary achInfo)
    {
        string imagePath = achInfo.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : "";
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

        PlayToastBanner(imagePath, title, subText, null);
    }

    public void PlayToastBanner(string imagePath, string title, string desc, Color? titleColor)
    {
        if (AchievementBanner == null)
            return;

        if (AchBannerIcon != null)
            AchBannerIcon.Texture = AssetLoader.TryLoad<Texture2D>(imagePath)
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
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
        // Real-time dismissal: bullet-time (time_scale 0.05) would stretch the
        // 3.2s hold into a minute and repeated unlocks would refresh it
        // forever, pinning the banner on screen.
        AchTween.SetIgnoreTimeScale(true);
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
