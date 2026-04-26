# TechRankExpander

A [MelonLoader](https://melonloader.com/) mod for **Farthest Frontier v1.1.0** that extends the tech tree: most technologies can now be researched up to **20 ranks** instead of the vanilla 1–3. Every limit and multiplier is fully configurable through a plain-text config file.

## Features

- **Extended tech ranks** — most techs support up to 20 ranks (some are capped lower to prevent broken math, see table below)
- **KP speed multiplier** — generate knowledge points faster (default: 5×)
- **KP Hotkey** — press a key (default: **F8**) to instantly add knowledge points in-game; amount is configurable
- **Carry capacity multiplier** — villagers carry more items per trip (default: 3×)
- **Work speed bonus** — each rank of *Production Management* gives +1 % work speed to all occupations (configurable)
- **Prereqs at rank 1** — dependent techs (e.g. Monument) unlock as soon as the first rank of their prerequisite is purchased
- **Buildings unlock at rank 1** — buildings tied to a tech are available immediately, not only after all ranks are bought
- **Reset Tech Tree** — set `Reset_Tech_Tree = true` in the config to refund all researched ranks back to KP on the next load
- **15 UI languages** for the Production Management tooltip

## Safe caps

Some techs reduce crafting or resource costs by a fixed % per rank. Going beyond the cap makes the value zero or negative, which breaks game logic:

| Tech | Cap | Effect per rank |
|------|:---:|----------------|
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

You can lower any cap in the config. Raising them above the listed values may produce unintended results.

## Requirements

- [MelonLoader](https://melonloader.com/) v0.6.1 or newer (**Mono** version)
- Farthest Frontier **v1.1.0**

## Installation

1. Install MelonLoader (Mono version) into the game folder.
2. Download the latest release zip.
3. Extract — drag the `Mods/` and `UserData/` folders into the game directory.
4. Launch the game. `UserData/TechRankExpander.cfg` will be created automatically with default values.

## Configuration

Edit `UserData/TechRankExpander.cfg` with any text editor. Changes take effect on the next map load.

```ini
KP_Speed_Multiplier = 5.0          # KP generation speed (1 = vanilla)
Carry_Capacity_Multiplier = 3.0    # villager carry weight (1 = vanilla)
Work_Speed_Per_Rank = 0.01         # +1 % per Production Management rank
Reset_Tech_Tree = false            # set true once to refund all ranks to KP
KP_Hotkey = F8                     # key to add KP instantly (UnityEngine.KeyCode name)
KP_Hotkey_Amount = 1               # how many KP to add per key press
Ranks_Steel_Tools = 9              # per-tech rank caps
Ranks_Metallurgy = 9
# ... one entry per tech
```

## Building from source

Requires the .NET SDK and a copy of Farthest Frontier v1.1.0 with MelonLoader installed.  
The `.csproj` references DLLs by relative path (`..\..\Farthest Frontier (Mono)\...`), so clone the repo into a folder **next to** the game directory:

```
games/
  Farthest Frontier (Mono)/   ← game installation
  ModProject/
    TechRankExpander/          ← this repo
```

```powershell
dotnet build -c Release
# Output: Farthest Frontier (Mono)/Mods/TechRankExpander.dll
```

## License

This project is released into the public domain under the [Unlicense](LICENSE).
