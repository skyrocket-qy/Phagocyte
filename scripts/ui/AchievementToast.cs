using Godot;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Real-time HUD banner celebrating achievement and immune cell archetype unlocks.
/// Smoothly slides down from the top edge with bio-luminescent borders and audio feedback.
/// </summary>
public partial class AchievementToast : PanelContainer
{
    private float _displayTimer = 3.2f;
    private bool _isDismissing = false;

    public static void ShowToast(Node? parent, Godot.Collections.Dictionary achData)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return;

        var toast = new AchievementToast();
        parent.AddChild(toast);
        toast.Initialize(achData);
    }

    public void Initialize(Godot.Collections.Dictionary achData)
    {
        ProcessMode = ProcessModeEnum.Always;

        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;
        OffsetLeft = -170.0f;
        OffsetRight = 170.0f;
        OffsetTop = -90.0f; // Start off-screen
        OffsetBottom = -26.0f;
        ZIndex = 120;

        var style = UiBuilders.PanelStyle(
            new Color(0.06f, 0.09f, 0.14f, 0.94f),
            border: new Color(0.85f, 0.72f, 0.35f, 0.95f),
            borderWidth: 2, cornerRadius: 8, marginH: 14, marginV: 8);
        AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        AddChild(hbox);

        string icon = achData.TryGetValue("icon", out var iVal) ? iVal.AsString() : "🧬";
        var iconLabel = new Label
        {
            Text = icon,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconLabel.AddThemeFontSizeOverride("font_size", 24);
        hbox.AddChild(iconLabel);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(vbox);

        string titleKey = achData.TryGetValue("title_key", out var tVal) ? tVal.AsString() : "ACH_TITLE";
        string descKey = achData.TryGetValue("desc_key", out var dVal) ? dVal.AsString() : "ACH_DESC";
        string rewardCell = achData.TryGetValue("reward_cell", out var rVal) ? rVal.AsString() : "";

        string headerText = !string.IsNullOrEmpty(rewardCell) 
            ? "🧬 IMMUNE ARCHETYPE UNLOCKED!" 
            : "⭐ MILESTONE ACHIEVED!";

        var headerLabel = new Label
        {
            Text = headerText,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        headerLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.35f));
        headerLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(headerLabel);

        var titleLabel = new Label
        {
            Text = Tr(titleKey),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        titleLabel.AddThemeColorOverride("font_color", Colors.White);
        titleLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(titleLabel);

        var descLabel = new Label
        {
            Text = Tr(descKey),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        descLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.75f, 0.85f));
        descLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(descLabel);

        // Slide down tween
        var tween = CreateTween();
        tween.TweenProperty(this, "offset_top", 16.0f, 0.35)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(this, "offset_bottom", 80.0f, 0.35)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        AudioManager.Instance?.PlayLevelUp();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _displayTimer -= dt;

        if (_displayTimer <= 0.0f && !_isDismissing)
        {
            _isDismissing = true;
            var tween = CreateTween();
            tween.TweenProperty(this, "offset_top", -90.0f, 0.35)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.In);
            tween.Parallel().TweenProperty(this, "offset_bottom", -26.0f, 0.35)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.In);
            tween.Parallel().TweenProperty(this, "modulate:a", 0.0f, 0.35);
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
