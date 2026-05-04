# Changelog

All notable changes to TechRankExpander are documented here.

## [1.8.1] — 2026-05-04

### Changed
- **Hygiene** capped at rank 4 and removed from the config file.
  Each rank reduces disease probability by −25%; rank 4 = −100% (fully eliminated).
  Rank 5+ would produce negative probability values, potentially breaking the
  disease-spread mechanic. Any existing `Ranks_Hygiene` line in the config is
  silently ignored from this version onward.

---

## [1.8.0] — 2026-05-05

### Changed
- **Performance optimisation** — eliminated all O(n) linear `List.Find()` scans
  on the tech-node list that were executing on every prereq check, every rank
  purchase, every tooltip open, and on every well that loads.
  - Added `TechCache` (built once in `TechTreeManager.Awake`): two dictionaries
    keyed by tech ID and by tech name so any lookup is now O(1).
  - `ArePrereqNodesActive` (the hottest path — called for every tech node on the
    UI) now uses `TechCache.ById` instead of iterating the whole list each call.
  - `ActivateTechOrRank`, `GetTechTreeNodeDescription` (Production Management and
    Deep Wells tooltips), `WorkSpeedHelper`, and `Patch_WellStart` all use the
    same cache.
  - Deep Wells tooltip now caches the tech ID on first call (like the Wax tooltip
    already did) so subsequent calls skip the name lookup entirely.
  - `GE_ManufacturingSourceItemModify.UpdateItemDef` prefix now caches the
    `FieldInfo` objects for `itemName` and `manuDef` instead of going through
    `HarmonyLib.Traverse` on every single wax-barrel update call.
  - `Patch_VillagerCarryCapacity` exits immediately when the multiplier is 1.0
    (no-op config), saving a float multiply per villager per capacity query.

---

## [1.7.4] — 2026-05-04

### Added
- **Deep Wells tooltip** — the tech description now shows the water capacity bonus
  added per rank (and the total at max rank), in the player's UI language (15 languages).
  Example at rank 5 with default config: `Well capacity: +250 (current) / +300 per rank (next rank)`.

---

## [1.7.3] — 2026-05-04

### Changed
- **Sheet Composting** removed from the config file. The cap of 3 is now hardcoded
  in code (same reason as Civic Inspections: each rank is −30% work time; rank 4+
  produces negative work time, breaking the Compost Yard). Any existing
  `Ranks_Sheet_Composting` line in a user's config is silently ignored.

---

## [1.7.2] — 2026-05-04

### Changed
- **Civic Inspections** removed from the config file entirely. The cap of 3 is now
  hardcoded in code and cannot be changed by users. This prevents the rank 4+
  bug (negative firefighter work time) from ever being triggered accidentally.
  Any existing `Ranks_Civic_Inspections` line in a user's config file is silently
  ignored.

---

## [1.7.1] — 2026-05-04

### Fixed
- **Civic Inspections** cap lowered from 20 to **3**. Each rank reduces firefighter
  work time by −30%; rank 4 would produce −120% (negative work time), causing
  firefighters to stop working entirely. Rank 3 = −90% (10% work time remaining)
  is the safe maximum.

---

## [1.7.0] — 2026-05-04

### Added
- **Deep Wells — bonus water volume per rank** — new config entry
  `Deep_Wells_Water_Volume_Per_Rank` (default: 50). Every well gains
  `rank × bonus` extra water capacity when the map loads.
  Example: 5 ranks × 50 = +250 capacity (basic well 100 → 350,
  upgraded well 300 → 550). Set to 0 to disable.
- **Sheet Composting — configurable ranks** — added to the tech rank
  table with a default of 3 ranks. Each rank gives +1 worker slot and
  -30% compost production work at the Compost Yard. Config key:
  `Ranks_Sheet_Composting` (default: 3; capped at 3 to keep work
  time ≥ 10%, raise at your own risk).

---

## [1.6.0] — 2026-05-04

### Added
- **Allot All Techs** — set `Allot_All_Techs = true` in the config, load your save
  once, and every technology is automatically filled to its configured rank cap,
  spending KP in the process. The flag clears itself automatically after the first
  load. This is the exact opposite of `Reset_Tech_Tree`.

---

## [1.5.0] — 2026-05-03

### Fixed
- **Academy stops at 152 KP** — `GetNumRanks()` is a trivial one-liner that the
  Mono JIT inlines in `GetNumKnowledgePointsRemaining()` and `ActivateTechOrRank()`,
  bypassing the Harmony Postfix patch. As a result those methods used the original
  vanilla rank counts (~152 total), causing the Academy to stop generating KP once
  the player had 152 unspent knowledge points. The fix writes the configured rank
  value directly into the private `numRanks` field on every `TechTreeNodeData`
  instance during `TechTreeManager.Awake()`, so all code paths (inlined or not) see
  the extended counts.

---

## [1.4.1] — 2026-04-28

### Added
- Russian translations for all config entry descriptions (bilingual EN / RU in the config file).

---

## [1.4.0] — 2026-04-26

### Added
- **BarrelWaxFix merged** — wax cost for Wax-Sealed Barrels is now capped at a configurable value (`Max_Wax_Per_Barrel`, default: 2). Previously the cap was hardcoded to 1 and used a different patch method (`Activate`). The new approach patches `UpdateItemDef` (same as the standalone BarrelWaxFix mod) and supports any cap value, including 0 to remove wax from the recipe entirely.
- The Wax-Sealed Barrels tech tooltip now shows the active wax cap in the player's UI language.

### Changed
- Replaced internal `Patch_GE_ManufacturingSourceItemModify_Activate` with `Patch_GE_ManufacturingSourceItemModify_UpdateItemDef` for wax cap logic — more flexible and consistent with BarrelWaxFix behavior.
- BarrelWaxFix is no longer needed as a separate mod.

---

## [1.3.0] — 2026-04-26

### Added
- **KP Hotkey** — press a configurable key (default: **F8**) in-game to instantly add knowledge points. Amount is also configurable (`KP_Hotkey_Amount`, default: 1). Both settings are visible in MelonPreferencesManager.

---

## [1.2.0] — 2026-04-25

### Added
- **Reset Tech Tree** — set `Reset_Tech_Tree = true` in the config, load your save once, and all researched tech ranks are refunded to your KP pool. The flag clears itself automatically after the first load.

### Fixed
- **Fully-researched techs blocked** — technologies already at max vanilla rank (state `Active`) now correctly allow purchasing additional ranks after the mod is installed. Previously Iron Shares 1/1, Military Logistics 2/2, Hygiene 1/1, etc. were stuck and could not be upgraded.
- **Favored Nation** cap set to **19** (−5 % trade price per rank; rank 20 = −100 % = zero export prices, breaking trade entirely).
- **Wax-Sealed Barrels** wax requirement stays at **1** regardless of rank count. Previously each additional rank added +1 wax needed, making higher ranks require absurd amounts of wax.
- **Auto-clamp on load** — if a save has more ranks than the current config cap, the excess ranks are automatically refunded as KP on the next load (no manual reset needed).

### Changed
- Version bumped to 1.2.0.

---

## [1.1.0] — 2026-04-24

### Fixed
- **Masonry / Monument unlock bug** — confirming a Masonry rank purchase no longer leaves the tech tree in a broken state. Previously, `UpdatePrereqNodes(force: false)` only re-evaluated `Locked` techs; dependent techs stuck in `Unlocked` state (e.g. Monument) were never promoted to `PrereqsMet`. Changed to `force: true` across all confirm code paths.
- After confirming any KP purchase (`OnKpsUsedConfirm`, `OnConfirmCachedChanges`), all tech prereqs are now fully re-evaluated so newly purchasable techs appear immediately.
- **Metallurgy** rank cap lowered from 20 to **9** (−10 % crafting time per rank; rank 10+ produces zero/negative work time).

### Changed
- Version bumped to 1.1.0.

---

## [1.0.0] — initial release

- Extended tech ranks (up to 20) for ~70 technologies.
- Configurable KP speed multiplier, carry capacity multiplier, work speed bonus.
- Prerequisites unlock at rank 1 instead of requiring full completion.
- Buildings tied to a tech unlock at rank 1.
- Work speed bonus for Production Management (all occupations).
- Multilingual Production Management tooltip (15 languages).
- Safe caps on techs with percentage-reduction effects (Steel Tools, Spring Pole Lathe, Masonry, etc.).
