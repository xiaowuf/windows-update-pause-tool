from pathlib import Path
from PIL import Image


project_root = Path(__file__).resolve().parent.parent
source = project_root / "assets" / "app-icon-source.jpg"
output_png = project_root / "assets" / "app-icon.png"
output_ico = project_root / "assets" / "app.ico"

with Image.open(source) as image:
    image = image.convert("RGBA")
    side = min(image.size)
    left = (image.width - side) // 2
    top = (image.height - side) // 2
    square = image.crop((left, top, left + side, top + side))
    square.resize((512, 512), Image.Resampling.LANCZOS).save(output_png)
    square.save(
        output_ico,
        format="ICO",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )

print(output_ico)
