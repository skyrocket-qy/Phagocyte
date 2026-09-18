using System;
using System.Collections.Generic;

namespace Phagocyte.Endgame;

/// <summary>
/// Pathological Overload Afflictions (docs/endgame.md §4): optional negative
/// modifiers selected before an Endless Cytokine Storm run. Each active
/// affliction adds its bonus to the settlement score multiplier (additive;
/// all six stack to +175% -> score ×2.75).
/// </summary>
public static class AfflictionManager
{
    public const string FebrileConvulsion = "febrile_convulsion";   // 高熱驚厥
    public const string Endotoxemia = "endotoxemia";                // 內毒素血症
    public const string AutophagicFailure = "autophagic_failure";   // 自噬衰竭
    public const string MicrotubuleSclerosis = "microtubule_sclerosis"; // 微管硬化
    public const string AntigenicDrift = "antigenic_drift";         // 抗原全漂移
    public const string ExtremeViscosity = "extreme_viscosity";     // 極限黏滯

    /// <summary>Effect tuning (docs/endgame.md §4).</summary>
    public const float EndotoxemiaDamageMultiplier = 1.5f;
    public const float ViscosityMoveSpeedPenalty = -0.25f;
    public const float FebrileBurnInterval = 5.0f;
    public const float FebrileBurnHealthFraction = 0.02f;
    public const float AntigenicDriftInterval = 20.0f;

    public sealed class Definition
    {
        public string Id { get; init; } = "";
        public string NameKey { get; init; } = "";
        public string DescKey { get; init; } = "";
        public string Icon { get; init; } = "";
        public int BonusPercent { get; init; }
    }

    /// <summary>Ordered catalog matching the docs/endgame.md §4 table.</summary>
    public static readonly List<Definition> Definitions = new()
    {
        new Definition
        {
            Id = FebrileConvulsion, Icon = "🔥",
            NameKey = "AFFLICTION_FEBRILE_NAME", DescKey = "AFFLICTION_FEBRILE_DESC",
            BonusPercent = 25
        },
        new Definition
        {
            Id = Endotoxemia, Icon = "🧫",
            NameKey = "AFFLICTION_ENDOTOXEMIA_NAME", DescKey = "AFFLICTION_ENDOTOXEMIA_DESC",
            BonusPercent = 30
        },
        new Definition
        {
            Id = AutophagicFailure, Icon = "🚫",
            NameKey = "AFFLICTION_AUTOPHAGY_NAME", DescKey = "AFFLICTION_AUTOPHAGY_DESC",
            BonusPercent = 40
        },
        new Definition
        {
            Id = MicrotubuleSclerosis, Icon = "🧱",
            NameKey = "AFFLICTION_MICROTUBULE_NAME", DescKey = "AFFLICTION_MICROTUBULE_DESC",
            BonusPercent = 35
        },
        new Definition
        {
            Id = AntigenicDrift, Icon = "🧬",
            NameKey = "AFFLICTION_DRIFT_NAME", DescKey = "AFFLICTION_DRIFT_DESC",
            BonusPercent = 20
        },
        new Definition
        {
            Id = ExtremeViscosity, Icon = "🌡️",
            NameKey = "AFFLICTION_VISCOSITY_NAME", DescKey = "AFFLICTION_VISCOSITY_DESC",
            BonusPercent = 25
        }
    };

    private static readonly HashSet<string> _selected = new(StringComparer.Ordinal);

    /// <summary>Raised whenever the selection changes (setup modal refresh).</summary>
    public static event Action? SelectionChanged;

    /// <summary>Affliction ids active for the upcoming/current endless run.</summary>
    public static IReadOnlyCollection<string> SelectedIds => _selected;

    public static bool IsActive(string afflictionId)
    {
        return !string.IsNullOrEmpty(afflictionId) && _selected.Contains(afflictionId);
    }

    public static Definition? GetDefinition(string afflictionId)
    {
        foreach (var def in Definitions)
        {
            if (def.Id == afflictionId)
                return def;
        }
        return null;
    }

    /// <summary>Additive score bonus (0.0..1.75).</summary>
    public static float ScoreBonus
    {
        get
        {
            float bonus = 0.0f;
            foreach (var def in Definitions)
            {
                if (_selected.Contains(def.Id))
                    bonus += def.BonusPercent / 100.0f;
            }
            return bonus;
        }
    }

    /// <summary>Settlement score multiplier: 1.0 with none selected, up to ×2.75.</summary>
    public static float ScoreMultiplier => 1.0f + ScoreBonus;

    public static void SetSelected(string afflictionId, bool selected)
    {
        if (GetDefinition(afflictionId) == null)
            return;

        bool changed = selected ? _selected.Add(afflictionId) : _selected.Remove(afflictionId);
        if (changed)
            SelectionChanged?.Invoke();
    }

    public static void Toggle(string afflictionId)
    {
        SetSelected(afflictionId, !IsActive(afflictionId));
    }

    public static void SetSelection(IEnumerable<string>? afflictionIds)
    {
        // Snapshot first: callers may legitimately pass SelectedIds itself.
        var incoming = new List<string>();
        if (afflictionIds != null)
        {
            foreach (string id in afflictionIds)
            {
                if (GetDefinition(id) != null)
                    incoming.Add(id);
            }
        }

        _selected.Clear();
        foreach (string id in incoming)
            _selected.Add(id);
        SelectionChanged?.Invoke();
    }

    public static void Clear()
    {
        if (_selected.Count == 0)
            return;
        _selected.Clear();
        SelectionChanged?.Invoke();
    }

    // --- Effect knobs consumed by the run systems (docs/endgame.md §4) ---

    /// <summary>內毒素血症: all pathogen damage taken +50%.</summary>
    public static float IncomingDamageMultiplier => IsActive(Endotoxemia) ? EndotoxemiaDamageMultiplier : 1.0f;

    /// <summary>自噬衰竭: global health_regen forced to zero.</summary>
    public static bool BlocksHealthRegen => IsActive(AutophagicFailure);

    /// <summary>微管硬化: Squeeze Mode (脫水穿梭避險) disabled.</summary>
    public static bool SqueezeModeDisabled => IsActive(MicrotubuleSclerosis);

    /// <summary>極限黏滯: base move speed -25% (applied as a percent modifier at run start).</summary>
    public static float MoveSpeedPercentPenalty => IsActive(ExtremeViscosity) ? ViscosityMoveSpeedPenalty : 0.0f;
}
