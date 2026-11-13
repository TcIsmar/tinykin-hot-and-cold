# Tinykin Hot & Cold

A quality-of-life mod for [Tinykin](https://store.steampowered.com/app/1599020/Tinykin/) that helps you find
the last few missing pollen and tinykin in a level with a toggleable "hot and cold" radar.
Once you've found 90% of both, it leads to everything that's left there, including artifacts and
unfinished quest steps. It narrows down the search without ever revealing exact locations.

> Status: early development. See [PLAN.md](PLAN.md).

## Building

Requirements:

- Tinykin (Steam, Windows)
- [BepInEx 5.4.23.x](https://github.com/BepInEx/BepInEx/releases) (win x64) installed into the game folder
- .NET SDK 8 or newer

Steps:

1. If Tinykin is not in the default Steam library, copy `LocalSettings.props.example` to
   `LocalSettings.props` and set `TinykinDir` to the folder that contains `Tinykin.exe`.
2. Run `dotnet build`.
   The plugin is copied to `<Tinykin>\BepInEx\plugins\TinykinHotCold\` automatically
   (turn this off with `-p:DeployToGame=false`).

Game assemblies are referenced from your local install and are never committed to this repository.
