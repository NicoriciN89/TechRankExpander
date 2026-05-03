# Changelog

All notable changes to TechRankExpander are documented here.

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
