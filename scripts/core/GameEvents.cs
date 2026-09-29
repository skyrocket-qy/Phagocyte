using System;

namespace Game.Core;

/// <summary>Synchronous event bus decoupling meta-progression systems.</summary>
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
