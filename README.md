# TechRankExpander

[![Release](https://img.shields.io/github/v/release/NicoriciN89/TechRankExpander?style=flat-square)](https://github.com/NicoriciN89/TechRankExpander/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/NicoriciN89/TechRankExpander/total?style=flat-square)](https://github.com/NicoriciN89/TechRankExpander/releases)
[![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue?style=flat-square)](LICENSE)
[![NexusMods](https://img.shields.io/badge/NexusMods-TechRankExpander-orange?style=flat-square)](https://www.nexusmods.com/farthestfrontier/mods/88)

A [MelonLoader](https://melonloader.com/) mod for **Farthest Frontier** that extends the tech tree beyond vanilla limits. Most technologies can be researched up to **20 ranks** instead of the vanilla 1–3, and every multiplier, cap, and hotkey is fully configurable through a plain-text config file — no code editing required.

## Table of Contents

- [Features](#features)
- [Safe Caps](#safe-caps)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Building from Source](#building-from-source)
- [Changelog](#changelog)
- [License](#license)

---

## Features

| Feature | Default |
|---------|---------|
| **Extended tech ranks** — most techs up to 20 ranks | — |
| **KP speed multiplier** — generate Knowledge Points faster | 5× |
| **KP Hotkey** — press a key to add KP instantly in-game | F8 |
| **Carry capacity multiplier** — villagers carry more per trip | 3× |
| **Work speed bonus** — per rank of Production Management, all occupations get a bonus | +1 % / rank |
| **Livestock capacity multiplier** — increase pen capacity for all animals | 3× |
| **Prereqs at rank 1** — dependent techs unlock after buying just 1 rank of a prerequisite | — |
| **Buildings unlock at rank 1** — buildings available immediately, not after full research | — |
| **Deep Wells water volume** — each Deep Wells rank adds bonus capacity to wells | +50 / rank |
| **Deep Wells tooltip** — shows current / next water bonus in the tech description (15 languages) | — |
| **Reset Tech Tree** — refund all researched ranks back to KP on next load (one-shot flag) | off |
| **Allot All Techs** — fill every tech to its configured cap on each load until disabled | off |
| **Favored Nation** — default 1, safe maximum 9 if you raise it manually in cfg | 1 |
| **Stale config cleanup** — old keys from previous versions are removed automatically on load | — |
| **15 UI languages** for mod tooltips (Production Management, Deep Wells) | auto |

---

## Safe Caps

Some techs reduce crafting time, costs, or probabilities by a fixed percentage per rank. Exceeding the safe cap produces zero or negative values which breaks game logic. The mod enforces these limits automatically — any excess ranks are refunded as KP on load.

### Hardcoded caps (not configurable — changing these would break the game)

| Technology | Cap | Effect per rank | At cap |
|:-----------|:---:|:----------------|:-------|
| Civic Inspections | 3 | −30 % firefighter work time | −90 % |
| Sheet Composting | 3 | −30 % compost work time | −90 % |
| Hygiene | 4 | −25 % disease probability | −100 % |

> These caps cannot be raised via config. Rank 4+ for Civic Inspections / Sheet Composting gives negative work time (workers stop). Rank 5+ for Hygiene gives negative disease probability.

### Configurable caps (can be lowered or raised in config)

| Technology | Default cap | Effect per rank | Notes |
|:-----------|:-----------:|:----------------|:------|
| Favored Nation | 1 | −10 % bazaar sell price | Hard clamped to max 9 — rank 10 = zero gold, rank 11+ = negative prices |
| Horse Barding | 6 | −15 % cavalry speed | Game clamps penalty to <100 % (rank 7+ silently rejected); cap 6 = −90 % speed; +30 % health/rank is uncapped and safe |
| Steel Tools | 9 | −10 % firewood work time | Rank 10 = instant chop; cap keeps ≥10 % time |
| Military Logistics | 20 | −10 % military upkeep cost | Uses `GE_ExpenseTypeAmountModify` — reduces upkeep cost, not work time; no instant-craft risk; safe to 20 |
| Production Logistics | 9 | −10 % crafting time | Rank 10 = instant craft; cap keeps ≥10 % time |
| Metallurgy | 9 | −10 % smelting time | Rank 10 = instant smelt; cap keeps ≥10 % time |
| Venting Chambers | 6 | −15 % crafting time | Rank 7 = instant; cap keeps ≥10 % time |
| Spring Pole Lathe | 4 | −20 % crafting time | Rank 5 = instant; cap keeps ≥20 % time |
| Stiff-Blade Saw | 4 | −20 % crafting time | Rank 5 = instant; cap keeps ≥20 % time |
| Stonecutting | 4 | −20 % mining time | Rank 5 = instant; cap keeps ≥20 % time |
| Adjustable Shoe Lasts | 3 | −25 % crafting time | Rank 4 = instant; cap keeps ≥25 % time |
| Masonry | 15 | compound −25 % brick cost | **Compound** reduction per rank (not linear); stabilises at 1–2 bricks; game clamps to 0 |
| Sustainable Farming | 3 | compound −25 % fertility loss | Hard clamped to max 3 — rank 4 = 0 % loss (edge); rank 5+ = fertility restores (infinite fertility) |
| Pharmaceutical Study | 1 (vanilla) | −50 % work / +100 % shelf life | Rank 2 = instant craft (0 work time); vanilla cap kept — not configurable |
| Variolation | 1 (vanilla) | −50 % smallpox chance | Rank 2 = −100 % (no smallpox at all); no further effect exists; vanilla cap kept |
| Printing Press | 1 | −50 % crafting time | Rank 2 = instant; cap keeps 50 % time |

> **Work-time reductions** (Steel Tools, Military Logistics, etc.) use `GE_ManufacturingWorkModify`. The game applies `Mathf.Max(0, …)` internally, so work time reaches 0 (instant) rather than going negative. The caps above prevent instant crafting, which can overwhelm the task queue.
>
> **Masonry** uses `GE_BuildingMaterialsQtyModify` with a *compound* formula — each rank reduces the **current** brick count by 25 %, not the original. Previous documentation incorrectly described this as linear. The game also clamps brick cost to 0, so no rank can produce negative costs.
>
> **Horse Barding** uses `GE_MountedSoldierModify(Speed, −0.15)`. The game's `AddMovementPenalty` clamps the total penalty to <1.0 (100 %), so rank 7+ is silently ignored — horses never freeze or move backwards. Cap 6 is set to avoid wasting research points on no-effect ranks.
>
> **Sustainable Farming** and **Favored Nation** are the only configurable techs with hard runtime clamps. Exceeding their safe maximum breaks core game mechanics (infinite fertility / negative trade prices).

---

## Requirements

- **Farthest Frontier** (tested on v1.1.2a)
- **[MelonLoader](https://melonloader.com/)** v0.6.1 or newer — **Mono** version (not Il2Cpp)

---

## Installation

1. Install MelonLoader (Mono version) into the `Farthest Frontier (Mono)` folder.
2. Download **TechRankExpander_vX.X.X.zip** from the [latest release](https://github.com/NicoriciN89/TechRankExpander/releases/latest).
3. Open the `Farthest Frontier (Mono)` folder inside your game directory.
4. Place `TechRankExpander.dll` into the `Mods` subfolder:
   ```
   Farthest Frontier (Mono)\Mods\TechRankExpander.dll
   ```
5. Launch the game. `UserData\TechRankExpander.cfg` is created automatically on first run.

> **Updating:** just overwrite the DLL. Your existing config is preserved. Any ranks above the safe caps are refunded as KP automatically on the next load. Stale config keys from older versions are cleaned up automatically.

---

## Configuration

Edit `UserData\TechRankExpander.cfg` with any text editor. Changes take effect on the **next map load** — no restart needed.

```ini
KP_Speed_Multiplier              = 5.0   # KP generation speed (1 = vanilla)
Carry_Capacity_Multiplier        = 3.0   # villager carry weight (1 = vanilla)
Livestock_Capacity_Multiplier    = 3.0   # animal pen capacity (1 = vanilla)
Work_Speed_Per_Rank              = 0.01  # +1 % work speed per Production Management rank
Deep_Wells_Water_Volume_Per_Rank = 50    # bonus well capacity per Deep Wells rank (0 = off)
Reset_Tech_Tree                  = false # set true once to refund all ranks to KP
Allot_All_Techs                  = false # set true to fill all techs to cap on each load until disabled
KP_Hotkey                        = F8    # key to add KP instantly (UnityEngine.KeyCode name)
KP_Hotkey_Amount                 = 1     # KP added per key press

# Per-tech rank caps — one entry per technology:
Ranks_Favored_Nation          = 1
Ranks_Steel_Tools             = 9
Ranks_Military_Logistics      = 9
Ranks_Masonry                 = 15
Ranks_Printing_Press          = 1
Ranks_Sustainable_Farming     = 3
Ranks_Silviculture            = 20
Ranks_Horse_Barding           = 20
Ranks_Reinforced_Palisades    = 20
# ... one line per tech (~70 total)
# Note: Civic Inspections, Sheet Composting, and Hygiene are hardcoded
# and do NOT appear in the config.
```

Valid `KP_Hotkey` values are [UnityEngine.KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) names, e.g. `F7`, `F9`, `Alpha1`, `Keypad0`.

### Config migration from older versions

If you are upgrading from **v2.1.5 or earlier**, the following config keys were renamed:

| Old key (ignored) | New key |
|:------------------|:--------|
| `Ranks_Dendrology` | `Ranks_Silviculture` |
| `Ranks_Horse_Armor` | `Ranks_Horse_Barding` |
| `Ranks_Mortar_Reinforced_Palisades` | `Ranks_Reinforced_Palisades` |

The old keys are cleaned up automatically. If you had custom values for those three techs, re-enter them under the new names.

---

## Building from Source

**Requirements:** .NET SDK, Farthest Frontier with MelonLoader (Mono) installed.

The `.csproj` references game and MelonLoader DLLs by relative path, so the repo must be cloned into a `ModProject` folder **inside** `Farthest Frontier (Mono)`:

```
Farthest Frontier (Mono)/
├── Farthest Frontier_Data/
├── MelonLoader/
├── Mods/
└── ModProject/
    └── TechRankExpander/    ← clone here
        ├── Class1.cs
        ├── TechRankExpander.csproj
        └── ...
```

```powershell
cd "Farthest Frontier (Mono)"
mkdir ModProject
cd ModProject
git clone https://github.com/NicoriciN89/TechRankExpander.git
cd TechRankExpander
dotnet build -c Release
# Output: ..\..\Mods\TechRankExpander.dll
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for the full version history.

---

## License

Released into the public domain under the [Unlicense](LICENSE) — do whatever you want with it.
