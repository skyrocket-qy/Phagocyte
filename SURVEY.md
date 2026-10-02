# Universal Stat System Pipeline Survey
# 全域通用 Stat 數值計算與公式接入審查報告

本報告針對《Project: Phagocyte》規格書（[`docs/stat.md`](docs/stat.md)）與全域通用屬性池（[`StatProfiles.Full`](scripts/core/StatBlock.cs)）所定義之 **35 項全域通用屬性**，逐一審查其在底層公式、技能開火快照（[`HitPayload`](scripts/combat/DamageService.cs)）、受擊結算管線（[`HitPipeline`](scripts/combat/HitPipeline.cs)）、主動技能系統（[`BaseSkill`](scripts/skills/BaseSkill.cs)）與角色實體上的實際接入狀況。

---

## 總覽綱要 (Executive Summary)

* **通用屬性總數**：35 項（戰鬥 23 項 + 生存 11 項 + 機制 1 項）
* **已完整接入實戰計算公式**：**32 項** (91.4%)
* **完全未接入計算公式**：**1 項** (`ailment_effect`)
* **存在公式但未接入即時戰鬥管線 / 玩家受擊屬性**：**2 項** (`dot_damage`, `damage_taken`)

```
全域屬性字典池 (35 項)
├── 通用戰鬥屬性 (Combat - 23 項)
│   ├── ✅ 完整接入 (21 項): damage, area, cooldown_reduction, projectile_speed, duration,
│   │                         amount, pierce, crit_chance, crit_damage, armor_penetration,
│   │                         ailment_chance, physical/fire/cold/lightning/chaos_damage,
│   │                         melee/spell/aoe/projectile/minion_damage (Increased 加法池)
│   ├── ⚠️ 僅有獨立 Helper，未接實戰管線 (1 項): dot_damage
│   └── ❌ 完全未接入結算公式 (1 項): ailment_effect
├── 通用生存屬性 (Defense - 11 項)
│   ├── ✅ 完整接入 (10 項): max_health, health_regen, armor, move_speed, evasion,
│   │                         block, life_steal, stagger, recoup, ailment_threshold
│   └── ⚠️ 管線有乘數，但 PlayerActor 漏乘屬性 (1 項): damage_taken
└── 通用機制屬性 (Utility - 1 項)
    └── ✅ 完整接入 (1 項): magnet
```

---

## 一、通用戰鬥屬性 (Combat - 23 項)

| # | 屬性標識 (Key) | 顯示名稱 | 狀態 | 核心實作位置 | 運算公式與架構行為 |
| :- | :--- | :--- | :-: | :--- | :--- |
| 1 | `damage` | 通用傷害 | ✅ 已接入 | [`BaseSkill.cs:296-308`](scripts/skills/BaseSkill.cs#L296-L308) | $\text{dmg} = \text{base} \times \max(0, 1.0 + \text{inc})$，其中 $\text{inc} = (\text{damage}-1) + (\text{type}-1) + \sum (\text{tag}-1)$（PoE 風格 Increased 加法池）。 |
| 2 | `area` | 範圍/體積 | ✅ 已接入 | [`BaseSkill.cs:362`](scripts/skills/BaseSkill.cs#L362)<br>[`PlayerActor.cs:247`](scripts/player/PlayerActor.cs#L247) | 技能判定半徑：$R = R_{\text{base}} \times \text{area}$；<br>角色本體幾何受擊面：$R_{\text{player}} = R_{\text{base}} \times \text{area}$，同步縮放視覺形變幅度。 |
| 3 | `cooldown_reduction` | CDR (冷卻縮減) | ✅ 已接入 | [`BaseSkill.cs:290`](scripts/skills/BaseSkill.cs#L290)<br>[`StatBlock.cs:129`](scripts/core/StatBlock.cs#L129) | $T_{\text{actual}} = T_{\text{base}} \times (1.0 - \text{CDR})$，在 `StatBlock` 內部硬上限鉗制於 $0.75$ (75%)。 |
| 4 | `projectile_speed` | 彈道速度 | ✅ 已接入 | [`BaseSkill.cs:383`](scripts/skills/BaseSkill.cs#L383) | $V = V_{\text{base}} \times \text{projectile\_speed}$，套用於 [`SalvoSkill`](scripts/skills/SalvoSkill.cs) 與各類飛行實體發射初速。 |
| 5 | `duration` | 持續時間 | ✅ 已接入 | [`BaseSkill.cs:390`](scripts/skills/BaseSkill.cs#L390)<br>[`StatBlock.cs:335`](scripts/core/StatBlock.cs#L335) | $D = D_{\text{base}} \times \text{duration}$，影響 [`ZoneSkill`](scripts/skills/ZoneSkill.cs#L54) 地面區域、酸霧與地雷留存引信時間。 |
| 6 | `amount` | 額外發射數量 | ✅ 已接入 | [`BaseSkill.cs:369`](scripts/skills/BaseSkill.cs#L369) | $N = N_{\text{base}} + \lfloor\text{amount}\rfloor$，在發射循環中直接增加彈道投射物束數、地雷拋射個數。 |
| 7 | `pierce` | 穿透次數 | ✅ 已接入 | [`BaseSkill.cs:376`](scripts/skills/BaseSkill.cs#L376) | $P = P_{\text{base}} + \lfloor\text{pierce}\rfloor$，注入彈道實體 Pierce 計數器，穿透目標後不銷毀繼續前行。 |
| 8 | `crit_chance` | 特異性暴擊率 | ✅ 已接入 | [`BaseSkill.cs:397`](scripts/skills/BaseSkill.cs#L397)<br>[`HitPipeline.cs:108`](scripts/combat/HitPipeline.cs#L108) | 施法時凍結進 [`HitPayload.CritChance`](scripts/combat/DamageService.cs#L39)，受擊時以 $\text{randf}() < \text{crit\_chance}$ 判定暴擊。硬上限 $1.0$。 |
| 9 | `crit_damage` | 暴擊傷害倍率 | ✅ 已接入 | [`BaseSkill.cs:405`](scripts/skills/BaseSkill.cs#L405)<br>[`HitPipeline.cs:65`](scripts/combat/HitPipeline.cs#L65) | 凍結至 [`HitPayload.CritMultiplier`](scripts/combat/DamageService.cs#L40)，若暴擊判定成功則 $\text{rawDamage} = \text{damage} \times \text{crit\_damage}$。 |
| 10 | `armor_penetration` | 護甲穿透 | ✅ 已接入 | [`BaseSkill.cs:414`](scripts/skills/BaseSkill.cs#L414)<br>[`CombatInterfaces.cs:28`](scripts/combat/CombatInterfaces.cs#L28) | 凍結至 [`HitPayload.ArmorPenetration`](scripts/combat/DamageService.cs#L41)，以 $\text{Armor}_{\text{eff}} = \text{Armor} \times (1.0 - \text{pen})$ 削減目標護甲。 |
| 11 | `ailment_chance` | 異常觸發率 | ✅ 已接入 | [`BaseSkill.cs:421`](scripts/skills/BaseSkill.cs#L421)<br>[`HitPipeline.cs:88`](scripts/combat/HitPipeline.cs#L88) | 命中後以 $\text{isCrit} \lor (\text{randf}() < \text{ailment\_chance})$ 判定是否觸發掛載之 [`EffectSpec`](scripts/combat/EffectSpec.cs)。 |
| 12 | `dot_damage` | 持續傷害倍率 | ⚠️ 未接實戰 | [`StatBlock.cs:327`](scripts/core/StatBlock.cs#L327) | `CalculateDotDamage(baseDps)` 實作了 $\text{baseDps} \times \text{damage} \times \text{dot\_damage}$，但主動技能烘焙 On-Hit 異常與 [`StatusController`](scripts/combat/StatusController.cs#L255) 運算未調用此屬性。 |
| 13 | `physical_damage` | 物理傷害加成 | ✅ 已接入 | [`BaseSkill.cs:355`](scripts/skills/BaseSkill.cs#L355) | 技能 `damage_type: physical` 時，與 `damage` 加算進 Increased 加法池。 |
| 14 | `fire_damage` | 火焰傷害加成 | ✅ 已接入 | [`BaseSkill.cs:351`](scripts/skills/BaseSkill.cs#L351) | 技能 `damage_type: fire` 時，與 `damage` 加算進 Increased 加法池。 |
| 15 | `cold_damage` | 冰霜傷害加成 | ✅ 已接入 | [`BaseSkill.cs:352`](scripts/skills/BaseSkill.cs#L352) | 技能 `damage_type: cold` 時，與 `damage` 加算進 Increased 加法池。 |
| 16 | `lightning_damage`| 閃電傷害加成 | ✅ 已接入 | [`BaseSkill.cs:353`](scripts/skills/BaseSkill.cs#L353) | 技能 `damage_type: lightning` 時，與 `damage` 加算進 Increased 加法池。 |
| 17 | `chaos_damage` | 混沌傷害加成 | ✅ 已接入 | [`BaseSkill.cs:354`](scripts/skills/BaseSkill.cs#L354) | 技能 `damage_type: chaos` 時，與 `damage` 加算進 Increased 加法池。 |
| 18 | `melee_damage` | 近戰傷害條件加成 | ✅ 已接入 | [`BaseSkill.cs:300`](scripts/skills/BaseSkill.cs#L300) | 若技能攜帶 `Melee` 標籤，加算進 Increased 加法池。 |
| 19 | `spell_damage` | 法術傷害條件加成 | ✅ 已接入 | [`BaseSkill.cs:301`](scripts/skills/BaseSkill.cs#L301) | 若技能攜帶 `Spell` 標籤，加算進 Increased 加法池。 |
| 20 | `aoe_damage` | 範圍傷害條件加成 | ✅ 已接入 | [`BaseSkill.cs:302`](scripts/skills/BaseSkill.cs#L302) | 若技能攜帶 `AOE` 標籤，加算進 Increased 加法池。 |
| 21 | `projectile_damage`| 投射物條件加成 | ✅ 已接入 | [`BaseSkill.cs:303`](scripts/skills/BaseSkill.cs#L303) | 若技能攜帶 `Projectile` 標籤，加算進 Increased 加法池。 |
| 22 | `minion_damage` | 召喚物條件加成 | ✅ 已接入 | [`BaseSkill.cs:304`](scripts/skills/BaseSkill.cs#L304) | 若技能攜帶 `Minion` 標籤，加算進 Increased 加法池（待召喚技能）。 |
| 23 | `ailment_effect`| 異常效果強度乘數 | ❌ 完全未接入 | [`StatBlock.cs:57`](scripts/core/StatBlock.cs#L57) | 僅於屬性池中定義預設值 $1.0$，[`HitPipeline.DispatchEffects`](scripts/combat/HitPipeline.cs#L135-L148) 施加狀態強度與時間時從未讀取攻擊者的 `ailment_effect`。 |

---

## 二、通用生存屬性 (Defense - 11 項)

| # | 屬性標識 (Key) | 顯示名稱 | 狀態 | 核心實作位置 | 運算公式與架構行為 |
| :- | :--- | :--- | :-: | :--- | :--- |
| 1 | `max_health` | 最大生命 | ✅ 已接入 | [`PlayerActor.cs:242`](scripts/player/PlayerActor.cs#L242)<br>[`PlayerActor.cs:578`](scripts/player/PlayerActor.cs#L578) | 初始化生命基準，受擊扣血與自癒上限限制；屬性變更時依百分比等比重算當前血量。 |
| 2 | `health_regen` | 自癒率 | ✅ 已接入 | [`PlayerActor.cs:369-373`](scripts/player/PlayerActor.cs#L369-L373) | 每物理幀由 `HandleRegen` 驅動：$\text{Heal}(\text{health\_regen} \times \Delta t)$（受突變詞條禁用約束）。 |
| 3 | `armor` | 膜剛性/護甲 | ✅ 已接入 | [`HitPipeline.cs:72`](scripts/combat/HitPipeline.cs#L72)<br>[`CombatInterfaces.cs:31`](scripts/combat/CombatInterfaces.cs#L31) | POE 邊際減傷公式：<br>$\text{DR} = \frac{\text{Armor}_{\text{eff}}}{\text{Armor}_{\text{eff}} + 5.0 \times \text{Damage}}$，大傷害穿透深，減傷上限 85%。 |
| 4 | `move_speed` | 移動速度 | ✅ 已接入 | [`PlayerActor.cs:404`](scripts/player/PlayerActor.cs#L404) | 驅動玩家物理移動向量 $V = \text{InputDirection} \times \text{move\_speed}$，包含衝刺速度的基準錨點。 |
| 5 | `evasion` | 流體閃避率 | ✅ 已接入 | [`HitPipeline.cs:35`](scripts/combat/HitPipeline.cs#L35)<br>[`StatBlock.cs:133`](scripts/core/StatBlock.cs#L133) | 受擊第 1 順位防禦判定：$\text{randf}() < \text{evasion}$ 觸發 `EVADED`，完全免疫該次直擊傷害。硬上限 $0.60$ (60%)。 |
| 6 | `block` | 糖萼格擋率 | ✅ 已接入 | [`HitPipeline.cs:48`](scripts/combat/HitPipeline.cs#L48)<br>[`StatBlock.cs:134`](scripts/core/StatBlock.cs#L134) | 受擊第 2 順位防禦判定：$\text{randf}() < \text{block}$ 觸發 `BLOCKED`，偏轉吸收傷害。硬上限 $0.75$ (75%)。 |
| 7 | `life_steal` | 受體汲取/吸血 | ✅ 已接入 | [`HitPipeline.cs:179`](scripts/combat/HitPipeline.cs#L179)<br>[`StatBlock.cs:135`](scripts/core/StatBlock.cs#L135) | 攻擊命中敵人時觸發：$\text{randf}() < \text{life\_steal}$ 觸發立即修復 1 點生命值。硬上限 $0.20$ (20%)。 |
| 8 | `stagger` | 偏轉/延傷 | ✅ 已接入 | [`PlayerActor.cs:568-570`](scripts/player/PlayerActor.cs#L568-L570)<br>[`PlayerActor.cs:380-385`](scripts/player/PlayerActor.cs#L380-L385) | 直擊受傷拆分：$\text{instant} = \text{damage} \times (1.0 - \text{stagger})$，其餘計入 `StaggerPool` 在 4 秒內平攤為無視護甲之 DoT 扣除。硬上限 $0.60$。 |
| 9 | `recoup` | 回收/延補 | ✅ 已接入 | [`PlayerActor.cs:571-572`](scripts/player/PlayerActor.cs#L571-L572)<br>[`PlayerActor.cs:388-394`](scripts/player/PlayerActor.cs#L388-L394) | 直擊受傷回補：$\text{RecoupPool} += \text{damage} \times \text{recoup}$，在 4 秒內分期自動治癒回復 HP。硬上限 $0.30$。 |
| 10 | `ailment_threshold`| 異常閾值 | ✅ 已接入 | [`HitPipeline.cs:150-166`](scripts/combat/HitPipeline.cs#L150-L166) | 異常承受門檻：$\text{Threshold} = \text{MaxHP} \times 0.05 \times \text{ailment\_threshold}$；<br>異常強度縮放因子 $\text{scale} = \text{clamp}(\text{DamageDealt} / \text{Threshold}, 0.0, 1.0)$。 |
| 11 | `damage_taken` | 承受傷害乘數 | ⚠️ 漏乘屬性 | [`HitPipeline.cs:73`](scripts/combat/HitPipeline.cs#L73)<br>[`PlayerActor.cs:172`](scripts/player/PlayerActor.cs#L172) | [`HitPipeline`](scripts/combat/HitPipeline.cs) 會乘以 `def.DamageTakenMultiplier`，但 [`PlayerActor.Defenses`](scripts/player/PlayerActor.cs#L165-L173) 僅賦值突變倍率 `RunMutatorService.IncomingDamageMultiplier`，未乘入 `Stats.GetStat("damage_taken")`。 |

---

## 三、通用機制屬性 (Utility - 1 項)

| # | 屬性標識 (Key) | 顯示名稱 | 狀態 | 核心實作位置 | 運算公式與架構行為 |
| :- | :--- | :--- | :-: | :--- | :--- |
| 1 | `magnet` | 趨化引力 (拾取) | ✅ 已接入 | [`EquipmentDrop.cs:72-80`](scripts/core/EquipmentDrop.cs#L72-L80) | 當掉落物與玩家距離 $\text{dist} \le \text{magnet}$ 時觸發牽引；<br>拉取速度 $V_{\text{pull}} = \text{lerp}(V_{\text{max}}, V_{\text{min}}, \text{dist} / \text{magnet})$ 沿方向向量平滑吸附。 |

---

## 四、缺失與待修復點分析 (Gap Analysis & Action Plan)

### 1. `ailment_effect` 異常效果乘數完全未介入
* **現狀**：當玩家掛載異常時，[`HitPipeline.DispatchEffects`](scripts/combat/HitPipeline.cs#L135-L148) 僅根據受到傷害與目標的 `ailment_threshold` 計算 `scale`，攻擊者身上的 `ailment_effect`（如天賦、裝備加成）完全未影響異常強度。
* **建議補齊方式**：
  在 [`HitPipeline.DispatchEffects`](scripts/combat/HitPipeline.cs#L135) 或 [`HitPayload`](scripts/combat/DamageService.cs#L36) 中傳入施加者的異常效果倍率，將最終施加的非 DoT 效果 magnitude 乘上該倍率：
  ```csharp
  float attackerAilEffect = attacker is PlayerActor pa ? pa.Stats?.GetStat("ailment_effect") ?? 1.0f : 1.0f;
  float mag = e.Magnitude >= 0.0f ? e.Magnitude * scale * attackerAilEffect : e.Magnitude;
  ```

### 2. `dot_damage` 持續傷害倍率未進入技能 On-Hit 烘焙
* **現狀**：[`BaseSkill.BuildOnHitEffects`](scripts/skills/BaseSkill.cs#L245-L269) 解析 `active.json` 中的 `on_hit` 異常（如 ignite、bleed）時，僅依直擊傷害 `dmg * mult` 計算 magnitude，漏掉了 `dot_damage` 乘數；此外 [`StatusController.Tick`](scripts/combat/StatusController.cs#L255) 亦無施加者屬性聯動。
* **建議補齊方式**：
  在 [`BaseSkill.TryParseOnHit`](scripts/skills/BaseSkill.cs#L230-L239) 或主動技能烘焙 EffectSpec 時，若該狀態為 DoT Channel（或在技能參數定義中），乘入 `host.GetStat("dot_damage")`。

### 3. `damage_taken` 承受傷害未綁定玩家防禦結構體
* **現狀**：[`PlayerActor.cs:172`](scripts/player/PlayerActor.cs#L172) 中 `_cachedDefenses.DamageTakenMultiplier` 僅被賦予 `RunMutatorService.IncomingDamageMultiplier`，忽視了天賦星盤或負面效果對玩家 `damage_taken` 屬性的修改。
* **建議補齊方式**：
  ```csharp
  DamageTakenMultiplier = RunMutatorService.IncomingDamageMultiplier * (Stats?.GetStat("damage_taken") ?? 1.0f),
  ```
