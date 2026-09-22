import subprocess
import tempfile
from pathlib import Path
from PIL import Image

def create_synthetic_1024_image(file_path: Path):
    """Creates a 1024x1024 synthetic image with 4x4 grid cells on black background."""
    img = Image.new("RGBA", (1024, 1024), (0, 0, 0, 255))

    cell_size = 256
    for row in range(4):
        for col in range(4):
            x = col * cell_size + 32
            y = row * cell_size + 32
            # Draw colored rectangle inside each cell
            color = (col * 60 + 50, row * 60 + 50, 200, 255)
            for dx in range(192):
                for dy in range(192):
                    img.putpixel((x + dx, y + dy), color)

    img.save(file_path, format="PNG")

def test_slice():
    with tempfile.TemporaryDirectory() as tmpdir:
        tmp_path = Path(tmpdir)
        input_img = tmp_path / "test_1024.png"
        out_dir = tmp_path / "output"

        create_synthetic_1024_image(input_img)

        # 1. Test grid slicing (4x4)
        cmd_grid = [
            "python3", "tools/slice/main.py", "grid",
            "--input", str(input_img),
            "--rows", "4",
            "--cols", "4",
            "--output", str(out_dir),
            "--names", "achievement/ach_01, achievement/ach_02, skill/skill_01, skip, passive_tree/trait_01, ui/icon_01"
        ]
        res = subprocess.run(cmd_grid, capture_output=True, text=True)
        assert res.returncode == 0, f"Grid slice failed: {res.stderr}\n{res.stdout}"

        ach1_file = out_dir / "achievement" / "ach_01.png"
        ach2_file = out_dir / "achievement" / "ach_02.png"
        skill1_file = out_dir / "skill" / "skill_01.png"
        trait1_file = out_dir / "passive_tree" / "trait_01.png"
        ui1_file = out_dir / "ui" / "icon_01.png"

        assert ach1_file.exists(), "ach_01.png not created"
        assert ach2_file.exists(), "ach_02.png not created"
        assert skill1_file.exists(), "skill_01.png not created"
        assert trait1_file.exists(), "trait_01.png not created"
        assert ui1_file.exists(), "icon_01.png not created"

        # Check cell resolution (256x256)
        with Image.open(ach1_file) as sliced_img:
            assert sliced_img.size == (256, 256), f"Unexpected size {sliced_img.size}"
            # Check background keying: corner pixel (5, 5) should be transparent (alpha == 0)
            corner_pixel = sliced_img.getpixel((5, 5))
            assert corner_pixel[3] == 0, f"Background pixel was not transparent: {corner_pixel}"
            # Inner pixel (100, 100) should be opaque (alpha == 255)
            inner_pixel = sliced_img.getpixel((100, 100))
            assert inner_pixel[3] == 255, f"Foreground pixel was transparent: {inner_pixel}"

        # 2. Test flexible slicing
        flex_out_dir = tmp_path / "flex_output"
        cmd_flex = [
            "python3", "tools/slice/main.py", "flexible",
            "--input", str(input_img),
            "--unit", "256",
            "--output", str(flex_out_dir),
            "--specs", "ui/panel_large,0,0,2,2; ui/button,2,0,1,1"
        ]
        res_flex = subprocess.run(cmd_flex, capture_output=True, text=True)
        assert res_flex.returncode == 0, f"Flexible slice failed: {res_flex.stderr}\n{res_flex.stdout}"

        panel_file = flex_out_dir / "ui" / "panel_large.png"
        btn_file = flex_out_dir / "ui" / "button.png"

        assert panel_file.exists(), "panel_large.png not created"
        assert btn_file.exists(), "button.png not created"

        with Image.open(panel_file) as panel_img:
            assert panel_img.size == (512, 512), f"Panel size mismatch: {panel_img.size}"

        print("ALL SLICE TESTS PASSED CLEANLY!")

if __name__ == "__main__":
    test_slice()
