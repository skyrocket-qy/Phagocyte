using Godot;
using Godot.Collections;

namespace Game.Directors;

/// <summary>Director-facing HUD surface interface.</summary>
public interface IDirectorHud
{
    bool PauseInputSuppressed { get; set; }
    void ResumeGame();
    void ShowOverdriveAlert(string title, string desc);
}

/// <summary>Director-facing run settlement modal interface.</summary>
public interface IRunSettlementModal
{
    void OpenSettlement(Dictionary record);
}

/// <summary>Run-scoped context interface for directors and stages.</summary>
public interface IRunContext
{
    Vector2 ArenaSize { get; }
    float EnvironmentTime { get; }
    float RunGoalSeconds { get; }
    bool IsEndlessRun { get; }
    bool RunEnded { get; }
    string StageId { get; }
    string RunDifficulty { get; }
    bool TerminalBossNeutralized { get; }
    CharacterBody2D? Player { get; }
    Node2D? EnemyContainer { get; }
    Camera2D? MainCamera { get; }
    IDirectorHud? HudNode { get; }
    int ScreenCapNormal { get; }
    int ScreenCapSwarm { get; }
    void EndRun(bool victory, string cause = "");
}
