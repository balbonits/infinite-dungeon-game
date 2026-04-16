# Godot 4 Engine Reference

What's built-in and what we should use. Updated 2026-04-16.

## UI System

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **Theme resources** (.tres) | Global styling — colors, fonts, styleboxes. Children inherit. | Not using — we do per-button overrides in code. Should migrate. |
| **RichTextLabel + BBCode** | Formatted text with colors, bold, effects, inline images | Not using — plain Labels. Good for ability descriptions. |
| **ItemList** | Built-in selectable list with icons, search, multi-select | Not using — custom button lists. |
| **Tree** | Hierarchical data with columns, checkboxes, editable cells | Not using — could fit skill trees. |
| **TabContainer/TabBar** | Built-in tab switching with themed tabs | Not using — built GameTabPanel (thin wrapper). |
| **ScrollContainer.FollowFocus** | Auto-scroll to focused child | Using. |
| **Container nodes** | Auto-layout (VBox, HBox, Grid, Margin, Split, Flow) | Using. |
| **Control.FocusNeighbor** | Set per-control Up/Down/Left/Right focus targets | Not using — custom KeyboardNav. Could simplify. |
| **PopupMenu** | Context menu with items, shortcuts, icons | Not using — built ActionMenu. |
| **GameWindow** (ours) | Unified base for all modal windows | Using — all 14+ windows migrated. |
| **GameTabPanel** (ours) | Reusable Q/E tab system | Using — Skills + Abilities tabs. |

## Animation & Effects

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **Tween** | Animate any property, chainable | Using (NPC fade). |
| **AnimationPlayer** | Keyframe animation for ANY property on ANY node | Partial (sprite rotations). |
| **AnimationTree + StateMachine** | State-based animation blending | Not using. Good for character states. |
| **GPUParticles2D** | GPU particle effects | Not using. Future: ability VFX. |
| **CanvasItem shader** | Per-node visual effects (flash, glow, outline) | Partial (FlashFx uses Modulate). |
| **SceneTreeTimer** | One-shot timer without a node: `GetTree().CreateTimer(sec)` | Not using — could replace Timer nodes. |

## Data & Resources

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **Custom Resources** (.tres/.res) | Editor-editable data, serializable, inspector support | Not using — C# records instead. |
| **ResourceLoader** async | Background loading with progress callback | Not using. |
| **Resource.LocalToScene** | Per-instance copies of shared resources | Not using. |

## Audio

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **AudioStreamPlayer** (2D/3D) | Play sounds, positional or global | Skipped (no audio tasks). |
| **AudioBus** | Mixer channels (Master/SFX/BGM) with effects | Skipped. |
| **AudioStreamRandomizer** | Random pitch/volume variation | Skipped. |

## Navigation & Physics

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **NavigationServer2D** | Pathfinding with nav meshes | Not using — enemies chase directly. |
| **TileSet terrains** | Smart auto-tiling with terrain rules | Partial. |
| **Area2D signals** | body_entered/exited detection | Using. |

## Rendering

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **CanvasLayer** | Layer separation (game world vs UI) | Using. |
| **Z-index** | Draw order within same layer | Using. |
| **MultiMesh** | Draw thousands of identical objects cheaply | Not using. Future: particle-heavy floors. |
| **CanvasGroup** | Group nodes for shared shader/blend effects | Not using. |

## C# Specific

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **Source generators** | Compile-time signal/export code gen | Using (automatic). |
| **Variant struct** | Type-safe Godot-C# interop, no boxing | Using (automatic). |
| **ToSignal + async/await** | `await ToSignal(GetTree(), SignalName.ProcessFrame)` | Not using. Could clean up tween callbacks. |
| **Platform defines** | `GODOT_WINDOWS`, `GODOT_MACOS` for conditional compilation | Not using. |
| **NuGet packages** | Full .NET ecosystem | Using (FluentAssertions, etc). |

## Signals & Communication

| Pattern | What It Does | Status |
|---------|-------------|--------|
| **Direct signals** | Node-to-node via Connect() | Using. |
| **EventBus autoload** | Global signal hub for cross-system events | Using (EventBus.cs). |
| **Groups + call_group** | Broadcast to tagged nodes | Partial (enemy/player groups). |
| **Notifications** | Engine lifecycle callbacks (_EnterTree, _ExitTree, etc) | Partial. |

## Input System

| Built-in | What It Does | Status |
|----------|-------------|--------|
| **InputMap** | Named actions mapped to keys/buttons | Using. |
| **_UnhandledInput** | Input after GUI consumes what it needs | Using (GameWindow). |
| **_Input** | High-priority input (pause, debug) | Not using — could use for pause toggle. |
| **Input.GetVector** | Poll movement axes directly | Using (Player movement). |
| **InputEvent propagation** | _Input -> _GuiInput -> _UnhandledInput order | Understanding improved — Player.cs bug was polling vs events. |

## Priority Improvements

1. **Theme resource** — replace hundreds of `AddThemeStyleboxOverride` calls with one `.tres` file
2. **RichTextLabel** — colored stat text in ability descriptions
3. **SceneTreeTimer** — replace Timer nodes for one-shot delays
4. **async/await + ToSignal** — cleaner than tween callbacks
5. **Control.FocusNeighbor** — could simplify KeyboardNav for linear lists

## Sources

- [Godot Features List](https://docs.godotengine.org/en/stable/about/list_of_features.html)
- [Theme Editor](https://docs.godotengine.org/en/stable/tutorials/ui/gui_using_theme_editor.html)
- [ThemeDB](https://docs.godotengine.org/en/latest/classes/class_themedb.html)
- [Using Containers](https://docs.godotengine.org/en/stable/tutorials/ui/gui_containers.html)
- [BBCode in RichTextLabel](https://docs.godotengine.org/en/stable/tutorials/ui/bbcode_in_richtextlabel.html)
- [Canvas Layers](https://docs.godotengine.org/en/stable/tutorials/2d/canvas_layers.html)
- [Tween](https://docs.godotengine.org/en/stable/classes/class_tween.html)
- [SceneTreeTimer](https://docs.godotengine.org/en/stable/classes/class_scenetreetimer.html)
- [AnimationTree StateMachine](https://kidscancode.org/godot_recipes/4.x/animation/using_animation_sm/index.html)
- [Custom Resources](https://ezcha.net/news/3-1-23-custom-resources-are-op-in-godot-4)
- [Signals Architecture](https://blog.febucci.com/2024/12/godot-signals-architecture/)
- [C# in Godot 4](https://godotengine.org/article/whats-new-in-csharp-for-godot-4-0/)
- [Audio Streams](https://docs.godotengine.org/en/stable/tutorials/audio/audio_streams.html)
- [Input Handling](https://school.gdquest.com/cheatsheets/input)
- [Best Practices](https://docs.godotengine.org/en/stable/tutorials/best_practices/index.html)
- [Node Alternatives](https://docs.godotengine.org/en/stable/tutorials/best_practices/node_alternatives.html)
- [Save/Load with Resources](https://www.gdquest.com/library/save_game_godot4/)
- [Godot Nodes Cheat Sheet](https://generalistprogrammer.com/cheatsheets/godot-nodes)
