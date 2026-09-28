using System;

namespace Game.Core;

/// <summary>
/// Cross-manager command bus (Phase 3): achievement side effects (class / map
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
    public static event Action<string>? MapUnlockRequested;
    public static event Action<string>? MapHardUnlockRequested;
    public static event Action? MapsResetRequested;

    private static bool _initialized = false;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;
        _initialized = true;
        ClassUnlockRequested += GameManager.UnlockClass;
        ClassLockRequested += GameManager.LockClass;
        MapUnlockRequested += GameManager.UnlockMap;
        MapHardUnlockRequested += GameManager.UnlockMapHard;
        MapsResetRequested += GameManager.ResetMapUnlocks;
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

    public static void RaiseMapUnlock(string stageId)
    {
        EnsureInitialized();
        MapUnlockRequested?.Invoke(stageId);
    }

    public static void RaiseMapHardUnlock(string stageId)
    {
        EnsureInitialized();
        MapHardUnlockRequested?.Invoke(stageId);
    }

    public static void RaiseMapsReset()
    {
        EnsureInitialized();
        MapsResetRequested?.Invoke();
    }
}
