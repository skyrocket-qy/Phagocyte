using Godot;
using Godot.Collections;

namespace Game.Directors;

/// <summary>
/// Director-facing HUD surface. Implemented by <c>Game.UI.Hud</c>;
/// lives here so directors never compile against concrete UI types
/// (gameplay → UI is forbidden — see docs/architecture/ARCH_RULE.md).
/// </summary>
public interface IDirectorHud
{
    bool PauseInputSuppressed { get; set; }
    void ResumeGame();
    void ShowOverdriveAlert(string title, string desc);
}

/// <summary>
/// Director-facing settlement modal surface. Implemented by
/// <c>Game.UI.RunRecordsModal</c>; same inversion as
/// <see cref="IDirectorHud"/> — directors resolve it via
/// <c>GetNodeOrNull&lt;IRunSettlementModal&gt;</c>, never the concrete type.
/// </summary>
public interface IRunSettlementModal
{
    void OpenSettlement(Dictionary record);
}

/// <summary>
/// Narrow run-scoped view of the orchestrator that director components are
/// allowed to touch. Implemented by <see cref="GameRoot"/>; breaks the
/// <c>StageEnvironment ↔ GameRoot</c> god-object cycle (suggestion 6) so directors
/// and organ environments can be unit-tested against a mock context.
/// Cap tunables stay on <see cref="GameRoot"/> — directors read them here so no
/// spawn-cap state is ever duplicated.
/// </summary>
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
