using Godot;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Single achievement card: left thumbnail + title + mini progress bar.
/// Layout lives in <c>achievement_card.tscn</c>; <see cref="Bind"/> fills the
/// per-achievement data (accent, lock state, art, progress). Selection is
/// reported through <see cref="OnSelected"/> so the gallery keeps owning
/// the detail panel.
/// </summary>
public partial class AchievementCard : PanelContainer
{
    /// <summary>Fired with the achievement id when the card is left-clicked.</summary>
    public Action<string>? OnSelected;

    private string _aid = "";

    /// <summary>Fills the card from an achievement info dict (same shape as
    /// <c>AchievementManager.GetAchievementInfo</c>). Call once per instance.
    /// </summary>
    public void Bind(Godot.Collections.Dictionary ach)
    {
        _aid = ach["id"].AsString();
        bool unlocked = ach["unlocked"].AsBool();
        Color accent = AchievementGalleryView.CategoryColor(ach);
        int pct = (int)(ach["progress_ratio"].AsSingle() * 100);

        var style = UiBuilders.PanelStyle(
            unlocked ? new Color(0.07f, 0.12f, 0.18f, 0.92f) : new Color(0.05f, 0.07f, 0.11f, 0.92f),
            accent, cornerRadius: 8, marginH: 12, marginV: 10);
        style.BorderWidthLeft = 4;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        AddThemeStyleboxOverride("panel", style);
        if (!unlocked)
            Modulate = new Color(0.85f, 0.87f, 0.92f);

        var thumbWrap = GetNodeOrNull<Control>("HBox/ThumbWrap");
        Texture2D? tex = AssetLoader.TryLoad<Texture2D>(ach.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : "")
            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        if (tex != null && thumbWrap != null)
        {
            var thumb = new TextureRect
            {
                Texture = tex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                AnchorRight = 1.0f,
                AnchorBottom = 1.0f,
                MouseFilter = MouseFilterEnum.Ignore
            };
            if (!unlocked)
                thumb.Modulate = AchievementGalleryView.LockedDim;
            thumbWrap.AddChild(thumb);
        }

        var title = GetNodeOrNull<Label>("HBox/VBox/Title");
        if (title != null)
        {
            title.Text = (unlocked ? "✅ " : "🔒 ") + ach["title"].AsString();
            title.AddThemeColorOverride("font_color",
                unlocked ? new Color(0.94f, 0.99f, 0.98f) : new Color(0.62f, 0.66f, 0.72f));
        }

        var mini = GetNodeOrNull<ProgressBar>("HBox/VBox/MiniProgress");
        if (mini != null)
        {
            mini.MinValue = 0;
            mini.MaxValue = 100;
            mini.Value = pct;
            mini.ShowPercentage = false;
        }

        GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                OnSelected?.Invoke(_aid);
        };
    }
}
