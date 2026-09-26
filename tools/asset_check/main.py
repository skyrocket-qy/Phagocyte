#!/usr/bin/env python3
"""Unified Asset Quality, Integrity & Style Linter for Phagocyte with Rich Colorful Output."""

import argparse
import sys
from pathlib import Path

# Add project root to sys.path
root_dir = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(root_dir))
tools_dir = Path(__file__).resolve().parent.parent
if str(tools_dir) not in sys.path:
    sys.path.insert(0, str(tools_dir))

try:
    from tools.asset_check.missing import check_missing_assets
    from tools.asset_check.naming import check_naming_conventions
    from tools.asset_check.dedup import check_duplicates
    from tools.asset_check.orphans import check_orphan_assets, check_orphan_imports
    from tools.asset_check.resolution import check_asset_resolutions
    from tools.asset_check.quality import check_image_quality
    from tools.asset_check.audio import check_audio_assets
except ImportError:
    from asset_check.missing import check_missing_assets
    from asset_check.naming import check_naming_conventions
    from asset_check.dedup import check_duplicates
    from asset_check.orphans import check_orphan_assets, check_orphan_imports
    from asset_check.resolution import check_asset_resolutions
    from asset_check.quality import check_image_quality
    from asset_check.audio import check_audio_assets

# ANSI Terminal Color Constants
RESET = "\033[0m"
BOLD = "\033[1m"
DIM = "\033[2m"
ITALIC = "\033[3m"
UNDERLINE = "\033[4m"

RED = "\033[91m"
GREEN = "\033[92m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
MAGENTA = "\033[95m"
CYAN = "\033[96m"
WHITE = "\033[97m"


def print_banner():
    print(f"\n{CYAN}{BOLD}╔══════════════════════════════════════════════════════════════════════════╗{RESET}")
    print(f"{CYAN}{BOLD}║{GREEN}{BOLD}          🔬  PHAGOCYTE ASSET INTEGRITY & QUALITY LINTER SUITE           {CYAN}{BOLD}║{RESET}")
    print(f"{CYAN}{BOLD}╚══════════════════════════════════════════════════════════════════════════╝{RESET}\n")


def print_step(step_num: int, total_steps: int, title: str):
    print(f"{BLUE}{BOLD} ◈ [{step_num}/{total_steps}]{RESET} {WHITE}{BOLD}{title}{RESET}")


def run_all_checks(target_category=None, summary_only=False) -> bool:
    print_banner()

    # 1. Missing & Unmapped Assets (Bidirectional Integrity)
    print_step(1, 7, "Verifying bidirectional asset references and registered IDs...")
    missing_by_cat, unmapped_issues, category_counts = check_missing_assets(root_dir, target_category=target_category)

    # 2. Naming Conventions
    print_step(2, 7, "Checking snake_case, casing, and prefix conventions...")
    naming_issues = check_naming_conventions(root_dir)

    # 3. Duplicates & Conflicts
    print_step(3, 7, "Scanning for duplicate files & near-duplicate collisions...")
    dedup_issues = check_duplicates(root_dir)

    # 4. Orphans & Dead Imports
    print_step(4, 7, "Checking for orphan Godot .import files & unreferenced assets...")
    orphan_imports = check_orphan_imports(root_dir)
    orphan_assets = check_orphan_assets(root_dir)

    # 5. Resolutions & Aspect Ratios
    print_step(5, 7, "Validating 1:1 aspect ratios and minimum resolutions in gen/... ")
    resolution_issues, res_stats = check_asset_resolutions(root_dir)

    # 6. Deep Image Quality & Artifact Inspection
    print_step(6, 7, "Inspecting border clipping, corner transparency, and halos...")
    quality_issues, quality_stats = check_image_quality(root_dir)

    # 7. Audio Manifest Integrity (manifest.json vs files on disk)
    print_step(7, 7, "Verifying audio manifest entries resolve to files on disk...")
    audio_missing, audio_total = check_audio_assets(root_dir)

    # --- Summary Table ---
    print(f"\n{MAGENTA}{BOLD}╔══════════════════════════════════════════════════════════════════════════╗{RESET}")
    print(f"{MAGENTA}{BOLD}║{WHITE}{BOLD}                         📊  ASSET AUDIT SUMMARY                          {MAGENTA}{BOLD}║{RESET}")
    print(f"{MAGENTA}{BOLD}╠══════════════════════════════════════════════════════════════════════════╣{RESET}")

    total_registered = sum(category_counts.values())
    total_missing = sum(len(items) for items in missing_by_cat.values())

    for cat, total in sorted(category_counts.items()):
        if target_category and cat.lower() != target_category.lower():
            continue
        missing_count = len(missing_by_cat.get(cat, []))
        ready_count = total - missing_count
        status_color = GREEN if missing_count == 0 else (YELLOW if ready_count > 0 else RED)
        print(f"{MAGENTA}{BOLD}║{RESET}  {CYAN}•{RESET} {WHITE}{cat:<16}{RESET} : {status_color}{BOLD}{ready_count:>3}/{total:<3}{RESET} ready ({status_color}{missing_count:>3} missing{RESET})                      {MAGENTA}{BOLD}║{RESET}")

    print(f"{MAGENTA}{BOLD}╚══════════════════════════════════════════════════════════════════════════╝{RESET}\n")

    has_errors = total_missing > 0 or len(audio_missing) > 0

    # Print Missing Assets by Category (Full list, NO truncation)
    if total_missing > 0:
        print(f"{RED}{BOLD}❌ MISSING ASSETS TO GENERATE ({total_missing} total):{RESET}\n")
        for cat, items in missing_by_cat.items():
            if not items:
                print(f"  {GREEN}{BOLD}✓ [{cat}] All assets generated!{RESET}")
                continue

            print(f"  {YELLOW}{BOLD}[{cat}] ({len(items)} missing):{RESET}")
            if summary_only:
                print(f"    {DIM}(Use without --summary to list all {len(items)} items){RESET}")
            else:
                for item in items:
                    print(f"    {RED}•{RESET} {item}")
            print()
    else:
        print(f"{GREEN}{BOLD}✓ All registered assets are present and ready on disk!{RESET}\n")

    # Print Unmapped Assets
    if unmapped_issues:
        print(f"{YELLOW}{BOLD}⚠️  UNMAPPED ASSETS ON DISK ({len(unmapped_issues)}):{RESET}")
        for issue in unmapped_issues:
            print(f"  {YELLOW}• {issue}{RESET}")
        print()

    # Print Naming Issues
    if naming_issues:
        print(f"{RED}{BOLD}❌ NAMING VIOLATIONS ({len(naming_issues)}):{RESET}")
        for issue in naming_issues:
            print(f"  {RED}• {issue}{RESET}")
        print()

    # Print Duplicate Issues
    if dedup_issues:
        print(f"{YELLOW}{BOLD}⚠️  DUPLICATE CONTENT ({len(dedup_issues)}):{RESET}")
        for issue in dedup_issues:
            print(f"  {YELLOW}• {issue}{RESET}")
        print()

    # Print Orphan Imports
    if orphan_imports:
        print(f"{RED}{BOLD}❌ ORPHAN .IMPORT FILES ({len(orphan_imports)}):{RESET}")
        for issue in orphan_imports:
            print(f"  {RED}• {issue}{RESET}")
        print()

    # Print Resolution Violations
    if resolution_issues:
        print(f"{RED}{BOLD}❌ RESOLUTION VIOLATIONS ({len(resolution_issues)}):{RESET}")
        for issue in resolution_issues:
            print(f"  {RED}• {issue}{RESET}")
        print()

    # Print Quality Issues
    if quality_issues:
        print(f"{YELLOW}{BOLD}⚠️  VISUAL QUALITY ISSUES ({len(quality_issues)}):{RESET}")
        for issue in quality_issues:
            print(f"  {YELLOW}• {issue}{RESET}")
        print()

    # Print Audio Manifest Issues
    if audio_missing:
        print(f"{RED}{BOLD}❌ AUDIO MANIFEST GAPS ({len(audio_missing)} of {audio_total}):{RESET}")
        for issue in audio_missing:
            print(f"  {RED}• {issue}{RESET}")
        print()
    else:
        print(f"{GREEN}{BOLD}✓ Audio manifest: all {audio_total} entries resolve to files on disk!{RESET}\n")

    print(f"{CYAN}{BOLD}══════════════════════════════════════════════════════════════════════════{RESET}")
    if has_errors or naming_issues or orphan_imports or resolution_issues:
        print(f"{YELLOW}{BOLD}Linter completed with pending items to generate or resolve.{RESET}\n")
        return False
    else:
        print(f"{GREEN}{BOLD}🎉 ALL ASSET INTEGRITY AND QUALITY CHECKS PASSED PERFECTLY!{RESET}\n")
        return True


def main():
    parser = argparse.ArgumentParser(description="Phagocyte Asset Quality & Integrity Linter")
    parser.add_argument("--category", "-c", type=str, default=None, help="Filter by category (achievement, skill, passive_tree, gear, ui)")
    parser.add_argument("--summary", "-s", action="store_true", help="Only show category counts without full item lists")
    args = parser.parse_args()

    success = run_all_checks(target_category=args.category, summary_only=args.summary)
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
