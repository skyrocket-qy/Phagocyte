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
    public EnergyPips? CostPips { get; private set; }
    public Label? NameLabel { get; private set; }
    public Label? StateLabel { get; private set; }
    public ColorRect? CategoryStrip { get; private set; }

    /// <summary>Catalog id currently shown ("" for an empty chamber slot).</summary>
    public string OrganelleId { get; private set; } = "";

    private static readonly Color EmptyIconTint = new(1, 1, 1, 0.28f);
    private static readonly Color EquippedIconTint = new(1, 1, 1, 0.45f);
    private static readonly Color LockedIconTint = new(0.42f, 0.46f, 0.52f, 0.5f);

    public override void _Ready()
    {
        Bind();
    }

    public void Bind()
    {
        IconTexture ??= GetNodeOrNull<TextureRect>("IconTexture");
        CostPips ??= GetNodeOrNull<EnergyPips>("CostPips");
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
    public void ShowOrganelle(string id, bool equipped, bool unlocked = true)
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
            if (CostPips != null)
                CostPips.Configure(0, 0, 0);
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
            IconTexture.Modulate = !unlocked
                ? LockedIconTint
                : equipped ? EquippedIconTint : Colors.White;
        }
        if (CostPips != null)
        {
            if (!unlocked)
            {
                CostPips.Configure(0, 0, 0);
            }
            else if (cost < 0)
            {
                // Generator: the granted energy point renders red.
                CostPips.Configure(-cost, 0, -cost);
            }
            else
            {
                CostPips.Configure(cost, cost, 0);
            }
        }
        if (CategoryStrip != null)
            CategoryStrip.Color = unlocked
                ? CategoryColor(entry["category"].AsString())
                : new Color(0.4f, 0.45f, 0.5f, 0.5f);
        if (NameLabel != null)
        {
            NameLabel.Text = Tr(nameKey);
            NameLabel.Modulate = !unlocked
                ? new Color(0.55f, 0.6f, 0.68f, 0.8f)
                : equipped ? new Color(0.75f, 0.8f, 0.88f, 0.85f) : Colors.White;
        }
        if (StateLabel != null)
        {
            if (!unlocked)
                StateLabel.Text = Tr("LOADOUT_LOCKED_TAG");
            else
                StateLabel.Text = equipped ? Tr("LOADOUT_EQUIPPED_TAG") : "";
            StateLabel.Visible = !unlocked || equipped;
            StateLabel.Modulate = unlocked
                ? new Color(0.45f, 1.0f, 0.72f)
                : new Color(1.0f, 0.72f, 0.35f);
        }

        string costText = cost < 0 ? $"+{-cost}" : cost.ToString();
        TooltipText = unlocked
            ? Tr(nameKey) + "   " + Tr("LOADOUT_COST_LABEL") + ": " + costText
                + "\n" + Tr(descKey) + "\n" + UiBuilders.StripLeadingLabel(Tr(bioKey))
            : Tr(nameKey) + "\n" + Tr("LOADOUT_DETAIL_LOCKED");
    }
}
