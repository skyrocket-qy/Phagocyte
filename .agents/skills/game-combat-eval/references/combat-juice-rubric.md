# Combat Juice, Kinesthetics & Micro-Dynamics Rubric (Phagocyte)

## Overview

This rubric defines the quantitative and qualitative grading criteria (Levels 1 to 5) for Project: Phagocyte's combat feel, tactile kinesthetics, camera trauma, damage typography, and bio-acoustic soundscape.

---

## 1. Hit-Stop, Impact Kinesthetics & Micro-Pauses

| Level | Grade | Quantitative & Qualitative Standard |
|:---:|:---:|:---|
| **Level 5** | **Exemplary (A+)** | **Micro-pauses**: 30ms for standard attacks, 50ms for critical strikes, 70ms for lethal executions/engulfment. Cell mass is physically felt via directional recoil impulses (e.g. Macrophage pseudopod lunge has solid counter-inertia; Neutrophil high acceleration). Pathogens react with instantaneous hit-flash modulation (`Color(1.8, 0.4, 0.4, 1.0)`) and directional knockback. |
| **Level 4** | **Proficient (A)** | Hit-stop is noticeable on critical hits (30-50ms) and sprite flash occurs reliably. Knockback is present, but class mass differentiation feels slightly uniform across all 5 classes. |
| **Level 3** | **Acceptable (B)** | Damage registers instantly without hit-stop. Enemies flash on damage, but impacts lack tangible weight or inertia. Locomotion feels somewhat like sliding on flat ice rather than navigating viscous interstitial fluid. |
| **Level 2** | **Deficient (C)** | No hit-stop. Flash modulation is either missing, delayed by multiple frames, or causes noticeable stutter. Recoil is absent or disorienting. |
| **Level 1** | **Unacceptable (F)** | Total disconnect between attack trigger and pathogen reaction. Zero visual or physical impact feedback. Enemies float through attacks unaffected. |

---

## 2. Screen Trauma & Camera Dynamics

| Level | Grade | Quantitative & Qualitative Standard |
|:---:|:---:|:---|
| **Level 5** | **Exemplary (A+)** | **Quadratic Trauma Decay**: Camera shake strictly computes as $\text{Shake} = \text{Trauma}^2$, producing intense initial punch with smooth non-linear damping. `MaxShakeOffset` is calibrated within 18–25px. Screen shake is strictly gated by `SettingsManager.ScreenShake` toggle. Heavy impacts (boss slams, massive lysis detonations) apply directional camera bias before rotational noise. |
| **Level 4** | **Proficient (A)** | Quadratic trauma shake is implemented and user-toggleable. Shake intensity is well-balanced for normal play, but directional impact vectors are simplified to uniform random jitter. |
| **Level 3** | **Acceptable (B)** | Linear camera shake ($\text{Shake} = \text{Trauma}$). Functional, but can induce visual fatigue during extended horde waves. Toggle exists in settings. |
| **Level 2** | **Deficient (C)** | Excessive camera shake (>35px offset) or non-damping camera jitter that causes disorientation. Settings toggle missing or non-functional. |
| **Level 1** | **Unacceptable (F)** | Static, rigid camera with zero dynamic trauma, or chaotic unbounded shake that causes severe nausea. |

---

## 3. Damage Typography & Visual Pop

| Level | Grade | Quantitative & Qualitative Standard |
|:---:|:---:|:---|
| **Level 5** | **Exemplary (A+)** | **Zero GC Allocation**: Pre-allocated struct pool ($\ge 256$ entries) drawn directly via `CanvasItem`. Strict visual hierarchy: <br/>• **Normal Damage**: Cyan-white (`#E0F7FA`), 14pt, rapid upward float.<br/>• **Critical Hit**: Fluorophore Gold (`#FFD700`), 18pt, slight scale punch + explosive burst.<br/>• **Player Damage Taken**: High-contrast Crimson (`#FF3366`), 16pt, downward scatter.<br/>• **Heal / ATP Absorption**: Bio-lime Green (`#39FF14`), 15pt, gentle upward dissipation.<br/>• **Evaded / Blocked**: Turquoise Aqua (`#40E0D0`), 14pt, horizontal disperse. |
| **Level 4** | **Proficient (A)** | Zero-allocation struct pooling with distinct colors for normal, crit, and player damage. Numbers pop clearly, but scale animations are static. |
| **Level 3** | **Acceptable (B)** | Floating combat text renders correctly, but uses standard Godot `Label` nodes or lacks distinct font size scaling for crits. May generate minor GC overhead under 400+ enemies. |
| **Level 2** | **Deficient (C)** | Numbers overlap into unreadable solid color blobs during dense waves. Critical hits are indistinguishable from normal scratches. |
| **Level 1** | **Unacceptable (F)** | Missing damage text entirely or severe performance drops (>20ms frame spikes) caused by floating combat text instantiations. |

---

## 4. Bio-Acoustic Frequency Staging & Anti-Cacophony Mix

| Level | Grade | Quantitative & Qualitative Standard |
|:---:|:---:|:---|
| **Level 5** | **Exemplary (A+)** | **Microscopic Bio-Acoustic Palette**: Viscous hydrodynamic squishes, membrane tension pops, enzymatic sizzle, and crisp ATP absorption chimes. Strict three-band frequency staging:<br/>• **Sub-Bass (30–80 Hz)**: Cell motility, heartbeat thumps, heavy engulfments.<br/>• **Mids (200–2000 Hz)**: Tissue tears, pathogen distress calls, perforin lance hiss.<br/>• **Highs (2000–8000 Hz)**: ATP pickups, critical glass chimes, UI confirmation.<br/>**Dynamic Anti-Cacophony Ducking**: When $>30$ impacts occur within 0.5s, voice priority clamps and minor hit SFX duck by 6dB to prevent white-noise fatigue. |
| **Level 4** | **Proficient (A)** | High audio asset quality. SFX pool prevents voice cutoffs. Manifest validates 100% on disk. Frequency staging is good, but dynamic ducking relies on fixed voice limits rather than adaptive thresholding. |
| **Level 3** | **Acceptable (B)** | Standard arcade sound effects. Audio plays reliably without missing files, but high-density waves sound repetitive and slightly fatiguing. |
| **Level 2** | **Deficient (C)** | Unbalanced audio levels: certain SFX (e.g. crit or explosion) overpower the mix. Occasional missing sound warnings in logs. |
| **Level 1** | **Unacceptable (F)** | Silence on major combat actions, sound buffer clipping, or missing audio manifest entries that trigger runtime errors. |
