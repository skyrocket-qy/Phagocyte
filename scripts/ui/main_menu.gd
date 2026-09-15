class_name MainMenu
extends Control

const GM = preload("res://scripts/core/game_manager.gd")

## Views
@onready var title_view: Control = $TitleView
@onready var class_view: Control = $ClassView
@onready var map_view: Control = $MapView
@onready var codex_modal: PanelContainer = $CodexModal

## Language Switcher
@onready var lang_btn: Button = $LangButton

## Title View Controls
@onready var title_lbl: Label = $TitleView/TitleLabel
@onready var subtitle_lbl: Label = $TitleView/SubtitleLabel
@onready var start_btn: Button = $TitleView/VBox/StartButton
@onready var codex_btn: Button = $TitleView/VBox/CodexButton
@onready var quit_btn: Button = $TitleView/VBox/QuitButton

## Codex Modal Controls
@onready var codex_title_lbl: Label = $CodexModal/VBox/Title
@onready var codex_content_lbl: Label = $CodexModal/VBox/Content
@onready var codex_close_btn: Button = $CodexModal/VBox/CloseButton

## Class View Controls
@onready var class_header_lbl: Label = $ClassView/HeaderLabel
@onready var class_list_container: VBoxContainer = $ClassView/HBox/ClassList
@onready var class_name_lbl: Label = $ClassView/HBox/DetailPanel/VBox/ClassNameLabel
@onready var class_role_lbl: Label = $ClassView/HBox/DetailPanel/VBox/ClassRoleLabel
@onready var class_trait_lbl: Label = $ClassView/HBox/DetailPanel/VBox/ClassTraitLabel
@onready var class_passive_lbl: Label = $ClassView/HBox/DetailPanel/VBox/ClassPassiveLabel
@onready var class_burst_lbl: Label = $ClassView/HBox/DetailPanel/VBox/ClassBurstLabel
@onready var class_status_lbl: Label = $ClassView/HBox/DetailPanel/VBox/ClassStatusLabel
@onready var class_confirm_btn: Button = $ClassView/Buttons/ConfirmButton
@onready var class_back_btn: Button = $ClassView/Buttons/BackButton

## Map View Controls
@onready var map_header_lbl: Label = $MapView/HeaderLabel
@onready var map_list_container: VBoxContainer = $MapView/HBox/MapList
@onready var map_name_lbl: Label = $MapView/HBox/DetailPanel/VBox/MapNameLabel
@onready var map_env_lbl: Label = $MapView/HBox/DetailPanel/VBox/MapEnvLabel
@onready var map_mech_lbl: Label = $MapView/HBox/DetailPanel/VBox/MapMechLabel
@onready var map_threat_lbl: Label = $MapView/HBox/DetailPanel/VBox/MapThreatLabel
@onready var deploy_btn: Button = $MapView/Buttons/DeployButton
@onready var map_back_btn: Button = $MapView/Buttons/BackButton

var active_class_key: String = "macrophage"
var active_map_key: String = "acute_wound"

func _ready() -> void:
	_switch_to_view(title_view)
	codex_modal.visible = false

	# Language toggle
	lang_btn.pressed.connect(_on_lang_toggle_pressed)
	GM.add_language_listener(_on_language_changed)

	# Title signals
	start_btn.pressed.connect(_on_start_pressed)
	codex_btn.pressed.connect(func(): codex_modal.visible = true)
	quit_btn.pressed.connect(func(): get_tree().quit())
	codex_close_btn.pressed.connect(func(): codex_modal.visible = false)

	# Class signals
	class_back_btn.pressed.connect(func(): _switch_to_view(title_view))
	class_confirm_btn.pressed.connect(_on_class_confirm_pressed)

	# Map signals
	map_back_btn.pressed.connect(func(): _switch_to_view(class_view))
	deploy_btn.pressed.connect(_on_deploy_pressed)

	_update_all_texts()

func _exit_tree() -> void:
	GM.remove_language_listener(_on_language_changed)

func _on_lang_toggle_pressed() -> void:
	GM.toggle_language()

func _on_language_changed(_locale: String) -> void:
	_update_all_texts()

func _update_all_texts() -> void:
	# Language button label indicates the alternate language to switch to
	lang_btn.text = "🌐 English" if GM.current_language == "zh_CN" else "🌐 简体中文"

	# Title View
	title_lbl.text = tr("TITLE_MAIN")
	subtitle_lbl.text = tr("SUBTITLE_MAIN")
	start_btn.text = tr("BTN_START")
	codex_btn.text = tr("BTN_CODEX")
	quit_btn.text = tr("BTN_QUIT")

	# Codex Modal
	codex_title_lbl.text = tr("CODEX_TITLE")
	codex_content_lbl.text = tr("CODEX_CONTENT")
	codex_close_btn.text = tr("BTN_CLOSE")

	# Class View
	class_header_lbl.text = tr("HEADER_SELECT_CLASS")
	class_back_btn.text = tr("BTN_BACK_TITLE")

	# Map View
	map_header_lbl.text = tr("HEADER_SELECT_MAP")
	map_back_btn.text = tr("BTN_BACK_CLASS")
	deploy_btn.text = tr("BTN_DEPLOY")

	_setup_class_buttons()
	_setup_map_buttons()
	_select_class(active_class_key)
	_select_map(active_map_key)

func _switch_to_view(target_view: Control) -> void:
	title_view.visible = (target_view == title_view)
	class_view.visible = (target_view == class_view)
	map_view.visible = (target_view == map_view)

func _setup_class_buttons() -> void:
	for child in class_list_container.get_children():
		child.queue_free()

	for key in GM.CLASS_DATA.keys():
		var data = GM.get_class_info(key)
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(280, 48)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT

		if data["unlocked"]:
			btn.text = " ✅ " + data["name"]
		else:
			btn.text = " 🔒 " + data["name"]

		btn.pressed.connect(func(): _select_class(key))
		class_list_container.add_child(btn)

func _select_class(key: String) -> void:
	active_class_key = key
	var data = GM.get_class_info(key)
	class_name_lbl.text = data["name"]
	class_role_lbl.text = tr("LABEL_ROLE") + data["role"]
	class_trait_lbl.text = tr("LABEL_TRAIT") + data["trait"]
	class_passive_lbl.text = tr("LABEL_PASSIVE") + data["passive"]
	class_burst_lbl.text = tr("LABEL_BURST") + data["burst"]

	if data["unlocked"]:
		class_status_lbl.text = tr("STATUS_UNLOCKED")
		class_status_lbl.modulate = Color(0.3, 1.0, 0.4)
		class_confirm_btn.disabled = false
		class_confirm_btn.text = tr("BTN_CONFIRM_CLASS")
	else:
		class_status_lbl.text = tr("STATUS_LOCKED")
		class_status_lbl.modulate = Color(0.9, 0.6, 0.2)
		class_confirm_btn.disabled = true
		class_confirm_btn.text = tr("BTN_CONFIRM_CLASS_LOCKED")

func _on_start_pressed() -> void:
	_switch_to_view(class_view)
	_select_class(active_class_key)

func _on_class_confirm_pressed() -> void:
	GM.selected_class = active_class_key
	_switch_to_view(map_view)
	_select_map(active_map_key)

func _setup_map_buttons() -> void:
	for child in map_list_container.get_children():
		child.queue_free()

	for key in GM.MAP_DATA.keys():
		var data = GM.get_map_info(key)
		var btn = Button.new()
		btn.custom_minimum_size = Vector2(280, 52)
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.text = " 🌐 " + data["name"]
		btn.pressed.connect(func(): _select_map(key))
		map_list_container.add_child(btn)

func _select_map(key: String) -> void:
	active_map_key = key
	var data = GM.get_map_info(key)
	map_name_lbl.text = data["name"]
	map_env_lbl.text = tr("LABEL_ENV") + data["environment"]
	map_mech_lbl.text = tr("LABEL_MECH") + data["mechanic"]
	map_threat_lbl.text = tr("LABEL_THREAT") + data["threat"]

func _on_deploy_pressed() -> void:
	GM.selected_map = active_map_key
	GM.start_game(get_tree())
