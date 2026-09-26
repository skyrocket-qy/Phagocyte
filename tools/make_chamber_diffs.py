import os
import shutil
from PIL import Image, ImageDraw, ImageFont
import numpy as np

chamber_dir = 'tmp/visual_chamber'
brain_dir = r'C:\Users\skyro\.gemini\antigravity\brain\decf2fb4-11a3-42ca-89e4-a32d70304d2c\visual_chamber'

os.makedirs(chamber_dir, exist_ok=True)
os.makedirs(brain_dir, exist_ok=True)

skills = [
    ("phagocytic_grasp", "Single Target", "Attack, Melee, AOE"),
    ("perforin_lance", "Linear Column", "Attack, Projectile"),
    ("complement_cascade", "Single Target (Detonation)", "Spell, Trap, AOE, Duration"),
    ("antibody_salvo", "Cluster Form", "Spell, Projectile"),
    ("granzyme_detonation", "Cluster Form (Apoptosis)", "Spell, AOE, Duration"),
    ("mhc_tracer_beam", "Single Target (Laser Lock)", "Spell, Duration"),
    ("lysosomal_overload", "Ground Hazard (Acid Pool)", "Spell, Trap, AOE, Duration"),
    ("ros_torrent", "Linear Column (Peroxide Jet)", "Spell, AOE, Duration"),
    ("pseudopod_lunge", "Single Target (Actin Fist)", "Attack, Melee, AOE"),
    ("nitric_oxide_halo", "Radial Nova (8 Dummies)", "Spell, AOE, Duration"),
    ("nuclease_blades", "Radial Nova (8 Dummies)", "Spell, Projectile, Duration"),
    ("interferon_wave", "Radial Nova (8 Dummies)", "Spell, AOE"),
    ("lysozyme_ricochet", "Cluster Form (Enzyme Bounce)", "Spell, Projectile"),
    ("phagolysosome_vent", "Ground Hazard (Digestive Trail)", "Spell, Trap, AOE, Duration"),
    ("pro_inflammatory_arc", "Cluster Form (Lightning Arc)", "Spell, AOE"),
    ("exosome_singularity", "Cluster Form (Vortex Pull)", "Spell, AOE, Duration"),
    ("defensin_barbs", "Radial Nova (8 Dummies)", "Attack, Projectile, AOE"),
    ("histamine_surge", "Radial Nova (8 Dummies)", "Spell, AOE")
]

print("Processing Isolated Chamber Visual Comparisons...")
results = []

for skill_id, formation, tags in skills:
    pre_path = os.path.join(chamber_dir, f"skill_{skill_id}_pre.png")
    post_path = os.path.join(chamber_dir, f"skill_{skill_id}_post.png")

    if not os.path.exists(pre_path) or not os.path.exists(post_path):
        print(f"Missing capture for {skill_id}: pre={os.path.exists(pre_path)}, post={os.path.exists(post_path)}")
        continue

    pre_img = Image.open(pre_path).convert("RGB")
    post_img = Image.open(post_path).convert("RGB")

    w, h = pre_img.size
    composite = Image.new("RGB", (w * 3, h), (10, 13, 20))

    composite.paste(pre_img, (0, 0))
    composite.paste(post_img, (w, 0))

    pre_arr = np.array(pre_img, dtype=np.float32)
    post_arr = np.array(post_img, dtype=np.float32)
    delta = np.abs(post_arr - pre_arr)
    delta_mag = np.max(delta, axis=-1)

    # Threshold for actual VFX difference
    changed = delta_mag > 6.0
    pct_changed = float(np.count_nonzero(changed)) / float(w * h) * 100.0
    mean_delta = float(np.mean(delta_mag[changed])) if np.any(changed) else 0.0

    # Background noise check: check outer corners [0:80, 0:80] and [0:80, -80:]
    corner_noise = np.count_nonzero(changed[:80, :80]) + np.count_nonzero(changed[:80, -80:])
    bg_noise_pct = float(corner_noise) / float(80 * 80 * 2) * 100.0

    heatmap = np.zeros((h, w, 3), dtype=np.uint8)
    if np.any(changed):
        heatmap[changed, 0] = np.clip(delta_mag[changed] * 3.5, 30, 255).astype(np.uint8)
        heatmap[changed, 1] = np.clip(255 - delta_mag[changed] * 1.5, 50, 255).astype(np.uint8)
        heatmap[changed, 2] = np.clip(delta[..., 2][changed] * 2.5 + 80, 50, 255).astype(np.uint8)

    diff_pil = Image.fromarray(heatmap)
    composite.paste(diff_pil, (w * 2, 0))

    draw = ImageDraw.Draw(composite)
    # 1. Panel 1 Label
    draw.rectangle([10, 10, 280, 42], fill=(0, 0, 0, 220))
    draw.text((20, 16), "1. PRE-CAST (T0 BASELINE)", fill=(100, 200, 255))

    # 2. Panel 2 Label
    draw.rectangle([w + 10, 10, w + 280, 42], fill=(0, 0, 0, 220))
    draw.text((w + 20, 16), "2. ACTIVE SKILL VFX (Tpeak)", fill=(100, 255, 160))

    # 3. Panel 3 Label
    draw.rectangle([w * 2 + 10, 10, w * 2 + 360, 42], fill=(0, 0, 0, 220))
    draw.text((w * 2 + 20, 16), f"3. ISOLATED VFX HEATMAP ({pct_changed:.2f}%)", fill=(255, 230, 90))

    # Bottom Info Banner
    draw.rectangle([10, h - 38, w * 3 - 10, h - 10], fill=(0, 0, 0, 200))
    draw.text((20, h - 30), f"Skill: {skill_id}  |  Formation: {formation}  |  Tags: [{tags}]  |  Bg Noise: {bg_noise_pct:.2f}%", fill=(200, 220, 240))

    out_file = f"diff_chamber_{skill_id}.png"
    out_chamber = os.path.join(chamber_dir, out_file)
    out_brain = os.path.join(brain_dir, out_file)

    composite.save(out_chamber)
    shutil.copy2(out_chamber, out_brain)
    results.append((skill_id, formation, pct_changed, mean_delta, bg_noise_pct, out_file))

print(f"\nSuccessfully generated {len(results)} isolated chamber visual diffs!\n")
print(f"{'Skill ID':26s} | {'Formation':22s} | {'VFX Delta':10s} | {'Bg Noise':9s}")
print("-" * 75)
for sid, form, pct, mean_d, bg_n, out_f in results:
    print(f"{sid:26s} | {form:22s} | {pct:8.2f}% | {bg_n:7.2f}%")
