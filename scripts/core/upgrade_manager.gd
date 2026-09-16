class_name UpgradeManager
extends RefCounted

## Manages Level-Up 3-Choice generation from Active Cytokines and Passive Organelles.

const ROSTorrentClass = preload("res://scripts/skills/ros_torrent_skill.gd")
const PerforinLanceClass = preload("res://scripts/skills/perforin_lance_skill.gd")
const ComplementCascadeClass = preload("res://scripts/skills/complement_cascade_skill.gd")
const AntibodySalvoClass = preload("res://scripts/skills/antibody_salvo_skill.gd")
const PseudopodLungeClass = preload("res://scripts/skills/pseudopod_lunge_skill.gd")

const PassiveActinClass = preload("res://scripts/skills/passive_actin_polymerization.gd")
const PassiveLysosomeClass = preload("res://scripts/skills/passive_lysosome_priming.gd")
const PassiveMitochondriaClass = preload("res://scripts/skills/passive_mitochondrial_overclock.gd")
const PassiveOpsoninClass = preload("res://scripts/skills/passive_opsonin_affinity.gd")
const PassiveChemokineClass = preload("res://scripts/skills/passive_chemokine_receptors.gd")

const GM = preload("res://scripts/core/game_manager.gd")

## All catalog active weapon classes
static var ACTIVE_CATALOG: Array[Dictionary] = [
	{ "id": "ros_torrent", "name": "SKILL_ROS_NAME", "desc": "SKILL_ROS_DESC", "icon": "💨", "class": ROSTorrentClass, "class_id": "macrophage" },
	{ "id": "perforin_lance", "name": "SKILL_PERFORIN_NAME", "desc": "SKILL_PERFORIN_DESC", "icon": "🗡️", "class": PerforinLanceClass, "class_id": "ctl" },
	{ "id": "complement_cascade", "name": "SKILL_COMPLEMENT_NAME", "desc": "SKILL_COMPLEMENT_DESC", "icon": "💥", "class": ComplementCascadeClass, "class_id": "neutrophil" },
	{ "id": "antibody_salvo", "name": "SKILL_ANTIBODY_NAME", "desc": "SKILL_ANTIBODY_DESC", "icon": "🏹", "class": AntibodySalvoClass, "class_id": "b_cell" },
	{ "id": "pseudopod_lunge", "name": "SKILL_LUNGE_NAME", "desc": "SKILL_LUNGE_DESC", "icon": "🥊", "class": PseudopodLungeClass, "class_id": "dendritic" }
]

## All catalog passive trait classes
static var PASSIVE_CATALOG: Array[Dictionary] = [
	{ "id": "passive_actin", "name": "SKILL_ACTIN_NAME", "desc": "SKILL_ACTIN_DESC", "icon": "🧬", "class": PassiveActinClass },
	{ "id": "passive_lysosome", "name": "SKILL_LYSOSOME_NAME", "desc": "SKILL_LYSOSOME_DESC", "icon": "🧪", "class": PassiveLysosomeClass },
	{ "id": "passive_mitochondria", "name": "SKILL_MITOCHONDRIA_NAME", "desc": "SKILL_MITOCHONDRIA_DESC", "icon": "⚡", "class": PassiveMitochondriaClass },
	{ "id": "passive_opsonin", "name": "SKILL_OPSONIN_NAME", "desc": "SKILL_OPSONIN_DESC", "icon": "🎯", "class": PassiveOpsoninClass },
	{ "id": "passive_chemokine", "name": "SKILL_CHEMOKINE_NAME", "desc": "SKILL_CHEMOKINE_DESC", "icon": "🧲", "class": PassiveChemokineClass }
]

## Generates 3 distinct randomized upgrade cards for the player
static func generate_choices(player: Node2D, count: int = 3) -> Array[Dictionary]:
	if player == null or not is_instance_valid(player):
		return []

	var sm: SkillManager = player.get_node_or_null("SkillManager")
	if sm == null:
		return []

	var candidates: Array[Dictionary] = []

	# Gather equipped active IDs
	var equipped_active_ids: Array[String] = []
	var active_count: int = 0
	for skill in sm.active_slots:
		if skill != null and is_instance_valid(skill):
			active_count += 1
			equipped_active_ids.append(skill.skill_id)
			if skill.level < skill.max_level:
				candidates.append({
					"type": "upgrade_active",
					"id": skill.skill_id,
					"name": skill.name_key if skill.name_key != "" else skill.skill_id,
					"icon": skill.icon_symbol,
					"level": skill.level + 1,
					"badge": "UPGRADE",
					"desc": skill.desc_key if skill.desc_key != "" else ("Upgrade to Lv.%d" % (skill.level + 1)),
					"skill_ref": skill
				})

	# Gather equipped passive IDs
	var equipped_passive_ids: Array[String] = []
	var passive_count: int = 0
	for skill in sm.passive_slots:
		if skill != null and is_instance_valid(skill):
			passive_count += 1
			equipped_passive_ids.append(skill.skill_id)
			if skill.level < skill.max_level:
				candidates.append({
					"type": "upgrade_passive",
					"id": skill.skill_id,
					"name": skill.name_key if skill.name_key != "" else skill.skill_id,
					"icon": skill.icon_symbol,
					"level": skill.level + 1,
					"badge": "UPGRADE",
					"desc": skill.desc_key if skill.desc_key != "" else ("Upgrade to Lv.%d" % (skill.level + 1)),
					"skill_ref": skill
				})

	# New Actives if slots available (< 5)
	if active_count < SkillManager.MAX_ACTIVE_SLOTS:
		for item in ACTIVE_CATALOG:
			# Check if required cell class is unlocked
			var req_class = item.get("class_id", "")
			if req_class != "" and not GM.is_class_unlocked(req_class):
				continue
			if not equipped_active_ids.has(item["id"]):
				candidates.append({
					"type": "new_active",
					"id": item["id"],
					"name": item["name"],
					"icon": item["icon"],
					"level": 1,
					"badge": "NEW ACTIVE",
					"desc": item["desc"],
					"skill_class": item["class"]
				})

	# New Passives if slots available (< 5)
	if passive_count < SkillManager.MAX_PASSIVE_SLOTS:
		for item in PASSIVE_CATALOG:
			if not equipped_passive_ids.has(item["id"]):
				candidates.append({
					"type": "new_passive",
					"id": item["id"],
					"name": item["name"],
					"icon": item["icon"],
					"level": 1,
					"badge": "NEW PASSIVE",
					"desc": item["desc"],
					"skill_class": item["class"]
				})

	# Shuffle candidates
	candidates.shuffle()

	# Pick requested count
	var results: Array[Dictionary] = []
	for i in range(mini(count, candidates.size())):
		results.append(candidates[i])

	# Fallback if no candidates exist (all maxed out)
	while results.size() < count:
		results.append({
			"type": "heal_fallback",
			"id": "heal_fallback",
			"name": "ATP 生化質回充",
			"icon": "💚",
			"level": 0,
			"badge": "HEAL",
			"desc": "立即回復 35% 最大生命值並觸發脈衝"
		})

	return results

## Applies the selected choice to the player
static func apply_choice(player: Node2D, choice: Dictionary) -> bool:
	if player == null or not is_instance_valid(player):
		return false

	var sm: SkillManager = player.get_node_or_null("SkillManager")
	if sm == null:
		return false

	var c_type = choice.get("type", "")

	match c_type:
		"new_active":
			var skill_class = choice.get("skill_class", null)
			if skill_class:
				var new_skill = skill_class.new()
				return sm.equip_active(new_skill)

		"new_passive":
			var skill_class = choice.get("skill_class", null)
			if skill_class:
				var new_skill = skill_class.new()
				return sm.equip_passive(new_skill)

		"upgrade_active", "upgrade_passive":
			var skill: BaseSkill = choice.get("skill_ref", null)
			if skill and is_instance_valid(skill):
				skill.upgrade()
				return true

		"heal_fallback":
			if player.has_method("heal") and "stats" in player and player.stats:
				var max_hp = player.stats.get_stat("max_health")
				player.heal(max_hp * 0.35)
				return true

	return false
