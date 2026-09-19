using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Complement Cascade (補體瀑布)
/// Spawns chemical resonance rings that detonate after a short delay.
/// </summary>
public partial class ComplementCascadeSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 45.0f;
    [Export] public float BaseAreaRadius { get; set; } = 80.0f;

    public ComplementCascadeSkill()
    {
        SkillId = SkillIds.ComplementCascade;
        NameKey = "SKILL_COMPLEMENT_NAME";
        DescKey = "SKILL_COMPLEMENT_DESC";
        BioKey = "SKILL_COMPLEMENT_BIO";
        IconSymbol = "💥";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 4.0f;
        CooldownTimer = 1.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        int amount = GetCalculatedAmount(1);
        float effRadius = GetCalculatedArea(BaseAreaRadius);
        float effDuration = GetCalculatedDuration(1.2f);

        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(50.0, 220.0);
            Vector2 spawnPos = Host.GlobalPosition + offset;
            SpawnResonanceRing(spawnPos, effRadius, effDuration);
        }
    }

    private void SpawnResonanceRing(Vector2 pos, float radius, float delay)
    {
        if (Host == null)
            return;

        var tree = Host.GetTree();
        if (tree == null)
            return;

        // Create a simple timer to trigger MAC detonation
        var timer = tree.CreateTimer(delay);
        timer.Timeout += () =>
        {
            if (Host == null || !GodotObject.IsInstanceValid(Host))
                return;
            DetonateMacRing(pos, radius);
        };
    }

    private void DetonateMacRing(Vector2 center, float radius)
    {
        if (Host == null)
            return;

        TargetingService.ForEachInRadius(center, radius, n =>
        {
            if (n.HasMethod("be_engulfed"))
                n.Call("be_engulfed", Host);
        });
    }
}
