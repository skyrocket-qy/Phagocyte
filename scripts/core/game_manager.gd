extends Node

## Runtime Player Selections (Static so accessible from anywhere)
static var selected_class: String = "macrophage"
static var selected_map: String = "acute_wound"

## Class Metadata from SPEC.md
const CLASS_DATA = {
	"macrophage": {
		"name": "巨噬細胞 (Macrophage)",
		"role": "近戰重裝 / 吞噬",
		"trait": "頂點位移幅度極大，偽足延伸速度快",
		"passive": "巨噬本能：吞噬範圍 +40%；每消化一隻病原體恢復 0.5% 最大生命值。",
		"burst": "呼吸爆發：飽食度滿時移速 +150%，釋放高壓酸性腐蝕力場 6 秒。",
		"unlocked": true
	},
	"ctl": {
		"name": "殺手 T 細胞 (CTL)",
		"role": "高速刺客 / 穿透",
		"trait": "體積小，呈緊湊橢球狀，變形頻率高但位移小",
		"passive": "穿孔處決：衝刺貫穿核心，對標記病原體造成 400% 凋亡傷害。",
		"burst": "顆粒酶過載：釋放大量穿孔素，全屏特定標記靶標瞬間裂解。",
		"unlocked": false
	},
	"neutrophil": {
		"name": "嗜中性球 (Neutrophil)",
		"role": "敢死爆破 / 陣地戰",
		"trait": "邊界極不穩定，易破碎",
		"passive": "NETosis 自毀：致命傷害時化為全螢幕覆蓋的 DNA 酸性黏網持續 8 秒。",
		"burst": "脫顆粒風暴：向四周爆發活性酵素彈幕，清理密集敵群。",
		"unlocked": false
	},
	"b_cell": {
		"name": "B 淋巴細胞 (B-Cell)",
		"role": "遠程發射 / 導引",
		"trait": "體積中等，變形平緩，體表佈滿受體凸起",
		"passive": "漿細胞轉化：靜止時無法吞噬，但抗體射擊頻率每秒 +20%（最高疊加至 300%）。",
		"burst": "高頻抗體雨：連續發射大批特異性免疫球蛋白引導彈。",
		"unlocked": false
	},
	"dendritic": {
		"name": "樹突狀細胞 (Dendritic)",
		"role": "戰術指揮 / 召喚",
		"trait": "具備多方向超長星狀突起（樹突）",
		"passive": "信號放大：採樣速度 +100%；週期性召喚周邊巡邏淋巴球群協同作戰。",
		"burst": "淋巴趨化募集：召集大量微血管效應細胞發動協同總攻。",
		"unlocked": false
	}
}

## Map Metadata from SPEC.md
const MAP_DATA = {
	"acute_wound": {
		"name": "表皮裂口 (Acute Wound)",
		"environment": "微血管破裂，體液滲出區",
		"mechanic": "週期性產生指向傷口外緣的強大組織液吸力；地面微血管網造成流體阻力。",
		"threat": "金黃色葡萄球菌 (Staph) 抱團侵襲",
		"bg_color": Color(0.05, 0.08, 0.12, 1.0),
		"unlocked": true
	},
	"alveolar_space": {
		"name": "肺泡腔室 (Alveolar Space)",
		"environment": "氣體交換介面，肺泡上皮微腔",
		"mechanic": "週期性呼吸氣流帶來大範圍流體推力；需利用偽足錨定上皮細胞壁以防失控。",
		"threat": "冠狀病毒 (S-Virus) 刺突減速",
		"bg_color": Color(0.04, 0.11, 0.13, 1.0),
		"unlocked": true
	}
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
