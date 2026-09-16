extends Node

## Runtime Player Selections
static var selected_class: String = "macrophage"
static var selected_map: String = "acute_wound"
static var current_language: String = "zh_CN"

signal language_changed(locale: String)

## Static callback list for decoupled notification
static var _language_listeners: Array[Callable] = []

const CELL_SCENES = {
	"macrophage": preload("res://scenes/characters/macrophage.tscn"),
	"ctl": preload("res://scenes/characters/ctl_cell.tscn"),
	"neutrophil": preload("res://scenes/characters/neutrophil_cell.tscn"),
	"b_cell": preload("res://scenes/characters/b_cell.tscn"),
	"dendritic": preload("res://scenes/characters/dendritic_cell.tscn"),
}

static func get_cell_scene(class_id: String) -> PackedScene:
	if CELL_SCENES.has(class_id):
		return CELL_SCENES[class_id]
	return CELL_SCENES["macrophage"]

## Class Metadata referencing translation keys
static var CLASS_DATA: Dictionary = {
	"macrophage": {
		"name_key": "CLASS_MACROPHAGE_NAME",
		"role_key": "CLASS_MACROPHAGE_ROLE",
		"trait_key": "CLASS_MACROPHAGE_TRAIT",
		"passive_key": "CLASS_MACROPHAGE_PASSIVE",
		"burst_key": "CLASS_MACROPHAGE_BURST",
		"unlocked": true,
		"unlock_achievement": ""
	},
	"ctl": {
		"name_key": "CLASS_CTL_NAME",
		"role_key": "CLASS_CTL_ROLE",
		"trait_key": "CLASS_CTL_TRAIT",
		"passive_key": "CLASS_CTL_PASSIVE",
		"burst_key": "CLASS_CTL_BURST",
		"unlocked": false,
		"unlock_achievement": "ach_engulf_20"
	},
	"neutrophil": {
		"name_key": "CLASS_NEUTROPHIL_NAME",
		"role_key": "CLASS_NEUTROPHIL_ROLE",
		"trait_key": "CLASS_NEUTROPHIL_TRAIT",
		"passive_key": "CLASS_NEUTROPHIL_PASSIVE",
		"burst_key": "CLASS_NEUTROPHIL_BURST",
		"unlocked": false,
		"unlock_achievement": "ach_trigger_burst"
	},
	"b_cell": {
		"name_key": "CLASS_B_CELL_NAME",
		"role_key": "CLASS_B_CELL_ROLE",
		"trait_key": "CLASS_B_CELL_TRAIT",
		"passive_key": "CLASS_B_CELL_PASSIVE",
		"burst_key": "CLASS_B_CELL_BURST",
		"unlocked": false,
		"unlock_achievement": "ach_reach_level_5"
	},
	"dendritic": {
		"name_key": "CLASS_DENDRITIC_NAME",
		"role_key": "CLASS_DENDRITIC_ROLE",
		"trait_key": "CLASS_DENDRITIC_TRAIT",
		"passive_key": "CLASS_DENDRITIC_PASSIVE",
		"burst_key": "CLASS_DENDRITIC_BURST",
		"unlocked": false,
		"unlock_achievement": "ach_survive_180s"
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

## Skill Catalog for Manual and Tooltips
const SKILL_CATALOG = {
	"macrophage_pseudopods": {
		"id": "macrophage_pseudopods",
		"name_key": "SKILL_DEFORM_NAME",
		"desc_key": "SKILL_DEFORM_DESC",
		"bio_key": "SKILL_DEFORM_BIO",
		"icon": "🦠",
		"type": "innate",
		"class_id": "macrophage",
		"cooldown": 0.0,
		"max_level": 5
	},
	"ros_torrent": {
		"id": "ros_torrent",
		"name_key": "SKILL_ROS_NAME",
		"desc_key": "SKILL_ROS_DESC",
		"bio_key": "SKILL_ROS_BIO",
		"icon": "💨",
		"type": "active",
		"class_id": "",
		"cooldown": 3.2,
		"max_level": 5
	},
	"complement_cascade": {
		"id": "complement_cascade",
		"name_key": "SKILL_COMPLEMENT_NAME",
		"desc_key": "SKILL_COMPLEMENT_DESC",
		"bio_key": "SKILL_COMPLEMENT_BIO",
		"icon": "💥",
		"type": "active",
		"class_id": "",
		"cooldown": 5.0,
		"max_level": 5
	},
	"lysosomal_overload": {
		"id": "lysosomal_overload",
		"name_key": "SKILL_LYSOSOME_NAME",
		"desc_key": "SKILL_LYSOSOME_DESC",
		"bio_key": "SKILL_LYSOSOME_BIO",
		"icon": "🧪",
		"type": "active",
		"class_id": "",
		"cooldown": 4.5,
		"max_level": 5
	},
	"interferon_pulse": {
		"id": "interferon_pulse",
		"name_key": "SKILL_INTERFERON_NAME",
		"desc_key": "SKILL_INTERFERON_DESC",
		"bio_key": "SKILL_INTERFERON_BIO",
		"icon": "📡",
		"type": "active",
		"class_id": "",
		"cooldown": 6.0,
		"max_level": 5
	},
	"perforin_injection": {
		"id": "perforin_injection",
		"name_key": "SKILL_PERFORIN_NAME",
		"desc_key": "SKILL_PERFORIN_DESC",
		"bio_key": "SKILL_PERFORIN_BIO",
		"icon": "🗡️",
		"type": "innate",
		"class_id": "ctl",
		"cooldown": 0.0,
		"max_level": 5
	},
	"net_trap": {
		"id": "net_trap",
		"name_key": "SKILL_NET_NAME",
		"desc_key": "SKILL_NET_DESC",
		"bio_key": "SKILL_NET_BIO",
		"icon": "🕸️",
		"type": "innate",
		"class_id": "neutrophil",
		"cooldown": 8.0,
		"max_level": 5
	},
	"phagocytic_instinct": {
		"id": "phagocytic_instinct",
		"name_key": "SKILL_INSTINCT_NAME",
		"desc_key": "SKILL_INSTINCT_DESC",
		"bio_key": "SKILL_INSTINCT_BIO",
		"icon": "🩸",
		"type": "passive",
		"class_id": "",
		"cooldown": 0.0,
		"max_level": 5
	},
	"chemotaxis_guidance": {
		"id": "chemotaxis_guidance",
		"name_key": "SKILL_CHEMOTAXIS_NAME",
		"desc_key": "SKILL_CHEMOTAXIS_DESC",
		"bio_key": "SKILL_CHEMOTAXIS_BIO",
		"icon": "🧭",
		"type": "passive",
		"class_id": "",
		"cooldown": 0.0,
		"max_level": 5
	}
}

## Pathogen Catalog for Codex
const PATHOGEN_CATALOG = {
	"staph": {
		"id": "staph",
		"name_key": "PATHOGEN_STAPH_NAME",
		"desc_key": "PATHOGEN_STAPH_DESC",
		"trait_key": "PATHOGEN_STAPH_TRAIT",
		"icon": "🧫",
		"danger_level": "★☆☆"
	},
	"s_virus": {
		"id": "s_virus",
		"name_key": "PATHOGEN_SVIRUS_NAME",
		"desc_key": "PATHOGEN_SVIRUS_DESC",
		"trait_key": "PATHOGEN_SVIRUS_TRAIT",
		"icon": "🦠",
		"danger_level": "★★☆"
	},
	"flu_drift": {
		"id": "flu_drift",
		"name_key": "PATHOGEN_FLUDRIFT_NAME",
		"desc_key": "PATHOGEN_FLUDRIFT_DESC",
		"trait_key": "PATHOGEN_FLUDRIFT_TRAIT",
		"icon": "🧬",
		"danger_level": "★★★"
	},
	"malignant_cell": {
		"id": "malignant_cell",
		"name_key": "PATHOGEN_MALIGNANT_NAME",
		"desc_key": "PATHOGEN_MALIGNANT_DESC",
		"trait_key": "PATHOGEN_MALIGNANT_TRAIT",
		"icon": "☣️",
		"danger_level": "★★★★"
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

static func unlock_class(key: String) -> void:
	if CLASS_DATA.has(key):
		CLASS_DATA[key]["unlocked"] = true

static func lock_class(key: String) -> void:
	if CLASS_DATA.has(key) and key != "macrophage":
		CLASS_DATA[key]["unlocked"] = false

static func is_class_unlocked(key: String) -> bool:
	if CLASS_DATA.has(key):
		return CLASS_DATA[key]["unlocked"]
	return false

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
		"unlocked": d["unlocked"],
		"unlock_achievement": d.get("unlock_achievement", "")
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

static func get_skill_info(key: String) -> Dictionary:
	if not SKILL_CATALOG.has(key):
		return {}
	var d = SKILL_CATALOG[key]
	var type_label = ""
	match d["type"]:
		"innate":
			type_label = TranslationServer.translate("TOOLTIP_TAG_INNATE")
		"active":
			type_label = TranslationServer.translate("TOOLTIP_TAG_ACTIVE")
		"passive":
			type_label = TranslationServer.translate("TOOLTIP_TAG_PASSIVE")
	return {
		"id": d["id"],
		"name": TranslationServer.translate(d["name_key"]),
		"description": TranslationServer.translate(d["desc_key"]),
		"biochemistry": TranslationServer.translate(d["bio_key"]),
		"icon": d["icon"],
		"type": d["type"],
		"type_label": type_label,
		"cooldown": d["cooldown"],
		"max_level": d["max_level"]
	}

static func get_pathogen_info(key: String) -> Dictionary:
	if not PATHOGEN_CATALOG.has(key):
		return {}
	var d = PATHOGEN_CATALOG[key]
	return {
		"id": d["id"],
		"name": TranslationServer.translate(d["name_key"]),
		"description": TranslationServer.translate(d["desc_key"]),
		"trait": TranslationServer.translate(d["trait_key"]),
		"icon": d["icon"],
		"danger_level": d["danger_level"]
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
