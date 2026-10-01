import { SkillId, SkillArchetype, SkillType, AilmentId, SfxId, type ActiveSkillDef } from "../../schemas/skill.schema";

export { SkillId, SkillArchetype, SkillType, AilmentId, SfxId };

export const ActiveSkills: readonly ActiveSkillDef[] = [
  {
    bio_key: "SKILL_GRASP_BIO",
    class_id: "macrophage",
    cooldown: 3,
    cooldown_per_level: [
      3,
      2.8,
      2.6,
      2.4,
      2.2
    ],
    damage_per_level: [
      28,
      36,
      46,
      58,
      72
    ],
    desc_key: "SKILL_GRASP_DESC",
    icon: "🦠",
    id: SkillId.PhagocyticGrasp,
    max_level: 5,
    name_key: "SKILL_GRASP_NAME",
    tags: [
      "Attack",
      "Melee",
      "AOE"
    ],
    type: SkillType.Innate,
    archetype: SkillArchetype.Strike,
    params: {
      damage_type: "physical",
      count: 2,
      reach: 280,
      splash_radius: 40,
      splash_mult: 0.4,
      chain_half_width: 18,
      chain_extend: 1250,
      chain_hold: 0.3,
      chain_kind: "cup",
      sfx: SfxId.HeavyStrike
    }
  },
  {
    bio_key: "SKILL_LYSOSOME_BIO",
    class_id: "",
    cooldown: 4.5,
    cooldown_per_level: [
      4.5,
      4.2,
      3.9,
      3.6,
      3.3
    ],
    damage_per_level: [
      20,
      27,
      35,
      44,
      55
    ],
    desc_key: "SKILL_LYSOSOME_DESC",
    icon: "🧪",
    id: SkillId.LysosomalOverload,
    max_level: 5,
    name_key: "SKILL_LYSOSOME_NAME",
    tags: [
      "Spell",
      "Trap",
      "AOE",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Zone,
    params: {
      damage_type: "fire",
      radius: 65,
      duration: 4,
      tick: 0.3,
      deploy_range: 450,
      deploy_fallback: 100,
      on_hit: [
        {
          ailment: AilmentId.Ignite,
          mult: 0.4,
          duration: 1.5
        },
        {
          ailment: AilmentId.Bleed,
          mult: 0.3,
          duration: 2
        }
      ],
      sfx: SfxId.Desecrate
    }
  },
  {
    bio_key: "SKILL_ROS_BIO",
    class_id: "macrophage",
    cooldown: 3.2,
    cooldown_per_level: [
      3.2,
      3,
      2.8,
      2.6,
      2.4
    ],
    damage_per_level: [
      16,
      21,
      27,
      34,
      42
    ],
    desc_key: "SKILL_ROS_DESC",
    icon: "💨",
    id: SkillId.RosTorrent,
    max_level: 5,
    name_key: "SKILL_ROS_NAME",
    tags: [
      "Spell",
      "AOE",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Salvo,
    params: {
      damage_type: "fire",
      count: 1,
      speed: 520,
      lifetime: 0.9,
      range: 650,
      spread: 0.22,
      pattern: "fan",
      steering: "linear",
      on_hit: [
        {
          ailment: AilmentId.Ignite,
          mult: 0.35,
          duration: 2
        }
      ],
      sfx: SfxId.Incinerate
    }
  },
  {
    bio_key: "SKILL_PERFORIN_BIO",
    class_id: "ctl",
    cooldown: 2.8,
    cooldown_per_level: [
      2.8,
      2.6,
      2.4,
      2.2,
      2
    ],
    damage_per_level: [
      35,
      45,
      57,
      71,
      88
    ],
    desc_key: "SKILL_PERFORIN_DESC",
    icon: "🗡️",
    id: SkillId.PerforinLance,
    max_level: 5,
    name_key: "SKILL_PERFORIN_NAME",
    tags: [
      "Attack",
      "Projectile"
    ],
    type: SkillType.Innate,
    archetype: SkillArchetype.Beam,
    params: {
      damage_type: "physical",
      mode: "pierce",
      range: 700,
      width: 24,
      count: 1,
      fan: 0.15,
      pierce: 3,
      on_hit: [
        {
          ailment: AilmentId.Shock,
          flat: 0.15,
          duration: 2.5
        }
      ],
      sfx: SfxId.HolyBolt
    }
  },
  {
    bio_key: "SKILL_COMPLEMENT_BIO",
    class_id: "neutrophil",
    cooldown: 4,
    cooldown_per_level: [
      4,
      3.7,
      3.4,
      3.1,
      2.8
    ],
    damage_per_level: [
      45,
      58,
      74,
      93,
      116
    ],
    desc_key: "SKILL_COMPLEMENT_DESC",
    icon: "💥",
    id: SkillId.ComplementCascade,
    max_level: 5,
    name_key: "SKILL_COMPLEMENT_NAME",
    tags: [
      "Spell",
      "Trap",
      "AOE",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Zone,
    params: {
      damage_type: "cold",
      count: 1,
      fuse: 1.2,
      drop_min: 50,
      drop_max: 220,
      nova_radius: 80,
      sfx: SfxId.IcicleMine,
      nova_sfx: SfxId.Explosion
    }
  },
  {
    bio_key: "SKILL_ANTIBODY_BIO",
    class_id: "b_cell",
    cooldown: 3.5,
    cooldown_per_level: [
      3.5,
      3.3,
      3.1,
      2.9,
      2.7
    ],
    damage_per_level: [
      22,
      29,
      37,
      46,
      57
    ],
    desc_key: "SKILL_ANTIBODY_DESC",
    icon: "🏹",
    id: SkillId.AntibodySalvo,
    max_level: 5,
    name_key: "SKILL_ANTIBODY_NAME",
    tags: [
      "Spell",
      "Projectile"
    ],
    type: SkillType.Innate,
    archetype: SkillArchetype.Salvo,
    params: {
      damage_type: "cold",
      count: 3,
      speed: 420,
      lifetime: 2,
      range: 600,
      spread: 0.35,
      pattern: "round_robin",
      steering: "homing",
      turn: 6,
      wobble_freq: 14,
      wobble_amp: 0.25,
      stagger_base: 0.2,
      stagger_step: 0.05,
      on_hit: [
        {
          ailment: AilmentId.Shock
        },
        {
          ailment: AilmentId.Chill,
          flat: 0.35,
          duration: 1.5
        }
      ],
      sfx: SfxId.SplitArrowFire
    }
  },
  {
    bio_key: "SKILL_LUNGE_BIO",
    class_id: "dendritic",
    cooldown: 3,
    cooldown_per_level: [
      3,
      2.8,
      2.6,
      2.4,
      2.2
    ],
    damage_per_level: [
      38,
      49,
      62,
      78,
      96
    ],
    desc_key: "SKILL_LUNGE_DESC",
    icon: "🥊",
    id: SkillId.PseudopodLunge,
    max_level: 5,
    name_key: "SKILL_LUNGE_NAME",
    tags: [
      "Attack",
      "Melee",
      "AOE"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Strike,
    params: {
      damage_type: "physical",
      count: 1,
      reach: 260,
      sort_by_distance: false,
      pull_tween: 0.15,
      chain_half_width: 16,
      chain_extend: 1800,
      chain_hold: 0.15,
      chain_kind: "fist",
      sfx: SfxId.GroundSlam
    }
  },
  {
    bio_key: "SKILL_NO_BIO",
    class_id: "",
    cooldown: 0.25,
    cooldown_per_level: [
      0.25,
      0.25,
      0.25,
      0.25,
      0.25
    ],
    damage_per_level: [
      10,
      14,
      19,
      25,
      32
    ],
    desc_key: "SKILL_NO_DESC",
    icon: "⭕",
    id: SkillId.NitricOxideHalo,
    max_level: 5,
    name_key: "SKILL_NO_NAME",
    tags: [
      "Spell",
      "AOE",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Aura,
    params: {
      damage_type: "chaos",
      mode: "radial",
      radius: 95,
      radius_offset: -48,
      plus_host_radius: true,
      on_hit: [
        {
          ailment: AilmentId.Bleed,
          mult: 0.25,
          duration: 1.5
        }
      ],
      sfx: SfxId.ShockNova
    }
  },
  {
    bio_key: "SKILL_NUCLEASE_BIO",
    class_id: "",
    cooldown: 4.5,
    cooldown_per_level: [
      4.5,
      4.2,
      3.9,
      3.6,
      3.3
    ],
    damage_per_level: [
      18,
      24,
      31,
      39,
      48
    ],
    desc_key: "SKILL_NUCLEASE_DESC",
    icon: "⛓️",
    id: SkillId.NucleaseBlades,
    max_level: 5,
    name_key: "SKILL_NUCLEASE_NAME",
    tags: [
      "Spell",
      "Projectile",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Aura,
    params: {
      damage_type: "physical",
      mode: "orbital",
      blades: 2,
      orbit: 115,
      rot_speed: 3.8,
      tick: 0.22,
      blade_radius: 28,
      sfx: SfxId.EtherealKnives
    }
  },
  {
    bio_key: "SKILL_GRANZYME_BIO",
    class_id: "neutrophil",
    cooldown: 3.5,
    cooldown_per_level: [
      3.5,
      3.2,
      2.9,
      2.6,
      2.3
    ],
    damage_per_level: [
      130,
      165,
      208,
      260,
      325
    ],
    desc_key: "SKILL_GRANZYME_DESC",
    icon: "🧬",
    id: SkillId.GranzymeDetonation,
    max_level: 5,
    name_key: "SKILL_GRANZYME_NAME",
    tags: [
      "Spell",
      "AOE",
      "Duration"
    ],
    type: SkillType.Innate,
    archetype: SkillArchetype.Nova,
    params: {
      damage_type: "chaos",
      shape: "sphere",
      radius: 110,
      delay: 1.5,
      follow_target: true,
      marker: true,
      range: 550,
      sfx: SfxId.DischargeBlast
    }
  },
  {
    bio_key: "SKILL_INTERFERON_BIO",
    class_id: "",
    cooldown: 6,
    cooldown_per_level: [
      6,
      5.6,
      5.2,
      4.8,
      4.4
    ],
    damage_per_level: [
      32,
      42,
      54,
      68,
      85
    ],
    desc_key: "SKILL_INTERFERON_DESC",
    icon: "🌊",
    id: SkillId.InterferonWave,
    max_level: 5,
    name_key: "SKILL_INTERFERON_NAME",
    tags: [
      "Spell",
      "AOE"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Nova,
    params: {
      damage_type: "cold",
      shape: "sphere",
      radius: 480,
      knockback_dist: 65,
      knockback_time: 0.2,
      slow_duration: 2,
      slow_factor: 0.5,
      sfx: SfxId.IceNova
    }
  },
  {
    bio_key: "SKILL_LYSOZYME_BIO",
    class_id: "",
    cooldown: 3.8,
    cooldown_per_level: [
      3.8,
      3.5,
      3.2,
      2.9,
      2.6
    ],
    damage_per_level: [
      24,
      31,
      40,
      50,
      62
    ],
    desc_key: "SKILL_LYSOZYME_DESC",
    icon: "🧪",
    id: SkillId.LysozymeRicochet,
    max_level: 5,
    name_key: "SKILL_LYSOZYME_NAME",
    tags: [
      "Spell",
      "Projectile"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Salvo,
    params: {
      damage_type: "cold",
      count: 1,
      speed: 520,
      lifetime: 3,
      hit_radius: 22,
      pattern: "random",
      spread: 0.35,
      steering: "chain",
      reacquire: 350,
      pierce: 4,
      sfx: SfxId.ColdSnap
    }
  },
  {
    bio_key: "SKILL_PHAGO_VENT_BIO",
    class_id: "",
    cooldown: 2,
    cooldown_per_level: [
      2,
      1.9,
      1.8,
      1.7,
      1.6
    ],
    damage_per_level: [
      14,
      19,
      25,
      32,
      40
    ],
    desc_key: "SKILL_PHAGO_VENT_DESC",
    icon: "🛢️",
    id: SkillId.PhagolysosomeVent,
    max_level: 5,
    name_key: "SKILL_PHAGO_VENT_NAME",
    tags: [
      "Spell",
      "Trap",
      "AOE",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Zone,
    params: {
      damage_type: "chaos",
      radius: 42,
      duration: 3.5,
      tick: 0.35,
      deploy: "self",
      sfx: SfxId.ToxicRain
    }
  },
  {
    bio_key: "SKILL_PRO_INFLAM_BIO",
    class_id: "",
    cooldown: 3,
    cooldown_per_level: [
      3,
      2.8,
      2.6,
      2.4,
      2.2
    ],
    desc_key: "SKILL_PRO_INFLAM_DESC",
    icon: "⚡",
    id: SkillId.ProInflammatoryArc,
    max_level: 5,
    name_key: "SKILL_PRO_INFLAM_NAME",
    tags: [
      "Spell",
      "AOE"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Beam,
    damage_per_level: [
      35,
      45,
      57,
      71,
      88
    ],
    params: {
      damage_type: "lightning",
      mode: "chain",
      range: 480,
      chain_range: 240,
      count: 3,
      sfx: SfxId.Spark
    }
  },
  {
    bio_key: "SKILL_EXOSOME_BIO",
    class_id: "",
    cooldown: 5.5,
    cooldown_per_level: [
      5.5,
      5.1,
      4.7,
      4.3,
      3.9
    ],
    damage_per_level: [
      12,
      16,
      21,
      27,
      34
    ],
    desc_key: "SKILL_EXOSOME_DESC",
    icon: "🧲",
    id: SkillId.ExosomeSingularity,
    max_level: 5,
    name_key: "SKILL_EXOSOME_NAME",
    tags: [
      "Spell",
      "AOE",
      "Duration"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Zone,
    params: {
      damage_type: "chaos",
      radius: 175,
      duration: 3,
      tick: 0.4,
      pull: 220,
      pull_eff: 0.4,
      deploy_range: 450,
      deploy_fallback: 140,
      sfx: SfxId.Vortex
    }
  },
  {
    bio_key: "SKILL_DEFENSIN_BIO",
    class_id: "",
    cooldown: 3,
    cooldown_per_level: [
      3,
      2.8,
      2.6,
      2.4,
      2.2
    ],
    damage_per_level: [
      22,
      28,
      36,
      45,
      56
    ],
    desc_key: "SKILL_DEFENSIN_DESC",
    icon: "🛡️",
    id: SkillId.DefensinBarbs,
    max_level: 5,
    name_key: "SKILL_DEFENSIN_NAME",
    tags: [
      "Attack",
      "Projectile",
      "AOE"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Salvo,
    params: {
      damage_type: "physical",
      count: 8,
      speed: 460,
      lifetime: 1.6,
      hit_radius: 20,
      pattern: "radial",
      steering: "linear",
      pierce: 2,
      sfx: SfxId.EtherealKnives
    }
  },
  {
    bio_key: "SKILL_MHC_TRACER_BIO",
    class_id: "dendritic",
    cooldown: 4,
    cooldown_per_level: [
      4,
      3.7,
      3.4,
      3.1,
      2.8
    ],
    damage_per_level: [
      32,
      41,
      52,
      65,
      80
    ],
    desc_key: "SKILL_MHC_TRACER_DESC",
    icon: "🎯",
    id: SkillId.MhcTracerBeam,
    max_level: 5,
    name_key: "SKILL_MHC_TRACER_NAME",
    tags: [
      "Spell",
      "Duration"
    ],
    type: SkillType.Innate,
    archetype: SkillArchetype.Beam,
    params: {
      damage_type: "fire",
      mode: "channel",
      range: 520,
      duration: 2,
      meta: "mhc_marked",
      sfx: SfxId.ScorchingRay
    }
  },
  {
    bio_key: "SKILL_HISTAMINE_BIO",
    class_id: "",
    cooldown: 4.8,
    cooldown_per_level: [
      4.8,
      4.4,
      4,
      3.6,
      3.2
    ],
    damage_per_level: [
      48,
      62,
      79,
      99,
      124
    ],
    desc_key: "SKILL_HISTAMINE_DESC",
    icon: "💉",
    id: SkillId.HistamineSurge,
    max_level: 5,
    name_key: "SKILL_HISTAMINE_NAME",
    tags: [
      "Spell",
      "AOE"
    ],
    type: SkillType.Active,
    archetype: SkillArchetype.Nova,
    params: {
      damage_type: "fire",
      shape: "cone",
      reach: 320,
      angle: 100,
      knockback_dist: 80,
      knockback_time: 0.2,
      sfx: SfxId.DischargeBlast
    }
  }
];
