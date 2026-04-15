#!/usr/bin/env python3
"""Generate skill and spell icon sprite sheets (512x512 each).

Draws pixel-art icons using Pillow. Each icon is 32x32 on a dark background.
Output:
  assets/icons/skills_icons.png  — Warrior + Ranger + Innate (73 icons)
  assets/icons/spells_icons.png  — Mage Arcane + Conduit (45 icons)

Layout: 16 columns x 16 rows grid. Each row of 5 = [base skill] + [4 specifics].
Icons are grouped by class → category → base skill.

Run: python3 scripts/generate_icons.py
"""

import os
from PIL import Image, ImageDraw

# ── Constants ───────────────────────────────────────────────────────────────

ICON_SIZE = 32
SHEET_SIZE = 512
COLS = SHEET_SIZE // ICON_SIZE  # 16
ROWS = SHEET_SIZE // ICON_SIZE  # 16
BG_COLOR = (15, 17, 23, 255)  # #0f1117
OUTLINE = (0, 0, 0, 255)

# ── Color palettes ──────────────────────────────────────────────────────────

# Warrior: warm golds, browns, reds
C_WARRIOR_BODY = {
    "primary": (210, 170, 90),    # gold
    "secondary": (180, 130, 60),  # dark gold
    "accent": (240, 200, 120),    # bright gold
    "highlight": (255, 220, 150), # cream
}
C_WARRIOR_MIND = {
    "primary": (160, 130, 200),   # purple-ish
    "secondary": (120, 100, 170), # dark purple
    "accent": (200, 170, 240),    # light purple
    "highlight": (220, 200, 255), # pale purple
}

# Ranger: greens, browns, earthy
C_RANGER_ARMS = {
    "primary": (130, 170, 80),    # green
    "secondary": (100, 140, 60),  # dark green
    "accent": (170, 200, 110),    # light green
    "highlight": (200, 230, 150), # pale green
}
C_RANGER_INSTINCT = {
    "primary": (100, 160, 160),   # teal
    "secondary": (70, 130, 130),  # dark teal
    "accent": (140, 200, 200),    # light teal
    "highlight": (180, 230, 230), # pale teal
}

# Innate: silver/blue
C_INNATE = {
    "primary": (150, 180, 220),   # silver-blue
    "secondary": (120, 150, 190), # dark silver
    "accent": (180, 210, 245),    # light silver
    "highlight": (220, 235, 255), # white-blue
}

# Mage element colors
C_FIRE = {
    "primary": (220, 80, 40),     # red-orange
    "secondary": (180, 50, 20),   # dark red
    "accent": (255, 160, 50),     # orange
    "highlight": (255, 220, 100), # yellow
}
C_WATER = {
    "primary": (60, 130, 220),    # blue
    "secondary": (40, 90, 180),   # dark blue
    "accent": (100, 180, 255),    # light blue
    "highlight": (180, 220, 255), # ice white
}
C_AIR = {
    "primary": (100, 200, 230),   # cyan
    "secondary": (60, 160, 200),  # dark cyan
    "accent": (150, 230, 255),    # light cyan
    "highlight": (220, 245, 255), # pale cyan
}
C_EARTH = {
    "primary": (160, 120, 70),    # brown
    "secondary": (120, 90, 50),   # dark brown
    "accent": (200, 160, 100),    # tan
    "highlight": (180, 170, 150), # stone gray
}
C_LIGHT = {
    "primary": (245, 200, 107),   # gold (#f5c86b)
    "secondary": (200, 160, 70),  # dark gold
    "accent": (255, 230, 150),    # bright gold
    "highlight": (255, 245, 210), # white-gold
}
C_DARK = {
    "primary": (130, 80, 180),    # purple
    "secondary": (90, 50, 140),   # dark purple
    "accent": (170, 110, 220),    # light purple
    "highlight": (100, 60, 120),  # deep purple
}
C_RESTORATION = {
    "primary": (80, 200, 120),    # green
    "secondary": (50, 160, 80),   # dark green
    "accent": (120, 230, 160),    # light green
    "highlight": (200, 255, 220), # pale green
}
C_AMPLIFICATION = {
    "primary": (80, 140, 240),    # electric blue
    "secondary": (50, 100, 200),  # dark blue
    "accent": (120, 180, 255),    # light blue
    "highlight": (180, 210, 255), # pale blue
}
C_OVERCHARGE = {
    "primary": (240, 100, 60),    # danger orange
    "secondary": (200, 60, 30),   # dark red
    "accent": (255, 160, 80),     # bright orange
    "highlight": (255, 200, 100), # yellow warning
}


# ── Drawing primitives ──────────────────────────────────────────────────────

def draw_pixel(draw, x, y, color, ox=0, oy=0):
    """Draw a single pixel at grid position (x,y) with offset."""
    draw.point((ox + x, oy + y), fill=color)


def draw_rect(draw, x1, y1, x2, y2, color, ox=0, oy=0):
    """Draw a filled rectangle."""
    draw.rectangle((ox + x1, oy + y1, ox + x2, oy + y2), fill=color)


def draw_line_px(draw, x1, y1, x2, y2, color, ox=0, oy=0):
    """Draw a 1px line."""
    draw.line((ox + x1, oy + y1, ox + x2, oy + y2), fill=color, width=1)


def draw_circle_filled(draw, cx, cy, r, color, ox=0, oy=0):
    """Draw a filled circle."""
    draw.ellipse((ox + cx - r, oy + cy - r, ox + cx + r, oy + cy + r), fill=color)


def draw_circle_outline(draw, cx, cy, r, color, ox=0, oy=0):
    """Draw a circle outline."""
    draw.ellipse((ox + cx - r, oy + cy - r, ox + cx + r, oy + cy + r), outline=color)


def draw_diamond(draw, cx, cy, r, color, ox=0, oy=0):
    """Draw a filled diamond."""
    pts = [(ox+cx, oy+cy-r), (ox+cx+r, oy+cy), (ox+cx, oy+cy+r), (ox+cx-r, oy+cy)]
    draw.polygon(pts, fill=color)


def draw_triangle_up(draw, cx, cy, size, color, ox=0, oy=0):
    """Draw upward triangle."""
    pts = [(ox+cx, oy+cy-size), (ox+cx+size, oy+cy+size), (ox+cx-size, oy+cy+size)]
    draw.polygon(pts, fill=color)


def draw_triangle_down(draw, cx, cy, size, color, ox=0, oy=0):
    """Draw downward triangle."""
    pts = [(ox+cx, oy+cy+size), (ox+cx+size, oy+cy-size), (ox+cx-size, oy+cy-size)]
    draw.polygon(pts, fill=color)


def draw_star(draw, cx, cy, r_out, r_in, points, color, ox=0, oy=0):
    """Draw a star shape."""
    import math
    pts = []
    for i in range(points * 2):
        angle = math.pi * i / points - math.pi / 2
        r = r_out if i % 2 == 0 else r_in
        pts.append((ox + cx + r * math.cos(angle), oy + cy + r * math.sin(angle)))
    draw.polygon(pts, fill=color)


# ── Icon drawing functions ──────────────────────────────────────────────────
# Each returns nothing, draws directly onto the ImageDraw at offset (ox, oy).
# All icons are 32x32.

# --- Generic shapes for base skill icons ---

def icon_fist(draw, c, ox, oy):
    """Clenched fist — Unarmed base."""
    draw_rect(draw, 11, 8, 20, 12, c["primary"], ox, oy)  # knuckles
    draw_rect(draw, 10, 12, 21, 22, c["primary"], ox, oy)  # fist body
    draw_rect(draw, 13, 22, 18, 25, c["secondary"], ox, oy)  # wrist
    # finger lines
    draw_line_px(draw, 14, 9, 14, 12, c["secondary"], ox, oy)
    draw_line_px(draw, 17, 9, 17, 12, c["secondary"], ox, oy)
    draw_line_px(draw, 20, 10, 20, 12, c["secondary"], ox, oy)
    # thumb
    draw_rect(draw, 8, 13, 10, 18, c["accent"], ox, oy)

def icon_sword(draw, c, ox, oy):
    """Sword — Bladed base."""
    # blade
    draw_line_px(draw, 16, 5, 16, 20, c["highlight"], ox, oy)
    draw_line_px(draw, 15, 6, 15, 19, c["accent"], ox, oy)
    draw_line_px(draw, 17, 6, 17, 19, c["accent"], ox, oy)
    # point
    draw_pixel(draw, 16, 4, c["highlight"], ox, oy)
    # guard
    draw_rect(draw, 12, 20, 20, 21, c["primary"], ox, oy)
    # grip
    draw_rect(draw, 15, 22, 17, 26, c["secondary"], ox, oy)
    # pommel
    draw_rect(draw, 14, 27, 18, 28, c["primary"], ox, oy)

def icon_hammer(draw, c, ox, oy):
    """Hammer — Blunt base."""
    # handle
    draw_rect(draw, 15, 14, 17, 27, c["secondary"], ox, oy)
    # head
    draw_rect(draw, 10, 6, 22, 14, c["primary"], ox, oy)
    draw_rect(draw, 11, 7, 21, 13, c["accent"], ox, oy)
    # highlight
    draw_rect(draw, 12, 8, 14, 10, c["highlight"], ox, oy)

def icon_spear(draw, c, ox, oy):
    """Spear — Polearms base."""
    # shaft
    draw_line_px(draw, 16, 10, 16, 28, c["secondary"], ox, oy)
    # spearhead
    draw_triangle_up(draw, 16, 7, 4, c["accent"], ox, oy)
    draw_pixel(draw, 16, 4, c["highlight"], ox, oy)

def icon_shield(draw, c, ox, oy):
    """Shield — Shields base."""
    # shield body (rounded shape)
    draw_rect(draw, 10, 7, 22, 22, c["primary"], ox, oy)
    draw_rect(draw, 11, 6, 21, 23, c["primary"], ox, oy)
    draw_rect(draw, 12, 23, 20, 25, c["primary"], ox, oy)
    draw_pixel(draw, 13, 25, c["primary"], ox, oy)
    draw_pixel(draw, 19, 25, c["primary"], ox, oy)
    # center cross
    draw_line_px(draw, 16, 9, 16, 21, c["accent"], ox, oy)
    draw_line_px(draw, 12, 14, 20, 14, c["accent"], ox, oy)
    # boss
    draw_circle_filled(draw, 16, 14, 2, c["highlight"], ox, oy)

def icon_brain(draw, c, ox, oy):
    """Brain/meditation — Inner base."""
    draw_circle_filled(draw, 16, 14, 8, c["primary"], ox, oy)
    # brain folds
    draw_line_px(draw, 16, 7, 16, 21, c["secondary"], ox, oy)
    draw_line_px(draw, 11, 11, 14, 14, c["accent"], ox, oy)
    draw_line_px(draw, 18, 11, 21, 14, c["accent"], ox, oy)
    draw_line_px(draw, 11, 17, 14, 14, c["accent"], ox, oy)
    draw_line_px(draw, 18, 17, 21, 14, c["accent"], ox, oy)
    # glow
    draw_circle_outline(draw, 16, 14, 10, c["highlight"], ox, oy)

def icon_shout(draw, c, ox, oy):
    """Shout/aura — Outer base."""
    # face
    draw_circle_filled(draw, 16, 14, 6, c["primary"], ox, oy)
    # open mouth
    draw_rect(draw, 14, 16, 18, 19, c["secondary"], ox, oy)
    # shout waves
    draw_line_px(draw, 23, 12, 26, 10, c["accent"], ox, oy)
    draw_line_px(draw, 24, 15, 27, 15, c["accent"], ox, oy)
    draw_line_px(draw, 23, 18, 26, 20, c["accent"], ox, oy)
    draw_line_px(draw, 5, 12, 8, 10, c["accent"], ox, oy)
    draw_line_px(draw, 4, 15, 7, 15, c["accent"], ox, oy)
    draw_line_px(draw, 5, 18, 8, 20, c["accent"], ox, oy)

def icon_bow(draw, c, ox, oy):
    """Bow — Drawn base."""
    # bow curve (left side)
    draw_line_px(draw, 10, 6, 8, 16, c["primary"], ox, oy)
    draw_line_px(draw, 8, 16, 10, 26, c["primary"], ox, oy)
    # string
    draw_line_px(draw, 10, 6, 10, 26, c["secondary"], ox, oy)
    # arrow
    draw_line_px(draw, 11, 16, 25, 16, c["accent"], ox, oy)
    # arrowhead
    draw_pixel(draw, 26, 16, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 15, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 17, c["highlight"], ox, oy)
    # fletching
    draw_pixel(draw, 12, 14, c["secondary"], ox, oy)
    draw_pixel(draw, 12, 18, c["secondary"], ox, oy)

def icon_throwing_knife(draw, c, ox, oy):
    """Throwing knife — Thrown base."""
    # blade (diagonal)
    draw_line_px(draw, 8, 8, 20, 20, c["accent"], ox, oy)
    draw_line_px(draw, 9, 8, 21, 20, c["accent"], ox, oy)
    # handle
    draw_line_px(draw, 21, 21, 25, 25, c["secondary"], ox, oy)
    # motion lines
    draw_line_px(draw, 6, 12, 4, 14, c["highlight"], ox, oy)
    draw_line_px(draw, 10, 6, 8, 8, c["highlight"], ox, oy)

def icon_pistol(draw, c, ox, oy):
    """Pistol — Firearms base."""
    # barrel
    draw_rect(draw, 6, 12, 18, 15, c["primary"], ox, oy)
    # body
    draw_rect(draw, 14, 10, 22, 16, c["primary"], ox, oy)
    # grip
    draw_rect(draw, 18, 16, 22, 24, c["secondary"], ox, oy)
    # trigger guard
    draw_line_px(draw, 16, 16, 16, 20, c["accent"], ox, oy)
    draw_line_px(draw, 16, 20, 18, 20, c["accent"], ox, oy)
    # muzzle flash
    draw_pixel(draw, 5, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 4, 12, c["highlight"], ox, oy)
    draw_pixel(draw, 4, 14, c["highlight"], ox, oy)

def icon_buckler(draw, c, ox, oy):
    """Small buckler — Ranger Melee base."""
    draw_circle_filled(draw, 16, 15, 8, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 5, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["accent"], ox, oy)

def icon_crosshair(draw, c, ox, oy):
    """Crosshair — Precision base."""
    draw_circle_outline(draw, 16, 15, 8, c["primary"], ox, oy)
    draw_line_px(draw, 16, 4, 16, 10, c["accent"], ox, oy)
    draw_line_px(draw, 16, 20, 16, 26, c["accent"], ox, oy)
    draw_line_px(draw, 5, 15, 11, 15, c["accent"], ox, oy)
    draw_line_px(draw, 21, 15, 27, 15, c["accent"], ox, oy)
    draw_pixel(draw, 16, 15, c["highlight"], ox, oy)

def icon_eye(draw, c, ox, oy):
    """Eye — Awareness base."""
    # eye shape
    pts = [(6, 15), (16, 8), (26, 15), (16, 22)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw_circle_filled(draw, 16, 15, 4, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["secondary"], ox, oy)
    draw_pixel(draw, 15, 14, c["highlight"], ox, oy)

def icon_trap(draw, c, ox, oy):
    """Bear trap — Trapping base."""
    # jaws (top)
    for i in range(5):
        x = 8 + i * 4
        draw_triangle_up(draw, x, 12, 2, c["accent"], ox, oy)
    # jaws (bottom)
    for i in range(5):
        x = 8 + i * 4
        draw_triangle_down(draw, x, 18, 2, c["accent"], ox, oy)
    # base plate
    draw_rect(draw, 8, 20, 24, 23, c["primary"], ox, oy)
    # chain
    draw_line_px(draw, 16, 23, 16, 27, c["secondary"], ox, oy)

# --- Specific skill icon variants ---
# These are simpler variations using shapes + color to differentiate

def icon_slash_arc(draw, c, ox, oy):
    """Arc slash attack."""
    import math
    for angle in range(0, 180, 8):
        rad = math.radians(angle)
        x = int(16 + 10 * math.cos(rad))
        y = int(16 - 8 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
        draw_pixel(draw, x, y+1, c["primary"], ox, oy)

def icon_stab(draw, c, ox, oy):
    """Thrust/stab."""
    draw_line_px(draw, 16, 4, 16, 24, c["accent"], ox, oy)
    draw_line_px(draw, 15, 5, 15, 23, c["primary"], ox, oy)
    # impact lines
    draw_line_px(draw, 12, 24, 20, 24, c["highlight"], ox, oy)
    draw_line_px(draw, 13, 26, 19, 26, c["highlight"], ox, oy)

def icon_overhead_chop(draw, c, ox, oy):
    """Cleave/overhead chop."""
    # blade coming down
    draw_rect(draw, 14, 4, 18, 16, c["accent"], ox, oy)
    # impact burst
    draw_line_px(draw, 10, 18, 22, 18, c["highlight"], ox, oy)
    draw_line_px(draw, 8, 20, 24, 20, c["highlight"], ox, oy)
    draw_line_px(draw, 12, 22, 20, 22, c["primary"], ox, oy)

def icon_crossed_swords(draw, c, ox, oy):
    """Parry — crossed blades."""
    draw_line_px(draw, 8, 6, 24, 22, c["accent"], ox, oy)
    draw_line_px(draw, 24, 6, 8, 22, c["accent"], ox, oy)
    draw_line_px(draw, 9, 6, 25, 22, c["primary"], ox, oy)
    draw_line_px(draw, 25, 6, 9, 22, c["primary"], ox, oy)
    # spark at center
    draw_pixel(draw, 16, 14, c["highlight"], ox, oy)
    draw_pixel(draw, 15, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 15, c["highlight"], ox, oy)

def icon_kick(draw, c, ox, oy):
    """Leg kick."""
    # leg
    draw_line_px(draw, 12, 8, 16, 16, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 16, 24, 14, c["primary"], ox, oy)
    draw_line_px(draw, 13, 8, 17, 16, c["secondary"], ox, oy)
    draw_line_px(draw, 17, 16, 25, 14, c["primary"], ox, oy)
    # boot
    draw_rect(draw, 23, 12, 27, 16, c["accent"], ox, oy)
    # impact
    draw_pixel(draw, 28, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 28, 15, c["highlight"], ox, oy)

def icon_grapple(draw, c, ox, oy):
    """Grasping hands."""
    # two hands reaching
    draw_rect(draw, 6, 10, 12, 20, c["primary"], ox, oy)
    draw_rect(draw, 20, 10, 26, 20, c["primary"], ox, oy)
    # fingers curling inward
    draw_rect(draw, 12, 12, 14, 14, c["accent"], ox, oy)
    draw_rect(draw, 12, 16, 14, 18, c["accent"], ox, oy)
    draw_rect(draw, 18, 12, 20, 14, c["accent"], ox, oy)
    draw_rect(draw, 18, 16, 20, 18, c["accent"], ox, oy)

def icon_elbow(draw, c, ox, oy):
    """Elbow/knee strike."""
    # arm bent
    draw_line_px(draw, 8, 8, 16, 16, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 16, 12, 24, c["secondary"], ox, oy)
    # elbow point
    draw_circle_filled(draw, 16, 16, 3, c["accent"], ox, oy)
    # impact sparks
    draw_pixel(draw, 19, 14, c["highlight"], ox, oy)
    draw_pixel(draw, 20, 16, c["highlight"], ox, oy)
    draw_pixel(draw, 19, 18, c["highlight"], ox, oy)


def icon_generic_attack(draw, c, ox, oy, symbol="X"):
    """Generic attack icon with visual symbol."""
    draw_circle_filled(draw, 16, 15, 10, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 7, c["primary"], ox, oy)
    # center accent
    draw_diamond(draw, 16, 15, 4, c["accent"], ox, oy)


def icon_generic_buff(draw, c, ox, oy):
    """Generic buff — upward arrow in circle."""
    draw_circle_filled(draw, 16, 15, 10, c["secondary"], ox, oy)
    draw_triangle_up(draw, 16, 12, 5, c["accent"], ox, oy)
    draw_rect(draw, 14, 17, 18, 23, c["accent"], ox, oy)


def icon_generic_debuff(draw, c, ox, oy):
    """Generic debuff — downward arrow in circle."""
    draw_circle_filled(draw, 16, 15, 10, c["secondary"], ox, oy)
    draw_triangle_down(draw, 16, 18, 5, c["accent"], ox, oy)
    draw_rect(draw, 14, 7, 18, 13, c["accent"], ox, oy)


def icon_generic_aoe(draw, c, ox, oy):
    """Generic AoE — concentric circles."""
    draw_circle_filled(draw, 16, 15, 11, c["secondary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 8, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 15, 5, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)


def icon_shield_raised(draw, c, ox, oy):
    """Shield raised — Block."""
    icon_shield(draw, c, ox, oy)
    # glow
    draw_line_px(draw, 8, 5, 8, 24, c["highlight"], ox, oy)
    draw_line_px(draw, 24, 5, 24, 24, c["highlight"], ox, oy)


def icon_shield_bash(draw, c, ox, oy):
    """Shield bash."""
    draw_rect(draw, 8, 8, 18, 22, c["primary"], ox, oy)
    # motion lines
    draw_line_px(draw, 20, 10, 26, 8, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 15, 27, 15, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 20, 26, 22, c["highlight"], ox, oy)


def icon_deflect(draw, c, ox, oy):
    """Arrow bouncing off shield."""
    draw_rect(draw, 6, 8, 14, 22, c["primary"], ox, oy)
    # incoming arrow
    draw_line_px(draw, 26, 8, 16, 14, c["accent"], ox, oy)
    # bouncing arrow
    draw_line_px(draw, 16, 14, 24, 22, c["highlight"], ox, oy)


def icon_fortress(draw, c, ox, oy):
    """Bulwark — fortified stance."""
    draw_rect(draw, 8, 10, 24, 24, c["primary"], ox, oy)
    draw_rect(draw, 10, 12, 22, 22, c["secondary"], ox, oy)
    # battlements
    draw_rect(draw, 8, 6, 11, 10, c["primary"], ox, oy)
    draw_rect(draw, 14, 6, 18, 10, c["primary"], ox, oy)
    draw_rect(draw, 21, 6, 24, 10, c["primary"], ox, oy)


def icon_focused_eye(draw, c, ox, oy):
    """Battle Focus — focused eye."""
    icon_eye(draw, c, ox, oy)
    # focus lines around
    draw_pixel(draw, 16, 3, c["highlight"], ox, oy)
    draw_pixel(draw, 16, 27, c["highlight"], ox, oy)
    draw_pixel(draw, 3, 15, c["highlight"], ox, oy)
    draw_pixel(draw, 29, 15, c["highlight"], ox, oy)


def icon_armor_body(draw, c, ox, oy):
    """Pain Tolerance — armored body."""
    # torso
    draw_rect(draw, 10, 8, 22, 22, c["primary"], ox, oy)
    # shoulders
    draw_rect(draw, 7, 8, 10, 14, c["accent"], ox, oy)
    draw_rect(draw, 22, 8, 25, 14, c["accent"], ox, oy)
    # center plate
    draw_rect(draw, 13, 10, 19, 20, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 10, 16, 20, c["accent"], ox, oy)


def icon_healing_breath(draw, c, ox, oy):
    """Second Wind — healing breath."""
    # swirl/wind
    import math
    for angle in range(0, 360, 15):
        rad = math.radians(angle)
        r = 6 + angle / 120
        x = int(16 + r * math.cos(rad))
        y = int(15 + r * math.sin(rad))
        if 2 <= x <= 29 and 2 <= y <= 29:
            draw_pixel(draw, x, y, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)


def icon_helmet(draw, c, ox, oy):
    """Iron Will — iron helmet."""
    draw_rect(draw, 10, 8, 22, 22, c["primary"], ox, oy)
    draw_rect(draw, 8, 12, 24, 18, c["primary"], ox, oy)
    # visor slit
    draw_line_px(draw, 12, 15, 20, 15, c["secondary"], ox, oy)
    # crown
    draw_rect(draw, 12, 6, 20, 8, c["accent"], ox, oy)


def icon_multiple_arrows(draw, c, ox, oy):
    """Rapid Fire — multiple arrows."""
    for i in range(3):
        y = 10 + i * 5
        draw_line_px(draw, 6, y, 22, y, c["accent"], ox, oy)
        draw_pixel(draw, 23 - i*2, y, c["highlight"], ox, oy)
        draw_pixel(draw, 22 - i*2, y-1, c["highlight"], ox, oy)
        draw_pixel(draw, 22 - i*2, y+1, c["highlight"], ox, oy)


def icon_arc_arrow(draw, c, ox, oy):
    """Arc Shot — arcing arrow trajectory."""
    import math
    for angle in range(0, 180, 8):
        rad = math.radians(angle)
        x = int(6 + angle / 180 * 20)
        y = int(20 - 12 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
    # arrowhead at end
    draw_pixel(draw, 26, 20, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 19, c["highlight"], ox, oy)


def icon_pinned(draw, c, ox, oy):
    """Pin Shot — arrow pinning to ground."""
    draw_line_px(draw, 16, 4, 16, 22, c["accent"], ox, oy)
    draw_pixel(draw, 16, 3, c["highlight"], ox, oy)
    draw_pixel(draw, 15, 4, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 4, c["highlight"], ox, oy)
    # ground
    draw_line_px(draw, 6, 24, 26, 24, c["secondary"], ox, oy)
    # cracks
    draw_line_px(draw, 14, 24, 12, 27, c["primary"], ox, oy)
    draw_line_px(draw, 18, 24, 20, 27, c["primary"], ox, oy)


def icon_spread_knives(draw, c, ox, oy):
    """Fan Throw — spread of projectiles."""
    import math
    for i in range(5):
        angle = math.radians(-60 + i * 30)
        x2 = int(10 + 14 * math.cos(angle))
        y2 = int(16 + 14 * math.sin(angle))
        draw_line_px(draw, 10, 16, x2, y2, c["accent"], ox, oy)
        draw_pixel(draw, x2, y2, c["highlight"], ox, oy)


def icon_ricochet(draw, c, ox, oy):
    """Bounce Shot — ricocheting projectile."""
    # zigzag path
    draw_line_px(draw, 6, 8, 14, 18, c["accent"], ox, oy)
    draw_line_px(draw, 14, 18, 22, 10, c["accent"], ox, oy)
    draw_line_px(draw, 22, 10, 28, 20, c["accent"], ox, oy)
    # bounce sparks
    draw_pixel(draw, 14, 18, c["highlight"], ox, oy)
    draw_pixel(draw, 22, 10, c["highlight"], ox, oy)


def icon_scope(draw, c, ox, oy):
    """Snipe — scope/crosshair with zoom."""
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 6, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 3, 16, 27, c["accent"], ox, oy)
    draw_line_px(draw, 4, 15, 28, 15, c["accent"], ox, oy)
    draw_pixel(draw, 16, 15, c["highlight"], ox, oy)


def icon_dodge_roll(draw, c, ox, oy):
    """Dodge Roll — rolling figure."""
    # circular arrow suggesting roll
    import math
    for angle in range(30, 330, 10):
        rad = math.radians(angle)
        x = int(16 + 8 * math.cos(rad))
        y = int(15 + 8 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
    # arrowhead
    draw_pixel(draw, 22, 8, c["highlight"], ox, oy)
    draw_pixel(draw, 23, 10, c["highlight"], ox, oy)
    # figure in center
    draw_circle_filled(draw, 16, 15, 3, c["primary"], ox, oy)


def icon_leap_back(draw, c, ox, oy):
    """Disengage — figure leaping back."""
    # figure
    draw_circle_filled(draw, 12, 12, 3, c["primary"], ox, oy)
    draw_line_px(draw, 12, 15, 12, 22, c["primary"], ox, oy)
    # arrow pointing away
    draw_line_px(draw, 16, 15, 26, 15, c["accent"], ox, oy)
    draw_pixel(draw, 25, 13, c["accent"], ox, oy)
    draw_pixel(draw, 25, 17, c["accent"], ox, oy)
    draw_pixel(draw, 27, 15, c["highlight"], ox, oy)


def icon_snare(draw, c, ox, oy):
    """Snare — rope trap."""
    draw_circle_outline(draw, 16, 18, 7, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 18, 5, c["primary"], ox, oy)
    # rope end
    draw_line_px(draw, 16, 11, 16, 5, c["secondary"], ox, oy)


def icon_tripwire(draw, c, ox, oy):
    """Tripwire — wire across ground."""
    # two posts
    draw_rect(draw, 6, 10, 9, 24, c["secondary"], ox, oy)
    draw_rect(draw, 23, 10, 26, 24, c["secondary"], ox, oy)
    # wire
    draw_line_px(draw, 9, 18, 23, 18, c["accent"], ox, oy)
    draw_line_px(draw, 9, 17, 23, 17, c["highlight"], ox, oy)


def icon_decoy(draw, c, ox, oy):
    """Decoy — dummy figure."""
    # stick figure on cross
    draw_line_px(draw, 16, 8, 16, 26, c["secondary"], ox, oy)
    draw_line_px(draw, 10, 14, 22, 14, c["secondary"], ox, oy)
    # head
    draw_circle_outline(draw, 16, 8, 3, c["accent"], ox, oy)
    # question mark
    draw_pixel(draw, 16, 7, c["highlight"], ox, oy)


def icon_ambush(draw, c, ox, oy):
    """Ambush — hidden figure striking."""
    # shadow
    draw_rect(draw, 6, 8, 14, 24, c["secondary"], ox, oy)
    # blade emerging
    draw_line_px(draw, 14, 12, 26, 12, c["accent"], ox, oy)
    draw_line_px(draw, 14, 13, 26, 13, c["primary"], ox, oy)
    # exclamation
    draw_rect(draw, 22, 6, 24, 10, c["highlight"], ox, oy)
    draw_pixel(draw, 23, 12, c["highlight"], ox, oy)


def icon_speed_lines(draw, c, ox, oy):
    """Haste — speed lines."""
    # figure running right
    draw_circle_filled(draw, 18, 10, 3, c["primary"], ox, oy)
    draw_line_px(draw, 18, 13, 18, 20, c["primary"], ox, oy)
    draw_line_px(draw, 18, 20, 22, 26, c["primary"], ox, oy)
    draw_line_px(draw, 18, 20, 14, 26, c["primary"], ox, oy)
    # speed lines behind
    for i in range(4):
        y = 10 + i * 4
        draw_line_px(draw, 4, y, 12, y, c["accent"], ox, oy)


def icon_radar_pulse(draw, c, ox, oy):
    """Sense — radiating pulse."""
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 15, 6, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 12, c["secondary"], ox, oy)


def icon_body_glow(draw, c, ox, oy):
    """Fortify — glowing body shield."""
    # body silhouette
    draw_circle_filled(draw, 16, 10, 4, c["primary"], ox, oy)
    draw_rect(draw, 12, 14, 20, 24, c["primary"], ox, oy)
    # glow aura
    draw_circle_outline(draw, 16, 16, 12, c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 16, 11, c["accent"], ox, oy)


# ── Mage spell icons ────────────────────────────────────────────────────────

def icon_flame(draw, c, ox, oy):
    """Fire base — flame."""
    draw_rect(draw, 13, 14, 19, 24, c["primary"], ox, oy)
    draw_rect(draw, 14, 10, 18, 14, c["accent"], ox, oy)
    draw_rect(draw, 15, 6, 17, 10, c["highlight"], ox, oy)
    draw_pixel(draw, 16, 5, c["highlight"], ox, oy)
    # flickers
    draw_pixel(draw, 11, 16, c["accent"], ox, oy)
    draw_pixel(draw, 21, 14, c["accent"], ox, oy)

def icon_fireball(draw, c, ox, oy):
    """Fireball — flaming sphere."""
    draw_circle_filled(draw, 16, 15, 7, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 5, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)
    # trail
    draw_line_px(draw, 4, 15, 9, 15, c["primary"], ox, oy)
    draw_line_px(draw, 5, 13, 8, 13, c["accent"], ox, oy)
    draw_line_px(draw, 5, 17, 8, 17, c["accent"], ox, oy)

def icon_flame_wall(draw, c, ox, oy):
    """Flame Wall — wall of fire."""
    for x in range(6, 26, 3):
        h = 8 + (x % 7)
        draw_rect(draw, x, 24 - h, x + 2, 24, c["primary"], ox, oy)
        draw_rect(draw, x, 24 - h, x + 1, 24 - h + 3, c["highlight"], ox, oy)

def icon_ignite(draw, c, ox, oy):
    """Ignite — burning target."""
    # target outline
    draw_circle_outline(draw, 16, 15, 8, c["secondary"], ox, oy)
    # flames on it
    draw_rect(draw, 14, 10, 18, 20, c["primary"], ox, oy)
    draw_rect(draw, 15, 7, 17, 10, c["accent"], ox, oy)
    draw_pixel(draw, 16, 6, c["highlight"], ox, oy)

def icon_inferno(draw, c, ox, oy):
    """Inferno — raging flames."""
    # large flame mass
    for x in range(4, 28, 2):
        h = 6 + (x * 3 % 11)
        draw_rect(draw, x, 28 - h, x + 1, 28, c["primary"], ox, oy)
        if h > 10:
            draw_pixel(draw, x, 28 - h, c["highlight"], ox, oy)
    draw_rect(draw, 10, 8, 22, 14, c["accent"], ox, oy)

def icon_water_drop(draw, c, ox, oy):
    """Water base — water drop."""
    draw_triangle_up(draw, 16, 8, 3, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 17, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 16, 4, c["accent"], ox, oy)
    draw_pixel(draw, 14, 14, c["highlight"], ox, oy)

def icon_frost_bolt(draw, c, ox, oy):
    """Frost Bolt — ice shard."""
    draw_diamond(draw, 16, 14, 8, c["primary"], ox, oy)
    draw_diamond(draw, 16, 14, 5, c["accent"], ox, oy)
    draw_diamond(draw, 16, 14, 2, c["highlight"], ox, oy)
    # trail
    draw_line_px(draw, 4, 22, 10, 18, c["secondary"], ox, oy)

def icon_freeze(draw, c, ox, oy):
    """Freeze — ice crystal."""
    # snowflake-like
    draw_line_px(draw, 16, 4, 16, 26, c["accent"], ox, oy)
    draw_line_px(draw, 6, 15, 26, 15, c["accent"], ox, oy)
    draw_line_px(draw, 8, 7, 24, 23, c["primary"], ox, oy)
    draw_line_px(draw, 24, 7, 8, 23, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)

def icon_wave(draw, c, ox, oy):
    """Tidal Wave — crashing wave."""
    import math
    for x in range(4, 28):
        y = int(14 + 4 * math.sin((x - 4) * math.pi / 8))
        draw_rect(draw, x, y, x, 26, c["primary"], ox, oy)
        draw_pixel(draw, x, y, c["accent"], ox, oy)
        draw_pixel(draw, x, y - 1, c["highlight"], ox, oy)

def icon_mist(draw, c, ox, oy):
    """Mist Veil — swirling mist."""
    for i in range(6):
        x = 6 + i * 4
        y = 12 + (i % 3) * 4
        draw_circle_filled(draw, x, y, 3, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 4, c["primary"], ox, oy)

def icon_wind(draw, c, ox, oy):
    """Air base — wind swirl."""
    import math
    for angle in range(0, 540, 12):
        rad = math.radians(angle)
        r = 3 + angle / 120
        x = int(16 + r * math.cos(rad))
        y = int(15 + r * math.sin(rad))
        if 2 <= x <= 29 and 2 <= y <= 29:
            draw_pixel(draw, x, y, c["accent"], ox, oy)

def icon_lightning(draw, c, ox, oy):
    """Lightning bolt."""
    pts = [(16, 4), (12, 14), (18, 14), (14, 26)]
    for i in range(len(pts) - 1):
        draw_line_px(draw, pts[i][0], pts[i][1], pts[i+1][0], pts[i+1][1], c["accent"], ox, oy)
        draw_line_px(draw, pts[i][0]+1, pts[i][1], pts[i+1][0]+1, pts[i+1][1], c["highlight"], ox, oy)

def icon_gust(draw, c, ox, oy):
    """Gust — wind blast."""
    for i in range(4):
        y = 8 + i * 5
        draw_line_px(draw, 6 + i, y, 24 - i, y, c["accent"], ox, oy)
        draw_pixel(draw, 25 - i, y, c["highlight"], ox, oy)

def icon_chain_lightning(draw, c, ox, oy):
    """Chain Shock — branching lightning."""
    # main bolt
    draw_line_px(draw, 8, 6, 14, 15, c["accent"], ox, oy)
    # branch 1
    draw_line_px(draw, 14, 15, 24, 10, c["highlight"], ox, oy)
    # branch 2
    draw_line_px(draw, 14, 15, 22, 24, c["highlight"], ox, oy)
    # sparks
    draw_pixel(draw, 24, 10, c["accent"], ox, oy)
    draw_pixel(draw, 22, 24, c["accent"], ox, oy)

def icon_storm(draw, c, ox, oy):
    """Tempest — storm cloud."""
    # cloud
    draw_circle_filled(draw, 12, 10, 5, c["secondary"], ox, oy)
    draw_circle_filled(draw, 20, 10, 5, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 8, 5, c["primary"], ox, oy)
    # rain/lightning below
    draw_line_px(draw, 12, 16, 10, 24, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 16, 24, c["accent"], ox, oy)
    draw_line_px(draw, 20, 16, 22, 24, c["accent"], ox, oy)

def icon_rock(draw, c, ox, oy):
    """Earth base — rock."""
    pts = [(10, 22), (6, 14), (12, 8), (20, 6), (26, 12), (24, 22)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw.polygon([(ox+x, oy+y) for x, y in pts], outline=c["secondary"])
    # highlight facet
    draw_rect(draw, 14, 10, 20, 16, c["accent"], ox, oy)

def icon_stone_spike(draw, c, ox, oy):
    """Stone Spike — sharp rock eruption."""
    # spikes coming up
    draw_triangle_up(draw, 12, 10, 5, c["primary"], ox, oy)
    draw_triangle_up(draw, 20, 8, 6, c["accent"], ox, oy)
    draw_triangle_up(draw, 16, 6, 4, c["highlight"], ox, oy)
    # ground
    draw_line_px(draw, 4, 24, 28, 24, c["secondary"], ox, oy)

def icon_quake(draw, c, ox, oy):
    """Quake — cracking ground."""
    # ground line
    draw_line_px(draw, 4, 16, 28, 16, c["primary"], ox, oy)
    # cracks
    draw_line_px(draw, 16, 16, 12, 26, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 20, 24, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 8, 22, c["secondary"], ox, oy)
    # debris
    draw_rect(draw, 10, 8, 13, 11, c["primary"], ox, oy)
    draw_rect(draw, 19, 6, 22, 9, c["primary"], ox, oy)

def icon_petrify(draw, c, ox, oy):
    """Petrify — stone figure."""
    # figure turning to stone
    draw_circle_filled(draw, 16, 8, 4, c["primary"], ox, oy)
    draw_rect(draw, 12, 12, 20, 24, c["primary"], ox, oy)
    # stone texture lines
    draw_line_px(draw, 13, 14, 19, 14, c["secondary"], ox, oy)
    draw_line_px(draw, 13, 18, 19, 18, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 12, 16, 24, c["secondary"], ox, oy)

def icon_stone_armor(draw, c, ox, oy):
    """Earthen Armor — rock coating."""
    # body outline
    draw_circle_filled(draw, 16, 10, 5, c["accent"], ox, oy)
    draw_rect(draw, 10, 15, 22, 26, c["accent"], ox, oy)
    # stone overlay
    draw_circle_outline(draw, 16, 10, 6, c["primary"], ox, oy)
    draw_rect(draw, 9, 14, 9, 26, c["primary"], ox, oy)
    draw_rect(draw, 23, 14, 23, 26, c["primary"], ox, oy)

def icon_star_light(draw, c, ox, oy):
    """Light base — radiant star."""
    draw_star(draw, 16, 15, 10, 4, 6, c["accent"], ox, oy)
    draw_star(draw, 16, 15, 6, 3, 6, c["highlight"], ox, oy)

def icon_energy_beam(draw, c, ox, oy):
    """Energy Blast — concentrated beam."""
    draw_circle_filled(draw, 8, 15, 4, c["accent"], ox, oy)
    draw_rect(draw, 12, 13, 28, 17, c["primary"], ox, oy)
    draw_rect(draw, 12, 14, 28, 16, c["highlight"], ox, oy)

def icon_radiance(draw, c, ox, oy):
    """Radiance — burst of light."""
    draw_circle_filled(draw, 16, 15, 5, c["highlight"], ox, oy)
    # rays
    import math
    for i in range(8):
        angle = math.radians(i * 45)
        x2 = int(16 + 12 * math.cos(angle))
        y2 = int(15 + 12 * math.sin(angle))
        draw_line_px(draw, 16, 15, x2, y2, c["accent"], ox, oy)

def icon_heal(draw, c, ox, oy):
    """Heal — healing cross."""
    draw_rect(draw, 13, 6, 19, 24, c["accent"], ox, oy)
    draw_rect(draw, 7, 12, 25, 18, c["accent"], ox, oy)
    draw_rect(draw, 14, 7, 18, 23, c["highlight"], ox, oy)
    draw_rect(draw, 8, 13, 24, 17, c["highlight"], ox, oy)

def icon_sparkles(draw, c, ox, oy):
    """Purify — cleansing sparkles."""
    positions = [(8, 8), (20, 6), (12, 20), (24, 16), (16, 12), (6, 16), (22, 24)]
    for x, y in positions:
        draw_star(draw, x, y, 3, 1, 4, c["accent"], ox, oy)
    draw_star(draw, 16, 12, 4, 2, 4, c["highlight"], ox, oy)

def icon_void(draw, c, ox, oy):
    """Dark base — dark void."""
    draw_circle_filled(draw, 16, 15, 10, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 7, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 4, (20, 15, 30), ox, oy)

def icon_drain(draw, c, ox, oy):
    """Drain Life — siphon."""
    # source
    draw_circle_filled(draw, 8, 15, 4, (200, 60, 60), ox, oy)
    # destination
    draw_circle_filled(draw, 24, 15, 4, c["primary"], ox, oy)
    # flow
    draw_line_px(draw, 12, 13, 20, 13, (200, 60, 60), ox, oy)
    draw_line_px(draw, 12, 15, 20, 15, c["accent"], ox, oy)
    draw_line_px(draw, 12, 17, 20, 17, (200, 60, 60), ox, oy)

def icon_skull(draw, c, ox, oy):
    """Curse — skull debuff."""
    draw_circle_filled(draw, 16, 12, 7, c["primary"], ox, oy)
    # eye sockets
    draw_rect(draw, 12, 10, 14, 13, c["secondary"], ox, oy)
    draw_rect(draw, 18, 10, 20, 13, c["secondary"], ox, oy)
    # nose
    draw_pixel(draw, 16, 15, c["secondary"], ox, oy)
    # jaw
    draw_rect(draw, 12, 18, 20, 22, c["primary"], ox, oy)
    draw_line_px(draw, 13, 20, 19, 20, c["secondary"], ox, oy)

def icon_dark_bolt(draw, c, ox, oy):
    """Shadow Bolt — dark projectile."""
    draw_circle_filled(draw, 18, 15, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 18, 15, 3, c["accent"], ox, oy)
    # dark trail
    draw_line_px(draw, 4, 15, 12, 15, c["secondary"], ox, oy)
    draw_line_px(draw, 6, 13, 10, 13, c["primary"], ox, oy)
    draw_line_px(draw, 6, 17, 10, 17, c["primary"], ox, oy)

def icon_void_zone(draw, c, ox, oy):
    """Void Zone — dark pool."""
    # dark ellipse on ground
    draw.ellipse((ox+4, oy+12, ox+28, oy+26), fill=c["secondary"])
    draw.ellipse((ox+6, oy+14, ox+26, oy+24), fill=c["primary"])
    draw.ellipse((ox+10, oy+16, ox+22, oy+22), fill=(20, 15, 30))
    # wisps rising
    draw_line_px(draw, 12, 12, 10, 6, c["accent"], ox, oy)
    draw_line_px(draw, 20, 12, 22, 6, c["accent"], ox, oy)

def icon_heart_heal(draw, c, ox, oy):
    """Restoration base — healing heart."""
    # heart shape
    draw_circle_filled(draw, 12, 12, 5, c["primary"], ox, oy)
    draw_circle_filled(draw, 20, 12, 5, c["primary"], ox, oy)
    pts = [(7, 14), (16, 25), (25, 14)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    # cross on heart
    draw_rect(draw, 15, 10, 17, 20, c["highlight"], ox, oy)
    draw_rect(draw, 12, 14, 20, 16, c["highlight"], ox, oy)

def icon_quick_heal(draw, c, ox, oy):
    """Mend — quick sparkle heal."""
    draw_star(draw, 16, 15, 8, 3, 4, c["accent"], ox, oy)
    draw_star(draw, 16, 15, 5, 2, 4, c["highlight"], ox, oy)
    # small + sign
    draw_rect(draw, 15, 13, 17, 17, c["highlight"], ox, oy)
    draw_rect(draw, 14, 14, 18, 16, c["highlight"], ox, oy)

def icon_barrier(draw, c, ox, oy):
    """Barrier — magic shield bubble."""
    draw_circle_outline(draw, 16, 15, 10, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 8, c["secondary"], ox, oy)
    # inner glow
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)

def icon_cleanse(draw, c, ox, oy):
    """Cleanse — purifying wave."""
    import math
    for x in range(4, 28):
        y = int(15 + 5 * math.sin((x - 4) * math.pi / 6))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
        draw_pixel(draw, x, y + 1, c["primary"], ox, oy)
    # sparkles
    draw_pixel(draw, 10, 8, c["highlight"], ox, oy)
    draw_pixel(draw, 22, 10, c["highlight"], ox, oy)

def icon_regen(draw, c, ox, oy):
    """Regeneration — green pulse."""
    draw_circle_filled(draw, 16, 15, 8, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 5, c["primary"], ox, oy)
    # upward arrows (growth)
    draw_triangle_up(draw, 16, 10, 3, c["highlight"], ox, oy)
    draw_triangle_up(draw, 10, 14, 2, c["accent"], ox, oy)
    draw_triangle_up(draw, 22, 14, 2, c["accent"], ox, oy)

def icon_neural(draw, c, ox, oy):
    """Amplification base — brain/neural."""
    draw_circle_filled(draw, 16, 14, 8, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 14, 6, c["primary"], ox, oy)
    # neural connections
    draw_line_px(draw, 10, 10, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 22, 10, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 10, 18, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 22, 18, 16, 14, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 14, 2, c["highlight"], ox, oy)

def icon_mana_burst(draw, c, ox, oy):
    """Mana Surge — blue mana burst."""
    draw_circle_filled(draw, 16, 15, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)
    # burst rays
    import math
    for i in range(6):
        angle = math.radians(i * 60)
        x2 = int(16 + 10 * math.cos(angle))
        y2 = int(15 + 10 * math.sin(angle))
        draw_line_px(draw, 16, 15, x2, y2, c["accent"], ox, oy)

def icon_quick_cast(draw, c, ox, oy):
    """Quick Cast — speed cast."""
    # clock-like symbol
    draw_circle_outline(draw, 16, 15, 8, c["primary"], ox, oy)
    draw_line_px(draw, 16, 15, 16, 8, c["accent"], ox, oy)
    draw_line_px(draw, 16, 15, 22, 13, c["accent"], ox, oy)
    # speed lines
    draw_line_px(draw, 4, 10, 8, 12, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 15, 8, 15, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 20, 8, 18, c["highlight"], ox, oy)

def icon_attunement(draw, c, ox, oy):
    """Attunement — elemental harmony."""
    # four small elemental dots
    draw_circle_filled(draw, 10, 10, 3, (220, 80, 40), ox, oy)  # fire
    draw_circle_filled(draw, 22, 10, 3, (60, 130, 220), ox, oy)  # water
    draw_circle_filled(draw, 10, 20, 3, (160, 120, 70), ox, oy)  # earth
    draw_circle_filled(draw, 22, 20, 3, (100, 200, 230), ox, oy)  # air
    # center connection
    draw_diamond(draw, 16, 15, 3, c["highlight"], ox, oy)

def icon_focus(draw, c, ox, oy):
    """Focus Channel — meditation circle."""
    draw_circle_outline(draw, 16, 15, 10, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 7, c["accent"], ox, oy)
    # figure sitting
    draw_circle_filled(draw, 16, 12, 3, c["highlight"], ox, oy)
    draw_triangle_down(draw, 16, 19, 4, c["primary"], ox, oy)

def icon_danger_spark(draw, c, ox, oy):
    """Overcharge base — crackling danger."""
    draw_circle_filled(draw, 16, 15, 8, c["secondary"], ox, oy)
    # lightning through it
    icon_lightning(draw, c, ox, oy)
    # danger border
    draw_circle_outline(draw, 16, 15, 10, c["primary"], ox, oy)

def icon_burning_brain(draw, c, ox, oy):
    """Neural Burn — burning brain."""
    draw_circle_filled(draw, 16, 14, 7, c["primary"], ox, oy)
    # brain folds
    draw_line_px(draw, 16, 8, 16, 20, c["secondary"], ox, oy)
    # flames on top
    draw_rect(draw, 12, 4, 14, 8, c["accent"], ox, oy)
    draw_rect(draw, 16, 3, 18, 8, c["highlight"], ox, oy)
    draw_rect(draw, 20, 5, 22, 8, c["accent"], ox, oy)

def icon_wild_mana(draw, c, ox, oy):
    """Mana Frenzy — wild mana storm."""
    # chaotic energy
    import math
    for i in range(12):
        angle = math.radians(i * 30)
        r = 8 + (i % 3) * 2
        x = int(16 + r * math.cos(angle))
        y = int(15 + r * math.sin(angle))
        draw_line_px(draw, 16, 15, x, y, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)

def icon_pain_power(draw, c, ox, oy):
    """Pain Conduit — pain to power."""
    # red incoming
    draw_line_px(draw, 4, 8, 14, 15, (200, 60, 60), ox, oy)
    draw_line_px(draw, 4, 22, 14, 15, (200, 60, 60), ox, oy)
    # center transform
    draw_diamond(draw, 16, 15, 4, c["primary"], ox, oy)
    # blue outgoing
    draw_line_px(draw, 18, 15, 28, 8, c["accent"], ox, oy)
    draw_line_px(draw, 18, 15, 28, 22, c["accent"], ox, oy)

def icon_last_resort(draw, c, ox, oy):
    """Last Resort — final burst."""
    # explosion
    draw_star(draw, 16, 15, 12, 5, 8, c["primary"], ox, oy)
    draw_star(draw, 16, 15, 8, 3, 8, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)


# ── Sprite sheet definitions ────────────────────────────────────────────────

# Skills: each entry = (draw_function, color_palette)
# Laid out 5 per row: [base] [spec1] [spec2] [spec3] [spec4]

SKILLS_LAYOUT = [
    # Row 0-4: Warrior Body
    # Unarmed
    (icon_fist, C_WARRIOR_BODY), (icon_generic_attack, C_WARRIOR_BODY),
    (icon_kick, C_WARRIOR_BODY), (icon_grapple, C_WARRIOR_BODY),
    (icon_elbow, C_WARRIOR_BODY),
    # Bladed
    (icon_sword, C_WARRIOR_BODY), (icon_slash_arc, C_WARRIOR_BODY),
    (icon_stab, C_WARRIOR_BODY), (icon_overhead_chop, C_WARRIOR_BODY),
    (icon_crossed_swords, C_WARRIOR_BODY),
    # Blunt
    (icon_hammer, C_WARRIOR_BODY), (icon_overhead_chop, C_WARRIOR_BODY),
    (icon_slash_arc, C_WARRIOR_BODY), (icon_generic_attack, C_WARRIOR_BODY),
    (icon_generic_aoe, C_WARRIOR_BODY),
    # Polearms
    (icon_spear, C_WARRIOR_BODY), (icon_stab, C_WARRIOR_BODY),
    (icon_slash_arc, C_WARRIOR_BODY), (icon_shield_raised, C_WARRIOR_BODY),
    (icon_dodge_roll, C_WARRIOR_BODY),
    # Shields
    (icon_shield, C_WARRIOR_BODY), (icon_shield_raised, C_WARRIOR_BODY),
    (icon_shield_bash, C_WARRIOR_BODY), (icon_deflect, C_WARRIOR_BODY),
    (icon_fortress, C_WARRIOR_BODY),

    # Row 5-6: Warrior Mind
    # Inner
    (icon_brain, C_WARRIOR_MIND), (icon_focused_eye, C_WARRIOR_MIND),
    (icon_armor_body, C_WARRIOR_MIND), (icon_healing_breath, C_WARRIOR_MIND),
    (icon_helmet, C_WARRIOR_MIND),
    # Outer
    (icon_shout, C_WARRIOR_MIND), (icon_generic_aoe, C_WARRIOR_MIND),
    (icon_generic_debuff, C_WARRIOR_MIND), (icon_generic_aoe, C_WARRIOR_MIND),
    (icon_generic_debuff, C_WARRIOR_MIND),

    # Row 7-10: Ranger Arms
    # Drawn
    (icon_bow, C_RANGER_ARMS), (icon_generic_attack, C_RANGER_ARMS),
    (icon_multiple_arrows, C_RANGER_ARMS), (icon_arc_arrow, C_RANGER_ARMS),
    (icon_pinned, C_RANGER_ARMS),
    # Thrown
    (icon_throwing_knife, C_RANGER_ARMS), (icon_generic_attack, C_RANGER_ARMS),
    (icon_generic_attack, C_RANGER_ARMS), (icon_spread_knives, C_RANGER_ARMS),
    (icon_ricochet, C_RANGER_ARMS),
    # Firearms
    (icon_pistol, C_RANGER_ARMS), (icon_generic_attack, C_RANGER_ARMS),
    (icon_scope, C_RANGER_ARMS), (icon_multiple_arrows, C_RANGER_ARMS),
    (icon_scope, C_RANGER_ARMS),
    # Melee
    (icon_buckler, C_RANGER_ARMS), (icon_crossed_swords, C_RANGER_ARMS),
    (icon_shield_raised, C_RANGER_ARMS), (icon_generic_attack, C_RANGER_ARMS),
    (icon_generic_debuff, C_RANGER_ARMS),

    # Row 11-13: Ranger Instinct
    # Precision
    (icon_crosshair, C_RANGER_INSTINCT), (icon_scope, C_RANGER_INSTINCT),
    (icon_focused_eye, C_RANGER_INSTINCT), (icon_generic_buff, C_RANGER_INSTINCT),
    (icon_crosshair, C_RANGER_INSTINCT),
    # Awareness
    (icon_eye, C_RANGER_INSTINCT), (icon_radar_pulse, C_RANGER_INSTINCT),
    (icon_dodge_roll, C_RANGER_INSTINCT), (icon_leap_back, C_RANGER_INSTINCT),
    (icon_dodge_roll, C_RANGER_INSTINCT),
    # Trapping
    (icon_trap, C_RANGER_INSTINCT), (icon_snare, C_RANGER_INSTINCT),
    (icon_tripwire, C_RANGER_INSTINCT), (icon_decoy, C_RANGER_INSTINCT),
    (icon_ambush, C_RANGER_INSTINCT),

    # Row 14: Innate
    (icon_speed_lines, C_INNATE), (icon_radar_pulse, C_INNATE),
    (icon_body_glow, C_INNATE),
]

SPELLS_LAYOUT = [
    # Row 0-5: Arcane
    # Fire
    (icon_flame, C_FIRE), (icon_fireball, C_FIRE),
    (icon_flame_wall, C_FIRE), (icon_ignite, C_FIRE),
    (icon_inferno, C_FIRE),
    # Water
    (icon_water_drop, C_WATER), (icon_frost_bolt, C_WATER),
    (icon_freeze, C_WATER), (icon_wave, C_WATER),
    (icon_mist, C_WATER),
    # Air
    (icon_wind, C_AIR), (icon_lightning, C_AIR),
    (icon_gust, C_AIR), (icon_chain_lightning, C_AIR),
    (icon_storm, C_AIR),
    # Earth
    (icon_rock, C_EARTH), (icon_stone_spike, C_EARTH),
    (icon_quake, C_EARTH), (icon_petrify, C_EARTH),
    (icon_stone_armor, C_EARTH),
    # Light
    (icon_star_light, C_LIGHT), (icon_energy_beam, C_LIGHT),
    (icon_radiance, C_LIGHT), (icon_heal, C_LIGHT),
    (icon_sparkles, C_LIGHT),
    # Dark
    (icon_void, C_DARK), (icon_drain, C_DARK),
    (icon_skull, C_DARK), (icon_dark_bolt, C_DARK),
    (icon_void_zone, C_DARK),

    # Row 6-8: Conduit
    # Restoration
    (icon_heart_heal, C_RESTORATION), (icon_quick_heal, C_RESTORATION),
    (icon_barrier, C_RESTORATION), (icon_cleanse, C_RESTORATION),
    (icon_regen, C_RESTORATION),
    # Amplification
    (icon_neural, C_AMPLIFICATION), (icon_mana_burst, C_AMPLIFICATION),
    (icon_quick_cast, C_AMPLIFICATION), (icon_attunement, C_AMPLIFICATION),
    (icon_focus, C_AMPLIFICATION),
    # Overcharge
    (icon_danger_spark, C_OVERCHARGE), (icon_burning_brain, C_OVERCHARGE),
    (icon_wild_mana, C_OVERCHARGE), (icon_pain_power, C_OVERCHARGE),
    (icon_last_resort, C_OVERCHARGE),
]

# ── Skill/spell name mappings (for metadata) ───────────────────────────────

SKILL_NAMES = [
    # Warrior Body
    "unarmed", "punch", "kick", "grapple", "elbow_knee",
    "bladed", "slash", "thrust", "cleave", "parry",
    "blunt", "smash", "sweep", "crush", "shatter",
    "polearms", "thrust_pole", "sweep_pole", "brace", "vault",
    "shields", "block", "shield_bash", "deflect", "bulwark",
    # Warrior Mind
    "inner", "battle_focus", "pain_tolerance", "second_wind", "iron_will",
    "outer", "war_cry", "intimidate", "menacing_presence", "battle_roar",
    # Ranger Arms
    "drawn", "power_shot", "rapid_fire", "arc_shot", "pin_shot",
    "thrown", "knife_throw", "axe_throw", "fan_throw", "bounce_shot",
    "firearms", "quick_draw", "steady_shot", "burst_fire", "snipe",
    "melee_ranger", "parry_ranger", "block_ranger", "riposte", "disarm",
    # Ranger Instinct
    "precision", "steady_aim", "weak_spot", "range_calc", "lead_shot",
    "awareness", "threat_sense", "dodge_roll", "disengage", "tumble",
    "trapping", "snare", "tripwire", "decoy", "ambush",
    # Innate
    "haste", "sense", "fortify",
]

SPELL_NAMES = [
    # Arcane
    "fire", "fireball", "flame_wall", "ignite", "inferno",
    "water", "frost_bolt", "freeze", "tidal_wave", "mist_veil",
    "air", "lightning", "gust", "chain_shock", "tempest",
    "earth", "stone_spike", "quake", "petrify", "earthen_armor",
    "light", "energy_blast", "radiance", "heal", "purify",
    "dark", "drain_life", "curse", "shadow_bolt", "void_zone",
    # Conduit
    "restoration", "mend", "barrier", "cleanse", "regeneration",
    "amplification", "mana_surge", "quick_cast", "attunement", "focus_channel",
    "overcharge", "neural_burn", "mana_frenzy", "pain_conduit", "last_resort",
]


# ── Sheet generation ────────────────────────────────────────────────────────

def generate_sheet(layout, names, output_path):
    """Generate a 512x512 sprite sheet from icon layout."""
    img = Image.new("RGBA", (SHEET_SIZE, SHEET_SIZE), BG_COLOR)
    draw = ImageDraw.Draw(img)

    for idx, (draw_fn, colors) in enumerate(layout):
        col = idx % COLS
        row = idx // COLS
        ox = col * ICON_SIZE
        oy = row * ICON_SIZE

        # Draw 1px dark border for visual separation
        draw_rect(draw, 0, 0, ICON_SIZE - 1, ICON_SIZE - 1, BG_COLOR, ox, oy)

        # Draw the icon
        draw_fn(draw, colors, ox, oy)

    img.save(output_path, "PNG")
    print(f"  Saved: {output_path} ({len(layout)} icons, {SHEET_SIZE}x{SHEET_SIZE})")
    return img


def generate_index(names, sheet_name, output_path):
    """Generate a JSON index mapping icon names to grid positions."""
    import json
    index = {}
    for idx, name in enumerate(names):
        col = idx % COLS
        row = idx // COLS
        index[name] = {
            "x": col * ICON_SIZE,
            "y": row * ICON_SIZE,
            "w": ICON_SIZE,
            "h": ICON_SIZE,
            "col": col,
            "row": row,
            "sheet": sheet_name,
        }
    with open(output_path, "w") as f:
        json.dump(index, f, indent=2)
    print(f"  Saved: {output_path} ({len(names)} entries)")


def main():
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    icons_dir = os.path.join(project_root, "assets", "icons")
    os.makedirs(icons_dir, exist_ok=True)

    print("Generating skill/spell icon sprite sheets (512x512)...")
    print()

    # Skills sheet
    print("Skills sprite sheet (Warrior + Ranger + Innate):")
    skills_path = os.path.join(icons_dir, "skills_icons.png")
    generate_sheet(SKILLS_LAYOUT, SKILL_NAMES, skills_path)
    generate_index(SKILL_NAMES, "skills_icons.png",
                   os.path.join(icons_dir, "skills_icons.json"))

    print()

    # Spells sheet
    print("Spells sprite sheet (Mage Arcane + Conduit):")
    spells_path = os.path.join(icons_dir, "spells_icons.png")
    generate_sheet(SPELLS_LAYOUT, SPELL_NAMES, spells_path)
    generate_index(SPELL_NAMES, "spells_icons.png",
                   os.path.join(icons_dir, "spells_icons.json"))

    print()
    print("Done! Sprite sheets ready in assets/icons/")
    print(f"  Skills: {len(SKILLS_LAYOUT)} icons in 5-col rows (base + 4 specific)")
    print(f"  Spells: {len(SPELLS_LAYOUT)} icons in 5-col rows (base + 4 specific)")
    print(f"  Grid: {COLS} cols x {ROWS} rows, {ICON_SIZE}x{ICON_SIZE}px per icon")


if __name__ == "__main__":
    main()
