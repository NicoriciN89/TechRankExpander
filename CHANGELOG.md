# Changelog

All notable changes to TechRankExpander are documented here.

## [1.9.4] — 2026-06-28

### Fixed — tech ranks not applied on game version 1.1.2a

In game v1.1.2a, `TechTreeManager.Awake()` was changed from `private` to `protected`.
Harmony's patch on `Awake` may not fire reliably when the method is `protected` and
overridden from a base class, causing `NumRanksHelper.Apply()` (which writes the
extended rank counts into `numRanks`) to never run — so all techs stayed at their
vanilla 1–3 rank caps despite the config showing the correct values.

Fix: `NumRanksHelper.Apply()` is now also called in the `Prefix` of
`Patch_TechTreeManager_Load`, which runs before save data is read on every map load.
This is idempotent (safe to call twice) and guarantees the extended ranks are in place
before any tech node data is loaded from the save file, regardless of whether the
`Awake` patch fires.

Thanks to **MatthewKnight** and **AdmiralPCK** on NexusMods for reporting and helping
diagnose this.

---

## [1.9.3] — 2026-06-25

### Fixed — three techs were never expanded (wrong internal names)

The mod matches each technology by its **internal name** (`TechTreeNodeData.GetTechName()`,
which returns the English tech name). Three config keys did not match the game's actual
internal names, so those techs were silently skipped — their rank caps stayed at the
vanilla maximum (1–3). Once a player maxed them, they could not be researched further
("stuck at 100%"). Verified by cross-checking every config key against the game's tech
list in `resources.assets` (`TechTree_TechNNN` localization terms).

- **`Dendrology` → `Silviculture`.** `Dendrology` was actually the name of the tech's
  *icon sprite* (`ICN_AG_Dendrology01`), not the tech. The real tech name is
  `Silviculture` (term `TechTree_Tech006`). This is the "tree growth" tech the bug
  report referenced.
- **`Horse Armor` → `Horse Barding`** (term `TechTree_Tech049`).
- **`Mortar-Reinforced Palisades` → `Reinforced Palisades`** (term `TechTree_Tech013`).

The remaining 68 config keys were already matching the game's internal names correctly.

**Config migration note:** after updating, the config file will create three new entries
at the default cap (20): `Ranks_Silviculture`, `Ranks_Horse_Barding`, and
`Ranks_Reinforced_Palisades`. The old entries (`Ranks_Dendrology`, `Ranks_Horse_Armor`,
`Ranks_Mortar_Reinforced_Palisades`) remain in the file but are now ignored — harmless,
but if you had customised the rank value for any of those three, set it again under the
new name.

Thanks to **MatthewKnight** on NexusMods for reporting this and correctly spotting that
the config and tech-tree names differed.

---

## [1.9.2] — 2026-06-01

### Fixed — wrong caps (found by decompiling Assembly-CSharp.dll)

- **Masonry cap raised from 5 → 15.**
  Previous cap was based on an incorrect assumption that `GE_BuildingMaterialsQtyModify`
  applies a *linear* −25 % per rank. Decompiled code reveals it is *compound* — each
  rank reduces the *current* brick quantity by 25 %, not the original. The formula also
  clamps to 0 min, so no rank can produce negative brick costs. At rank 15 brick
  requirements stabilise at 1–2 per building. Players who already purchased rank 5 will
  see their extra 10 ranks unlocked on next load (no KP refund needed).

- **Stonecutting cap lowered from 5 → 4.**
  At rank 5, `workUnitModifier = 1 − 5 × 0.20 = 0`. The game clamps via
  `Mathf.Max(0, CeilToInt(…))` so work units become exactly 0 (instant mining), not
  negative. Zero work time can overwhelm the task queue with back-to-back instant jobs.
  Cap at 4 leaves 20 % work time (a noticeable reduction without being instant).

- **Sustainable Farming default raised from 3 → 4 with a hard runtime clamp.**
  `GE_GenericGameEffect fertilityLossFromCropPlantings` uses
  `depletionAmount *= (1 + effectProportionTotal)`. The game does NOT clamp this.
  Rank 4 → multiplier = 0.0 → no fertility depletion (farms sustainable forever, safe).
  Rank 5+ → negative multiplier → fertility *restores* over time (infinite fertility,
  farming challenge disappears). A runtime clamp in `RefreshRuntimeConfig` now enforces
  max 4 and logs a warning if a higher value is detected in the config file.

- **All work-time cap comments corrected.**
  Previous comments said "makes crafting work *negative*". Decompiled code shows the
  game uses `Mathf.Max(0, …)`, so the true behaviour is "makes crafting *instant*
  (0 work units)". Caps are intentionally set one rank below the instant threshold so
  workers always have some non-zero work time remaining.

### Added — runtime protection mechanism

- **`Patch_GetWorkRateMultiplier_Clamp`** — new Harmony Postfix on
  `HappinessManager.GetWorkRateMultiplier(Villager)`.
  The per-villager work-rate formula is `happinessCurve + techBonus` with **no
  game-side clamp** on the total. A sufficiently negative `techBonus` (e.g.
  Civic Inspections rank 4+, or any future modded tech using
  `GE_OccupationWorkRate` with a large negative modifier) produces a negative
  work rate — workers freeze or behave erratically. This patch clamps the
  return value to a minimum of **0.01 (1 % speed)**, ensuring workers always
  have at least a token work rate no matter what configuration is loaded.

---

## [1.9.1] — 2026-06-01

### Fixed
- **Livestock double-multiply on reload** — `LivestockHerdSetupData` ScriptableObjects
  persist in memory across scene reloads, so their `numLivestockToBeOverpopulated` value
  was already multiplied from the previous load when `Start()` fired again. The multiplier
  was applied on top of an already-multiplied value each reload (8 → 16 → 32 → 64 …).
  Fixed by storing the original vanilla value in `_originalValues` (never cleared) and
  always computing the target from the vanilla base, so the result is always `vanilla × mult`
  regardless of how many times the map is loaded in one session.
- **Accumulated KP values not reset at load start** — `AccumulatedKpRefund` and
  `AccumulatedKpCost` were only reset at the end of `TechTreeManager.Load` (in Postfix).
  If the load was interrupted by an exception before Postfix ran, stale values would carry
  over to the next load and cause incorrect KP refunds or charges. Both values are now
  reset in the Prefix (at load start) so each load begins with a clean slate.
- **`InTechManagerLoad` flag could get stuck after a failed load** — same root cause as
  above; resetting accumulated values in Prefix also ensures the load state is
  re-initialized even if a previous load threw before Postfix could clean up.

---

## [1.9.0-beta] — 2026-05-21

### Changed
- **Favored Nation** is configurable again with a safe cap of 9 and a default
  cap of 1. The mod clamps the value to the safe range so trade pricing cannot
  go negative by accident.
- **Allot All Techs** no longer clears itself after the first load. It remains
  enabled until you turn it off manually in the config.
- Updated the release packaging and NexusMods description for the current build.

---

## [1.8.2] — 2026-05-05

### Changed
- **Favored Nation** capped at rank 9 and removed from the config file.
  Each rank reduces trading-post sell prices by −10%; rank 10 = −100% (zero
  gold from the bazaar); rank 11+ produces negative prices, breaking trade
  entirely. Any existing `Ranks_Favored_Nation` line in the config is silently
  ignored from this version onward.

---

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
