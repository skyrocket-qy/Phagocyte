import { SkillId, StatId, PassiveModMode, type PassiveSkillDef } from "../../schemas/skill.schema";

export { SkillId, StatId, PassiveModMode };

export const PassiveSkills: readonly PassiveSkillDef[] = [
  {
    bio_key: "SKILL_ACTIN_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_ACTIN_DESC",
    icon: "🧬",
    id: SkillId.Actin,
    max_level: 5,
    name_key: "SKILL_ACTIN_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.Area,
        per_level: 0.12,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.MoveSpeed,
        per_level: 0.06,
        mode: PassiveModMode.Mult
      }
    ]
  },
  {
    bio_key: "SKILL_LYSOSOME_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "TREE_NODE_LYSOSOME_DESC",
    icon: "🧪",
    id: SkillId.Lysosome,
    max_level: 5,
    name_key: "TREE_NODE_LYSOSOME_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.Might,
        per_level: 0.1,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.HealthRegen,
        per_level: 0.6,
        mode: PassiveModMode.Flat
      }
    ]
  },
  {
    bio_key: "SKILL_MITOCHONDRIA_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_MITOCHONDRIA_DESC",
    icon: "⚡",
    id: SkillId.Mitochondria,
    max_level: 5,
    name_key: "SKILL_MITOCHONDRIA_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.CooldownReduction,
        per_level: 0.08,
        mode: PassiveModMode.Flat
      },
      {
        stat: StatId.Duration,
        per_level: 0.1,
        mode: PassiveModMode.Mult
      }
    ]
  },
  {
    bio_key: "SKILL_OPSONIN_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_OPSONIN_DESC",
    icon: "🎯",
    id: SkillId.Opsonin,
    max_level: 5,
    name_key: "SKILL_OPSONIN_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.CritChance,
        per_level: 0.05,
        mode: PassiveModMode.Flat
      },
      {
        stat: StatId.CritDamage,
        per_level: 0.25,
        mode: PassiveModMode.Mult
      }
    ]
  },
  {
    bio_key: "SKILL_CHEMOKINE_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_CHEMOKINE_DESC",
    icon: "🧲",
    id: SkillId.Chemokine,
    max_level: 5,
    name_key: "SKILL_CHEMOKINE_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.Magnet,
        per_level: 0.25,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.MoveSpeed,
        per_level: 0.06,
        mode: PassiveModMode.Mult
      }
    ]
  },
  {
    bio_key: "SKILL_BILAYER_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_BILAYER_DESC",
    icon: "🛡️",
    id: SkillId.Bilayer,
    max_level: 5,
    name_key: "SKILL_BILAYER_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.MaxHealth,
        per_level: 0.15,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.Armor,
        per_level: 2.0,
        mode: PassiveModMode.Flat
      }
    ]
  },
  {
    bio_key: "SKILL_AUTOPHAGY_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_AUTOPHAGY_DESC",
    icon: "🔄",
    id: SkillId.Autophagy,
    max_level: 5,
    name_key: "SKILL_AUTOPHAGY_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.HealthRegen,
        per_level: 0.4,
        mode: PassiveModMode.Flat
      }
    ]
  },
  {
    bio_key: "SKILL_GLYCOLYSIS_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_GLYCOLYSIS_DESC",
    icon: "🍬",
    id: SkillId.Glycolysis,
    max_level: 5,
    name_key: "SKILL_GLYCOLYSIS_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.MoveSpeed,
        per_level: 0.06,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.Might,
        per_level: 0.05,
        mode: PassiveModMode.Mult
      }
    ]
  },
  {
    bio_key: "SKILL_KINESIN_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_KINESIN_DESC",
    icon: "🛤️",
    id: SkillId.Kinesin,
    max_level: 5,
    name_key: "SKILL_KINESIN_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.ProjectileSpeed,
        per_level: 0.15,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.Pierce,
        per_level: 1.0,
        mode: PassiveModMode.FlatOnce
      }
    ]
  },
  {
    bio_key: "SKILL_LONGEVITY_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_LONGEVITY_DESC",
    icon: "⏳",
    id: SkillId.Longevity,
    max_level: 5,
    name_key: "SKILL_LONGEVITY_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.Duration,
        per_level: 0.15,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.Knockback,
        per_level: 0.1,
        mode: PassiveModMode.Mult
      }
    ]
  },
  {
    bio_key: "SKILL_VDJ_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_VDJ_DESC",
    icon: "🎲",
    id: SkillId.Vdj,
    max_level: 5,
    name_key: "SKILL_VDJ_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.CritDamage,
        per_level: 0.15,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.CritChance,
        per_level: 0.03,
        mode: PassiveModMode.Flat
      }
    ]
  },
  {
    bio_key: "SKILL_ENDOTOXIN_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_ENDOTOXIN_DESC",
    icon: "🧱",
    id: SkillId.Endotoxin,
    max_level: 5,
    name_key: "SKILL_ENDOTOXIN_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.Armor,
        per_level: 3.0,
        mode: PassiveModMode.Flat
      },
      {
        stat: StatId.Block,
        per_level: 0.04,
        mode: PassiveModMode.Flat
      }
    ]
  },
  {
    bio_key: "SKILL_HEMATOPOIETIC_BIO",
    class_id: "",
    cooldown: 0.0,
    desc_key: "SKILL_HEMATOPOIETIC_DESC",
    icon: "🩸",
    id: SkillId.Hematopoietic,
    max_level: 5,
    name_key: "SKILL_HEMATOPOIETIC_NAME",
    type: "passive",
    archetype: "passive",
    mods: [
      {
        stat: StatId.MaxHealth,
        per_level: 0.1,
        mode: PassiveModMode.Mult
      },
      {
        stat: StatId.Block,
        per_level: 0.03,
        mode: PassiveModMode.Flat
      }
    ]
  }
];
