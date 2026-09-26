# Active Skill Formation & Timing Reference Matrix

This reference maps all **18 active skills** in *Project: Phagocyte* to their optimal target dummy formation, peak capture frame, special test triggers, and expected visual signatures within `SkillTestChamber`.

## Master Formation Matrix

| Skill ID | Display Name | Formation | Peak Frame ($T_{peak}$) | Special Action / Trigger | Expected Visual Signature |
| :--- | :--- | :--- | :---: | :--- | :--- |
| `phagocytic_grasp` | Phagocytic Grasp | `Single` | 5 | Default trigger | Actin pseudopod tentacle extending from membrane to dummy, vesicle burst at tip. |
| `perforin_lance` | Perforin Lance | `Line` | 4 | Default trigger | High-velocity green/violet piercing laser beam puncturing 3 dummies with pore decals. |
| `complement_cascade` | Complement Cascade | `Single` | 15 | Accelerate mine assembly (`AssemblyTime = 0.05f`) | Cyan MAC assembly circle deploying, concentric detonation shockwave. |
| `antibody_salvo` | Antibody Salvo | `Cluster` | 18 | Default trigger | Volley of teal Y-shaped immunoglobulin missiles homing toward dummy cluster. |
| `granzyme_detonation` | Granzyme Detonation | `Cluster` | 6 | `DetonateCaspase(dummy.GlobalPosition)` | Pulsing caspase orange rune, followed by violent apoptotic multi-ring explosion. |
| `mhc_tracer_beam` | MHC Tracer Beam | `Single` | 4 | Default trigger | Sustained emerald laser beam connecting cell to dummy with scanning aperture reticle. |
| `lysosomal_overload` | Lysosomal Overload | `GroundHazard` | 8 | Default trigger | Yellow-green caustic acid pool expanding with dissolving bubbles beneath dummy. |
| `ros_torrent` | ROS Torrent | `Line` | 6 | Default trigger | Jet of reactive oxygen peroxide particles streaming horizontally across targets. |
| `pseudopod_lunge` | Pseudopod Lunge | `Single` | 5 | Default trigger | Rapid elongated cellular protrusion striking forward with impact dust. |
| `nitric_oxide_halo` | Nitric Oxide Halo | `Radial` | 6 | Default trigger | Cyan gas ring expanding across 8 radial dummies with orbiting nitrogen bubbles. |
| `nuclease_blades` | Nuclease Blades | `Radial` | 5 | Default trigger | Whirling green enzyme blades slicing through surrounding circular targets. |
| `interferon_wave` | Interferon Wave | `Radial` | 6 | Default trigger | High-frequency concentric shockwave ripple triggering simultaneous flashes on all 8 dummies. |
| `lysozyme_ricochet` | Lysozyme Ricochet | `Cluster` | 8 | Default trigger | High-speed enzymatic crystal projectile ricocheting between clustered targets. |
| `phagolysosome_vent` | Phagolysosome Vent | `GroundHazard` | 6 | Default trigger | Red digestive trail vented behind cell, bubbling hazard area. |
| `pro_inflammatory_arc`| Pro-Inflammatory Arc | `Cluster` | 5 | Default trigger | Golden/amber cytokine lightning bolt arcing and branching across 3 dummies. |
| `exosome_singularity` | Exosome Singularity | `Cluster` | 8 | Default trigger | Swirling gravitational exosome vortex pulling dummy cluster inward with spiral particle trails. |
| `defensin_barbs` | Defensin Barbs | `Radial` | 6 | Default trigger | 360-degree burst of sharp antimicrobial barbs radiating outward into 8 targets. |
| `histamine_surge` | Histamine Surge | `Radial` | 6 | Default trigger | Golden inflammatory shockwave expanding rapidly outward, vascular permeability aura. |

---

## Formation Geometries

All dummy positions are calculated relative to `Center = Vector2(640, 360)`:

### 1. `DummyFormation.Single`
- `Dummy 1`: `Center + (160, 0)`

### 2. `DummyFormation.Line`
- `Dummy 1`: `Center + (100, 0)`
- `Dummy 2`: `Center + (165, 0)`
- `Dummy 3`: `Center + (230, 0)`

### 3. `DummyFormation.Cluster`
- `Dummy 1`: `Center + (140, -35)`
- `Dummy 2`: `Center + (185, 0)`
- `Dummy 3`: `Center + (150, 35)`

### 4. `DummyFormation.Radial`
- 8 Dummies arranged symmetrically at $45^\circ$ increments along radius $R = 135\text{px}$:
  $$\text{Pos}_i = \text{Center} + \left(135 \cdot \cos\left(\frac{i \cdot 2\pi}{8}\right), 135 \cdot \sin\left(\frac{i \cdot 2\pi}{8}\right)\right)$$

### 5. `DummyFormation.GroundHazard`
- `Dummy 1`: `Center + (150, 0)`
