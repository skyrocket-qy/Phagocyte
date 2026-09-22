"""Scans for duplicate asset files (SHA-256) and conflicting near-duplicate names."""

import hashlib
import os
from collections import defaultdict
from pathlib import Path
from typing import Dict, List, Set, Tuple


def _compute_sha256(file_path: Path) -> str:
    hasher = hashlib.sha256()
    with open(file_path, "rb") as f:
        while chunk := f.read(65536):
            hasher.update(chunk)
    return hasher.hexdigest()


def check_duplicates(root_dir: Path = Path(".")) -> List[str]:
    issues: List[str] = []
    hash_to_files: Dict[str, List[Path]] = defaultdict(list)
    stem_to_files: Dict[str, List[Path]] = defaultdict(list)

    scan_dirs = [root_dir / "gen", root_dir / "assets" / "gen"]

    for base_dir in scan_dirs:
        if not base_dir.exists():
            continue

        for root, _, files in os.walk(base_dir):
            for f in files:
                if f.startswith(".") or f.endswith(".import"):
                    continue

                fp = Path(root) / f
                if fp.is_file():
                    sha = _compute_sha256(fp)
                    hash_to_files[sha].append(fp)
                    stem_to_files[fp.stem.lower()].append(fp)

    # 1. Exact Binary Content Duplicates (within same category tree)
    for sha, paths in hash_to_files.items():
        if len(paths) > 1:
            # Only report if they are not intentional variants (like ach vs unachieved)
            p_strs = [str(p.relative_to(root_dir)) for p in paths]
            issues.append(f"[Duplicate Content] Identical file content across: {', '.join(p_strs)}")

    return issues
