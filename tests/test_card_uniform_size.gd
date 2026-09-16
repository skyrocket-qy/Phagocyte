extends SceneTree

var frame_count: int = 0

func _init() -> void:
	print("--- TESTING UPGRADE MODAL CARD SIZE UNIFORMITY ---")
	var modal_scene = load("res://scenes/ui/upgrade_modal.tscn")
	var modal = modal_scene.instantiate()
	root.add_child(modal)

func _process(_delta: float) -> bool:
	frame_count += 1
	if frame_count < 3:
		return false

	var modal = root.get_node_or_null("UpgradeModal")
	if modal == null:
		printerr("Modal not found")
		quit(1)
		return true

	var cards = modal.get_node("CenterContainer/VBox/CardsContainer").get_children()
	assert(cards.size() == 3, "Expected 3 cards")

	# Card 0: Tiny text
	cards[0].get_node("VBox/TitleLabel").text = "短"
	cards[0].get_node("VBox/BadgeLabel").text = "[ 升 ]"
	cards[0].get_node("VBox/DescLabel").text = "微量效果"

	# Card 1: Very long English and Chinese text
	cards[1].get_node("VBox/TitleLabel").text = "線粒體超頻呼吸 (Mitochondrial Overclock Cascade Ultra)"
	cards[1].get_node("VBox/BadgeLabel").text = "[ NEW PASSIVE ORGANELLE LEVEL 5 ]"
	cards[1].get_node("VBox/DescLabel").text = "Cooldown Reduction +8% (max 75%), Skill Duration +10% per level. Highly accelerated oxidative phosphorylation turnover rate in mitochondrial matrix."

	# Card 2: Standard medium text
	cards[2].get_node("VBox/TitleLabel").text = "活性氧射流 (ROS Torrent)"
	cards[2].get_node("VBox/BadgeLabel").text = "[ 新主动武器 ]"
	cards[2].get_node("VBox/DescLabel").text = "向前方喷射高压酸雾，造成持续破甲腐蚀。"

	# Force layout update
	modal.get_node("CenterContainer").queue_sort()

	# Check min sizes
	var s0 = cards[0].get_combined_minimum_size()
	var s1 = cards[1].get_combined_minimum_size()
	var s2 = cards[2].get_combined_minimum_size()

	print("Card 0 min size: ", s0)
	print("Card 1 min size: ", s1)
	print("Card 2 min size: ", s2)

	assert(is_equal_approx(s0.x, s1.x) and is_equal_approx(s1.x, s2.x), "All cards MUST have identical minimum width! Found: %s vs %s vs %s" % [s0.x, s1.x, s2.x])
	assert(is_equal_approx(s0.y, s1.y) and is_equal_approx(s1.y, s2.y), "All cards MUST have identical minimum height! Found: %s vs %s vs %s" % [s0.y, s1.y, s2.y])

	print("[PASS] All 3 cards have 100% IDENTICAL dimensions!")
	quit(0)
	return true
