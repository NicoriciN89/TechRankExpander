# TechRankExpander

[![Release](https://img.shields.io/github/v/release/NicoriciN89/TechRankExpander?style=flat-square)](https://github.com/NicoriciN89/TechRankExpander/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/NicoriciN89/TechRankExpander/total?style=flat-square)](https://github.com/NicoriciN89/TechRankExpander/releases)
[![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue?style=flat-square)](LICENSE)
[![NexusMods](https://img.shields.io/badge/NexusMods-TechRankExpander-orange?style=flat-square)](https://www.nexusmods.com/farthestfrontier/mods/88)

A [MelonLoader](https://melonloader.com/) mod for **Farthest Frontier v1.1.0** that extends the tech tree beyond vanilla limits. Most technologies can be researched up to **20 ranks** instead of the vanilla 1–3, and every multiplier, cap, and hotkey is fully configurable through a plain-text config file — no code editing required.

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
| **Prereqs at rank 1** — dependent techs unlock after buying just 1 rank of a prerequisite | — |
| **Buildings unlock at rank 1** — buildings available immediately, not after full research | — |
| **Reset Tech Tree** — refund all researched ranks back to KP on next load (one-shot flag) | off |
| **15 UI languages** for the Production Management tooltip | auto |

---

## Safe Caps

Some techs reduce crafting time or costs by a fixed percentage per rank. Exceeding the cap produces zero or negative values, which breaks game logic. The mod enforces these limits automatically — any excess ranks are refunded as KP on load.

| Technology | Cap | Effect per rank |
|:-----------|:---:|:----------------|
| Steel Tools | 9 | −10 % crafting time |
| Metallurgy | 9 | −10 % crafting time |
| Military Logistics | 9 | −10 % crafting time |
| Production Logistics | 9 | −10 % crafting time |
| Spring Pole Lathe | 4 | −20 % crafting time |
| Stiff-Blade Saw | 4 | −20 % crafting time |
| Venting Chambers | 6 | −15 % crafting time |
| Stonecutting | 5 | −20 % mining time |
| Masonry | 5 | −20 % brick cost |
| Adjustable Shoe Lasts | 3 | −25 % crafting time |
| Sustainable Farming | 3 | −25 % fertility loss |
| Printing Press | 1 | −50 % crafting time |
| Favored Nation | 19 | −5 % export price |

> You can lower any cap in the config. Raising above these values may cause unintended results the game cannot recover from.

---

## Requirements

- **Farthest Frontier** v1.1.0
- **[MelonLoader](https://melonloader.com/)** v0.6.1 or newer — **Mono** version (not Il2Cpp)

---

## Installation

1. Install MelonLoader (Mono version) into the game folder.
2. Download **TechRankExpander_vX.X.X.zip** from the [latest release](https://github.com/NicoriciN89/TechRankExpander/releases/latest).
3. Extract the archive — drag the `Mods\` folder into the game directory.
4. Launch the game. `UserData\TechRankExpander.cfg` is created automatically on first run.

> **Updating:** just overwrite the DLL. Your existing config is preserved. Any ranks above the safe caps are refunded as KP automatically on the next load.

---

## Configuration

Edit `UserData\TechRankExpander.cfg` with any text editor. Changes take effect on the **next map load** — no restart needed.

```ini
KP_Speed_Multiplier       = 5.0    # KP generation speed (1 = vanilla)
Carry_Capacity_Multiplier = 3.0    # villager carry weight (1 = vanilla)
Work_Speed_Per_Rank       = 0.01   # +1 % work speed per Production Management rank
Reset_Tech_Tree           = false  # set true once to refund all ranks to KP
KP_Hotkey                 = F8     # key to add KP instantly (UnityEngine.KeyCode name)
KP_Hotkey_Amount          = 1      # KP added per key press

# Per-tech rank caps — one entry per technology:
Ranks_Steel_Tools         = 9
Ranks_Metallurgy          = 9
Ranks_Favored_Nation      = 19
# ... one line per tech (~71 total)
```

Valid `KP_Hotkey` values are [UnityEngine.KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) names, e.g. `F7`, `F9`, `Alpha1`, `Keypad0`.

---

## Building from Source

**Requirements:** .NET SDK, Farthest Frontier v1.1.0 with MelonLoader installed.

The `.csproj` references game and MelonLoader DLLs by relative path, so the repo must be cloned into a folder **next to** the game directory:

```
games/
├── Farthest Frontier (Mono)/    ← game installation
└── ModProject/
    └── TechRankExpander/
        └── _src/                ← clone here
```

```powershell
git clone https://github.com/NicoriciN89/TechRankExpander.git
cd TechRankExpander/_src
dotnet build -c Release
# Output: ../../Farthest Frontier (Mono)/Mods/TechRankExpander.dll
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for the full version history.

---

## License

Released into the public domain under the [Unlicense](LICENSE) — do whatever you want with it.
