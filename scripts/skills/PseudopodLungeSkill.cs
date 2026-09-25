using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Pseudopod Lunge (偽足猛擊)
/// Snaps out an elongated amoebic arm to pull enemies in for phagocytosis.
/// </summary>
public partial class PseudopodLungeSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 30.0f;
    [Export] public float BaseReach { get; set; } = 260.0f;

    public PseudopodLungeSkill()
    {
        SkillId = SkillIds.PseudopodLunge;
        NameKey = "SKILL_LUNGE_NAME";
        DescKey = "SKILL_LUNGE_DESC";
        BioKey = "SKILL_LUNGE_BIO";
        IconSymbol = "🥊";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.0f;
        CooldownTimer = 2.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        int amount = GetCalculatedAmount(1);
        float reach = GetCalculatedArea(BaseReach);
        GetDamage(BaseDamage, out float dmg, out bool isCrit);

        int pulled = 0;
        TargetingService.ForEachInRadius(Host.GlobalPosition, reach, n =>
        {
            if (pulled >= amount)
                return;

            pulled++;

            CombatHelper.DealDamage(n, dmg, Host, isCrit);

            // Chain-strike presentation: same visual language as the grasp.
            // Mechanics stay instant (whip); the chain is pure presentation.
            var chain = new PseudopodChainVisual
            {
                GlobalPosition = Host.GlobalPosition,
                Host = Host,
                Target = n,
                BaseHalfWidth = 16.0f * GetCalculatedArea(1.0f),
                ExtendSpeed = 1800.0f,
                HoldDuration = 0.15f,
                IsBluntFist = true,
                ChainFillColor = SkillAssetPalette.Accent(SkillIds.PseudopodLunge, new Color(0.25f, 0.80f, 0.46f)),
                ChainEdgeColor = SkillAssetPalette.Core(SkillIds.PseudopodLunge, Colors.White)
            };
            Host.GetParent().AddChild(chain);

            // Pull pathogen rapidly toward player (displacement only).
            var tween = Host.CreateTween();
            tween.TweenProperty(n, "global_position", Host.GlobalPosition, 0.15f);
        });
    }
}
