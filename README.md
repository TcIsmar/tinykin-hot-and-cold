# Tinykin Hot & Cold

A quality-of-life mod for [Tinykin](https://store.steampowered.com/app/1599020/Tinykin/) that helps you find
the last few things you're missing in a level with a toggleable **hot and cold radar**.

You've explored the whole house, the counter says 1,586 of 1,600 pollen, and the last 14 could be anywhere in
a huge room. Turn the radar on and a bar tells you how warm you are: Freezing, Cold, Chilly, Cool, Warm, Toasty,
Hot, Burning. It never shows a marker, a distance or a map. The final find is always yours.

> **Status:** version 0.1.0. It did what it was made for: it helped its author finish Tinykin and track down
> the last pollen, tinykin and bonus quests in each level. Download it from the
> [Releases page](https://github.com/TcIsmar/tinykin-hot-and-cold/releases) and see [Installation](#installation).
> Development notes and history are in [PLAN.md](PLAN.md).

## Contents

- [Principles](#principles)
- [Features](#features)
- [Controls](#controls)
- [Configuration](#configuration)
- [Installation](#installation)
- [Uninstalling](#uninstalling)
- [Troubleshooting](#troubleshooting)
- [Building from source](#building-from-source)
- [How it works](#how-it-works)
- [Project layout](#project-layout)
- [Credits](#credits)
- [License](#license)

## Principles

- **Quality of life, not a cheat.** The radar narrows the search down; you still do the finding.
- **Never reveals an exact location.** No markers, icons through walls, distance numbers, item lists, maps or
  teleporting.
- **Unlocks late.** The radar only works in a level once you've found 90% of its pollen and tinykin on your own.
- **Read-only.** The mod never writes to your save file and never touches achievements.
- **Hint strength is fixed.** The heat ranges and the unlock rule are in the code, not the config file. Only
  comfort settings (keys, panel position and size, sound, rumble) can be changed.

## Features

### The radar

Press **F6** (or **D-pad Up** on a controller) in a level to turn the radar on or off.
A small panel appears at the top of the screen, drawn in the game's own font and style:

- **An eight-step colored bar:** Freezing, Cold, Chilly, Cool, Warm, Toasty, Hot, Burning. Each step is
  about 1.6 times closer than the one before it.
- **A tick** that speeds up as you get closer, with a pulse on the panel.
- **Controller rumble** pulses from Warm upwards, stronger the hotter it gets.
- **A hint at Burning:** "Something here is hiding it" when what you're after is inside or behind
  something. That could be pollen in a breakable, a tinykin in a cage, or a soap bar.

The bar always follows the **nearest** thing that's left, measured in a straight line from where you stand.
When you find something, the bar usually drops a few steps as it switches to the next nearest thing.
That's how you know you found what it was leading you to. Because it measures a straight line, something on the
floor above you can read Burning too.

The panel never says *what* it's leading you to or *how many* are left. It only says how warm you are.

### Unlocking

The radar unlocks separately in each level, once you've found **90% of the pollen and 90% of the tinykin**
there. A level with no pollen or no tinykin only needs the other. Until then the panel shows how far along you are:

```
Radar locked
Find 90% of pollen and tinykin
(pollen 85%, tinykin 92%)
```

### What it leads to

Once it's unlocked, the radar leads to the nearest of everything that's left in the level:

| What | Counts as left |
|---|---|
| **Pollen** | Loose pollen, and pollen still inside something: breakable objects, tinykin interactions, soap bars |
| **Tinykin** | Unhatched eggs and tinykin in unbroken cages |
| **Collectibles** | The level's artifacts and the item behind the pause menu's "soap" counter |
| **Quest steps** | Unfinished steps of quests you've started: the main quest, side quests and optional bonus quests (such as lighting every candle) |

It also handles a few special cases:

- **Deliveries.** Some rewards only pay out when your tinykin bring an item somewhere, like a letter to a
  mailbox. The radar leads to the item, not the box. While your tinykin are carrying it, the radar leads back
  to the box.
- **Rewards that haven't appeared yet.** A soap bar or artifact that only shows up once you've done something
  leads to what's left to do for it, like the last unlit candle. When the radar can't tell what reveals it,
  the reward's own spot is a last resort, used only when nothing else is left.
- **Rewards handed to you.** A collectible that appears on your character when you finish something has no
  meaningful spot of its own. The radar leads to the quest step that earns it instead.

When everything is done, the panel says **"Nothing left to track here!"**

### What it leaves out

- **Quests you haven't started.** The radar won't spoil a quest you haven't found. A main or side quest
  counts as started once the game says so. An optional quest counts as started once you've done one of its steps.
- **Races and achievements.** These aren't tracked.
- **Other levels.** The radar only looks at the level you're in.

## Controls

| Action | Keyboard | Controller |
|---|---|---|
| Radar on/off | F6 | D-pad Up |

- The radar's key appears in the game's own **Controls** menu (main menu and pause menu). You can rebind it there,
  and the game's "reset controls" resets it too.
- Your bindings are saved in the mod's config file, never in the game's save.
- Tinykin doesn't use the D-pad during play (only in menus), so D-pad Up is free.
- The radar ignores its key, and hides, while the game is paused, in menus, in dialogue and in cutscenes.

## Configuration

Settings are in `BepInEx\config\tinykin.hotandcold.cfg` in the game folder. The file is created the first time
the game starts with the mod. Edit it while the game is closed.

| Section | Setting | Default | What it does |
|---|---|---|---|
| `Controls` | `ToggleRadarKeyboard` | `<Keyboard>/f6` | Keyboard key that turns the radar on and off. Empty = unbound |
| `Controls` | `ToggleRadarGamepad` | `<Gamepad>/dpad/up` | Controller button that turns the radar on and off. Empty = unbound |
| `Feedback` | `Sound` | `true` | The tick that speeds up as you get closer |
| `Feedback` | `Rumble` | `true` | Short rumble pulses at Warm and hotter (also follows the game's vibration setting) |
| `HUD` | `Position` | `TopCenter` | `TopLeft`, `TopCenter`, `TopRight`, `BottomLeft`, `BottomCenter` or `BottomRight` |
| `HUD` | `Scale` | `1` | Panel size, from `0.5` to `2` |

Bindings use Unity Input System control paths, for example `<Keyboard>/f6` or `<Gamepad>/buttonWest`.
Rebinding in the Controls menu is easier.

## Installation

You need:

- **Tinykin** on Steam for Windows. The mod was made for the Steam build current in November 2026. A future
  game update could break it.
- **[BepInEx 5.4.23.x](https://github.com/BepInEx/BepInEx/releases)**, the Windows x64 version
  (`BepInEx_win_x64_5.4.23.x.zip`). BepInEx 6 is not supported.

Steps:

1. **Back up your save (recommended).** The mod never writes to it, but a backup costs nothing. Saves are in
   `%USERPROFILE%\AppData\LocalLow\tinyBuild Games\Tinykin`.
2. **Open the game folder.** In Steam, right-click Tinykin, then choose **Manage > Browse local files**.
3. **Install BepInEx.** Extract the BepInEx zip into that folder, so that `winhttp.dll` sits next to
   `Tinykin.exe`.
4. **Start the game once and quit.** BepInEx creates its folders, including `BepInEx\plugins`.
5. **Install the mod.** Download `TinykinHotCold-0.1.0.zip` from the
   [Releases page](https://github.com/TcIsmar/tinykin-hot-and-cold/releases) and extract it into the game
   folder. It puts `TinykinHotCold.dll` in `BepInEx\plugins\TinykinHotCold`.
6. **Try it.** Start the game, load a level and press F6.

Optional: to watch the mod's log while you play, set `Enabled = true` under `[Logging.Console]` in
`BepInEx\config\BepInEx.cfg`.

### Steam Deck and Linux

Not tested. BepInEx 5 usually runs under Proton when you set this as the game's launch option in Steam:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

### Controllers

- **D-pad:** works on any controller the game supports (tested with a DualShock 4).
- **Rumble:** goes through the game's own rumble, so it follows the game's vibration setting. A DualShock 4
  connected directly doesn't rumble this way. Turn on Steam Input for it in Steam's controller settings.
  Xbox-compatible controllers should rumble directly, but that hasn't been verified.

## Uninstalling

- **Remove the mod:** delete `BepInEx\plugins\TinykinHotCold` and `BepInEx\config\tinykin.hotandcold.cfg`.
- **Remove BepInEx as well:** delete the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`,
  `.doorstop_version` and `changelog.txt` from the game folder.

Uninstalling Tinykin in Steam leaves these files behind, so delete the game folder afterwards if you want
everything gone. Your save is untouched either way.

## Troubleshooting

**Nothing happens when I press F6.**

1. Check that you're in a level, not the main menu, and that the game isn't paused.
2. Open `BepInEx\LogOutput.log` in the game folder. It should contain `Tinykin Hot & Cold v0.1.0 loaded.`
3. If the log doesn't exist, BepInEx isn't running. Check that `winhttp.dll` is next to `Tinykin.exe`. On Steam
   Deck or Linux, also check the launch option above.

**The radar leads somewhere that seems empty.**
Each time you turn the radar on, the log gets a line like this:

```
Radar on. Can lead to: 1 pollen (CollectibleContainer, hidden), 1 quest step (Obstacle)
```

It lists what the radar can lead to by kind, without positions. Please include that line if you report a problem.

**It says "Can't locate the rest right now".**
The game's counter says something is left, but the radar can't find it in the level. Something may still have to
happen first, or it's something the radar doesn't track.

## Building from source

Requirements:

- Windows, with Tinykin and BepInEx 5.4.23.x installed (see [Installation](#installation)). The build
  references the game's, Unity's and BepInEx's assemblies from your own install. They are never copied into
  this repository.
- [.NET SDK](https://dotnet.microsoft.com/download) 8 or newer. The plugin targets .NET Framework 4.7.2. If
  the build complains about missing reference assemblies, install the .NET Framework 4.7.2 Developer Pack.

Steps:

1. **Point the build at your game, if needed.** If Tinykin isn't in
   `C:\Program Files (x86)\Steam\steamapps\common\Tinykin`, copy `LocalSettings.props.example` to
   `LocalSettings.props` (git-ignored) and set `TinykinDir` to the folder that contains `Tinykin.exe`. A
   `TinykinDir` environment variable works too.
2. **Build:**

   | Command | Result |
   |---|---|
   | `dotnet build -c Release` | The public build |
   | `dotnet build` | A Debug build with the development diagnostics described below |

   Either one copies `TinykinHotCold.dll` into `<Tinykin>\BepInEx\plugins\TinykinHotCold\` automatically. Add
   `-p:DeployToGame=false` to skip that. Close the game first, because it locks the plugin file.

The output is in `src\TinykinHotCold\bin\<Debug|Release>\net472\`.

### Development diagnostics (Debug builds only)

Debug builds log a **level census** when a level starts and whenever a counter changes. The census counts:

- remaining pollen by where it is (loose, in breakables, in soap bars, in deliveries)
- remaining tinykin in eggs and cages
- every quest with its type, status and steps
- the level's collectibles
- what reveals hidden rewards

It checks those counts against the game's own counters, so a mismatch shows up immediately. Two extra keys:

| Key | What it does |
|---|---|
| F10 | Logs a census now |
| F11 | Logs the distance to the nearest remaining pollen and tinykin |

The census logs counts and distances, never positions. None of this code is in Release builds.

## How it works

The mod is a [BepInEx 5](https://github.com/BepInEx/BepInEx) plugin. It reads the game's own objects and doesn't
patch any game code. Tinykin is a Unity 2020.3 game on the Mono backend, and its code is in
`Tinykin_Data\Managed\Game.dll`.

| Need | Game API |
|---|---|
| Player position, level state | `Game.PLAYER`, `Game.IsGameStarted`, `Game.INSTANCE.IsPaused` |
| Loose pollen | `Collectible` objects that are active and not `IsCollected` |
| Pollen inside things | `CollectibleContainer` with pollen left, owned by an `Obstacle`, an `SbirzTrigger` (tinykin interaction) or a `SoapCollectible` |
| Pollen counter (unlock rule) | `Game.INSTANCE.pollenCount` against `SceneData.GetAllPollenCount()` |
| Tinykin | `SbirzEgg` not `IsCollected`; `Cage` not `IsCompleted` |
| Tinykin counter (unlock rule) | `PlayerController.NewlyFoundSbirzCount` against `SceneData.TotalSbirzCount` |
| Artifacts and the soap counter | `UniqueCollectible` of type `Artifact` or `Bubble` |
| Quests | `Quest` (type, status, objectives) and its saved objective progress, read with `SaveGameSystem.LoadInt` |
| Rumble | `PlayerController.StartRumble` and `StopRumble` |
| Tick sound | The game's Wwise "contextual icon" UI event (Unity's own audio is turned off in this game) |

What happens while the radar is on:

- **Every 10 seconds** it collects the level's candidate objects. Tinykin loads a whole level at once, so
  everything is there.
- **Ten times a second** it finds the nearest one that's still left and turns the distance into a heat step.
  A small dead zone at each step's edge stops the bar from flickering.
- **Deliveries:** a tinykin interaction with required items (`SbirzTrigger.requiredObjects`) leads to items
  that haven't arrived yet (`SmallLiftable.ArrivedAtDestination`).
- **Hidden rewards:** the level wires "when this is done, show that reward" into a trigger's events. The mod
  reads those connections (persistent `UnityEvent` calls to `RareCollectible.Show`) and follows them to the
  trigger or quest. Some rewards are revealed by the game's visual scripting instead. Their spot becomes the last
  resort.
- **Quests:** an objective counts as done when either the live progress or the saved progress says so, because
  not every kind of step remembers being done after you reload. Optional quests never save their status, so one
  counts as started once any of its steps is done.

The panel is built with Unity UI and TextMeshPro, using the font and material of the game's HUD pollen counter.
The radar's key is an action in the mod's own Input System asset. A row for it is cloned into each tab of the
game's Controls menu, and a rebind there is saved to the mod's config. The action is only enabled during play,
because Unity can't rebind an enabled action.

## Project layout

```
src/TinykinHotCold/
  Plugin.cs                    BepInEx entry point
  RadarSettings.cs             Config file entries (comfort settings only)
  Radar/
    RadarController.cs         Level tracking, input, unlock rule, tick, rumble
    TargetScanner.cs           Finds what's left and which of it is nearest
    HeatModel.cs               Distance to heat step, tick speed
    FoundProgress.cs           Pollen and tinykin progress from the game's counters
    RadarHudView.cs            The panel
    GameUiAssets.cs            Borrows the game's HUD font and material
    UiSprites.cs               Rounded sprite for the panel and bar
    Enums.cs
  Input/RadarActions.cs        The radar's Input System action and its bindings
  Integration/ControlsMenuRows.cs   Rows in the game's Controls menu
  Dev/                         Debug-only diagnostics (census, F10/F11)
PLAN.md                        Design decisions, phases and playtest notes
```

## Credits

- **Mod:** designed and playtested by [TcIsmar](https://github.com/TcIsmar), and built with
  [Claude](https://claude.ai), Anthropic's AI model, in Claude Code. Claude read the game's code to find how
  it works, wrote the mod, and wrote this README.
- **Tinykin:** made by Splashteam and published by tinyBuild. This is an unofficial fan mod, not affiliated
  with or endorsed by either of them. It contains no game files.
- **[BepInEx](https://github.com/BepInEx/BepInEx):** the mod loader.

## License

[MIT](LICENSE)
