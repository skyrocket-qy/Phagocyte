# 《Project: Phagocyte（吞噬體）》全域設計規格文件庫 (Documentation Portal)

歡迎查閱《Project: Phagocyte》的官方設計規格與技術架構文件庫。本文件庫包含遊戲設計、醫學擬真哲學、系統數值模型、關卡流體力學及程式架構的完整規格文檔。

---

## 1. 知識庫地圖與文檔索引 (Documentation Index)

文件庫按系統模組進行深度解耦與專業拆解，各專題文檔如下：

```
docs/
├── README.md         # [本頁] 文件庫總導覽與架構全景圖
├── spec.md           # 專案總綱 Master GDD / PRD（全域主規格書）
├── real.md           # 生物擬真與遊戲化設計哲學（寓教於樂、機制直覺性與反枯燥心流）
├── cell.md           # 五大白血球形態學與底盤系統（微絲微管底盤、動態噪聲與細胞核物理）
├── skill.md          # 技能系統、通用 Stat 與超武體系（100%通用屬性、主動5+被動5、超武二合一）
├── map.md            # 關卡病理環境、流體力學與波次導演（5大人體器官、吸力/剪切流、難度雙軌制）
├── pathogen.md       # 病原體圖鑑與免疫對抗機制（20+種細菌、病毒、真菌、寄生蟲、朊病毒、癌細胞）
├── passivetree.md    # 造血幹細胞正交天賦星盤（DBD血網轉化、90度正交微管、四階稀有度、五大分化譜系）
├── achievement.md    # 成就、里程碑與局外解鎖系統（角色解鎖鏈、固有技能、Steamworks 整合）
└── record.md         # 臨床病歷單結算、歷史記錄與衝榜系統（SIRS 陣亡 vs 中和通關、評級 S/A/B/C/D）
```

---

## 2. 系統架構互聯關係全景圖 (System Interconnection Architecture)

```mermaid
graph TD
    subgraph CorePillars [核心底層與哲學]
        PHIL["real.md<br>生理擬真與寓教於樂哲學"]
        SPEC["spec.md<br>Master GDD 總綱"]
    end

    subgraph PlayerSystems [玩家實體與戰鬥構築]
        CELL["cell.md<br>五大白血球形態學與底盤"]
        SKILL["skill.md<br>純通用 Stat 屬性池<br>主動 5 ＋ 被動 5 ＋ 超武二合一"]
        TREE["passivetree.md<br>造血幹細胞正交天賦星盤<br>(Confocal Microtubules)"]
    end

    subgraph WorldSystems [戰場環境與威脅]
        MAP["map.md<br>5 大器官組織關卡<br>(流體力學 · 雙軌難度)"]
        PATH["pathogen.md<br>20+ 種真實病原體 AI<br>(抗原逃逸 · 破膜克制)"]
    end

    subgraph MetaSystems [局外循環與持久化]
        ACH["achievement.md<br>成就與局外解鎖系統<br>(細胞 / 地圖 / 技能 / Steam)"]
        REC["record.md<br>臨床病歷單結算與歷史衝榜<br>(Rank S-D 評級)"]
    end

    PHIL --> CELL & SKILL & MAP
    CELL --> SKILL
    TREE -->|注入全域 Stat| SKILL
    SKILL <-->|戰鬥對抗與吞噬| PATH
    MAP -->|流體力學約束| CELL & PATH
    PATH -->|吞噬轉化 EXP| CELL
    PATH & MAP -->|通關 / 陣亡結算| REC
    REC -->|解鎖進度回饋| ACH
    ACH -->|發放天賦點| TREE
    ACH -->|解鎖新細胞 / 地圖 / 技能| CELL & MAP & SKILL
```

---

## 3. 各專題文檔速覽導引

### 🌟 總綱與設計哲學
- **[spec.md](file:///Users/zelin/project/Phagocyte/docs/spec.md)**：專案全景總規格書，涵蓋產品定位、核心循環、研發 Checklist 與技術管線。
- **[real.md](file:///Users/zelin/project/Phagocyte/docs/real.md)**：深層闡述「玩遊戲即理解免疫學」的核心設計哲學，探討如何將真實醫學術語轉化為頂級割草爽感。

### 🧬 細胞與戰鬥構築
- **[cell.md](file:///Users/zelin/project/Phagocyte/docs/cell.md)**：解密「底盤 ＋ 形態參數模組 ＋ 外掛細胞器」解耦架構，詳述巨噬細胞、殺手 T、嗜中性球、B 細胞與樹突狀細胞的顯微鏡特徵與平衡性。
- **[skill.md](file:///Users/zelin/project/Phagocyte/docs/skill.md)**：徹底剖析 100% 通用 Stat 矩陣、「主動 5 ＋ 被動 5」經典槽位、5 大表觀遺傳超武二合一融合機制，以及脫水穿梭避險模式（Squeeze Mode）。
- **[passivetree.md](file:///Users/zelin/project/Phagocyte/docs/passivetree.md)**：融合 DBD 血網與 PoE 星盤靈感的 90 度正交微管棋盤系統，詳解曼哈頓層級、四階節點稀有度（含 Rare 負面代償）與五大分化譜系。

### 🦠 關卡與病原體
- **[map.md](file:///Users/zelin/project/Phagocyte/docs/map.md)**：詳解 5 大微觀人體器官切片（皮下創口、肺泡微腔、肝血竇、胃黏膜、血腦屏障）的專屬流體力學與 15 分鐘波次導演排程。
- **[pathogen.md](file:///Users/zelin/project/Phagocyte/docs/pathogen.md)**：百科全書式的病原體圖鑑，涵蓋細菌、病毒、真菌、寄生蟲、朊病毒與惡性腫瘤等 20 餘種真實微生物的 AI 行為與克制途徑。

### 🏆 局外養成與結算
- **[achievement.md](file:///Users/zelin/project/Phagocyte/docs/achievement.md)**：規範角色與器官地圖解鎖鏈、固有技能解鎖、廣譜生化武器投放，以及與 Steamworks Achievements API 的無縫對接。
- **[record.md](file:///Users/zelin/project/Phagocyte/docs/record.md)**：將每局結算包裝為臨床病理報告單，定義「特異性中和成功」與「SIRS 敗血陣亡」判準，以及 Rank S～D 的臨床衝榜評級模型。
