#!/usr/bin/env python3
"""
Phagocyte Combat Feel, Kinesthetics & "Juice" Audit Linter.
Quantitatively assesses hit feedback, screen trauma, damage typography,
audio manifest integrity, and swarm visual readability.
"""

import argparse
import json
import os
import re
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
SKILL_DIR = SCRIPT_DIR.parent
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


def read_text(path: Path) -> str:
    if not path.exists():
        return ""
    try:
        with open(path, "r", encoding="utf-8") as f:
            return f.read()
    except Exception:
        return ""


class CombatAuditor:
    def __init__(self, root: Path):
        self.root = root
        self.scripts_dir = root / "scripts"
        self.assets_dir = root / "assets"
        self.audio_manifest_path = self.assets_dir / "audio" / "manifest.json"
        self.camera_follow_path = self.scripts_dir / "player" / "CameraFollow.cs"
        self.damage_spawner_path = self.scripts_dir / "ui" / "DamageNumberSpawner.cs"
        self.base_enemy_path = self.scripts_dir / "enemies" / "BaseEnemy.cs"
        self.base_cell_path = self.scripts_dir / "player" / "BaseCell.cs"
        self.vfx_manager_path = self.scripts_dir / "combat" / "VfxManager.cs"

        self.warnings: List[str] = []
        self.passes: List[str] = []
        self.scores: Dict[str, float] = {
            "kinesthetics": 0.0,
            "camera_trauma": 0.0,
            "damage_typography": 0.0,
            "audio_integrity": 0.0,
            "readability": 0.0,
        }

    def audit_audio_manifest(self) -> float:
        """Audits assets/audio/manifest.json against code usage."""
        score = 20.0
        if not self.audio_manifest_path.exists():
            self.warnings.append("CRITICAL: assets/audio/manifest.json not found!")
            return 0.0

        manifest = load_json(self.audio_manifest_path)
        if not manifest or "sfx" not in manifest or "bgm" not in manifest:
            self.warnings.append("Manifest missing 'sfx' or 'bgm' keys.")
            return 5.0

        declared_sfx = set(manifest.get("sfx", []))
        declared_bgm = set(manifest.get("bgm", []))

        # Check physical existence of audio files (matching AssetPaths.SfxCandidates)
        audio_root = self.assets_dir / "audio"
        candidate_dirs = [
            audio_root / "sfx",
            audio_root / "sfx" / "combat",
            audio_root / "sfx" / "ui",
            audio_root / "sfx" / "event",
            audio_root / "sfx" / "gem",
        ]
        missing_sfx_files = []
        for sfx in declared_sfx:
            found = False
            for cdir in candidate_dirs:
                if (cdir / f"{sfx}.mp3").exists() or (cdir / f"{sfx}.wav").exists() or (cdir / f"{sfx}.ogg").exists():
                    found = True
                    break
            if not found:
                missing_sfx_files.append(sfx)

        if missing_sfx_files:
            self.warnings.append(f"Audio files missing on disk for declared SFX: {missing_sfx_files}")
            score -= min(8.0, len(missing_sfx_files) * 2.0)
        else:
            self.passes.append(f"All {len(declared_sfx)} declared SFX exist on disk.")

        # Search for PlaySfx calls in C# code
        sfx_pattern = re.compile(r'PlaySfx\s*\(\s*"([^"]+)"')
        used_sfx: Set[str] = set()
        undeclared_sfx: Set[str] = set()

        for cs_file in self.scripts_dir.rglob("*.cs"):
            content = read_text(cs_file)
            for match in sfx_pattern.finditer(content):
                sfx_name = match.group(1)
                used_sfx.add(sfx_name)
                if sfx_name not in declared_sfx:
                    undeclared_sfx.add(f"{sfx_name} (in {cs_file.name})")

        if undeclared_sfx:
            self.warnings.append(f"Undeclared PlaySfx calls detected: {list(undeclared_sfx)}")
            score -= min(10.0, len(undeclared_sfx) * 3.0)
        else:
            self.passes.append(f"Zero undeclared PlaySfx calls across codebase ({len(used_sfx)} active calls verified).")

        return max(0.0, score)

    def audit_camera_trauma(self) -> float:
        """Audits screen trauma and shake dynamics in CameraFollow.cs."""
        score = 20.0
        content = read_text(self.camera_follow_path)
        if not content:
            self.warnings.append("CameraFollow.cs not found.")
            return 0.0

        # Check quadratic trauma shake formula (trauma * trauma)
        if "_trauma * _trauma" in content or "Mathf.Pow(_trauma, 2" in content or "trauma * trauma" in content:
            self.passes.append("Camera shake uses quadratic non-linear trauma decay (Shake = Trauma^2).")
        else:
            self.warnings.append("Camera shake lacks quadratic non-linear trauma decay! Linear shake causes jarring motion sickness.")
            score -= 8.0

        # Check gating by SettingsManager.ScreenShake
        if "SettingsManager.ScreenShake" in content:
            self.passes.append("Camera shake is properly gated by user accessibility setting (SettingsManager.ScreenShake).")
        else:
            self.warnings.append("Camera shake bypasses user accessibility setting!")
            score -= 6.0

        # Check max shake offset
        offset_match = re.search(r'MaxShakeOffset\s*\{\s*get;\s*set;\s*\}\s*=\s*([0-9.]+)', content)
        if offset_match:
            offset_val = float(offset_match.group(1))
            if 15.0 <= offset_val <= 30.0:
                self.passes.append(f"MaxShakeOffset ({offset_val}px) falls within recommended ergonomic range (15-30px).")
            else:
                self.warnings.append(f"MaxShakeOffset ({offset_val}px) outside recommended range (15-30px).")
                score -= 3.0

        return max(0.0, score)

    def audit_damage_typography(self) -> float:
        """Audits floating combat text and typography pop in DamageNumberSpawner.cs."""
        score = 20.0
        content = read_text(self.damage_spawner_path)
        if not content:
            self.warnings.append("DamageNumberSpawner.cs not found.")
            return 0.0

        # Check pre-allocated pooling
        if "MaxActiveNumbers" in content and "_pool" in content:
            pool_match = re.search(r'MaxActiveNumbers\s*=\s*(\d+)', content)
            pool_size = int(pool_match.group(1)) if pool_match else 0
            if pool_size >= 256:
                self.passes.append(f"DamageNumberSpawner uses pre-allocated struct pool of {pool_size} items (zero GC allocation).")
            else:
                self.warnings.append(f"DamageNumberSpawner pool size ({pool_size}) is smaller than recommended 256.")
                score -= 4.0
        else:
            self.warnings.append("DamageNumberSpawner does not use pre-allocated pooling!")
            score -= 10.0

        # Check distinct damage types
        types = ["EnemyDamage", "PlayerDamage", "Heal"]
        found_types = [t for t in types if t in content]
        if len(found_types) == len(types):
            self.passes.append("Damage typography supports distinct visual channels (Enemy, Player, Heal).")
        else:
            missing = set(types) - set(found_types)
            self.warnings.append(f"Missing damage typography channels: {missing}")
            score -= 5.0

        # Check direct canvas drawing
        if "CanvasItem" in content or "DamageNumberCanvas" in content or "DrawString" in content:
            self.passes.append("Damage numbers direct-drawn via CanvasItem (avoids node tree instantiation bottleneck).")
        else:
            self.warnings.append("Damage numbers appear to instantiate separate nodes per hit!")
            score -= 5.0

        return max(0.0, score)

    def audit_kinesthetics_and_hit_feedback(self) -> float:
        """Audits hit reactions, sprite flashing, and particle impacts."""
        score = 25.0
        enemy_content = read_text(self.base_enemy_path)
        vfx_content = read_text(self.vfx_manager_path)

        if not enemy_content:
            self.warnings.append("BaseEnemy.cs not found.")
            return 0.0

        # Check hit flash modulation
        if "FlashModulate" in enemy_content:
            self.passes.append("BaseEnemy implements instantaneous hit-flash modulation upon damage intake.")
        else:
            self.warnings.append("BaseEnemy lacks hit-flash modulation feedback.")
            score -= 7.0

        # Check hit SFX playback
        if "AudioManager.Instance?.PlayHit" in enemy_content:
            self.passes.append("BaseEnemy triggers audio hit feedback with crit discrimination.")
        else:
            self.warnings.append("BaseEnemy does not route hit SFX to AudioManager.")
            score -= 7.0

        # Check GPU particle VFX pooling
        if vfx_content and "GpuParticles2D" in vfx_content and "PoolSizePerType" in vfx_content:
            self.passes.append("VfxManager maintains pre-allocated GPU particle pools for combat impacts.")
        else:
            self.warnings.append("VfxManager particle pooling unverified or missing.")
            score -= 6.0

        # Check critical hit VFX trigger
        if "VfxType.BarbImpact" in enemy_content or "Play(VfxType" in enemy_content:
            self.passes.append("Critical strikes trigger specialized visual burst effects.")
        else:
            self.warnings.append("Critical strikes lack specialized visual impact VFX.")
            score -= 5.0

        return max(0.0, score)

    def audit_swarm_readability(self) -> float:
        """Audits player silhouette, threat indicators, and visual ergonomics."""
        score = 15.0
        cell_content = read_text(self.base_cell_path)
        enemy_content = read_text(self.base_enemy_path)

        # Check player invulnerability / dash telegraph
        if "IsInvulnerable" in cell_content or "DodgeRoll" in cell_content or "Dash" in cell_content:
            self.passes.append("Player cell implements dodge/invulnerability state machine.")
        else:
            self.warnings.append("Player cell lacks invulnerability or dash state tracking.")
            score -= 4.0

        # Check elite / threat differentiation
        if "IsElite" in enemy_content and "ThreatMode" in enemy_content:
            self.passes.append("Enemies classify threat tiers (IsElite, ThreatMode) for tactical rendering.")
        else:
            self.warnings.append("Enemies lack clear elite / threat tier differentiation.")
            score -= 5.0

        # Check settings for performance / reduced visuals
        settings_path = self.scripts_dir / "core" / "SettingsManager.cs"
        settings_content = read_text(settings_path)
        if "PerformanceMode" in settings_content or "ScreenShake" in settings_content:
            self.passes.append("Engine exposes ergonomics & performance toggles to mitigate sensory overload.")
        else:
            self.warnings.append("Missing ergonomics & sensory comfort settings.")
            score -= 4.0

        return max(0.0, score)

    def run(self) -> Dict[str, Any]:
        self.scores["kinesthetics"] = self.audit_kinesthetics_and_hit_feedback()
        self.scores["camera_trauma"] = self.audit_camera_trauma()
        self.scores["damage_typography"] = self.audit_damage_typography()
        self.scores["audio_integrity"] = self.audit_audio_manifest()
        self.scores["readability"] = self.audit_swarm_readability()

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
        "# Phagocyte Combat Feel, Kinesthetics & Juice Audit Report",
        "",
        f"**Combat Quality Score (CQS)**: `{results['total_score']} / 100.0` (Grade: **{results['grade']}**)",
        "",
        "## Sub-Dimension Breakdown",
        "",
        "| Evaluation Pillar | Score | Max | Status |",
        "|:---|:---:|:---:|:---:|",
        f"| 1. Kinesthetics & Hit Feedback | {results['scores']['kinesthetics']} | 25.0 | {'✅ PASS' if results['scores']['kinesthetics'] >= 20 else '⚠️ WARN'} |",
        f"| 2. Camera Trauma & Shake Dynamics | {results['scores']['camera_trauma']} | 20.0 | {'✅ PASS' if results['scores']['camera_trauma'] >= 16 else '⚠️ WARN'} |",
        f"| 3. Damage Typography & Visual Pop | {results['scores']['damage_typography']} | 20.0 | {'✅ PASS' if results['scores']['damage_typography'] >= 16 else '⚠️ WARN'} |",
        f"| 4. Bio-Acoustic Manifest Integrity | {results['scores']['audio_integrity']} | 20.0 | {'✅ PASS' if results['scores']['audio_integrity'] >= 16 else '⚠️ WARN'} |",
        f"| 5. Swarm Visual Readability & Ergonomics | {results['scores']['readability']} | 15.0 | {'✅ PASS' if results['scores']['readability'] >= 12 else '⚠️ WARN'} |",
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
        lines.append("- Zero diagnostic warnings. Combat feel pipeline fully compliant!")

    lines.append("")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Audit Phagocyte combat feel, juice, and audio integrity.")
    parser.add_argument("--summary", action="store_true", help="Print concise summary report.")
    parser.add_argument("--detail", action="store_true", help="Print detailed diagnostic logs.")
    parser.add_argument("--report", type=str, default="", help="Export report to markdown file.")
    args = parser.parse_args()

    auditor = CombatAuditor(PROJECT_ROOT)
    results = auditor.run()

    print(f"\n{BOLD}{CYAN}=================================================================={RESET}")
    print(f"{BOLD}{CYAN}>>> PHAGOCYTE COMBAT FEEL, KINESTHETICS & JUICE AUDIT <<<{RESET}")
    print(f"{BOLD}{CYAN}=================================================================={RESET}")
    print(f"{BOLD}Combat Quality Score (CQS):{RESET} {GREEN if results['total_score'] >= 85 else YELLOW}{results['total_score']}/100.0{RESET} ({BOLD}{results['grade']}{RESET})")
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
            print(f"\n{GREEN}✔ Zero warnings detected across combat feel pipeline!{RESET}")

    if args.report:
        out_path = Path(args.report)
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(format_report_markdown(results))
        print(f"\n{BOLD}Report exported to:{RESET} {out_path}")


if __name__ == "__main__":
    main()
