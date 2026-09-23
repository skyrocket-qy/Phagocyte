using Godot;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// PoE-style cursor tooltip for organelle cards: a styled floating panel that
/// follows the mouse while hovering (category-tinted title, energy cost,
/// effect, bio). Built in code and owned by the hosting view (LoadoutView);
/// the engine built-in TooltipText stays cleared on those cards so the two
/// never double up. Pure view: hidden by default, never touches saves.
/// </summary>
public partial class OrganelleTooltip : PanelContainer
{
    private static readonly Vector2 CursorOffset = new(18, 24);
    private const float CardWidth = 280.0f;

    private Label? _titleLabel;
    private Label? _costLabel;
    private Label? _descLabel;
    private Label? _bioLabel;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 100;
        Visible = false;

        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.03f, 0.06f, 0.10f, 0.97f),
            BorderColor = new Color(0.35f, 0.85f, 0.95f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 12.0f,
            ContentMarginTop = 10.0f,
            ContentMarginRight = 12.0f,
            ContentMarginBottom = 10.0f
        });

        var vbox = new VBoxContainer { Name = "VBox" };
        vbox.AddThemeConstantOverride("separation", 6);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(vbox);

        var medium = AssetLoader.TryLoad<Font>("res://assets/fonts/BodyMediumFont.tres");

        _titleLabel = new Label { Name = "TitleLabel", MouseFilter = MouseFilterEnum.Ignore };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        if (medium != null)
            _titleLabel.AddThemeFontOverride("font", medium);
        vbox.AddChild(_titleLabel);

        _costLabel = new Label { Name = "CostLabel", MouseFilter = MouseFilterEnum.Ignore };
        _costLabel.AddThemeFontSizeOverride("font_size", 13);
        _costLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.92f, 1.0f));
        vbox.AddChild(_costLabel);

        var sep = new HSeparator { Name = "Sep", MouseFilter = MouseFilterEnum.Ignore };
        sep.AddThemeStyleboxOverride("separator", new StyleBoxLine
        {
            Color = new Color(0.0f, 0.9f, 1.0f, 0.25f),
            Thickness = 1
        });
        vbox.AddChild(sep);

        _descLabel = MakeBody(vbox, 13, new Color(0.88f, 0.94f, 0.92f), "DescLabel");
        _bioLabel = MakeBody(vbox, 12, new Color(0.62f, 0.70f, 0.78f, 0.9f), "BioLabel");
    }

    private static Label MakeBody(VBoxContainer parent, int fontSize, Color color, string nodeName)
    {
        var label = new Label
        {
            Name = nodeName,
            CustomMinimumSize = new Vector2(CardWidth, 0),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("line_spacing", 4);
        parent.AddChild(label);
        return label;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;
        Vector2 mouse = GetGlobalMousePosition();
        Vector2 vp = GetViewportRect().Size;
        Vector2 size = GetCombinedMinimumSize();
        Vector2 pos = mouse + CursorOffset;
        pos.X = Mathf.Min(pos.X, Mathf.Max(8.0f, vp.X - size.X - 8.0f));
        pos.Y = Mathf.Min(pos.Y, Mathf.Max(8.0f, vp.Y - size.Y - 8.0f));
        GlobalPosition = pos;
    }

    /// <summary>Shows the tooltip for an organelle id (hides when id is empty/unknown).</summary>
    public void ShowFor(string id)
    {
        if (string.IsNullOrEmpty(id)
            || !GameManager.OrganelleCatalog.TryGetValue(id, out var entryVar)
            || _titleLabel == null || _costLabel == null || _descLabel == null || _bioLabel == null)
        {
            HideTip();
            return;
        }

        var entry = entryVar.AsGodotDictionary();
        string category = entry["category"].AsString();
        int cost = entry["energy_cost"].AsInt32();
        bool unlocked = OrganelleUnlockManager.IsUnlocked(id);

        _titleLabel.Text = (unlocked ? "" : "🔒 ") + Tr(entry["name_key"].AsString());
        _titleLabel.AddThemeColorOverride("font_color", unlocked
            ? OrganelleSlot.CategoryColor(category)
            : new Color(1.0f, 0.72f, 0.35f));

        if (!unlocked)
        {
            _costLabel.Text = "";
            _descLabel.Text = Tr("LOADOUT_DETAIL_LOCKED");
            _bioLabel.Text = "";
        }
        else
        {
            string costText = cost < 0 ? $"+{-cost}" : cost.ToString();
            _costLabel.Text = Tr("LOADOUT_COST_LABEL") + ": " + costText;
            _descLabel.Text = Tr(entry["desc_key"].AsString());
            _bioLabel.Text = UiBuilders.StripLeadingLabel(Tr(entry["bio_key"].AsString()));
        }

        Visible = true;
    }

    public void HideTip()
    {
        Visible = false;
    }
}
