extends Node

## Manages audio volume, graphics display modes, and persistent configuration.

signal settings_changed()

const SAVE_PATH: String = "user://settings.json"

static var instance: Node = null

static var master_volume: float = 1.0
static var sfx_volume: float = 1.0
static var bgm_volume: float = 0.8
static var fullscreen: bool = false
static var vsync: bool = true

func _init() -> void:
	instance = self

func _ready() -> void:
	instance = self
	load_from_disk()
	apply_settings()

## Apply runtime audio and display server settings
static func apply_settings() -> void:
	# Audio Bus volumes if buses exist
	if AudioServer.get_bus_count() > 0:
		var master_idx = AudioServer.get_bus_index("Master")
		if master_idx >= 0:
			var db = linear_to_db(maxf(0.0001, master_volume)) if master_volume > 0.0 else -80.0
			AudioServer.set_bus_volume_db(master_idx, db)

	# Window display mode
	if DisplayServer.has_feature(DisplayServer.FEATURE_SUBWINDOWS):
		var mode = DisplayServer.WINDOW_MODE_FULLSCREEN if fullscreen else DisplayServer.WINDOW_MODE_WINDOWED
		DisplayServer.window_set_mode(mode)

	# VSync mode
	var vsync_mode = DisplayServer.VSYNC_ENABLED if vsync else DisplayServer.VSYNC_DISABLED
	DisplayServer.window_set_vsync_mode(vsync_mode)

	if instance != null and is_instance_valid(instance):
		instance.settings_changed.emit()

static func set_master_volume(val: float) -> void:
	master_volume = clampf(val, 0.0, 1.0)
	apply_settings()
	save_to_disk()

static func set_sfx_volume(val: float) -> void:
	sfx_volume = clampf(val, 0.0, 1.0)
	apply_settings()
	save_to_disk()

static func set_bgm_volume(val: float) -> void:
	bgm_volume = clampf(val, 0.0, 1.0)
	apply_settings()
	save_to_disk()

static func set_fullscreen(enabled: bool) -> void:
	fullscreen = enabled
	apply_settings()
	save_to_disk()

static func set_vsync(enabled: bool) -> void:
	vsync = enabled
	apply_settings()
	save_to_disk()

static func save_to_disk() -> void:
	var payload = {
		"master_volume": master_volume,
		"sfx_volume": sfx_volume,
		"bgm_volume": bgm_volume,
		"fullscreen": fullscreen,
		"vsync": vsync
	}
	var file = FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(payload, "\t"))
		file.close()

static func load_from_disk() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		return

	var file = FileAccess.open(SAVE_PATH, FileAccess.READ)
	if not file:
		return

	var text = file.get_as_text()
	file.close()

	var json = JSON.new()
	if json.parse(text) == OK and json.data is Dictionary:
		var d = json.data
		master_volume = float(d.get("master_volume", 1.0))
		sfx_volume = float(d.get("sfx_volume", 1.0))
		bgm_volume = float(d.get("bgm_volume", 0.8))
		fullscreen = bool(d.get("fullscreen", false))
		vsync = bool(d.get("vsync", true))

static func reset_defaults() -> void:
	master_volume = 1.0
	sfx_volume = 1.0
	bgm_volume = 0.8
	fullscreen = false
	vsync = true
	apply_settings()
	if FileAccess.file_exists(SAVE_PATH):
		DirAccess.remove_absolute(SAVE_PATH)
