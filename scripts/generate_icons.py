#!/usr/bin/env python3
"""Generate combined abilities icon sprite sheet (512x1024).

Draws pixel-art icons using Pillow. Each icon is 32x32 on a dark background.
Output:
  assets/icons/abilities_icons.png  -- combined sprite sheet
  assets/icons/abilities_icons.json -- atlas index

Layout: 16 columns x 32 rows grid.
Each row = one mastery: [mastery icon] [ability 1] [ability 2] ... [remaining empty]

Run: python3 scripts/generate_icons.py
"""

import json
import math
import os
from PIL import Image, ImageDraw

# -- Constants ---------------------------------------------------------------

ICON_SIZE = 32
SHEET_W = 512
SHEET_H = 1024
COLS = SHEET_W // ICON_SIZE   # 16
ROWS = SHEET_H // ICON_SIZE   # 32
BG_COLOR = (15, 17, 23, 255)  # #0f1117

# -- Color palettes ----------------------------------------------------------

C_INNATE = {
    "primary": (150, 180, 220),   # silver-blue #96B4DC
    "secondary": (120, 150, 190),
    "accent": (180, 210, 245),
    "highlight": (220, 235, 255),
}

C_WARRIOR_BODY = {
    "primary": (210, 170, 90),    # gold #D2AA5A
    "secondary": (180, 130, 60),
    "accent": (240, 200, 120),
    "highlight": (255, 220, 150),
}

C_WARRIOR_MIND = {
    "primary": (160, 130, 200),   # purple #A082C8
    "secondary": (120, 100, 170),
    "accent": (200, 170, 240),
    "highlight": (220, 200, 255),
}

C_RANGER_WEAPONRY = {
    "primary": (130, 170, 80),    # green #82AA50
    "secondary": (100, 140, 60),
    "accent": (170, 200, 110),
    "highlight": (200, 230, 150),
}

C_RANGER_SURVIVAL = {
    "primary": (100, 160, 160),   # teal #64A0A0
    "secondary": (70, 130, 130),
    "accent": (140, 200, 200),
    "highlight": (180, 230, 230),
}

C_FIRE = {
    "primary": (220, 80, 40),     # red-orange
    "secondary": (180, 50, 20),
    "accent": (255, 160, 50),
    "highlight": (255, 220, 100),
}

C_WATER = {
    "primary": (60, 130, 220),    # blue
    "secondary": (40, 90, 180),
    "accent": (100, 180, 255),
    "highlight": (180, 220, 255),
}

C_AIR = {
    "primary": (220, 200, 60),    # yellow
    "secondary": (180, 160, 40),
    "accent": (255, 240, 100),
    "highlight": (255, 250, 180),
}

C_EARTH = {
    "primary": (160, 120, 70),    # brown
    "secondary": (120, 90, 50),
    "accent": (200, 160, 100),
    "highlight": (180, 170, 150),
}

C_MAGE_AETHER = {
    "primary": (224, 224, 255),   # white/light #E0E0FF
    "secondary": (58, 32, 96),    # dark #3A2060
    "accent": (180, 160, 240),
    "highlight": (240, 240, 255),
}

C_MAGE_ATTUNEMENT = {
    "primary": (80, 200, 200),    # cyan #50C8C8
    "secondary": (50, 160, 160),
    "accent": (120, 230, 230),
    "highlight": (200, 250, 250),
}


# -- Drawing primitives ------------------------------------------------------

def draw_pixel(draw, x, y, color, ox=0, oy=0):
    draw.point((ox + x, oy + y), fill=color)


def draw_rect(draw, x1, y1, x2, y2, color, ox=0, oy=0):
    draw.rectangle((ox + x1, oy + y1, ox + x2, oy + y2), fill=color)


def draw_line_px(draw, x1, y1, x2, y2, color, ox=0, oy=0):
    draw.line((ox + x1, oy + y1, ox + x2, oy + y2), fill=color, width=1)


def draw_circle_filled(draw, cx, cy, r, color, ox=0, oy=0):
    draw.ellipse((ox + cx - r, oy + cy - r, ox + cx + r, oy + cy + r), fill=color)


def draw_circle_outline(draw, cx, cy, r, color, ox=0, oy=0):
    draw.ellipse((ox + cx - r, oy + cy - r, ox + cx + r, oy + cy + r), outline=color)


def draw_diamond(draw, cx, cy, r, color, ox=0, oy=0):
    pts = [(ox+cx, oy+cy-r), (ox+cx+r, oy+cy), (ox+cx, oy+cy+r), (ox+cx-r, oy+cy)]
    draw.polygon(pts, fill=color)


def draw_triangle_up(draw, cx, cy, size, color, ox=0, oy=0):
    pts = [(ox+cx, oy+cy-size), (ox+cx+size, oy+cy+size), (ox+cx-size, oy+cy+size)]
    draw.polygon(pts, fill=color)


def draw_triangle_down(draw, cx, cy, size, color, ox=0, oy=0):
    pts = [(ox+cx, oy+cy+size), (ox+cx+size, oy+cy-size), (ox+cx-size, oy+cy-size)]
    draw.polygon(pts, fill=color)


def draw_star(draw, cx, cy, r_out, r_in, points, color, ox=0, oy=0):
    pts = []
    for i in range(points * 2):
        angle = math.pi * i / points - math.pi / 2
        r = r_out if i % 2 == 0 else r_in
        pts.append((ox + cx + r * math.cos(angle), oy + cy + r * math.sin(angle)))
    draw.polygon(pts, fill=color)


# -- Icon drawing functions --------------------------------------------------
# Each draws onto ImageDraw at offset (ox, oy). All icons are 32x32.

# --- Row 0: Innate ---

def icon_innate_mastery(draw, c, ox, oy):
    """Innate mastery -- radiant body silhouette."""
    draw_circle_filled(draw, 16, 10, 4, c["primary"], ox, oy)
    draw_rect(draw, 12, 14, 20, 24, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 16, 12, c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 16, 11, c["accent"], ox, oy)

def icon_haste(draw, c, ox, oy):
    """Haste -- speed lines with running figure."""
    draw_circle_filled(draw, 18, 10, 3, c["primary"], ox, oy)
    draw_line_px(draw, 18, 13, 18, 20, c["primary"], ox, oy)
    draw_line_px(draw, 18, 20, 22, 26, c["primary"], ox, oy)
    draw_line_px(draw, 18, 20, 14, 26, c["primary"], ox, oy)
    for i in range(4):
        y = 10 + i * 4
        draw_line_px(draw, 4, y, 12, y, c["accent"], ox, oy)

def icon_sense(draw, c, ox, oy):
    """Sense -- radiating pulse."""
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 15, 6, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 12, c["secondary"], ox, oy)

def icon_fortify(draw, c, ox, oy):
    """Fortify -- glowing body shield."""
    draw_circle_filled(draw, 16, 10, 4, c["primary"], ox, oy)
    draw_rect(draw, 12, 14, 20, 24, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 16, 12, c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 16, 11, c["accent"], ox, oy)
    # shield accent
    draw_diamond(draw, 16, 16, 4, c["accent"], ox, oy)

def icon_armor(draw, c, ox, oy):
    """Armor -- chestplate."""
    draw_rect(draw, 10, 8, 22, 22, c["primary"], ox, oy)
    draw_rect(draw, 7, 8, 10, 14, c["accent"], ox, oy)
    draw_rect(draw, 22, 8, 25, 14, c["accent"], ox, oy)
    draw_rect(draw, 13, 10, 19, 20, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 10, 16, 20, c["highlight"], ox, oy)

# --- Row 1: Unarmed ---

def icon_unarmed_mastery(draw, c, ox, oy):
    """Unarmed mastery -- clenched fist."""
    draw_rect(draw, 11, 8, 20, 12, c["primary"], ox, oy)
    draw_rect(draw, 10, 12, 21, 22, c["primary"], ox, oy)
    draw_rect(draw, 13, 22, 18, 25, c["secondary"], ox, oy)
    draw_line_px(draw, 14, 9, 14, 12, c["secondary"], ox, oy)
    draw_line_px(draw, 17, 9, 17, 12, c["secondary"], ox, oy)
    draw_line_px(draw, 20, 10, 20, 12, c["secondary"], ox, oy)
    draw_rect(draw, 8, 13, 10, 18, c["accent"], ox, oy)

def icon_punch(draw, c, ox, oy):
    """Punch -- forward fist with impact."""
    draw_rect(draw, 6, 12, 18, 20, c["primary"], ox, oy)
    draw_rect(draw, 6, 13, 8, 19, c["accent"], ox, oy)
    # impact lines
    draw_line_px(draw, 20, 14, 26, 12, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 16, 27, 16, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 18, 26, 20, c["highlight"], ox, oy)

def icon_kick(draw, c, ox, oy):
    """Kick -- leg kick."""
    draw_line_px(draw, 12, 8, 16, 16, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 16, 24, 14, c["primary"], ox, oy)
    draw_line_px(draw, 13, 8, 17, 16, c["secondary"], ox, oy)
    draw_line_px(draw, 17, 16, 25, 14, c["primary"], ox, oy)
    draw_rect(draw, 23, 12, 27, 16, c["accent"], ox, oy)
    draw_pixel(draw, 28, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 28, 15, c["highlight"], ox, oy)

def icon_grappling(draw, c, ox, oy):
    """Grappling -- grasping hands."""
    draw_rect(draw, 6, 10, 12, 20, c["primary"], ox, oy)
    draw_rect(draw, 20, 10, 26, 20, c["primary"], ox, oy)
    draw_rect(draw, 12, 12, 14, 14, c["accent"], ox, oy)
    draw_rect(draw, 12, 16, 14, 18, c["accent"], ox, oy)
    draw_rect(draw, 18, 12, 20, 14, c["accent"], ox, oy)
    draw_rect(draw, 18, 16, 20, 18, c["accent"], ox, oy)

def icon_elbow_strike(draw, c, ox, oy):
    """Elbow Strike -- bent arm with impact."""
    draw_line_px(draw, 8, 8, 16, 16, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 16, 12, 24, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 16, 3, c["accent"], ox, oy)
    draw_pixel(draw, 19, 14, c["highlight"], ox, oy)
    draw_pixel(draw, 20, 16, c["highlight"], ox, oy)
    draw_pixel(draw, 19, 18, c["highlight"], ox, oy)

# --- Row 2: Bladed ---

def icon_bladed_mastery(draw, c, ox, oy):
    """Bladed mastery -- sword."""
    draw_line_px(draw, 16, 5, 16, 20, c["highlight"], ox, oy)
    draw_line_px(draw, 15, 6, 15, 19, c["accent"], ox, oy)
    draw_line_px(draw, 17, 6, 17, 19, c["accent"], ox, oy)
    draw_pixel(draw, 16, 4, c["highlight"], ox, oy)
    draw_rect(draw, 12, 20, 20, 21, c["primary"], ox, oy)
    draw_rect(draw, 15, 22, 17, 26, c["secondary"], ox, oy)
    draw_rect(draw, 14, 27, 18, 28, c["primary"], ox, oy)

def icon_slash(draw, c, ox, oy):
    """Slash -- arc slash attack."""
    for angle in range(0, 180, 8):
        rad = math.radians(angle)
        x = int(16 + 10 * math.cos(rad))
        y = int(16 - 8 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
        draw_pixel(draw, x, y+1, c["primary"], ox, oy)

def icon_thrust(draw, c, ox, oy):
    """Thrust -- forward stab."""
    draw_line_px(draw, 16, 4, 16, 24, c["accent"], ox, oy)
    draw_line_px(draw, 15, 5, 15, 23, c["primary"], ox, oy)
    draw_line_px(draw, 12, 24, 20, 24, c["highlight"], ox, oy)
    draw_line_px(draw, 13, 26, 19, 26, c["highlight"], ox, oy)

def icon_cleave(draw, c, ox, oy):
    """Cleave -- overhead chop."""
    draw_rect(draw, 14, 4, 18, 16, c["accent"], ox, oy)
    draw_line_px(draw, 10, 18, 22, 18, c["highlight"], ox, oy)
    draw_line_px(draw, 8, 20, 24, 20, c["highlight"], ox, oy)
    draw_line_px(draw, 12, 22, 20, 22, c["primary"], ox, oy)

def icon_parry(draw, c, ox, oy):
    """Parry -- crossed blades."""
    draw_line_px(draw, 8, 6, 24, 22, c["accent"], ox, oy)
    draw_line_px(draw, 24, 6, 8, 22, c["accent"], ox, oy)
    draw_line_px(draw, 9, 6, 25, 22, c["primary"], ox, oy)
    draw_line_px(draw, 25, 6, 9, 22, c["primary"], ox, oy)
    draw_pixel(draw, 16, 14, c["highlight"], ox, oy)
    draw_pixel(draw, 15, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 15, c["highlight"], ox, oy)

# --- Row 3: Blunt ---

def icon_blunt_mastery(draw, c, ox, oy):
    """Blunt mastery -- hammer."""
    draw_rect(draw, 15, 14, 17, 27, c["secondary"], ox, oy)
    draw_rect(draw, 10, 6, 22, 14, c["primary"], ox, oy)
    draw_rect(draw, 11, 7, 21, 13, c["accent"], ox, oy)
    draw_rect(draw, 12, 8, 14, 10, c["highlight"], ox, oy)

def icon_smash(draw, c, ox, oy):
    """Smash -- hammer impact."""
    draw_rect(draw, 13, 4, 19, 12, c["primary"], ox, oy)
    draw_rect(draw, 15, 12, 17, 18, c["secondary"], ox, oy)
    # impact lines
    draw_line_px(draw, 8, 20, 24, 20, c["highlight"], ox, oy)
    draw_line_px(draw, 10, 22, 22, 22, c["accent"], ox, oy)
    draw_line_px(draw, 12, 24, 20, 24, c["primary"], ox, oy)

def icon_bump(draw, c, ox, oy):
    """Bump -- shoulder charge."""
    draw_circle_filled(draw, 12, 12, 5, c["primary"], ox, oy)
    draw_rect(draw, 8, 14, 18, 24, c["primary"], ox, oy)
    # motion lines
    draw_line_px(draw, 20, 10, 26, 8, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 14, 27, 14, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 18, 26, 20, c["highlight"], ox, oy)

def icon_crush(draw, c, ox, oy):
    """Crush -- heavy downward strike."""
    draw_rect(draw, 10, 4, 22, 10, c["primary"], ox, oy)
    draw_rect(draw, 14, 10, 18, 16, c["secondary"], ox, oy)
    # ground cracks
    draw_line_px(draw, 6, 20, 26, 20, c["accent"], ox, oy)
    draw_line_px(draw, 16, 20, 12, 28, c["highlight"], ox, oy)
    draw_line_px(draw, 16, 20, 20, 28, c["highlight"], ox, oy)

def icon_shatter(draw, c, ox, oy):
    """Shatter -- breaking fragments."""
    # fragments flying outward
    draw_rect(draw, 14, 12, 18, 18, c["primary"], ox, oy)
    draw_rect(draw, 6, 6, 10, 10, c["accent"], ox, oy)
    draw_rect(draw, 22, 6, 26, 10, c["accent"], ox, oy)
    draw_rect(draw, 6, 20, 10, 24, c["accent"], ox, oy)
    draw_rect(draw, 22, 20, 26, 24, c["accent"], ox, oy)
    # cracks from center
    draw_line_px(draw, 16, 15, 8, 8, c["highlight"], ox, oy)
    draw_line_px(draw, 16, 15, 24, 8, c["highlight"], ox, oy)
    draw_line_px(draw, 16, 15, 8, 22, c["highlight"], ox, oy)
    draw_line_px(draw, 16, 15, 24, 22, c["highlight"], ox, oy)

# --- Row 4: Polearms ---

def icon_polearms_mastery(draw, c, ox, oy):
    """Polearms mastery -- spear."""
    draw_line_px(draw, 16, 10, 16, 28, c["secondary"], ox, oy)
    draw_triangle_up(draw, 16, 7, 4, c["accent"], ox, oy)
    draw_pixel(draw, 16, 4, c["highlight"], ox, oy)

def icon_pierce(draw, c, ox, oy):
    """Pierce -- spear thrust."""
    draw_line_px(draw, 16, 6, 16, 26, c["accent"], ox, oy)
    draw_pixel(draw, 16, 4, c["highlight"], ox, oy)
    draw_pixel(draw, 15, 5, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 5, c["highlight"], ox, oy)
    # impact
    draw_line_px(draw, 12, 26, 20, 26, c["highlight"], ox, oy)

def icon_sweep(draw, c, ox, oy):
    """Sweep -- wide horizontal arc."""
    draw_line_px(draw, 4, 16, 28, 16, c["accent"], ox, oy)
    draw_line_px(draw, 4, 15, 28, 15, c["primary"], ox, oy)
    # sweep arc below
    for angle in range(0, 180, 10):
        rad = math.radians(angle)
        x = int(16 + 12 * math.cos(rad))
        y = int(18 + 4 * math.sin(rad))
        draw_pixel(draw, x, y, c["highlight"], ox, oy)

def icon_brace(draw, c, ox, oy):
    """Brace -- spear set against charge."""
    draw_line_px(draw, 8, 24, 24, 8, c["accent"], ox, oy)
    draw_line_px(draw, 9, 24, 25, 8, c["accent"], ox, oy)
    # point
    draw_pixel(draw, 24, 7, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 7, c["highlight"], ox, oy)
    # brace foot
    draw_rect(draw, 6, 24, 12, 27, c["secondary"], ox, oy)

def icon_vault(draw, c, ox, oy):
    """Vault -- pole vault leap."""
    # pole diagonal
    draw_line_px(draw, 12, 26, 16, 6, c["secondary"], ox, oy)
    # figure arcing over
    draw_circle_filled(draw, 20, 8, 3, c["primary"], ox, oy)
    # arc path
    for angle in range(0, 180, 15):
        rad = math.radians(angle)
        x = int(16 + 8 * math.cos(rad))
        y = int(12 - 4 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)

def icon_haft_blow(draw, c, ox, oy):
    """Haft Blow -- blunt end strike."""
    draw_line_px(draw, 8, 8, 24, 24, c["secondary"], ox, oy)
    draw_line_px(draw, 9, 8, 25, 24, c["secondary"], ox, oy)
    # blunt end highlighted
    draw_circle_filled(draw, 24, 24, 3, c["accent"], ox, oy)
    # impact
    draw_pixel(draw, 27, 22, c["highlight"], ox, oy)
    draw_pixel(draw, 27, 26, c["highlight"], ox, oy)

# --- Row 5: Shields ---

def icon_shields_mastery(draw, c, ox, oy):
    """Shields mastery -- shield."""
    draw_rect(draw, 10, 7, 22, 22, c["primary"], ox, oy)
    draw_rect(draw, 11, 6, 21, 23, c["primary"], ox, oy)
    draw_rect(draw, 12, 23, 20, 25, c["primary"], ox, oy)
    draw_line_px(draw, 16, 9, 16, 21, c["accent"], ox, oy)
    draw_line_px(draw, 12, 14, 20, 14, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 14, 2, c["highlight"], ox, oy)

def icon_block(draw, c, ox, oy):
    """Block -- shield raised."""
    draw_rect(draw, 10, 7, 22, 22, c["primary"], ox, oy)
    draw_rect(draw, 11, 6, 21, 23, c["primary"], ox, oy)
    draw_line_px(draw, 8, 5, 8, 24, c["highlight"], ox, oy)
    draw_line_px(draw, 24, 5, 24, 24, c["highlight"], ox, oy)

def icon_shield_bash(draw, c, ox, oy):
    """Shield Bash -- shield strike."""
    draw_rect(draw, 8, 8, 18, 22, c["primary"], ox, oy)
    draw_line_px(draw, 20, 10, 26, 8, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 15, 27, 15, c["highlight"], ox, oy)
    draw_line_px(draw, 20, 20, 26, 22, c["highlight"], ox, oy)

def icon_deflect(draw, c, ox, oy):
    """Deflect -- arrow bouncing off."""
    draw_rect(draw, 6, 8, 14, 22, c["primary"], ox, oy)
    draw_line_px(draw, 26, 8, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 16, 14, 24, 22, c["highlight"], ox, oy)

def icon_bulwark(draw, c, ox, oy):
    """Bulwark -- fortified stance."""
    draw_rect(draw, 8, 10, 24, 24, c["primary"], ox, oy)
    draw_rect(draw, 10, 12, 22, 22, c["secondary"], ox, oy)
    draw_rect(draw, 8, 6, 11, 10, c["primary"], ox, oy)
    draw_rect(draw, 14, 6, 18, 10, c["primary"], ox, oy)
    draw_rect(draw, 21, 6, 24, 10, c["primary"], ox, oy)

# --- Row 6: Dual Wield ---

def icon_dual_wield_mastery(draw, c, ox, oy):
    """Dual Wield mastery -- two crossed swords."""
    # left sword
    draw_line_px(draw, 8, 6, 16, 22, c["accent"], ox, oy)
    draw_line_px(draw, 9, 6, 17, 22, c["accent"], ox, oy)
    # right sword
    draw_line_px(draw, 24, 6, 16, 22, c["accent"], ox, oy)
    draw_line_px(draw, 23, 6, 15, 22, c["accent"], ox, oy)
    # guards
    draw_line_px(draw, 10, 18, 14, 16, c["primary"], ox, oy)
    draw_line_px(draw, 22, 18, 18, 16, c["primary"], ox, oy)

def icon_dual_stab(draw, c, ox, oy):
    """Dual Stab -- two blades thrusting."""
    draw_line_px(draw, 10, 4, 10, 24, c["accent"], ox, oy)
    draw_line_px(draw, 11, 4, 11, 24, c["accent"], ox, oy)
    draw_line_px(draw, 22, 4, 22, 24, c["accent"], ox, oy)
    draw_line_px(draw, 23, 4, 23, 24, c["accent"], ox, oy)
    # impact
    draw_line_px(draw, 7, 24, 14, 24, c["highlight"], ox, oy)
    draw_line_px(draw, 19, 24, 26, 24, c["highlight"], ox, oy)

def icon_dual_slash(draw, c, ox, oy):
    """Dual Slash -- two arcs."""
    for angle in range(0, 180, 8):
        rad = math.radians(angle)
        x = int(12 + 7 * math.cos(rad))
        y = int(16 - 6 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
    for angle in range(0, 180, 8):
        rad = math.radians(angle)
        x = int(20 + 7 * math.cos(rad))
        y = int(16 - 6 * math.sin(rad))
        draw_pixel(draw, x, y, c["highlight"], ox, oy)

def icon_spin_attack(draw, c, ox, oy):
    """Spin Attack -- circular blade motion."""
    draw_circle_outline(draw, 16, 15, 10, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)
    # blade tips
    draw_pixel(draw, 16, 5, c["highlight"], ox, oy)
    draw_pixel(draw, 16, 25, c["highlight"], ox, oy)
    draw_pixel(draw, 6, 15, c["highlight"], ox, oy)
    draw_pixel(draw, 26, 15, c["highlight"], ox, oy)

def icon_rapid_combo(draw, c, ox, oy):
    """Rapid Combo -- multiple slash lines."""
    for i in range(5):
        x = 6 + i * 5
        draw_line_px(draw, x, 6 + i, x + 4, 24 - i, c["accent"], ox, oy)
        draw_pixel(draw, x + 2, 6 + i - 1, c["highlight"], ox, oy)

# --- Row 7: Discipline ---

def icon_discipline_mastery(draw, c, ox, oy):
    """Discipline mastery -- meditation brain."""
    draw_circle_filled(draw, 16, 14, 8, c["primary"], ox, oy)
    draw_line_px(draw, 16, 7, 16, 21, c["secondary"], ox, oy)
    draw_line_px(draw, 11, 11, 14, 14, c["accent"], ox, oy)
    draw_line_px(draw, 18, 11, 21, 14, c["accent"], ox, oy)
    draw_line_px(draw, 11, 17, 14, 14, c["accent"], ox, oy)
    draw_line_px(draw, 18, 17, 21, 14, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 14, 10, c["highlight"], ox, oy)

def icon_focus(draw, c, ox, oy):
    """Focus -- focused eye."""
    pts = [(6, 15), (16, 8), (26, 15), (16, 22)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw_circle_filled(draw, 16, 15, 4, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["secondary"], ox, oy)
    draw_pixel(draw, 15, 14, c["highlight"], ox, oy)
    # focus lines
    draw_pixel(draw, 16, 3, c["highlight"], ox, oy)
    draw_pixel(draw, 16, 27, c["highlight"], ox, oy)

def icon_endure(draw, c, ox, oy):
    """Endure -- armored body."""
    draw_rect(draw, 10, 8, 22, 22, c["primary"], ox, oy)
    draw_rect(draw, 7, 8, 10, 14, c["accent"], ox, oy)
    draw_rect(draw, 22, 8, 25, 14, c["accent"], ox, oy)
    draw_rect(draw, 13, 10, 19, 20, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 10, 16, 20, c["accent"], ox, oy)

def icon_deep_breaths(draw, c, ox, oy):
    """Deep Breaths -- healing breath spiral."""
    for angle in range(0, 360, 15):
        rad = math.radians(angle)
        r = 6 + angle / 120
        x = int(16 + r * math.cos(rad))
        y = int(15 + r * math.sin(rad))
        if 2 <= x <= 29 and 2 <= y <= 29:
            draw_pixel(draw, x, y, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)

def icon_blood_lust(draw, c, ox, oy):
    """Blood Lust -- raging aura."""
    draw_circle_filled(draw, 16, 15, 8, (180, 50, 50), ox, oy)
    draw_circle_filled(draw, 16, 15, 5, (220, 80, 60), ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)
    # rage sparks
    draw_line_px(draw, 16, 3, 16, 7, c["accent"], ox, oy)
    draw_line_px(draw, 4, 15, 8, 15, c["accent"], ox, oy)
    draw_line_px(draw, 24, 15, 28, 15, c["accent"], ox, oy)

# --- Row 8: Intimidation ---

def icon_intimidation_mastery(draw, c, ox, oy):
    """Intimidation mastery -- shouting face with aura."""
    draw_circle_filled(draw, 16, 14, 6, c["primary"], ox, oy)
    draw_rect(draw, 14, 16, 18, 19, c["secondary"], ox, oy)
    draw_line_px(draw, 23, 12, 26, 10, c["accent"], ox, oy)
    draw_line_px(draw, 24, 15, 27, 15, c["accent"], ox, oy)
    draw_line_px(draw, 23, 18, 26, 20, c["accent"], ox, oy)
    draw_line_px(draw, 5, 12, 8, 10, c["accent"], ox, oy)
    draw_line_px(draw, 4, 15, 7, 15, c["accent"], ox, oy)
    draw_line_px(draw, 5, 18, 8, 20, c["accent"], ox, oy)

def icon_shout(draw, c, ox, oy):
    """Shout -- sound waves from mouth."""
    draw_circle_filled(draw, 10, 14, 4, c["primary"], ox, oy)
    draw_rect(draw, 12, 15, 15, 18, c["secondary"], ox, oy)
    # sound waves
    for i in range(3):
        r = 4 + i * 3
        draw_circle_outline(draw, 18, 15, r, c["accent"], ox, oy)

def icon_intimidate(draw, c, ox, oy):
    """Intimidate -- fear aura."""
    draw_circle_filled(draw, 16, 14, 6, c["primary"], ox, oy)
    draw_rect(draw, 13, 11, 15, 13, (40, 30, 50), ox, oy)  # dark eye
    draw_rect(draw, 17, 11, 19, 13, (40, 30, 50), ox, oy)  # dark eye
    draw_circle_outline(draw, 16, 14, 10, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 14, 12, c["highlight"], ox, oy)

def icon_ugly_mug(draw, c, ox, oy):
    """Ugly Mug -- grotesque face."""
    draw_circle_filled(draw, 16, 14, 7, c["primary"], ox, oy)
    # misshapen features
    draw_rect(draw, 12, 10, 14, 14, c["secondary"], ox, oy)
    draw_rect(draw, 19, 11, 21, 13, c["secondary"], ox, oy)
    draw_line_px(draw, 12, 18, 20, 20, c["accent"], ox, oy)
    # scar
    draw_line_px(draw, 10, 8, 14, 16, c["highlight"], ox, oy)

def icon_battle_roar(draw, c, ox, oy):
    """Battle Roar -- mighty roar with shockwave."""
    draw_circle_filled(draw, 16, 14, 5, c["primary"], ox, oy)
    draw_rect(draw, 14, 16, 18, 20, c["secondary"], ox, oy)
    # shockwave rings
    draw_circle_outline(draw, 16, 14, 8, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 14, 11, c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 14, 14, c["accent"], ox, oy)

# --- Row 9: Bowmanship ---

def icon_bowmanship_mastery(draw, c, ox, oy):
    """Bowmanship mastery -- drawn bow."""
    draw_line_px(draw, 10, 6, 8, 16, c["primary"], ox, oy)
    draw_line_px(draw, 8, 16, 10, 26, c["primary"], ox, oy)
    draw_line_px(draw, 10, 6, 10, 26, c["secondary"], ox, oy)
    draw_line_px(draw, 11, 16, 25, 16, c["accent"], ox, oy)
    draw_pixel(draw, 26, 16, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 15, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 17, c["highlight"], ox, oy)

def icon_dead_eye(draw, c, ox, oy):
    """Dead Eye -- precision crosshair."""
    draw_circle_outline(draw, 16, 15, 8, c["primary"], ox, oy)
    draw_line_px(draw, 16, 4, 16, 10, c["accent"], ox, oy)
    draw_line_px(draw, 16, 20, 16, 26, c["accent"], ox, oy)
    draw_line_px(draw, 5, 15, 11, 15, c["accent"], ox, oy)
    draw_line_px(draw, 21, 15, 27, 15, c["accent"], ox, oy)
    draw_pixel(draw, 16, 15, c["highlight"], ox, oy)

def icon_pepper(draw, c, ox, oy):
    """Pepper -- multiple arrows."""
    for i in range(3):
        y = 10 + i * 5
        draw_line_px(draw, 6, y, 22, y, c["accent"], ox, oy)
        draw_pixel(draw, 23 - i*2, y, c["highlight"], ox, oy)
        draw_pixel(draw, 22 - i*2, y-1, c["highlight"], ox, oy)
        draw_pixel(draw, 22 - i*2, y+1, c["highlight"], ox, oy)

def icon_lob(draw, c, ox, oy):
    """Lob -- arc shot trajectory."""
    for angle in range(0, 180, 8):
        rad = math.radians(angle)
        x = int(6 + angle / 180 * 20)
        y = int(20 - 12 * math.sin(rad))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
    draw_pixel(draw, 26, 20, c["highlight"], ox, oy)
    draw_pixel(draw, 25, 19, c["highlight"], ox, oy)

def icon_pin(draw, c, ox, oy):
    """Pin -- arrow pinning to ground."""
    draw_line_px(draw, 16, 4, 16, 22, c["accent"], ox, oy)
    draw_pixel(draw, 16, 3, c["highlight"], ox, oy)
    draw_pixel(draw, 15, 4, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 4, c["highlight"], ox, oy)
    draw_line_px(draw, 6, 24, 26, 24, c["secondary"], ox, oy)
    draw_line_px(draw, 14, 24, 12, 27, c["primary"], ox, oy)
    draw_line_px(draw, 18, 24, 20, 27, c["primary"], ox, oy)

def icon_flame_arrow(draw, c, ox, oy):
    """Flame Arrow -- flaming projectile."""
    # arrow shaft
    draw_line_px(draw, 6, 16, 20, 16, c["accent"], ox, oy)
    draw_pixel(draw, 21, 16, c["highlight"], ox, oy)
    draw_pixel(draw, 20, 15, c["highlight"], ox, oy)
    draw_pixel(draw, 20, 17, c["highlight"], ox, oy)
    # flames on tip
    draw_rect(draw, 22, 12, 26, 20, (220, 80, 40), ox, oy)
    draw_rect(draw, 24, 10, 28, 14, (255, 160, 50), ox, oy)
    draw_pixel(draw, 26, 9, (255, 220, 100), ox, oy)

# --- Row 10: Throwing ---

def icon_throwing_mastery(draw, c, ox, oy):
    """Throwing mastery -- throwing knife."""
    draw_line_px(draw, 8, 8, 20, 20, c["accent"], ox, oy)
    draw_line_px(draw, 9, 8, 21, 20, c["accent"], ox, oy)
    draw_line_px(draw, 21, 21, 25, 25, c["secondary"], ox, oy)
    draw_line_px(draw, 6, 12, 4, 14, c["highlight"], ox, oy)
    draw_line_px(draw, 10, 6, 8, 8, c["highlight"], ox, oy)

def icon_flick(draw, c, ox, oy):
    """Flick -- quick thrown knife."""
    draw_line_px(draw, 10, 14, 24, 14, c["accent"], ox, oy)
    draw_line_px(draw, 10, 15, 24, 15, c["accent"], ox, oy)
    draw_pixel(draw, 25, 14, c["highlight"], ox, oy)
    # speed lines
    draw_line_px(draw, 4, 12, 8, 14, c["secondary"], ox, oy)
    draw_line_px(draw, 4, 16, 8, 15, c["secondary"], ox, oy)

def icon_chuck(draw, c, ox, oy):
    """Chuck -- heavy throw."""
    draw_circle_filled(draw, 20, 14, 5, c["primary"], ox, oy)
    draw_circle_filled(draw, 20, 14, 3, c["accent"], ox, oy)
    # motion arc
    for angle in range(90, 270, 12):
        rad = math.radians(angle)
        x = int(12 + 6 * math.cos(rad))
        y = int(14 + 6 * math.sin(rad))
        draw_pixel(draw, x, y, c["highlight"], ox, oy)

def icon_fan(draw, c, ox, oy):
    """Fan -- spread of projectiles."""
    for i in range(5):
        angle = math.radians(-60 + i * 30)
        x2 = int(10 + 14 * math.cos(angle))
        y2 = int(16 + 14 * math.sin(angle))
        draw_line_px(draw, 10, 16, x2, y2, c["accent"], ox, oy)
        draw_pixel(draw, x2, y2, c["highlight"], ox, oy)

def icon_ricochet(draw, c, ox, oy):
    """Ricochet -- bouncing projectile."""
    draw_line_px(draw, 6, 8, 14, 18, c["accent"], ox, oy)
    draw_line_px(draw, 14, 18, 22, 10, c["accent"], ox, oy)
    draw_line_px(draw, 22, 10, 28, 20, c["accent"], ox, oy)
    draw_pixel(draw, 14, 18, c["highlight"], ox, oy)
    draw_pixel(draw, 22, 10, c["highlight"], ox, oy)

def icon_frost_blade(draw, c, ox, oy):
    """Frost Blade -- icy thrown blade."""
    draw_line_px(draw, 8, 8, 22, 22, (100, 180, 255), ox, oy)
    draw_line_px(draw, 9, 8, 23, 22, (100, 180, 255), ox, oy)
    draw_line_px(draw, 22, 22, 26, 24, c["secondary"], ox, oy)
    # frost particles
    draw_pixel(draw, 6, 10, (180, 220, 255), ox, oy)
    draw_pixel(draw, 10, 6, (180, 220, 255), ox, oy)
    draw_pixel(draw, 12, 12, (220, 240, 255), ox, oy)

# --- Row 11: Firearms ---

def icon_firearms_mastery(draw, c, ox, oy):
    """Firearms mastery -- pistol."""
    draw_rect(draw, 6, 12, 18, 15, c["primary"], ox, oy)
    draw_rect(draw, 14, 10, 22, 16, c["primary"], ox, oy)
    draw_rect(draw, 18, 16, 22, 24, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 16, 16, 20, c["accent"], ox, oy)
    draw_line_px(draw, 16, 20, 18, 20, c["accent"], ox, oy)
    draw_pixel(draw, 5, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 4, 12, c["highlight"], ox, oy)
    draw_pixel(draw, 4, 14, c["highlight"], ox, oy)

def icon_quick_draw(draw, c, ox, oy):
    """Quick Draw -- fast draw speed lines."""
    draw_rect(draw, 14, 12, 24, 16, c["primary"], ox, oy)
    draw_rect(draw, 20, 16, 24, 22, c["secondary"], ox, oy)
    # speed lines
    draw_line_px(draw, 4, 10, 12, 12, c["accent"], ox, oy)
    draw_line_px(draw, 4, 14, 12, 14, c["accent"], ox, oy)
    draw_line_px(draw, 4, 18, 12, 16, c["accent"], ox, oy)
    draw_pixel(draw, 13, 13, c["highlight"], ox, oy)

def icon_bead(draw, c, ox, oy):
    """Bead -- draw a bead, tight crosshair."""
    draw_circle_outline(draw, 16, 15, 6, c["primary"], ox, oy)
    draw_line_px(draw, 16, 6, 16, 11, c["accent"], ox, oy)
    draw_line_px(draw, 16, 19, 16, 24, c["accent"], ox, oy)
    draw_line_px(draw, 7, 15, 12, 15, c["accent"], ox, oy)
    draw_line_px(draw, 20, 15, 25, 15, c["accent"], ox, oy)
    draw_pixel(draw, 16, 15, c["highlight"], ox, oy)

def icon_spray(draw, c, ox, oy):
    """Spray -- scattered rapid shots."""
    # scattered projectiles in fan pattern
    draw_circle_filled(draw, 20, 10, 2, c["accent"], ox, oy)
    draw_circle_filled(draw, 24, 14, 2, c["accent"], ox, oy)
    draw_circle_filled(draw, 22, 18, 2, c["accent"], ox, oy)
    draw_circle_filled(draw, 26, 12, 1, c["highlight"], ox, oy)
    draw_circle_filled(draw, 19, 20, 1, c["highlight"], ox, oy)
    # muzzle
    draw_rect(draw, 4, 12, 10, 18, c["primary"], ox, oy)
    draw_pixel(draw, 3, 14, c["highlight"], ox, oy)
    draw_pixel(draw, 3, 16, c["highlight"], ox, oy)

def icon_snipe(draw, c, ox, oy):
    """Snipe -- scope crosshair with zoom."""
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 6, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 3, 16, 27, c["accent"], ox, oy)
    draw_line_px(draw, 4, 15, 28, 15, c["accent"], ox, oy)
    draw_pixel(draw, 16, 15, c["highlight"], ox, oy)

def icon_shock_round(draw, c, ox, oy):
    """Shock Round -- electrified bullet."""
    draw_circle_filled(draw, 16, 15, 4, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, (220, 200, 60), ox, oy)
    # lightning sparks
    draw_line_px(draw, 12, 10, 8, 6, (255, 240, 100), ox, oy)
    draw_line_px(draw, 20, 10, 24, 6, (255, 240, 100), ox, oy)
    draw_line_px(draw, 12, 20, 8, 24, (255, 240, 100), ox, oy)
    draw_line_px(draw, 20, 20, 24, 24, (255, 240, 100), ox, oy)

# --- Row 12: CQC ---

def icon_cqc_mastery(draw, c, ox, oy):
    """CQC mastery -- small buckler/dagger combo."""
    draw_circle_filled(draw, 12, 15, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 12, 15, 3, c["secondary"], ox, oy)
    # dagger
    draw_line_px(draw, 20, 6, 20, 22, c["accent"], ox, oy)
    draw_rect(draw, 18, 16, 22, 17, c["primary"], ox, oy)
    draw_pixel(draw, 20, 5, c["highlight"], ox, oy)

def icon_cqc_parry(draw, c, ox, oy):
    """CQC Parry -- blade deflection."""
    draw_line_px(draw, 8, 6, 24, 22, c["accent"], ox, oy)
    draw_line_px(draw, 24, 6, 8, 22, c["accent"], ox, oy)
    draw_pixel(draw, 16, 14, c["highlight"], ox, oy)
    draw_pixel(draw, 15, 13, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 15, c["highlight"], ox, oy)

def icon_hunker(draw, c, ox, oy):
    """Hunker -- crouched behind cover."""
    # low cover/barricade
    draw_rect(draw, 6, 18, 26, 24, c["secondary"], ox, oy)
    draw_line_px(draw, 6, 18, 26, 18, c["primary"], ox, oy)
    # crouched figure behind cover
    draw_circle_filled(draw, 16, 12, 3, c["accent"], ox, oy)
    draw_rect(draw, 13, 15, 19, 18, c["primary"], ox, oy)

def icon_riposte(draw, c, ox, oy):
    """Riposte -- counter-attack blade."""
    # parry line
    draw_line_px(draw, 6, 10, 16, 16, c["secondary"], ox, oy)
    # counter thrust
    draw_line_px(draw, 16, 16, 28, 10, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 28, 11, c["accent"], ox, oy)
    draw_pixel(draw, 28, 10, c["highlight"], ox, oy)
    # spark at deflection point
    draw_circle_filled(draw, 16, 16, 2, c["highlight"], ox, oy)

def icon_shiv(draw, c, ox, oy):
    """Shiv -- quick dagger strike."""
    draw_line_px(draw, 10, 6, 22, 24, c["accent"], ox, oy)
    draw_line_px(draw, 11, 6, 23, 24, c["accent"], ox, oy)
    # handle
    draw_rect(draw, 22, 24, 26, 28, c["secondary"], ox, oy)
    # impact sparks
    draw_pixel(draw, 8, 6, c["highlight"], ox, oy)
    draw_pixel(draw, 10, 4, c["highlight"], ox, oy)
    draw_pixel(draw, 12, 5, c["highlight"], ox, oy)

# --- Row 13: Awareness ---

def icon_awareness_mastery(draw, c, ox, oy):
    """Awareness mastery -- all-seeing eye."""
    pts = [(6, 15), (16, 8), (26, 15), (16, 22)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw_circle_filled(draw, 16, 15, 4, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["secondary"], ox, oy)
    draw_pixel(draw, 15, 14, c["highlight"], ox, oy)

def icon_keen_senses(draw, c, ox, oy):
    """Keen Senses -- enhanced eye with glow."""
    pts = [(6, 15), (16, 8), (26, 15), (16, 22)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw_circle_filled(draw, 16, 15, 4, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)
    # glow lines
    draw_pixel(draw, 16, 3, c["highlight"], ox, oy)
    draw_pixel(draw, 16, 27, c["highlight"], ox, oy)
    draw_pixel(draw, 3, 15, c["highlight"], ox, oy)
    draw_pixel(draw, 29, 15, c["highlight"], ox, oy)

def icon_tip_toes(draw, c, ox, oy):
    """Tip Toes -- stealth footsteps."""
    # foot outlines, subtle
    draw_circle_filled(draw, 12, 14, 3, c["secondary"], ox, oy)
    draw_circle_filled(draw, 20, 18, 3, c["secondary"], ox, oy)
    # dotted trail
    draw_pixel(draw, 8, 10, c["accent"], ox, oy)
    draw_pixel(draw, 24, 22, c["accent"], ox, oy)
    draw_pixel(draw, 6, 8, c["primary"], ox, oy)

def icon_disengage(draw, c, ox, oy):
    """Disengage -- leaping back."""
    draw_circle_filled(draw, 12, 12, 3, c["primary"], ox, oy)
    draw_line_px(draw, 12, 15, 12, 22, c["primary"], ox, oy)
    draw_line_px(draw, 16, 15, 26, 15, c["accent"], ox, oy)
    draw_pixel(draw, 25, 13, c["accent"], ox, oy)
    draw_pixel(draw, 25, 17, c["accent"], ox, oy)
    draw_pixel(draw, 27, 15, c["highlight"], ox, oy)

def icon_steady_breathing(draw, c, ox, oy):
    """Steady Breathing -- calm breath waves."""
    for i in range(3):
        y = 10 + i * 6
        for x in range(6, 26):
            yy = int(y + 2 * math.sin((x - 6) * math.pi / 5))
            draw_pixel(draw, x, yy, c["accent"], ox, oy)

def icon_rangefinding(draw, c, ox, oy):
    """Rangefinding -- distance lines."""
    draw_line_px(draw, 4, 16, 28, 16, c["secondary"], ox, oy)
    # markers at intervals
    for x in [8, 14, 20, 26]:
        draw_line_px(draw, x, 13, x, 19, c["accent"], ox, oy)
    # distance numbers suggestion
    draw_pixel(draw, 8, 11, c["highlight"], ox, oy)
    draw_pixel(draw, 14, 11, c["highlight"], ox, oy)
    draw_pixel(draw, 20, 11, c["highlight"], ox, oy)

def icon_tracking(draw, c, ox, oy):
    """Tracking -- footprint trail."""
    # footprints in a path
    draw_rect(draw, 6, 8, 10, 12, c["primary"], ox, oy)
    draw_rect(draw, 12, 14, 16, 18, c["primary"], ox, oy)
    draw_rect(draw, 18, 20, 22, 24, c["primary"], ox, oy)
    # dots connecting
    draw_pixel(draw, 11, 13, c["accent"], ox, oy)
    draw_pixel(draw, 17, 19, c["accent"], ox, oy)
    draw_pixel(draw, 23, 25, c["accent"], ox, oy)

def icon_steady_aim(draw, c, ox, oy):
    """Steady Aim -- crosshair locked on."""
    draw_circle_outline(draw, 16, 15, 10, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 5, c["accent"], ox, oy)
    draw_line_px(draw, 16, 2, 16, 28, c["secondary"], ox, oy)
    draw_line_px(draw, 2, 15, 28, 15, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)

def icon_weak_spot(draw, c, ox, oy):
    """Weak Spot -- target with crack."""
    draw_circle_outline(draw, 16, 15, 8, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 4, c["accent"], ox, oy)
    draw_pixel(draw, 16, 15, c["highlight"], ox, oy)
    # crack lines
    draw_line_px(draw, 16, 15, 22, 10, c["highlight"], ox, oy)
    draw_line_px(draw, 16, 15, 10, 22, c["highlight"], ox, oy)

# --- Row 14: Trapping ---

def icon_trapping_mastery(draw, c, ox, oy):
    """Trapping mastery -- bear trap."""
    for i in range(5):
        x = 8 + i * 4
        draw_triangle_up(draw, x, 12, 2, c["accent"], ox, oy)
    for i in range(5):
        x = 8 + i * 4
        draw_triangle_down(draw, x, 18, 2, c["accent"], ox, oy)
    draw_rect(draw, 8, 20, 24, 23, c["primary"], ox, oy)
    draw_line_px(draw, 16, 23, 16, 27, c["secondary"], ox, oy)

def icon_snare(draw, c, ox, oy):
    """Snare -- rope trap."""
    draw_circle_outline(draw, 16, 18, 7, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 18, 5, c["primary"], ox, oy)
    draw_line_px(draw, 16, 11, 16, 5, c["secondary"], ox, oy)

def icon_tripwire(draw, c, ox, oy):
    """Tripwire -- wire across ground."""
    draw_rect(draw, 6, 10, 9, 24, c["secondary"], ox, oy)
    draw_rect(draw, 23, 10, 26, 24, c["secondary"], ox, oy)
    draw_line_px(draw, 9, 18, 23, 18, c["accent"], ox, oy)
    draw_line_px(draw, 9, 17, 23, 17, c["highlight"], ox, oy)

def icon_decoy(draw, c, ox, oy):
    """Decoy -- dummy figure."""
    draw_line_px(draw, 16, 8, 16, 26, c["secondary"], ox, oy)
    draw_line_px(draw, 10, 14, 22, 14, c["secondary"], ox, oy)
    draw_circle_outline(draw, 16, 8, 3, c["accent"], ox, oy)
    draw_pixel(draw, 16, 7, c["highlight"], ox, oy)

def icon_bait(draw, c, ox, oy):
    """Bait -- lure on hook."""
    draw_line_px(draw, 16, 4, 16, 14, c["secondary"], ox, oy)
    # hook
    draw_line_px(draw, 16, 14, 20, 18, c["accent"], ox, oy)
    draw_line_px(draw, 20, 18, 16, 22, c["accent"], ox, oy)
    draw_pixel(draw, 16, 23, c["highlight"], ox, oy)
    # bait glow
    draw_circle_filled(draw, 16, 22, 3, c["primary"], ox, oy)

def icon_ambush(draw, c, ox, oy):
    """Ambush -- hidden figure striking."""
    draw_rect(draw, 6, 8, 14, 24, c["secondary"], ox, oy)
    draw_line_px(draw, 14, 12, 26, 12, c["accent"], ox, oy)
    draw_line_px(draw, 14, 13, 26, 13, c["primary"], ox, oy)
    draw_rect(draw, 22, 6, 24, 10, c["highlight"], ox, oy)
    draw_pixel(draw, 23, 12, c["highlight"], ox, oy)

# --- Row 15: Sapping ---

def icon_sapping_mastery(draw, c, ox, oy):
    """Sapping mastery -- bomb with fuse."""
    draw_circle_filled(draw, 16, 18, 8, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 18, 6, c["secondary"], ox, oy)
    # fuse
    draw_line_px(draw, 16, 10, 20, 6, c["accent"], ox, oy)
    draw_pixel(draw, 21, 5, c["highlight"], ox, oy)
    draw_pixel(draw, 22, 4, (255, 160, 50), ox, oy)

def icon_frag(draw, c, ox, oy):
    """Frag -- exploding fragments."""
    draw_circle_filled(draw, 16, 15, 5, c["primary"], ox, oy)
    # fragments flying out
    for i in range(8):
        angle = math.radians(i * 45)
        x2 = int(16 + 10 * math.cos(angle))
        y2 = int(15 + 10 * math.sin(angle))
        draw_line_px(draw, 16, 15, x2, y2, c["accent"], ox, oy)
        draw_pixel(draw, x2, y2, c["highlight"], ox, oy)

def icon_smoke_bomb(draw, c, ox, oy):
    """Smoke Bomb -- cloud of smoke."""
    for i in range(6):
        x = 6 + i * 4
        y = 12 + (i % 3) * 4
        draw_circle_filled(draw, x, y, 3 + (i % 2), c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 4, c["primary"], ox, oy)

def icon_flashbang(draw, c, ox, oy):
    """Flashbang -- bright burst."""
    draw_circle_filled(draw, 16, 15, 5, c["highlight"], ox, oy)
    for i in range(8):
        angle = math.radians(i * 45)
        x2 = int(16 + 12 * math.cos(angle))
        y2 = int(15 + 12 * math.sin(angle))
        draw_line_px(draw, 16, 15, x2, y2, c["accent"], ox, oy)

def icon_caltrops(draw, c, ox, oy):
    """Caltrops -- scattered spiky objects."""
    positions = [(8, 18), (16, 14), (24, 20), (12, 24), (20, 10)]
    for px, py in positions:
        draw_star(draw, px, py, 3, 1, 4, c["accent"], ox, oy)

def icon_sticky_bomb(draw, c, ox, oy):
    """Sticky Bomb -- bomb stuck to surface."""
    draw_circle_filled(draw, 16, 16, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 16, 4, c["secondary"], ox, oy)
    # sticky drips
    draw_line_px(draw, 12, 22, 12, 26, c["accent"], ox, oy)
    draw_line_px(draw, 16, 22, 16, 28, c["accent"], ox, oy)
    draw_line_px(draw, 20, 22, 20, 25, c["accent"], ox, oy)
    # fuse spark
    draw_pixel(draw, 16, 10, c["highlight"], ox, oy)
    draw_pixel(draw, 17, 9, (255, 160, 50), ox, oy)

# --- Row 16: Fire ---

def icon_fire_mastery(draw, c, ox, oy):
    """Fire mastery -- flame."""
    draw_rect(draw, 13, 14, 19, 24, c["primary"], ox, oy)
    draw_rect(draw, 14, 10, 18, 14, c["accent"], ox, oy)
    draw_rect(draw, 15, 6, 17, 10, c["highlight"], ox, oy)
    draw_pixel(draw, 16, 5, c["highlight"], ox, oy)
    draw_pixel(draw, 11, 16, c["accent"], ox, oy)
    draw_pixel(draw, 21, 14, c["accent"], ox, oy)

def icon_fireball(draw, c, ox, oy):
    """Fireball -- flaming sphere."""
    draw_circle_filled(draw, 16, 15, 7, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 5, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 15, 9, 15, c["primary"], ox, oy)
    draw_line_px(draw, 5, 13, 8, 13, c["accent"], ox, oy)
    draw_line_px(draw, 5, 17, 8, 17, c["accent"], ox, oy)

def icon_flame_wall(draw, c, ox, oy):
    """Flame Wall -- wall of fire."""
    for x in range(6, 26, 3):
        h = 8 + (x % 7)
        draw_rect(draw, x, 24 - h, x + 2, 24, c["primary"], ox, oy)
        draw_rect(draw, x, 24 - h, x + 1, 24 - h + 3, c["highlight"], ox, oy)

def icon_ignite(draw, c, ox, oy):
    """Ignite -- burning target."""
    draw_circle_outline(draw, 16, 15, 8, c["secondary"], ox, oy)
    draw_rect(draw, 14, 10, 18, 20, c["primary"], ox, oy)
    draw_rect(draw, 15, 7, 17, 10, c["accent"], ox, oy)
    draw_pixel(draw, 16, 6, c["highlight"], ox, oy)

def icon_inferno(draw, c, ox, oy):
    """Inferno -- raging flames."""
    for x in range(4, 28, 2):
        h = 6 + (x * 3 % 11)
        draw_rect(draw, x, 28 - h, x + 1, 28, c["primary"], ox, oy)
        if h > 10:
            draw_pixel(draw, x, 28 - h, c["highlight"], ox, oy)
    draw_rect(draw, 10, 8, 22, 14, c["accent"], ox, oy)

# --- Row 17: Water ---

def icon_water_mastery(draw, c, ox, oy):
    """Water mastery -- water drop."""
    draw_triangle_up(draw, 16, 8, 3, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 17, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 16, 4, c["accent"], ox, oy)
    draw_pixel(draw, 14, 14, c["highlight"], ox, oy)

def icon_frost_bolt(draw, c, ox, oy):
    """Frost Bolt -- ice shard."""
    draw_diamond(draw, 16, 14, 8, c["primary"], ox, oy)
    draw_diamond(draw, 16, 14, 5, c["accent"], ox, oy)
    draw_diamond(draw, 16, 14, 2, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 22, 10, 18, c["secondary"], ox, oy)

def icon_freeze(draw, c, ox, oy):
    """Freeze -- ice crystal."""
    draw_line_px(draw, 16, 4, 16, 26, c["accent"], ox, oy)
    draw_line_px(draw, 6, 15, 26, 15, c["accent"], ox, oy)
    draw_line_px(draw, 8, 7, 24, 23, c["primary"], ox, oy)
    draw_line_px(draw, 24, 7, 8, 23, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 2, c["highlight"], ox, oy)

def icon_tidal_wave(draw, c, ox, oy):
    """Tidal Wave -- crashing wave."""
    for x in range(4, 28):
        y = int(14 + 4 * math.sin((x - 4) * math.pi / 8))
        draw_rect(draw, x, y, x, 26, c["primary"], ox, oy)
        draw_pixel(draw, x, y, c["accent"], ox, oy)
        draw_pixel(draw, x, y - 1, c["highlight"], ox, oy)

def icon_mist_veil(draw, c, ox, oy):
    """Mist Veil -- swirling mist."""
    for i in range(6):
        x = 6 + i * 4
        y = 12 + (i % 3) * 4
        draw_circle_filled(draw, x, y, 3, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 4, c["primary"], ox, oy)

# --- Row 18: Air ---

def icon_air_mastery(draw, c, ox, oy):
    """Air mastery -- wind swirl."""
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
    """Gust -- wind blast."""
    for i in range(4):
        y = 8 + i * 5
        draw_line_px(draw, 6 + i, y, 24 - i, y, c["accent"], ox, oy)
        draw_pixel(draw, 25 - i, y, c["highlight"], ox, oy)

def icon_chain_shock(draw, c, ox, oy):
    """Chain Shock -- branching lightning."""
    draw_line_px(draw, 8, 6, 14, 15, c["accent"], ox, oy)
    draw_line_px(draw, 14, 15, 24, 10, c["highlight"], ox, oy)
    draw_line_px(draw, 14, 15, 22, 24, c["highlight"], ox, oy)
    draw_pixel(draw, 24, 10, c["accent"], ox, oy)
    draw_pixel(draw, 22, 24, c["accent"], ox, oy)

def icon_tempest(draw, c, ox, oy):
    """Tempest -- storm cloud."""
    draw_circle_filled(draw, 12, 10, 5, c["secondary"], ox, oy)
    draw_circle_filled(draw, 20, 10, 5, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 8, 5, c["primary"], ox, oy)
    draw_line_px(draw, 12, 16, 10, 24, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 16, 24, c["accent"], ox, oy)
    draw_line_px(draw, 20, 16, 22, 24, c["accent"], ox, oy)

# --- Row 19: Earth ---

def icon_earth_mastery(draw, c, ox, oy):
    """Earth mastery -- rock."""
    pts = [(10, 22), (6, 14), (12, 8), (20, 6), (26, 12), (24, 22)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw.polygon([(ox+x, oy+y) for x, y in pts], outline=c["secondary"])
    draw_rect(draw, 14, 10, 20, 16, c["accent"], ox, oy)

def icon_stone_spike(draw, c, ox, oy):
    """Stone Spike -- sharp rock eruption."""
    draw_triangle_up(draw, 12, 10, 5, c["primary"], ox, oy)
    draw_triangle_up(draw, 20, 8, 6, c["accent"], ox, oy)
    draw_triangle_up(draw, 16, 6, 4, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 24, 28, 24, c["secondary"], ox, oy)

def icon_quake(draw, c, ox, oy):
    """Quake -- cracking ground."""
    draw_line_px(draw, 4, 16, 28, 16, c["primary"], ox, oy)
    draw_line_px(draw, 16, 16, 12, 26, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 20, 24, c["accent"], ox, oy)
    draw_line_px(draw, 16, 16, 8, 22, c["secondary"], ox, oy)
    draw_rect(draw, 10, 8, 13, 11, c["primary"], ox, oy)
    draw_rect(draw, 19, 6, 22, 9, c["primary"], ox, oy)

def icon_petrify(draw, c, ox, oy):
    """Petrify -- stone figure."""
    draw_circle_filled(draw, 16, 8, 4, c["primary"], ox, oy)
    draw_rect(draw, 12, 12, 20, 24, c["primary"], ox, oy)
    draw_line_px(draw, 13, 14, 19, 14, c["secondary"], ox, oy)
    draw_line_px(draw, 13, 18, 19, 18, c["secondary"], ox, oy)
    draw_line_px(draw, 16, 12, 16, 24, c["secondary"], ox, oy)

def icon_earthen_armor(draw, c, ox, oy):
    """Earthen Armor -- rock coating."""
    draw_circle_filled(draw, 16, 10, 5, c["accent"], ox, oy)
    draw_rect(draw, 10, 15, 22, 26, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 10, 6, c["primary"], ox, oy)
    draw_rect(draw, 9, 14, 9, 26, c["primary"], ox, oy)
    draw_rect(draw, 23, 14, 23, 26, c["primary"], ox, oy)

# --- Row 20: Aether ---

def icon_aether_mastery(draw, c, ox, oy):
    """Aether mastery -- void/energy orb."""
    draw_circle_filled(draw, 16, 15, 10, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 7, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 4, c["highlight"], ox, oy)

def icon_nova(draw, c, ox, oy):
    """Nova -- radiant burst."""
    draw_circle_filled(draw, 16, 15, 5, c["highlight"], ox, oy)
    for i in range(8):
        angle = math.radians(i * 45)
        x2 = int(16 + 12 * math.cos(angle))
        y2 = int(15 + 12 * math.sin(angle))
        draw_line_px(draw, 16, 15, x2, y2, c["accent"], ox, oy)

def icon_weld(draw, c, ox, oy):
    """Weld -- energy connecting two points."""
    draw_circle_filled(draw, 8, 15, 4, c["primary"], ox, oy)
    draw_circle_filled(draw, 24, 15, 4, c["primary"], ox, oy)
    draw_line_px(draw, 12, 13, 20, 13, c["accent"], ox, oy)
    draw_line_px(draw, 12, 15, 20, 15, c["highlight"], ox, oy)
    draw_line_px(draw, 12, 17, 20, 17, c["accent"], ox, oy)

def icon_purify(draw, c, ox, oy):
    """Purify -- cleansing sparkles."""
    positions = [(8, 8), (20, 6), (12, 20), (24, 16), (16, 12), (6, 16), (22, 24)]
    for x, y in positions:
        draw_star(draw, x, y, 3, 1, 4, c["accent"], ox, oy)
    draw_star(draw, 16, 12, 4, 2, 4, c["highlight"], ox, oy)

def icon_drain(draw, c, ox, oy):
    """Drain -- siphon energy."""
    draw_circle_filled(draw, 8, 15, 4, (200, 60, 60), ox, oy)
    draw_circle_filled(draw, 24, 15, 4, c["primary"], ox, oy)
    draw_line_px(draw, 12, 13, 20, 13, (200, 60, 60), ox, oy)
    draw_line_px(draw, 12, 15, 20, 15, c["accent"], ox, oy)
    draw_line_px(draw, 12, 17, 20, 17, (200, 60, 60), ox, oy)

def icon_singularity(draw, c, ox, oy):
    """Singularity -- dark void pulling inward."""
    draw_circle_filled(draw, 16, 15, 10, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 7, (30, 20, 50), ox, oy)
    draw_circle_filled(draw, 16, 15, 3, (10, 5, 20), ox, oy)
    # inward spiral lines
    for i in range(6):
        angle = math.radians(i * 60)
        x1 = int(16 + 12 * math.cos(angle))
        y1 = int(15 + 12 * math.sin(angle))
        x2 = int(16 + 5 * math.cos(angle + 0.5))
        y2 = int(15 + 5 * math.sin(angle + 0.5))
        draw_line_px(draw, x1, y1, x2, y2, c["accent"], ox, oy)

# --- Row 21: Restoration ---

def icon_restoration_mastery(draw, c, ox, oy):
    """Restoration mastery -- healing heart."""
    draw_circle_filled(draw, 12, 12, 5, c["primary"], ox, oy)
    draw_circle_filled(draw, 20, 12, 5, c["primary"], ox, oy)
    pts = [(7, 14), (16, 25), (25, 14)]
    draw.polygon([(ox+x, oy+y) for x, y in pts], fill=c["primary"])
    draw_rect(draw, 15, 10, 17, 20, c["highlight"], ox, oy)
    draw_rect(draw, 12, 14, 20, 16, c["highlight"], ox, oy)

def icon_mend(draw, c, ox, oy):
    """Mend -- quick sparkle heal."""
    draw_star(draw, 16, 15, 8, 3, 4, c["accent"], ox, oy)
    draw_star(draw, 16, 15, 5, 2, 4, c["highlight"], ox, oy)
    draw_rect(draw, 15, 13, 17, 17, c["highlight"], ox, oy)
    draw_rect(draw, 14, 14, 18, 16, c["highlight"], ox, oy)

def icon_barrier(draw, c, ox, oy):
    """Barrier -- magic shield bubble."""
    draw_circle_outline(draw, 16, 15, 10, c["accent"], ox, oy)
    draw_circle_outline(draw, 16, 15, 9, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 8, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)

def icon_cleanse(draw, c, ox, oy):
    """Cleanse -- purifying wave."""
    for x in range(4, 28):
        y = int(15 + 5 * math.sin((x - 4) * math.pi / 6))
        draw_pixel(draw, x, y, c["accent"], ox, oy)
        draw_pixel(draw, x, y + 1, c["primary"], ox, oy)
    draw_pixel(draw, 10, 8, c["highlight"], ox, oy)
    draw_pixel(draw, 22, 10, c["highlight"], ox, oy)

def icon_regeneration(draw, c, ox, oy):
    """Regeneration -- green pulse."""
    draw_circle_filled(draw, 16, 15, 8, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 5, c["primary"], ox, oy)
    draw_triangle_up(draw, 16, 10, 3, c["highlight"], ox, oy)
    draw_triangle_up(draw, 10, 14, 2, c["accent"], ox, oy)
    draw_triangle_up(draw, 22, 14, 2, c["accent"], ox, oy)

# --- Row 22: Amplification ---

def icon_amplification_mastery(draw, c, ox, oy):
    """Amplification mastery -- neural network."""
    draw_circle_filled(draw, 16, 14, 8, c["secondary"], ox, oy)
    draw_circle_filled(draw, 16, 14, 6, c["primary"], ox, oy)
    draw_line_px(draw, 10, 10, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 22, 10, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 10, 18, 16, 14, c["accent"], ox, oy)
    draw_line_px(draw, 22, 18, 16, 14, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 14, 2, c["highlight"], ox, oy)

def icon_mana_surge(draw, c, ox, oy):
    """Mana Surge -- blue mana burst."""
    draw_circle_filled(draw, 16, 15, 6, c["primary"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)
    for i in range(6):
        angle = math.radians(i * 60)
        x2 = int(16 + 10 * math.cos(angle))
        y2 = int(15 + 10 * math.sin(angle))
        draw_line_px(draw, 16, 15, x2, y2, c["accent"], ox, oy)

def icon_quick_cast(draw, c, ox, oy):
    """Quick Cast -- speed cast clock."""
    draw_circle_outline(draw, 16, 15, 8, c["primary"], ox, oy)
    draw_line_px(draw, 16, 15, 16, 8, c["accent"], ox, oy)
    draw_line_px(draw, 16, 15, 22, 13, c["accent"], ox, oy)
    draw_line_px(draw, 4, 10, 8, 12, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 15, 8, 15, c["highlight"], ox, oy)
    draw_line_px(draw, 4, 20, 8, 18, c["highlight"], ox, oy)

def icon_resonance(draw, c, ox, oy):
    """Resonance -- vibrating waves."""
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)
    for r in [5, 8, 11]:
        draw_circle_outline(draw, 16, 15, r, c["accent"], ox, oy)

def icon_focus_channel(draw, c, ox, oy):
    """Focus Channel -- meditation circle."""
    draw_circle_outline(draw, 16, 15, 10, c["primary"], ox, oy)
    draw_circle_outline(draw, 16, 15, 7, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 12, 3, c["highlight"], ox, oy)
    draw_triangle_down(draw, 16, 19, 4, c["primary"], ox, oy)

# --- Row 23: Overcharge ---

def icon_overcharge_mastery(draw, c, ox, oy):
    """Overcharge mastery -- crackling danger."""
    draw_circle_filled(draw, 16, 15, 8, c["secondary"], ox, oy)
    # lightning through
    pts = [(16, 4), (12, 14), (18, 14), (14, 26)]
    for i in range(len(pts) - 1):
        draw_line_px(draw, pts[i][0], pts[i][1], pts[i+1][0], pts[i+1][1], c["accent"], ox, oy)
        draw_line_px(draw, pts[i][0]+1, pts[i][1], pts[i+1][0]+1, pts[i+1][1], c["highlight"], ox, oy)
    draw_circle_outline(draw, 16, 15, 10, c["primary"], ox, oy)

def icon_neural_burn(draw, c, ox, oy):
    """Neural Burn -- burning brain."""
    draw_circle_filled(draw, 16, 14, 7, c["primary"], ox, oy)
    draw_line_px(draw, 16, 8, 16, 20, c["secondary"], ox, oy)
    draw_rect(draw, 12, 4, 14, 8, c["accent"], ox, oy)
    draw_rect(draw, 16, 3, 18, 8, c["highlight"], ox, oy)
    draw_rect(draw, 20, 5, 22, 8, c["accent"], ox, oy)

def icon_mana_frenzy(draw, c, ox, oy):
    """Mana Frenzy -- wild mana storm."""
    for i in range(12):
        angle = math.radians(i * 30)
        r = 8 + (i % 3) * 2
        x = int(16 + r * math.cos(angle))
        y = int(15 + r * math.sin(angle))
        draw_line_px(draw, 16, 15, x, y, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)

def icon_pain_gate(draw, c, ox, oy):
    """Pain Gate -- pain to power transformation."""
    draw_line_px(draw, 4, 8, 14, 15, (200, 60, 60), ox, oy)
    draw_line_px(draw, 4, 22, 14, 15, (200, 60, 60), ox, oy)
    draw_diamond(draw, 16, 15, 4, c["primary"], ox, oy)
    draw_line_px(draw, 18, 15, 28, 8, c["accent"], ox, oy)
    draw_line_px(draw, 18, 15, 28, 22, c["accent"], ox, oy)

def icon_last_resort(draw, c, ox, oy):
    """Last Resort -- final burst explosion."""
    draw_star(draw, 16, 15, 12, 5, 8, c["primary"], ox, oy)
    draw_star(draw, 16, 15, 8, 3, 8, c["accent"], ox, oy)
    draw_circle_filled(draw, 16, 15, 3, c["highlight"], ox, oy)


# -- Row layout definition ---------------------------------------------------
# Each row: (row_number, mastery_name, palette, [(ability_name, draw_fn), ...])
# The first icon in each row is the mastery icon.

ROW_LAYOUT = [
    # Row 0: Innate
    (0, "innate", C_INNATE, icon_innate_mastery, [
        ("haste", icon_haste),
        ("sense", icon_sense),
        ("fortify", icon_fortify),
        ("armor", icon_armor),
    ]),

    # Rows 1-8: Warrior Body + Mind
    (1, "unarmed", C_WARRIOR_BODY, icon_unarmed_mastery, [
        ("punch", icon_punch),
        ("kick", icon_kick),
        ("grappling", icon_grappling),
        ("elbow_strike", icon_elbow_strike),
    ]),
    (2, "bladed", C_WARRIOR_BODY, icon_bladed_mastery, [
        ("slash", icon_slash),
        ("thrust", icon_thrust),
        ("cleave", icon_cleave),
        ("parry", icon_parry),
    ]),
    (3, "blunt", C_WARRIOR_BODY, icon_blunt_mastery, [
        ("smash", icon_smash),
        ("bump", icon_bump),
        ("crush", icon_crush),
        ("shatter", icon_shatter),
    ]),
    (4, "polearms", C_WARRIOR_BODY, icon_polearms_mastery, [
        ("pierce", icon_pierce),
        ("sweep", icon_sweep),
        ("brace", icon_brace),
        ("vault", icon_vault),
        ("haft_blow", icon_haft_blow),
    ]),
    (5, "shields", C_WARRIOR_BODY, icon_shields_mastery, [
        ("block", icon_block),
        ("shield_bash", icon_shield_bash),
        ("deflect", icon_deflect),
        ("bulwark", icon_bulwark),
    ]),
    (6, "dual_wield", C_WARRIOR_BODY, icon_dual_wield_mastery, [
        ("dual_stab", icon_dual_stab),
        ("dual_slash", icon_dual_slash),
        ("spin_attack", icon_spin_attack),
        ("rapid_combo", icon_rapid_combo),
    ]),
    (7, "discipline", C_WARRIOR_MIND, icon_discipline_mastery, [
        ("focus", icon_focus),
        ("endure", icon_endure),
        ("deep_breaths", icon_deep_breaths),
        ("blood_lust", icon_blood_lust),
    ]),
    (8, "intimidation", C_WARRIOR_MIND, icon_intimidation_mastery, [
        ("shout", icon_shout),
        ("intimidate", icon_intimidate),
        ("ugly_mug", icon_ugly_mug),
        ("battle_roar", icon_battle_roar),
    ]),

    # Rows 9-15: Ranger Weaponry + Survival
    (9, "bowmanship", C_RANGER_WEAPONRY, icon_bowmanship_mastery, [
        ("dead_eye", icon_dead_eye),
        ("pepper", icon_pepper),
        ("lob", icon_lob),
        ("pin", icon_pin),
        ("flame_arrow", icon_flame_arrow),
    ]),
    (10, "throwing", C_RANGER_WEAPONRY, icon_throwing_mastery, [
        ("flick", icon_flick),
        ("chuck", icon_chuck),
        ("fan", icon_fan),
        ("ricochet", icon_ricochet),
        ("frost_blade", icon_frost_blade),
    ]),
    (11, "firearms", C_RANGER_WEAPONRY, icon_firearms_mastery, [
        ("quick_draw", icon_quick_draw),
        ("bead", icon_bead),
        ("spray", icon_spray),
        ("snipe", icon_snipe),
        ("shock_round", icon_shock_round),
    ]),
    (12, "cqc", C_RANGER_WEAPONRY, icon_cqc_mastery, [
        ("cqc_parry", icon_cqc_parry),
        ("hunker", icon_hunker),
        ("riposte", icon_riposte),
        ("shiv", icon_shiv),
    ]),
    (13, "awareness", C_RANGER_SURVIVAL, icon_awareness_mastery, [
        ("keen_senses", icon_keen_senses),
        ("tip_toes", icon_tip_toes),
        ("disengage", icon_disengage),
        ("steady_breathing", icon_steady_breathing),
        ("rangefinding", icon_rangefinding),
        ("tracking", icon_tracking),
        ("steady_aim", icon_steady_aim),
        ("weak_spot", icon_weak_spot),
    ]),
    (14, "trapping", C_RANGER_SURVIVAL, icon_trapping_mastery, [
        ("snare", icon_snare),
        ("tripwire", icon_tripwire),
        ("decoy", icon_decoy),
        ("bait", icon_bait),
        ("ambush", icon_ambush),
    ]),
    (15, "sapping", C_RANGER_SURVIVAL, icon_sapping_mastery, [
        ("frag", icon_frag),
        ("smoke_bomb", icon_smoke_bomb),
        ("flashbang", icon_flashbang),
        ("caltrops", icon_caltrops),
        ("sticky_bomb", icon_sticky_bomb),
    ]),

    # Rows 16-23: Mage Elemental + Aether + Attunement
    (16, "fire", C_FIRE, icon_fire_mastery, [
        ("fireball", icon_fireball),
        ("flame_wall", icon_flame_wall),
        ("ignite", icon_ignite),
        ("inferno", icon_inferno),
    ]),
    (17, "water", C_WATER, icon_water_mastery, [
        ("frost_bolt", icon_frost_bolt),
        ("freeze", icon_freeze),
        ("tidal_wave", icon_tidal_wave),
        ("mist_veil", icon_mist_veil),
    ]),
    (18, "air", C_AIR, icon_air_mastery, [
        ("lightning", icon_lightning),
        ("gust", icon_gust),
        ("chain_shock", icon_chain_shock),
        ("tempest", icon_tempest),
    ]),
    (19, "earth", C_EARTH, icon_earth_mastery, [
        ("stone_spike", icon_stone_spike),
        ("quake", icon_quake),
        ("petrify", icon_petrify),
        ("earthen_armor", icon_earthen_armor),
    ]),
    (20, "aether", C_MAGE_AETHER, icon_aether_mastery, [
        ("nova", icon_nova),
        ("weld", icon_weld),
        ("purify", icon_purify),
        ("drain", icon_drain),
        ("singularity", icon_singularity),
    ]),
    (21, "restoration", C_MAGE_ATTUNEMENT, icon_restoration_mastery, [
        ("mend", icon_mend),
        ("barrier", icon_barrier),
        ("cleanse", icon_cleanse),
        ("regeneration", icon_regeneration),
    ]),
    (22, "amplification", C_MAGE_ATTUNEMENT, icon_amplification_mastery, [
        ("mana_surge", icon_mana_surge),
        ("quick_cast", icon_quick_cast),
        ("resonance", icon_resonance),
        ("focus_channel", icon_focus_channel),
    ]),
    (23, "overcharge", C_MAGE_ATTUNEMENT, icon_overcharge_mastery, [
        ("neural_burn", icon_neural_burn),
        ("mana_frenzy", icon_mana_frenzy),
        ("pain_gate", icon_pain_gate),
        ("last_resort", icon_last_resort),
    ]),
]


# -- Sheet generation --------------------------------------------------------

def generate_sheet(row_layout, output_png, output_json):
    """Generate the combined 512x1024 sprite sheet and JSON atlas."""
    img = Image.new("RGBA", (SHEET_W, SHEET_H), BG_COLOR)
    draw = ImageDraw.Draw(img)
    atlas = {}

    total_icons = 0

    for row_num, mastery_name, palette, mastery_fn, abilities in row_layout:
        # Column 0: mastery icon
        ox = 0
        oy = row_num * ICON_SIZE
        mastery_fn(draw, palette, ox, oy)
        atlas[mastery_name] = {
            "x": ox, "y": oy, "w": ICON_SIZE, "h": ICON_SIZE,
            "col": 0, "row": row_num,
        }
        total_icons += 1

        # Columns 1+: ability icons
        for col_idx, (ability_name, ability_fn) in enumerate(abilities, start=1):
            ox = col_idx * ICON_SIZE
            oy = row_num * ICON_SIZE
            ability_fn(draw, palette, ox, oy)
            atlas[ability_name] = {
                "x": ox, "y": oy, "w": ICON_SIZE, "h": ICON_SIZE,
                "col": col_idx, "row": row_num,
            }
            total_icons += 1

    img.save(output_png, "PNG")
    print(f"  Saved: {output_png} ({total_icons} icons, {SHEET_W}x{SHEET_H})")

    with open(output_json, "w") as f:
        json.dump(atlas, f, indent=2)
    print(f"  Saved: {output_json} ({len(atlas)} entries)")

    return total_icons


def main():
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    icons_dir = os.path.join(project_root, "assets", "icons")
    os.makedirs(icons_dir, exist_ok=True)

    print("Generating combined abilities icon sprite sheet (512x1024)...")
    print()

    png_path = os.path.join(icons_dir, "abilities_icons.png")
    json_path = os.path.join(icons_dir, "abilities_icons.json")

    total = generate_sheet(ROW_LAYOUT, png_path, json_path)

    print()
    print("Done! Sprite sheet ready in assets/icons/")
    print(f"  Total icons: {total}")
    print(f"  Grid: {COLS} cols x {ROWS} rows, {ICON_SIZE}x{ICON_SIZE}px per icon")
    print(f"  Sheet: {SHEET_W}x{SHEET_H}px")
    print()
    print("Note: Old files (skills_icons.png, spells_icons.png) preserved.")


if __name__ == "__main__":
    main()
