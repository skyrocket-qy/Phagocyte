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

    /// <summary>Builds the banner and subscribes to achievement unlocks. Call once from Hud._Ready.</summary>
    public void Bind()
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

        AchBannerIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(40, 40),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
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

        _achUnlockCallback = Callable.From((string a, Dictionary b) => OnAchievementUnlocked(a, b));
        AchievementManager.AddUnlockListener(_achUnlockCallback);
    }

    /// <summary>Removes the achievement listener. Call from Hud._ExitTree.</summary>
    public void Unbind()
    {
        AchievementManager.RemoveUnlockListener(_achUnlockCallback);
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
