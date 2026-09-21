using Godot;
using Phagocyte.UI;

namespace Phagocyte.Directors;

/// <summary>
/// Narrow run-scoped view of the orchestrator that director components are
/// allowed to touch. Implemented by <see cref="Main"/>; breaks the
/// <c>MapEnvironment ↔ Main</c> god-object cycle (suggestion 6) so directors
/// and organ environments can be unit-tested against a mock context.
/// Cap tunables stay on <see cref="Main"/> — directors read them here so no
/// spawn-cap state is ever duplicated.
/// </summary>
public interface IRunContext
{
    Vector2 ArenaSize { get; }
    float EnvironmentTime { get; }
    float RunGoalSeconds { get; }
    bool IsEndlessRun { get; }
    bool RunEnded { get; }
    string MapId { get; }
    string RunDifficulty { get; }
    bool TerminalBossNeutralized { get; }
    CharacterBody2D? Player { get; }
    Node2D? EnemyContainer { get; }
    Camera2D? MainCamera { get; }
    Hud? HudNode { get; }
    int ScreenCapNormal { get; }
    int ScreenCapSwarm { get; }
    Vector2 CurrentFluidVector { get; set; }
    void EndRun(bool victory, string cause = "");
}
