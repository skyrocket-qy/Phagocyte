class_name HUD
extends CanvasLayer

const GM = preload("res://scripts/core/game_manager.gd")

@onready var title_label: Label = $MarginContainer/PanelContainer/VBoxContainer/TitleLabel
@onready var map_label: Label = $MarginContainer/PanelContainer/VBoxContainer/MapLabel
@onready var hp_bar: ProgressBar = $MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPBar
@onready var hp_label: Label = $MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPLabel
@onready var atp_bar: ProgressBar = $MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPBar
@onready var atp_label: Label = $MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPLabel
@onready var size_label: Label = $MarginContainer/PanelContainer/VBoxContainer/SizeLabel
@onready var count_label: Label = $MarginContainer/PanelContainer/VBoxContainer/CountLabel
@onready var help_label: Label = $HelpContainer/HelpPanel/HelpLabel

@onready var burst_panel: PanelContainer = $BurstContainer
@onready var burst_label: Label = $BurstContainer/VBox/BurstLabel
@onready var burst_bar: ProgressBar = $BurstContainer/VBox/BurstBar

@onready var pause_modal: PanelContainer = $PauseModal
@onready var pause_title: Label = $PauseModal/VBox/Title
@onready var resume_btn: Button = $PauseModal/VBox/ResumeButton
@onready var restart_btn: Button = $PauseModal/VBox/RestartButton
@onready var menu_btn: Button = $PauseModal/VBox/MenuButton

## Skill Bar Nodes
@onready var skill_title_lbl: Label = $SkillContainer/VBox/TitleLabel
@onready var slots_container: HBoxContainer = $SkillContainer/VBox/SlotsContainer

# Cached last stats for re-rendering upon language change
var last_health: float = 100.0
var last_max_health: float = 100.0
var last_satiety: float = 0.0
var last_max_satiety: float = 100.0
var last_radius_ratio: float = 1.0
var last_digested_count: int = 0
var last_burst_time_left: float = 0.0

var player_ref: Node2D = null

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	burst_panel.visible = false
	pause_modal.visible = false

	if not resume_btn.pressed.is_connected(resume_game):
		resume_btn.pressed.connect(resume_game)
	if not restart_btn.pressed.is_connected(_on_restart_pressed):
		restart_btn.pressed.connect(_on_restart_pressed)
	if not menu_btn.pressed.is_connected(_on_menu_pressed):
		menu_btn.pressed.connect(_on_menu_pressed)

	GM.add_language_listener(_on_language_changed)
	_update_localized_texts()

func _exit_tree() -> void:
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
	help_label.text = tr("HUD_GUIDE")
	skill_title_lbl.text = tr("SKILL_BAR_TITLE")

	if burst_panel.visible:
		burst_label.text = tr("HUD_BURST_ALERT") % last_burst_time_left

	pause_title.text = tr("PAUSE_TITLE")
	resume_btn.text = tr("PAUSE_RESUME")
	restart_btn.text = tr("PAUSE_RESTART")
	menu_btn.text = tr("PAUSE_MENU")

	_update_skill_slots()

func _update_skill_slots() -> void:
	if not player_ref or not is_instance_valid(player_ref):
		player_ref = get_tree().get_first_node_in_group("player")
		if not player_ref:
			return

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
			# Active or Innate Skill
			if icon_lbl:
				icon_lbl.text = data["icon"]
			if badge_lbl:
				if data["is_innate"]:
					badge_lbl.text = tr("SKILL_INNATE_TAG")
					badge_lbl.modulate = Color(0.4, 0.95, 0.8)
				else:
					badge_lbl.text = tr("SKILL_LV") % data["level"]
					badge_lbl.modulate = Color(1.0, 0.9, 0.3)
			if cd_overlay:
				cd_overlay.visible = data["cooldown_ratio"] > 0.0
				cd_overlay.value = data["cooldown_ratio"]
		else:
			# Empty Slot
			if icon_lbl:
				icon_lbl.text = "+"
			if badge_lbl:
				badge_lbl.text = ""
			if cd_overlay:
				cd_overlay.visible = false

func _on_restart_pressed() -> void:
	GM.restart_game(get_tree())

func _on_menu_pressed() -> void:
	GM.go_to_menu(get_tree())

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("toggle_pause") or (event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE):
		toggle_pause()

func toggle_pause() -> void:
	var paused = not get_tree().paused
	get_tree().paused = paused
	pause_modal.visible = paused

func resume_game() -> void:
	get_tree().paused = false
	pause_modal.visible = false

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
