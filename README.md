# Unity Greybox Maze Prototype

A randomly generated 3D maze you walk through in first person. Greybox only — no
textures, no menus, no win logic. Every time you press Play you get a brand-new maze.

All code lives in `Assets/Scripts/`.

---

## ⚠️ Read this first — repository assumption

**This repository is not a Unity project.** It is the `langchain-quickstart`
Streamlit app (its original README is preserved at the bottom of this file).
There is no `Assets/`, `ProjectSettings/` or `Packages/` folder in it.

So the maze code has been added as a **drop-in `Assets/Scripts/` folder**. To use it:

1. Open your existing Unity 3D project (Unity 6 LTS).
2. Copy the four `.cs` files from this repo's `Assets/Scripts/` into your project's
   `Assets/Scripts/` folder (create it if needed). Drag-and-drop into the Unity
   Project window works fine.
3. Unity will generate the matching `.meta` files on import. There are no `.meta`
   files in this repo because only the Editor can create them correctly.

Everything else below assumes the scripts are sitting in your Unity project.

---

## How to run — three steps

1. In any scene (an empty one is fine), **create an empty GameObject**
   (`GameObject > Create Empty`). Name it anything, e.g. `Bootstrap`.
2. **Attach `MazeBootstrap.cs`** to it (drag the script onto the GameObject).
3. **Press Play.**

That's it. No prefabs, no dragging references into slots, no editor scripts.
At runtime the bootstrap builds the floor, the maze walls, the red centre cube,
a light, the player and the camera — all from code.

### Controls

| Input | Action |
|---|---|
| `W` `A` `S` `D` (or arrow keys) | Move |
| Mouse | Look |
| `Esc` | Free / re-lock the mouse cursor |
| Left click (when cursor is free) | Re-lock the cursor |

You spawn in the bottom-left corner facing down the first open corridor. Walk toward
the **red cube** at the centre. The maze is always solvable (see "Solvability" below).

---

## The scripts

| File | What it does |
|---|---|
| `MazeGenerator.cs` | Pure C# — no Unity types. Runs the recursive backtracker and outputs a `bool[,]` "solid / open" tile grid. |
| `MazeBootstrap.cs` | The single component you attach. Builds all geometry, lighting, player and camera at runtime. |
| `FirstPersonController.cs` | WASD + mouse look on a `CharacterController`. Added to the player automatically. |
| `SimpleInput.cs` | Thin input wrapper so the prototype works with either the old Input Manager **or** the new Input System package. |

---

## Tunable fields

All of these are on the **`MazeBootstrap`** component in the Inspector.

### Maze
| Field | Default | Meaning |
|---|---|---|
| `mazeWidth` | `15` | Maze size in **cells** (rooms), not tiles. Clamped to 2–200. |
| `mazeHeight` | `15` | Same, for the other axis. |
| `seed` | `0` | `0` or less = **new random maze every Play**. Any value above `0` = that exact maze every time (for reproducible debugging). The seed actually used is printed to the Console on every build, so you can copy it back in to replay a maze you liked. |

### Dimensions
| Field | Default | Meaning |
|---|---|---|
| `tileSize` | `2` | World units per tile. This is **both** the corridor width and the wall thickness. |
| `wallHeight` | `3` | Wall height in world units. |

### Colours (greybox)
| Field | Default | Meaning |
|---|---|---|
| `wallColor` | light grey | Wall colour. |
| `floorColor` | mid grey | Floor colour. |
| `markerColor` | red | The centre landmark cube. |

### Player
| Field | Default | Meaning |
|---|---|---|
| `moveSpeed` | `4.5` | Units per second. |
| `mouseSensitivity` | `2.0` | Higher = twitchier. |
| `eyeHeight` | `1.6` | Camera height above the floor. |

### Scene setup
| Field | Default | Meaning |
|---|---|---|
| `createLightIfMissing` | `true` | Adds a directional light **only** if the scene has no light at all. |
| `ambientLight` | dark grey | Flat ambient colour so corridors are never pitch black. Set to pure black to leave the scene's own lighting settings untouched. |

There are also fields on `FirstPersonController` (`gravity`, `pitchLimit`, `invertY`),
but the bootstrap creates that component at runtime, so change those in the script's
defaults rather than the Inspector.

### Rebuild without restarting
While in Play mode, right-click the `MazeBootstrap` component header in the Inspector
and choose **Rebuild Maze** to roll a new maze on the spot.

---

## How the maze is built

1. **Recursive backtracker (depth-first carve).** Start at cell (0,0), repeatedly walk
   to a random *unvisited* neighbour and knock down the wall between them. At a dead end,
   backtrack. Implemented with an explicit `Stack<>` rather than real recursion, so a
   large maze cannot blow the call stack.
2. **Cells → tiles.** A `W x H` maze becomes a `(2W+1) x (2H+1)` grid of tiles. Each cell
   is one open tile; the tile between two linked cells is carved open as a doorway;
   everything else stays solid. The outer ring is always solid, so the maze is sealed.
3. **Tiles → cubes.** Each *horizontal run* of solid tiles is merged into a single
   stretched cube. Identical shape and collision to one-cube-per-tile, but roughly
   4–5x fewer GameObjects.

### Solvability

The recursive backtracker produces a **perfect maze**: every cell is visited exactly once
during the carve, and each carve links a visited cell to an unvisited one. That makes the
result a spanning tree over the cells — every cell reachable from every other cell, with
exactly one path between any two. So there is always a route from the spawn corner to the
centre landmark. This is guaranteed by construction, not by a post-generation check.

I also verified it empirically: the generator was ported line-for-line to Python and run
over ~400 mazes (sizes 2x2, 3x3, 15x15, 16x9, 5x40, 40x5, 2x60 across many seeds),
asserting on each one that (a) every cell got visited, (b) the outer ring is fully solid,
(c) breadth-first search reaches the centre tile from the spawn tile, and (d) *every* open
tile is in that same connected component. All passed.

---

## Assumptions made

- **Unity 6 LTS**, C#, 3D project. The code uses `FindFirstObjectByType` /
  `FindObjectsByType`, which need Unity 2022.2+. On older Unity these will not compile.
- **Either render pipeline works.** `MazeBootstrap.CreateMaterial` checks
  `GraphicsSettings.currentRenderPipeline` to see which pipeline is *active* — `null` means
  Built-in (`Standard` shader), otherwise URP or HDRP `Lit` — then sets whichever of
  `_BaseColor` / `_Color` that shader actually exposes. It checks the active pipeline
  rather than just probing shader names, because a Built-in project with the URP package
  merely installed would otherwise pick the URP shader and render everything magenta.
  If no shader resolves at all, it logs a warning and leaves primitives on their default
  material.
- **Either input backend works.** `SimpleInput` compiles against the new Input System when
  `ENABLE_INPUT_SYSTEM` is defined and the legacy manager is off, and against the old
  `Input` class otherwise. This matters because Unity 6 templates commonly ship with the
  new Input System, where legacy `Input.GetAxis` throws at runtime.
- **Existing camera is reused, not duplicated.** If the scene already has a camera, the
  bootstrap re-parents it onto the player instead of spawning a second one (two cameras
  would render on top of each other). If there is none, it creates one, adding an
  `AudioListener` only if the scene has none.
- **Folder layout:** scripts live in `Assets/Scripts/` with no assembly definition file,
  so they land in the default `Assembly-CSharp`. All types are in the `MazePrototype`
  namespace.
- **World origin.** The maze is built starting at world `(0, 0, 0)` and extends into
  `+X` / `+Z`. The bootstrap GameObject's own transform is ignored — moving it does not
  move the maze.
- **No gravity tricks.** The ground is flat; gravity exists only to keep the
  `CharacterController` grounded.

## Deliberately not included

Per the brief: no textures, no custom shaders, no menus, no win screen, no enemies,
no audio, no editor scripts, no scene file.

---

## What I could NOT verify

I do not have access to a Unity Editor or a C# compiler in this environment, so:

- **The scripts were never compiled.** I re-read all four files for syntax and API errors,
  and checked `Awake`/`Start` for null-reference risk, but there is no compiler output to
  point at. If something does fail to compile, it will be an API-name detail, not a
  logic error.
- **Nothing was ever run in Play mode.** Visual results — wall proportions, lighting
  brightness, mouse-look feel, movement speed — are educated guesses, not observations.
  `mouseSensitivity` in particular may need adjusting on the new Input System backend:
  it applies a fixed `0.05` scale to raw pixel deltas to roughly match the old Input
  Manager's feel.
- **Collision was not tested in-engine.** The `CharacterController` radius is
  `min(0.4, tileSize * 0.3)`, which is comfortably narrower than a `tileSize = 2`
  corridor, but it has not been walked through.
- **The generator logic WAS verified**, via the Python port described above — that part is
  tested, just not tested inside Unity.

### Null-reference review of `Awake` / `Start`

- `FirstPersonController.Awake` — `_controller` is guaranteed by `[RequireComponent]`.
  `cameraTransform` is auto-found via `GetComponentInChildren<Camera>()` and every later
  use is null-checked, so the controller still works with no camera at all.
- `MazeBootstrap.Start` — only calls `Build()`. `Build()` creates its own root object and
  generator before touching either. `tileSize` / `wallHeight` are clamped away from zero.
  `MazeGenerator` clamps its own dimensions, so a `0 x 0` maze in the Inspector cannot
  produce an empty array.
- `SimpleInput` — every `Keyboard.current` / `Mouse.current` access is null-guarded, which
  matters on machines with no attached device and in headless runs.

---
---

# Original repository README (LangChain Streamlit quickstart)

The content below belongs to the pre-existing project in this repository and is
unrelated to the maze prototype.

## 🦜🔗 Langchain - Quickstart App

Build your first LLM powered app with Langchain and Streamlit.

### Overview of the App

<img src="diagram.jpg" width="75%">

- Accepts input text (*e.g.* `What are the three key pieces of advice for learning how to code?`) as prompt input using Streamlit's `st.text_area()`, then assign this to the `text` variable.
- LLM model is called via `llm()` and it is applied on the prompt input `text` to generate a response via `llm(text)`

### Demo App

[![Streamlit App](https://static.streamlit.io/badges/streamlit_badge_black_white.svg)](https://langchain-quickstart.streamlit.app/)

### Get an OpenAI API key

You can get your own OpenAI API key by following the following instructions:
1. Go to https://platform.openai.com/account/api-keys.
2. Click on the `+ Create new secret key` button.
3. Next, enter an identifier name (optional) and click on the `Create secret key` button.

### Try out the app

Once the app is loaded, go ahead and enter your OpenAI API key and type a question in the text box and wait for a generated response.
