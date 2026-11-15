"""Draws the Outpost Gale astronaut as two layers (run: python tools/art/make_character.py).

char_base.png   the suit in greys plus a dark outline. The game tints it with the suit colour.
char_detail.png visor, gloves, boots, chest panel, antenna: fixed colours drawn on top.

Everything is drawn at 4x and scaled down for smooth edges. Original art, no third-party assets.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W, H = 320, 400          # output size
S = 4                    # supersampling
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Art')


def sc(*v):
    return [int(round(x * S)) for x in v]


def layer():
    return Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))


def rrect(d, box, r, fill):
    d.rounded_rectangle(sc(*box), radius=r * S, fill=fill)


def capsule(d, a, b, width, fill):
    d.line(sc(*a, *b), fill=fill, width=int(width * S))
    for p in (a, b):
        x, y = p
        d.ellipse(sc(x - width / 2, y - width / 2, x + width / 2, y + width / 2), fill=fill)


def grey(v):
    return (v, v, v, 255)


# ---------- suit (tintable greys) ----------
base = layer()
d = ImageDraw.Draw(base)
rrect(d, (82, 198, 238, 302), 30, grey(176))                 # backpack, peeking out
capsule(d, (104, 226), (72, 292), 36, grey(222))             # left arm, down
capsule(d, (216, 226), (254, 160), 36, grey(214))            # right arm, waving
rrect(d, (112, 296, 152, 368), 18, grey(222))                # legs
rrect(d, (168, 296, 208, 368), 18, grey(206))
rrect(d, (98, 204, 222, 316), 42, grey(238))                 # body
d.ellipse(sc(108, 190, 212, 222), fill=grey(196))            # neck ring
d.ellipse(sc(66, 34, 254, 222), fill=grey(244))              # helmet shell

# soft light from the upper left: darken toward the lower right
shade = Image.linear_gradient('L').rotate(-35, expand=False).resize(base.size)
shade = shade.point(lambda v: 255 - int(v * 0.22))
r, g, b, a = base.split()
r = ImageChops.multiply(r, shade)
g = ImageChops.multiply(g, shade)
b = ImageChops.multiply(b, shade)
base = Image.merge('RGBA', (r, g, b, a))
d = ImageDraw.Draw(base)

# ---------- details (fixed colours) ----------
det = layer()
dd = ImageDraw.Draw(det)
INK = (36, 26, 51, 255)
rrect(dd, (102, 352, 158, 386), 14, (59, 51, 84, 255))       # boots
rrect(dd, (162, 352, 218, 386), 14, (52, 45, 76, 255))
rrect(dd, (100, 300, 220, 312), 6, (92, 84, 120, 255))       # belt
rrect(dd, (128, 232, 192, 278), 12, (42, 33, 72, 255))       # chest panel
for x, col in ((145, (255, 90, 78, 255)), (160, (255, 194, 61, 255)), (175, (59, 167, 240, 255))):
    dd.ellipse(sc(x - 6, 249, x + 6, 261), fill=col)
rrect(dd, (136, 266, 184, 271), 2, (120, 110, 160, 255))
for c, rr in (((70, 298), 21), ((256, 152), 21)):             # gloves
    dd.ellipse(sc(c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr), fill=(237, 231, 246, 255))
    dd.ellipse(sc(c[0] - rr + 7, c[1] - rr + 5, c[0] + 2, c[1] - 2), fill=(255, 255, 255, 180))
capsule(dd, (214, 58), (232, 22), 6, (140, 132, 168, 255))   # antenna
dd.ellipse(sc(224, 12, 244, 32), fill=(255, 194, 61, 255))

# visor: deep space glass with a warm Mars reflection and a bright glint
visor_mask = Image.new('L', base.size, 0)
ImageDraw.Draw(visor_mask).rounded_rectangle(sc(94, 82, 226, 178), radius=44 * S, fill=255)
visor = Image.new('RGBA', base.size, (0, 0, 0, 0))
grad = Image.linear_gradient('L').resize((base.size[0], base.size[1]))   # 0 at the top, 255 at the bottom
navy_top, navy_bot = (24, 18, 60), (70, 52, 130)
y0, y1 = 82 / H, 178 / H                                                   # the visor's vertical span


def ramp(a, b):
    def f(v):
        t = min(1.0, max(0.0, (v / 255 - y0) / (y1 - y0)))
        return int(a + (b - a) * t)
    return f


visor_col = Image.merge('RGB', [grad.point(ramp(a, b)) for a, b in zip(navy_top, navy_bot)])
visor.paste(visor_col.convert('RGBA'), (0, 0), visor_mask)
vd = ImageDraw.Draw(visor)
warm = layer()
ImageDraw.Draw(warm).ellipse(sc(80, 160, 240, 230), fill=(255, 138, 91, 70))
warm = warm.filter(ImageFilter.GaussianBlur(10 * S))
visor = Image.alpha_composite(visor, Image.composite(warm, Image.new('RGBA', base.size, (0, 0, 0, 0)), visor_mask))
glint = layer()
gd = ImageDraw.Draw(glint)
gd.arc(sc(108, 92, 196, 160), start=195, end=255, fill=(255, 255, 255, 220), width=7 * S)
gd.ellipse(sc(196, 100, 208, 112), fill=(255, 255, 255, 230))
visor = Image.alpha_composite(visor, Image.composite(glint, Image.new('RGBA', base.size, (0, 0, 0, 0)), visor_mask))
det = Image.alpha_composite(det, visor)

# helmet shine (stays white whatever the suit colour)
shine = layer()
ImageDraw.Draw(shine).ellipse(sc(92, 46, 150, 80), fill=(255, 255, 255, 150))
shine = shine.filter(ImageFilter.GaussianBlur(3 * S))
det = Image.alpha_composite(det, shine)

# ---------- outline around the whole figure, baked into the base layer ----------
union = ImageChops.lighter(base.split()[3], det.split()[3])
grow = union.filter(ImageFilter.MaxFilter(4 * S * 2 + 1))
outline = Image.new('RGBA', base.size, INK)
outline.putalpha(grow)
base = Image.alpha_composite(outline, base)

os.makedirs(OUT, exist_ok=True)
for img, name in ((base, 'char_base.png'), (det, 'char_detail.png')):
    img.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, name))
print('wrote char_base.png and char_detail.png to', os.path.abspath(OUT))
