# Project: Phagocyte（吞噬體）

> **2D 俯視角微觀動作肉鴿遊戲 (Top-Down Microscopic Survivor-like)**
>
> 引擎：Godot 4.x (.NET / C#) ｜ 平台：PC (Steam)

---

## 🔬 遊戲概述 (Overview)

《Project: Phagocyte》是一款以人體微觀免疫系統為舞台的硬核動作肉鴿遊戲。玩家將操控白血球（巨噬細胞、殺手 T 細胞、嗜中性球、B 淋巴細胞、樹突狀細胞），穿梭於人體受損組織與微血管中，利用動態阿米巴偽足吞噬病原體、活化抗原呈遞、調用表觀遺傳超武，對抗細菌、病毒與異變癌細胞的瘋狂入侵。

### 🌟 核心特色 (Core Highlights)
- **所見即所得的有機物理變形**：白血球外形由 FastNoiseLite 頂點位移驅動，多邊形邊界與碰撞箱完全即時同步。
- **硬核生理學機制遊戲化**：將抗原呈遞、調理作用、過載消化、NETosis 轉化為極致肉鴿爽點，「遊玩即理解免疫學」。
- **純通用全域 Stat 數值矩陣**：嚴格依循 Survivor-like 哲學，所有屬性 100% 通用化，絕無單一技能特化屬性。
- **經典「主動 5 ＋ 被動 5」與 1:1 終極超武閉環**：滿級主動與被動二合一融合成就 5 大表觀遺傳超武。
- **正交微管造血幹細胞天賦星盤**：共軛焦螢光顯微風格，90 度正交無重疊微管，五大分化譜系自由跨界構築。

---

## 📚 專案完整設計規格書與知識庫 (Documentation)

專案的所有設計文檔與規格書均已歸檔並整理於 [`docs/`](file:///Users/zelin/project/Phagocyte/docs/README.md) 目錄：

- 📖 **[文檔庫總覽導航 (Documentation Portal)](file:///Users/zelin/project/Phagocyte/docs/README.md)**
- 📋 **[專案總綱 Master GDD / PRD (spec.md)](file:///Users/zelin/project/Phagocyte/docs/spec.md)**
- 💡 **[生物擬真與遊戲化設計哲學 (real.md)](file:///Users/zelin/project/Phagocyte/docs/real.md)**
- 🧬 **[五大白血球形態學與底盤系統 (cell.md)](file:///Users/zelin/project/Phagocyte/docs/cell.md)**
- 📐 **[全域通用 Stat 數值系統規格書 (stat.md)](file:///Users/zelin/project/Phagocyte/docs/stat.md)**
- ⚔️ **[技能系統、超武體系與微操 (skill.md)](file:///Users/zelin/project/Phagocyte/docs/skill.md)**
- 🩹 **[關卡病理環境、流體力學與波次導演 (map.md)](file:///Users/zelin/project/Phagocyte/docs/map.md)**
- 🦠 **[病原體圖鑑與免疫對抗機制 (pathogen.md)](file:///Users/zelin/project/Phagocyte/docs/pathogen.md)**
- 🕸️ **[造血幹細胞正交天賦星盤 (passivetree.md)](file:///Users/zelin/project/Phagocyte/docs/passivetree.md)**
- 🏆 **[成就、里程碑與局外解鎖 (achievement.md)](file:///Users/zelin/project/Phagocyte/docs/achievement.md)**
- 📊 **[臨床病歷單結算、歷史記錄與衝榜 (record.md)](file:///Users/zelin/project/Phagocyte/docs/record.md)**
- 🧭 **[新手引導與直覺 UI/UX 規範 (tutorial.md)](file:///Users/zelin/project/Phagocyte/docs/tutorial.md)**
- ♾️ **[終局無盡細胞因子風暴模式 (endgame.md)](file:///Users/zelin/project/Phagocyte/docs/endgame.md)**
- 🛠️ **[臨床異常反饋、Bug 診斷與數值遙測 (feedback.md)](file:///Users/zelin/project/Phagocyte/docs/feedback.md)**

---

## 🛠️ 開發與建構 (Development)

本專案使用 **Godot 4.x (.NET 版本)** 開發，核心遊戲邏輯採用 **C#** 撰寫：

- 開發環境：Godot Engine 4.x .NET / C# 10.0+ / .NET 8.0 SDK
- 主場景：`scenes/Main.tscn`
