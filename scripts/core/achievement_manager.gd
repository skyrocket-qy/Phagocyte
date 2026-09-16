extends Node

## Manages game achievements, milestone tracking, persistent unlocks, and class rewards.

signal achievement_unlocked(ach_id: String, ach_data: Dictionary)

const SAVE_PATH: String = "user://achievements.json"
const GM = preload("res://scripts/core/game_manager.gd")

static var instance: Node = null
static var _listeners: Array[Callable] = []

const ACHIEVEMENTS: Dictionary = {
	"ach_first_digestion": {
		"id": "ach_first_digestion",
		"title_key": "ACH_FIRST_DIGESTION_TITLE",
		"desc_key": "ACH_FIRST_DIGESTION_DESC",
		"reward_key": "",
		"reward_cell": "",
		"icon": "🦠",
		"target_value": 1.0,
		"stat_key": "digested"
	},
	"ach_engulf_20": {
		"id": "ach_engulf_20",
		"title_key": "ACH_ENGULF_20_TITLE",
		"desc_key": "ACH_ENGULF_20_DESC",
		"reward_key": "ACH_ENGULF_20_REWARD",
		"reward_cell": "ctl",
		"icon": "⚡",
		"target_value": 20.0,
		"stat_key": "digested"
	},
	"ach_trigger_burst": {
		"id": "ach_trigger_burst",
		"title_key": "ACH_TRIGGER_BURST_TITLE",
		"desc_key": "ACH_TRIGGER_BURST_DESC",
		"reward_key": "ACH_TRIGGER_BURST_REWARD",
		"reward_cell": "neutrophil",
		"icon": "💥",
		"target_value": 1.0,
		"stat_key": "burst"
	},
	"ach_reach_level_5": {
		"id": "ach_reach_level_5",
		"title_key": "ACH_REACH_LEVEL_5_TITLE",
		"desc_key": "ACH_REACH_LEVEL_5_DESC",
		"reward_key": "ACH_REACH_LEVEL_5_REWARD",
		"reward_cell": "b_cell",
		"icon": "🏹",
		"target_value": 5.0,
		"stat_key": "level"
	},
	"ach_survive_180s": {
		"id": "ach_survive_180s",
		"title_key": "ACH_SURVIVE_180S_TITLE",
		"desc_key": "ACH_SURVIVE_180S_DESC",
		"reward_key": "ACH_SURVIVE_180S_REWARD",
		"reward_cell": "dendritic",
		"icon": "📍",
		"target_value": 180.0,
		"stat_key": "survival_time"
	},
	"ach_giant_volume": {
		"id": "ach_giant_volume",
		"title_key": "ACH_GIANT_VOLUME_TITLE",
		"desc_key": "ACH_GIANT_VOLUME_DESC",
		"reward_key": "",
		"reward_cell": "",
		"icon": "🌟",
		"target_value": 2.0,
		"stat_key": "radius_ratio"
	},
	"ach_full_arsenal": {
		"id": "ach_full_arsenal",
		"title_key": "ACH_FULL_ARSENAL_TITLE",
		"desc_key": "ACH_FULL_ARSENAL_DESC",
		"reward_key": "",
		"reward_cell": "",
		"icon": "🛡️",
		"target_value": 3.0,
		"stat_key": "active_skills"
	}
}

static var unlocked_ids: Dictionary = {}
static var progress_data: Dictionary = {
	"digested": 0.0,
	"burst": 0.0,
	"level": 1.0,
	"survival_time": 0.0,
	"radius_ratio": 1.0,
	"active_skills": 1.0
}

func _init() -> void:
	instance = self

func _ready() -> void:
	instance = self
	load_from_disk()

static func add_unlock_listener(callback: Callable) -> void:
	if not _listeners.has(callback):
		_listeners.append(callback)

static func remove_unlock_listener(callback: Callable) -> void:
	_listeners.erase(callback)

## Check if an achievement is unlocked
static func is_unlocked(ach_id: String) -> bool:
	return unlocked_ids.has(ach_id) and unlocked_ids[ach_id] == true

## Manually or systematically unlock an achievement
static func unlock(ach_id: String) -> bool:
	if not ACHIEVEMENTS.has(ach_id):
		return false
	if is_unlocked(ach_id):
		return false

	unlocked_ids[ach_id] = true
	var data = ACHIEVEMENTS[ach_id]

	# Unlock rewarding immune cell if applicable
	var reward_c = data.get("reward_cell", "")
	if reward_c != "":
		GM.unlock_class(reward_c)

	save_to_disk()

	var info = get_achievement_info(ach_id)
	if instance != null and is_instance_valid(instance):
		instance.achievement_unlocked.emit(ach_id, info)
	for cb in _listeners:
		if cb.is_valid():
			cb.call(ach_id, info)

	return true

## Record runtime in-game events and evaluate completion conditions
static func record_event(event_name: String, value: Variant = null) -> void:
	match event_name:
		"pathogen_digested":
			var count: float = float(value) if value != null else (progress_data.get("digested", 0.0) + 1.0)
			progress_data["digested"] = maxf(progress_data.get("digested", 0.0), count)
			if progress_data["digested"] >= 1.0:
				unlock("ach_first_digestion")
			if progress_data["digested"] >= 20.0:
				unlock("ach_engulf_20")

		"burst_activated":
			progress_data["burst"] = 1.0
			unlock("ach_trigger_burst")

		"level_up":
			var lvl: float = float(value) if value != null else 1.0
			progress_data["level"] = maxf(progress_data.get("level", 1.0), lvl)
			if progress_data["level"] >= 5.0:
				unlock("ach_reach_level_5")

		"survival_time":
			var st: float = float(value) if value != null else 0.0
			progress_data["survival_time"] = maxf(progress_data.get("survival_time", 0.0), st)
			if progress_data["survival_time"] >= 180.0:
				unlock("ach_survive_180s")

		"radius_ratio":
			var rr: float = float(value) if value != null else 1.0
			progress_data["radius_ratio"] = maxf(progress_data.get("radius_ratio", 1.0), rr)
			if progress_data["radius_ratio"] >= 2.0:
				unlock("ach_giant_volume")

		"active_skills_count":
			var cnt: float = float(value) if value != null else 1.0
			progress_data["active_skills"] = maxf(progress_data.get("active_skills", 1.0), cnt)
			if progress_data["active_skills"] >= 3.0:
				unlock("ach_full_arsenal")

## Get localized information for a single achievement
static func get_achievement_info(ach_id: String) -> Dictionary:
	if not ACHIEVEMENTS.has(ach_id):
		return {}

	var raw = ACHIEVEMENTS[ach_id]
	var unlocked = is_unlocked(ach_id)
	var stat_key = raw.get("stat_key", "")
	var current_val = progress_data.get(stat_key, 0.0)
	var target_val = raw.get("target_value", 1.0)

	var title = TranslationServer.translate(raw["title_key"])
	var desc = TranslationServer.translate(raw["desc_key"])
	var reward_text = TranslationServer.translate(raw["reward_key"]) if raw.get("reward_key", "") != "" else ""

	return {
		"id": ach_id,
		"title": title,
		"desc": desc,
		"reward": reward_text,
		"reward_cell": raw.get("reward_cell", ""),
		"icon": raw.get("icon", "🏆"),
		"unlocked": unlocked,
		"current_value": current_val,
		"target_value": target_val,
		"progress_ratio": clampf(current_val / target_val if target_val > 0.0 else 1.0, 0.0, 1.0)
	}

## Get all achievements formatted for UI display
static func get_all_achievements() -> Array[Dictionary]:
	var list: Array[Dictionary] = []
	for key in ACHIEVEMENTS.keys():
		list.append(get_achievement_info(key))
	return list

## Helper: Get unlock condition text for a locked immune cell
static func get_cell_unlock_requirement_text(class_id: String) -> String:
	for ach_id in ACHIEVEMENTS.keys():
		var ach = ACHIEVEMENTS[ach_id]
		if ach.get("reward_cell", "") == class_id:
			var title = TranslationServer.translate(ach["title_key"])
			var desc = TranslationServer.translate(ach["desc_key"])
			return "%s🏆 %s (%s)" % [TranslationServer.translate("LABEL_UNLOCK_REQ"), title, desc]
	return TranslationServer.translate("STATUS_LOCKED")

## Save achievement state to disk
static func save_to_disk() -> void:
	var payload = {
		"unlocked_ids": unlocked_ids,
		"progress_data": progress_data
	}
	var file = FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(payload, "\t"))
		file.close()

## Load achievement state from disk
static func load_from_disk() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		_sync_unlocked_classes()
		return

	var file = FileAccess.open(SAVE_PATH, FileAccess.READ)
	if not file:
		_sync_unlocked_classes()
		return

	var json_str = file.get_as_text()
	file.close()

	var json = JSON.new()
	var err = json.parse(json_str)
	if err == OK and json.data is Dictionary:
		var data = json.data
		if data.has("unlocked_ids") and data["unlocked_ids"] is Dictionary:
			unlocked_ids = data["unlocked_ids"]
		if data.has("progress_data") and data["progress_data"] is Dictionary:
			for k in data["progress_data"].keys():
				progress_data[k] = float(data["progress_data"][k])

	_sync_unlocked_classes()

## Synchronize unlocked classes in GameManager based on unlocked achievements
static func _sync_unlocked_classes() -> void:
	GM.unlock_class("macrophage")

	for ach_id in ACHIEVEMENTS.keys():
		var raw = ACHIEVEMENTS[ach_id]
		var reward_cell = raw.get("reward_cell", "")
		if reward_cell != "":
			if is_unlocked(ach_id):
				GM.unlock_class(reward_cell)
			else:
				GM.lock_class(reward_cell)

## Reset all progress and locked state (used for testing or clean restarts)
static func reset_all() -> void:
	unlocked_ids.clear()
	progress_data = {
		"digested": 0.0,
		"burst": 0.0,
		"level": 1.0,
		"survival_time": 0.0,
		"radius_ratio": 1.0,
		"active_skills": 1.0
	}
	_sync_unlocked_classes()
	if FileAccess.file_exists(SAVE_PATH):
		DirAccess.remove_absolute(SAVE_PATH)
