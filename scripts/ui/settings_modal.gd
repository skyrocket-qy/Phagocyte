class_name SettingsModal
extends PanelContainer

const GM = preload("res://scripts/core/game_manager.gd")
const SettingsMgr = preload("res://scripts/core/settings_manager.gd")

signal closed()

@onready var title_label: Label = $VBox/Header/Title
@onready var close_btn: Button = $VBox/Header/CloseButton

## Tab Buttons
@onready var tab_controls_btn: Button = $VBox/TabBar/ControlsTab
@onready var tab_audio_btn: Button = $VBox/TabBar/AudioTab
@onready var tab_graphics_btn: Button = $VBox/TabBar/GraphicsTab

## Panels
@onready var controls_panel: VBoxContainer = $VBox/Content/ControlsPanel
@onready var audio_panel: VBoxContainer = $VBox/Content/AudioPanel
@onready var graphics_panel: VBoxContainer = $VBox/Content/GraphicsPanel

## Controls Tab Labels
@onready var controls_header_lbl: Label = $VBox/Content/ControlsPanel/HeaderLabel
@onready var ctrl_move_lbl: Label = $VBox/Content/ControlsPanel/MoveRow/DescLabel
@onready var ctrl_pause_lbl: Label = $VBox/Content/ControlsPanel/PauseRow/DescLabel
@onready var ctrl_phago_lbl: Label = $VBox/Content/ControlsPanel/PhagoRow/DescLabel
@onready var ctrl_burst_lbl: Label = $VBox/Content/ControlsPanel/BurstRow/DescLabel

## Audio Controls
@onready var master_slider: HSlider = $VBox/Content/AudioPanel/MasterRow/Slider
@onready var master_val_lbl: Label = $VBox/Content/AudioPanel/MasterRow/ValLabel
@onready var master_title_lbl: Label = $VBox/Content/AudioPanel/MasterRow/TitleLabel

@onready var sfx_slider: HSlider = $VBox/Content/AudioPanel/SFXRow/Slider
@onready var sfx_val_lbl: Label = $VBox/Content/AudioPanel/SFXRow/ValLabel
@onready var sfx_title_lbl: Label = $VBox/Content/AudioPanel/SFXRow/TitleLabel

@onready var bgm_slider: HSlider = $VBox/Content/AudioPanel/BGMRow/Slider
@onready var bgm_val_lbl: Label = $VBox/Content/AudioPanel/BGMRow/ValLabel
@onready var bgm_title_lbl: Label = $VBox/Content/AudioPanel/BGMRow/TitleLabel

## Graphics Controls
@onready var fullscreen_check: CheckBox = $VBox/Content/GraphicsPanel/FullscreenCheck
@onready var vsync_check: CheckBox = $VBox/Content/GraphicsPanel/VSyncCheck

var current_tab: int = 0

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false

	close_btn.pressed.connect(close_settings)

	tab_controls_btn.pressed.connect(func(): switch_tab(0))
	tab_audio_btn.pressed.connect(func(): switch_tab(1))
	tab_graphics_btn.pressed.connect(func(): switch_tab(2))

	# Audio signals
	master_slider.value_changed.connect(_on_master_slider_changed)
	sfx_slider.value_changed.connect(_on_sfx_slider_changed)
	bgm_slider.value_changed.connect(_on_bgm_slider_changed)

	# Graphics signals
	fullscreen_check.toggled.connect(_on_fullscreen_toggled)
	vsync_check.toggled.connect(_on_vsync_toggled)

	GM.add_language_listener(_on_language_changed)
	_sync_ui_from_settings()
	update_localized_texts()

func _exit_tree() -> void:
	GM.remove_language_listener(_on_language_changed)

func open_settings(target_tab: int = 0) -> void:
	_sync_ui_from_settings()
	visible = true
	switch_tab(target_tab)

func close_settings() -> void:
	visible = false
	closed.emit()

func switch_tab(tab_idx: int) -> void:
	current_tab = tab_idx

	tab_controls_btn.modulate = Color(1, 1, 1) if tab_idx == 0 else Color(0.7, 0.7, 0.7)
	tab_audio_btn.modulate = Color(1, 1, 1) if tab_idx == 1 else Color(0.7, 0.7, 0.7)
	tab_graphics_btn.modulate = Color(1, 1, 1) if tab_idx == 2 else Color(0.7, 0.7, 0.7)

	controls_panel.visible = (tab_idx == 0)
	audio_panel.visible = (tab_idx == 1)
	graphics_panel.visible = (tab_idx == 2)

func _sync_ui_from_settings() -> void:
	master_slider.value = SettingsMgr.master_volume * 100.0
	master_val_lbl.text = "%d%%" % int(master_slider.value)

	sfx_slider.value = SettingsMgr.sfx_volume * 100.0
	sfx_val_lbl.text = "%d%%" % int(sfx_slider.value)

	bgm_slider.value = SettingsMgr.bgm_volume * 100.0
	bgm_val_lbl.text = "%d%%" % int(bgm_slider.value)

	fullscreen_check.button_pressed = SettingsMgr.fullscreen
	vsync_check.button_pressed = SettingsMgr.vsync

func _on_master_slider_changed(val: float) -> void:
	master_val_lbl.text = "%d%%" % int(val)
	SettingsMgr.set_master_volume(val / 100.0)

func _on_sfx_slider_changed(val: float) -> void:
	sfx_val_lbl.text = "%d%%" % int(val)
	SettingsMgr.set_sfx_volume(val / 100.0)

func _on_bgm_slider_changed(val: float) -> void:
	bgm_val_lbl.text = "%d%%" % int(val)
	SettingsMgr.set_bgm_volume(val / 100.0)

func _on_fullscreen_toggled(toggled_on: bool) -> void:
	SettingsMgr.set_fullscreen(toggled_on)

func _on_vsync_toggled(toggled_on: bool) -> void:
	SettingsMgr.set_vsync(toggled_on)

func _on_language_changed(_locale: String) -> void:
	update_localized_texts()

func update_localized_texts() -> void:
	title_label.text = tr("SETTINGS_TITLE")
	close_btn.text = "✕"

	tab_controls_btn.text = tr("SETTINGS_TAB_CONTROLS")
	tab_audio_btn.text = tr("SETTINGS_TAB_AUDIO")
	tab_graphics_btn.text = tr("SETTINGS_TAB_GRAPHICS")

	controls_header_lbl.text = tr("CONTROLS_TITLE")
	ctrl_move_lbl.text = tr("CONTROLS_MOVE_DESC")
	ctrl_pause_lbl.text = tr("CONTROLS_PAUSE_DESC")
	ctrl_phago_lbl.text = tr("CONTROLS_PHAGO_DESC")
	ctrl_burst_lbl.text = tr("CONTROLS_BURST_DESC")

	master_title_lbl.text = tr("SETTINGS_MASTER_VOL")
	sfx_title_lbl.text = tr("SETTINGS_SFX_VOL")
	bgm_title_lbl.text = tr("SETTINGS_BGM_VOL")

	fullscreen_check.text = tr("SETTINGS_FULLSCREEN")
	vsync_check.text = tr("SETTINGS_VSYNC")
