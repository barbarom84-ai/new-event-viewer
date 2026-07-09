"""Generate WinUI Assets + app.ico from assets/app.ico."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "assets" / "app.ico"
ASSETS = ROOT / "EventViewer.WinUI" / "Assets"
BG = (17, 19, 24, 255)


def load_base() -> Image.Image:
    im = Image.open(SRC)
    im = im.convert("RGBA")
    # Prefer largest size if multi-frame ICO.
    best = im
    best_area = im.size[0] * im.size[1]
    try:
        n = getattr(im, "n_frames", 1)
        for i in range(n):
            im.seek(i)
            frame = im.convert("RGBA")
            area = frame.size[0] * frame.size[1]
            if area > best_area:
                best = frame.copy()
                best_area = area
    except Exception:
        pass
    return best


def save_square(base: Image.Image, size: int, name: str) -> None:
    out = base.resize((size, size), Image.Resampling.LANCZOS)
    path = ASSETS / name
    out.save(path, format="PNG")
    print(f"Wrote {name} {size}x{size}")


def save_wide(base: Image.Image, w: int, h: int, name: str) -> None:
    canvas = Image.new("RGBA", (w, h), BG)
    side = int(min(w, h) * 0.62)
    icon = base.resize((side, side), Image.Resampling.LANCZOS)
    x = (w - side) // 2
    y = (h - side) // 2
    canvas.paste(icon, (x, y), icon)
    path = ASSETS / name
    canvas.save(path, format="PNG")
    print(f"Wrote {name} {w}x{h}")


def main() -> None:
    if not SRC.exists():
        raise SystemExit(f"Missing icon: {SRC}")

    ASSETS.mkdir(parents=True, exist_ok=True)
    # Keep a copy next to WinUI assets for ApplicationIcon.
    (ASSETS / "app.ico").write_bytes(SRC.read_bytes())
    print(f"Copied app.ico -> {ASSETS / 'app.ico'}")

    base = load_base()
    print(f"Base source: {base.size[0]}x{base.size[1]}")

    squares = {
        "StoreLogo.png": 50,
        "Square44x44Logo.scale-200.png": 88,
        "Square44x44Logo.targetsize-24_altform-unplated.png": 24,
        "Square150x150Logo.scale-200.png": 300,
        "LockScreenLogo.scale-200.png": 48,
        "Square44x44Logo.png": 44,
        "Square150x150Logo.png": 150,
    }
    for name, size in squares.items():
        save_square(base, size, name)

    save_wide(base, 620, 300, "Wide310x150Logo.scale-200.png")
    save_wide(base, 310, 150, "Wide310x150Logo.png")
    save_wide(base, 1240, 600, "SplashScreen.scale-200.png")
    save_wide(base, 620, 300, "SplashScreen.png")
    print("Done.")


if __name__ == "__main__":
    main()
