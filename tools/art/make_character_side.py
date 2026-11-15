"""Draws the Outpost Gale astronaut in side view, as a 4-frame walk cycle facing right
(run: python tools/art/make_character_side.py). The game flips it to walk left.

char_side_base_N.png   suit in greys plus a dark outline; the game tints it with the suit colour.
char_side_detail_N.png visor, gloves, boots, side panel, antenna: fixed colours drawn on top.

Same canvas, scale and style as make_character.py so the front and side views swap cleanly.
Everything is drawn at 4x and scaled down for smooth edges. Original art, no third-party assets.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W, H = 320, 400
S = 4
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Art')
INK = (36, 26, 51, 255)


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


def grey(v):
    return (v, v, v, 255)


def frame(swing, lift_near, lift_far):
    """swing > 0: near leg forward (right), far leg back. Arms swing the other way."""
    base = layer()
    d = ImageDraw.Draw(base)
    near_foot = (166 + swing, 352 - lift_near)
    far_foot = (150 - swing, 352 - lift_far)
    near_hand = (170 - swing * 0.8, 294)
    far_hand = (150 + swing * 0.8, 292)

    capsule(d, (150, 232), far_hand, 32, grey(186))          # far arm, behind the body
    capsule(d, (152, 300), far_foot, 38, grey(190))           # far leg
    rrect(d, (60, 196, 118, 306), 22, grey(170))              # backpack
    rrect(d, (104, 204, 212, 316), 40, grey(236))             # body
    capsule(d, (166, 300), near_foot, 38, grey(222))          # near leg
    d.ellipse(sc(110, 190, 206, 222), fill=grey(196))         # neck ring
    d.ellipse(sc(70, 34, 250, 222), fill=grey(244))           # helmet shell
    capsule(d, (168, 232), near_hand, 34, grey(228))          # near arm

    # soft light from the upper left
    shade = Image.linear_gradient('L').rotate(-35, expand=False).resize(base.size)
    shade = shade.point(lambda v: 255 - int(v * 0.22))
    r, g, b, a = base.split()
    base = Image.merge('RGBA', (ImageChops.multiply(r, shade), ImageChops.multiply(g, shade), ImageChops.multiply(b, shade), a))

    det = layer()
    dd = ImageDraw.Draw(det)
    for (fx, fy), col in ((far_foot, (46, 40, 68, 255)), (near_foot, (59, 51, 84, 255))):
        rrect(dd, (fx - 22, fy - 4, fx + 32, fy + 32), 14, col)       # boots, toes forward
    rrect(dd, (104, 300, 212, 312), 6, (92, 84, 120, 255))            # belt
    rrect(dd, (150, 238, 198, 274), 10, (42, 33, 72, 255))            # side panel
    for x, col in ((162, (255, 90, 78, 255)), (175, (255, 194, 61, 255)), (188, (59, 167, 240, 255))):
        dd.ellipse(sc(x - 5, 251, x + 5, 261), fill=col)
    for (hx, hy), col in ((far_hand, (200, 194, 214, 255)), (near_hand, (237, 231, 246, 255))):
        dd.ellipse(sc(hx - 19, hy - 19, hx + 19, hy + 19), fill=col)  # gloves
    capsule(dd, (112, 58), (92, 22), 6, (140, 132, 168, 255))         # antenna, at the back
    dd.ellipse(sc(82, 12, 102, 32), fill=(255, 194, 61, 255))

    # visor on the front of the helmet, clipped to the shell
    helmet = Image.new('L', base.size, 0)
    ImageDraw.Draw(helmet).ellipse(sc(76, 40, 244, 216), fill=255)
    vmask = Image.new('L', base.size, 0)
    ImageDraw.Draw(vmask).rounded_rectangle(sc(160, 84, 262, 178), radius=40 * S, fill=255)
    vmask = ImageChops.multiply(vmask, helmet)
    grad = Image.linear_gradient('L').resize(base.size)
    y0, y1 = 84 / H, 178 / H

    def ramp(a_, b_):
        def f(v):
            t = min(1.0, max(0.0, (v / 255 - y0) / (y1 - y0)))
            return int(a_ + (b_ - a_) * t)
        return f

    visor = Image.new('RGBA', base.size, (0, 0, 0, 0))
    col = Image.merge('RGB', [grad.point(ramp(a_, b_)) for a_, b_ in zip((24, 18, 60), (70, 52, 130))])
    visor.paste(col.convert('RGBA'), (0, 0), vmask)
    warm = layer()
    ImageDraw.Draw(warm).ellipse(sc(150, 160, 270, 230), fill=(255, 138, 91, 70))
    warm = warm.filter(ImageFilter.GaussianBlur(10 * S))
    empty = Image.new('RGBA', base.size, (0, 0, 0, 0))
    visor = Image.alpha_composite(visor, Image.composite(warm, empty, vmask))
    glint = layer()
    gd = ImageDraw.Draw(glint)
    gd.arc(sc(176, 94, 246, 160), start=200, end=262, fill=(255, 255, 255, 220), width=7 * S)
    gd.ellipse(sc(226, 100, 238, 112), fill=(255, 255, 255, 230))
    visor = Image.alpha_composite(visor, Image.composite(glint, empty, vmask))
    det = Image.alpha_composite(det, visor)

    shine = layer()
    ImageDraw.Draw(shine).ellipse(sc(96, 48, 150, 80), fill=(255, 255, 255, 150))
    det = Image.alpha_composite(det, shine.filter(ImageFilter.GaussianBlur(3 * S)))

    union = ImageChops.lighter(base.split()[3], det.split()[3])
    outline = Image.new('RGBA', base.size, INK)
    outline.putalpha(union.filter(ImageFilter.MaxFilter(4 * S * 2 + 1)))
    base = Image.alpha_composite(outline, base)
    return base, det


# A 4-step walk: contact, passing (far leg lifted), contact the other way, passing (near leg lifted).
FRAMES = [(26, 0, 0), (0, 0, 14), (-26, 0, 0), (0, 14, 0)]
os.makedirs(OUT, exist_ok=True)
for i, (swing, ln, lf) in enumerate(FRAMES):
    b, d = frame(swing, ln, lf)
    b.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'char_side_base_%d.png' % i))
    d.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'char_side_detail_%d.png' % i))
print('wrote char_side_base_0-3 and char_side_detail_0-3 to', os.path.abspath(OUT))
