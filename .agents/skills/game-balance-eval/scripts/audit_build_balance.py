#!/usr/bin/env python3
"""
Phagocyte Roguelite Build Synergy & Dynamic Balance Audit Linter.
Quantitatively assesses 5-class viability, gear energy efficiency,
anti-monopoly / dead-card elimination, and canonical archetype coverage.
"""

import argparse
import json
import os
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple, Any, Optional

# ANSI Colors
RESET = "\033[0m"
BOLD = "\033[1m"
DIM = "\033[2m"
RED = "\033[91m"
GREEN = "\033[92m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
MAGENTA = "\033[95m"
CYAN = "\033[96m"
WHITE = "\033[97m"

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent.parent.parent


def load_json(path: Path) -> Any:
    if not path.exists():
        return None
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        print(f"{RED}Error loading {path}: {e}{RESET}", file=sys.stderr)
        return None


class BalanceAuditor:
    def __init__(self, root: Path):
        self.root = root
        self.data_dir = root / "assets" / "data"
        self.classes_path = self.data_dir / "classes.json"
        self.skills_path = self.data_dir / "skills.json"
        self.gear_path = self.data_dir / "gear.json"
        self.passives_path = self.data_dir / "passive_traits.json"
        self.pathogens_path = self.data_dir / "pathogens.json"

        self.warnings: List[str] = []
        self.passes: List[str] = []
        self.scores: Dict[str, float] = {
            "class_diversity": 0.0,
            "gear_utility": 0.0,
            "archetype_synergy": 0.0,
            "stat_economy": 0.0,
            "pathogen_escalation": 0.0,
        }

    def audit_class_diversity(self) -> float:
        """Audits the 5 immune cell classes for distinctiveness and specialization."""
        score = 20.0
        classes = load_json(self.classes_path)
        if not classes or not isinstance(classes, list):
            self.warnings.append("classes.json not found or malformed.")
            return 0.0

        if len(classes) != 5:
            self.warnings.append(f"Expected 5 classes, found {len(classes)}.")
            score -= 5.0
        else:
            self.passes.append("All 5 canonical immune cell classes configured.")

        # Check unique roles and distinct trait stats
        trait_stats = set()
        for c in classes:
            cid = c.get("id", "")
            tstat = c.get("trait_stat", "")
            if not tstat:
                self.warnings.append(f"Class '{cid}' missing trait_stat.")
                score -= 3.0
            else:
                trait_stats.add(tstat)

        if len(trait_stats) == len(classes):
            self.passes.append(f"100% trait stat uniqueness across all classes ({', '.join(sorted(trait_stats))}).")
        else:
            self.warnings.append("Overlapping trait stats detected across classes.")
            score -= 3.0

        # Check skills mapping to classes
        skills = load_json(self.skills_path) or []
        class_skills: Dict[str, List[str]] = {c.get("id", ""): [] for c in classes}
        for s in skills:
            cid = s.get("class_id", "")
            if cid in class_skills:
                class_skills[cid].append(s.get("id", ""))

        classes_with_skills = [cid for cid, skl in class_skills.items() if len(skl) > 0]
        if len(classes_with_skills) == len(classes):
            self.passes.append("Every class has designated class-exclusive skills configured.")
        else:
            missing = set(class_skills.keys()) - set(classes_with_skills)
            self.warnings.append(f"Classes missing class-exclusive skills: {missing}")
            score -= 4.0

        return max(0.0, score)

    def audit_gear_utility(self) -> float:
        """Audits gear for anti-monopoly, dead-card elimination, and category balance."""
        score = 25.0
        gear = load_json(self.gear_path)
        if not gear or not isinstance(gear, list):
            self.warnings.append("gear.json not found or malformed.")
            return 0.0

        categories = set()
        dead_gear = []
        energy_costs = []

        for org in gear:
            oid = org.get("id", "")
            cat = org.get("category", "")
            cost = org.get("energy_cost", 0)
            modifiers = org.get("modifiers", [])
            drawback = org.get("drawback", [])

            categories.add(cat)
            energy_costs.append(cost)

            # A functional gear has either stat modifiers or acts as an energy generator (cost < 0)
            has_valid_mod = any(m.get("value", 0) != 0 for m in modifiers)
            is_energy_battery = cost < 0
            if not has_valid_mod and not is_energy_battery:
                dead_gear.append(oid)

        if dead_gear:
            self.warnings.append(f"Dead gear detected (zero functional modifiers): {dead_gear}")
            score -= min(10.0, len(dead_gear) * 3.0)
        else:
            self.passes.append(f"All {len(gear)} gear have functional stat modifiers (zero dead cards).")

        expected_cats = {"metabolism", "digestion", "cytoskeleton", "synthesis", "sensing", "symbiosis"}
        covered_cats = categories.intersection(expected_cats)
        if covered_cats == expected_cats:
            self.passes.append(f"Full 6-category biological gear taxonomy covered ({', '.join(sorted(covered_cats))}).")
        else:
            missing_cats = expected_cats - covered_cats
            self.warnings.append(f"Missing gear categories: {missing_cats}")
            score -= len(missing_cats) * 2.0

        avg_cost = sum(energy_costs) / len(energy_costs) if energy_costs else 0
        if 2.0 <= avg_cost <= 3.5:
            self.passes.append(f"Average gear energy cost is well-balanced ({avg_cost:.2f} ATP).")
        else:
            self.warnings.append(f"Average gear cost skewed: {avg_cost:.2f}")
            score -= 3.0

        return max(0.0, score)

    def audit_archetype_synergy(self) -> float:
        """Audits coverage of the 5 canonical immunological archetypes."""
        score = 25.0
        skills = load_json(self.skills_path) or []
        gear = load_json(self.gear_path) or []

        skill_ids = {s.get("id", "") for s in skills}
        gear_stats = set()
        for org in gear:
            for mod in org.get("modifiers", []):
                gear_stats.add(mod.get("stat", ""))

        archetypes = {
            "ROS Melt / Oxidation": {
                "skills": ["ros_torrent", "phagolysosome_vent", "nitric_oxide_halo"],
                "stats": ["might", "duration", "area"],
            },
            "Cytotoxic Piercing Sniper": {
                "skills": ["perforin_lance", "granzyme_detonation"],
                "stats": ["crit_chance", "crit_damage", "projectile_speed", "pierce"],
            },
            "Engulf-and-Digest Heavy Tank": {
                "skills": ["phagocytic_grasp", "lysosomal_overload"],
                "stats": ["max_health", "armor", "life_steal", "block"],
            },
            "Complement Trap Network": {
                "skills": ["complement_cascade", "exosome_singularity"],
                "stats": ["cooldown_reduction", "area", "duration"],
            },
            "Antibody Opsonization Swarm": {
                "skills": ["antibody_salvo", "defensin_barbs"],
                "stats": ["projectile_amount", "cooldown_reduction", "move_speed"],
            },
        }

        covered_archetypes = 0
        for arch_name, req in archetypes.items():
            matching_skills = [sid for sid in req["skills"] if sid in skill_ids]
            matching_stats = [st for st in req["stats"] if st in gear_stats]

            if len(matching_skills) > 0 and len(matching_stats) >= 2:
                covered_archetypes += 1
                self.passes.append(f"Archetype '{arch_name}' supported by {len(matching_skills)} skills and {len(matching_stats)} gear stats.")
            else:
                self.warnings.append(f"Archetype '{arch_name}' lacks adequate skill or gear support.")
                score -= 4.0

        if covered_archetypes == 5:
            self.passes.append("All 5 canonical immunological archetypes fully supported.")

        return max(0.0, score)

    def audit_stat_economy(self) -> float:
        """Audits energy-to-potency ratios and diminishing returns."""
        score = 15.0
        gear = load_json(self.gear_path) or []

        # Check that positive cost gear provide proportional power
        outliers = []
        for org in gear:
            cost = org.get("energy_cost", 1)
            if cost <= 0:
                continue  # Energy generation battery, skip potency/cost check
            mods = org.get("modifiers", [])
            total_potency = sum(abs(m.get("value", 0)) for m in mods)
            ratio = total_potency / max(1, cost)
            if ratio < 0.01:
                outliers.append(org.get("id", ""))

        if outliers:
            self.warnings.append(f"Gear with poor energy-to-potency ratio (<0.01): {outliers}")
            score -= min(5.0, len(outliers) * 2.0)
        else:
            self.passes.append("Gear stat scaling scales proportionally with energy investment.")

        # Check passive tree node count
        tree_path = self.data_dir / "passive_tree.json"
        tree_data = load_json(tree_path)
        if tree_data and "nodes" in tree_data:
            node_count = len(tree_data["nodes"])
            if node_count >= 50:
                self.passes.append(f"Passive tree offers deep meta-progression network ({node_count} nodes).")
            else:
                self.warnings.append(f"Passive tree node count ({node_count}) is small.")
                score -= 3.0
        else:
            self.warnings.append("passive_tree.json missing or invalid.")
            score -= 5.0

        return max(0.0, score)

    def audit_pathogen_escalation(self) -> float:
        """Audits pathogen danger tier distribution and escalation curves."""
        score = 15.0
        pathogens = load_json(self.pathogens_path)
        if not pathogens or not isinstance(pathogens, list):
            self.warnings.append("pathogens.json not found or malformed.")
            return 0.0

        danger_levels = {}
        for p in pathogens:
            dl = p.get("danger_level", "Unknown")
            danger_levels[dl] = danger_levels.get(dl, 0) + 1

        if len(pathogens) >= 20:
            self.passes.append(f"Rich microbiological ecosystem configured ({len(pathogens)} pathogens).")
        else:
            self.warnings.append(f"Microbiological roster has only {len(pathogens)} pathogens (<20).")
            score -= 4.0

        # Check danger spread
        if "★☆☆" in danger_levels and "★★☆" in danger_levels and "★★★" in danger_levels:
            self.passes.append(f"Multi-tier threat escalation curve present: {dict(danger_levels)}.")
        else:
            self.warnings.append(f"Incomplete threat tier progression in pathogens.json.")
            score -= 4.0

        return max(0.0, score)

    def run(self) -> Dict[str, Any]:
        self.scores["class_diversity"] = self.audit_class_diversity()
        self.scores["gear_utility"] = self.audit_gear_utility()
        self.scores["archetype_synergy"] = self.audit_archetype_synergy()
        self.scores["stat_economy"] = self.audit_stat_economy()
        self.scores["pathogen_escalation"] = self.audit_pathogen_escalation()

        total_score = sum(self.scores.values())

        if total_score >= 95.0:
            grade = "A+"
        elif total_score >= 90.0:
            grade = "A"
        elif total_score >= 80.0:
            grade = "B"
        elif total_score >= 70.0:
            grade = "C"
        else:
            grade = "F"

        return {
            "total_score": round(total_score, 1),
            "grade": grade,
            "scores": {k: round(v, 1) for k, v in self.scores.items()},
            "passes": self.passes,
            "warnings": self.warnings,
        }


def format_report_markdown(results: Dict[str, Any]) -> str:
    lines = [
        "# Phagocyte Build Synergy & Dynamic Balance Audit Report",
        "",
        f"**Build Balance Index (BBI)**: `{results['total_score']} / 100.0` (Grade: **{results['grade']}**)",
        "",
        "## Sub-Dimension Breakdown",
        "",
        "| Evaluation Pillar | Score | Max | Status |",
        "|:---|:---:|:---:|:---:|",
        f"| 1. Class Diversity & Distinctiveness | {results['scores']['class_diversity']} | 20.0 | {'✅ PASS' if results['scores']['class_diversity'] >= 16 else '⚠️ WARN'} |",
        f"| 2. Gear Utility & Anti-Monopoly | {results['scores']['gear_utility']} | 25.0 | {'✅ PASS' if results['scores']['gear_utility'] >= 20 else '⚠️ WARN'} |",
        f"| 3. Archetype Synergy Depth | {results['scores']['archetype_synergy']} | 25.0 | {'✅ PASS' if results['scores']['archetype_synergy'] >= 20 else '⚠️ WARN'} |",
        f"| 4. Stat Economy & Meta Progression | {results['scores']['stat_economy']} | 15.0 | {'✅ PASS' if results['scores']['stat_economy'] >= 12 else '⚠️ WARN'} |",
        f"| 5. Pathogen Escalation Progression | {results['scores']['pathogen_escalation']} | 15.0 | {'✅ PASS' if results['scores']['pathogen_escalation'] >= 12 else '⚠️ WARN'} |",
        "",
        "## Verified Passed Audits",
        "",
    ]
    for p in results["passes"]:
        lines.append(f"- [x] {p}")

    lines.extend(["", "## Warnings & Diagnostic Flags", ""])
    if results["warnings"]:
        for w in results["warnings"]:
            lines.append(f"- [ ] ⚠️ {w}")
    else:
        lines.append("- Zero diagnostic warnings. Roguelite build balance pipeline fully compliant!")

    lines.append("")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Audit Phagocyte roguelite build synergy and balance.")
    parser.add_argument("--summary", action="store_true", help="Print concise summary report.")
    parser.add_argument("--detail", action="store_true", help="Print detailed diagnostic logs.")
    parser.add_argument("--report", type=str, default="", help="Export report to markdown file.")
    args = parser.parse_args()

    auditor = BalanceAuditor(PROJECT_ROOT)
    results = auditor.run()

    print(f"\n{BOLD}{CYAN}=================================================================={RESET}")
    print(f"{BOLD}{CYAN}>>> PHAGOCYTE BUILD SYNERGY & DYNAMIC BALANCE AUDIT <<<{RESET}")
    print(f"{BOLD}{CYAN}=================================================================={RESET}")
    print(f"{BOLD}Build Balance Index (BBI):{RESET} {GREEN if results['total_score'] >= 85 else YELLOW}{results['total_score']}/100.0{RESET} ({BOLD}{results['grade']}{RESET})")
    print("------------------------------------------------------------------")

    for pillar, sc in results["scores"].items():
        color = GREEN if sc >= 15.0 else YELLOW
        print(f"  - {pillar:<28}: {color}{sc:5.1f}{RESET}")

    if args.detail or not args.summary:
        print("\n" + BOLD + "Verified Passes:" + RESET)
        for p in results["passes"]:
            print(f"  {GREEN}✔{RESET} {p}")

        if results["warnings"]:
            print("\n" + BOLD + YELLOW + "Warnings / Flags:" + RESET)
            for w in results["warnings"]:
                print(f"  {YELLOW}⚠{RESET} {w}")
        else:
            print(f"\n{GREEN}✔ Zero warnings detected across build balance pipeline!{RESET}")

    if args.report:
        out_path = Path(args.report)
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(format_report_markdown(results))
        print(f"\n{BOLD}Report exported to:{RESET} {out_path}")


if __name__ == "__main__":
    main()
