using Godot;
using Godot.Collections;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Shared Control/cosmetic builders for HUD, codex and modal widgets.
/// Centralizes the skill badge text/colors, tab highlight colors and the
/// panel style-box boilerplate that used to be copy-pasted across scripts/ui.
/// </summary>
public static class UiBuilders
{
    public static readonly Color TabActiveColor = new Color(0.35f, 1.0f, 0.88f);
    public static readonly Color TabInactiveColor = new Color(0.88f, 0.94f, 0.98f);

    public static readonly StyleBox TabActiveStyle =
        AssetLoader.Load<StyleBox>("res://assets/theme/tab_button_active.tres");
    public static readonly StyleBox TabNormalStyle =
        AssetLoader.Load<StyleBox>("res://assets/theme/tab_button_normal.tres");
    public static readonly StyleBox TabHoverStyle =
        AssetLoader.Load<StyleBox>("res://assets/theme/tab_button_hover.tres");

    public static readonly Color BadgeActiveColor = new Color(1.0f, 0.85f, 0.3f);
    public static readonly Color BadgePassiveColor = new Color(0.6f, 0.8f, 1.0f);

    /// <summary>YFP-gold fill for collection-level progress (gallery header/detail).</summary>
    public static readonly Color AchievementGoldFill = new Color(1.0f, 0.84f, 0.35f);
    private static readonly Color AchievementTrackColor = new Color(0.05f, 0.08f, 0.12f, 0.9f);
    private static readonly Color AchievementTrackBorder = new Color(0.2f, 0.35f, 0.5f, 0.6f);

    /// <summary>
    /// Styles a gallery progress bar with the bio-fluorescence track + fill.
    /// One source of truth for the header, detail and per-card mini bars
    /// (which otherwise fall back to the gray default theme).
    /// </summary>
    public static void StyleAchievementProgress(ProgressBar? bar, Color fill)
    {
        if (bar == null)
            return;
        var bg = new StyleBoxFlat { BgColor = AchievementTrackColor, BorderColor = AchievementTrackBorder };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(4);
        var fg = new StyleBoxFlat { BgColor = fill };
        fg.SetCornerRadiusAll(4);
        bar.AddThemeStyleboxOverride("background", bg);
        bar.AddThemeStyleboxOverride("fill", fg);
    }

    /// <summary>Highlights a tab button (or any Control) as active/inactive with cyber-fluorescence styling.</summary>
    public static void SetTabActive(Control? tab, bool active)
    {
        if (tab == null)
            return;

        tab.Modulate = Colors.White;
        if (tab is Button btn)
        {
            btn.AddThemeStyleboxOverride("normal", active ? TabActiveStyle : TabNormalStyle);
            btn.AddThemeStyleboxOverride("hover", TabHoverStyle);
            btn.AddThemeStyleboxOverride("pressed", TabActiveStyle);
            btn.AddThemeColorOverride("font_color", active ? TabActiveColor : TabInactiveColor);
            btn.AddThemeColorOverride("font_hover_color", new Color(0.5f, 1.0f, 0.9f));
            btn.AddThemeColorOverride("font_pressed_color", TabActiveColor);
        }
        else
        {
            tab.Modulate = active ? TabActiveColor : TabInactiveColor;
        }
    }

    /// <summary>
    /// Builds the "[ TAG ]" badge, its color and the cooldown/level stats line
    /// shared by the HUD skill tooltip and the codex skill detail pane.
    /// Only the functional split is shown: "passive" renders the passive
    /// badge, everything else (active + cell innates, which are active
    /// weapons) renders the active badge. No innate/exclusive wording.
    /// </summary>
    public static void BuildSkillBadge(string skillType, float cooldown, int level, int maxLevel,
        out string badgeText, out Color badgeColor, out string statsText)
    {
        string tag;
        bool alwaysActive = skillType != "active" && skillType != "innate";

        switch (skillType)
        {
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

    /// <summary>
    /// Baseline vitals block: raw HP / speed / armor numbers plus the class
    /// signature stat with a plain-language label (no academic stat names).
    /// Shared by the class-select dossiers and the codex archive.
    /// </summary>
    public static string BuildClassVitalsText(Dictionary data)
    {
        float hp = data.TryGetValue("base_hp", out Variant hpVal) ? hpVal.AsSingle() : 100.0f;
        float speed = data.TryGetValue("base_speed", out Variant spVal) ? spVal.AsSingle() : 230.0f;
        float armor = data.TryGetValue("base_armor", out Variant arVal) ? arVal.AsSingle() : 0.0f;

        var lines = new System.Collections.Generic.List<string>
        {
            $"{PassiveTreeManager.GetStatLabel("max_health")} {hp:F0}",
            $"{PassiveTreeManager.GetStatLabel("move_speed")} {speed:F0}",
            $"{PassiveTreeManager.GetStatLabel("armor")} {armor:F0}"
        };

        string sigStat = data.TryGetValue("trait_stat", out Variant sigVal) ? sigVal.AsString() : "";
        if (!string.IsNullOrEmpty(sigStat))
        {
            float sigNum = data.TryGetValue("trait_stat_value", out Variant signVal) ? signVal.AsSingle() : 0.0f;
            lines.Add($"{SignatureStatLabel(sigStat)} {FormatSignatureStat(sigStat, sigNum)}");
        }
        return string.Join("\n", lines);
    }

    public static string SignatureStatLabel(string stat)
    {
        string key = stat switch
        {
            "block" => "CLASS_SIG_BLOCK",
            "crit_chance" => "CLASS_SIG_CRIT",
            "might" => "CLASS_SIG_MIGHT",
            "projectile_speed" => "CLASS_SIG_PROJSPEED",
            "magnet" => "CLASS_SIG_MAGNET",
            _ => ""
        };
        return string.IsNullOrEmpty(key) ? PassiveTreeManager.GetStatLabel(stat) : TranslationServer.Translate(key);
    }

    public static string FormatSignatureStat(string stat, float value) => stat switch
    {
        "crit_chance" or "evasion" or "block" or "life_steal" or "cooldown_reduction" => $"{value * 100.0f:F0}%",
        "might" or "area" or "projectile_speed" or "duration" or "amount" or "knockback" or "crit_damage" => $"×{value:F1}".TrimEnd('0').TrimEnd('.'),
        _ => value % 1.0f == 0.0f ? $"{value:F0}" : $"{value:F1}"
    };

    /// <summary>
    /// Innate skill line: name + full description from the skill catalog.
    /// Shared by the class-select dossiers and the codex archive.
    /// </summary>
    public static (string Text, string ImagePath) BuildClassSkillText(string classKey)
    {
        foreach (string id in GameManager.SkillCatalog.Keys)
        {
            var s = (Dictionary)GameManager.SkillCatalog[id];
            if (s.TryGetValue("type", out Variant typeVal) && typeVal.AsString() == "innate"
                && s.TryGetValue("class_id", out Variant cidVal) && cidVal.AsString() == classKey)
            {
                string nameKey = s.TryGetValue("name_key", out Variant nVal) ? nVal.AsString() : "";
                string descKey = s.TryGetValue("desc_key", out Variant dVal) ? dVal.AsString() : "";
                string imagePath = s.TryGetValue("image_path", out Variant ipVal) ? ipVal.AsString() : AssetPaths.SkillIcon(id);
                return ($"{TranslationServer.Translate(nameKey)}\n{TranslationServer.Translate(descKey)}", imagePath);
            }
        }
        return ("-", "");
    }

    private static readonly System.Text.RegularExpressions.Regex BioPrefixRegex =
        new(@"^\s*[^：:\n]{2,30}[：:]\s*", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Strips a redundant leading "Biochem: / 生物机制：" style label from
    /// biochemistry body text. Call sites prepend their own section header,
    /// so without this the label renders twice (e.g. codex detail + tooltip).
    /// Only the first short "Label:" run is removed; body text is untouched.
    /// </summary>
    public static string StripLeadingLabel(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return BioPrefixRegex.Replace(text, "", 1);
    }
}
