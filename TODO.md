# TODO — 胞器腔室 2x2＋能量負載裝備系統（第三系統）

> 定位：獨立裝備系統，與主動5＋被動5並存。被動＝通用底盤加成；腔室＝高費極端件＋發電拼圖。
> 核心約束：2x2＝最多4件（全1x1無拼接）／基礎能量6／成本域 energy_cost∈[-1,4]（-1＝+1發電）。
> 能量模型（已用-上限）：已用＝Σ正成本，上限＝6＋Σ發電量，合法⇔已用≤上限 且 件數≤4 且 發電件≤2。
> 展示：生化展示櫃 Schematic Display（統一圓形數據邊框＋frame_organelle佔位，只生1x1標本頭）。
> 數值紅線：100%通用Stat（18項），禁私有屬性；+1發電必帶重度負面。
> 出戰流程：選細胞→選配裝（可預配多套）→選天賦→選地圖。背包：6種類籤、上限12（＝首批全圖鑑）、run-scoped。
> 工程量大，分期交付，每期獨立可測。規約見 AGENTS.md（warnaserror、AssetLoader唯一入口、場景純淨、測試隔離）。

## Phase 0 — 數據＋邏輯核心（無UI，純邏輯可測）

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
- [ ] `scripts/core/OrganelleChamber.cs`（新，Node2D，掛玩家下與SkillManager並列）
  - 常數 MaxSlots=4／BaseEnergy=6／MaxGenerators=2／BackpackCap=12；信號 ChamberChanged
  - API：Slots[4]／Backpack List／UsedEnergy／MaxEnergy／CanEquip＋reason／Equip／Unequip／Swap／Discard／GetUiData
  - 經CellStats.Add/RemoveModifier（仿被動特質模式），卸下/替換精確回滾，_ExitTree清理
  - 5張角色卡加子節點（純加節點不改名）；BaseCell._Ready接線（仿CellStats缺失fallback模式）
- [ ] translations.csv：ORGANELLE_*_NAME/DESC/BIO 12套（8欄，缺欄只warn）

## Phase 1 — 出戰配裝頁＋預設套組

- [ ] 新 `LoadoutManager`（static＋JsonStore "organelle_loadouts.json"，每細胞多套預設；仿PassiveTree profile機制）
- [ ] `TestHarness.IsolateSaves` 追加 loadout 隔離路徑（唯一要動的測試腳手架，先標記）
- [ ] 新 `scenes/ui/loadout_view.tscn`：左6籤背包（全12件試穿）＋右2x2上陣＋能量條已用/上限；超載紅閃拒絕
- [ ] `main_menu.tscn` 流程插入：ClassView→LoadoutView→PassiveView→MapView（SwitchToView；組裝檔保持純組裝）
- [ ] `Main.ApplyChamberLoadout()`（仿ApplyTreeLoadout，開局讀active profile 0~4件；Phase1全12件默認解鎖；StartingSlots常數預留，平衡不對可先限2件）
- [ ] translations.csv：CHAMBER_*/LOADOUT_* 鍵

## Phase 2 — 局內獲取＋換/丟＋背包籤UI

- [ ] `UpgradeManager` 新增 `new_organelle` 卡型（含energy_cost/category/image_path；去重已裝＋已擁有）
  - 有空槽＋能量夠→直接上陣；否則進背包；背包滿→換/丟二選一（換：指定替1件；丟：轉小額回血復用heal_fallback）
  - 超載/滿槽置灰（selectable:false＋reason）而非剔除；滿槽滿被動舊斷言不受影響（只斷言!=new_active/new_passive）
- [ ] `UpgradeModal` 右側「活性胞器裝配區」＋新卡渲染（圓框＋cost角標＋種類色；沿用UiBuilders.PanelStyle）
- [ ] `organelle_backpack.tscn`（籤列＋GridContainer）＋`organelle_slot.tscn`（TextureRect64＋Cost角標＋種類色邊框；空位frame_organelle半透明）
- [ ] `organelle_chamber.tscn`（PanelContainer→VBox標題＋能量HBox pips＋GridContainer columns=2）
- [ ] HUD能量條（已用/上限＋超載紅閃；擴容7/7光暈tween）；新場景ExtResource同文件自包含；按鈕沿用menu_buttons.tres

## Phase 3 — 美術管線

- [x] `asset_check` 註冊 organelle 類別（missing雙向檢查＋unmapped／resolution 128x128＋1:1／`--category organelle`）
- [x] `to_target_asset` registry 加 organelle pipeline（128x128＋圓形遮罩，同 passive_tree）
- [x] SKILL.md 目錄架構補 `organelle/`；現況：0/12 ready（12 missing，待生圖）
- [ ] `gen/organelle/` 12張（前綴-free，stems＝id；熒光顯微＋純黑底＋1:1）
- [ ] `make check-assets` 綠燈；缺圖回退frame_organelle但排版正確（佔位驗收）

## Phase 4 — 平衡＋文檔＋新測試

- [ ] P4平衡回測：開局配裝對前三分鐘波次影響；發電雙解強度（殘廢流vs血換電）；amount+1鎖4費複查；CDR/閃避/格擋硬上限複查
- [ ] 文檔：spec.md §5（5+5+腔室4槽位圖）／skill.md §4（六類表）／stat.md（能量註記：約束層非第19屬性）／cell.md（外掛胞器段）
- [ ] 新 `tests/TestOrganelleChamber.cs`（仿TestLevelUpModal骨架＋Gate＋IsolateSaves＋block/evasion歸零＋正對照）
  - 槽上限／能量超載拒絕／發電擴容6/6→6/7／發電負面生效＋卸下回滾／背包上限13拒絕／去重／能量只算已裝／籤過濾／Loadout preset存取隔離／開局生效／滿槽滿包draft換丟分支／非法profile拒絕

## Phase 5 — 全量迴歸＋視覺驗收（可選/後續）

- [ ] `dotnet build --warnaserror` 零警告；`GD.Load/ResourceLoader` 零殘留（只允GodotAssetProvider.cs）
- [ ] 全量42套件（除TestHarness/TestAchievementPreview）綠燈
- [ ] Headed截圖（禁--headless，PHAGOCYTE_CAPTURE_DIR＋/tmp，before/after肉眼對比）
- [ ] Phase2+（可選）：暫停覆層自由整理（仿TreeOverlayPanel）／成就解鎖門檻／背包擴容／開局件數放開
