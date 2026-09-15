class_name HUD
extends CanvasLayer

const GM = preload("res://scripts/core/game_manager.gd")

@onready var hp_bar: ProgressBar = $MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPBar
@onready var hp_label: Label = $MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPLabel
@onready var atp_bar: ProgressBar = $MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPBar
@onready var atp_label: Label = $MarginContainer/PanelContainer/VBoxContainer/ATPContainer/ATPLabel
@onready var size_label: Label = $MarginContainer/PanelContainer/VBoxContainer/SizeLabel
@onready var count_label: Label = $MarginContainer/PanelContainer/VBoxContainer/CountLabel
@onready var map_label: Label = $MarginContainer/PanelContainer/VBoxContainer/MapLabel

@onready var burst_panel: PanelContainer = $BurstContainer
@onready var burst_label: Label = $BurstContainer/VBox/BurstLabel
@onready var burst_bar: ProgressBar = $BurstContainer/VBox/BurstBar

@onready var pause_modal: PanelContainer = $PauseModal
@onready var resume_btn: Button = $PauseModal/VBox/ResumeButton
@onready var restart_btn: Button = $PauseModal/VBox/RestartButton
@onready var menu_btn: Button = $PauseModal/VBox/MenuButton

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

	# Display map info
	if GM.selected_map in GM.MAP_DATA:
		map_label.text = "戰場：" + GM.MAP_DATA[GM.selected_map]["name"]

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
	if player.has_signal("stats_changed") and not player.stats_changed.is_connected(_on_player_stats_changed):
		player.stats_changed.connect(_on_player_stats_changed)
	if player.has_signal("burst_state_changed") and not player.burst_state_changed.is_connected(_on_player_burst_state_changed):
		player.burst_state_changed.connect(_on_player_burst_state_changed)
	if player.has_signal("pathogen_digested") and not player.pathogen_digested.is_connected(_on_pathogen_digested):
		player.pathogen_digested.connect(_on_pathogen_digested)

func _on_player_stats_changed(health: float, max_health: float, satiety: float, max_satiety: float, radius_ratio: float) -> void:
	hp_bar.max_value = max_health
	hp_bar.value = health
	hp_label.text = "生命值 (HP): %d / %d" % [int(health), int(max_health)]

	atp_bar.max_value = max_satiety
	atp_bar.value = satiety
	atp_label.text = "飽食能量 (ATP): %d%%" % [int((satiety / max_satiety) * 100.0)]

	size_label.text = "細胞半徑擴展: %.1fx" % radius_ratio

func _on_player_burst_state_changed(is_active: bool, time_left: float, max_time: float) -> void:
	burst_panel.visible = is_active
	if is_active:
		burst_bar.max_value = max_time
		burst_bar.value = time_left
		burst_label.text = "⚡ 呼吸爆發 (Respiratory Burst) 剩餘 %.1fs ⚡\n移速 +150%% | 酸性腐蝕力場生效中" % time_left

func _on_pathogen_digested(_enemy: Node2D, _atp: float) -> void:
	var player = get_tree().get_first_node_in_group("player")
	if player and "digested_count" in player:
		count_label.text = "已吞噬病原體: %d" % player.digested_count
