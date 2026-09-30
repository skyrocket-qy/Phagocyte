Architectural Implementation Plan: PoE-Style Damage Pipeline & Asymmetric Projectile LifecycleSystem Architecture[Cast / Fire Step]
  Source Actor (Player / Enemy)
    │
    ▼
  Snapshot to HitPayload (32–48B struct, Value Type)
    ├── Raw Damage, Type, Pen, Flags, AttackerId, 3-slot EffectSpec (Effect0/1/2 + count, zero-alloc)
    │
    ▼
  [Flight Step: ProjectileManager (Data Pool 4096)]
    ├── Linear Ticking (Zero-GC, Hot Path)
    └── Lazy Cleanup Hook (Checks DeadEntityRegistry bitset/hash)
          │
          ▼ Collision Detected
[Hit Step: Central DamagePipeline]
  HitPayload + Target.Stats (ICombatReceiver / IStatusHost)
    ├── 1. Armor / Resistance Mitigation
    ├── 2. Ailment Magnitude / Duration Evaluation (via Target Threshold)
    ├── 3. Health Depletion & Knockback Application
    └── 4. Event Return -> Attacker Actor (Leech, Trigger Hooks)

[Death Event: Asymmetric Despawn Policy]
  Enemy Dies  ──> Push ID to DeadEntityRegistry ──> Projectiles Fizzle via Lazy Cleanup
  Player Dies ──> Grace Period Timer Starts (1.0s) ──> Projectiles Fly & Settle Hits
Phase 1: Data Contracts & Lean HitPayloadDefine immutable, compact transmission structures to decouple flight time from dynamic actor stats.Define HitPayload (struct, 40 bytes target):float RawDamage (4B)DamageType Element (1B enum: Physical, Acid, Fire, Cold, Lightning)HitFlags Flags (1B flags: IsCrit, BypassesArmor, CannotBeReflected)byte Faction (1B: 0 = Player, 1 = Enemy, 2 = Neutral)Vector2 KnockbackImpulse (8B)EffectSpec Effect0/1/2 + int count (3 inline slots, zero-alloc via ApplyAll; stays — no single-slot shrink)ulong AttackerId (8B, entity tracking for leech/kills)Define HitResult (struct, return context):float DamageDealtbool TargetKilledbool IsCritbool IsEvadedbool IsBlockedImplement Startup Def Validation:Build a startup validator ensuring every EffectId referenced by skills/spawners exists inside ailments.json. Throw hard assertions on mismatched string keys during boot.Phase 2: Centralized DamagePipeline EngineEliminate polymorphic resolution and decentralized mitigation logic.Consolidate Calculations into DamagePipeline.cs:Step 1 (Mitigation):Compute mitigation using target's current stats (armor, shields, phase reductions).Step 2 (Ailment Derivation):Do not read baked durations for standard ailments from the payload. Compute status parameters dynamically based on actual post-mitigation damage versus target AilmentThreshold (derived from max HP).Step 3 (Status Dispatch):Dispatch computed parameters directly into target.Status.Apply(id, derivedMag, derivedDur).Step 4 (Feedback & Leech Dispatch):Return HitResult. If AttackerId is alive, trigger attacker-side leech and resource recovery in-place without routing leech parameters through target defensive interfaces.Phase 3: ProjectileManager Lazy Cleanup for Enemy DespawnMaintain high throughput in ProjectileManager.cs without $O(N)$ allocation spikes upon mass-kill events.DeadEntityRegistry Integration:Create a high-density, flat bitset or fixed-capacity ring buffer tracking entities terminated in the current frame.When an enemy Die() triggers, register its ActorId into the registry; run no projectile search loops at the point of death.Inline Tick Pruning in _PhysicsProcess:During the standard contiguous for (int i = 0; i < _activeCount; i++) update loop:C#ulong owner = _projectiles[i].AttackerId;
if (_projectiles[i].Faction == Faction.Enemy && DeadEntityRegistry.IsDead(owner))
{
    SpawnFizzleVfx(_projectiles[i].Position, _projectiles[i].ProjectileTypeIndex);
    // Fast swap-back removal
    _projectiles[i] = _projectiles[_activeCount - 1];
    _activeCount--;
    i--;
    continue;
}
VFX Batching:Use a pooled particle emission buffer or MultiMesh visual pop for fizzling enemy bullets so removal feels intentional rather than a rendering glitch.Phase 4: Player Death Grace Period & Mutual-Kill ResolutionProtect late-game projectile trades and player tactical investments.Decouple Player Projectile Lifetime:Player bullets retain valid flight physics even if PlayerActor enters an inactive/dead state.Prohibit player death from registering into the immediate projectile cancellation registry.Implement GracePeriodController:When player HP hits 0, transition PlayerActor to Downed state:Disable input, collision hurtboxes, and health regen.Trigger a global visual/audio slow-down filter.Start a non-blocking 1.0-second countdown timer.Mutual Kill Arbiter:If a terminal boss or stage-clear condition resolves during this 1.0-second window, intercept the defeat state and award the stage victory.If the timer elapses without meeting the clear threshold, trigger the standard RunSettlementModal.Phase 5: Verification & Architectural BoundariesZero-Allocation Benchmark:Run a 4,000-projectile saturation test. Ensure memory allocations per frame on hot paths remain strictly at 0 bytes (no GC collections in Gen 0).Automated Architecture Rule Audit:Update check_arch.py to assert:No references to ILeechable, IStunnable, or ISlowable.Direct calls to DamagePipeline.ResolveHit() allowed only from combat collision layers.No dictionary lookups per projectile tick inside ProjectileManager.cs.