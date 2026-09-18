using Godot;
using System;
using Phagocyte.Combat;

namespace Phagocyte.Enemies;

/// <summary>
/// Sticky alginate extracellular polymeric substance (EPS) biofilm puddle left by Pseudomonas aeruginosa.
/// Slows player movement by 50% and protects bacteria inside.
/// </summary>
public partial class BiofilmArea : BioHazardArea
{
    public BiofilmArea()
    {
        Duration = 8.0f;
        Radius = 75.0f;
        SlowFactor = 0.5f;
        SlowsTarget = true;
        DealsDamage = false;
        CoreColor = new Color(0.12f, 0.55f, 0.22f, 0.35f);
        RimColor = new Color(0.25f, 0.85f, 0.35f, 0.65f);
    }
}
