import { EnemyId, ThreatMode, type EnemyDef } from "../../schemas/enemy.schema";

export { EnemyId, ThreatMode };

export const Enemies: readonly EnemyDef[] = [
  {
    id: EnemyId.Staph,
    max_health: 25.0,
    armor: 0.0,
    xp: 12.0,
    score: 15,
    float_speed: 35.0,
    contact_damage: 3.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 1.0,
    tint: "",
    spawn_cluster: {
      count: 3,
      spread: 30.0
    },
    traits: {}
  },
  {
    id: EnemyId.Norovirus,
    max_health: 8.0,
    armor: 0.0,
    xp: 3.5,
    score: 5,
    float_speed: 50.0,
    contact_damage: 3.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 0.03,
    tint: "#4DFFFF",
    spawn_cluster: {
      count: 10,
      spread: 50.0
    },
    traits: {}
  },
  {
    id: EnemyId.SVirus,
    max_health: 22.0,
    armor: 0.0,
    xp: 10.0,
    score: 15,
    float_speed: 38.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Standoff,
    elite: false,
    boss: false,
    body_microns: 0.1,
    tint: "",
    steering: {
      preferred_range: 280.0
    },
    traits: {
      ranged: {
        interval: 2.5,
        initial_delay: 1.6,
        range: 640.0,
        damage: 9.0,
        speed: 300.0,
        lifetime: 5.0,
        spawn_offset: 16.0,
        core_color: "#FFA633F2",
        aura_color: "#F2666659"
      },
      replicate: {
        interval: 18.0,
        spread: 30.0
      },
      contact: {
        kind: "slow",
        factor: 0.65,
        duration: 2.5,
        margin: 12.0
      }
    }
  },
  {
    id: EnemyId.FluDrift,
    max_health: 65.0,
    armor: 0.0,
    xp: 26.0,
    score: 100,
    float_speed: 42.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: true,
    boss: false,
    body_microns: 0.12,
    tint: "",
    traits: {
      cleanse_pulse: {
        interval: 25.0,
        radius: 250.0,
        knockback: 200.0
      }
    }
  },
  {
    id: EnemyId.MalignantCell,
    max_health: 120.0,
    armor: 3.0,
    xp: 48.0,
    score: 100,
    float_speed: 26.0,
    contact_damage: 6.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: true,
    boss: false,
    body_microns: 15.0,
    tint: "",
    traits: {
      mitosis: {
        interval: 20.0,
        radius: 450.0,
        cap: 5
      }
    }
  },
  {
    id: EnemyId.Pseudomonas,
    max_health: 30.0,
    armor: 0.0,
    xp: 14.0,
    score: 35,
    float_speed: 42.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 2.0,
    tint: "",
    traits: {
      death_drop: {
        radius: 75.0,
        lifetime: 8.0,
        dps: 0.0,
        slow_factor: 0.5,
        slow_duration: 1.0
      }
    }
  },
  {
    id: EnemyId.EColi,
    max_health: 28.0,
    armor: 0.0,
    xp: 15.0,
    score: 15,
    float_speed: 45.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 2.0,
    tint: "",
    traits: {
      charge: {
        trigger_time: 3.0,
        trigger_range: 500.0,
        windup: 0.6,
        dash_speed: 320.0,
        damage: 12.0,
        knockback: 350.0,
        rebound: 50.0,
        charge_time: 1.0,
        cooldown: 1.5
      }
    }
  },
  {
    id: EnemyId.Tb,
    max_health: 35.0,
    armor: 2.0,
    xp: 18.0,
    score: 100,
    float_speed: 30.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 3.0,
    tint: "",
    traits: {}
  },
  {
    id: EnemyId.Tetanus,
    max_health: 30.0,
    armor: 0.0,
    xp: 20.0,
    score: 35,
    float_speed: 35.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Standoff,
    elite: false,
    boss: false,
    body_microns: 2.5,
    tint: "",
    steering: {
      use_generic: false,
      preferred_range: 425.0
    },
    traits: {
      keep_band: {
        min: 350.0,
        max: 500.0,
        back_mult: 1.2
      },
      ranged: {
        interval: 4.0,
        initial_delay: 0.0,
        range: 2000.0,
        damage: 10.0,
        speed: 220.0,
        lifetime: 4.0,
        stun: 0.75,
        spawn_offset: 20.0
      }
    }
  },
  {
    id: EnemyId.HPylori,
    max_health: 32.0,
    armor: 1.0,
    xp: 16.0,
    score: 35,
    float_speed: 58.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Invader,
    elite: false,
    boss: false,
    body_microns: 3.0,
    tint: "",
    traits: {
      latch_lesion: {
        interval: 3.0,
        initial_delay: 1.0,
        radius: 46.0,
        damage: 4.0,
        tick: 0.6,
        slow_factor: 0.6,
        lifetime: 4.0,
        core_color: "#8CBF334D",
        rim_color: "#BFF25C99"
      }
    }
  },
  {
    id: EnemyId.AnthraxSpore,
    max_health: 45.0,
    armor: 3.0,
    xp: 10.0,
    score: 100,
    float_speed: 25.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Drifter,
    elite: false,
    boss: false,
    body_microns: 1.0,
    tint: "",
    traits: {
      transform_hp: {
        frac: 0.5,
        spawn: EnemyId.AnthraxBacillus,
        speed_mult: 1.8,
        damage_mult: 1.5,
        free_self: true
      },
      death_hatch: {
        spawn: EnemyId.AnthraxBacillus
      }
    }
  },
  {
    id: EnemyId.AnthraxBacillus,
    max_health: 35.0,
    armor: 0.0,
    xp: 22.0,
    score: 100,
    float_speed: 70.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 4.0,
    tint: "",
    damage_mult: 1.0,
    traits: {
      telegraph_circle: {
        interval: 4.5,
        initial_delay: 2.0,
        radius: 70.0,
        damage: 22.0,
        telegraph: 1.1
      }
    }
  },
  {
    id: EnemyId.Hiv,
    max_health: 24.0,
    armor: 0.0,
    xp: 14.0,
    score: 35,
    float_speed: 48.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Interceptor,
    elite: false,
    boss: false,
    body_microns: 0.12,
    tint: "",
    traits: {
      contact: {
        kind: "drain",
        amount: 15.0,
        interval: 1.5,
        no_damage: true
      }
    }
  },
  {
    id: EnemyId.Rabies,
    max_health: 22.0,
    armor: 0.0,
    xp: 16.0,
    score: 35,
    float_speed: 65.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.Interceptor,
    elite: false,
    boss: false,
    body_microns: 0.18,
    tint: "",
    traits: {
      zigzag: {
        turn_interval: 0.4,
        bias: 0.6
      },
      contact: {
        kind: "invert",
        duration: 2.0,
        interval: 2.5,
        margin: 12.0
      }
    }
  },
  {
    id: EnemyId.Ebola,
    max_health: 42.0,
    armor: 0.0,
    xp: 24.0,
    score: 35,
    float_speed: 38.0,
    contact_damage: 6.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 0.8,
    tint: "",
    traits: {
      contact: {
        kind: "true_damage",
        amount: 14.0,
        interval: 2.0
      }
    }
  },
  {
    id: EnemyId.VaricellaZoster,
    max_health: 26.0,
    armor: 0.0,
    xp: 18.0,
    score: 15,
    float_speed: 35.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Drifter,
    elite: false,
    boss: false,
    body_microns: 0.2,
    tint: "",
    batch_variant: "varicella_dormant",
    traits: {
      ambush: {
        wake_dist: 150.0,
        wake_player_hp: 0.75,
        wake_on_damage: true,
        speed_to: 85.0,
        threat_to: ThreatMode.ChemoChaser
      }
    }
  },
  {
    id: EnemyId.Candida,
    max_health: 45.0,
    armor: 0.0,
    xp: 24.0,
    score: 35,
    float_speed: 32.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 5.0,
    tint: "",
    batch_variant: "candida_retracted",
    traits: {
      telegraph_line: {
        trigger_range: 200.0,
        length: 150.0,
        line_width: 30.0,
        damage: 18.0,
        telegraph: 0.9,
        root_duration: 1.6,
        trigger_on_damage: true
      }
    }
  },
  {
    id: EnemyId.Aspergillus,
    max_health: 38.0,
    armor: 0.0,
    xp: 22.0,
    score: 15,
    float_speed: 28.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 2.5,
    tint: "",
    traits: {
      death_drop: {
        radius: 100.0,
        grow_from: 20.0,
        grow_time: 1.5,
        lifetime: 6.0,
        dps: 8.0,
        tick: 0.5,
        slow: false
      }
    }
  },
  {
    id: EnemyId.Plasmodium,
    max_health: 30.0,
    armor: 0.0,
    xp: 28.0,
    score: 15,
    float_speed: 25.0,
    contact_damage: 5.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 7.0,
    tint: "",
    traits: {
      split_death: {
        spawn: EnemyId.PlasmodiumMerozoite,
        count: 6,
        count_max: 8,
        speed: 90.0,
        offset_min: 10.0,
        offset_max: 25.0,
        also_on_damage: true
      }
    }
  },
  {
    id: EnemyId.PlasmodiumMerozoite,
    max_health: 10.0,
    armor: 0.0,
    xp: 4.0,
    score: 5,
    float_speed: 85.0,
    contact_damage: 3.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 1.5,
    tint: "",
    traits: {}
  },
  {
    id: EnemyId.Toxoplasma,
    max_health: 40.0,
    armor: 0.0,
    xp: 25.0,
    score: 35,
    float_speed: 32.0,
    contact_damage: 4.0,
    threat_mode: ThreatMode.Drifter,
    elite: false,
    boss: false,
    body_microns: 4.0,
    tint: "",
    traits: {
      aura_pull: {
        interval: 5.5,
        duration: 1.6,
        speed: 180.0
      }
    }
  },
  {
    id: EnemyId.Prion,
    max_health: 150.0,
    armor: 4.0,
    xp: 60.0,
    score: 100,
    float_speed: 30.0,
    contact_damage: 12.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: true,
    body_microns: 0.005,
    tint: "",
    traits: {
      split_hp: {
        frac: 0.5,
        spawn: EnemyId.PrionFragment,
        count: 2,
        pattern: "split_pair"
      }
    }
  },
  {
    id: EnemyId.PrionFragment,
    max_health: 25.0,
    armor: 0.0,
    xp: 18.0,
    score: 5,
    float_speed: 50.0,
    contact_damage: 8.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: false,
    boss: false,
    body_microns: 0.005,
    tint: "",
    traits: {}
  },
  {
    id: EnemyId.StreptococcusChainLord,
    max_health: 420.0,
    armor: 3.0,
    xp: 120.0,
    score: 600,
    float_speed: 54.0,
    contact_damage: 20.0,
    threat_mode: ThreatMode.Interceptor,
    elite: true,
    boss: true,
    body_microns: 26.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      sway: {
        amplitude: 0.85,
        frequency: 3.2
      },
      segmented: {
        count: 10,
        spacing: 26.0,
        damage: 14.0,
        damage_per_segment: 0.05,
        hit_cooldown: 0.45,
        knockback: 180.0,
        hit_range: 24.0,
        hit_radius_factor: 0.35
      }
    }
  },
  {
    id: EnemyId.FluDriftCyclone,
    max_health: 380.0,
    armor: 2.0,
    xp: 130.0,
    score: 600,
    float_speed: 46.0,
    contact_damage: 18.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 30.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      cleanse_pulse: {
        interval: 30.0,
        radius: 900.0,
        knockback: 220.0,
        clear_mark: true
      }
    }
  },
  {
    id: EnemyId.TbGranulomaBehemoth,
    max_health: 650.0,
    armor: 14.0,
    xp: 150.0,
    score: 600,
    float_speed: 24.0,
    contact_damage: 24.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 34.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      death_obstacle: {
        radius: 46.0
      }
    }
  },
  {
    id: EnemyId.VacaSecretor,
    max_health: 400.0,
    armor: 4.0,
    xp: 120.0,
    score: 600,
    float_speed: 34.0,
    contact_damage: 18.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 28.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      timed_zone: {
        interval: 2.8,
        initial_delay: 1.2,
        cap: 6,
        radius: 40.0,
        grow_rate: 17.0,
        grow_max: 125.0,
        lifetime: 9.0,
        dps: 6.0,
        tick: 0.5,
        slow_factor: 0.55,
        slow_duration: 1.0,
        core_color: "#59731459",
        rim_color: "#BFF240A6"
      }
    }
  },
  {
    id: EnemyId.ToxoplasmaMegaCyst,
    max_health: 520.0,
    armor: 5.0,
    xp: 140.0,
    score: 600,
    float_speed: 20.0,
    contact_damage: 22.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 36.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      split_hp: {
        frac: 0.3,
        spawn: EnemyId.Tachyzoite,
        count: 4,
        pattern: "orthogonal",
        offset: 34.0,
        trigger: "poll"
      }
    }
  },
  {
    id: EnemyId.Tachyzoite,
    max_health: 8.0,
    armor: 0.0,
    xp: 3.0,
    score: 5,
    float_speed: 320.0,
    contact_damage: 3.0,
    threat_mode: ThreatMode.Drifter,
    elite: false,
    boss: false,
    body_microns: 4.0,
    tint: "",
    traits: {
      dash_suicide: {
        dash_time: 2.6,
        hit_damage: 9.0,
        hit_margin: 20.0,
        hit_radius_mult: 0.4
      }
    }
  },
  {
    id: EnemyId.MrsaSuperColony,
    max_health: 1600.0,
    armor: 12.0,
    xp: 400.0,
    score: 3000,
    float_speed: 20.0,
    contact_damage: 30.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 46.0,
    tint: "",
    telegraph_scale: 1.45,
    telegraph_damage: 24.0,
    traits: {
      split_death: {
        spawn: EnemyId.MrsaEnragedElite,
        count: 4,
        speed: 120.0,
        ring_offset: 54.0
      }
    }
  },
  {
    id: EnemyId.MrsaEnragedElite,
    max_health: 140.0,
    armor: 4.0,
    xp: 40.0,
    score: 35,
    float_speed: 72.0,
    contact_damage: 16.0,
    threat_mode: ThreatMode.ChemoChaser,
    elite: true,
    boss: false,
    body_microns: 18.0,
    tint: "",
    traits: {}
  },
  {
    id: EnemyId.SyncytialMegaCapsid,
    max_health: 1400.0,
    armor: 6.0,
    xp: 380.0,
    score: 3000,
    float_speed: 18.0,
    contact_damage: 28.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 40.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      slow_aura: {
        radius_base: 420.0,
        radius_max: 760.0,
        factor: 0.78,
        duration: 0.25
      },
      traction_pulse: {
        interval: 6.0,
        initial_delay: 6.0,
        pull: 260.0,
        radius_bonus: 140.0
      }
    }
  },
  {
    id: EnemyId.PlasmodiumMacroSchizont,
    max_health: 1500.0,
    armor: 5.0,
    xp: 380.0,
    score: 3000,
    float_speed: 20.0,
    contact_damage: 28.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 40.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      feed: {
        interval: 5.0,
        initial_delay: 5.0,
        pct: 0.06
      },
      split_death: {
        spawn: EnemyId.PlasmodiumMerozoite,
        count: 10,
        speed: 140.0,
        ring_offset: 44.0,
        jitter: 0.15
      }
    }
  },
  {
    id: EnemyId.HPyloriBiofilmCore,
    max_health: 1500.0,
    armor: 8.0,
    xp: 380.0,
    score: 3000,
    float_speed: 16.0,
    contact_damage: 28.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 42.0,
    tint: "",
    telegraph_scale: 1.35,
    telegraph_damage: 24.0,
    traits: {
      aura_dot: {
        interval: 0.9,
        initial_delay: 0.9,
        radius: 170.0,
        radius_player_mult: 0.5,
        damage: 10.0,
        shove: 180.0
      },
      timed_zone: {
        interval: 6.0,
        initial_delay: 6.0,
        cap: 8,
        radius: 80.0,
        lifetime: 9999.0,
        dps: 7.0,
        tick: 0.5,
        slow_factor: 0.4,
        slow_duration: 1.0,
        core_color: "#61800D66",
        rim_color: "#C7F233B3"
      }
    }
  },
  {
    id: EnemyId.PrpscAmyloidAggregate,
    max_health: 1800.0,
    armor: 25.0,
    xp: 450.0,
    score: 3000,
    float_speed: 14.0,
    contact_damage: 32.0,
    threat_mode: ThreatMode.Drifter,
    elite: true,
    boss: true,
    body_microns: 44.0,
    tint: "",
    telegraph_scale: 1.5,
    telegraph_damage: 24.0,
    traits: {
      shell: {
        pool_pct: 0.5,
        absorb: 0.85,
        break_armor_to: 6.0,
        wave_radius: 280.0
      }
    }
  }
];
