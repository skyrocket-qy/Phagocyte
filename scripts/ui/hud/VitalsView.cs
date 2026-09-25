using Godot;
using Phagocyte.Core;
using Phagocyte.Player;

namespace Phagocyte.UI;

/// <summary>
/// HP / EXP / level / vitals readout (former <c>Hud</c> vitals block).
/// Owns its node references, cached render state and buff tag; driven
/// explicitly by <see cref="Hud"/> (no <c>_Process</c> of its own so the
/// coordinator controls tick order and headless tests can drive it manually).
/// </summary>
public partial class VitalsView : Node
{
    public Label? HpTitleLabel { get; set; }
    public ProgressBar? HpBar { get; set; }
    public Label? HpLabel { get; set; }
    public Label? ExpTitleLabel { get; set; }
    public ProgressBar? ExpBar { get; set; }
    public Label? ExpLabel { get; set; }
    public Label? KillLabel { get; set; }
    public ProgressBar? BottomExpBar { get; set; }
    public ColorRect? VignetteRect { get; set; }
    public Label? BuffTag { get; set; }

    // Cached last stats for re-rendering upon language change
    public float LastHealth { get; set; } = 100.0f;
    public float LastMaxHealth { get; set; } = 100.0f;
    public float LastCurrentExp { get; set; } = 0.0f;
    public float LastExpToNext { get; set; } = 30.0f;
    public int LastLevel { get; set; } = 1;
    public float LastRadiusRatio { get; set; } = 1.0f;
    public int LastDigestedCount { get; set; } = 0;
    public float LastSpeed { get; set; } = 230.0f;

    public Node2D? PlayerRef { get; set; }

    private float _lastVignetteIntensity = -1.0f;
    private int _lastBuffKind = int.MinValue;
    private int _lastBuffTenths = int.MinValue;

    /// <summary>
    /// Text-shaping throttle: HP/EXP labels reshape + relayout their containers
    /// on every write, which measured 326us per StatsChanged emit (contact storm
    /// = hundreds of emits/sec). Bars update immediately (cheap); text lines
    /// refresh at most every 100ms via <see cref="SyncPendingTexts"/>, driven by
    /// Hud._Process. Death and language changes force an immediate refresh.
    /// </summary>
    private const int TextRefreshIntervalMsec = 100;
    private ulong _lastTextMsec;
    private bool _pendingVitalText;
    private bool _pendingExpText;

    /// <summary>Raised after any vitals update so the coordinator can refresh dependent overlays.</summary>
    public event System.Action? VitalsChanged;

    /// <summary>Wires vitals nodes (with legacy-path fallbacks). Call once from Hud._Ready.</summary>
    public void Bind(Node root)
    {
        HpTitleLabel = root.GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPTopHBox/HPTitleLabel");
        HpBar = root.GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPBar");
        HpLabel = root.GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPTopHBox/HPLabel")
            ?? root.GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/HPContainer/HPLabel");

        ExpTitleLabel = root.GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/EXPContainer/EXPTopHBox/EXPTitleLabel");
        ExpBar = root.GetNodeOrNull<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/EXPContainer/EXPBar");
        ExpLabel = root.GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/EXPContainer/EXPTopHBox/EXPLabel");

        KillLabel = root.GetNodeOrNull<Label>("TopCenterCapsule/HBox/KillLabel");
        BottomExpBar = root.GetNodeOrNull<ProgressBar>("BottomExpBar");
        VignetteRect = root.GetNodeOrNull<ColorRect>("CriticalHpVignette");
        BuffTag = root.GetNodeOrNull<Label>("MarginContainer/PanelContainer/VBoxContainer/BuffContainer/BuffTag");
    }

    /// <summary>Typed player subscription + initial render (called from Hud.ConnectPlayer).</summary>
    public void ConnectPlayer(BaseCell bc)
    {
        PlayerRef = bc;
        bc.StatsChanged += (h, mh, r) => OnPlayerStatsChanged(h, mh, r);
        bc.PathogenDigested += (p, atp) => OnPathogenDigested(p, atp);
        bc.ExpChanged += (cur, max, lvl) => OnPlayerExpChanged(cur, max, lvl);

        LastHealth = bc.Health;
        LastMaxHealth = bc.Stats != null ? bc.Stats.GetStat("max_health") : bc.MaxHealth;
        LastCurrentExp = bc.CurrentExp;
        LastExpToNext = bc.ExpToNextLevel;
        LastLevel = bc.CurrentLevel;
        LastSpeed = bc.CurrentSpeed > 0 ? bc.CurrentSpeed : bc.BaseSpeed;
        LastRadiusRatio = bc.CurrentRadius / Mathf.Max(1.0f, bc.BaseRadius);
        LastDigestedCount = bc.DigestedCount;

        if (HpBar != null)
        {
            HpBar.MaxValue = LastMaxHealth;
            HpBar.Value = LastHealth;
        }
        UpdateExpDisplay();
        RenderVitalLines(LastHealth, LastMaxHealth);
        RenderCountLines(LastDigestedCount);
    }

    /// <summary>GDScript-fallback subscription (called from Hud.ConnectPlayer).</summary>
    public void ConnectFallback(Node2D player)
    {
        PlayerRef = player;
        if (player.HasSignal("StatsChanged"))
            player.Connect("StatsChanged", Callable.From((float h, float mh, float r) => OnPlayerStatsChanged(h, mh, r)));

        if (player.HasSignal("PathogenDigested"))
            player.Connect("PathogenDigested", Callable.From((Node2D p, float atp) => OnPathogenDigested(p, atp)));

        if (player.HasSignal("ExpChanged"))
            player.Connect("ExpChanged", Callable.From((float c, float m, int l) => OnPlayerExpChanged(c, m, l)));
    }

    /// <summary>Low-HP vignette pulse (&lt;30% HP). Driven by Hud._Process with the run clock.</summary>
    public void TickVignette(float time)
    {
        if (VignetteRect != null && VignetteRect.Material is ShaderMaterial vignetteMat)
        {
            float hpRatio = LastMaxHealth > 0 ? LastHealth / LastMaxHealth : 1.0f;
            float intensity;
            if (hpRatio < 0.30f && LastHealth > 0.0f)
            {
                float dangerFactor = 1.0f - (hpRatio / 0.30f);
                float pulse = 0.45f + 0.35f * Mathf.Sin(time * 7.5f);
                intensity = Mathf.Clamp(dangerFactor * pulse, 0.0f, 1.0f);
            }
            else
            {
                intensity = 0.0f;
            }
            if (Mathf.Abs(intensity - _lastVignetteIntensity) < 0.01f)
                return;
            _lastVignetteIntensity = intensity;
            vignetteMat.SetShaderParameter("pulse_intensity", intensity);
        }
    }

    public void UpdateLocalizedTexts()
    {
        if (HpTitleLabel != null) HpTitleLabel.Text = Tr("HUD_HP_TITLE");
        _lastTextMsec = 0;
        RenderVitalLines(LastHealth, LastMaxHealth);
        if (ExpTitleLabel != null) ExpTitleLabel.Text = Tr("HUD_EXP_TITLE");
        _lastTextMsec = 0;
        RefreshExpText(true);
        RenderCountLines(LastDigestedCount);
        UpdateBuffStatus();
    }

    public void UpdateExpDisplay()
    {
        if (ExpBar != null)
        {
            ExpBar.MaxValue = Mathf.Max(1.0f, LastExpToNext);
            ExpBar.Value = LastCurrentExp;
        }
        if (BottomExpBar != null)
        {
            BottomExpBar.MaxValue = Mathf.Max(1.0f, LastExpToNext);
            BottomExpBar.Value = LastCurrentExp;
        }
        RefreshExpText(false);
    }

    /// <summary>EXP text line, throttled unless forced.</summary>
    private void RefreshExpText(bool force)
    {
        if (ExpLabel == null)
            return;
        ulong now = Time.GetTicksMsec();
        if (!force && now - _lastTextMsec < TextRefreshIntervalMsec)
        {
            _pendingExpText = true;
            return;
        }
        _lastTextMsec = now;
        _pendingExpText = false;
        int percent = (int)((LastCurrentExp / Mathf.Max(1.0f, LastExpToNext)) * 100.0f);
        ExpLabel.Text = TextFormatter.Format(Tr("HUD_EXP_VAL"), (int)LastCurrentExp, (int)LastExpToNext, percent);
    }

    /// <summary>Flushes throttled text lines. Called from Hud._Process.</summary>
    public void SyncPendingTexts()
    {
        if (!_pendingVitalText && !_pendingExpText)
            return;
        ulong now = Time.GetTicksMsec();
        if (now - _lastTextMsec < TextRefreshIntervalMsec)
            return;
        _lastTextMsec = now;
        if (_pendingVitalText)
        {
            _pendingVitalText = false;
            RenderVitalLines(LastHealth, LastMaxHealth);
        }
        if (_pendingExpText)
        {
            _pendingExpText = false;
            int percent = (int)((LastCurrentExp / Mathf.Max(1.0f, LastExpToNext)) * 100.0f);
            if (ExpLabel != null)
                ExpLabel.Text = TextFormatter.Format(Tr("HUD_EXP_VAL"), (int)LastCurrentExp, (int)LastExpToNext, percent);
        }
    }

    public void RenderVitalLines(float health, float maxHealth)
    {
        if (HpLabel == null)
            return;
        // Death forces through: end screens read the final line.
        bool force = health <= 0.0f;
        ulong now = Time.GetTicksMsec();
        if (!force && now - _lastTextMsec < TextRefreshIntervalMsec)
        {
            _pendingVitalText = true;
            return;
        }
        _lastTextMsec = now;
        _pendingVitalText = false;
        HpLabel.Text = TextFormatter.Format(Tr("HUD_HP_VAL"), (int)health, (int)maxHealth);
    }

    public void RenderCountLines(int digested)
    {
        if (KillLabel != null) KillLabel.Text = TextFormatter.Format(Tr("HUD_KILL_COUNT"), digested);
    }

    public void UpdateBuffStatus()
    {
        if (BuffTag == null) return;

        int kind = 0;
        float timer = 0.0f;
        if (PlayerRef is BaseCell bc && bc.TbBurnTimer > 0.0f)
        {
            kind = 1;
            timer = bc.TbBurnTimer;
        }
        else if (PlayerRef is BaseCell bc2 && bc2.InvertControlsTimer > 0.0f)
        {
            kind = 2;
            timer = bc2.InvertControlsTimer;
        }
        else if (PlayerRef is BaseCell bc3 && bc3.SlowTimer > 0.0f)
        {
            kind = 3;
            timer = bc3.SlowTimer;
        }

        int tenths = kind == 0 ? 0 : Mathf.RoundToInt(timer * 10.0f);
        if (kind == _lastBuffKind && tenths == _lastBuffTenths)
            return;
        _lastBuffKind = kind;
        _lastBuffTenths = tenths;

        if (kind == 1 && PlayerRef is BaseCell tb)
        {
            BuffTag.Visible = true;
            BuffTag.Text = TextFormatter.Format(Tr("HUD_TB_DEBUFF"), tb.TbBurnTimer);
            BuffTag.Modulate = new Color(1.0f, 0.35f, 0.35f, 0.95f);
        }
        else if (kind == 2 && PlayerRef is BaseCell bcInv)
        {
            BuffTag.Visible = true;
            BuffTag.Text = $"🌀 {Tr("STATUS_CONFUSION")}: {bcInv.InvertControlsTimer:F1}s";
            BuffTag.Modulate = new Color(0.85f, 0.45f, 1.0f, 0.95f);
        }
        else if (kind == 3 && PlayerRef is BaseCell bcSlow)
        {
            BuffTag.Visible = true;
            BuffTag.Text = $"🐌 {Tr("STATUS_SLOW")}: {bcSlow.SlowTimer:F1}s";
            BuffTag.Modulate = new Color(0.4f, 0.8f, 0.5f, 0.95f);
        }
        else
        {
            BuffTag.Visible = false;
            BuffTag.Text = "";
        }
    }

    public void OnPlayerLeveledUp(int newLevel)
    {
        LastLevel = newLevel;
        UpdateExpDisplay();
    }

    public void OnPlayerExpChanged(float currentExp, float expToNext, int level)
    {
        LastCurrentExp = currentExp;
        LastExpToNext = expToNext;
        LastLevel = level;
        UpdateExpDisplay();
        VitalsChanged?.Invoke();
    }

    public void OnPlayerStatsChanged(float health, float maxHealth, float radiusRatio)
    {
        LastHealth = health;
        LastMaxHealth = maxHealth;
        LastRadiusRatio = radiusRatio;

        if (PlayerRef is BaseCell bc)
        {
            LastSpeed = bc.CurrentSpeed;
        }

        if (HpBar != null)
        {
            HpBar.MaxValue = maxHealth;
            HpBar.Value = health;
        }

        RenderVitalLines(health, maxHealth);
        VitalsChanged?.Invoke();
    }

    public void OnPathogenDigested(Node2D _enemy, float _atp)
    {
        if (PlayerRef is BaseCell bc)
        {
            if (bc.DigestedCount == LastDigestedCount)
                return;
            LastDigestedCount = bc.DigestedCount;
            RenderCountLines(LastDigestedCount);
            return;
        }
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player != null)
        {
            var digProp = player.Get("digested_count");
            if (digProp.VariantType == Variant.Type.Int)
            {
                int count = digProp.AsInt32();
                if (count == LastDigestedCount)
                    return;
                LastDigestedCount = count;
                RenderCountLines(LastDigestedCount);
            }
        }
    }
}
