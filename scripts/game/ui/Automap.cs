using Godot;
using DungeonGame.Dungeon;

namespace DungeonGame;

public enum AutomapMode { Off, Overlay, FullMap }

/// <summary>
/// Diablo 1-style automap overlay. Renders a wireframe minimap of explored
/// dungeon tiles on top of gameplay. Supports Off / Overlay / FullMap modes.
/// </summary>
public partial class Automap : Control
{
    // Scale: pixels per tile
    private const float OverlayScale = 3f;
    private const float FullMapScale = 5f;

    // Isometric basis vectors (half-tile in screen space per grid step)
    private static readonly Vector2 IsoX = new(1f, 0.5f);
    private static readonly Vector2 IsoY = new(-1f, 0.5f);

    // Colors — Diablo-style wireframe palette
    private static readonly Color WallColor = new(0.71f, 0.63f, 0.31f);
    private static readonly Color FloorColor = new(0.71f, 0.63f, 0.31f, 0.3f);
    private static readonly Color PlayerColor = new(1.0f, 0.66f, 0.0f);
    private static readonly Color EntranceColor = new(0.2f, 1.0f, 0.33f);
    private static readonly Color ExitColor = new(1.0f, 1.0f, 0.0f);
    private static readonly Color BossOutline = new(1.0f, 0.2f, 0.2f);
    private static readonly Color TreasureOutline = new(1.0f, 0.84f, 0.0f);
    private static readonly Color ChallengeOutline = new(1.0f, 0.53f, 0.0f);
    private static readonly Color FullMapBg = new(0f, 0f, 0f, 0.2f);

    private AutomapMode _mode = AutomapMode.Off;
    private FloorData? _floor;
    private int _playerX;
    private int _playerY;
    private Vector2 _panOffset = Vector2.Zero;

    public AutomapMode Mode => _mode;

    public void CycleMode()
    {
        _mode = _mode switch
        {
            AutomapMode.Off => AutomapMode.Overlay,
            AutomapMode.Overlay => AutomapMode.FullMap,
            AutomapMode.FullMap => AutomapMode.Off,
            _ => AutomapMode.Off
        };
        _panOffset = Vector2.Zero;
        QueueRedraw();
    }

    public void SetFloorData(FloorData floor)
    {
        _floor = floor;
        _panOffset = Vector2.Zero;
        QueueRedraw();
    }

    public void SetPlayerPosition(int tileX, int tileY)
    {
        _playerX = tileX;
        _playerY = tileY;
        QueueRedraw();
    }

    /// <summary>
    /// Shift the full-map pan offset (arrow keys in full-map mode).
    /// </summary>
    public void Pan(Vector2 delta)
    {
        if (_mode == AutomapMode.FullMap)
        {
            _panOffset += delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_mode == AutomapMode.Off || _floor == null) return;

        var viewport = GetViewportRect().Size;
        float scale = _mode == AutomapMode.Overlay ? OverlayScale : FullMapScale;

        // Center of the viewport
        var center = viewport * 0.5f;

        // Player tile mapped to iso screen coords (used as camera center)
        var playerIso = TileToIso(_playerX, _playerY, scale);

        // Origin offset so the player is at screen center, plus panning
        var origin = center - playerIso + _panOffset;

        // Full map gets a dim background
        if (_mode == AutomapMode.FullMap)
            DrawRect(new Rect2(Vector2.Zero, viewport), FullMapBg);

        // --- Draw explored floor dots and wall lines ---
        for (int x = 0; x < _floor.Width; x++)
        {
            for (int y = 0; y < _floor.Height; y++)
            {
                if (!_floor.IsExplored(x, y)) continue;

                var pos = TileToIso(x, y, scale) + origin;

                // Cull tiles outside viewport (with generous margin)
                if (pos.X < -scale * 4 || pos.X > viewport.X + scale * 4 ||
                    pos.Y < -scale * 4 || pos.Y > viewport.Y + scale * 4)
                    continue;

                if (_floor.Tiles[x, y] == TileType.Floor)
                {
                    // Small dot for floor tiles
                    float dotSize = scale * 0.3f;
                    if (dotSize < 0.5f) dotSize = 0.5f;
                    DrawCircle(pos, dotSize, FloorColor);
                }
                else if (_floor.Tiles[x, y] == TileType.Wall)
                {
                    // Only draw walls adjacent to explored floors
                    if (IsAdjacentToExploredFloor(x, y))
                        DrawWallEdges(x, y, scale, origin);
                }
            }
        }

        // --- Draw special room outlines ---
        foreach (var room in _floor.Rooms)
        {
            Color? outlineColor = room.Kind switch
            {
                RoomKind.Boss => BossOutline,
                RoomKind.Treasure => TreasureOutline,
                RoomKind.Challenge => ChallengeOutline,
                _ => null
            };

            if (outlineColor == null) continue;

            // Only draw if at least one tile in the room is explored
            if (!IsRoomPartiallyExplored(room)) continue;

            DrawRoomOutline(room, scale, origin, outlineColor.Value);
        }

        // --- Draw entrance/exit markers ---
        foreach (var room in _floor.Rooms)
        {
            if (room.Kind != RoomKind.Entrance && room.Kind != RoomKind.Exit) continue;
            if (!_floor.IsExplored(room.CenterX, room.CenterY)) continue;

            var markerPos = TileToIso(room.CenterX, room.CenterY, scale) + origin;
            float markerSize = scale * 0.8f;
            if (markerSize < 2f) markerSize = 2f;

            Color markerColor = room.Kind == RoomKind.Entrance ? EntranceColor : ExitColor;
            DrawCircle(markerPos, markerSize, markerColor);
        }

        // --- Draw player marker ---
        var playerPos = TileToIso(_playerX, _playerY, scale) + origin;
        float playerSize = scale * 0.7f;
        if (playerSize < 2f) playerSize = 2f;
        DrawCircle(playerPos, playerSize, PlayerColor);
    }

    private static Vector2 TileToIso(int tileX, int tileY, float scale)
    {
        return (IsoX * tileX + IsoY * tileY) * scale;
    }

    private bool IsAdjacentToExploredFloor(int x, int y)
    {
        if (_floor == null) return false;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (_floor.IsFloor(nx, ny) && _floor.IsExplored(nx, ny))
                    return true;
            }
        return false;
    }

    private bool IsRoomPartiallyExplored(RoomData room)
    {
        if (_floor == null) return false;
        for (int rx = room.X; rx < room.X + room.Width; rx++)
            for (int ry = room.Y; ry < room.Y + room.Height; ry++)
                if (_floor.IsExplored(rx, ry))
                    return true;
        return false;
    }

    private void DrawWallEdges(int x, int y, float scale, Vector2 origin)
    {
        // Draw lines along edges where this wall tile borders an explored floor tile
        // Uses isometric diamond edges for a wireframe look
        var center = TileToIso(x, y, scale) + origin;
        float hs = scale; // half-size of iso diamond

        // Top-right edge (towards +X neighbor)
        var tr = center + new Vector2(hs, hs * 0.5f);
        // Bottom-right edge (towards +Y neighbor)
        var br = center + new Vector2(-hs, hs * 0.5f);
        // Top vertex
        var top = center + new Vector2(0, -hs * 0.5f);
        // Bottom vertex
        var bot = center + new Vector2(0, hs * 0.5f);

        float lineWidth = _mode == AutomapMode.Overlay ? 1.0f : 1.5f;

        // Check each cardinal direction and draw the corresponding edge
        if (_floor!.IsFloor(x + 1, y) && _floor.IsExplored(x + 1, y))
            DrawLine(top, tr, WallColor, lineWidth);
        if (_floor.IsFloor(x - 1, y) && _floor.IsExplored(x - 1, y))
            DrawLine(bot, br, WallColor, lineWidth);
        if (_floor.IsFloor(x, y + 1) && _floor.IsExplored(x, y + 1))
            DrawLine(top, br, WallColor, lineWidth);
        if (_floor.IsFloor(x, y - 1) && _floor.IsExplored(x, y - 1))
            DrawLine(bot, tr, WallColor, lineWidth);

        // If this wall has explored-floor neighbors on both sides of a corner,
        // draw the full outline edge for visual continuity
        if (_floor.IsFloor(x + 1, y) && _floor.IsFloor(x, y - 1) &&
            _floor.IsExplored(x + 1, y) && _floor.IsExplored(x, y - 1))
            DrawLine(tr, new Vector2(center.X + hs, center.Y), WallColor, lineWidth);

        if (_floor.IsFloor(x - 1, y) && _floor.IsFloor(x, y + 1) &&
            _floor.IsExplored(x - 1, y) && _floor.IsExplored(x, y + 1))
            DrawLine(br, new Vector2(center.X - hs, center.Y), WallColor, lineWidth);
    }

    private void DrawRoomOutline(RoomData room, float scale, Vector2 origin, Color color)
    {
        // Draw an isometric rectangle outline around the room bounds
        var tl = TileToIso(room.X, room.Y, scale) + origin;
        var tr = TileToIso(room.X + room.Width, room.Y, scale) + origin;
        var bl = TileToIso(room.X, room.Y + room.Height, scale) + origin;
        var br = TileToIso(room.X + room.Width, room.Y + room.Height, scale) + origin;

        float lineWidth = _mode == AutomapMode.Overlay ? 1.0f : 1.5f;
        DrawLine(tl, tr, color, lineWidth);
        DrawLine(tr, br, color, lineWidth);
        DrawLine(br, bl, color, lineWidth);
        DrawLine(bl, tl, color, lineWidth);
    }
}
