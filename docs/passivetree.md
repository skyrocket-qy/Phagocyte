# 《Project: Phagocyte》造血幹細胞正交天賦星盤規格書 (Hematopoiesis Talent Matrix)

---

## 1. 核心設計概念與五大起點架構

天賦系統將《流亡黯道》（Path of Exile）的單一大星盤架構與微觀生物學結合，打造為頂級的**共軛焦螢光微觀生化星盤（Confocal Fluorescence Microtubule Board）**：

- **五大白血球獨立起點中心 (5 Distinct Cell Starting Hubs)**：
  不同於傳統單一中心向外輻射的設計，**五種免疫細胞各有獨立的專屬起點中心**。
  當玩家出戰巨噬細胞時，直接從「巨噬起點中心」點亮起步；出戰 CTL 時，從「CTL 起點中心」起步。玩家既能在自身譜系內深耕，也能沿著微管網絡延伸至中央互通幹道，跨入其他細胞的起點與特化區域（跨界嵌合分化）。

```mermaid
flowchart TD
    subgraph StartingHubs [五大細胞獨立起點中心 (5 Distinct Starting Hubs)]
        HUB_MAC["【巨噬起點中心】<br>(Macrophage Hub · 左上方)<br>體積 Area / 生命 HP / 護甲 Armor / 格擋 Block"]
        HUB_CTL["【殺手 T 起點中心】<br>(CTL Hub · 右側)<br>移速 Speed / 暴擊 Crit / 穿透 Pierce / 閃避 Evasion"]
        HUB_NEU["【嗜中性球起點中心】<br>(Neutrophil Hub · 左側)<br>傷害 Might / 擊退 Knock / 自癒 Regen"]
        HUB_B["【B 細胞起點中心】<br>(B-Cell Hub · 右下方)<br>彈道數 Amount / 彈速 ProjSpd / CDR"]
        HUB_DC["【樹突狀起點中心】<br>(Dendritic Hub · 正上方)<br>拾取 Magnet / 持續 Duration / 冷卻 CDR"]
    end

    subgraph CentralHighway [中央微管聯通主幹網絡 (Inter-Hub Microtubules)]
        CORE["【微管互通交叉網絡 (Nexus)】<br>跨界嵌合分化通道"]
    end

    HUB_MAC <--> CORE
    HUB_CTL <--> CORE
    HUB_NEU <--> CORE
    HUB_B <--> CORE
    HUB_DC <--> CORE
```

---

## 2. 正交微管網格棋盤拓撲與環層定義 (Orthogonal Grid & Ring Layers)

為杜絕蜘蛛網交錯與斜線重疊的視覺混亂，全星盤嚴格採用 **90 度正交網格（Orthogonal Grid Board）**：

- **單格單節點原則**：每個節點精準落於單一整數網格點 $(col, row)$。
- **世界座標映射公式**：
  $$\text{Position} = \text{WorldCenter} + \begin{pmatrix} col \times \text{GridStep} \\ -row \times \text{GridStep} \end{pmatrix} \quad (\text{GridStep} = 150.0\,\text{px})$$
- **正交相鄰連線原則**：微管光纖僅允許在上下左右相鄰格點間鋪設，**絕不允許斜向連線，絕不交叉重疊**。
- **細胞專屬曼哈頓環層（Cell-Specific Ring Layer $L$）**：
  每個節點相對於各細胞專屬起點 $(col_{\text{start}}, row_{\text{start}})$ 的層級深度由曼哈頓步數決定：
  $$\text{Ring Layer } L_{\text{cell}} = |col - col_{\text{start}}| + |row - row_{\text{start}}|$$
  - **$L = 0$**：該細胞的專屬起點中心（先天永久點亮、消耗 0 點）。
  - **$L = 1 \sim 2$**：該細胞的近端核心代謝環（提供核心生存與專精基礎屬性）。
  - **$L = 3 \sim 4$**：中程特化微管與通往中央互通網絡的聯絡幹道。
  - **$L \ge 5$**：進入中央微管樞紐，或跨界滲透進入其他細胞的特化領域。

---

## 3. 節點稀有度與「零複雜 Scaling」數值設計原則

> [!IMPORTANT]
> **簡約數值原則 (Simple & No Scaling Philosophy)**：
> 目前版本階段，天賦屬性設計嚴格遵守**「純粹、直觀、無複合二次縮放（No Scaling / No Cross-Attribute Conversion）」**原則。
> - 🚫 **嚴禁屬性聯動轉化**（例如：「每 100 點生命增加 5% 傷害」、「將護甲折算為暴擊率」等一律不採用）。
> - ✅ **嚴格採用標準雙軌基礎加成**：
>   - **純固定值 (Flat)**：如 `max_health +15`、`armor +2`、`amount +1`、`pierce +1`。
>   - **純百分比 (Simple Percent)**：如 `might +5%`、`move_speed +5%`、`area +6%`、`cooldown_reduction +4%`、`evasion +3%`、`block +4%`。
> - 最終通用公式純粹透明：$\text{FinalStat} = (\text{Base} + \text{FlatBonus}) \times (1.0 + \text{PercentBonus})$。

### 節點稀有度分級表

| 稀有度等級 | 外觀圖元 | 數值加成結構 (純粹直觀加成) | 生物代謝代價 (Trade-off) | 當前版本狀態 |
| :--- | :--- | :--- | :--- | :--- |
| **普通 (Normal)** | 半透明正圓形小囊泡 | 提供單一基礎通用屬性（如 `might +5%` 或 `max_health +10`）。 | 無代價。 | **正式實裝** |
| **魔法 (Magic)** | 發光菱形 / 橢圓囊泡 | 提供較高額屬性加成，或兩個相輔相成的通用屬性（如 `area +8%` ＋ `armor +2`）。 | 無代價。 | **正式實裝** |
| **稀有 (Rare)** | 六角形生化複合體圖騰 | 提供大幅度單項或雙項純屬性加成（如 `might +15%`、`move_speed +10%`）。 | 無代價或輕微固定數值權衡。 | **正式實裝** |
| **獨特/傳奇 (Unique)** | 八角星芒 / 巨大核仁圖騰 | 顛覆性機制質變節點（Keystone），改變遊戲底層運算邏輯。 | 顯著機制代價。 | **[TODO in the future] 暫不實裝**<br>(避免早期破壞數值平衡，留待未來大型更新) |

---

## 4. 五大細胞專屬起點與區域特化

各細胞起點與周圍微管網絡呈現專屬的微觀螢光染色氛圍：

### 4.1 巨噬細胞起點中心 (Macrophage Starting Hub)
- **色調**：暗紅褐與暖琥珀螢光。
- **位置**：星盤左上方。
- **專精純屬性**：`area`（體積/範圍）、`max_health`（生命上限）、`armor`（膜剛性減傷）、`block`（糖萼格擋率）、`knockback`（撞擊質量）。

### 4.2 殺手 T 細胞起點中心 (CTL Starting Hub)
- **色調**：冰藍色與銳利青綠螢光。
- **位置**：星盤右側。
- **專精純屬性**：`move_speed`（移動速度）、`crit_chance`（暴擊率）、`crit_damage`（暴擊傷害）、`pierce`（彈道穿透數）、`evasion`（流體閃避率）。

### 4.3 嗜中性球起點中心 (Neutrophil Starting Hub)
- **色調**：烈焰橙與酸性亮黃螢光。
- **位置**：星盤左側。
- **專精純屬性**：`might`（傷害強度）、`knockback`（擊退力量）、`health_regen`（生命自癒）、`armor`（膜剛性護甲）。

### 4.4 B 淋巴細胞起點中心 (B-Cell Starting Hub)
- **色調**：深靛藍與電離紫螢光。
- **位置**：星盤右下方。
- **專精純屬性**：`amount`（投射物發射數量）、`projectile_speed`（投射物速度）、`cooldown_reduction`（技能冷卻縮減）。

### 4.5 樹突狀細胞起點中心 (Dendritic Starting Hub)
- **色調**：電離紫與微光金綠螢光。
- **位置**：星盤正上方。
- **專精純屬性**：`magnet`（ATP 拾取半徑）、`duration`（狀態與光環持續時間）、`cooldown_reduction`（技能冷卻縮減）、`area`（感知與效果範圍）。

---

## 5. 點亮機制與跨譜系構築 (Progression & Cross-Lineage Builds)

1. **點數消耗原則**：
   - 每個普通/魔法/稀有節點僅能點亮一次（`MaxStacks = 1`），每次購買固定消耗 **1 點微管天賦點**。
   - 當前選中細胞的起點中心節點為先天生效、永久點亮，消耗 0 點。
2. **天賦點獲取途徑**：
   - **地圖通關獎勵**：通關 5 大器官地圖的 Normal 與 Hard 難度，各提供 1 點（共穩定獲取 10 點）。
   - **成就里程碑解鎖**：達成特定單一條件科研成就發放額外天賦點。
3. **跨界嵌合分化（Cross-Lineage Differentiation）**：
   - 玩家可自本體細胞起點出發，朝向中央微管互通交叉網絡點亮，隨後自由延伸進入其他細胞的特化領域：
     - *例*：嗜中性球從左側起步，穿過中央微管網絡點入右下方 B 細胞區域，打造「高額發射數 ＋ 狂暴高傷」的重砲流；
     - *例*：巨噬細胞自左上方起步，穿過中央點入右側 CTL 區域，打造「高移速 ＋ 巨大體積碾壓」的高速巨噬流。
4. **無痛洗點體驗**：
   - 星盤數據保存於本地 `user://passive_tree.json`，隨時支援 **單鍵免費重置（Reset All）**，鼓勵玩家隨心嘗試不同純屬性流派。
