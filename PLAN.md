# Tinykin Hot & Cold: Plan

A quality-of-life BepInEx mod for **Tinykin** (Steam, Unity 2020.3, Mono) that helps you find the last few
missing **pollen** and **tinykin** in a level, and then the rest of what's left to do there (artifacts,
the bubble, unfinished quest steps), with a toggleable "hot and cold" radar.

## Principles

- **Quality of life, not a cheat.** The radar narrows the search; the final find is always yours.
- **Never reveal an exact location.** No markers, icons through walls, distance numbers, item lists, maps or teleporting.
- **Read-only.** The mod never writes to the save file and never touches achievements.
- Hint strength (heat ranges, compass cutoff, unlock gate) is fixed in code. Only comfort settings are configurable:
  keys, HUD position and size, sound, rumble, compass on/off.
- Targets the current Steam build of Tinykin. Future game updates are out of scope for now.

## Features

### Targets

One radar and one shared bar that leads to the nearest of everything that's left (decided 2026-11-13;
switching between pollen and tinykin was dropped). The panel never says what it's leading to or how many
are left. When nothing is left it says so ("Nothing left to track here!").

| Target | Counts as remaining |
|---|---|
| Pollen | Loose pollen not yet collected; pollen still inside containers (breakable obstacles, tinykin interactions, soap bars). A box that pays out when your tinykin deliver an item to it (a letter to a mailbox) leads to the item instead, and back to the box while your tinykin carry it. A soap bar that only appears once something is done (every candle in the Bedroom lit) isn't led to until it's out, unless nothing else is left |
| Tinykin | Unhatched eggs; tinykin in unbroken cages |
| Collectibles | The level's artifacts and its bubble (the pause menu's "soap" counter), not yet collected. One that isn't out yet leads to what's left to do to earn it when the level wires that up; otherwise its spot is a last resort, and one that is handed to you as a reward is left to the quest step that earns it |
| Quest steps | Unfinished steps of quests you have started. Optional quests (lighting every candle) never save that you started them, so one counts as started once any of its steps is done. An objective that is complete in the game or in the save is skipped |

### Unlock gate

The radar unlocks in a level once **90% of the pollen and 90% of the tinykin** have been found there.
A level with no pollen or no tinykin only needs the other. Fixed, not configurable.

### Hot/Cold (default)

- Eight levels (Freezing, Cold, Chilly, Cool, Warm, Toasty, Hot, Burning) shown as a colored bar.
  From 75 units away down to 4, each level is about 1.6 times closer than the one before.
- A pulse and tick that speed up as you get closer.
- No distance numbers and no warmer/colder indicator: the bar alone is the hint (decided in playtesting).
- At Burning, a hint when the target is hidden inside something (break it, push it, use tinykin).
- The panel uses the game's own font and text style and shows only the bar and its level (or a short message).

### Compass (opt-in)

- Rough 8-way direction plus above/below while the target is far away.
- Switches itself off once you reach Warm, so the last part of the search is always yours.

### Controls

Shown and rebindable in the game's own Controls menu (saved in the mod's config, never the game's save;
the game's "reset controls" resets them too). Radar keys can't take a key the game's gameplay uses.
Ignored while the game is paused or in menus.

| Action | Keyboard | Controller |
|---|---|---|
| Radar on/off | F6 | D-pad Up |
| Compass on/off (Phase 4, if enabled in config) | F9 | D-pad Down |

F7/F8 and D-pad Left/Right (previous/next target) were removed with the shared bar.

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
Dev keys are F10 (census) and F11 (nearest distance) so they don't clash with the radar controls.

### Phase 2: Core radar

- [x] Target tracker (pollen + tinykin) and nearest-target query
- [x] Heat model (six levels, trend) and the 90% unlock gate
- [x] First HUD and controls
- [x] Playtest: panel placement, distance bands and tick speed all good (all 14 Living Room pollen found)
- [x] Remove the warmer/colder indicator (too much of a hint)
- [x] Style the panel with the game's font, text style and icons; add previous/next arrows
- [x] Clearer "radar locked" message below 90%
- [x] Radar keys in the game's Controls menu (view, rebind, reset)
- [x] Playtest the restyled panel: the Veranda taken to 100% pollen and 100% tinykin with the radar,
  including the tinykin side, the 90% lock message and switching targets
- [x] Radar rows appear in the Controls menu (main menu and pause menu, confirmed in the log)
- [ ] Try rebinding a radar key in the Controls menu
- [x] Finer bar: eight levels instead of six (Chilly and Toasty added), same Freezing and Burning edges
- [x] Playtest the eight-level bar (used to finish the game)
- [x] Delivery boxes (mailbox and letter): lead to the item to deliver, not the box; the census logs them as "delivery"
- [ ] Playtest a delivery box (the Kitchen's 10-pollen tinykin interaction)
- [x] One shared bar for everything: no target switching, no icon or count on the panel,
  unlocks at 90% of both pollen and tinykin, "Nothing left to track here!" when done
- [x] Also leads to artifacts, the bubble and unfinished quest steps (logged on radar on as "Can lead to: ...")
- [x] Playtest the shared bar, collectibles and quest steps (the game finished with it, 2026-11-13)
- [x] Hidden rewards lead to what reveals them, not their own spot (the Bedroom's soap bar only appears once
  all 10 candles are lit): when a trigger's events say "show this reward", the radar follows that trigger or
  quest (works for the Bedroom's thief artifact); when the level doesn't say (the candle soap bar), the reward's
  spot is only a last resort, used when nothing else is left. The census logs this as "REVEALED BY EVENTS"
- [x] Optional quests count as started once any of their steps is done (they never save their status)
- [x] Playtest it in the Bedroom (led to the last candle, not the soap bar's spot)

### Phase 3: Feedback

- [x] Visual pulse and tick sound
- [x] Rumble pulses at Warm and hotter (Xbox-compatible controllers)
- [ ] Verify rumble with an Xbox controller or Steam Input

### Phase 4: Compass

- [ ] Opt-in 8-way direction plus above/below, switching off at Warm

### Phase 5: Release

- [x] HUD styled like the game; documented config options
- [x] Strip development diagnostics (position logging) from release builds (`Dev/` is Debug-only)
- [x] Player README: install, uninstall, controls, Steam Deck launch option (plus building and how it works)
- [x] LICENSE (MIT, in the author's GitHub name)
- [x] Packaging: plugin-only zip (`TinykinHotCold-0.1.0.zip`, the DLL and the license)
- [ ] Packaging: a full pack that includes BepInEx (skipped for now; the README links BepInEx instead)
- [x] Showcase post in the Steam General Discussions with a highlight video (credits Claude)
- [x] GitHub repository (TcIsmar/tinykin-hot-and-cold), made public with a v0.1.0 release
- [ ] Nexus Mods page; post in the
  [Steam thread](https://steamcommunity.com/app/1599020/discussions/0/3432326055082924313) that asked for this feature
