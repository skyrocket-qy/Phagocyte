#!/usr/bin/env python3
"""
Phagocyte Visual Target Auto-Discovery & Coverage Linter.
Scans scenes/ui, scripts/skills, and preview test suites to auto-discover
all UI surfaces and active skill visual effects, reporting visual test coverage.
"""

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple

# ANSI Terminal Colors
RESET = "\033[0m"
BOLD = "\033[1m"
RED = "\033[91m"
GREEN = "\033[92m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
CYAN = "\033[96m"
WHITE = "\033[97m"
DIM = "\033[2m"


def discover_ui_scenes(root_dir: Path) -> List[Dict[str, str]]:
    """Scan scenes/ui/ for all .tscn scene files."""
    ui_dir = root_dir / "scenes" / "ui"
    scenes = []
    if not ui_dir.exists():
        return scenes

    for p in sorted(ui_dir.glob("*.tscn")):
        rel_path = p.relative_to(root_dir)
        scene_name = p.stem
        # Read first few lines of tscn to identify script / type
        script = ""
        try:
            with open(p, "r", encoding="utf-8") as f:
                content = f.read(1500)
                m = re.search(r'path="res://([^"]+\.cs)"', content)
                if m:
                    script = m.group(1)
        except Exception:
            pass

        scenes.append({
            "name": scene_name,
            "path": str(rel_path),
            "script": script,
        })
    return scenes


def discover_active_skills(root_dir: Path) -> List[Dict[str, str]]:
    """Scan scripts/skills/ and scenes/skills/ for active weapon skills with VFX."""
    skills_dir = root_dir / "scripts" / "skills"
    skills = []
    if not skills_dir.exists():
        return skills

    for p in sorted(skills_dir.glob("*Skill.cs")):
        name = p.stem
        if name in ("BaseSkill", "TreeStatBundleSkill"):
            continue

        rel_path = p.relative_to(root_dir)
        is_passive = False
        skill_id = ""

        try:
            with open(p, "r", encoding="utf-8") as f:
                code = f.read()
                if "IsPassive = true" in code:
                    is_passive = True
                m_id = re.search(r'SkillId\s*=\s*([^;]+);', code)
                if m_id:
                    skill_id = m_id.group(1).strip()
        except Exception:
            pass

        if not is_passive:
            skills.append({
                "name": name,
                "path": str(rel_path),
                "skill_id": skill_id,
            })
    return skills


def inspect_preview_suite(suite_path: Path) -> Set[str]:
    """Parse TestFullVisualPreview.cs to extract all CaptureScreenshot target names."""
    captured_targets = set()
    if not suite_path.exists():
        return captured_targets

    try:
        with open(suite_path, "r", encoding="utf-8") as f:
            for line in f:
                m = re.search(r'CaptureScreenshot\("([^"]+)"\)', line)
                if m:
                    captured_targets.add(m.group(1))
    except Exception:
        pass
    return captured_targets


def check_existing_captures(capture_dir: Path) -> Set[str]:
    """List PNG images present in the given capture directory."""
    if not capture_dir.exists():
        return set()
    return {p.name for p in capture_dir.glob("*.png")}


def run_discovery_audit(root_dir: Path, capture_dir: Path) -> bool:
    """Print the complete visual targets auto-discovery and coverage report."""
    print(f"\n{CYAN}{BOLD}╔══════════════════════════════════════════════════════════════════════════╗{RESET}")
    print(f"{CYAN}{BOLD}║{WHITE}{BOLD}         🔬  PHAGOCYTE VISUAL TARGETS AUTO-DISCOVERY & AUDIT              {CYAN}{BOLD}║{RESET}")
    print(f"{CYAN}{BOLD}╚══════════════════════════════════════════════════════════════════════════╝{RESET}\n")

    ui_scenes = discover_ui_scenes(root_dir)
    active_skills = discover_active_skills(root_dir)

    suite_path = root_dir / "tests" / "TestFullVisualPreview.cs"
    captured_targets = inspect_preview_suite(suite_path)
    disk_captures = check_existing_captures(capture_dir)

    print(f"{BLUE}{BOLD}1. UI Scenes Discovery ({len(ui_scenes)} scenes found in scenes/ui/):{RESET}")

    # Map scenes to expected capture files
    ui_capture_map = {
        "title_view": "title_view.png",
        "class_view": "class_view.png",
        "loadout_view": "loadout_view.png",
        "passive_view": "passive_view.png",
        "map_view": "map_view.png",
        "achievement_gallery": "gallery_all.png",
        "endgame_setup_modal": "endgame_setup.png",
        "codex_modal": "codex_modal.png",
        "settings_modal": "settings_modal.png",
        "run_records_modal": "run_records.png",
        "toast_banner": "toast_banner.png",
        "achievement_toast": "achievement_toast.png",
        "upgrade_modal": "upgrade_modal.png",
        "hud": "hud_hp.png",
        "main_menu": "title_view.png",
    }

    covered_ui_count = 0
    uncovered_ui = []

    for item in ui_scenes:
        name = item["name"]
        expected_cap = ui_capture_map.get(name)
        is_covered = expected_cap and (expected_cap in captured_targets)
        has_png = expected_cap and (expected_cap in disk_captures)

        if is_covered:
            covered_ui_count += 1
            status_str = f"{GREEN}{BOLD}✓ COVERED{RESET}"
            cap_note = f"-> {expected_cap} ({'ON DISK' if has_png else 'PENDING CAPTURE'})"
        else:
            uncovered_ui.append(item)
            status_str = f"{YELLOW}⚠️  COMPONENT{RESET}"
            cap_note = "(nested sub-scene or needs stage)"

        print(f"  • {item['name']:<24} {status_str:<18} {DIM}{cap_note}{RESET}")

    print(f"\n{BLUE}{BOLD}2. Active Skill VFX Discovery ({len(active_skills)} weapon skills in scripts/skills/):{RESET}")

    skill_capture_map = {
        "ComplementCascadeSkill": "skill_mac_mine.png",
        "AntibodySalvoSkill": "skill_antibody_missile.png",
        "PerforinLanceSkill": "skill_perforin_lance.png",
        "PhagocyticGraspSkill": "skill_grasp_chain.png",
        "RosTorrentSkill": "skill_ros_torrent.png",
    }

    covered_skill_count = 0
    uncovered_skills = []

    for s in active_skills:
        name = s["name"]
        expected_cap = skill_capture_map.get(name)
        is_covered = expected_cap and (expected_cap in captured_targets)
        has_png = expected_cap and (expected_cap in disk_captures)

        if is_covered:
            covered_skill_count += 1
            status_str = f"{GREEN}{BOLD}✓ COVERED{RESET}"
            cap_note = f"-> {expected_cap} ({'ON DISK' if has_png else 'PENDING CAPTURE'})"
        else:
            uncovered_skills.append(s)
            status_str = f"{YELLOW}• UNTESTED VFX{RESET}"
            cap_note = "(not yet in TestFullVisualPreview.cs)"

        print(f"  • {s['name']:<28} {status_str:<20} {DIM}{cap_note}{RESET}")

    # Summary
    print(f"\n{CYAN}{BOLD}══════════════════════════════════════════════════════════════════════════{RESET}")
    print(f"{WHITE}{BOLD}Coverage Summary:{RESET}")
    print(f"  • UI Screen Coverage    : {covered_ui_count}/{len(ui_scenes)} scenes mapped ({covered_ui_count/max(1, len(ui_scenes))*100:.1f}%)")
    print(f"  • Active Skill VFX      : {covered_skill_count}/{len(active_skills)} skills with live VFX captures")
    print(f"  • Total Captured Targets: {len(captured_targets)} distinct screenshots scripted in preview harness")
    print(f"  • Captures on Disk      : {len(disk_captures)} PNG files in {capture_dir}")
    print(f"{CYAN}{BOLD}══════════════════════════════════════════════════════════════════════════{RESET}\n")

    return True


def run_preview_capture(root_dir: Path, target_dir: Path) -> bool:
    """Execute TestFullVisualPreview.cs to refresh all 21 captures."""
    print(f"{CYAN}Running headed visual preview suite into: {target_dir}...{RESET}")
    godot_bin = "/Applications/Godot_mono.app/Contents/MacOS/Godot"
    if not os.path.exists(godot_bin):
        print(f"{RED}Error: Godot binary not found at {godot_bin}{RESET}")
        return False

    env = os.environ.copy()
    env["PHAGOCYTE_CAPTURE_DIR"] = str(target_dir)

    cmd = [godot_bin, "--path", str(root_dir), "-s", "res://tests/TestFullVisualPreview.cs"]
    res = subprocess.run(cmd, env=env)
    return res.returncode == 0


def main():
    parser = argparse.ArgumentParser(description="Phagocyte Visual Target Auto-Discovery & Coverage Tool")
    parser.add_argument("--capture-dir", "-d", type=Path, default=Path("tmp/visual_full"), help="Directory containing current captures")
    parser.add_argument("--capture", "-c", action="store_true", help="Run headed preview suite to capture all targets")
    args = parser.parse_args()

    root_dir = Path(__file__).resolve().parent.parent.parent.parent.parent

    if args.capture:
        success = run_preview_capture(root_dir, args.capture_dir)
        if not success:
            sys.exit(1)

    run_discovery_audit(root_dir, args.capture_dir)


if __name__ == "__main__":
    main()
