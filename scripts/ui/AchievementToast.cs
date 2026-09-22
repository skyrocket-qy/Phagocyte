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

        // Shell lives in achievement_toast.tscn (off-screen start pose included).
        var toast = AssetLoader.Load<PackedScene>("res://scenes/ui/achievement_toast.tscn").Instantiate<AchievementToast>();
        parent.AddChild(toast);
        toast.Initialize(achData);
    }

    public void Initialize(Godot.Collections.Dictionary achData)
    {
        string imagePath = achData.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : "";
        Texture2D? tex = AssetLoader.TryLoad<Texture2D>(imagePath)
            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        var icon = GetNodeOrNull<TextureRect>("HBox/Icon");
        if (tex != null && icon != null)
            icon.Texture = tex;

        string titleKey = achData.TryGetValue("title_key", out var tVal) ? tVal.AsString() : "ACH_TITLE";
        string descKey = achData.TryGetValue("desc_key", out var dVal) ? dVal.AsString() : "ACH_DESC";
        string rewardCell = achData.TryGetValue("reward_cell", out var rVal) ? rVal.AsString() : "";

        var header = GetNodeOrNull<Label>("HBox/VBox/Header");
        if (header != null)
            header.Text = !string.IsNullOrEmpty(rewardCell)
                ? Tr("ACH_HEADER_ARCHETYPE")
                : Tr("ACH_HEADER_MILESTONE");

        var titleLabel = GetNodeOrNull<Label>("HBox/VBox/Title");
        if (titleLabel != null)
            titleLabel.Text = Tr(titleKey);

        var descLabel = GetNodeOrNull<Label>("HBox/VBox/Desc");
        if (descLabel != null)
            descLabel.Text = Tr(descKey);

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
