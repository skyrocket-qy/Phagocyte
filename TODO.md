# 《Project: Phagocyte》後續開發待辦清單 (TODO)

本清單記錄評估自 `/Users/zelin/vistrace` 且確認納入後續迭代的核心系統與玩法延伸。

---

## 優先排程待辦項目 (From Vistrace Evaluation)

### 1. 動態流體原生質 / 生命能量球 (`HeroGlobe.cs` + `globe_liquid.gdshader`)
* **系統分類**：視覺表現 / 核心 HUD
* **核心功能**：
  * 引入正弦波動頻率、表面張力波紋、球體玻璃邊緣發光 (Rim Glow)、頂部高光與流體平滑插值 (`Lerp`)。
  * 用於 HUD 左下角的 **「巨噬細胞原生質體積 (Cytoplasm Volume / HP)」** 與 **「ATP 粒線體能量儲量球」**。
  * 替換傳統扁平的線性進度條，隨著生命值與能量的增減呈現生動的微觀膠質流體動態。
* **參考檔案**：
  * `vistrace/assets/shaders/globe_liquid.gdshader`
  * `vistrace/src/ui/hud/HeroGlobe.cs`

---

### 2. 微環境病理詞綴系統 (Tissue Microenvironment Affixes)
* **系統分類**：關卡機制 / Roguelike 重玩性
* **核心功能**：
  * 為 5 大感染組織（急性創口、肺泡腔、腸道腔、微血管管腔、淋巴結）引入隨機或自選的「微環境病理詞綴」：
    * **低氧酸中毒 (Hypoxic Acidosis)**：病原體與玩家異常傷害 +40%，但移動速度 -15%。
    * **急性發炎風暴 (Cytokine Storm)**：病原體生成速率 +50%，ATP 與生化點數掉落翻倍。
    * **纖維蛋白沉積 (Fibrin Deposition)**：地表偶爾生成膠原纖維網阻礙移動，但玩家格擋率 +15%。
  * 支援詞綴權重、危險等級（Hazard Rating）與掉落回報加成。
* **參考檔案**：
  * `vistrace/src/domain/maps/MapModifierRegistry.cs`
  * `vistrace/src/domain/maps/SurvivorMapConfig.cs`

---

### 3. 免疫援軍 / 守護伴隨微粒系統 (Immune Companions)
* **系統分類**：戰鬥深度 / 僚機輔助玩法
* **核心功能**：
  * 建立獨立伴隨微粒管理器，支援上限控制、跟隨/環繞軌道、自動索敵與等級縮放。
  * **三大免疫僚機原型**：
    * **補體 MAC 微粒 (Complement MAC Drone)**：環繞巨噬細胞旋轉，自動朝周圍病原體射出穿孔素微刺。
    * **血小板防護環 (Platelet Ring)**：在細胞外圍浮動旋轉，可抵擋或吸收病原體噴射的毒素彈幕。
    * **趨化外泌體 (Chemotactic Exosome)**：主動飛向遠處高密度細菌群，施加「調理作用 (Opsonization)」易傷標記。
* **參考檔案**：
  * `vistrace/src/gameplay/minions/MinionManager.cs`
  * `vistrace/src/gameplay/minions/Minion.cs`
  * `vistrace/src/domain/minions/MinionDef.cs`

---

## 候選評估儲備庫 (Backlog Candidates)

* [ ] **趨化性自動巡航與壓力測試機器人 (`BotPlayerInputProvider.cs` + `AiBotModalPicker.cs`)**：
  * 基於向量位能場的自動風箏與避障 Bot，支援休閒自動巡航與一鍵長時間無人自動壓測。
* [ ] **生化觸發與輔助修飾基因 (`TriggerGemDef.cs` + `SupportGemDef.cs`)**：
  * 「吞噬時觸發 (Cast on Engulf)」、「暴擊時觸發 (Cast on Crit)」、「重傷時觸發 (Cast when Damaged)」。
  * 投射物連鎖 (Chain) 與異常狀態蔓延擴散 (Proliferation)。
* [ ] **19 屬性全明細生化檢查面板 (`CharacterPanel.cs`)**：
  * 遊戲內暫停或結算時，查看進攻、防禦、生存、異常傷害與護甲減傷率的完整數值面板。
* [ ] **持續生化光環 / 趨化場域 (`AuraRegistry.cs` + `AuraDef.cs`)**：
  * 圍繞細胞的持續發炎/趨化力場，對範圍內病原體持續施加微量減速或調理標記。
* [ ] **運行期異常看門狗與黑盒子記錄器 (`AnomalyWatchdog.cs` + `FlightRecorder.cs`)**：
  * 偵測 NaN/無窮大物理向量、殭屍怪物與記憶體洩漏，自動輸出崩潰報告。
