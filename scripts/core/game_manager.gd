extends Node

## Runtime Player Selections
static var selected_class: String = "macrophage"
static var selected_map: String = "acute_wound"
static var current_language: String = "zh_CN"

signal language_changed(locale: String)

## Static callback list for decoupled notification
static var _language_listeners: Array[Callable] = []

## Class Metadata referencing translation keys
const CLASS_DATA = {
	"macrophage": {
		"name_key": "CLASS_MACROPHAGE_NAME",
		"role_key": "CLASS_MACROPHAGE_ROLE",
		"trait_key": "CLASS_MACROPHAGE_TRAIT",
		"passive_key": "CLASS_MACROPHAGE_PASSIVE",
		"burst_key": "CLASS_MACROPHAGE_BURST",
		"unlocked": true
	},
	"ctl": {
		"name_key": "CLASS_CTL_NAME",
		"role_key": "CLASS_CTL_ROLE",
		"trait_key": "CLASS_CTL_TRAIT",
		"passive_key": "CLASS_CTL_PASSIVE",
		"burst_key": "CLASS_CTL_BURST",
		"unlocked": false
	},
	"neutrophil": {
		"name_key": "CLASS_NEUTROPHIL_NAME",
		"role_key": "CLASS_NEUTROPHIL_ROLE",
		"trait_key": "CLASS_NEUTROPHIL_TRAIT",
		"passive_key": "CLASS_NEUTROPHIL_PASSIVE",
		"burst_key": "CLASS_NEUTROPHIL_BURST",
		"unlocked": false
	},
	"b_cell": {
		"name_key": "CLASS_B_CELL_NAME",
		"role_key": "CLASS_B_CELL_ROLE",
		"trait_key": "CLASS_B_CELL_TRAIT",
		"passive_key": "CLASS_B_CELL_PASSIVE",
		"burst_key": "CLASS_B_CELL_BURST",
		"unlocked": false
	},
	"dendritic": {
		"name_key": "CLASS_DENDRITIC_NAME",
		"role_key": "CLASS_DENDRITIC_ROLE",
		"trait_key": "CLASS_DENDRITIC_TRAIT",
		"passive_key": "CLASS_DENDRITIC_PASSIVE",
		"burst_key": "CLASS_DENDRITIC_BURST",
		"unlocked": false
	}
}

## Map Metadata referencing translation keys
const MAP_DATA = {
	"acute_wound": {
		"name_key": "MAP_WOUND_NAME",
		"env_key": "MAP_WOUND_ENV",
		"mech_key": "MAP_WOUND_MECH",
		"threat_key": "MAP_WOUND_THREAT",
		"bg_color": Color(0.05, 0.08, 0.12, 1.0),
		"unlocked": true
	},
	"alveolar_space": {
		"name_key": "MAP_ALVEOLAR_NAME",
		"env_key": "MAP_ALVEOLAR_ENV",
		"mech_key": "MAP_ALVEOLAR_MECH",
		"threat_key": "MAP_ALVEOLAR_THREAT",
		"bg_color": Color(0.04, 0.11, 0.13, 1.0),
		"unlocked": true
	}
}

func _ready() -> void:
	# Initialize locale
	set_language(current_language)

static func add_language_listener(callback: Callable) -> void:
	if not _language_listeners.has(callback):
		_language_listeners.append(callback)

static func remove_language_listener(callback: Callable) -> void:
	_language_listeners.erase(callback)

static func set_language(locale: String) -> void:
	current_language = locale
	TranslationServer.set_locale(locale)
	# Notify all registered listeners
	for cb in _language_listeners:
		if cb.is_valid():
			cb.call(locale)

static func toggle_language() -> String:
	var next_lang = "en" if current_language == "zh_CN" else "zh_CN"
	set_language(next_lang)
	return next_lang

static func get_class_info(key: String) -> Dictionary:
	if not CLASS_DATA.has(key):
		return {}
	var d = CLASS_DATA[key]
	return {
		"name": TranslationServer.translate(d["name_key"]),
		"role": TranslationServer.translate(d["role_key"]),
		"trait": TranslationServer.translate(d["trait_key"]),
		"passive": TranslationServer.translate(d["passive_key"]),
		"burst": TranslationServer.translate(d["burst_key"]),
		"unlocked": d["unlocked"]
	}

static func get_map_info(key: String) -> Dictionary:
	if not MAP_DATA.has(key):
		return {}
	var d = MAP_DATA[key]
	return {
		"name": TranslationServer.translate(d["name_key"]),
		"environment": TranslationServer.translate(d["env_key"]),
		"mechanic": TranslationServer.translate(d["mech_key"]),
		"threat": TranslationServer.translate(d["threat_key"]),
		"bg_color": d["bg_color"],
		"unlocked": d["unlocked"]
	}

static func start_game(tree: SceneTree) -> void:
	tree.paused = false
	tree.change_scene_to_file("res://scenes/main.tscn")

static func go_to_menu(tree: SceneTree) -> void:
	tree.paused = false
	tree.change_scene_to_file("res://scenes/ui/main_menu.tscn")

static func restart_game(tree: SceneTree) -> void:
	tree.paused = false
	tree.reload_current_scene()
