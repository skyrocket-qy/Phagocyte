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
        HUB_CTL["【殺手 T 起點中心】<br>(CTL Hub · 右上方)<br>移速 Speed / 暴擊 Crit / 穿透 Pierce / 閃避 Evasion"]
        HUB_NEU["【嗜中性球起點中心】<br>(Neutrophil Hub · 左下方)<br>傷害 Might / 擊退 Knock / 自癒 Regen"]
        HUB_B["【B 細胞起點中心】<br>(B-Cell Hub · 右下方)<br>彈道數 Amount / 彈速 ProjSpd / CDR / 汲取 LifeSteal"]
        HUB_DC["【樹突狀起點中心】<br>(Dendritic Hub · 左中段)<br>拾取 Magnet / 持續 Duration / 冷卻 CDR"]
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

### 1.1 六區域方格排列 (2×3 Region Matrix)

每個區域都是 **3×3 = 9 個節點的完整方格（Region Square）**，細胞起點位於方格正中心；
五個細胞方格加上「造血核心區」共六個方格，以 **2 欄 × 3 列** 緊密相連（edge-to-edge、無間隙），
拼成一整塊 **6×9 的節點棋盤（共 54 個節點）**：

```
[巨噬 vitality ][CTL precision  ]
[樹突 senses   ][核心 core      ]
[嗜中 motility ][B 細胞 ballistics]
```

- 每一格節點都與上下左右相鄰節點以單位正交微管相連（含跨區邊界），全盤共 93 條連線。
- **造血核心區（Hematopoietic Core）** 位於棋盤中段右欄，與樹突、CTL、B 細胞三區相鄰。
- 每個區域方格邊長 450px（3 格）；起點先天點亮，玩家由起點向四方逐步點亮，可自由跨越區界。

### 1.2 節點－性狀－數值三層架構 (Node → Trait → Stat)

資料嚴格分為三層，與微管星盤的「天賦點／性狀／屬性」對應：

- **節點（Node）**：只擁有位置與所屬區域 `{id, branch, col, row}`，外加一個 `trait` 參照。
- **性狀（Trait）**：可重用的天賦定義 `{id, name_key, desc_key, icon, rarity, modifiers[]}`。
  名稱、圖示、稀有度與屬性**全部由性狀擁有**——因此**相同性狀必定有相同圖示**，不可能出現同名不同圖。
- **屬性（Stat）**：性狀內的 `modifiers` 直接對應 `stat_labels.json` 的通用屬性。

目前 54 個節點各自對應 54 個性狀（一節點一性狀）；未來新增重複的小節點時，只需共用既有性狀 id，即可自動繼承同樣的名稱、圖示與數值。
例外是五個細胞起點：它們的性狀**不帶任何屬性**（`modifiers` 為空），僅作為出發錨點，因此五個起點外觀完全一致（青色正方形、新稀有度「起始」）。

---

## 2. 正交微管網格棋盤拓撲與環層定義 (Orthogonal Grid & Ring Layers)

為杜絕蜘蛛網交錯與斜線重疊的視覺混亂，全星盤嚴格採用 **90 度正交網格（Orthogonal Grid Board）**：

- **單格單節點原則**：每個節點精準落於單一整數網格點 $(col, row)$。
- **世界座標映射公式**：
  $$\text{Position} = \text{WorldCenter} + \begin{pmatrix} col \times \text{GridStep} \\ -row \times \text{GridStep} \end{pmatrix} \quad (\text{GridStep} = 150.0\,\text{px})$$
- **正交相鄰連線原則**：微管光纖僅允許在上下左右相鄰格點間鋪設，**絕不允許斜向連線，絕不交叉重疊**。
- **3×3 區域方格（Region Squares）**：五個細胞區域與造血核心各佔一個完整的 3×3 節點方格（9 節點），以 2 欄×3 列拼成一塊 6×9 棋盤；細胞起點固定在所屬方格正中心。所有上下左右相鄰的節點（含跨區邊界）皆以單位正交微管相連，因此可自由跨區點亮。
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
>   - **純百分比 (Simple Percent)**：如 `might +5%`、`move_speed +5%`、`area +6%`、`cooldown_reduction +4%`、`evasion +3%`、`block +4%`、`life_steal +1%`。
> - 最終通用公式純粹透明：$\text{FinalStat} = (\text{Base} + \text{FlatBonus}) \times (1.0 + \text{PercentBonus})$。

### 節點稀有度分級表

全盤 54 節點固定配額：**傳奇 2 ／ 稀有 4 ／ 魔法 10 ／ 普通 33 ／ 起始 5**；
稀有度採 **分散配置（scatter）**：每個區域至少一個非普通節點、單區最多 3 個特殊節點，
2 個傳奇分居不同區域，稀有橫跨 3 個區域；
五個細胞起點統一為新稀有度「起始」——青色正方形、先天點亮、不帶任何屬性。

| 稀有度等級 | 外觀圖元 | 數值加成結構 (純粹直觀加成) | 生物代謝代價 (Trade-off) | 當前版本狀態 |
| :--- | :--- | :--- | :--- | :--- |
| **普通 (Normal)** | 白色正圓形小囊泡 | 提供單一基礎通用屬性（如 `might +4%` 或 `health_regen +0.3`）。 | 無代價。 | **正式實裝（33 個）** |
| **魔法 (Magic)** | 藍色正圓形囊泡（較大） | 提供單一加強或兩個相輔相成的通用屬性（如 `duration +14%`、`magnet +40%`）。 | 無代價。 | **正式實裝（10 個）** |
| **稀有 (Rare)** | 金色菱形生化複合體圖騰 | 提供兩項大幅度純屬性加成（如 `armor +3` ＋ `block +4%`、`crit_damage +15%` ＋ `crit_chance +3%`）。 | 無代價。 | **正式實裝（4 個）** |
| **獨特/傳奇 (Unique)** | 橙色六邊形圖騰 | 頂級多重屬性圖騰（如 `max_health +15%` ＋ `armor +2`、`area +30%` 等多達五項）。 | 顯著固定代價（如 `max_health -25%`）。 | **正式實裝（2 個）** |
| **起始 (Start)** | 青色正方形中樞 | **不帶任何屬性**——僅作為各細胞的出發點；五個起點外觀完全一致。 | 先天點亮（0 點）。 | **正式實裝（5 個）** |

---

## 4. 五大細胞專屬起點與區域特化

各細胞起點與周圍微管網絡呈現專屬的微觀螢光染色氛圍：

### 4.1 巨噬細胞起點中心 (Macrophage Starting Hub)
- **色調**：暗紅褐與暖琥珀螢光。
- **位置**：星盤左上方。
- **專精純屬性**：`area`（體積/範圍）、`max_health`（生命上限）、`armor`（膜剛性減傷）、`block`（糖萼格擋率）、`knockback`（撞擊質量）。

### 4.2 殺手 T 細胞起點中心 (CTL Starting Hub)
- **色調**：冰藍色與銳利青綠螢光。
- **位置**：星盤右上方。
- **專精純屬性**：`move_speed`（移動速度）、`crit_chance`（暴擊率）、`crit_damage`（暴擊傷害）、`pierce`（彈道穿透數）、`evasion`（流體閃避率）。

### 4.3 嗜中性球起點中心 (Neutrophil Starting Hub)
- **色調**：烈焰橙與酸性亮黃螢光。
- **位置**：星盤左下方。
- **專精純屬性**：`might`（傷害強度）、`knockback`（擊退力量）、`health_regen`（生命自癒）、`armor`（膜剛性護甲）。

### 4.4 B 淋巴細胞起點中心 (B-Cell Starting Hub)
- **色調**：深靛藍與電離紫螢光。
- **位置**：星盤右下方。
- **專精純屬性**：`amount`（投射物發射數量）、`projectile_speed`（投射物速度）、`cooldown_reduction`（技能冷卻縮減）、`life_steal`（受體汲取/命中吸血）。

### 4.5 樹突狀細胞起點中心 (Dendritic Starting Hub)
- **色調**：電離紫與微光金綠螢光。
- **位置**：星盤左中段。
- **專精純屬性**：`magnet`（ATP 拾取半徑）、`duration`（狀態與光環持續時間）、`cooldown_reduction`（技能冷卻縮減）、`area`（感知與效果範圍）。

---

## 5. 點亮機制與跨譜系構築 (Progression & Cross-Lineage Builds)

1. **點數消耗原則**：
   - 每個普通／魔法／稀有／傳奇節點僅能點亮一次（`MaxStacks = 1`），每次購買固定消耗 **1 點微管天賦點**。
   - 當前選中細胞的起點中心節點為先天生效、永久點亮，消耗 0 點；五個起點不帶任何屬性，且外觀統一為青色正方形（稀有度「起始」）。
2. **天賦點獲取途徑**：
   - **地圖通關獎勵**：通關 5 大器官地圖的 Normal 與 Hard 難度，各提供 1 點（共穩定獲取 10 點）。
   - **成就里程碑解鎖**：達成特定單一條件科研成就發放額外天賦點。
3. **跨界嵌合分化（Cross-Lineage Differentiation）**：
   - 玩家可自本體細胞起點出發，朝向中央微管互通交叉網絡點亮，隨後自由延伸進入其他細胞的特化領域：
     - *例*：嗜中性球從左側起步，穿過中央微管網絡點入右下方 B 細胞區域，打造「高額發射數 ＋ 狂暴高傷」的重砲流；
     - *例*：巨噬細胞自左上方起步，穿過中央點入右側 CTL 區域，打造「高移速 ＋ 巨大體積碾壓」的高速巨噬流。
4. **無痛洗點體驗**：
   - 星盤數據保存於本地 `user://passive_tree.json`，隨時支援 **單鍵免費重置（Reset All）**，鼓勵玩家隨心嘗試不同純屬性流派。
5. **多設定檔構築（Build Profiles，最多 3 檔）**：
   - 每個細胞擁有 1～3 個天賦設定檔（動態新增／刪除，`＋`／`刪除此配置`）；新檔為空並自動成為當前檔，刪除當前檔時自動退回前一檔，最後一檔不可刪除。
   - 細胞等級與成就天賦點由該細胞三檔共用；切換設定檔只改變已花費點數，不需洗點。
   - 出戰採用按下「下一步」時選中的設定檔；舊版單檔存檔會自動遷移為設定檔一。
   - 暫停選單的天賦總覽會標示當前設定檔名稱（唯讀）。
