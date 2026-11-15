"""Draws the airlock's spare-suit rack (run: python tools/art/make_suit_rack.py).

Two empty spacesuits hanging limp on hangers from a rail, with their gold-visored helmets on a
shelf above, so they read as gear, not people. Same outline style as the characters.
Drawn at 4x and scaled down. Original art. Writes airlock_rack.png (480 x 420) to Assets/Resources/Art.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W, H = 480, 420
S = 4
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Art')
INK = (36, 26, 51, 255)
STEEL = (106, 116, 158, 255)
STEEL_DARK = (74, 82, 120, 255)
SUIT = (226, 226, 236, 255)
SUIT_SHADE = (196, 196, 210, 255)
STRIPE = (255, 122, 69, 255)
GLOVE = (170, 168, 188, 255)
BOOT = (64, 58, 88, 255)


def sc(*v):
    return [int(round(x * S)) for x in v]


def layer():
    return Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))


def rrect(d, box, r, fill):
    d.rounded_rectangle(sc(*box), radius=r * S, fill=fill)


def capsule(d, a, b, width, fill):
    d.line(sc(*a, *b), fill=fill, width=int(width * S))
    for x, y in (a, b):
        d.ellipse(sc(x - width / 2, y - width / 2, x + width / 2, y + width / 2), fill=fill)


img = layer()
d = ImageDraw.Draw(img)

# shelf with two helmets resting on it
rrect(d, (16, 100, 464, 114), 5, STEEL)
for cx in (130, 350):
    d.ellipse(sc(cx - 50, 18, cx + 50, 116), fill=SUIT)                # helmet dome
    d.rectangle(sc(cx - 52, 96, cx + 52, 104), fill=SUIT_SHADE)        # neck ring at the bottom
    capsule(d, (cx + 30, 30), (cx + 40, 6), 4, (140, 132, 168, 255))   # antenna
    d.ellipse(sc(cx + 34, 0, cx + 46, 12), fill=(255, 194, 61, 255))

# rail and hangers
rrect(d, (24, 146, 456, 158), 5, STEEL)
for cx in (130, 350):
    d.line(sc(cx, 152, cx, 166), fill=STEEL_DARK, width=4 * S)                      # hook
    d.polygon([tuple(sc(x, y)) for x, y in ((cx, 166), (cx - 58, 190), (cx + 58, 190))], fill=STEEL_DARK)   # hanger

    # the empty suit, hanging limp: arms straight down, no head
    capsule(d, (cx - 50, 202), (cx - 62, 300), 30, SUIT_SHADE)       # arms hang down at the sides
    capsule(d, (cx + 50, 202), (cx + 62, 300), 30, SUIT_SHADE)
    rrect(d, (cx - 40, 296, cx - 6, 384), 15, SUIT_SHADE)            # legs
    rrect(d, (cx + 6, 296, cx + 40, 384), 15, SUIT_SHADE)
    rrect(d, (cx - 56, 184, cx + 56, 306), 34, SUIT)                 # torso
    d.ellipse(sc(cx - 28, 178, cx + 28, 200), fill=(42, 34, 58, 255))   # open neck: nobody inside

shade = Image.linear_gradient('L').rotate(-35, expand=False).resize(img.size).point(lambda v: 255 - int(v * 0.18))
r, g, b, a = img.split()
img = Image.merge('RGBA', (ImageChops.multiply(r, shade), ImageChops.multiply(g, shade), ImageChops.multiply(b, shade), a))

det = layer()
dd = ImageDraw.Draw(det)
for cx in (130, 350):
    # gold sun visor (like real spacewalk helmets), with a glint
    dd.rounded_rectangle(sc(cx - 36, 40, cx + 36, 90), radius=22 * S, fill=(214, 160, 60, 255))
    dd.arc(sc(cx - 28, 46, cx + 10, 82), start=200, end=260, fill=(255, 236, 170, 255), width=5 * S)
    # suit details
    for x in (cx - 62, cx + 62):
        dd.ellipse(sc(x - 17, 290, x + 17, 322), fill=GLOVE)             # empty gloves
        dd.line(sc(x - 11, 246 + (x - cx) * 0.03, x + 11, 246 + (x - cx) * 0.03), fill=STRIPE, width=7 * S)  # arm stripes
    rrect(dd, (cx - 26, 226, cx + 26, 258), 9, (42, 33, 72, 255))       # chest panel, switched off
    for k, x in enumerate((cx - 12, cx, cx + 12)):
        dd.ellipse(sc(x - 4, 238, x + 4, 246), fill=(90, 84, 110, 255))
    rrect(dd, (cx - 56, 288, cx + 56, 298), 4, (92, 84, 120, 255))      # belt
    rrect(dd, (cx - 46, 372, cx - 2, 402), 12, BOOT)                     # boots
    rrect(dd, (cx + 2, 372, cx + 46, 402), 12, BOOT)

union = ImageChops.lighter(img.split()[3], det.split()[3])
outline = Image.new('RGBA', img.size, INK)
outline.putalpha(union.filter(ImageFilter.MaxFilter(3 * S * 2 + 1)))
art = Image.alpha_composite(Image.alpha_composite(outline, img), det)

os.makedirs(OUT, exist_ok=True)
art.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'airlock_rack.png'))
print('wrote airlock_rack.png to', os.path.abspath(OUT))
