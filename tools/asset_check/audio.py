"""Audio manifest integrity: every BGM track and SFX name listed in
assets/audio/manifest.json (the file AudioManager validates at boot and
TestAudioAssets asserts in-suite) must resolve to a file on disk, using the
same candidate probing order as AssetPaths (C#)."""

import json
from pathlib import Path
from typing import List, Tuple


def _load_manifest(root_dir: Path):
    manifest_path = root_dir / "assets" / "audio" / "manifest.json"
    if not manifest_path.exists():
        return None, [f"manifest.json itself is missing: {manifest_path}"]
    try:
        with open(manifest_path, "r", encoding="utf-8") as f:
            return json.load(f), []
    except Exception as e:
        return None, [f"manifest.json is malformed: {e}"]


def _bgm_candidates(audio_dir: Path, track: str) -> List[Path]:
    return [audio_dir / "bgm" / f"{track}.{ext}" for ext in ("mp3", "ogg", "wav")]


def _sfx_candidates(audio_dir: Path, name: str) -> List[Path]:
    candidates = []
    for sub in ("", "combat", "ui", "gem"):
        for ext in ("mp3", "wav"):
            candidates.append(audio_dir / "sfx" / sub / f"{name}.{ext}" if sub else audio_dir / "sfx" / f"{name}.{ext}")
    return candidates


def check_audio_assets(root_dir: Path = Path(".")) -> Tuple[List[str], int]:
    """Returns (missing_entries, total_entries).

    missing_entries holds 'bgm/<track>' / 'sfx/<name>' strings with no file
    on disk; total_entries counts manifest entries checked.
    """
    manifest, errors = _load_manifest(root_dir)
    if manifest is None:
        return errors, 0

    audio_dir = root_dir / "assets" / "audio"
    missing: List[str] = []
    total = 0

    for track in manifest.get("bgm", []) or []:
        total += 1
        if not any(p.exists() for p in _bgm_candidates(audio_dir, track)):
            missing.append(f"bgm/{track}")

    for name in manifest.get("sfx", []) or []:
        total += 1
        if not any(p.exists() for p in _sfx_candidates(audio_dir, name)):
            missing.append(f"sfx/{name}")

    return missing, total
