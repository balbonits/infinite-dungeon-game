# Flow: Blacksmith

**Script:** `scripts/ui/BlacksmithWindow.cs`
**Opened by:** NPC Panel → Blacksmith → "Open Forge"

## Two Tabs

| Tab | Key | Function |
|-----|-----|----------|
| Craft | Q | Apply affixes to equipment |
| Recycle | E | Break down gear for materials/gold |

## Craft Flow

```
1. Select equipment item from inventory
2. View available affixes (filtered by item level)
3. Select affix to apply
4. Verify: Crafting.CanApplyAffix(item, affix, inventory)
   - Item level >= affix min level
   - Prefix count < 3 (or suffix < 3)
   - No duplicate affix
   - Enough gold
5. Press confirm
6. Crafting.ApplyAffix(item, affix, inventory)
7. Gold deducted, affix permanently added
```

## Recycle Flow

```
1. Select equipment item
2. Preview gold return: Crafting.RecycleItem(item)
   - Base: 5 + itemLevel * 2
   - Quality bonus: Superior +25%, Elite +50%
   - Per-affix bonus: +10 gold each
3. Press confirm
4. Item destroyed, gold awarded
```

## Input

| Input | Action |
|-------|--------|
| Q / E | Switch Craft/Recycle tabs |
| Up/Down | Navigate items/affixes |
| S / action_cross | Confirm action |
| D / Escape | Close blacksmith |
