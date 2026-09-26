---
name: reverse-grill-me
description: >-
  Post-generation oral defense for unfamiliar domains (Godot 4 / C#). The AI
  generates code, locks the diff, then grills YOU on it — 3 to 5 questions,
  one at a time — before anything may be committed. Use whenever AI writes
  code you couldn't have written yourself.
---

# Reverse Grill-Me: Oral Defense Before Commit

## Overview

Vibe-coding produces the illusion of competence: code appears, the scene
runs, nothing was learned. This skill inverts the grill — instead of the AI
interrogating your requirements, it interrogates **your understanding** of
the code it just generated. Nothing merges until you can defend every
load-bearing line. Think thesis defense, not code review.

Generated code under this skill is **uncommittable** until the defense passes.

## Protocol

1. **Generate, then lock.** Produce the implementation, then freeze it. No
   further edits until the defense concludes (typos excepted).
2. **Ask 3–5 questions, one at a time.** Wait for each answer. Never batch,
   never lecture — the requester must formulate explanations in their own words.
3. **Every question must hit one of these angles** (see
   [Godot defense rubric](./references/godot-defense-rubric.md)):
   - *Choice vs alternatives:* why this node, lifecycle hook, or C# type —
     and why not the obvious alternatives?
   - *Engine pitfalls:* signals/GC leaks, per-frame costs, garbage pressure.
   - *Edge cases:* node leaves the tree, zero delta, rapid input, freed
     await targets, locale/format surprises.
4. **Grade honestly.** Shallow or wrong answers get pushback with a pointer
   (Godot docs class/method, not a re-explanation). Pass requires correct,
   self-worded reasoning on every question.
5. **On pass:** unlock the diff and merge. On fail after two honest attempts:
   shrink the change (delete what can't be defended, rewrite it by hand) and
   re-grill the smaller surface.

## Anti-Patterns (Refuse These)

- Rubber-stamp reviews ("looks fine, compiles, runs") — that is the trap.
- Answering your own questions. Ask, wait, grade.
- Jargon-first questions ("pushdown automaton or hierarchical FSM?") when the
  requester lacks the ecosystem baseline. Calibrate: Godot lifecycle, signals,
  C# specifics, tick model.
- Letting math-heavy or LINQ-dense blocks slide with "trust me." Those get
  grilled hardest.
