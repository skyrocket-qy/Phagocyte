# Universal Stat System Pipeline Survey
# Universal Stat Computation and Formula Integration Audit Report

This report audits each of the **35 universal stats** defined by the Project: Phagocyte spec ([`docs/stat.md`](docs/stat.md)) and the universal stat pool ([`StatProfiles.Full`](scripts/core/StatBlock.cs)), verifying their actual integration across core formulas, skill fire snapshots ([`HitPayload`](scripts/combat/DamageService.cs)), the on-hit resolution pipeline ([`HitPipeline`](scripts/combat/HitPipeline.cs)), the active skill system ([`BaseSkill`](scripts/skills/BaseSkill.cs)), and character entities.

---

## Executive Summary (Executive Summary)

* **Total universal stats**: 35 (23 Combat + 11 Survival + 1 Utility)
* **Fully integrated into live combat formulas**: **35** (100%; `ailment_effect`/`dot_damage`/`damage_taken` were completed in this pass, see the Section 4 fix log)
* **Not integrated into any formula**: **0**

```
Universal stat dictionary pool (35 stats)
├── Universal Combat stats (Combat - 23 stats)
│   ├── ✅ Fully integrated (23 stats): damage, area, cooldown_reduction, projectile_speed, duration,
│   │                         amount, pierce, crit_chance, crit_damage, armor_penetration,
│   │                         ailment_chance, ailment_effect, dot_damage, physical/fire/cold/lightning/chaos_damage,
│   │                         melee/spell/aoe/projectile/minion_damage (Increased additive pool)
├── Universal Survival stats (Defense - 11 stats)
│   ├── ✅ Fully integrated (11 stats): max_health, health_regen, armor, damage_taken, move_speed, evasion,
│   │                         block, life_steal, stagger, recoup, ailment_threshold
└── Universal Utility stats (Utility - 1 stat)
    └── ✅ Fully integrated (1 stat): magnet
```

---

## I. Universal Combat Stats (Combat - 23 stats)

| # | Stat Key | Display Name | Status | Core Implementation | Formula & Architectural Behavior |
| :- | :--- | :--- | :-: | :--- | :--- |
| 1 | `damage` | Universal Damage | ✅ Integrated | [`BaseSkill.cs:296-308`](scripts/skills/BaseSkill.cs#L296-L308) | $\text{dmg} = \text{base} \times \max(0, 1.0 + \text{inc})$, where $\text{inc} = (\text{damage}-1) + (\text{type}-1) + \sum (\text{tag}-1)$ (PoE-style Increased additive pool). |
| 2 | `area` | Area/Volume | ✅ Integrated | [`BaseSkill.cs:362`](scripts/skills/BaseSkill.cs#L362)<br>[`PlayerActor.cs:247`](scripts/player/PlayerActor.cs#L247) | Skill hit radius: $R = R_{\text{base}} \times \text{area}$;<br>Character body geometric hit surface: $R_{\text{player}} = R_{\text{base}} \times \text{area}$, scaling the visual deformation amplitude in sync. |
| 3 | `cooldown_reduction` | CDR (Cooldown Reduction) | ✅ Integrated | [`BaseSkill.cs:290`](scripts/skills/BaseSkill.cs#L290)<br>[`StatBlock.cs:129`](scripts/core/StatBlock.cs#L129) | $T_{\text{actual}} = T_{\text{base}} \times (1.0 - \text{CDR})$, hard-capped at $0.75$ (75%) inside `StatBlock`. |
| 4 | `projectile_speed` | Projectile Speed | ✅ Integrated | [`BaseSkill.cs:383`](scripts/skills/BaseSkill.cs#L383) | $V = V_{\text{base}} \times \text{projectile\_speed}$, applied to [`SalvoSkill`](scripts/skills/SalvoSkill.cs) and the launch velocity of all flying entities. |
| 5 | `duration` | Duration | ✅ Integrated | [`BaseSkill.cs:390`](scripts/skills/BaseSkill.cs#L390)<br>[`StatBlock.cs:335`](scripts/core/StatBlock.cs#L335) | $D = D_{\text{base}} \times \text{duration}$, affecting ground zones, acid mist, and mine persistence/fuse time for [`ZoneSkill`](scripts/skills/ZoneSkill.cs#L54). |
| 6 | `amount` | Bonus Projectile Count | ✅ Integrated | [`BaseSkill.cs:369`](scripts/skills/BaseSkill.cs#L369) | $N = N_{\text{base}} + \lfloor\text{amount}\rfloor$, directly adding projectile beams and lobbed mines in the fire loop. |
| 7 | `pierce` | Pierce Count | ✅ Integrated | [`BaseSkill.cs:376`](scripts/skills/BaseSkill.cs#L376) | $P = P_{\text{base}} + \lfloor\text{pierce}\rfloor$, injected into the projectile entity's Pierce counter so it keeps travelling instead of despawning after penetrating a target. |
| 8 | `crit_chance` | Specificity Crit Chance | ✅ Integrated | [`BaseSkill.cs:397`](scripts/skills/BaseSkill.cs#L397)<br>[`HitPipeline.cs:108`](scripts/combat/HitPipeline.cs#L108) | Frozen into [`HitPayload.CritChance`](scripts/combat/DamageService.cs#L39) at cast time; crit is rolled on hit with $\text{randf}() < \text{crit\_chance}$. Hard cap $1.0$. |
| 9 | `crit_damage` | Crit Damage Multiplier | ✅ Integrated | [`BaseSkill.cs:405`](scripts/skills/BaseSkill.cs#L405)<br>[`HitPipeline.cs:65`](scripts/combat/HitPipeline.cs#L65) | Frozen into [`HitPayload.CritMultiplier`](scripts/combat/DamageService.cs#L40); on a successful crit roll, $\text{rawDamage} = \text{damage} \times \text{crit\_damage}$. |
| 10 | `armor_penetration` | Armor Penetration | ✅ Integrated | [`BaseSkill.cs:414`](scripts/skills/BaseSkill.cs#L414)<br>[`CombatInterfaces.cs:28`](scripts/combat/CombatInterfaces.cs#L28) | Frozen into [`HitPayload.ArmorPenetration`](scripts/combat/DamageService.cs#L41), reducing target armor via $\text{Armor}_{\text{eff}} = \text{Armor} \times (1.0 - \text{pen})$. |
| 11 | `ailment_chance` | Ailment Chance | ✅ Integrated | [`BaseSkill.cs:421`](scripts/skills/BaseSkill.cs#L421)<br>[`HitPipeline.cs:88`](scripts/combat/HitPipeline.cs#L88) | After a hit, $\text{isCrit} \lor (\text{randf}() < \text{ailment\_chance})$ decides whether the attached [`EffectSpec`](scripts/combat/EffectSpec.cs) triggers. |
| 12 | `dot_damage` | Damage-over-Time Multiplier | ✅ Integrated (hit-time) | [`HitPipeline.cs`](scripts/combat/HitPipeline.cs) `DispatchEffects`/`DotDamageOf` | Reads the applier live on hit: $\text{dps} = \text{mag} \times \text{scale} \times \text{dot\_damage}$ (deliberately not baked at cast time, see the Section IV.2 fix log). |
| 13 | `physical_damage` | Physical Damage Bonus | ✅ Integrated | [`BaseSkill.cs:355`](scripts/skills/BaseSkill.cs#L355) | When the skill's `damage_type: physical`, adds together with `damage` into the Increased additive pool. |
| 14 | `fire_damage` | Fire Damage Bonus | ✅ Integrated | [`BaseSkill.cs:351`](scripts/skills/BaseSkill.cs#L351) | When the skill's `damage_type: fire`, adds together with `damage` into the Increased additive pool. |
| 15 | `cold_damage` | Cold Damage Bonus | ✅ Integrated | [`BaseSkill.cs:352`](scripts/skills/BaseSkill.cs#L352) | When the skill's `damage_type: cold`, adds together with `damage` into the Increased additive pool. |
| 16 | `lightning_damage`| Lightning Damage Bonus | ✅ Integrated | [`BaseSkill.cs:353`](scripts/skills/BaseSkill.cs#L353) | When the skill's `damage_type: lightning`, adds together with `damage` into the Increased additive pool. |
| 17 | `chaos_damage` | Chaos Damage Bonus | ✅ Integrated | [`BaseSkill.cs:354`](scripts/skills/BaseSkill.cs#L354) | When the skill's `damage_type: chaos`, adds together with `damage` into the Increased additive pool. |
| 18 | `melee_damage` | Melee Conditional Damage Bonus | ✅ Integrated | [`BaseSkill.cs:300`](scripts/skills/BaseSkill.cs#L300) | When the skill carries the `Melee` tag, adds into the Increased additive pool. |
| 19 | `spell_damage` | Spell Conditional Damage Bonus | ✅ Integrated | [`BaseSkill.cs:301`](scripts/skills/BaseSkill.cs#L301) | When the skill carries the `Spell` tag, adds into the Increased additive pool. |
| 20 | `aoe_damage` | Area Conditional Damage Bonus | ✅ Integrated | [`BaseSkill.cs:302`](scripts/skills/BaseSkill.cs#L302) | When the skill carries the `AOE` tag, adds into the Increased additive pool. |
| 21 | `projectile_damage`| Projectile Conditional Bonus | ✅ Integrated | [`BaseSkill.cs:303`](scripts/skills/BaseSkill.cs#L303) | When the skill carries the `Projectile` tag, adds into the Increased additive pool. |
| 22 | `minion_damage` | Minion Conditional Bonus | ✅ Integrated | [`BaseSkill.cs:304`](scripts/skills/BaseSkill.cs#L304) | When the skill carries the `Minion` tag, adds into the Increased additive pool (pending summon skills). |
| 23 | `ailment_effect`| Ailment Effect Multiplier | ✅ Integrated (hit-time) | [`HitPipeline.cs`](scripts/combat/HitPipeline.cs) channel dispatch + [`StatusController.MainChannelOf`](scripts/combat/StatusController.cs) | Slow/amp land fixed `0.3 × ailment_effect` on damage-derived odds; DoT/stun unaffected; non-player appliers count as `1.0`. |

---

## II. Universal Survival Stats (Defense - 11 stats)

| # | Stat Key | Display Name | Status | Core Implementation | Formula & Architectural Behavior |
| :- | :--- | :--- | :-: | :--- | :--- |
| 1 | `max_health` | Max Health | ✅ Integrated | [`PlayerActor.cs:242`](scripts/player/PlayerActor.cs#L242)<br>[`PlayerActor.cs:578`](scripts/player/PlayerActor.cs#L578) | Establishes the baseline HP; bounds on-hit HP loss and self-heal caps; current HP is rescaled proportionally when the stat changes. |
| 2 | `health_regen` | Self-Heal Rate | ✅ Integrated | [`PlayerActor.cs:369-373`](scripts/player/PlayerActor.cs#L369-L373) | Driven by `HandleRegen` every physics frame: $\text{Heal}(\text{health\_regen} \times \Delta t)$ (subject to mutator-based disable constraints). |
| 3 | `armor` | Membrane Rigidity/Armor | ✅ Integrated | [`HitPipeline.cs:72`](scripts/combat/HitPipeline.cs#L72)<br>[`CombatInterfaces.cs:31`](scripts/combat/CombatInterfaces.cs#L31) | POE diminishing-returns formula:<br>$\text{DR} = \frac{\text{Armor}_{\text{eff}}}{\text{Armor}_{\text{eff}} + 5.0 \times \text{Damage}}$, large hits penetrate deeper; damage-reduction cap 85%. |
| 4 | `move_speed` | Move Speed | ✅ Integrated | [`PlayerActor.cs:404`](scripts/player/PlayerActor.cs#L404) | Drives the player physics movement vector $V = \text{InputDirection} \times \text{move\_speed}$, including the baseline anchor for dash speed. |
| 5 | `evasion` | Fluid Evasion Chance | ✅ Integrated | [`HitPipeline.cs:35`](scripts/combat/HitPipeline.cs#L35)<br>[`StatBlock.cs:133`](scripts/core/StatBlock.cs#L133) | First-priority defense roll on hit: $\text{randf}() < \text{evasion}$ triggers `EVADED`, fully negating that direct hit. Hard cap $0.60$ (60%). |
| 6 | `block` | Glycocalyx Block Chance | ✅ Integrated | [`HitPipeline.cs:48`](scripts/combat/HitPipeline.cs#L48)<br>[`StatBlock.cs:134`](scripts/core/StatBlock.cs#L134) | Second-priority defense roll on hit: $\text{randf}() < \text{block}$ triggers `BLOCKED`, deflecting and absorbing the damage. Hard cap $0.75$ (75%). |
| 7 | `life_steal` | Receptor Leech/Life Steal | ✅ Integrated | [`HitPipeline.cs:179`](scripts/combat/HitPipeline.cs#L179)<br>[`StatBlock.cs:135`](scripts/core/StatBlock.cs#L135) | Triggers when an attack hits an enemy: $\text{randf}() < \text{life\_steal}$ triggers an instant 1-HP restore. Hard cap $0.20$ (20%). |
| 8 | `stagger` | Deflect/Delayed Damage | ✅ Integrated | [`PlayerActor.cs:568-570`](scripts/player/PlayerActor.cs#L568-L570)<br>[`PlayerActor.cs:380-385`](scripts/player/PlayerActor.cs#L380-L385) | Direct-hit damage split: $\text{instant} = \text{damage} \times (1.0 - \text{stagger})$, with the remainder booked into `StaggerPool` and spread over 4 seconds as armor-ignoring DoT deductions. Hard cap $0.60$. |
| 9 | `recoup` | Recoup/Delayed Healing | ✅ Integrated | [`PlayerActor.cs:571-572`](scripts/player/PlayerActor.cs#L571-L572)<br>[`PlayerActor.cs:388-394`](scripts/player/PlayerActor.cs#L388-L394) | Direct-hit recovery: $\text{RecoupPool} += \text{damage} \times \text{recoup}$, automatically healing back HP in installments over 4 seconds. Hard cap $0.30$. |
| 10 | `ailment_threshold`| Ailment Threshold | ✅ Integrated | [`HitPipeline.cs`](scripts/combat/HitPipeline.cs) `ControlThresholdOf` | Control-threshold multiplier: slow/amp $\text{Threshold} = \text{MaxHP} \times 0.10 \times \text{mult}$, stun $\text{Threshold} = \text{MaxHP} \times 0.20 \times \text{mult}$;<br>control odds $\text{chance} = \min(\text{dealt} / \text{Threshold} \times (1 + \text{ailment\_chance}) \times (\text{crit} ? 2 : 1), 1.0)$; landed slow/amp fix at `0.3`, stun at `0.5s`. |
| 11 | `damage_taken` | Damage Taken Multiplier | ✅ Integrated | [`PlayerActor.cs`](scripts/player/PlayerActor.cs) `InvalidateDefenses`/`TakeDoTDamage` | Player defense = mutator multiplier x `damage_taken`; direct hits and DoT entries each multiply once on intake, while stagger-pool drains take the original path without double-counting; `0` counts as unset and is skipped (consistent with the pipeline guard). |

---

## III. Universal Utility Stats (Utility - 1 stat)

| # | Stat Key | Display Name | Status | Core Implementation | Formula & Architectural Behavior |
| :- | :--- | :--- | :-: | :--- | :--- |
| 1 | `magnet` | Chemotactic Pull (Pickup) | ✅ Integrated | [`EquipmentDrop.cs:72-80`](scripts/core/EquipmentDrop.cs#L72-L80) | Pull triggers when a drop is within $\text{dist} \le \text{magnet}$ of the player;<br>pull speed $V_{\text{pull}} = \text{lerp}(V_{\text{max}}, V_{\text{min}}, \text{dist} / \text{magnet})$ smoothly attracts it along the direction vector. |

---

## IV. Gap Analysis & Action Plan (Gap Analysis & Action Plan) — Sections 1-3 Fixed, Records Kept for Reference

### 1. `ailment_effect` Ailment-Effect Multiplier Never Applied -> ✅ Fixed (hit-time)
* **Original state**: when the player applied ailments, `HitPipeline.DispatchEffects` computed `scale` only from damage dealt and the target's `ailment_threshold`; the attacker's `ailment_effect` (e.g. talent and equipment bonuses) never affected ailment intensity.
* **Actual fix (control rework)**: slow/amp land fixed `0.3 × ailment_effect` on a damage-derived roll (`AilmentEffectOf`, non-players = `1.0`); stun lands fixed `0.5s`; DoT anchors to dealt damage instead of scaling.
* **Suggested completion**:
  Pass the applier's ailment-effect multiplier in [`HitPipeline.DispatchEffects`](scripts/combat/HitPipeline.cs#L135) or [`HitPayload`](scripts/combat/DamageService.cs#L36) so the final applied non-DoT effect magnitude is multiplied by it:
  ```csharp
  float attackerAilEffect = attacker is PlayerActor pa ? pa.Stats?.GetStat("ailment_effect") ?? 1.0f : 1.0f;
  float mag = e.Magnitude >= 0.0f ? e.Magnitude * scale * attackerAilEffect : e.Magnitude;
  ```

### 2. `dot_damage` DoT Multiplier Missing from Skill On-Hit Baking -> ✅ Fixed (moved to hit-time, not bake-time)
* **Original state**: when [`BaseSkill.BuildOnHitEffects`](scripts/skills/BaseSkill.cs#L245-L269) parsed `on_hit` ailments (e.g. ignite, bleed) from `active.json`, it computed magnitude only from direct-hit damage `dmg * mult`, dropping the `dot_damage` multiplier; [`StatusController.Tick`](scripts/combat/StatusController.cs#L255) likewise had no applier-stat linkage.
* **Actual fix (control rework)**: DoT dps anchors to dealt damage — `mag = dealt × (baked / RawDamage) × dot_damage × (1 − FromArmorDot)`, duration fixed from payload; `dot_damage` still read live on hit (`DotDamageOf`).
* **Suggested completion**:
  In [`BaseSkill.TryParseOnHit`](scripts/skills/BaseSkill.cs#L230-L239) or when baking EffectSpecs for active skills, multiply in `host.GetStat("dot_damage")` if the status is a DoT kind (or in the skill parameter definitions).

### 3. `damage_taken` Damage-Taken Multiplier Not Bound to Player Defense Struct -> ✅ Fixed (including DoT intake)
* **Original state**: [`PlayerActor.cs:172`](scripts/player/PlayerActor.cs#L172) assigned `_cachedDefenses.DamageTakenMultiplier` only from `RunMutatorService.IncomingDamageMultiplier`, ignoring talent-tree or debuff modifications to the player's `damage_taken` stat.
* **Actual fix**: `InvalidateDefenses` now uses mutator multiplier x `damage_taken`; `TakeDoTDamage` multiplies likewise on intake (scripted direct damage and stagger-pool drains bypass this path); `0` counts as unset and is skipped, consistent with the pipeline guard.
* **Suggested completion**:
  ```csharp
  DamageTakenMultiplier = RunMutatorService.IncomingDamageMultiplier * (Stats?.GetStat("damage_taken") ?? 1.0f),
  ```
