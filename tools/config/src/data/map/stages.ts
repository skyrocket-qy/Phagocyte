import { StageId, StageEffectKind, StageProp, StatId, type StageDef } from "../../schemas/stage.schema";

export { StageId, StageEffectKind, StageProp, StatId };

export const Stages: readonly StageDef[] = [
  {
    bg_color: "#0d141fff",
    bg_color_accent: "#240a14ff",
    bg_color_deep: "#0a0d17ff",
    bio_key: "MAP_WOUND_BIO",
    color_code: "#e63946ff",
    difficulty: 1,
    env_key: "MAP_WOUND_ENV",
    fiber_color: "#382e5259",
    hard_unlocked: false,
    id: StageId.AcuteWound,
    effects: [],
    mech_key: "MAP_WOUND_MECH",
    name_key: "MAP_WOUND_NAME",
    organ_icon: "🩹",
    organ_key: "ORGAN_SKIN",
    scanner_pos: [
      0.26,
      0.48
    ],
    subtitle_key: "MAP_WOUND_SUBTITLE",
    threat_key: "MAP_WOUND_THREAT",
    unlocked: true
  },
  {
    bg_color: "#0a1c21ff",
    bg_color_accent: "#0a1f29ff",
    bg_color_deep: "#050f1aff",
    bio_key: "MAP_ALVEOLAR_BIO",
    color_code: "#2a9d8fff",
    difficulty: 2,
    env_key: "MAP_ALVEOLAR_ENV",
    fiber_color: "#3380a659",
    hard_unlocked: false,
    id: StageId.AlveolarSpace,
    effects: [
      {
        kind: StageEffectKind.Spawner,
        prop: StageProp.BuffZone,
        interval: 6.0,
        max_active: 3,
        initial_delay: 3.0,
        min_dist: 200.0,
        max_dist: 700.0,
        anchor: "player",
        overrides: {
          stat_id: StatId.CooldownReduction,
          bonus: 0.25,
          duration: 6.0,
          radius: 82.0,
          lifetime: 22.0
        }
      }
    ],
    mech_key: "MAP_ALVEOLAR_MECH",
    name_key: "MAP_ALVEOLAR_NAME",
    organ_icon: "🫁",
    organ_key: "ORGAN_LUNGS",
    scanner_pos: [
      0.5,
      0.28
    ],
    subtitle_key: "MAP_ALVEOLAR_SUBTITLE",
    threat_key: "MAP_ALVEOLAR_THREAT",
    unlocked: false
  },
  {
    bg_color: "#0d0805ff",
    bg_color_accent: "#2e170aff",
    bg_color_deep: "#0d0805ff",
    bio_key: "MAP_HEPATIC_BIO",
    color_code: "#e76f51ff",
    difficulty: 3,
    env_key: "MAP_HEPATIC_ENV",
    fiber_color: "#8c662659",
    hard_unlocked: false,
    id: StageId.HepaticSinusoid,
    effects: [
      {
        kind: StageEffectKind.StatStrip,
        stat: StatId.Armor,
        duration: 3.0,
        interval: 18.0,
        initial_delay: 10.0
      },
      {
        kind: StageEffectKind.Spawner,
        prop: StageProp.BlockerWall,
        interval: 24.0,
        max_active: 3,
        initial_delay: 5.0,
        min_dist: 300.0,
        max_dist: 900.0,
        anchor: "arena",
        arena_scale: 0.7,
        orientation: "random",
        overrides: {
          gap_half_width: 55.0,
          length: 320.0
        }
      }
    ],
    mech_key: "MAP_HEPATIC_MECH",
    name_key: "MAP_HEPATIC_NAME",
    organ_icon: "🫀",
    organ_key: "ORGAN_LIVER",
    scanner_pos: [
      0.42,
      0.38
    ],
    subtitle_key: "MAP_HEPATIC_SUBTITLE",
    threat_key: "MAP_HEPATIC_THREAT",
    unlocked: false
  },
  {
    bg_color: "#0f0a03ff",
    bg_color_accent: "#2b1f05ff",
    bg_color_deep: "#0f0a03ff",
    bio_key: "MAP_GASTRIC_BIO",
    color_code: "#f4a261ff",
    difficulty: 4,
    env_key: "MAP_GASTRIC_ENV",
    fiber_color: "#99801a59",
    hard_unlocked: false,
    id: StageId.GastricLumen,
    effects: [
      {
        kind: StageEffectKind.Spawner,
        prop: StageProp.SafeZone,
        interval: 9.0,
        max_active: 3,
        initial_delay: 3.0,
        min_dist: 200.0,
        max_dist: 900.0,
        anchor: "arena",
        arena_scale: 0.8,
        overrides: {
          radius: 120.0,
          lifetime: 16.0
        }
      },
      {
        kind: StageEffectKind.Volley,
        prop: StageProp.DotZone,
        count: 2,
        interval: 14.0,
        initial_delay: 6.0,
        min_dist: 120.0,
        max_dist: 760.0,
        anchor: "player",
        overrides: {
          radius: 260.0,
          lifetime: 6.0
        }
      },
      {
        kind: StageEffectKind.DotScan,
        dps: 7.0,
        slow_factor: 0.3,
        slow_duration: 0.7,
        safe_zone: true
      }
    ],
    mech_key: "MAP_GASTRIC_MECH",
    name_key: "MAP_GASTRIC_NAME",
    organ_icon: "🌋",
    organ_key: "ORGAN_STOMACH",
    scanner_pos: [
      0.58,
      0.4
    ],
    subtitle_key: "MAP_GASTRIC_SUBTITLE",
    threat_key: "MAP_GASTRIC_THREAT",
    unlocked: false
  },
  {
    bg_color: "#080512ff",
    bg_color_accent: "#140a26ff",
    bg_color_deep: "#080512ff",
    bio_key: "MAP_BBB_BIO",
    color_code: "#9d4eddff",
    difficulty: 5,
    env_key: "MAP_BBB_ENV",
    fiber_color: "#5940a666",
    hard_unlocked: false,
    id: StageId.BloodBrainBarrier,
    effects: [
      {
        kind: StageEffectKind.Scatter,
        prop: StageProp.BlockerPillar,
        count: 5,
        ring_radius: 620.0,
        angle_offset: 0.4,
        radius_base: 70.0,
        radius_step: 14.0
      },
      {
        kind: StageEffectKind.Scramble,
        duration: 1.6,
        hard_mult: 1.5,
        interval: 11.0,
        initial_delay: 6.0
      }
    ],
    mech_key: "MAP_BBB_MECH",
    name_key: "MAP_BBB_NAME",
    organ_icon: "🧠",
    organ_key: "ORGAN_BRAIN",
    scanner_pos: [
      0.5,
      0.11
    ],
    subtitle_key: "MAP_BBB_SUBTITLE",
    threat_key: "MAP_BBB_THREAT",
    unlocked: false
  }
];
