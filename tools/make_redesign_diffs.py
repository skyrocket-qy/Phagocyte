import os
import shutil
from PIL import Image, ImageDraw, ImageFont
import numpy as np

baseline_dir = 'tmp/visual_chamber/baseline'
redesign_dir = 'tmp/visual_chamber/redesign'
diff_out_dir = 'tmp/visual_chamber/redesign_diff'
brain_dir = r'C:\Users\skyro\.gemini\antigravity\brain\decf2fb4-11a3-42ca-89e4-a32d70304d2c\visual_redesign'

os.makedirs(diff_out_dir, exist_ok=True)
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

print("Processing Redesign Visual Comparisons (Before vs After)...")
results = []

for skill_id, formation, tags in skills:
    before_path = os.path.join(baseline_dir, f"skill_{skill_id}_post.png")
    after_path = os.path.join(redesign_dir, f"skill_{skill_id}_post.png")

    if not os.path.exists(before_path) or not os.path.exists(after_path):
        print(f"[SKIP] Missing frame for {skill_id}")
        continue

    img_before = Image.open(before_path).convert('RGB')
    img_after = Image.open(after_path).convert('RGB')

    arr_before = np.array(img_before, dtype=np.int16)
    arr_after = np.array(img_after, dtype=np.int16)

    # Compute absolute difference
    diff = np.abs(arr_after - arr_before)
    diff_gray = np.max(diff, axis=2)

    # Pixel change percentage (threshold > 10 / 255)
    changed_mask = diff_gray > 10
    pct_changed = (np.count_nonzero(changed_mask) / diff_gray.size) * 100.0
    mean_delta = np.mean(diff_gray[changed_mask]) if np.any(changed_mask) else 0.0

    # Background noise check: sample corners (0:100, 0:100) and (1180:1280, 0:100)
    bg_corners = np.concatenate([
        diff_gray[0:100, 0:100].flatten(),
        diff_gray[0:100, 1180:1280].flatten(),
        diff_gray[620:720, 0:100].flatten(),
        diff_gray[620:720, 1180:1280].flatten()
    ])
    bg_noise_pct = (np.count_nonzero(bg_corners > 8) / bg_corners.size) * 100.0

    # Heatmap color coding: amplified 4x
    h_map = np.zeros_like(arr_after, dtype=np.uint8)
    h_map[..., 0] = np.clip(diff_gray * 5.0, 0, 255).astype(np.uint8) # Red channel
    h_map[..., 1] = np.clip(diff_gray * 3.0, 0, 255).astype(np.uint8) # Green
    h_map[..., 2] = np.clip(diff_gray * 1.5, 0, 255).astype(np.uint8) # Blue

    # Highlight changed areas
    h_map[changed_mask, 0] = np.clip(h_map[changed_mask, 0] + 60, 0, 255)
    img_heatmap = Image.fromarray(h_map)

    # Composite 3 panels: [ 1. BASELINE (BEFORE) | 2. REDESIGNED (AFTER) | 3. ISOLATED DELTA ]
    w, h = img_before.size
    composite = Image.new('RGB', (w * 3, h), (10, 12, 18))
    composite.paste(img_before, (0, 0))
    composite.paste(img_after, (w, 0))
    composite.paste(img_heatmap, (w * 2, 0))

    draw = ImageDraw.Draw(composite)

    # 1. Panel 1 Label
    draw.rectangle([10, 10, 320, 42], fill=(0, 0, 0, 220))
    draw.text((20, 16), "1. ORIGINAL BASELINE (BEFORE)", fill=(180, 200, 255))

    # 2. Panel 2 Label
    draw.rectangle([w + 10, 10, w + 300, 42], fill=(0, 0, 0, 220))
    draw.text((w + 20, 16), "2. REDESIGNED VFX (AFTER)", fill=(100, 255, 160))

    # 3. Panel 3 Label
    draw.rectangle([w * 2 + 10, 10, w * 2 + 360, 42], fill=(0, 0, 0, 220))
    draw.text((w * 2 + 20, 16), f"3. ISOLATED VFX DELTA ({pct_changed:.2f}%)", fill=(255, 230, 90))

    # Bottom Info Banner
    draw.rectangle([10, h - 38, w * 3 - 10, h - 10], fill=(0, 0, 0, 200))
    draw.text((20, h - 30), f"Skill: {skill_id}  |  Formation: {formation}  |  Tags: [{tags}]  |  Bg Noise: {bg_noise_pct:.2f}%", fill=(200, 220, 240))

    out_file = f"diff_redesign_{skill_id}.png"
    out_chamber = os.path.join(diff_out_dir, out_file)
    out_brain = os.path.join(brain_dir, out_file)

    composite.save(out_chamber)
    shutil.copy2(out_chamber, out_brain)
    results.append((skill_id, formation, pct_changed, mean_delta, bg_noise_pct, out_file))

print(f"\nSuccessfully generated {len(results)} redesign visual comparisons!\n")
print(f"{'Skill ID':26s} | {'Formation':22s} | {'VFX Delta':10s} | {'Bg Noise':9s}")
print("-" * 75)
for sid, form, pct, mean_d, bg_n, out_f in results:
    print(f"{sid:26s} | {form:22s} | {pct:8.2f}% | {bg_n:7.2f}%")
