using System.Collections.Generic;

namespace DungeonGame.Dungeon;

public class FloorCache
{
    private readonly int _maxSize;
    private readonly DungeonGenerator _generator;
    private readonly Dictionary<int, FloorData> _cache = new();
    private readonly LinkedList<int> _accessOrder = new();

    public int Count => _cache.Count;

    public FloorCache(DungeonGenerator generator, int maxSize = 10)
    {
        _generator = generator;
        _maxSize = maxSize;
    }

    public FloorData GetFloor(int floorNumber, int seed)
    {
        if (_cache.TryGetValue(floorNumber, out var cached))
        {
            // Move to end of access order (most recently used)
            _accessOrder.Remove(floorNumber);
            _accessOrder.AddLast(floorNumber);
            return cached;
        }

        // Generate new floor
        var floor = _generator.Generate(seed, floorNumber);

        // Evict LRU if at capacity
        while (_cache.Count >= _maxSize)
            Evict();

        _cache[floorNumber] = floor;
        _accessOrder.AddLast(floorNumber);
        return floor;
    }

    public bool Contains(int floorNumber) => _cache.ContainsKey(floorNumber);

    private void Evict()
    {
        if (_accessOrder.Count == 0) return;
        int lru = _accessOrder.First!.Value;
        _accessOrder.RemoveFirst();
        _cache.Remove(lru);
    }

    public void Clear()
    {
        _cache.Clear();
        _accessOrder.Clear();
    }
}
