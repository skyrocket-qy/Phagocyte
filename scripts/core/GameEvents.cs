using System;

namespace Game.Core;

/// <summary>
/// Cross-manager command bus (Phase 3): achievement side effects (class / stage
/// unlocks, talent points) travel as synchronous C# events instead of direct
/// static calls, breaking the AchievementManager → GameManager /
/// PassiveTreeManager compile-time edges. Queries (IsUnlocked, unlock text)
/// intentionally stay direct reads — events carry commands, not answers.
/// Subscriptions are installed once via <see cref="EnsureInitialized"/>,
/// which every Raise path calls defensively (autoload order independent).
/// </summary>
public static class GameEvents
{
    public static event Action<string>? ClassUnlockRequested;
    public static event Action<string>? ClassLockRequested;
    public static event Action<string>? StageUnlockRequested;
    public static event Action<string>? StageHardUnlockRequested;
    public static event Action? StagesResetRequested;

    private static bool _initialized = false;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;
        _initialized = true;
        ClassUnlockRequested += GameManager.UnlockClass;
        ClassLockRequested += GameManager.LockClass;
        StageUnlockRequested += GameManager.UnlockStage;
        StageHardUnlockRequested += GameManager.UnlockStageHard;
        StagesResetRequested += GameManager.ResetStageUnlocks;
    }

    public static void RaiseClassUnlock(string classId)
    {
        EnsureInitialized();
        ClassUnlockRequested?.Invoke(classId);
    }

    public static void RaiseClassLock(string classId)
    {
        EnsureInitialized();
        ClassLockRequested?.Invoke(classId);
    }

    public static void RaiseStageUnlock(string stageId)
    {
        EnsureInitialized();
        StageUnlockRequested?.Invoke(stageId);
    }

    public static void RaiseStageHardUnlock(string stageId)
    {
        EnsureInitialized();
        StageHardUnlockRequested?.Invoke(stageId);
    }

    public static void RaiseStagesReset()
    {
        EnsureInitialized();
        StagesResetRequested?.Invoke();
    }
}
