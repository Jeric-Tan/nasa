"""Draws BOLT, the base robot mascot (run: python tools/art/make_bolt.py).

A round, friendly helper robot: a big head with a screen face and glowing eyes, a gold antenna
bulb, orange ear pods, a lightning-bolt badge (its name) and a little hover jet instead of legs.
Same outline and soft-light style as the astronaut. Drawn at 4x and scaled down. Original art.
Writes bolt.png (256 x 256) to Assets/Resources/Art.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W = H = 256
S = 4
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Art')
INK = (36, 26, 51, 255)


def sc(*v):
    return [int(round(x * S)) for x in v]


def layer():
    return Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))


def capsule(d, a, b, width, fill):
    d.line(sc(*a, *b), fill=fill, width=int(width * S))
    for x, y in (a, b):
        d.ellipse(sc(x - width / 2, y - width / 2, x + width / 2, y + width / 2), fill=fill)


# ---------- the body shapes (these get the outline) ----------
shape = layer()
d = ImageDraw.Draw(shape)
capsule(d, (98, 190), (72, 206), 18, (214, 210, 230, 255))        # arms, little and waving
capsule(d, (158, 190), (188, 172), 18, (214, 210, 230, 255))
d.ellipse(sc(62, 196, 82, 216), fill=(240, 236, 248, 255))         # hands
d.ellipse(sc(178, 162, 198, 182), fill=(240, 236, 248, 255))
d.rounded_rectangle(sc(108, 206, 148, 226), radius=8 * S, fill=(120, 112, 150, 255))   # hover jet
d.ellipse(sc(84, 160, 172, 224), fill=(228, 226, 240, 255))       # body
d.ellipse(sc(20, 98, 60, 138), fill=(255, 122, 69, 255))          # ear pods
d.ellipse(sc(196, 98, 236, 138), fill=(255, 122, 69, 255))
capsule(d, (128, 58), (128, 26), 7, (140, 132, 168, 255))         # antenna stalk
d.ellipse(sc(114, 8, 142, 36), fill=(255, 194, 61, 255))          # antenna bulb
d.rounded_rectangle(sc(36, 50, 220, 182), radius=66 * S, fill=(246, 244, 252, 255))   # head

# soft light from the upper left
shade = Image.linear_gradient('L').rotate(-35, expand=False).resize(shape.size)
shade = shade.point(lambda v: 255 - int(v * 0.2))
r, g, b, a = shape.split()
shape = Image.merge('RGBA', (ImageChops.multiply(r, shade), ImageChops.multiply(g, shade), ImageChops.multiply(b, shade), a))

# ---------- details drawn on top ----------
det = layer()
dd = ImageDraw.Draw(det)
dd.ellipse(sc(28, 106, 52, 130), fill=(255, 170, 120, 255))      # ear pod centres
dd.ellipse(sc(204, 106, 228, 130), fill=(255, 170, 120, 255))
dd.ellipse(sc(120, 14, 130, 24), fill=(255, 240, 190, 255))      # bulb glint
# screen face: deep navy glass
screen = layer()
ImageDraw.Draw(screen).rounded_rectangle(sc(58, 76, 198, 164), radius=40 * S, fill=(30, 24, 56, 255))
det = Image.alpha_composite(det, screen)
glow = layer()
gl = ImageDraw.Draw(glow)
for box in ((86, 96, 110, 136), (146, 96, 170, 136)):             # eyes: tall glowing pills
    gl.rounded_rectangle(sc(*box), radius=12 * S, fill=(111, 231, 255, 255))
gl.arc(sc(106, 124, 150, 152), start=25, end=155, fill=(111, 231, 255, 255), width=5 * S)   # smile
halo = glow.filter(ImageFilter.GaussianBlur(5 * S))
det = Image.alpha_composite(det, Image.blend(layer(), halo, 0.6))
det = Image.alpha_composite(det, glow)
dd = ImageDraw.Draw(det)
for x in (92, 152):                                                # eye glints
    dd.ellipse(sc(x, 101, x + 9, 110), fill=(255, 255, 255, 235))
dd.ellipse(sc(70, 136, 88, 147), fill=(255, 143, 163, 200))       # cheeks
dd.ellipse(sc(168, 136, 186, 147), fill=(255, 143, 163, 200))
# lightning-bolt badge on the body
dd.polygon([tuple(sc(x, y)) for x, y in ((132, 170), (114, 194), (126, 194), (120, 214), (142, 186), (130, 186), (138, 170))],
           fill=(255, 194, 61, 255))
# head shine
shine = layer()
ImageDraw.Draw(shine).ellipse(sc(60, 58, 116, 78), fill=(255, 255, 255, 170))
shine = shine.filter(ImageFilter.GaussianBlur(3 * S))
head = Image.new('L', shine.size, 0)
ImageDraw.Draw(head).rounded_rectangle(sc(42, 56, 214, 176), radius=60 * S, fill=255)   # keep the shine on the head
shine.putalpha(ImageChops.multiply(shine.split()[3], head))
det = Image.alpha_composite(det, shine)

# ---------- outline around everything, then the hover flame under it ----------
union = ImageChops.lighter(shape.split()[3], det.split()[3])
outline = Image.new('RGBA', shape.size, INK)
outline.putalpha(union.filter(ImageFilter.MaxFilter(4 * S * 2 + 1)))
art = Image.alpha_composite(outline, shape)
art = Image.alpha_composite(art, det)

flame = layer()
fd = ImageDraw.Draw(flame)
fd.ellipse(sc(104, 224, 152, 252), fill=(111, 231, 255, 150))
fd.ellipse(sc(116, 226, 140, 244), fill=(220, 250, 255, 220))
flame = flame.filter(ImageFilter.GaussianBlur(3 * S))
final = Image.alpha_composite(flame, art)

os.makedirs(OUT, exist_ok=True)
final.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'bolt.png'))
print('wrote bolt.png to', os.path.abspath(OUT))
