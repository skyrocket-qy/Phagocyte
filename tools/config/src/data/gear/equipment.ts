import { GearId, GearCategory, StatId, ModType, type EquipmentDef } from "../../schemas/gear.schema";

export { GearId, GearCategory, StatId, ModType };

export const Equipment: readonly EquipmentDef[] = [
  {
    bio_key: "ORGANELLE_MITO_MKII_BIO",
    category: GearCategory.Metabolism,
    desc_key: "ORGANELLE_MITO_MKII_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "⚡",
    id: GearId.MitochondriaMkii,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.CooldownReduction,
        unit: ModType.PercentagePoints,
        value: 0.16
      },
      {
        stat: StatId.Duration,
        unit: ModType.Percent,
        value: 0.1
      }
    ],
    name_key: "ORGANELLE_MITO_MKII_NAME"
  },
  {
    bio_key: "ORGANELLE_GLYCOLYTIC_BYPASS_BIO",
    category: GearCategory.Metabolism,
    desc_key: "ORGANELLE_GLYCOLYTIC_BYPASS_DESC",
    drawback: [],
    energy_cost: 1,
    icon: "🍬",
    id: GearId.GlycolyticBypass,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.CooldownReduction,
        unit: ModType.PercentagePoints,
        value: 0.05
      },
      {
        stat: StatId.MoveSpeed,
        unit: ModType.Percent,
        value: 0.03
      }
    ],
    name_key: "ORGANELLE_GLYCOLYTIC_BYPASS_NAME"
  },
  {
    bio_key: "ORGANELLE_ACIDIC_LYSOSOME_BIO",
    category: GearCategory.Digestion,
    desc_key: "ORGANELLE_ACIDIC_LYSOSOME_DESC",
    drawback: [],
    energy_cost: 3,
    icon: "🧪",
    id: GearId.AcidicLysosome,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Might,
        unit: ModType.Percent,
        value: 0.12
      },
      {
        stat: StatId.DotDamage,
        unit: ModType.Percent,
        value: 0.15
      }
    ],
    name_key: "ORGANELLE_ACIDIC_LYSOSOME_NAME"
  },
  {
    bio_key: "ORGANELLE_PROTEASOME_SIEVE_BIO",
    category: GearCategory.Digestion,
    desc_key: "ORGANELLE_PROTEASOME_SIEVE_DESC",
    drawback: [],
    energy_cost: 1,
    icon: "🧫",
    id: GearId.ProteasomeSieve,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.DotDamage,
        unit: ModType.Percent,
        value: 0.08
      },
      {
        stat: StatId.HealthRegen,
        unit: ModType.Flat,
        value: 0.3
      }
    ],
    name_key: "ORGANELLE_PROTEASOME_SIEVE_NAME"
  },
  {
    bio_key: "ORGANELLE_FLAGELLAR_BASE_BIO",
    category: GearCategory.Cytoskeleton,
    desc_key: "ORGANELLE_FLAGELLAR_BASE_DESC",
    drawback: [],
    energy_cost: 3,
    icon: "🌀",
    id: GearId.FlagellarBase,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.MoveSpeed,
        unit: ModType.Percent,
        value: 0.12
      }
    ],
    name_key: "ORGANELLE_FLAGELLAR_BASE_NAME"
  },
  {
    bio_key: "ORGANELLE_MICROTUBULE_ANCHOR_BIO",
    category: GearCategory.Cytoskeleton,
    desc_key: "ORGANELLE_MICROTUBULE_ANCHOR_DESC",
    drawback: [],
    energy_cost: 1,
    icon: "🔗",
    id: GearId.MicrotubuleAnchor,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.MoveSpeed,
        unit: ModType.Percent,
        value: 0.04
      },
      {
        stat: StatId.Area,
        unit: ModType.Percent,
        value: 0.04
      }
    ],
    name_key: "ORGANELLE_MICROTUBULE_ANCHOR_NAME"
  },
  {
    bio_key: "ORGANELLE_ROUGH_ER_BIO",
    category: GearCategory.Synthesis,
    desc_key: "ORGANELLE_ROUGH_ER_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "🏭",
    id: GearId.RoughEr,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Amount,
        unit: ModType.Flat,
        value: 1.0
      },
      {
        stat: StatId.ProjectileSpeed,
        unit: ModType.Percent,
        value: 0.08
      }
    ],
    name_key: "ORGANELLE_ROUGH_ER_NAME"
  },
  {
    bio_key: "ORGANELLE_RIBOSOME_CLUSTER_BIO",
    category: GearCategory.Synthesis,
    desc_key: "ORGANELLE_RIBOSOME_CLUSTER_DESC",
    drawback: [],
    energy_cost: 2,
    icon: "🧬",
    id: GearId.RibosomeCluster,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.ProjectileSpeed,
        unit: ModType.Percent,
        value: 0.08
      },
      {
        stat: StatId.Duration,
        unit: ModType.Percent,
        value: 0.08
      }
    ],
    name_key: "ORGANELLE_RIBOSOME_CLUSTER_NAME"
  },
  {
    bio_key: "ORGANELLE_ION_CHANNEL_ARRAY_BIO",
    category: GearCategory.Sensing,
    desc_key: "ORGANELLE_ION_CHANNEL_ARRAY_DESC",
    drawback: [],
    energy_cost: 3,
    icon: "📡",
    id: GearId.IonChannelArray,
    max_copies: 1,
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
      },
      {
        stat: StatId.Magnet,
        unit: ModType.Percent,
        value: 0.15
      }
    ],
    name_key: "ORGANELLE_ION_CHANNEL_ARRAY_NAME"
  },
  {
    bio_key: "ORGANELLE_CHEMOKINE_PATCH_BIO",
    category: GearCategory.Sensing,
    desc_key: "ORGANELLE_CHEMOKINE_PATCH_DESC",
    drawback: [],
    energy_cost: 1,
    icon: "🧲",
    id: GearId.ChemokinePatch,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Magnet,
        unit: ModType.Percent,
        value: 0.2
      },
      {
        stat: StatId.Evasion,
        unit: ModType.Flat,
        value: 0.02
      }
    ],
    name_key: "ORGANELLE_CHEMOKINE_PATCH_NAME"
  },
  {
    bio_key: "ORGANELLE_SYMBIOTIC_FLORA_BIO",
    category: GearCategory.Symbiosis,
    desc_key: "ORGANELLE_SYMBIOTIC_FLORA_DESC",
    drawback: [
      {
        stat: StatId.MoveSpeed,
        unit: ModType.Percent,
        value: -0.3
      },
      {
        stat: StatId.Might,
        unit: ModType.Percent,
        value: -0.15
      }
    ],
    energy_cost: -1,
    icon: "🦠",
    id: GearId.SymbioticFlora,
    max_copies: 1,
    modifiers: [],
    name_key: "ORGANELLE_SYMBIOTIC_FLORA_NAME"
  },
  {
    bio_key: "ORGANELLE_PHAGE_FRAGMENT_BIO",
    category: GearCategory.Symbiosis,
    desc_key: "ORGANELLE_PHAGE_FRAGMENT_DESC",
    drawback: [
      {
        stat: StatId.MaxHealth,
        unit: ModType.Percent,
        value: -0.2
      }
    ],
    energy_cost: -1,
    icon: "👾",
    id: GearId.PhageFragment,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.CooldownReduction,
        unit: ModType.PercentagePoints,
        value: 0.05
      }
    ],
    name_key: "ORGANELLE_PHAGE_FRAGMENT_NAME"
  },
  {
    bio_key: "ORGANELLE_ATP_SHUTTLE_BIO",
    category: GearCategory.Metabolism,
    desc_key: "ORGANELLE_ATP_SHUTTLE_DESC",
    drawback: [],
    energy_cost: 2,
    icon: "🔋",
    id: GearId.AtpShuttle,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.MoveSpeed,
        unit: ModType.Percent,
        value: 0.06
      },
      {
        stat: StatId.Duration,
        unit: ModType.Percent,
        value: 0.06
      }
    ],
    name_key: "ORGANELLE_ATP_SHUTTLE_NAME"
  },
  {
    bio_key: "ORGANELLE_KREBS_CYCLE_BIO",
    category: GearCategory.Metabolism,
    desc_key: "ORGANELLE_KREBS_CYCLE_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "♻️",
    id: GearId.KrebsCycle,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.CooldownReduction,
        unit: ModType.PercentagePoints,
        value: 0.1
      },
      {
        stat: StatId.Might,
        unit: ModType.Percent,
        value: 0.1
      }
    ],
    name_key: "ORGANELLE_KREBS_CYCLE_NAME"
  },
  {
    bio_key: "ORGANELLE_BILE_SALT_POOL_BIO",
    category: GearCategory.Digestion,
    desc_key: "ORGANELLE_BILE_SALT_POOL_DESC",
    drawback: [],
    energy_cost: 2,
    icon: "🟡",
    id: GearId.BileSaltPool,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.DotDamage,
        unit: ModType.Percent,
        value: 0.1
      },
      {
        stat: StatId.HealthRegen,
        unit: ModType.Flat,
        value: 0.4
      }
    ],
    name_key: "ORGANELLE_BILE_SALT_POOL_NAME"
  },
  {
    bio_key: "ORGANELLE_PHAGOLYSOSOME_CORE_BIO",
    category: GearCategory.Digestion,
    desc_key: "ORGANELLE_PHAGOLYSOSOME_CORE_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "🟣",
    id: GearId.PhagolysosomeCore,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Might,
        unit: ModType.Percent,
        value: 0.12
      },
      {
        stat: StatId.LifeSteal,
        unit: ModType.Flat,
        value: 0.05
      }
    ],
    name_key: "ORGANELLE_PHAGOLYSOSOME_CORE_NAME"
  },
  {
    bio_key: "ORGANELLE_ACTIN_MESH_BIO",
    category: GearCategory.Cytoskeleton,
    desc_key: "ORGANELLE_ACTIN_MESH_DESC",
    drawback: [],
    energy_cost: 2,
    icon: "🕸️",
    id: GearId.ActinMesh,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Area,
        unit: ModType.Percent,
        value: 0.08
      },
      {
        stat: StatId.Evasion,
        unit: ModType.Flat,
        value: 0.02
      }
    ],
    name_key: "ORGANELLE_ACTIN_MESH_NAME"
  },
  {
    bio_key: "ORGANELLE_CENTROSOME_ARRAY_BIO",
    category: GearCategory.Cytoskeleton,
    desc_key: "ORGANELLE_CENTROSOME_ARRAY_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "🎯",
    id: GearId.CentrosomeArray,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Pierce,
        unit: ModType.Flat,
        value: 1.0
      }
    ],
    name_key: "ORGANELLE_CENTROSOME_ARRAY_NAME"
  },
  {
    bio_key: "ORGANELLE_GOLGI_STACK_BIO",
    category: GearCategory.Synthesis,
    desc_key: "ORGANELLE_GOLGI_STACK_DESC",
    drawback: [],
    energy_cost: 2,
    icon: "📦",
    id: GearId.GolgiStack,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.ProjectileSpeed,
        unit: ModType.Percent,
        value: 0.12
      },
      {
        stat: StatId.Area,
        unit: ModType.Percent,
        value: 0.05
      }
    ],
    name_key: "ORGANELLE_GOLGI_STACK_NAME"
  },
  {
    bio_key: "ORGANELLE_NUCLEOLUS_PRIME_BIO",
    category: GearCategory.Synthesis,
    desc_key: "ORGANELLE_NUCLEOLUS_PRIME_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "🌟",
    id: GearId.NucleolusPrime,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Amount,
        unit: ModType.Flat,
        value: 1.0
      },
      {
        stat: StatId.CritDamage,
        unit: ModType.Percent,
        value: 0.15
      }
    ],
    name_key: "ORGANELLE_NUCLEOLUS_PRIME_NAME"
  },
  {
    bio_key: "ORGANELLE_TOLL_RECEPTOR_BIO",
    category: GearCategory.Sensing,
    desc_key: "ORGANELLE_TOLL_RECEPTOR_DESC",
    drawback: [],
    energy_cost: 2,
    icon: "🔔",
    id: GearId.TollReceptor,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Block,
        unit: ModType.Flat,
        value: 0.03
      },
      {
        stat: StatId.Evasion,
        unit: ModType.Flat,
        value: 0.02
      }
    ],
    name_key: "ORGANELLE_TOLL_RECEPTOR_NAME"
  },
  {
    bio_key: "ORGANELLE_MEMBRANE_RAFT_BIO",
    category: GearCategory.Sensing,
    desc_key: "ORGANELLE_MEMBRANE_RAFT_DESC",
    drawback: [],
    energy_cost: 4,
    icon: "🛟",
    id: GearId.MembraneRaft,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Armor,
        unit: ModType.Flat,
        value: 5.0
      },
      {
        stat: StatId.Block,
        unit: ModType.Flat,
        value: 0.03
      }
    ],
    name_key: "ORGANELLE_MEMBRANE_RAFT_NAME"
  },
  {
    bio_key: "ORGANELLE_REDOX_SYMBIONT_BIO",
    category: GearCategory.Symbiosis,
    desc_key: "ORGANELLE_REDOX_SYMBIONT_DESC",
    drawback: [
      {
        stat: StatId.Armor,
        unit: ModType.Flat,
        value: -3.0
      },
      {
        stat: StatId.Evasion,
        unit: ModType.Flat,
        value: -0.03
      }
    ],
    energy_cost: -1,
    icon: "🪸",
    id: GearId.RedoxSymbiont,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Might,
        unit: ModType.Percent,
        value: 0.1
      }
    ],
    name_key: "ORGANELLE_REDOX_SYMBIONT_NAME"
  },
  {
    bio_key: "ORGANELLE_QUORUM_COLONY_BIO",
    category: GearCategory.Symbiosis,
    desc_key: "ORGANELLE_QUORUM_COLONY_DESC",
    drawback: [
      {
        stat: StatId.MoveSpeed,
        unit: ModType.Percent,
        value: -0.15
      },
      {
        stat: StatId.MaxHealth,
        unit: ModType.Percent,
        value: -0.1
      }
    ],
    energy_cost: -1,
    icon: "🐝",
    id: GearId.QuorumColony,
    max_copies: 1,
    modifiers: [
      {
        stat: StatId.Magnet,
        unit: ModType.Percent,
        value: 0.25
      },
      {
        stat: StatId.HealthRegen,
        unit: ModType.Flat,
        value: 0.5
      }
    ],
    name_key: "ORGANELLE_QUORUM_COLONY_NAME"
  }
];
