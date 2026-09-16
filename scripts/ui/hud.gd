class_name HUD
extends CanvasLayer

const GM = preload("res://scripts/core/game_manager.gd")
const AM = preload("res://scripts/core/achievement_manager.gd")
const UpgradeModalClass = preload("res://scripts/ui/upgrade_modal.gd")
const UpgradeModalScene = preload("res://scenes/ui/upgrade_modal.tscn")

var achievement_banner: PanelContainer = null
var ach_banner_title: Label = null
var ach_banner_desc: Label = null
var ach_banner_icon: Label = null
var ach_tween: Tween = null

@onready var title_label: Label = $MarginContainer/PanelContainer/VBoxContainer/TitleLabel
@onready var map_label: Label = $MarginContainer/PanelContainer/VBoxContainer/MapLabel
@onready var hp_bar: ProgressBar = $MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPBar
@onready var hp_label: Label = $MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPLabel
@onready var atp_bar: ProgressBar = $MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPBar
@onready var atp_label: Label = $MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPLabel
@onready var size_label: Label = $MarginContainer/PanelContainer/VBoxContainer/SizeLabel
@onready var count_label: Label = $MarginContainer/PanelContainer/VBoxContainer/CountLabel

@onready var burst_panel: PanelContainer = $BurstContainer
@onready var burst_label: Label = $BurstContainer/VBox/BurstLabel
@onready var burst_bar: ProgressBar = $BurstContainer/VBox/BurstBar

@onready var pause_modal: PanelContainer = $PauseModal
@onready var pause_title: Label = $PauseModal/VBox/Title
@onready var resume_btn: Button = $PauseModal/VBox/ResumeButton
@onready var settings_btn: Button = $PauseModal/VBox/SettingsButton
@onready var manual_btn: Button = $PauseModal/VBox/ManualButton
@onready var restart_btn: Button = $PauseModal/VBox/RestartButton
@onready var menu_btn: Button = $PauseModal/VBox/MenuButton
@onready var codex_modal = $CodexModal
@onready var settings_modal = $SettingsModal

## Skill Bar & Tooltip Nodes
@onready var skill_title_lbl: Label = $SkillContainer/VBox/TitleLabel
@onready var slots_container: GridContainer = $SkillContainer/VBox/SlotsContainer

@onready var skill_tooltip: PanelContainer = $SkillTooltip
@onready var tooltip_icon: Label = $SkillTooltip/VBox/HeaderHBox/TooltipIcon
@onready var tooltip_title: Label = $SkillTooltip/VBox/HeaderHBox/TooltipTitle
@onready var tooltip_badge: Label = $SkillTooltip/VBox/HeaderHBox/TooltipBadge
@onready var tooltip_stats: Label = $SkillTooltip/VBox/TooltipStats
@onready var tooltip_desc: Label = $SkillTooltip/VBox/TooltipDesc
@onready var tooltip_bio: Label = $SkillTooltip/VBox/TooltipBio

var hovered_slot_idx: int = -1

# Cached last stats for re-rendering upon language change
var last_health: float = 100.0
var last_max_health: float = 100.0
var last_satiety: float = 0.0
var last_max_satiety: float = 100.0
var last_radius_ratio: float = 1.0
var last_digested_count: int = 0
var last_burst_time_left: float = 0.0

var player_ref: Node2D = null
var upgrade_modal: UpgradeModalClass = null

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	burst_panel.visible = false
	pause_modal.visible = false

	if has_node("UpgradeModal"):
		upgrade_modal = $UpgradeModal as UpgradeModalClass
	else:
		if UpgradeModalScene:
			upgrade_modal = UpgradeModalScene.instantiate() as UpgradeModalClass
			add_child(upgrade_modal)

	if not resume_btn.pressed.is_connected(resume_game):
		resume_btn.pressed.connect(resume_game)
	if settings_btn and not settings_btn.pressed.is_connected(_on_settings_pressed):
		settings_btn.pressed.connect(_on_settings_pressed)
	if not manual_btn.pressed.is_connected(_on_manual_pressed):
		manual_btn.pressed.connect(_on_manual_pressed)
	if not restart_btn.pressed.is_connected(_on_restart_pressed):
		restart_btn.pressed.connect(_on_restart_pressed)
	if not menu_btn.pressed.is_connected(_on_menu_pressed):
		menu_btn.pressed.connect(_on_menu_pressed)

	_setup_slot_hover_signals()
	_setup_achievement_banner()

	AM.add_unlock_listener(_on_achievement_unlocked)
	GM.add_language_listener(_on_language_changed)
	_update_localized_texts()

func _exit_tree() -> void:
	AM.remove_unlock_listener(_on_achievement_unlocked)
	GM.remove_language_listener(_on_language_changed)

func _process(_delta: float) -> void:
	_update_skill_slots()

func _on_language_changed(_locale: String) -> void:
	_update_localized_texts()

func _update_localized_texts() -> void:
	title_label.text = tr("HUD_TITLE")
	
	var map_info = GM.get_map_info(GM.selected_map)
	if map_info.has("name"):
		map_label.text = tr("HUD_BATTLEFIELD") + map_info["name"]

	hp_label.text = tr("HUD_HP") % [int(last_health), int(last_max_health)]
	atp_label.text = tr("HUD_ATP") % [int((last_satiety / max(1.0, last_max_satiety)) * 100.0)]
	size_label.text = tr("HUD_SIZE") % last_radius_ratio
	count_label.text = tr("HUD_DIGESTED") % last_digested_count
	skill_title_lbl.text = tr("SKILL_BAR_DUAL_TITLE")

	if burst_panel.visible:
		burst_label.text = tr("HUD_BURST_ALERT") % last_burst_time_left

	pause_title.text = tr("PAUSE_TITLE")
	resume_btn.text = tr("PAUSE_RESUME")
	if settings_btn:
		settings_btn.text = tr("BTN_SETTINGS")
	manual_btn.text = tr("PAUSE_MANUAL")
	restart_btn.text = tr("PAUSE_RESTART")
	menu_btn.text = tr("PAUSE_MENU")

	if hovered_slot_idx >= 0 and is_instance_valid(skill_tooltip) and skill_tooltip.visible:
		_refresh_tooltip_content(hovered_slot_idx)

	_update_skill_slots()

func _update_skill_slots() -> void:
	if not player_ref or not is_instance_valid(player_ref):
		player_ref = get_tree().get_first_node_in_group("player")
		if not player_ref:
			return
		if player_ref.has_signal("level_up") and not player_ref.level_up.is_connected(_on_player_level_up):
			player_ref.level_up.connect(_on_player_level_up)

	var sm = player_ref.get_node_or_null("SkillManager")
	if not sm or not sm.has_method("get_all_ui_data"):
		return

	var skills_data = sm.get_all_ui_data()
	var slot_children = slots_container.get_children()

	for i in range(mini(slot_children.size(), skills_data.size())):
		var slot_card = slot_children[i]
		var data = skills_data[i]

		var icon_lbl = slot_card.get_node_or_null("IconLabel")
		var badge_lbl = slot_card.get_node_or_null("BadgeLabel")
		var cd_overlay = slot_card.get_node_or_null("CooldownBar")

		if data["id"] != "":
			if icon_lbl:
				icon_lbl.text = data["icon"]
				icon_lbl.modulate = Color(1, 1, 1, 1)
			if badge_lbl:
				if data.get("is_innate", false):
					badge_lbl.text = tr("SKILL_INNATE_TAG")
					badge_lbl.modulate = Color(0.4, 0.95, 0.8)
				elif data.get("is_passive", false) or i >= 5:
					badge_lbl.text = tr("SKILL_LV") % data["level"]
					badge_lbl.modulate = Color(0.8, 0.6, 1.0)
				else:
					badge_lbl.text = tr("SKILL_LV") % data["level"]
					badge_lbl.modulate = Color(1.0, 0.9, 0.3)
			if cd_overlay:
				var has_cd = (not data.get("is_passive", false)) and data.get("cooldown_ratio", 0.0) > 0.0
				cd_overlay.visible = has_cd
				cd_overlay.value = data.get("cooldown_ratio", 0.0)
		else:
			# Empty Slot
			if icon_lbl:
				icon_lbl.text = "+"
				if i >= 5:
					icon_lbl.modulate = Color(0.65, 0.55, 0.8, 0.5)
				else:
					icon_lbl.modulate = Color(0.4, 0.5, 0.6, 0.6)
			if badge_lbl:
				badge_lbl.text = ""
			if cd_overlay:
				cd_overlay.visible = false

func _on_restart_pressed() -> void:
	GM.restart_game(get_tree())

func _on_menu_pressed() -> void:
	GM.go_to_menu(get_tree())

func _on_manual_pressed() -> void:
	codex_modal.open_codex(0)

func _on_settings_pressed() -> void:
	if settings_modal and settings_modal.has_method("open_settings"):
		settings_modal.open_settings(0)

func _setup_slot_hover_signals() -> void:
	var slot_children = slots_container.get_children()
	for i in range(slot_children.size()):
		var card = slot_children[i]
		card.mouse_filter = Control.MOUSE_FILTER_STOP
		var idx = i
		card.mouse_entered.connect(func(): _on_slot_mouse_entered(idx, card))
		card.mouse_exited.connect(func(): _on_slot_mouse_exited(idx))

func _on_slot_mouse_entered(slot_idx: int, card: Control) -> void:
	hovered_slot_idx = slot_idx
	_refresh_tooltip_content(slot_idx)

	var card_rect = card.get_global_rect()
	var vp_size = get_viewport().get_visible_rect().size
	var target_x = clampf(card_rect.get_center().x - 150.0, 10.0, vp_size.x - 310.0)
	var target_y = card_rect.position.y - skill_tooltip.size.y - 10.0
	skill_tooltip.global_position = Vector2(target_x, target_y)
	skill_tooltip.visible = true

func _on_slot_mouse_exited(slot_idx: int) -> void:
	if hovered_slot_idx == slot_idx:
		hovered_slot_idx = -1
		skill_tooltip.visible = false

func _refresh_tooltip_content(slot_idx: int) -> void:
	if not player_ref or not is_instance_valid(player_ref):
		player_ref = get_tree().get_first_node_in_group("player")
		if not player_ref:
			return
	var sm = player_ref.get_node_or_null("SkillManager")
	if not sm or not sm.has_method("get_all_ui_data"):
		return

	var skills_data = sm.get_all_ui_data()
	if slot_idx < 0 or slot_idx >= skills_data.size():
		return

	var data = skills_data[slot_idx]
	if data["id"] != "":
		tooltip_icon.text = data["icon"]
		tooltip_title.text = data["name"]

		var badge_text = ""
		var badge_color = Color(1, 1, 1)
		var stats_text = ""

		if data["is_innate"]:
			badge_text = "[ " + tr("TOOLTIP_TAG_INNATE") + " ]"
			badge_color = Color(0.4, 0.95, 0.8)
			stats_text = tr("TOOLTIP_ALWAYS_ACTIVE") + " • " + (tr("TOOLTIP_LV_FORMAT") % [data["level"], data["max_level"]])
		elif data["is_passive"]:
			badge_text = "[ " + tr("TOOLTIP_TAG_PASSIVE") + " ]"
			badge_color = Color(0.6, 0.8, 1.0)
			stats_text = tr("TOOLTIP_ALWAYS_ACTIVE") + " • " + (tr("TOOLTIP_LV_FORMAT") % [data["level"], data["max_level"]])
		else:
			badge_text = "[ " + tr("TOOLTIP_TAG_ACTIVE") + " ]"
			badge_color = Color(1.0, 0.85, 0.3)
			var max_cd = data.get("cooldown_max", 3.2)
			stats_text = (tr("TOOLTIP_CD") % max_cd) + " • " + (tr("TOOLTIP_LV_FORMAT") % [data["level"], data["max_level"]])

		tooltip_badge.text = badge_text
		tooltip_badge.modulate = badge_color
		tooltip_stats.text = stats_text
		tooltip_desc.text = tr("CODEX_HEADER_TACTICAL") + "\n" + data["description"]
		var bio_text = data.get("biochemistry", "")
		tooltip_bio.text = tr("CODEX_HEADER_BIO") + "\n" + bio_text
		tooltip_bio.visible = (bio_text != "")
	else:
		tooltip_icon.text = "+"
		tooltip_title.text = tr("TOOLTIP_EMPTY_TITLE")
		if slot_idx >= 5:
			tooltip_badge.text = "[ " + tr("TOOLTIP_TAG_PASSIVE") + " ]"
			tooltip_badge.modulate = Color(0.75, 0.55, 1.0)
			tooltip_stats.text = "🧬 被动特质槽 (Passive Slot)"
		else:
			tooltip_badge.text = "[ " + tr("SKILL_EMPTY") + " ]"
			tooltip_badge.modulate = Color(0.6, 0.6, 0.6)
			tooltip_stats.text = tr("SKILL_BAR_TITLE")
		tooltip_desc.text = tr("TOOLTIP_EMPTY_DESC")
		tooltip_bio.text = ""
		tooltip_bio.visible = false

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("toggle_pause") or (event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE):
		if settings_modal and settings_modal.visible:
			settings_modal.close_settings()
			return
		if codex_modal.visible:
			codex_modal.close_codex()
			return
		toggle_pause()

func toggle_pause() -> void:
	var paused = not get_tree().paused
	get_tree().paused = paused
	pause_modal.visible = paused
	if not paused:
		codex_modal.visible = false
		if settings_modal:
			settings_modal.visible = false

func resume_game() -> void:
	get_tree().paused = false
	pause_modal.visible = false
	codex_modal.visible = false
	if settings_modal:
		settings_modal.visible = false

func connect_player(player: Node2D) -> void:
	player_ref = player
	if player.has_signal("stats_changed") and not player.stats_changed.is_connected(_on_player_stats_changed):
		player.stats_changed.connect(_on_player_stats_changed)
	if player.has_signal("burst_state_changed") and not player.burst_state_changed.is_connected(_on_player_burst_state_changed):
		player.burst_state_changed.connect(_on_player_burst_state_changed)
	if player.has_signal("pathogen_digested") and not player.pathogen_digested.is_connected(_on_pathogen_digested):
		player.pathogen_digested.connect(_on_pathogen_digested)

	_update_skill_slots()

func _on_player_stats_changed(health: float, max_health: float, satiety: float, max_satiety: float, radius_ratio: float) -> void:
	last_health = health
	last_max_health = max_health
	last_satiety = satiety
	last_max_satiety = max_satiety
	last_radius_ratio = radius_ratio

	hp_bar.max_value = max_health
	hp_bar.value = health
	hp_label.text = tr("HUD_HP") % [int(health), int(max_health)]

	atp_bar.max_value = max_satiety
	atp_bar.value = satiety
	atp_label.text = tr("HUD_ATP") % [int((satiety / max_satiety) * 100.0)]

	size_label.text = tr("HUD_SIZE") % radius_ratio

func _on_player_burst_state_changed(is_active: bool, time_left: float, max_time: float) -> void:
	burst_panel.visible = is_active
	last_burst_time_left = time_left
	if is_active:
		burst_bar.max_value = max_time
		burst_bar.value = time_left
		burst_label.text = tr("HUD_BURST_ALERT") % time_left

func _on_pathogen_digested(_enemy: Node2D, _atp: float) -> void:
	var player = get_tree().get_first_node_in_group("player")
	if player and "digested_count" in player:
		last_digested_count = player.digested_count
		count_label.text = tr("HUD_DIGESTED") % last_digested_count

func _on_player_level_up(_new_level: int) -> void:
	if upgrade_modal and is_instance_valid(upgrade_modal):
		upgrade_modal.open_upgrade_modal(player_ref)

func _setup_achievement_banner() -> void:
	achievement_banner = PanelContainer.new()
	achievement_banner.name = "AchievementBanner"
	achievement_banner.visible = false
	achievement_banner.mouse_filter = Control.MOUSE_FILTER_IGNORE
	achievement_banner.custom_minimum_size = Vector2(400, 60)
	achievement_banner.anchor_left = 0.5
	achievement_banner.anchor_right = 0.5
	achievement_banner.anchor_top = 0.0
	achievement_banner.anchor_bottom = 0.0
	achievement_banner.offset_left = -200.0
	achievement_banner.offset_right = 200.0
	achievement_banner.offset_top = 18.0
	achievement_banner.offset_bottom = 78.0

	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.07, 0.09, 0.15, 0.95)
	style.border_color = Color(1.0, 0.84, 0.28, 0.95)
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	style.content_margin_left = 16.0
	style.content_margin_right = 16.0
	style.content_margin_top = 8.0
	style.content_margin_bottom = 8.0
	achievement_banner.add_theme_stylebox_override("panel", style)

	var hbox = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 14)
	achievement_banner.add_child(hbox)

	ach_banner_icon = Label.new()
	ach_banner_icon.text = "🏆"
	ach_banner_icon.add_theme_font_size_override("font_size", 28)
	hbox.add_child(ach_banner_icon)

	var vbox = VBoxContainer.new()
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_child(vbox)

	ach_banner_title = Label.new()
	ach_banner_title.text = tr("TOAST_ACH_UNLOCKED")
	ach_banner_title.add_theme_font_size_override("font_size", 15)
	ach_banner_title.modulate = Color(1.0, 0.9, 0.3)
	vbox.add_child(ach_banner_title)

	ach_banner_desc = Label.new()
	ach_banner_desc.text = ""
	ach_banner_desc.add_theme_font_size_override("font_size", 12)
	ach_banner_desc.modulate = Color(0.9, 0.95, 1.0)
	vbox.add_child(ach_banner_desc)

	add_child(achievement_banner)

func _on_achievement_unlocked(_ach_id: String, ach_info: Dictionary) -> void:
	if achievement_banner == null:
		return

	ach_banner_icon.text = ach_info.get("icon", "🏆")
	ach_banner_title.text = tr("TOAST_ACH_UNLOCKED") + " " + ach_info.get("title", "")

	var sub_text = ach_info.get("desc", "")
	if ach_info.get("reward_cell", "") != "":
		var cell_info = GM.get_class_info(ach_info["reward_cell"])
		var c_name = cell_info.get("name", ach_info["reward_cell"])
		sub_text = tr("TOAST_CELL_UNLOCKED") % c_name
	elif ach_info.get("reward", "") != "":
		sub_text = ach_info["reward"]
	ach_banner_desc.text = sub_text

	achievement_banner.visible = true
	achievement_banner.modulate.a = 0.0
	achievement_banner.position.y = 0.0

	if ach_tween and ach_tween.is_valid():
		ach_tween.kill()
	ach_tween = create_tween()
	ach_tween.set_parallel(true)
	ach_tween.tween_property(achievement_banner, "modulate:a", 1.0, 0.35)
	ach_tween.tween_property(achievement_banner, "position:y", 18.0, 0.35).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	ach_tween.chain().tween_interval(3.2)
	ach_tween.chain().tween_property(achievement_banner, "modulate:a", 0.0, 0.5)
	ach_tween.chain().tween_callback(func(): achievement_banner.visible = false)

