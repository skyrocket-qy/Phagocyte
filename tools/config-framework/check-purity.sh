#!/bin/sh
# Guard: the framework must contain zero game-specific references.
# Fails on game-layer import paths or game-vocabulary tokens.
set -eu
cd "$(dirname "$0")"

fail=0

echo "--- purity: game-layer import paths ---"
if grep -rn --include="*.ts" -E "(from|import)[^\"']*[\"']\.\./(ids|schemas|data)/|/(ids|schemas|data)/" src/; then
  fail=1
else
  echo "clean"
fi

echo "--- purity: game-vocabulary tokens ---"
if grep -rni --include="*.ts" -E "phagocyte|macrophage|vistrace|StatId|GearId|EnemyId|SkillId|ClassId|StageId|TraitId|AchievementId|SfxId|UiId|BgmId|AilmentId" src/; then
  fail=1
else
  echo "clean"
fi

if [ "$fail" -ne 0 ]; then
  echo "PURITY FAILED: game-specific references found in config-framework."
  exit 1
fi
echo "PURITY PASSED: config-framework is game-free."
