# Coal Respawn Mod — The Long Dark

A MelonLoader mod that makes coal deposits in caves respawn after a configurable amount of time.

## Features

- Coal deposits (`RadialSpawn_coal`) automatically refill after they've been emptied
- **Five time presets** selectable from **Options → Mods → Coal Respawn**:
  - Daily — 1 in-game day
  - Fast — 5 in-game days
  - Normal — 15 in-game days *(default)*
  - Slow — 30 in-game days
  - Realistic — 60 in-game days
- **Respawn location** — choose where coal is allowed to respawn:
  - Caves only
  - Mines only
  - Caves & Mines *(default)*
  - Everywhere
- Configurable min/max coal pieces spawned per deposit
- Configurable scan radius
- Save-game aware — respawn timers persist across saves / loads / sessions
- Full localization support (15 languages matching the game)

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
