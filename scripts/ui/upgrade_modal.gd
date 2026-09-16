class_name UpgradeModal
extends Control

## Level-Up 3-Choice Epigenetic Mutation Modal.
## Pauses the game, displays 3 distinct choices (Active/Passive), and applies the selected upgrade.

signal choice_applied(choice: Dictionary)

const UpgradeManager = preload("res://scripts/core/upgrade_manager.gd")

@onready var title_label: Label = $CenterContainer/VBox/TitleLabel
@onready var subtitle_label: Label = $CenterContainer/VBox/SubtitleLabel
@onready var cards_container: HBoxContainer = $CenterContainer/VBox/CardsContainer

var current_choices: Array[Dictionary] = []
var player_ref: Node2D = null
var pending_levels: int = 0

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	_setup_card_listeners()

func _setup_card_listeners() -> void:
	var cards = cards_container.get_children()
	for i in range(cards.size()):
		var card = cards[i]
		var btn = card.get_node_or_null("SelectButton")
		if btn:
			var idx = i
			btn.pressed.connect(func(): _on_card_clicked(idx))

func open_upgrade_modal(player: Node2D) -> void:
	player_ref = player
	if visible:
		pending_levels += 1
		return

	_show_next_upgrade()

func _show_next_upgrade() -> void:
	if player_ref == null or not is_instance_valid(player_ref):
		return

	current_choices = UpgradeManager.generate_choices(player_ref, 3)
	if current_choices.is_empty():
		return

	_populate_cards()
	visible = true
	get_tree().paused = true

func _populate_cards() -> void:
	if title_label:
		title_label.text = tr("UPGRADE_MODAL_TITLE")
	if subtitle_label:
		subtitle_label.text = tr("UPGRADE_MODAL_SUBTITLE")

	var cards = cards_container.get_children()
	for i in range(cards.size()):
		var card = cards[i]
		if i < current_choices.size():
			var choice = current_choices[i]
			card.visible = true

			var icon_lbl = card.get_node_or_null("VBox/IconLabel")
			var title_lbl = card.get_node_or_null("VBox/TitleLabel")
			var badge_lbl = card.get_node_or_null("VBox/BadgeLabel")
			var desc_lbl = card.get_node_or_null("VBox/DescLabel")

			if icon_lbl:
				icon_lbl.text = choice.get("icon", "⚡")
			if title_lbl:
				title_lbl.text = tr(choice.get("name", ""))
			if badge_lbl:
				var b_type = choice.get("type", "")
				if b_type == "new_active":
					badge_lbl.text = "[ " + tr("BADGE_NEW_ACTIVE") + " ]"
					badge_lbl.modulate = Color(0.9, 0.4, 0.4)
				elif b_type == "new_passive":
					badge_lbl.text = "[ " + tr("BADGE_NEW_PASSIVE") + " ]"
					badge_lbl.modulate = Color(0.4, 0.9, 0.6)
				elif b_type.begins_with("upgrade"):
					badge_lbl.text = "[ " + tr("BADGE_UPGRADE") + " Lv.%d ]" % choice.get("level", 2)
					badge_lbl.modulate = Color(1.0, 0.85, 0.3)
				else:
					badge_lbl.text = "[ " + tr(choice.get("badge", "UPGRADE")) + " ]"
					badge_lbl.modulate = Color(0.5, 0.8, 1.0)
			if desc_lbl:
				desc_lbl.text = tr(choice.get("desc", ""))
		else:
			card.visible = false

func _on_card_clicked(idx: int) -> void:
	if idx < 0 or idx >= current_choices.size():
		return

	var choice = current_choices[idx]
	UpgradeManager.apply_choice(player_ref, choice)
	choice_applied.emit(choice)

	visible = false
	get_tree().paused = false

	if pending_levels > 0:
		pending_levels -= 1
		call_deferred("_show_next_upgrade")
