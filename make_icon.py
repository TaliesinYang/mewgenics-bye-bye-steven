"""Generate app icon: Steven's head with a red prohibition sign overlay."""
from PIL import Image, ImageDraw

SRC = r"C:\Users\Alex\AppData\Local\Temp\utools-clipboard\1771655930253.png"
OUT = r"C:\Users\Alex\AppData\Roaming\Glaiel Games\Mewgenics\MewgenicsSaveGuardian\src\Resources\app.ico"

def make_icon():
    img = Image.open(SRC).convert("RGBA")

    # Crop to square centered on Steven's head
    w, h = img.size
    side = min(w, h)
    left = (w - side) // 2
    top = (h - side) // 2
    img = img.crop((left, top, left + side, top + side))

    # Generate multiple sizes for the ICO
    sizes = [256, 128, 64, 48, 32, 16]
    frames = []

    for sz in sizes:
        frame = img.resize((sz, sz), Image.LANCZOS)
        draw = ImageDraw.Draw(frame)

        # Draw prohibition sign: red circle with diagonal slash
        pad = int(sz * 0.04)
        stroke = max(int(sz * 0.09), 2)

        # Outer circle
        bbox = (pad, pad, sz - pad - 1, sz - pad - 1)
        draw.ellipse(bbox, outline=(200, 40, 40, 230), width=stroke)

        # Diagonal slash (top-right to bottom-left)
        import math
        cx, cy = sz // 2, sz // 2
        r = (sz - 2 * pad) // 2
        angle = math.radians(45)
        x1 = cx + int(r * math.cos(angle))
        y1 = cy - int(r * math.sin(angle))
        x2 = cx - int(r * math.cos(angle))
        y2 = cy + int(r * math.sin(angle))
        draw.line((x1, y1, x2, y2), fill=(200, 40, 40, 230), width=stroke)

        frames.append(frame)

    # Save as ICO with multiple sizes
    frames[0].save(
        OUT,
        format="ICO",
        sizes=[(sz, sz) for sz in sizes],
        append_images=frames[1:],
    )
    print(f"Icon saved to {OUT}")
    print(f"Sizes: {sizes}")

if __name__ == "__main__":
    make_icon()
