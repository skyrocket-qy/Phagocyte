---
name: grill-me
description: >-
  Pre-build requirements interrogation (Matt Pocock pattern). The AI asks
  sharp scoping questions before writing any code. Use for familiar domains
  where you hold the internal compiler, or any task with ambiguous
  requirements, edge cases, or architectural trade-offs.
---

# Grill-Me: Spec Before Code

## Overview

No code until the spec is crisp. When this skill is invoked, the AI acts as
a strict staff engineer running a requirements review. It interrogates the
requester — scope, edge cases, non-goals, trade-offs — and only builds once
ambiguity is resolved. Prevents "programming by coincidence" and the
Jenga-tower rework it causes.

Use `grill-me` when **you** understand the domain (backend systems, APIs,
pipelines) and the risk is underspecification. When the domain itself is
unfamiliar (Godot 4 / C# game code), use `reverse-grill-me` instead.

## Protocol

1. **Lock the keyboard.** No implementation, no scaffolding, no "quick draft"
   until the defense below passes.
2. **Ask 3–7 questions, one at a time.** Wait for each answer before asking
   the next. One question per turn keeps the interrogation honest.
3. **Cover, at minimum:**
   - Goal in one sentence: what changes for the user?
   - Scope boundaries: what is explicitly out of scope (non-goals)?
   - Edge cases: empty input, failure modes, concurrency, scale limits.
   - Trade-offs: which of the 2–3 viable approaches fits, and why not the others?
   - Verification: how will we prove it works (suite, repro, metric)?
4. **Challenge guesses.** If an answer is a guess on load-bearing requirements,
   say so and demand a decision or a documented assumption.
5. **Emit the locked spec.** A short bullet list: goal, non-goals, decisions,
   verification. Only then build — and the build must not silently expand scope.

## Anti-Patterns (Refuse These)

- Rubber-stamping: answering "whatever you recommend" on a load-bearing
  question. Push back and force a real decision.
- Spec creep mid-build: new requirements restart the grill, they don't ride along.
- Jargon traps: never ask a question the requester can't evaluate (e.g. don't
  ask "hierarchical or pushdown automaton?" to someone who doesn't know the
  ecosystem). Calibrate to their domain.
