using Godot;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.UI;

/// <summary>
/// First-run micro-cues: WASD breathing ring, one-shot Squeeze hint,
/// fluid-shear arrows and the first level-up bullet-time (docs/tutorial.md §2).
/// Driven explicitly by <see cref="Hud"/> (no <c>_Process</c> of its own).
/// </summary>
public partial class TutorialCueView : Node
{
    public TutorialOverlay? TutorialOverlayNode { get; set; }

    public Node2D? PlayerRef { get; set; }
    public UpgradeModal? CellUpgradeModal { get; set; }

    private float _moveCueTimer = 0.0f;
    private float _squeezeHintTimer = 0.0f;
    private bool _squeezeHintShown = false;
    private float _nearbyCheckTimer = 0.0f;
    private bool _firstLevelUpCuePlayed = false;
    private Tween? _bulletTimeTween = null;

    /// <summary>True once the first level-up bullet-time cue has fired this run.</summary>
    public bool FirstLevelUpCuePlayed => _firstLevelUpCuePlayed;

    /// <summary>True while the 0.5s bullet-time transition is running.</summary>
    public bool IsBulletTimeActive => _bulletTimeTween != null;

    /// <summary>True once the Squeeze Mode hint has been shown (once per run).</summary>
    public bool SqueezeHintShownOnce => _squeezeHintShown;

    /// <summary>Bullet-time ease duration for the first level-up cue (docs: 0.5s).</summary>
    public float LevelUpBulletTimeSeconds { get; set; } = 0.5f;

    /// <summary>When true the first level-up opens the draft immediately (headless tests).</summary>
    public bool SkipLevelUpBulletTime { get; set; } = false;

    /// <summary>Raised on every level-up so the coordinator can refresh vitals state.</summary>
    public event System.Action<int>? LeveledUp;

    /// <summary>Creates the screen-space cue overlay. Call once from Hud._Ready.</summary>
    public void Bind()
    {
        TutorialOverlayNode = new TutorialOverlay { Name = "TutorialOverlay" };
        AddChild(TutorialOverlayNode);
    }

    /// <summary>Typed player subscription (called from Hud.ConnectPlayer).</summary>
    public void ConnectPlayer(Node2D player)
    {
        PlayerRef = player;
        if (player is BaseCell bc)
        {
            bc.LevelUp += (lvl) => OnPlayerLevelUp((int)lvl);
        }
        else if (player.HasSignal("LevelUp"))
        {
            player.Connect("LevelUp", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));
        }
        else if (player.HasSignal("level_up"))
        {
            player.Connect("level_up", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));
        }
    }

    /// <summary>
    /// Drives the non-intrusive micro-cues (docs/tutorial.md §2): the opening
    /// WASD ring, the one-shot Squeeze hint and the fluid-shear arrow trails.
    /// </summary>
    public void UpdateTutorialCues(float delta, Vector2 fluidVector)
    {
        if (TutorialOverlayNode == null)
            return;

        TutorialOverlayNode.PlayerRef = PlayerRef;

        // Cue 1: opening 5 seconds breathing ring
        if (_moveCueTimer > 0.0f)
            _moveCueTimer = Mathf.Max(0.0f, _moveCueTimer - delta);
        TutorialOverlayNode.ShowMoveCue = _moveCueTimer > 0.0f && PlayerRef != null;
        TutorialOverlayNode.MoveCueProgress = _moveCueTimer / Hud.MoveCueSeconds;

        // Cue 3: first damage or >15 nearby pathogens -> floating squeeze hint
        if (!_squeezeHintShown)
        {
            _nearbyCheckTimer -= delta;
            if (_nearbyCheckTimer <= 0.0f)
            {
                _nearbyCheckTimer = 0.25f;
                bool hurt = PlayerRef is BaseCell hurtCell && hurtCell.HasTakenDamage;
                if (hurt || CountNearbyPathogens(Hud.SqueezeHintNearbyRadius) > Hud.SqueezeHintNearbyThreshold)
                    ShowSqueezeHint();
            }
        }

        if (_squeezeHintTimer > 0.0f)
            _squeezeHintTimer = Mathf.Max(0.0f, _squeezeHintTimer - delta);

        TutorialOverlayNode.ShowSqueezeHint = _squeezeHintTimer > 0.0f;
        TutorialOverlayNode.SqueezeHintText = _squeezeHintShown ? LocalizedSqueezeHint() : "";
        TutorialOverlayNode.SqueezeHintAlpha = Mathf.Min(1.0f, _squeezeHintTimer);

        // Cue 5: fluid-shear arrow trails while an environmental field is active
        TutorialOverlayNode.FluidVector = fluidVector;
        TutorialOverlayNode.FluidFieldActive = fluidVector.LengthSquared() > 0.01f;
    }

    public void ShowSqueezeHint()
    {
        if (_squeezeHintShown)
            return;

        _squeezeHintShown = true;
        _squeezeHintTimer = Hud.SqueezeHintSeconds;
        GD.Print("[Tutorial] Squeeze Mode hint shown.");
    }

    /// <summary>Resets the per-run micro-cue state and restarts the move cue window.</summary>
    public void ResetTutorialCues()
    {
        _moveCueTimer = Hud.MoveCueSeconds;
        _squeezeHintTimer = 0.0f;
        _squeezeHintShown = false;
        _nearbyCheckTimer = 0.0f;
        _firstLevelUpCuePlayed = false;
        Engine.TimeScale = 1.0;
    }

    private static string LocalizedSqueezeHint()
    {
        bool gamepad = Input.GetConnectedJoypads().Count > 0;
        return TranslationServer.Translate(gamepad ? "HUD_SQUEEZE_HINT_PAD" : "HUD_SQUEEZE_HINT");
    }

    public int CountNearbyPathogens(float radius)
    {
        if (PlayerRef == null)
            return 0;

        float radiusSq = radius * radius;
        int count = 0;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!GodotObject.IsInstanceValid(enemy))
                continue;
            if (enemy.GlobalPosition.DistanceSquaredTo(PlayerRef.GlobalPosition) <= radiusSq)
                count++;
        }
        return count;
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        LeveledUp?.Invoke(newLevel);
        if (CellUpgradeModal == null || !GodotObject.IsInstanceValid(CellUpgradeModal) || PlayerRef == null)
            return;

        // Cue 2 (docs/tutorial.md §2): the first level-up eases into 0.5s of
        // bullet time, freezes on the draft, then the 3-choice modal opens.
        if (!_firstLevelUpCuePlayed)
        {
            _firstLevelUpCuePlayed = true;
            if (SkipLevelUpBulletTime)
            {
                CellUpgradeModal.OpenUpgradeModal(PlayerRef);
                return;
            }
            PlayLevelUpBulletTime();
            return;
        }

        CellUpgradeModal.OpenUpgradeModal(PlayerRef);
    }

    private void PlayLevelUpBulletTime()
    {
        _bulletTimeTween?.Kill();
        _bulletTimeTween = CreateTween();
        _bulletTimeTween.SetIgnoreTimeScale(true);
        _bulletTimeTween.TweenMethod(
                Callable.From<float>(v => Engine.TimeScale = v), 1.0f, 0.05f, LevelUpBulletTimeSeconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _bulletTimeTween.TweenCallback(Callable.From(() =>
        {
            _bulletTimeTween = null;
            if (CellUpgradeModal != null && GodotObject.IsInstanceValid(CellUpgradeModal) && PlayerRef != null)
                CellUpgradeModal.OpenUpgradeModal(PlayerRef);
        }));
        GD.Print("[Tutorial] First level-up bullet time engaged.");
    }

    public void RestoreTimeScaleAfterLevelUp()
    {
        _bulletTimeTween?.Kill();
        _bulletTimeTween = null;

        var tween = CreateTween();
        tween.SetIgnoreTimeScale(true);
        tween.TweenMethod(Callable.From<float>(v => Engine.TimeScale = v), Engine.TimeScale, 1.0f, 0.3f);
    }
}
