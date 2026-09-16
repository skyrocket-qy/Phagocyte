class_name CodexModal
extends PanelContainer

const GM = preload("res://scripts/core/game_manager.gd")
const AM = preload("res://scripts/core/achievement_manager.gd")

signal closed()

@onready var title_label: Label = $VBox/Header/Title
@onready var close_btn: Button = $VBox/Header/CloseButton

## Tab Buttons
@onready var tab_skills_btn: Button = $VBox/TabBar/SkillsTab
@onready var tab_cells_btn: Button = $VBox/TabBar/CellsTab
@onready var tab_pathogens_btn: Button = $VBox/TabBar/PathogensTab
@onready var tab_maps_btn: Button = $VBox/TabBar/MapsTab
@onready var tab_achievements_btn: Button = $VBox/TabBar/AchievementsTab

## Content Panels
@onready var item_list: VBoxContainer = $VBox/HBox/Scroll/ItemList
@onready var detail_title: Label = $VBox/HBox/DetailPanel/VBox/DetailTitle
@onready var detail_badge: Label = $VBox/HBox/DetailPanel/VBox/DetailBadge
@onready var detail_stats: Label = $VBox/HBox/DetailPanel/VBox/DetailStats
@onready var detail_desc: Label = $VBox/HBox/DetailPanel/VBox/DetailDesc
@onready var detail_bio: Label = $VBox/HBox/DetailPanel/VBox/DetailBio

var current_tab: int = 0
var active_item_key: String = ""

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false

	close_btn.pressed.connect(close_codex)

	tab_skills_btn.pressed.connect(func(): switch_tab(0))
	tab_cells_btn.pressed.connect(func(): switch_tab(1))
	tab_pathogens_btn.pressed.connect(func(): switch_tab(2))
	tab_maps_btn.pressed.connect(func(): switch_tab(3))
	tab_achievements_btn.pressed.connect(func(): switch_tab(4))

	GM.add_language_listener(_on_language_changed)
	update_localized_texts()

func _exit_tree() -> void:
	GM.remove_language_listener(_on_language_changed)

func open_codex(target_tab: int = 0) -> void:
	visible = true
	switch_tab(target_tab)

func close_codex() -> void:
	visible = false
	closed.emit()

func _on_language_changed(_locale: String) -> void:
	update_localized_texts()

func update_localized_texts() -> void:
	title_label.text = tr("CODEX_TITLE")
	close_btn.text = "✕"
	tab_skills_btn.text = tr("CODEX_TAB_SKILLS")
	tab_cells_btn.text = tr("CODEX_TAB_CELLS")
	tab_pathogens_btn.text = tr("CODEX_TAB_PATHOGENS")
	tab_maps_btn.text = tr("CODEX_TAB_MAPS")
	tab_achievements_btn.text = tr("CODEX_TAB_ACHIEVEMENTS")

	_render_current_tab()

func switch_tab(tab_idx: int) -> void:
	current_tab = tab_idx

	# Update active button style/color
	tab_skills_btn.modulate = Color(1, 1, 1) if tab_idx == 0 else Color(0.7, 0.7, 0.7)
	tab_cells_btn.modulate = Color(1, 1, 1) if tab_idx == 1 else Color(0.7, 0.7, 0.7)
	tab_pathogens_btn.modulate = Color(1, 1, 1) if tab_idx == 2 else Color(0.7, 0.7, 0.7)
	tab_maps_btn.modulate = Color(1, 1, 1) if tab_idx == 3 else Color(0.7, 0.7, 0.7)
	tab_achievements_btn.modulate = Color(1, 1, 1) if tab_idx == 4 else Color(0.7, 0.7, 0.7)

	active_item_key = ""
	_render_current_tab()

func _render_current_tab() -> void:
	for child in item_list.get_children():
		item_list.remove_child(child)
		child.queue_free()

	match current_tab:
		0:
			_render_skills_tab()
		1:
			_render_cells_tab()
		2:
			_render_pathogens_tab()
		3:
			_render_maps_tab()
		4:
			_render_achievements_tab()

func _render_skills_tab() -> void:
	var first_key: String = ""
	for key in GM.SKILL_CATALOG.keys():
		if first_key == "":
			first_key = key
		var skill_info = GM.get_skill_info(key)
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(230, 42)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.text = " " + skill_info["icon"] + " " + skill_info["name"]
		btn.pressed.connect(func(): _select_skill(key))
		item_list.add_child(btn)

	var target = active_item_key if active_item_key != "" and GM.SKILL_CATALOG.has(active_item_key) else first_key
	if target != "":
		_select_skill(target)

func _select_skill(key: String) -> void:
	active_item_key = key
	var d = GM.get_skill_info(key)
	detail_title.text = d["icon"] + " " + d["name"]
	detail_badge.text = "[ " + d["type_label"] + " ]"

	match d["type"]:
		"innate":
			detail_badge.modulate = Color(0.4, 0.95, 0.8)
			detail_stats.text = tr("TOOLTIP_ALWAYS_ACTIVE") + " • " + (tr("TOOLTIP_LV_FORMAT") % [1, d["max_level"]])
		"active":
			detail_badge.modulate = Color(1.0, 0.85, 0.3)
			detail_stats.text = (tr("TOOLTIP_CD") % d["cooldown"]) + " • " + (tr("TOOLTIP_LV_FORMAT") % [1, d["max_level"]])
		"passive":
			detail_badge.modulate = Color(0.6, 0.8, 1.0)
			detail_stats.text = tr("TOOLTIP_ALWAYS_ACTIVE") + " • " + (tr("TOOLTIP_LV_FORMAT") % [1, d["max_level"]])

	detail_desc.text = tr("CODEX_HEADER_TACTICAL") + "\n" + d["description"]
	detail_bio.text = tr("CODEX_HEADER_BIO") + "\n" + d["biochemistry"]

func _render_cells_tab() -> void:
	var first_key: String = ""
	for key in GM.CLASS_DATA.keys():
		if first_key == "":
			first_key = key
		var d = GM.get_class_info(key)
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(230, 42)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.text = (" ✅ " if d["unlocked"] else " 🔒 ") + d["name"]
		btn.pressed.connect(func(): _select_cell(key))
		item_list.add_child(btn)

	var target = active_item_key if active_item_key != "" and GM.CLASS_DATA.has(active_item_key) else first_key
	if target != "":
		_select_cell(target)

func _select_cell(key: String) -> void:
	active_item_key = key
	var d = GM.get_class_info(key)
	detail_title.text = "🛡️ " + d["name"]
	detail_badge.text = "[ " + (tr("STATUS_UNLOCKED") if d["unlocked"] else tr("STATUS_LOCKED")) + " ]"
	detail_badge.modulate = Color(0.3, 1.0, 0.4) if d["unlocked"] else Color(0.9, 0.6, 0.2)
	detail_stats.text = tr("LABEL_ROLE") + d["role"]

	if d["unlocked"]:
		detail_desc.text = tr("CODEX_HEADER_TRAIT") + "\n" + d["trait"]
	else:
		detail_desc.text = AM.get_cell_unlock_requirement_text(key) + "\n\n" + tr("CODEX_HEADER_TRAIT") + "\n" + d["trait"]
	detail_bio.text = tr("CODEX_HEADER_PASSIVE") + "\n" + d["passive"] + "\n\n" + tr("CODEX_HEADER_BURST") + "\n" + d["burst"]

func _render_pathogens_tab() -> void:
	var first_key: String = ""
	for key in GM.PATHOGEN_CATALOG.keys():
		if first_key == "":
			first_key = key
		var d = GM.get_pathogen_info(key)
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(230, 42)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.text = " " + d["icon"] + " " + d["name"]
		btn.pressed.connect(func(): _select_pathogen(key))
		item_list.add_child(btn)

	var target = active_item_key if active_item_key != "" and GM.PATHOGEN_CATALOG.has(active_item_key) else first_key
	if target != "":
		_select_pathogen(target)

func _select_pathogen(key: String) -> void:
	active_item_key = key
	var d = GM.get_pathogen_info(key)
	detail_title.text = d["icon"] + " " + d["name"]
	var danger_lv = d.get("danger_level", d.get("threat_level", "I"))
	detail_badge.text = "[ " + (tr("CODEX_THREAT_LV") % danger_lv) + " ]"
	detail_badge.modulate = Color(1.0, 0.4, 0.4)
	detail_stats.text = d["trait"]

	detail_desc.text = tr("CODEX_HEADER_PATHOGEN_TRAIT") + "\n" + d["description"]
	detail_bio.text = tr("CODEX_HEADER_TACTIC_ADVICE") + "\n" + tr("CODEX_PATHOGEN_ADVICE_DEFAULT")

func _render_maps_tab() -> void:
	var first_key: String = ""
	for key in GM.MAP_DATA.keys():
		if first_key == "":
			first_key = key
		var d = GM.get_map_info(key)
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(230, 42)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.text = " 🌐 " + d["name"]
		btn.pressed.connect(func(): _select_map(key))
		item_list.add_child(btn)

	var target = active_item_key if active_item_key != "" and GM.MAP_DATA.has(active_item_key) else first_key
	if target != "":
		_select_map(target)

func _select_map(key: String) -> void:
	active_item_key = key
	var d = GM.get_map_info(key)
	detail_title.text = "🌐 " + d["name"]
	detail_badge.text = "[ " + tr("CODEX_STAGE_STATUS_OPEN") + " ]"
	detail_badge.modulate = Color(0.4, 0.8, 1.0)
	detail_stats.text = tr("LABEL_ENV") + d["environment"]

	detail_desc.text = tr("CODEX_HEADER_MECH") + "\n" + d["mechanic"]
	detail_bio.text = tr("CODEX_HEADER_THREAT") + "\n" + d["threat"]

func _render_achievements_tab() -> void:
	var ach_list = AM.get_all_achievements()
	var first_id = ""
	for ach in ach_list:
		if first_id == "":
			first_id = ach["id"]
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(230, 42)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		var status_icon = " ✅ " if ach["unlocked"] else " 🔒 "
		btn.text = status_icon + ach["icon"] + " " + ach["title"]
		var aid = ach["id"]
		btn.pressed.connect(func(): _select_achievement(aid))
		item_list.add_child(btn)

	var target = active_item_key if active_item_key != "" and AM.ACHIEVEMENTS.has(active_item_key) else first_id
	if target != "":
		_select_achievement(target)

func _select_achievement(ach_id: String) -> void:
	active_item_key = ach_id
	var d = AM.get_achievement_info(ach_id)
	detail_title.text = d["icon"] + " " + d["title"]
	detail_badge.text = "[ " + (tr("STATUS_ACH_COMPLETED") if d["unlocked"] else tr("STATUS_ACH_LOCKED")) + " ]"
	detail_badge.modulate = Color(0.3, 1.0, 0.4) if d["unlocked"] else Color(0.9, 0.6, 0.2)

	var cur_val_str = str(int(d["current_value"])) if d["target_value"] >= 1.0 else "%.1f" % d["current_value"]
	var target_val_str = str(int(d["target_value"])) if d["target_value"] >= 1.0 else "%.1f" % d["target_value"]
	detail_stats.text = "%s: %s / %s (%d%%)" % [
		(tr("STATUS_ACH_COMPLETED") if d["unlocked"] else tr("STATUS_ACH_LOCKED")),
		cur_val_str,
		target_val_str,
		int(d["progress_ratio"] * 100)
	]

	detail_desc.text = tr("LABEL_UNLOCK_REQ") + "\n" + d["desc"]
	if d["reward"] != "":
		detail_bio.text = tr("LABEL_REWARD") + "\n" + d["reward"]
	else:
		detail_bio.text = ""
