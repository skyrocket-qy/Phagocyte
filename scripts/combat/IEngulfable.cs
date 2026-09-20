using Godot;

namespace Phagocyte.Combat;

/// <summary>
/// Typed contract for anything the cell can engulf (phagocytosis).
/// Replaces string duck-typing (<c>HasMethod("be_engulfed")</c>).
/// </summary>
public interface IEngulfable
{
    bool CanBeEngulfed { get; }
    float GetAtpValue();
    int GetBaseScore();
    void BeEngulfed(Node2D? predator);
    void OnEngulfAttemptFailed(Node2D? predator);
}
