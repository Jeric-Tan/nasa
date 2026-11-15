"""Draws the two crew members, Mira (botanist) and Tomas (geologist), in their base jumpsuits
(run: python tools/art/make_crew.py). Inside the base there's no helmet, so you see their faces.

For each of mira and tomas it writes (to Assets/Resources/Art):
  crew_<name>.png          front view, standing
  crew_<name>_side_N.png   side view facing right, 4-frame walk cycle (the game flips it)

Same 320 x 400 canvas, proportions and outline style as the astronaut, so they swap cleanly.
Full colour (not tinted). Drawn at 4x and scaled down. Original art, no third-party assets.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W, H = 320, 400
S = 4
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Art')
INK = (36, 26, 51, 255)

CREW = {
    'mira': dict(skin=(232, 176, 138, 255), hair=(92, 52, 40, 255), suit=(70, 178, 104, 255), suit_dark=(48, 140, 80, 255),
                 trim=(214, 245, 222, 255), hairstyle='bun', badge='leaf'),
    'tomas': dict(skin=(168, 107, 76, 255), hair=(40, 30, 46, 255), suit=(64, 150, 222, 255), suit_dark=(42, 112, 180, 255),
                  trim=(220, 238, 255, 255), hairstyle='curls', badge='rock'),
}
BOOT = (52, 45, 76, 255)
BELT = (70, 62, 96, 255)
EYE = (42, 31, 61, 255)
BLUSH = (255, 140, 150, 150)
PATCH = (255, 122, 69, 255)


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


def circle(d, c, r, fill):
    d.ellipse(sc(c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=fill)


def badge(d, kind, c):
    x, y = c
    if kind == 'leaf':
        d.ellipse(sc(x - 9, y - 6, x + 9, y + 6), fill=(120, 220, 120, 255))
        d.line(sc(x - 8, y + 4, x + 8, y - 4), fill=(40, 120, 60, 255), width=2 * S)
    else:
        d.polygon([tuple(sc(px, py)) for px, py in ((x - 9, y + 6), (x - 6, y - 5), (x + 2, y - 8), (x + 9, y - 1), (x + 7, y + 7))], fill=(170, 160, 180, 255))
        d.ellipse(sc(x - 3, y - 3, x + 1, y + 1), fill=(220, 214, 230, 255))


def finish(img):
    """Soft light from the upper left, then the ink outline around the whole figure."""
    shade = Image.linear_gradient('L').rotate(-35, expand=False).resize(img.size).point(lambda v: 255 - int(v * 0.16))
    r, g, b, a = img.split()
    img = Image.merge('RGBA', (ImageChops.multiply(r, shade), ImageChops.multiply(g, shade), ImageChops.multiply(b, shade), a))
    outline = Image.new('RGBA', img.size, INK)
    outline.putalpha(a.filter(ImageFilter.MaxFilter(4 * S * 2 + 1)))
    return Image.alpha_composite(outline, img)


def front(c):
    img = layer()
    d = ImageDraw.Draw(img)
    if c['hairstyle'] == 'bun':
        circle(d, (160, 40), 34, c['hair'])                         # bun on top
        d.ellipse(sc(80, 52, 240, 196), fill=c['hair'])               # hair behind the head
    rrect(d, (118, 294, 154, 364), 16, c['suit_dark'])                # legs
    rrect(d, (166, 294, 202, 364), 16, c['suit_dark'])
    rrect(d, (106, 348, 160, 386), 14, BOOT)                         # boots
    rrect(d, (160, 348, 214, 386), 14, BOOT)
    capsule(d, (110, 232), (88, 296), 32, c['suit'])                  # arms
    capsule(d, (210, 232), (232, 296), 32, c['suit'])
    circle(d, (88, 300), 16, c['skin'])                               # hands
    circle(d, (232, 300), 16, c['skin'])
    rrect(d, (104, 204, 216, 316), 38, c['suit'])                     # body
    rrect(d, (104, 294, 216, 306), 5, BELT)                           # belt
    d.polygon([tuple(sc(x, y)) for x, y in ((136, 206), (160, 232), (184, 206))], fill=c['trim'])   # collar
    circle(d, (190, 250), 11, PATCH)                                  # mission patch
    badge(d, c['badge'], (132, 252))
    if c['badge'] == 'rock':                                          # rock hammer on the belt
        d.line(sc(198, 300, 212, 330), fill=(140, 100, 70, 255), width=5 * S)
        rrect(d, (200, 324, 226, 334), 3, (170, 176, 190, 255))
    rrect(d, (146, 190, 174, 214), 8, c['skin'])                      # neck
    circle(d, (84, 136), 15, c['skin'])                               # ears
    circle(d, (236, 136), 15, c['skin'])
    d.ellipse(sc(84, 56, 236, 208), fill=c['skin'])                   # head
    # hair on top
    if c['hairstyle'] == 'bun':
        d.pieslice(sc(82, 50, 238, 176), 180, 360, fill=c['hair'])
        d.pieslice(sc(82, 70, 150, 160), 180, 290, fill=c['hair'])   # side-swept fringe
    else:
        d.pieslice(sc(80, 46, 240, 164), 180, 360, fill=c['hair'])
        for x in range(92, 232, 18):                                  # curls along the hairline
            circle(d, (x, 104 - 10 * (1 - abs(x - 160) / 80)), 12, c['hair'])
        d.line(sc(86, 92, 234, 92), fill=(90, 90, 110, 255), width=7 * S)   # headlamp band
        circle(d, (160, 84), 11, (255, 210, 90, 255))                 # headlamp
        circle(d, (157, 81), 4, (255, 250, 220, 255))
    # face
    for x in (132, 188):
        d.ellipse(sc(x - 9, 126, x + 9, 150), fill=EYE)
        circle(d, (x + 3, 132), 4, (255, 255, 255, 240))
    d.arc(sc(142, 150, 178, 174), start=20, end=160, fill=EYE, width=4 * S)
    d.ellipse(sc(104, 152, 124, 164), fill=BLUSH)
    d.ellipse(sc(196, 152, 216, 164), fill=BLUSH)
    return finish(img)


def side(c, swing, lift_near, lift_far):
    img = layer()
    d = ImageDraw.Draw(img)
    near_foot = (166 + swing, 350 - lift_near)
    far_foot = (150 - swing, 350 - lift_far)
    near_hand = (172 - swing * 0.8, 292)
    far_hand = (150 + swing * 0.8, 290)
    if c['hairstyle'] == 'bun':
        circle(d, (104, 52), 32, c['hair'])                           # bun at the back of the head
    capsule(d, (150, 232), far_hand, 30, c['suit_dark'])             # far arm
    circle(d, far_hand, 15, c['skin'])
    capsule(d, (152, 298), far_foot, 36, c['suit_dark'])             # far leg
    rrect(d, (far_foot[0] - 22, far_foot[1] - 4, far_foot[0] + 32, far_foot[1] + 32), 14, BOOT)
    rrect(d, (108, 204, 210, 316), 38, c['suit'])                     # body
    rrect(d, (108, 294, 210, 306), 5, BELT)
    rrect(d, (186, 214, 206, 236), 6, c['trim'])                      # collar edge
    circle(d, (176, 252), 10, PATCH)
    capsule(d, (166, 298), near_foot, 36, c['suit'])                  # near leg
    rrect(d, (near_foot[0] - 22, near_foot[1] - 4, near_foot[0] + 32, near_foot[1] + 32), 14, BOOT)
    if c['badge'] == 'rock':
        d.line(sc(126, 300, 112, 330), fill=(140, 100, 70, 255), width=5 * S)
        rrect(d, (98, 324, 124, 334), 3, (170, 176, 190, 255))
    rrect(d, (146, 190, 174, 214), 8, c['skin'])                      # neck
    d.ellipse(sc(86, 56, 238, 208), fill=c['skin'])                   # head
    circle(d, (238, 140), 9, c['skin'])                               # nose
    if c['hairstyle'] == 'bun':
        d.pieslice(sc(84, 50, 240, 176), 180, 330, fill=c['hair'])
        d.ellipse(sc(84, 60, 160, 186), fill=c['hair'])               # hair down the back
    else:
        d.pieslice(sc(82, 46, 242, 164), 180, 340, fill=c['hair'])
        for x, y in ((96, 108), (92, 128), (100, 148), (112, 90), (130, 78)):
            circle(d, (x, y), 13, c['hair'])                          # curls at the back
        d.line(sc(100, 92, 238, 96), fill=(90, 90, 110, 255), width=7 * S)
        circle(d, (232, 94), 10, (255, 210, 90, 255))                 # headlamp, facing forward
    circle(d, (150, 138), 14, c['skin'])                              # ear
    circle(d, (150, 138), 6, tuple(max(0, v - 30) for v in c['skin'][:3]) + (255,))
    d.ellipse(sc(196, 126, 214, 150), fill=EYE)                       # eye
    circle(d, (208, 132), 4, (255, 255, 255, 240))
    d.arc(sc(196, 152, 226, 172), start=20, end=110, fill=EYE, width=4 * S)   # smile
    d.ellipse(sc(178, 154, 198, 166), fill=BLUSH)
    capsule(d, (170, 232), near_hand, 32, c['suit'])                  # near arm
    circle(d, near_hand, 16, c['skin'])
    return finish(img)


FRAMES = [(26, 0, 0), (0, 0, 14), (-26, 0, 0), (0, 14, 0)]
os.makedirs(OUT, exist_ok=True)
for name, c in CREW.items():
    front(c).resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'crew_%s.png' % name))
    for i, f in enumerate(FRAMES):
        side(c, *f).resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'crew_%s_side_%d.png' % (name, i)))
print('wrote crew_mira / crew_tomas (front + 4 side frames) to', os.path.abspath(OUT))
