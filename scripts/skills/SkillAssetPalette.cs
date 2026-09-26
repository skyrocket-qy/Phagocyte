using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Skill-icon palette table (Phase 4, TODO line 70: skill visuals must match
/// their asset art). Accents are the most-saturated hues sampled from
/// <c>gen/skill/&lt;id&gt;.png</c> (see commit message for the sampler); VFX
/// keeps its shape language and tints core/halo from here. Unknown ids fall
/// back to the caller's previous hardcoded colors so new skills never break.
/// </summary>
public static class SkillAssetPalette
{
    private static readonly Dictionary<string, Color> Accents = new()
    {
        { SkillIds.PerforinLance, Color.FromHtml("#39c06a") },
        { SkillIds.LysosomalOverload, Color.FromHtml("#b5e61d") },
        { SkillIds.RosTorrent, Color.FromHtml("#2bd2b9") },
        { SkillIds.PhagocyticGrasp, Color.FromHtml("#24d15b") },
        { SkillIds.ComplementCascade, Color.FromHtml("#25a4e2") },
        { SkillIds.AntibodySalvo, Color.FromHtml("#26afc0") },
        { SkillIds.PseudopodLunge, Color.FromHtml("#40cd76") },
        { SkillIds.NitricOxideHalo, Color.FromHtml("#4ccae3") },
        { SkillIds.NucleaseBlades, Color.FromHtml("#41c471") },
        { SkillIds.GranzymeDetonation, Color.FromHtml("#e78c41") },
        { SkillIds.InterferonWave, Color.FromHtml("#1aabc6") },
        { SkillIds.LysozymeRicochet, Color.FromHtml("#1c6fdc") },
        { SkillIds.PhagolysosomeVent, Color.FromHtml("#ed4543") },
        { SkillIds.ProInflammatoryArc, Color.FromHtml("#e3a638") },
        { SkillIds.ExosomeSingularity, Color.FromHtml("#22add4") },
        { SkillIds.DefensinBarbs, Color.FromHtml("#4de892") },
        { SkillIds.MhcTracerBeam, Color.FromHtml("#1dc457") },
        { SkillIds.HistamineSurge, Color.FromHtml("#e3a638") },
    };

    /// <summary>Saturated icon accent for a skill id, or <paramref name="fallback"/>.</summary>
    public static Color Accent(string skillId, Color fallback)
    {
        return Accents.TryGetValue(skillId, out var c) ? c : fallback;
    }

    /// <summary>White-leaning excitation core derived from the accent.</summary>
    public static Color Core(string skillId, Color fallback)
    {
        return Accent(skillId, fallback).Lerp(Colors.White, 0.55f);
    }
}
