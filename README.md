# Coal Respawn Mod — The Long Dark

![GitHub release](https://img.shields.io/github/v/release/NicoriciN89/CoalRespawnMod?label=release)
![License](https://img.shields.io/github/license/NicoriciN89/CoalRespawnMod)

A MelonLoader mod that adds configurable options for coal deposit respawning — choose where deposits refill, how quickly, and how much coal appears.

## Features

- Coal deposits automatically refill after they've been emptied
- **Respawn time** — five presets:

  | Preset | In-game time |
  |---|---|
  | Daily | 1 day |
  | Fast | 5 days |
  | Normal | 15 days *(default)* |
  | Slow | 30 days |
  | Realistic | 60 days |

- **Respawn location** — choose which scenes are affected:

  | Option | Description |
  |---|---|
  | Caves only | Cave interiors |
  | Mines only | Mine / tunnel scenes |
  | Caves & Mines | Both *(default)* |
  | Everywhere | All playable scenes |

- Configurable min/max coal pieces per deposit (1–8)
- Configurable scan radius (1–12 m)
- Save-game aware — timers persist across saves, loads, and sessions
- Fully localised — 15 languages matching the base game

## Supported Languages

English · Russian · French · German · Spanish · Brazilian Portuguese · Polish · Czech · Turkish · Italian · Dutch · Japanese · Korean · Chinese Simplified · Chinese Traditional

## Requirements

| Dependency | Version |
|---|---|
| [MelonLoader](https://melonwiki.xyz/) | 0.6.x |
| [ModSettings](https://www.nexusmods.com/thelongdark/mods/107) | 1.9.0+ |
| [ModData](https://www.nexusmods.com/thelongdark/mods/?) | 1.5.x |

## Installation

1. Drop `CoalRespawnMod.dll` into `The Long Dark/Mods/`
2. Launch the game — settings appear under **Options → Mods → Coal Respawn**

## Building from Source

```powershell
cd CoalRespawnMod_Dev
dotnet build CoalRespawnMod.csproj --configuration Release
```

Requires the game to be installed at the path set in the `.csproj` HintPath entries, or adjust them accordingly.

## How It Works

1. A coroutine wakes every 60 seconds and scans all `RadialObjectSpawner` objects whose name contains `coal`
2. The current scene is checked against the **Respawn location** setting — if it doesn't match, the scan is skipped entirely
3. When no `GEAR_Coal` items are found within the scan radius, the spawner is marked *empty* with a timestamp (in game-hours)
4. Once the configured respawn time has elapsed, 2–4 coal pieces are spawned at random offsets inside the deposit
5. State is saved per-scene per-position via **ModData** — survives across sessions

## License

MIT
