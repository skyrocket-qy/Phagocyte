import { TraitId, TraitRarity, StatId, ModType, type TraitsFile } from "../../schemas/passive.schema";

export { TraitId, TraitRarity, StatId, ModType };

export const Traits: TraitsFile = {
  traits: [
    {
      bio_key: "SKILL_ACTIN_BIO",
      desc_key: "",
      icon: "🧬",
      id: TraitId.Actin,
      modifiers: [],
      name_key: "SKILL_ACTIN_NAME",
      rarity: TraitRarity.Start
    },
    {
      bio_key: "SKILL_AUTOPHAGY_BIO",
      desc_key: "SKILL_AUTOPHAGY_DESC",
      icon: "🔄",
      id: TraitId.Autophagy,
      modifiers: [
        {
          stat: StatId.HealthRegen,
          unit: ModType.Flat,
          value: 0.4
        }
      ],
      name_key: "SKILL_AUTOPHAGY_NAME",
      rarity: TraitRarity.Magic
    },
    {
      bio_key: "SKILL_BILAYER_BIO",
      desc_key: "SKILL_BILAYER_DESC",
      icon: "🛡️",
      id: TraitId.Bilayer,
      modifiers: [
        {
          stat: StatId.MaxHealth,
          unit: ModType.Percent,
          value: 0.15
        },
        {
          stat: StatId.Armor,
          unit: ModType.Flat,
          value: 2.0
        }
      ],
      name_key: "SKILL_BILAYER_NAME",
      rarity: TraitRarity.Unique
    },
    {
      bio_key: "SKILL_CHEMOKINE_BIO",
      desc_key: "",
      icon: "🧲",
      id: TraitId.Chemokine,
      modifiers: [],
      name_key: "SKILL_CHEMOKINE_NAME",
      rarity: TraitRarity.Start
    },
    {
      bio_key: "SKILL_ENDOTOXIN_BIO",
      desc_key: "SKILL_ENDOTOXIN_DESC",
      icon: "🧱",
      id: TraitId.Endotoxin,
      modifiers: [
        {
          stat: StatId.Armor,
          unit: ModType.Flat,
          value: 3.0
        },
        {
          stat: StatId.Block,
          unit: ModType.Flat,
          value: 0.04
        }
      ],
      name_key: "SKILL_ENDOTOXIN_NAME",
      rarity: TraitRarity.Rare
    },
    {
      bio_key: "SKILL_GLYCOLYSIS_BIO",
      desc_key: "SKILL_GLYCOLYSIS_DESC",
      icon: "🍬",
      id: TraitId.Glycolysis,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.06
        },
        {
          stat: StatId.Damage,
          unit: ModType.Percent,
          value: 0.05
        }
      ],
      name_key: "SKILL_GLYCOLYSIS_NAME",
      rarity: TraitRarity.Magic
    },
    {
      bio_key: "SKILL_HEMATOPOIETIC_BIO",
      desc_key: "SKILL_HEMATOPOIETIC_DESC",
      icon: "🩸",
      id: TraitId.Hematopoietic,
      modifiers: [
        {
          stat: StatId.MaxHealth,
          unit: ModType.Percent,
          value: 0.1
        },
        {
          stat: StatId.Block,
          unit: ModType.Flat,
          value: 0.03
        }
      ],
      name_key: "SKILL_HEMATOPOIETIC_NAME",
      rarity: TraitRarity.Rare
    },
    {
      bio_key: "SKILL_KINESIN_BIO",
      desc_key: "",
      icon: "🛤️",
      id: TraitId.Kinesin,
      modifiers: [],
      name_key: "SKILL_KINESIN_NAME",
      rarity: TraitRarity.Start
    },
    {
      bio_key: "SKILL_LONGEVITY_BIO",
      desc_key: "SKILL_LONGEVITY_DESC",
      icon: "⏳",
      id: TraitId.Longevity,
      modifiers: [
        {
          stat: StatId.Duration,
          unit: ModType.Percent,
          value: 0.15
        }
      ],
      name_key: "SKILL_LONGEVITY_NAME",
      rarity: TraitRarity.Magic
    },
    {
      bio_key: "TREE_NODE_LYSOSOME_BIO",
      desc_key: "",
      icon: "🧪",
      id: TraitId.Lysosome,
      modifiers: [],
      name_key: "TREE_NODE_LYSOSOME_NAME",
      rarity: TraitRarity.Start
    },
    {
      bio_key: "SKILL_MITOCHONDRIA_BIO",
      desc_key: "SKILL_MITOCHONDRIA_DESC",
      icon: "⚡",
      id: TraitId.Mitochondria,
      modifiers: [
        {
          stat: StatId.CooldownReduction,
          unit: ModType.PercentagePoints,
          value: 0.08
        },
        {
          stat: StatId.Duration,
          unit: ModType.Percent,
          value: 0.1
        }
      ],
      name_key: "SKILL_MITOCHONDRIA_NAME",
      rarity: TraitRarity.Magic
    },
    {
      bio_key: "SKILL_OPSONIN_BIO",
      desc_key: "",
      icon: "🎯",
      id: TraitId.Opsonin,
      modifiers: [],
      name_key: "SKILL_OPSONIN_NAME",
      rarity: TraitRarity.Start
    },
    {
      bio_key: "SKILL_VDJ_BIO",
      desc_key: "SKILL_VDJ_DESC",
      icon: "🎲",
      id: TraitId.Vdj,
      modifiers: [
        {
          stat: StatId.CritDamage,
          unit: ModType.Percent,
          value: 0.15
        },
        {
          stat: StatId.CritChance,
          unit: ModType.Flat,
          value: 0.03
        }
      ],
      name_key: "SKILL_VDJ_NAME",
      rarity: TraitRarity.Rare
    },
    {
      desc_key: "",
      icon: "🌀",
      id: TraitId.SmallArea,
      modifiers: [
        {
          stat: StatId.Area,
          unit: ModType.Percent,
          value: 0.06
        }
      ],
      name_key: "TREE_NODE_SMALL_AREA_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🧲",
      id: TraitId.SmallMagnet,
      modifiers: [
        {
          stat: StatId.Magnet,
          unit: ModType.Percent,
          value: 0.1
        }
      ],
      name_key: "TREE_NODE_SMALL_MAGNET_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "⚔️",
      id: TraitId.SmallMight,
      modifiers: [
        {
          stat: StatId.Damage,
          unit: ModType.Percent,
          value: 0.04
        }
      ],
      name_key: "TREE_NODE_SMALL_MIGHT_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "💨",
      id: TraitId.SmallMoveSpeed,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.03
        }
      ],
      name_key: "TREE_NODE_SMALL_MOVE_SPEED_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🚀",
      id: TraitId.SmallProjectileSpeed,
      modifiers: [
        {
          stat: StatId.ProjectileSpeed,
          unit: ModType.Percent,
          value: 0.08
        }
      ],
      name_key: "TREE_NODE_SMALL_PROJECTILE_SPEED_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🚀",
      id: TraitId.AdaptiveOverdrive,
      modifiers: [
        {
          stat: StatId.Damage,
          unit: ModType.Percent,
          value: 0.2
        },
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.08
        }
      ],
      name_key: "TREE_NODE_ADAPTIVE_OVERDRIVE_NAME",
      rarity: TraitRarity.Rare
    },
    {
      desc_key: "",
      icon: "🏃",
      id: TraitId.AerobicSprint,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.1
        }
      ],
      name_key: "TREE_NODE_AEROBIC_SPRINT_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🌾",
      id: TraitId.AntigenHarvest,
      modifiers: [
        {
          stat: StatId.Magnet,
          unit: ModType.Percent,
          value: 0.15
        }
      ],
      name_key: "TREE_NODE_ANTIGEN_HARVEST_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "⚔️",
      id: TraitId.AssassinsMandate,
      modifiers: [
        {
          stat: StatId.CritChance,
          unit: ModType.Flat,
          value: 0.1
        }
      ],
      name_key: "TREE_NODE_ASSASSINS_MANDATE_NAME",
      rarity: TraitRarity.Magic
    },
    {
      desc_key: "",
      icon: "🧵",
      id: TraitId.BallisticThreads,
      modifiers: [
        {
          stat: StatId.ProjectileSpeed,
          unit: ModType.Percent,
          value: 0.12
        }
      ],
      name_key: "TREE_NODE_BALLISTIC_THREADS_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🩸",
      id: TraitId.BloodPrice,
      modifiers: [
        {
          stat: StatId.Damage,
          unit: ModType.Percent,
          value: 0.08
        }
      ],
      name_key: "TREE_NODE_BLOOD_PRICE_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🌋",
      id: TraitId.ContainedFury,
      modifiers: [
        {
          stat: StatId.Damage,
          unit: ModType.Percent,
          value: 0.09
        }
      ],
      name_key: "TREE_NODE_CONTAINED_FURY_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🌀",
      id: TraitId.CytoskeletalDrift,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.05
        }
      ],
      name_key: "TREE_NODE_CYTOSKELETAL_DRIFT_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🩹",
      id: TraitId.DeepWound,
      modifiers: [
        {
          stat: StatId.CritDamage,
          unit: ModType.Percent,
          value: 0.15
        }
      ],
      name_key: "TREE_NODE_DEEP_WOUND_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🥾",
      id: TraitId.EnduringMarch,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.04
        }
      ],
      name_key: "TREE_NODE_ENDURING_MARCH_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "⏱️",
      id: TraitId.ExecutionTempo,
      modifiers: [
        {
          stat: StatId.CritDamage,
          unit: ModType.Percent,
          value: 0.2
        }
      ],
      name_key: "TREE_NODE_EXECUTION_TEMPO_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "⏳",
      id: TraitId.ExtendedChambers,
      modifiers: [
        {
          stat: StatId.Duration,
          unit: ModType.Percent,
          value: 0.12
        }
      ],
      name_key: "TREE_NODE_EXTENDED_CHAMBERS_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🔭",
      id: TraitId.FarSense,
      modifiers: [
        {
          stat: StatId.Magnet,
          unit: ModType.Percent,
          value: 0.2
        }
      ],
      name_key: "TREE_NODE_FAR_SENSE_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🛰️",
      id: TraitId.GuidedSalvo,
      modifiers: [
        {
          stat: StatId.ProjectileSpeed,
          unit: ModType.Percent,
          value: 0.1
        }
      ],
      name_key: "TREE_NODE_GUIDED_SALVO_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "♾️",
      id: TraitId.ImmortalCulture,
      modifiers: [
        {
          stat: StatId.HealthRegen,
          unit: ModType.Flat,
          value: 2.0
        },
        {
          stat: StatId.LifeSteal,
          unit: ModType.Percent,
          value: 0.04
        }
      ],
      name_key: "TREE_NODE_IMMORTAL_CULTURE_NAME",
      rarity: TraitRarity.Magic
    },
    {
      desc_key: "",
      icon: "🦾",
      id: TraitId.IronMembrane,
      modifiers: [
        {
          stat: StatId.Armor,
          unit: ModType.Flat,
          value: 3.0
        }
      ],
      name_key: "TREE_NODE_IRON_MEMBRANE_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🪽",
      id: TraitId.LightfootedKiller,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.07
        }
      ],
      name_key: "TREE_NODE_LIGHTFOOTED_KILLER_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🍀",
      id: TraitId.LuckyMutation,
      modifiers: [
        {
          stat: StatId.Evasion,
          unit: ModType.Percent,
          value: 0.04
        }
      ],
      name_key: "TREE_NODE_LUCKY_MUTATION_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🧿",
      id: TraitId.MarkedCore,
      modifiers: [
        {
          stat: StatId.CritChance,
          unit: ModType.Flat,
          value: 0.02
        }
      ],
      name_key: "TREE_NODE_MARKED_CORE_NAME",
      rarity: TraitRarity.Magic
    },
    {
      desc_key: "",
      icon: "🌌",
      id: TraitId.OmnipotentCytoplasm,
      modifiers: [
        {
          stat: StatId.Area,
          unit: ModType.Percent,
          value: 0.3
        },
        {
          stat: StatId.Duration,
          unit: ModType.Percent,
          value: 0.2
        },
        {
          stat: StatId.ProjectileSpeed,
          unit: ModType.Percent,
          value: 0.2
        },
        {
          stat: StatId.CooldownReduction,
          unit: ModType.PercentagePoints,
          value: 0.15
        },
        {
          stat: StatId.MaxHealth,
          unit: ModType.Percent,
          value: -0.25
        }
      ],
      name_key: "TREE_NODE_OMNIPOTENT_CYTOPLASM_NAME",
      rarity: TraitRarity.Unique
    },
    {
      desc_key: "",
      icon: "🏹",
      id: TraitId.OverdrawnStrings,
      modifiers: [
        {
          stat: StatId.Damage,
          unit: ModType.Percent,
          value: 0.07
        }
      ],
      name_key: "TREE_NODE_OVERDRAWN_STRINGS_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🔥",
      id: TraitId.OxidativePace,
      modifiers: [
        {
          stat: StatId.CooldownReduction,
          unit: ModType.PercentagePoints,
          value: 0.05
        }
      ],
      name_key: "TREE_NODE_OXIDATIVE_PACE_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🦉",
      id: TraitId.PatientObserver,
      modifiers: [
        {
          stat: StatId.Duration,
          unit: ModType.Percent,
          value: 0.14
        }
      ],
      name_key: "TREE_NODE_PATIENT_OBSERVER_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "📌",
      id: TraitId.PiercingFilaments,
      modifiers: [
        {
          stat: StatId.Pierce,
          unit: ModType.Flat,
          value: 1.0
        }
      ],
      name_key: "TREE_NODE_PIERCING_FILAMENTS_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🔍",
      id: TraitId.PreciseEdge,
      modifiers: [
        {
          stat: StatId.CritChance,
          unit: ModType.Flat,
          value: 0.02
        }
      ],
      name_key: "TREE_NODE_PRECISE_EDGE_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🏇",
      id: TraitId.PseudopodMarathon,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.18
        }
      ],
      name_key: "TREE_NODE_PSEUDOPOD_MARATHON_NAME",
      rarity: TraitRarity.Magic
    },
    {
      desc_key: "",
      icon: "🩺",
      id: TraitId.RapidClotting,
      modifiers: [
        {
          stat: StatId.Armor,
          unit: ModType.Flat,
          value: 2.0
        }
      ],
      name_key: "TREE_NODE_RAPID_CLOTTING_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🧭",
      id: TraitId.RapidReposition,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.06
        }
      ],
      name_key: "TREE_NODE_RAPID_REPOSITION_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "⚖️",
      id: TraitId.RiskAssessment,
      modifiers: [
        {
          stat: StatId.CooldownReduction,
          unit: ModType.PercentagePoints,
          value: 0.04
        }
      ],
      name_key: "TREE_NODE_RISK_ASSESSMENT_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🌩️",
      id: TraitId.RollingThunder,
      modifiers: [
        {
          stat: StatId.ProjectileSpeed,
          unit: ModType.Percent,
          value: 0.3
        }
      ],
      name_key: "TREE_NODE_ROLLING_THUNDER_NAME",
      rarity: TraitRarity.Magic
    },
    {
      desc_key: "",
      icon: "🗺️",
      id: TraitId.ScavengerField,
      modifiers: [
        {
          stat: StatId.Magnet,
          unit: ModType.Percent,
          value: 0.17
        }
      ],
      name_key: "TREE_NODE_SCAVENGER_FIELD_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🌬️",
      id: TraitId.SecondWind,
      modifiers: [
        {
          stat: StatId.HealthRegen,
          unit: ModType.Flat,
          value: 0.5
        }
      ],
      name_key: "TREE_NODE_SECOND_WIND_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "💨",
      id: TraitId.Slipstream,
      modifiers: [
        {
          stat: StatId.MoveSpeed,
          unit: ModType.Percent,
          value: 0.08
        }
      ],
      name_key: "TREE_NODE_SLIPSTREAM_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "🔱",
      id: TraitId.SplittingVolley,
      modifiers: [
        {
          stat: StatId.Amount,
          unit: ModType.Flat,
          value: 1.0
        }
      ],
      name_key: "TREE_NODE_SPLITTING_VOLLEY_NAME",
      rarity: TraitRarity.Normal
    },
    {
      desc_key: "",
      icon: "📡",
      id: TraitId.SwarmCartography,
      modifiers: [
        {
          stat: StatId.Magnet,
          unit: ModType.Percent,
          value: 0.4
        }
      ],
      name_key: "TREE_NODE_SWARM_CARTOGRAPHY_NAME",
      rarity: TraitRarity.Magic
    },
    {
      desc_key: "",
      icon: "🫧",
      id: TraitId.ThickCytoplasm,
      modifiers: [
        {
          stat: StatId.MaxHealth,
          unit: ModType.Percent,
          value: 0.08
        }
      ],
      name_key: "TREE_NODE_THICK_CYTOPLASM_NAME",
      rarity: TraitRarity.Normal
    }
  ]
};
