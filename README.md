# Wilderness Renewable — The Long Dark

![GitHub release](https://img.shields.io/github/v/release/NicoriciN89/CoalRespawnMod?label=release)
![License](https://img.shields.io/github/license/NicoriciN89/CoalRespawnMod)

A MelonLoader mod that makes wilderness resources renewable — coal, large sticks, small sticks, and raven feathers respawn over time at the locations where you originally found them.

## Features

- **Coal** — refills at coal spawner anchors in caves and/or mines
- **Large Sticks** — respawns at positions where sticks were previously found
- **Small Sticks** — same system as large sticks
- **Raven Feathers** — respawns at known feather locations

Each resource type is independently configurable.

## Respawn Presets

| Preset | In-game time |
|---|---|
| Daily | 1 day |
| Fast | 5 days *(default for sticks & feathers)* |
| Normal | 15 days *(default for coal)* |
| Slow | 30 days |
| Realistic | 60 days |

## Coal Location Options

| Option | Description |
|---|---|
| Caves only | Cave interiors |
| Mines only | Mine / tunnel scenes |
| Caves & Mines | Both *(default)* |
| Everywhere | All playable scenes |

## Settings

All settings are in **Options → Mod Settings → Wilderness Renewable**.

- Enable / disable each resource type
- Respawn timer preset per resource
- Min / max quantity per spawn
- Coal: scan radius (1–12 m) and respawn location

## Requirements

| Dependency | Version |
|---|---|
| [MelonLoader](https://melonwiki.xyz/) | 0.6+ |
| [ModSettings](https://www.nexusmods.com/thelongdark/mods/107) | 1.9.0+ |
| [ModData](https://www.nexusmods.com/thelongdark/mods/106) | 1.5.x+ |

## Installation

1. Install MelonLoader, ModSettings, and ModData
2. Drop `WildernessRenewableMod.dll` into `The Long Dark/Mods/`
3. Launch the game — settings appear under **Options → Mod Settings**

## Building from Source

```powershell
cd CoalRespawnMod_Dev
.\build.ps1
```

Requires The Long Dark installed at `E:\games\TheLongDark` or adjust the `<GameDir>` path in `CoalRespawnMod.csproj`.

## How It Works

1. On scene load a coroutine starts and checks resources every 60 real seconds
2. When a resource spot is empty, the position and timestamp (in game-hours) are recorded
3. Once the configured number of in-game days has elapsed, items respawn at the original location
4. Coal uses the game's own `RadialObjectSpawner` anchors as fixed refill points
5. Sticks and feathers discover positions organically from live items in the scene
6. State is saved per scene per position via ModData — persists across saves and sessions

## Supported Languages

English · Russian · French · German · Spanish · Brazilian Portuguese · Polish · Czech · Turkish · Italian · Dutch · Japanese · Korean · Chinese (Simplified) · Chinese (Traditional) · Ukrainian · Swedish · Norwegian

## License

MIT
