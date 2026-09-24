using Godot;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Run timer + top-center capsule (former <c>Hud</c> timer block). Owns the
/// survival clock; driven explicitly by <see cref="Hud"/> (no <c>_Process</c>
/// of its own).
/// </summary>
public partial class WaveTimerView : Node
{
    public PanelContainer? TopCenterCapsule { get; set; }
    public Label? TimerLabel { get; set; }

    public float SurvivalTime { get; set; } = 0.0f;

    // Survival goal shown next to the timer (0 hides it).
    public float GoalSeconds { get; set; } = 0.0f;

    /// <summary>
    /// Endless overdrive: once the standard goal is crossed the timer turns into a
    /// burning dark-gold fluorescence and counts on without a cap (docs/endgame.md §3.1).
    /// </summary>
    public bool EndlessMode { get; set; } = false;

    /// <summary>Wires timer nodes (with legacy-path fallbacks). Call once from Hud._Ready.</summary>
    public void Bind(Node root)
    {
        TopCenterCapsule = root.GetNodeOrNull<PanelContainer>("TopCenterCapsule");
        TimerLabel = root.GetNodeOrNull<Label>("TopCenterCapsule/HBox/TimerLabel");
    }

    /// <summary>Burning dark-gold fluorescence used by the endless overdrive timer.</summary>
    private static Color BurningGoldFluorescence(float time)
    {
        float flicker = 0.85f + 0.15f * Mathf.Sin(time * 6.0f);
        float ember = 0.75f + 0.25f * Mathf.Sin(time * 2.3f + 1.7f);
        return new Color(1.0f * flicker, (0.52f + 0.16f * ember) * flicker, 0.06f, 1.0f);
    }

    /// <summary>Advances the run clock and renders the timer label. Driven by Hud._Process.</summary>
    public void Tick(float delta)
    {
        if (GetTree().Paused)
            return;

        SurvivalTime += delta;
        int minutes = (int)(SurvivalTime / 60.0f);
        int seconds = (int)(SurvivalTime % 60.0f);
        if (TimerLabel != null)
        {
            string timerText = $"⏱️ {minutes:D2}:{seconds:D2}";
            bool overdrive = EndlessMode && GoalSeconds > 0.0f && SurvivalTime >= GoalSeconds;
            if (GoalSeconds > 0.0f)
            {
                if (overdrive)
                {
                    timerText += " / ∞";
                }
                else
                {
                    int goalMinutes = (int)(GoalSeconds / 60.0f);
                    int goalSeconds = (int)(GoalSeconds % 60.0f);
                    timerText += $" / {goalMinutes:D2}:{goalSeconds:D2}";
                }
            }
            TimerLabel.Text = timerText;
            TimerLabel.Modulate = overdrive
                ? BurningGoldFluorescence(SurvivalTime)
                : Colors.White;
        }
    }
}
