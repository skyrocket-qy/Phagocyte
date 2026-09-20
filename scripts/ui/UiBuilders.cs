using Godot;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Shared Control/cosmetic builders for HUD, codex and modal widgets.
/// Centralizes the skill badge text/colors, tab highlight colors and the
/// panel style-box boilerplate that used to be copy-pasted across scripts/ui.
/// </summary>
public static class UiBuilders
{
    public static readonly Color TabActiveColor = new Color(1.0f, 1.0f, 1.0f);
    public static readonly Color TabInactiveColor = new Color(0.7f, 0.7f, 0.7f);

    public static readonly Color BadgeInnateColor = new Color(0.4f, 0.95f, 0.8f);
    public static readonly Color BadgeActiveColor = new Color(1.0f, 0.85f, 0.3f);
    public static readonly Color BadgePassiveColor = new Color(0.6f, 0.8f, 1.0f);

    /// <summary>Highlights a tab button (or any Control) as active/inactive.</summary>
    public static void SetTabActive(Control? tab, bool active)
    {
        if (tab != null)
            tab.Modulate = active ? TabActiveColor : TabInactiveColor;
    }

    /// <summary>
    /// Builds the "[ TAG ]" badge, its color and the cooldown/level stats line
    /// shared by the HUD skill tooltip and the codex skill detail pane.
    /// <paramref name="skillType"/> uses the catalog values
    /// "innate" | "active" | "passive" (unknown values fall back to active).
    /// </summary>
    public static void BuildSkillBadge(string skillType, float cooldown, int level, int maxLevel,
        out string badgeText, out Color badgeColor, out string statsText)
    {
        string tag;
        bool alwaysActive = skillType != "active";

        switch (skillType)
        {
            case "innate":
                tag = TranslationServer.Translate("TOOLTIP_TAG_INNATE");
                badgeColor = BadgeInnateColor;
                break;
            case "passive":
                tag = TranslationServer.Translate("TOOLTIP_TAG_PASSIVE");
                badgeColor = BadgePassiveColor;
                break;
            default:
                tag = TranslationServer.Translate("TOOLTIP_TAG_ACTIVE");
                badgeColor = BadgeActiveColor;
                break;
        }

        string levelText = TextFormatter.Format(
            TranslationServer.Translate("TOOLTIP_LV_FORMAT"), level, maxLevel);
        statsText = alwaysActive
            ? TranslationServer.Translate("TOOLTIP_ALWAYS_ACTIVE") + " • " + levelText
            : TextFormatter.Format(TranslationServer.Translate("TOOLTIP_CD"), cooldown) + " • " + levelText;

        badgeText = "[ " + tag + " ]";
    }

    /// <summary>
    /// Composes a StyleBoxFlat from the common bg/border/corner/margin shape
    /// used by panels across scripts/ui. Margins default to unchanged.
    /// </summary>
    public static StyleBoxFlat PanelStyle(Color bg, Color? border = null, int borderWidth = 0,
        int cornerRadius = 0, int marginH = 0, int marginV = 0)
    {
        var style = new StyleBoxFlat { BgColor = bg };

        if (border.HasValue)
        {
            style.BorderColor = border.Value;
            if (borderWidth > 0)
                style.SetBorderWidthAll(borderWidth);
        }

        if (cornerRadius > 0)
            style.SetCornerRadiusAll(cornerRadius);

        if (marginH > 0)
        {
            style.ContentMarginLeft = marginH;
            style.ContentMarginRight = marginH;
        }

        if (marginV > 0)
        {
            style.ContentMarginTop = marginV;
            style.ContentMarginBottom = marginV;
        }

        return style;
    }
}
