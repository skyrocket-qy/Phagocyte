using Godot;
using Phagocyte.Player;

namespace Phagocyte.Skills;

/// <summary>
/// Shared pseudopod palette: every leukocyte extends pseudopods in its own
/// cytoplasm/membrane colors so cross-lineage builds (e.g. a B cell firing
/// a pseudopod attack) still read as one body.
/// </summary>
public static class PseudopodPalette
{
    public static (Color Fill, Color Edge) ResolveHostColors(Node2D? host)
    {
        Color fill;
        Color edge;
        if (host is Macrophage)
        {
            fill = Macrophage.ColorNormal;
            edge = Macrophage.MembraneNormal;
        }
        else if (host is CtlCell)
        {
            fill = CtlCell.ColorNormal;
            edge = CtlCell.MembraneNormal;
        }
        else if (host is BCell)
        {
            fill = BCell.ColorNormal;
            edge = BCell.MembraneNormal;
        }
        else if (host is DendriticCell)
        {
            fill = DendriticCell.ColorNormal;
            edge = DendriticCell.MembraneNormal;
        }
        else if (host is NeutrophilCell)
        {
            fill = NeutrophilCell.ColorNormal;
            edge = NeutrophilCell.MembraneNormal;
        }
        else
        {
            // Detached / unknown host: legacy macrophage-tinted fallback.
            return (new Color(0.20f, 0.36f, 0.58f, 0.60f), new Color(0.80f, 0.92f, 1.0f, 0.90f));
        }

        // Pseudopods read a touch denser than the body glass so strikes stay legible.
        fill.A = Mathf.Clamp(fill.A + 0.25f, 0.50f, 0.70f);
        edge.A = Mathf.Clamp(edge.A + 0.15f, 0.70f, 0.95f);
        return (fill, edge);
    }
}
