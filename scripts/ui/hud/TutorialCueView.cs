using Godot;
using Game.Core;
using Game.Enemies;
using Game.Player;

namespace Game.UI;

/// <summary>
/// First-run micro-cues: one-shot dodge hint
/// and the first level-up bullet-time (docs/tutorial.md §2).
/// Driven explicitly by <see cref="Hud"/> (no <c>_Process</c> of its own).
/// </summary>
public partial class TutorialCueView : Node
{
    public TutorialOverlay? TutorialOverlayNode { get; set; }

    public Node2D? PlayerRef { get; set; }
    public UpgradeModal? CellUpgradeModal { get; set; }

    private float _dodgeHintTimer = 0.0f;
    private bool _dodgeHintShown = false;
    private float _nearbyCheckTimer = 0.0f;
    private bool _firstLevelUpCuePlayed = false;
    private Tween? _bulletTimeTween = null;

    /// <summary>True once the first level-up bullet-time cue has fired this run.</summary>
    public bool FirstLevelUpCuePlayed => _firstLevelUpCuePlayed;

    /// <summary>True while the 0.5s bullet-time transition is running.</summary>
    public bool IsBulletTimeActive => _bulletTimeTween != null;

    /// <summary>True once the Dodge roll hint has been shown (once per run).</summary>
    public bool DodgeHintShownOnce => _dodgeHintShown;

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
        if (player is PlayerActor bc)
        {
            bc.LevelUp += (lvl) => OnPlayerLevelUp((int)lvl);
        }
        else if (player.HasSignal("LevelUp"))
        {
            player.Connect("LevelUp", Callable.From((int lvl) => OnPlayerLevelUp(lvl)));
        }
    }

    /// <summary>
    /// Drives the non-intrusive micro-cues (docs/tutorial.md §2):
    /// the one-shot dodge hint.
    /// </summary>
    public void UpdateTutorialCues(float delta)
    {
        if (TutorialOverlayNode == null)
            return;

        TutorialOverlayNode.PlayerRef = PlayerRef;

        // Cue 3: first damage or >15 nearby pathogens -> floating dodge hint
        if (!_dodgeHintShown)
        {
            _nearbyCheckTimer -= delta;
            if (_nearbyCheckTimer <= 0.0f)
            {
                _nearbyCheckTimer = 0.25f;
                bool hurt = PlayerRef is PlayerActor hurtCell && hurtCell.HasTakenDamage;
                if (hurt || CountNearbyEnemies(Hud.DodgeHintNearbyRadius) > Hud.DodgeHintNearbyThreshold)
                    ShowDodgeHint();
            }
        }

        if (_dodgeHintTimer > 0.0f)
            _dodgeHintTimer = Mathf.Max(0.0f, _dodgeHintTimer - delta);

        TutorialOverlayNode.ShowDodgeHint = _dodgeHintTimer > 0.0f;
        TutorialOverlayNode.DodgeHintText = _dodgeHintShown ? LocalizedDodgeHint() : "";
        TutorialOverlayNode.DodgeHintAlpha = Mathf.Min(1.0f, _dodgeHintTimer);
    }

    public void ShowDodgeHint()
    {
        if (_dodgeHintShown)
            return;

        _dodgeHintShown = true;
        _dodgeHintTimer = Hud.DodgeHintSeconds;
        GD.Print("[Tutorial] Dodge roll hint shown.");
    }

    /// <summary>Resets the per-run micro-cue state.</summary>
    public void ResetTutorialCues()
    {
        _dodgeHintTimer = 0.0f;
        _dodgeHintShown = false;
        _nearbyCheckTimer = 0.0f;
        _firstLevelUpCuePlayed = false;
        Engine.TimeScale = 1.0;
    }

    private static string LocalizedDodgeHint()
    {
        bool gamepad = Input.GetConnectedJoypads().Count > 0;
        return TranslationServer.Translate(gamepad ? "HUD_DODGE_HINT_PAD" : "HUD_DODGE_HINT");
    }

    public int CountNearbyEnemies(float radius)
    {
        if (PlayerRef == null)
            return 0;

        float radiusSq = radius * radius;
        int count = 0;
        foreach (var enemy in EnemyActor.ActiveEnemies)
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

