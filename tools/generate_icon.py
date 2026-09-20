#!/usr/bin/env python3
"""Generate a lightbulb icon representing knowledge.

Produces:
  - bulb.png            (1024x1024, transparent background)
  - bulb.ico            (multi-size 16/24/32/48/64/128/256)
"""
from PIL import Image, ImageDraw, ImageFilter
import math

SIZE = 1024


def lerp(a, b, t):
    return a + (b - a) * t


def draw_lightbulb(image):
    d = ImageDraw.Draw(image)

    # ---- palette
    glow = (255, 232, 160)
    bulb_body = (255, 214, 90)
    bulb_highlight = (255, 250, 220)
    filament = (180, 120, 20)          # warm wire
    base = (150, 152, 160)             # metal screw
    base_dark = (90, 92, 100)
    ray = (255, 210, 60)

    # ---- geometry
    cy = 470                      # bulb center
    bulb_r = 300                  # main sphere radius
    tip_y = 330
    neck_top = 640                # where sphere meets neck

    # ==== light rays (behind everything, radiating from bulb core) ====
    cx, cy0 = SIZE // 2, 500
    inner = 300
    outer = 470
    for i in range(12):
        angle = math.radians(-90 + i * 30)
        p = (cx + math.cos(angle) * inner, cy0 + math.sin(angle) * inner)
        q = (cx + math.cos(angle) * outer, cy0 + math.sin(angle) * outer)
        w = 26 if i % 3 == 0 else 14
        d.line([p, q], fill=ray, width=w)

    halo = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    hd.ellipse(
        [cx - bulb_r - 60, cy - bulb_r - 60, cx + bulb_r + 60, cy + bulb_r + 60],
        fill=(255, 236, 140, 70),
    )
    halo = halo.filter(ImageFilter.GaussianBlur(60))
    image.alpha_composite(halo)

    d = ImageDraw.Draw(image)

    # ==== bulb sphere ====
    bulb_box = [cx - bulb_r, cy - bulb_r, cx + bulb_r, cy + bulb_r]
    d.ellipse(bulb_box, fill=bulb_body, outline=(230, 170, 40), width=10)

    # highlight (gloss on top-left)
    hl = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    hl_d = ImageDraw.Draw(hl)
    hl_d.ellipse(
        [cx - 190, cy - 200, cx - 60, cy - 70],
        fill=bulb_highlight,
    )
    hl = hl.filter(ImageFilter.GaussianBlur(40))
    image.alpha_composite(hl)

    d = ImageDraw.Draw(image)

    # ==== filament (zig-zag wire inside) ====
    points = [
        (cx - 85, neck_top - 60),
        (cx - 55, neck_top - 150),
        (cx - 10, neck_top - 120),
        (cx + 40, neck_top - 190),
        (cx + 85, neck_top - 70),
    ]
    d.line(points, fill=filament, width=18, joint="curve")

    # small glow around filament
    fil_glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    fg = ImageDraw.Draw(fil_glow)
    fg.line(points, fill=(255, 170, 30, 200), width=40, joint="curve")
    fil_glow = fil_glow.filter(ImageFilter.GaussianBlur(24))
    image.alpha_composite(fil_glow)

    d = ImageDraw.Draw(image)
    d.line(points, fill=filament, width=18, joint="curve")

    # ==== neck (glass transition) ====
    n_left = cx - 110
    n_right = cx + 110
    d.polygon(
        [(n_left, neck_top), (n_right, neck_top), (n_right - 40, 780), (n_left + 40, 780)],
        fill=bulb_body,
        outline=(230, 170, 40),
        width=6,
    )

    # ==== screw base ====
    base_top = 780
    base_bottom = 900
    base_left = cx - 120
    base_right = cx + 120
    d.rounded_rectangle(
        [base_left, base_top, base_right, base_bottom],
        radius=24,
        fill=base,
        outline=base_dark,
        width=8,
    )

    # screw threads
    for yy in range(800, 892, 22):
        d.line([(base_left + 16, yy), (base_right - 16, yy)], fill=base_dark, width=6)

    # contact tip
    d.ellipse(
        [cx - 40, base_bottom - 14, cx + 40, base_bottom + 30],
        fill=base_dark,
    )


def main():
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw_lightbulb(image)

    image.save("bulb.png")
    print("wrote bulb.png")

    # multi-size ico
    image.save(
        "bulb.ico",
        format="ICO",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )
    print("wrote bulb.ico")


if __name__ == "__main__":
    main()