#!/usr/bin/env python3
"""Generate isometric tile PNGs for 'A Dungeon in the Middle of Nowhere'.

Creates:
  assets/tiles/floor.png  -- 64x32 solid dark-blue diamond
  assets/tiles/wall.png   -- 64x32 two-tone diamond (outline + semi-transparent interior)

No external dependencies -- uses struct + zlib to write minimal valid PNGs.
"""

import os
import struct
import zlib

# ---------------------------------------------------------------------------
# Tile dimensions
# ---------------------------------------------------------------------------
WIDTH = 64
HEIGHT = 32
CX = WIDTH / 2.0   # 32.0
CY = HEIGHT / 2.0  # 16.0

# ---------------------------------------------------------------------------
# Colors (R, G, B, A)
# ---------------------------------------------------------------------------
FLOOR_COLOR = (36, 49, 74, 255)          # #24314a, fully opaque
WALL_OUTLINE = (60, 70, 100, 255)        # #3c4664, fully opaque
WALL_INTERIOR = (60, 70, 100, 128)       # #3c4664, half-transparent
TRANSPARENT = (0, 0, 0, 0)              # Fully transparent

WALL_OUTLINE_THRESHOLD = 0.85           # Normalized distance threshold


def diamond_distance(x: int, y: int) -> float:
    """Return the normalized Manhattan distance from the tile center.

    A value <= 1.0 means the pixel is inside the diamond.
    A value > 1.0 means the pixel is outside.
    """
    return abs(x - CX) / CX + abs(y - CY) / CY


def make_floor_pixel(x: int, y: int) -> tuple[int, int, int, int]:
    """Return RGBA for a floor tile pixel."""
    if diamond_distance(x, y) <= 1.0:
        return FLOOR_COLOR
    return TRANSPARENT


def make_wall_pixel(x: int, y: int) -> tuple[int, int, int, int]:
    """Return RGBA for a wall tile pixel."""
    d = diamond_distance(x, y)
    if d > 1.0:
        return TRANSPARENT
    if d > WALL_OUTLINE_THRESHOLD:
        return WALL_OUTLINE
    return WALL_INTERIOR


def create_png(width: int, height: int, pixel_func) -> bytes:
    """Create a minimal valid PNG file in memory.

    Args:
        width: Image width in pixels.
        height: Image height in pixels.
        pixel_func: Callable(x, y) -> (R, G, B, A) for each pixel.

    Returns:
        Complete PNG file as bytes.
    """
    # Build raw image data (filter byte 0 = None for each row, then RGBA pixels)
    raw_rows = []
    for y in range(height):
        row = b'\x00'  # Filter type: None
        for x in range(width):
            r, g, b, a = pixel_func(x, y)
            row += struct.pack('BBBB', r, g, b, a)
        raw_rows.append(row)
    raw_data = b''.join(raw_rows)

    # Compress with zlib (deflate)
    compressed = zlib.compress(raw_data)

    # --- PNG file structure ---
    # Signature
    signature = b'\x89PNG\r\n\x1a\n'

    # IHDR chunk
    ihdr_data = struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)
    # 8 = bit depth, 6 = RGBA color type, 0 = compression, 0 = filter, 0 = interlace
    ihdr_chunk = _make_chunk(b'IHDR', ihdr_data)

    # IDAT chunk (compressed image data)
    idat_chunk = _make_chunk(b'IDAT', compressed)

    # IEND chunk
    iend_chunk = _make_chunk(b'IEND', b'')

    return signature + ihdr_chunk + idat_chunk + iend_chunk


def _make_chunk(chunk_type: bytes, data: bytes) -> bytes:
    """Create a PNG chunk: length + type + data + CRC32."""
    length = struct.pack('>I', len(data))
    crc = struct.pack('>I', zlib.crc32(chunk_type + data) & 0xFFFFFFFF)
    return length + chunk_type + data + crc


def main():
    os.makedirs('assets/tiles', exist_ok=True)

    # Generate floor tile
    floor_png = create_png(WIDTH, HEIGHT, make_floor_pixel)
    with open('assets/tiles/floor.png', 'wb') as f:
        f.write(floor_png)
    print(f'Created assets/tiles/floor.png ({len(floor_png)} bytes)')

    # Generate wall tile
    wall_png = create_png(WIDTH, HEIGHT, make_wall_pixel)
    with open('assets/tiles/wall.png', 'wb') as f:
        f.write(wall_png)
    print(f'Created assets/tiles/wall.png ({len(wall_png)} bytes)')


if __name__ == '__main__':
    main()
