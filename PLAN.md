# Tinykin Hot & Cold: Plan

A quality-of-life BepInEx mod for **Tinykin** (Steam, Unity 2020.3, Mono) that helps you find the last few
missing **pollen** and **tinykin** in a level with a toggleable "hot and cold" radar.

## Principles

- **Quality of life, not a cheat.** The radar narrows the search; the final find is always yours.
- **Never reveal an exact location.** No markers, icons through walls, distance numbers, item lists, maps or teleporting.
- **Read-only.** The mod never writes to the save file and never touches achievements.
- Hint strength (heat ranges, compass cutoff, unlock gate) is fixed in code. Only comfort settings are configurable:
  keys, HUD position and size, sound, rumble, compass on/off.
- Targets the current Steam build of Tinykin. Future game updates are out of scope for now.

## Features

### Targets

One radar with two target types that you switch between in-game. It starts on whichever one is unfinished
in the current level.

| Target | Counts as remaining |
|---|---|
| Pollen | Loose pollen not yet collected; pollen still inside containers (breakable obstacles, tinykin interactions, soap bars) |
| Tinykin | Unhatched eggs; tinykin in unbroken cages |

### Unlock gate

The radar for a target type unlocks in a level once **90%** of that type has been found there.
Fixed, not configurable.

### Hot/Cold (default)

- Six coarse levels (Freezing, Cold, Cool, Warm, Hot, Burning) shown as a colored bar.
- A warmer/colder trend and a pulse that speeds up as you get closer.
- No distance numbers.
- At Burning, a hint when the target is hidden inside something (break it, push it, use tinykin).

### Compass (opt-in)

- Rough 8-way direction plus above/below while the target is far away.
- Switches itself off once you reach Warm, so the last part of the search is always yours.

### Controls

Rebindable. Ignored while the game is paused or in menus.

| Action | Keyboard | Controller |
|---|---|---|
| Radar on/off | F6 | D-pad Up |
| Switch Pollen/Tinykin | F7 | D-pad Left/Right |
| Compass on/off (if enabled in config) | F8 | D-pad Down |

Tinykin binds no gameplay action to the D-pad (it only navigates menus), so it is free during play.

### Feedback

- Visual pulse (always on).
- Controller rumble pulses through the game's own rumble, which respects the in-game vibration setting.
- Audio, best effort: reuse a game sound as a Geiger tick, else our own beep if Unity audio is enabled, else none.

## Technical approach

- **Loader:** BepInEx 5.4.23.6 (win x64) with HarmonyX. The plugin targets `net472`.
- **Game code:** `Tinykin_Data/Managed/Game.dll`, decompiled locally with `ilspycmd` into the git-ignored `.decompiled/`.
- **Key game APIs** (verified by reading the game code):

| Need | Game API |
|---|---|
| Player position | `Game.PLAYER` |
| Level running / paused | `Game.IsGameStarted`, `Game.INSTANCE.IsPaused` |
| Loose pollen | Active `Collectible` objects with `!IsCollected` (collected pollen get deactivated) |
| Pollen in containers | `CollectibleContainer` with `!IsEmpty` (owned by `Obstacle`, `SbirzTrigger`, `SoapCollectible`) |
| Pollen totals | `SceneData.GetAllPollenCount()` vs `Game.INSTANCE.pollenCount` |
| Eggs | `Game.INSTANCE.GetAllSbirz()` with `!IsCollected` (found eggs stay active) |
| Cages | `Cage` (`amount` tinykin; broken once `IsCompleted`) |
| Tinykin totals | `SceneData.TotalSbirzCount` vs `PlayerController.NewlyFoundSbirzCount` |
| Rumble | `PlayerController.StartRumble(float)` / `StopRumble()` |
| Sounds | `Game.PostWwiseEvent(...)` with existing Wwise events |

The game ships a developer `DebugTools` component (pollen icons, but also achievement and quest cheats).
The mod never enables it.

## Phases

### Phase 0: Setup

- [x] Back up the save folder (`Documents\TinykinSaveBackups\2026-11-10_171723-before-bepinex`)
- [x] Install BepInEx 5.4.23.6 x64 into the game folder (console window enabled for development)
- [x] Install the .NET 10 SDK and ilspycmd; decompile the game code into `.decompiled/`
- [x] Scaffold the project with a build step that deploys to `BepInEx/plugins`
- [x] Local git repository (no remote yet)
- [x] In-game smoke test: the plugin loads and logs scene changes

### Phase 1: Verification spike

Debug-only diagnostics (`src/TinykinHotCold/Dev/`, excluded from Release builds):
F6 / D-pad Up = census, F7 / D-pad Left or Right = nearest distance, F8 / D-pad Down = sound + rumble test.

- [x] Log remaining pollen (loose + containers) and tinykin (eggs + cages) per level
- [x] Reconcile those numbers with the game's own counters on a real save
  (Living Room: pollen 14 = 2 loose + 12 in 2 touch containers; tinykin 0; both MATCH)
- [x] Confirm the whole level is loaded at once
- [x] Confirm keyboard and controller input through the Input System (F-keys, DualShock 4 D-pad)
- [x] Try playing a game sound (Wwise) and a Unity sound
  (Unity audio is disabled in this game; tick = Wwise "UI contextual icon", picked in playtesting)

Findings: rumble through the game's `StartRumble` does nothing on a DualShock 4, so rumble is supported
for Xbox-compatible controllers and PlayStation players are pointed to Steam Input.
Dev keys moved to F9 (census) and F10 (nearest distance) so they don't clash with the radar controls.

### Phase 2: Core radar

- [x] Target tracker (pollen + tinykin) and nearest-target query
- [x] Heat model (six levels, trend) and the 90% unlock gate
- [x] First HUD and controls
- [ ] Playtest: panel placement, distance bands, tick speed, trend responsiveness

### Phase 3: Feedback

- [x] Visual pulse and tick sound
- [x] Rumble pulses at Warm and hotter (Xbox-compatible controllers)
- [ ] Verify rumble with an Xbox controller or Steam Input

### Phase 4: Compass

- [ ] Opt-in 8-way direction plus above/below, switching off at Warm

### Phase 5: Release

- [ ] HUD styled like the game; documented config options
- [ ] Strip development diagnostics (position logging) from release builds
- [ ] Player README: install, uninstall, controls, Steam Deck launch option
- [ ] LICENSE (MIT; confirm the copyright name)
- [ ] Packaging: plugin-only zip and a full pack that includes BepInEx
- [ ] GitHub repository and Nexus Mods page; post in the
  [Steam thread](https://steamcommunity.com/app/1599020/discussions/0/3432326055082924313) that asked for this feature
