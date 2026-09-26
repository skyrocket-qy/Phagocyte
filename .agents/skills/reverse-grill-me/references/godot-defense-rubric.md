# Godot 4 / C# Oral-Defense Rubric

Question bank for `reverse-grill-me`. Pick 3–5 per defense, covering at
least one item from each pillar. Every question must have a checkable
answer grounded in engine behavior, not opinion.

## Pillar 1 — Lifecycle & Scene Tree

- `_EnterTree` vs `_Ready` vs `_ExitTree`: when does each fire, and what
  breaks when node access happens in the wrong one (not-in-tree errors)?
- What guarantees does `_Notification` ordering give between parent/child?
- If this node is reparented or the packed scene structure changes, which
  `GetNode`/`GetNodeOrNull` paths break? Why `GetNodeOrNull` over `GetNode`?
- What must be disconnected or freed in `_ExitTree`, and what leaks if skipped?

## Pillar 2 — Tick Model & Performance

- `_Process` vs `_PhysicsProcess` for this logic: which tick does it need,
  and what goes wrong on the other (frame-rate dependence, physics jitter)?
- Is `delta` handled (and is it the right delta)? What happens at zero delta
  or a hitch spike?
- Per-frame allocations: any LINQ, string concat, or `GetNode` calls inside
  the tick? What is the garbage/collector cost at 60fps?
- `MoveAndSlide()` on `CharacterBody2D`: why is `Velocity` a property (not a
  parameter), where is delta applied, and why is `IsOnFloor()` only valid
  strictly after the call?

## Pillar 3 — Signals & Async Safety

- Why this connection style (`Callable.From`, editor connection, `Connect`)?
  Who owns the lifetime of each endpoint?
- If either endpoint is freed while connected, what happens? Where is the
  guard?
- `await` on a signal or `ToSignal`: if the node is freed mid-await, what is
  the outcome, and how is it guarded (`IsInstanceValid`, weak refs, tokens)?
- Godot `Array`/`Dictionary` vs .NET collections across the C# boundary:
  which crosses safely, and what breaks at runtime (variant-compat errors)?

## Pillar 4 — State & Edge Cases

- Rapid input (double activation same frame): idempotent, queued, or corrupt?
- Empty/null/external change: missing resource, unassigned export, renamed
  node, locale-switched format strings — which path fires?
- Save/load and re-entry: does this state survive `ReloadFromDisk`, scene
  re-instantiation, or profile switching? What resets, what persists?
- Multi-profile/multi-cell interaction: does per-cell/per-slot isolation hold
  (no shared static leaking between builds)?

## Pass Standard

Correct, self-worded mechanism for every question. A quoted-back answer with
no causal reasoning fails. Two failed attempts → shrink the diff and re-grill.
