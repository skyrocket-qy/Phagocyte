# TODO — 胞器腔室 2x2＋能量負載裝備系統（第三系統）

> 定位：獨立裝備系統，與主動5＋被動5並存。被動＝通用底盤加成；腔室＝高費極端件＋發電拼圖。
> 核心約束：2x2＝最多4件（全1x1無拼接）／基礎能量6／成本域 energy_cost∈[-1,4]（-1＝+1發電）。
> 能量模型（已用-上限）：已用＝Σ正成本，上限＝6＋Σ發電量，合法⇔已用≤上限 且 件數≤4 且 發電件≤2。
> 展示：生化展示櫃 Schematic Display（統一圓形數據邊框＋frame_organelle佔位，只生1x1標本頭）。
> 數值紅線：100%通用Stat（19項），禁私有屬性；+1發電必帶重度負面。
> 出戰流程：選細胞→選配裝（可預配多套）→選天賦→選地圖。背包：6種類籤、上限12（＝首批全圖鑑）、run-scoped。
> 工程量大，分期交付，每期獨立可測。規約見 AGENTS.md（warnaserror、AssetLoader唯一入口、場景純淨、測試隔離）。

## Phase 0 — 數據＋邏輯核心（無UI，純邏輯可測）✅ 完成

- [x] `assets/data/organelles.json`（頂層array，12件：6類×2＝1高費核心＋1低費/發電對照）
  - 欄位：id／category（metabolism/digestion/cytoskeleton/synthesis/sensing/symbiosis）
    ／energy_cost(-1~4)／max_copies=1／modifiers[{stat,value,unit}]／drawback[]／
    name_key/desc_key/bio_key／icon
  - 12件定稿：mitochondria_mkii(4)／glycolytic_bypass(1)／acidic_lysosome(3)／
    proteasome_sieve(1)／flagellar_base(3)／microtubule_anchor(1)／rough_er(4,唯一amount+1)／
    ribosome_cluster(2)／ion_channel_array(3)／chemokine_patch(1)／
    symbiotic_flora(-1,移速-30%/might-15%)／phage_fragment(-1,max_health-20%/CDR+0.05)
- [x] `DataPaths.Organelles`＋`CatalogBuilders.BuildOrganelles()`（判重id、cost越界、stat合法、發電必有drawback）
- [x] `GameManager.OrganelleCatalog`（仿SkillCatalog懶載入＋EnsureValidated）
- [x] 資料契約測試 `tests/TestOrganelleCatalog.cs`（12件／6類×2／cost域／發電必有drawback／stat合法，headless 綠燈）
- [x] `scripts/core/OrganelleChamber.cs`（新，Node2D，掛玩家下與SkillManager並列）
  - 常數 MaxSlots=4／BaseEnergy=6／MaxGenerators=2／BackpackCap=12；信號 ChamberChanged
  - API：Slots[4]／Backpack List／UsedEnergy／MaxEnergy／CanEquip＋reason／Equip／Unequip／Swap／Discard／GetUiData
  - 經CellStats.Add/RemoveModifier（仿被動特質模式），卸下/替換精確回滾，_ExitTree清理
  - 5張角色卡加子節點（純加節點不改名）；BaseCell._Ready接線（仿CellStats缺失fallback模式）
- [x] translations.csv：ORGANELLE_*_NAME/DESC/BIO 12套（8欄，缺欄只warn）
- [x] `tests/TestOrganelleChamber.cs`（17項：常數／超載拒絕／發電擴容6→7／負面生效與回滾／替換語義／背包去重／能量只算已裝／reason token／雙發電上限8／UiData／Discard／_ExitTree／5角色卡接線＋code fallback／翻譯解析，headless 綠燈）

## Phase 1 — 出戰配裝頁＋預設套組

- 設計定案：**細胞預設沒有任何裝備（裸裝開局）**；配裝頁可預先配置，預設 profile 為 4 空槽
- [x] 新 `LoadoutManager`（static＋JsonStore "organelle_loadouts.json"，每細胞多套預設；仿PassiveTree profile機制）
- [x] `TestHarness.IsolateSaves` 追加 loadout 隔離路徑（唯一要動的測試腳手架，先標記）
- [x] 新 `scenes/ui/loadout_view.tscn`：左6籤背包（全12件試穿）＋右2x2上陣＋能量條已用/上限；超載紅閃拒絕
  - 互動：點擊胞器裝入首個空槽／再點已裝者卸下；超載/發電上限/滿槽以 hint＋能量條紅閃拒絕（拖放留 Phase2+）
  - 場景拆分：`loadout_view.tscn`（殼）＋`organelle_slot.tscn`（item，code loop 生成12+4張）
- [x] `main_menu.tscn` 流程插入：ClassView→LoadoutView→PassiveView→MapView（SwitchToView；組裝檔保持純組裝）
  - 返回鍵鏈更新為 Map→Passive→Loadout→Class→Title（`TestMenuFlow`／`TestPassiveTree`／`TestAchievementGallery` 同步更新）
- [x] `Main.ApplyChamberLoadout()`（仿ApplyTreeLoadout，開局讀active profile；預設空＝不裝備，非法項略過）
- [x] translations.csv：LOADOUT_*/ORGANELLE_CAT_* 鍵（27條×8語言）
- [x] 視覺驗收：headed 截圖確認版面（標題／profile籤／6籤庫存／2x2腔室／能量條／拒絕提示）

### Phase 1 修訂（使用者要求）

- 版面：**裝備欄（2x2腔室）在左、胞器庫存在右**
- 取得方式：**胞器改為打怪掉落解鎖**（基礎機率 2%，刻意偏低）；庫存預設**全鎖**
- [x] 新 `scripts/core/OrganelleUnlockManager.cs`（static＋JsonStore "organelle_unlocks.json"；IsUnlocked／Unlock／RollLockedId／TrySpawnDrop／UnlockAll／listener 通知）
  - 可調 seam：`DropChance`（預設 0.02）、`DropsEnabled`（測試關閉以求確定性）
- [x] 新 `scripts/core/OrganelleDrop.cs`（code-only 瞬態拾取物：脈衝光環＋圖示→磁吸（吃 `magnet` stat）→拾取→解鎖→toast）
- [x] 掉落入口單一化：`BaseEnemy.Die()`（所有死亡皆經此），掠過非真正掉落
- [x] 三重解鎖閘：`LoadoutManager.ParseRow`（讀檔剝離）／`LoadoutManager.SetSlots`（寫入拒絕）／`Main.ApplyChamberLoadout`（開局略過）
- [x] UI：鎖定卡片灰階＋「未解鎖」橙標＋無費用角標；詳情顯示「尚未解鎖」；庫存標題帶進度「已解鎖 n/12」
- [x] `ToastView` 新增胞器解鎖 banner（綠 accent，共用現有 toast_banner.tscn）
- [x] translations：LOADOUT_LOCKED_TAG／LOADOUT_HINT_LOCKED／LOADOUT_DETAIL_LOCKED／LOADOUT_VAULT_PROGRESS／TOAST_ORGANELLE_UNLOCKED
- [x] 測試：`TestOrganelleChamber` 增 7 項（全鎖起始／解鎖冪等＋持久化／只滾鎖定id／suite 預設關掉落／機率1→生成→拾取解鎖／全解鎖停止掉落／鎖定項被 profile 拒絕與剝離）
- [x] 順帶修復既有 flake：`TestLevelUpModal` 在斷言前重跑隔離（凍結玩家 physics＋關閉殘留 modal＋重置 HUD 快照），20/20 穩定

### Phase 1 修訂 2（能量圖形化）

- [x] 新 `scripts/ui/EnergyPips.cs`（Control＋`_Draw`）：一顆圓點＝1 點能量；已消耗＝青色實心、未用＝暗色空心、**發電增益＝紅色**；`MaxDiameter/Gap/AlignLeft` 為 `[Export]`，圓徑自動縮放以適配卡片與腔室列
- [x] 卡片費用角標改為 pip 列（`organelle_slot.tscn` 的 `CostPips`）：cost N → N 青點；發電件 → 1 紅點；未解鎖 → 不顯示；tooltip 保留數字（`LOADOUT_COST_LABEL`）
- [x] 腔室能量條由 ProgressBar 改為 pip 列（6~8 顆）：尾端發電增益點為紅
- [x] 清理：移除已無引用的能量 StyleBox 與舊 CostLabel；`LOADOUT_COST_LABEL` 8 語言
- [x] 視覺驗收：headed 截圖確認（4 青＋2 暗＋1 紅＝4/7 含 1 發電；卡片費用 pip 與未解鎖空白）

### Phase 1 修訂 3（版面與測試確定性）

- [x] `class_view.tscn` 標題與內容面板重疊修復：內容框由「置中錨點＋向上溢出」改為「頂部固定 92／底部固定 -106＋向下增長」，`ClassBioLabel` 改為可壓縮（`clip_text`＋`size_flags_vertical=3` 吸收餘裕）、VBox 間距 10→6
  - 修前：header 40-85 vs 面板頂 67（重疊 18px）；修後：header 40-85 vs 面板頂 92（安全間距）
- [x] 既有 flake 修復（AGENTS.md 確定性規則）：
  - `TestSurvivorHudUx`：斷言前重跑隔離（凍結玩家 physics／關閉殘留 modal／解除暫停／重置教學 cue），避免環境升級把 HUD 透明度目標翻回 1.0；12/12 穩定
  - `TestMapEnvironments`：酸潮傷害斷言前歸零 `block`／`evasion`（巨噬底盤自帶 8% 格擋，原會 ~8% 機率假失敗）；12/12 穩定
- [x] 連續兩輪全量掃描 44/44 綠燈

## Phase 2 — 局內獲取＋換/丟（✅ 完成）

- 設計變更：不再另開「背包籤UI」頁；胞器取得走**升級三選一**，滿槽時在**同一 modal 內**做換裝
- [x] `UpgradeManager` 新增 `new_organelle` 卡型：僅**已解鎖且未擁有**才進池，且每輪最多 **1 張**胞器卡（保證只有一件待安置物）
  - `ApplyChoice`：先入 run 背包，再 best-effort 裝入首個「空且合法」槽；重複／未解鎖拒收
- [x] `UpgradeModal` 滿槽換裝步驟（`SwapPanel`，全在 upgrade_modal.tscn 內）：
  - 標題／候選（名稱＋能量）／能量列（`EnergyPips`，發電點紅）／4 個槽位卡／提示／三鍵：存入背包、丟棄＋回血、返回三選一
  - 「替換」＝點槽位卡：`chamber.Equip(id, slot)`（自動退還舊件）；超載/發電上限等以 hint 顯示原因並留在換裝中
  - 「丟棄＋回血」＝`chamber.Discard` ＋ `Heal(maxHp × 15%)`；「取消」回三選一（遊戲維持暫停，不消耗等級）
  - 新增公開 API：`EnterSwapMode／OnSwapSlotPressed／OnSwapStorePressed／OnSwapDiscardPressed／OnSwapCancelPressed／SwapHintText／PendingOrganelle`（另接受卡片字典的 `player` 作為 owner 覆寫，供腳本化流程／測試）
- [x] 卡面渲染：`BADGE_NEW_ORGANELLE` 標籤 ＋ 描述下方附「能量: N」（工具提示沿用 LOADOUT_COST_LABEL）
- [x] HUD 腔室能量列（`SkillBarView`，code-built 免改共用 hud.tscn）：`⚡ 已用/上限`＋pip 列；**裝了/持有胞器才顯示**，超載轉紅
- [x] translations：`BADGE_NEW_ORGANELLE`＋`SWAP_TITLE/STORE/DISCARD/CANCEL/HINT`（8 語言）
- [x] 測試（`TestOrganelleChamber` 再增）：
  - Draft：只出已解鎖＋未擁有／每輪≤1 張／收取即自動上陣／重複與未解鎖拒收
  - Swap：滿槽開啟換裝／替換成功（退還舊件＋能量重算）／超載拒絕並顯示 overload hint／取消回卡片且仍暫停／存入保留／丟棄移除
  - Discard & HUD：僅能丟未裝備件／HUD 列未互動隱藏、裝備後顯示 `1 / 6`
- [x] 視覺驗收：headed 截圖確認換裝步驟（候選＋5/7 能量 pips 含紅點＋4 槽位＋三鍵）與 HUD 列（`⚡ 5 / 7`）
- [x] 全量掃描 44/44 綠燈

## Phase 3 — 美術管線

- [x] `asset_check` 註冊 organelle 類別（missing雙向檢查＋unmapped／resolution 128x128＋1:1／`--category organelle`）
- [x] `to_target_asset` registry 加 organelle pipeline（128x128＋圓形遮罩，同 passive_tree）
- [x] SKILL.md 目錄架構補 `organelle/`
- [x] `gen/organelle/` 12張（前綴-free，stems＝id；熒光顯微＋純黑底＋1:1）＋`assets/gen/organelle/` 已處理
- [x] `make check-assets` 綠燈：Organelle 12/12 ready，無 naming／orphan／resolution／unmapped 問題

## Phase 4 — 平衡＋文檔＋新測試 ✅ 完成

- [x] P4平衡回測：開局配裝對前三分鐘波次影響；發電雙解強度（殘廢流vs血換電）；amount+1鎖4費複查；CDR/閃避/格擋硬上限複查
  - 開局影響：庫存預設全鎖＋掉落基礎機率 2%，新號裸裝開局，前三分鐘（03:00 單精英）無配裝紅利；老號滿配上限約 might +12%（acidic_lysosome 單源），`4+1+1` 滿配合法、`4+3` 超載，屬微調非質變
  - 發電雙解：symbiotic_flora（+1 電／move -30%／might -15%，殘廢流）＋phage_fragment（+1 電／CDR +0.05／max_hp -20%，血換電）；雙發電 6→8 能量，總代價 move -30%／might -15%／hp -20%，8 能量供 `4+3`（已用 7）貪婪連招，坦度／機動墊底
  - amount+1 鎖 4 費：唯一來源 rough_er（4 費，佔基礎預算 2/3）；`rough(4)+acidic(3)=7>6` 非法、`rough+1+1=6` 合法，機會成本成立
  - 硬上限：腔室 CDR 合計 +0.26＋被動 Lv5 +0.40＋底盤 0.10 仍被 0.75 鉗制；腔室 evasion +0.02／block +0.04 遠低於 0.60／0.75；數值零調整，約束生效
  - 新測試 `tests/TestOrganelleBalance.cs`（5 項：預算／雙發電代價／amount 鎖定／硬上限／開局上限＋能量非 stat，headless 綠燈）
- [x] 文檔：spec.md §5（5+5+腔室4槽位圖）／skill.md §4.1（六類表）／stat.md §3.1（能量註記：約束層非 stat）／cell.md §1.4（外掛胞器段）
  - 順帶對齊：stat 18→19 項（補 `ailment_damage` 行；spec §4／stat §1／skill §1 計數同步），能量為約束層、不佔 stat 槽位
- [x] `tests/TestOrganelleChamber.cs`（已建；Phase 0 邏輯項全綠）
  - 已完成：常數／能量超載拒絕／發電擴容6→7／發電負面生效＋卸下回滾／替換語義／去重（max_copies）／能量只算已裝／CanEquip reason token／雙發電上限／UiData／Discard／_ExitTree／5角色卡接線＋code fallback／翻譯鍵解析
  - 待補（依賴後續Phase）：籤過濾（分類籤已存在於配裝頁；draft 不需籤）
  - 已完成（Phase 1）：Loadout preset存取隔離／開局生效／非法profile拒絕／預設空裝／頁面裝卸持久化
  - 已完成（Phase 2）：draft 候選／收取自動上陣／滿槽換裝／超載拒絕／存入／丟棄＋回血／取消／HUD 能量列
  - 註：BackpackCap=12 與 MaxGenerators=2 在現有12件×max_copies=1資料下為結構性守衛（不可達）；已由去重/copy_cap覆蓋等效行為