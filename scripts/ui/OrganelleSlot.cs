using Godot;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// One organelle slot card (2x2 chamber or backpack grid). Dumb view: the
/// owner pushes state through <see cref="ShowOrganelle"/>; clicks are forwarded
/// via <see cref="Pressed"/> (inherited from Button).
/// </summary>
public partial class OrganelleSlot : Button
{
    public TextureRect? IconTexture { get; private set; }
    public Label? CostLabel { get; private set; }
    public Label? NameLabel { get; private set; }
    public Label? StateLabel { get; private set; }
    public ColorRect? CategoryStrip { get; private set; }

    /// <summary>Catalog id currently shown ("" for an empty chamber slot).</summary>
    public string OrganelleId { get; private set; } = "";

    private static readonly Color EmptyIconTint = new(1, 1, 1, 0.28f);
    private static readonly Color EquippedIconTint = new(1, 1, 1, 0.45f);

    public override void _Ready()
    {
        Bind();
    }

    public void Bind()
    {
        IconTexture ??= GetNodeOrNull<TextureRect>("IconTexture");
        CostLabel ??= GetNodeOrNull<Label>("CostLabel");
        NameLabel ??= GetNodeOrNull<Label>("NameLabel");
        StateLabel ??= GetNodeOrNull<Label>("StateLabel");
        CategoryStrip ??= GetNodeOrNull<ColorRect>("CategoryStrip");
    }

    /// <summary>Per-category accent color shared by slot strips and tab buttons.</summary>
    public static Color CategoryColor(string category)
    {
        return category switch
        {
            "metabolism" => new Color(0.98f, 0.78f, 0.30f),
            "digestion" => new Color(0.72f, 0.95f, 0.40f),
            "cytoskeleton" => new Color(0.40f, 0.85f, 0.98f),
            "synthesis" => new Color(0.55f, 0.70f, 1.0f),
            "sensing" => new Color(0.85f, 0.60f, 1.0f),
            "symbiosis" => new Color(0.98f, 0.52f, 0.62f),
            _ => new Color(0.6f, 0.7f, 0.8f)
        };
    }

    /// <summary>Renders an organelle (or an empty chamber slot when id is "").</summary>
    public void ShowOrganelle(string id, bool equipped)
    {
        Bind();
        OrganelleId = id ?? "";

        if (string.IsNullOrEmpty(OrganelleId))
        {
            if (IconTexture != null)
            {
                IconTexture.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
                IconTexture.Modulate = EmptyIconTint;
            }
            if (CostLabel != null)
                CostLabel.Text = "";
            if (StateLabel != null)
                StateLabel.Text = "";
            if (CategoryStrip != null)
                CategoryStrip.Color = new Color(0.35f, 0.45f, 0.55f, 0.35f);
            if (NameLabel != null)
            {
                NameLabel.Text = Tr("LOADOUT_EMPTY_SLOT");
                NameLabel.Modulate = new Color(0.6f, 0.68f, 0.78f, 0.75f);
            }
            TooltipText = "";
            return;
        }

        var entry = GameManager.OrganelleCatalog.TryGetValue(OrganelleId, out var entryVar)
            ? entryVar.AsGodotDictionary()
            : null;
        if (entry == null)
            return;

        int cost = entry["energy_cost"].AsInt32();
        string nameKey = entry["name_key"].AsString();
        string descKey = entry["desc_key"].AsString();
        string bioKey = entry["bio_key"].AsString();

        if (IconTexture != null)
        {
            IconTexture.Texture = AssetLoader.TryLoad<Texture2D>(entry["image_path"].AsString())
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
            IconTexture.Modulate = equipped ? EquippedIconTint : Colors.White;
        }
        if (CostLabel != null)
        {
            CostLabel.Text = cost < 0 ? $"+{-cost}" : cost.ToString();
            CostLabel.Modulate = cost < 0
                ? new Color(0.45f, 1.0f, 0.72f)
                : cost >= 4 ? new Color(1.0f, 0.55f, 0.45f) : new Color(0.85f, 0.92f, 1.0f);
        }
        if (CategoryStrip != null)
            CategoryStrip.Color = CategoryColor(entry["category"].AsString());
        if (NameLabel != null)
        {
            NameLabel.Text = Tr(nameKey);
            NameLabel.Modulate = equipped ? new Color(0.75f, 0.8f, 0.88f, 0.85f) : Colors.White;
        }
        if (StateLabel != null)
        {
            StateLabel.Text = equipped ? Tr("LOADOUT_EQUIPPED_TAG") : "";
            StateLabel.Visible = equipped;
        }

        TooltipText = Tr(nameKey) + "\n" + Tr(descKey) + "\n" + UiBuilders.StripLeadingLabel(Tr(bioKey));
    }
}
