using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(TechRankExpanderMod.TechRankExpander), "TechRankExpander", "2.2.4", "NicoriciN")]
[assembly: MelonGame("Crate Entertainment", "Farthest Frontier")]

namespace TechRankExpanderMod
{
    internal static class TechDefaults
    {
        // ── Cap rationale ──────────────────────────────────────────────────────
        // Work-time techs (GE_ManufacturingWorkModify): each rank subtracts from
        // workUnitModifier (starts at 1.0). The game uses Mathf.Max(0, CeilToInt(...))
        // so work units are clamped to 0 — NOT negative. Zero means instant production.
        // Caps are set so the modifier stays > 0 (some work time remains).
        //
        // Brick-cost tech (GE_BuildingMaterialsQtyModify / Masonry): COMPOUND reduction —
        // each rank cuts the *current* qty by 25%, not the original. Formula is clamped
        // to 0 by the game. Cap 15 is safe; the curve stabilises at 1–2 bricks before
        // that point. Previous cap of 5 was based on an incorrect linear assumption.
        //
        // Fertility tech (Sustainable Farming): depletionAmount *= (1 + total). NOT
        // clamped by the game. Rank 4 = multiplier 0.0 → no fertility loss (safe).
        // Rank 5+ = negative multiplier → fertility restores (infinite fertility).
        // Clamped to max 4 in RefreshRuntimeConfig.
        //
        // Occupation work-rate (GE_OccupationWorkRate): final = happinessCurve + bonus,
        // NO game-side clamp. Negative bonus → workers freeze. Civic Inspections and
        // Sheet Composting use negative modifiers and are hardcoded to 3 below.
        // Runtime guard Patch_GetWorkRateMultiplier_Clamp enforces min 0.01 on every
        // GetWorkRateMultiplier(Villager) call as a belt-and-suspenders safety net.
        // ──────────────────────────────────────────────────────────────────────
        internal static readonly Dictionary<string, int> DefaultRanks = new Dictionary<string, int>
        {
            // ── Vanilla 2-rank techs → expanded to 20 ────────────────────────
            { "Vermicast",                       20 },
            { "Command Structure",               20 },
            { "Taxation",                        20 },
            { "Alcohol Sterilization",           20 },
            { "Military Logistics",               9 },  // −10% work/rank; rank 10 = instant; cap keeps ≥10% work time
            { "Scientific Discovery",            20 },
            { "Sustainable Forestry",            20 },
            { "Selective Breeding: Grains",      20 },
            { "Selective Breeding: Non-Grain",   20 },
            { "Metallurgy",                       9 },  // −10% work/rank; rank 10 = instant; cap keeps ≥10% work time
            { "Architecture",                    20 },
            { "Soldier Training",                20 },
            { "Scientific Method",               20 },
            { "Production Logistics",             9 },  // −10% work/rank; rank 10 = instant; cap keeps ≥10% work time
            { "Disease Resistance",              20 },
            { "Artificial Selection",            20 },
            { "Defensive Barricades",            20 },
            { "Double-Walled Hives",             20 },
            { "Animal Rearing",                  20 },
            { "Glass Recycling",                 20 },
            { "Steel Armaments",                 20 },
            { "Structural Engineering",          20 },
            { "Drought Tolerance",               20 },
            { "Dendrology",                      20 },  // display name "Silviculture"; GetTechName() returns "Dendrology"

            // ── Vanilla 3-rank techs → expanded to 20 ────────────────────────
            { "Woodlore",                        20 },
            { "Beautification",                  20 },
            { "Deep Mine Ventilation",           20 },
            { "Deep Wells",                      20 },

            // ── Vanilla 1-rank techs with scaleable % effect → expanded ──────
            // These apply a repeatable bonus per rank; safe caps where needed.
            { "Sustainable Farming",              3 },  // compound −25% fertility loss/rank; rank 3 = 25% loss (safe margin); rank 4 = 0% loss (edge); rank 5+ = fertility restored → clamped to 3
            { "Production Management",           20 },
            { "Marksman Training",               20 },
            { "Rehabilitation",                  20 },
            // Pharmaceutical Study: −50% work/rank; rank 2 = instant craft — NOT listed, vanilla cap kept.
            { "Favored Nation",                   1 },  // −10% sell price/rank; rank 10 = 0 gold; rank 11+ = negative prices → clamped to max 9
            { "Steel Tools",                      9 },  // −10% work/rank; rank 10 = instant; cap keeps ≥10% work time
            // Variolation: −50% smallpox/rank; rank 2 = −100% (no further effect) — NOT listed, vanilla cap kept.
            { "Masonry",                         15 },  // compound −25% bricks/rank (current qty); stabilises at 1–2 bricks
            // "Civic Inspections" hardcoded to 3 — NOT configurable.
            { "Horse Armor",                      6 },  // display name "Horse Barding"; −15% speed/rank via GE_MountedSoldierModify(Speed, −0.15); game clamps penalty to <100% so no crash, but rank 7+ (≥105%) is silently rejected — cap 6 = −90% speed (near-stationary); +30% health/rank (MaxLife) is uncapped and safe
            { "Wheel-Lock Crossbow",             20 },
            { "Printing Press",                   1 },  // −50% work/rank; rank 2 = instant; cap keeps 50% work time
            { "Fire Assaying",                   20 },
            { "Spring Pole Lathe",                4 },  // −20% work/rank; rank 5 = instant
            { "Treadwheel Crane",                20 },
            { "Iron-Rimmed Wheels",              20 },
            { "Steel Surgical Tools",            20 },
            { "Midwives",                        20 },
            { "Ratting Dogs",                    20 },
            { "Fortification Engineering",       20 },
            { "Tower Shields",                   20 },
            { "Cast-Iron Axe Blades",            20 },
            { "Adjustable Shoe Lasts",            3 },  // −25% work/rank; rank 4 = instant
            { "Spindlewick Production",          20 },
            { "Wax-Sealed Barrels",              20 },
            { "Stiff-Blade Saw",                  4 },  // −20% work/rank; rank 5 = instant
            { "Heavy Freight Wagons",            20 },
            { "Foothold Traps",                  20 },
            { "Mortar-Reinforced Palisades",     20 },  // display name "Reinforced Palisades"; GetTechName() returns "Mortar-Reinforced Palisades"
            { "Trailblazing",                    20 },
            // Hygiene capped at 4 in code — not configurable.
            { "Spotters",                        20 },
            { "Militia",                         20 },
            { "Natural Philosophy",              20 },
            { "Sustainable Fishing",             20 },
            { "Venting Chambers",                 6 },  // −15% work/rank; rank 7 = instant
            { "Stonecutting",                     4 },  // −20% work/rank; rank 5 = instant
            { "Iron Shares",                     20 },
            { "Border Policies",                 20 },
            { "Heat-Treated Halberds",           20 },
            { "Advanced Metal-Casting",          20 },  // GE_HeavyToolBonusModify — +tool/heavy tool health per rank; no inversion; safe to 20
            // "Sheet Composting" hardcoded to 3 — NOT configurable.
            // Unlock-only techs (open a building, no repeatable bonus) are NOT listed here.
            // Mod leaves their vanilla numRanks completely untouched.
        };
    }

    internal static class RuntimeConfig
    {
        internal static Dictionary<string, int> ActiveRanks = new Dictionary<string, int>();
        internal static float KpSpeedMultiplier = 5f;
        internal static float CarryCapacityMultiplier = 3f;
        internal static float WorkSpeedPerRank = 0.01f;  // +1% work speed per rank of Production Management
        internal static bool ResetTechTree = false;
        internal static bool AllotAllTechs = false;
        internal static int DeepWellsWaterVolumePerRank = 50;
        internal static KeyCode KpHotkey = KeyCode.F8;
        internal static int KpHotkeyAmount = 1;
        internal static int MaxWaxPerBarrel = 2;
        internal static float LivestockCapacityMultiplier = 2f;
    }

    // ── Tech lookup cache ──────────────────────────────────────────────────────────────────
    // Built once in TechTreeManagerAwake so all patches can do O(1) lookups
    // instead of O(n) List.Find() scans every time.
    internal static class TechCache
    {
        internal static readonly Dictionary<int,    TechTreeNodeData> ById   = new Dictionary<int,    TechTreeNodeData>();
        internal static readonly Dictionary<string, TechTreeNodeData> ByName = new Dictionary<string, TechTreeNodeData>();

        internal static void Build(TechTreeManager ttm)
        {
            ById.Clear();
            ByName.Clear();
            if (ttm?.techTreeNodeData == null) return;
            foreach (var t in ttm.techTreeNodeData)
            {
                ById[t.GetId()]        = t;
                ByName[t.GetTechName()] = t;
            }
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: UISubWidgetLivestockControls — show mod max on herd-size slider ────────
    // Appends "(mod max N)" to the slider counter text whenever the herd-size
    // panel is opened or the slider value changes, so players can see the new cap.
    [HarmonyPatch(typeof(UISubWidgetLivestockControls), "OnChangeHerdSize")]
    internal static class Patch_UILivestock_OnChangeHerdSize
    {
        static void Postfix(UISubWidgetLivestockControls __instance) =>
            LivestockSliderTextHelper.UpdateText(__instance);
    }

    [HarmonyPatch(typeof(UISubWidgetLivestockControls), "OnUserDefinedHerdSizeChanged")]
    internal static class Patch_UILivestock_OnHerdSizeChanged
    {
        static void Postfix(UISubWidgetLivestockControls __instance) =>
            LivestockSliderTextHelper.UpdateText(__instance);
    }

    internal static class LivestockSliderTextHelper
    {
        private static System.Reflection.FieldInfo _textField;
        private static System.Reflection.FieldInfo _sliderField;

        internal static void UpdateText(UISubWidgetLivestockControls inst)
        {
            if (RuntimeConfig.LivestockCapacityMultiplier <= 1f) return;

            if (_textField == null)
            {
                var t = typeof(UISubWidgetLivestockControls);
                _textField   = t.GetField("herdSizeText",   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _sliderField = t.GetField("herdSizeSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            if (_textField == null || _sliderField == null) return;

            var text   = _textField.GetValue(inst)   as TMPro.TextMeshProUGUI;
            var slider = _sliderField.GetValue(inst) as UnityEngine.UI.Slider;
            if (text == null || slider == null) return;

            int cur = Mathf.RoundToInt(slider.value);
            int max = Mathf.RoundToInt(slider.maxValue);
            text.text = "x" + cur + " <size=75%><color=#F4D44D>(mod max " + max + ")</color></size>";
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: LivestockBuilding.Start — Livestock barn capacity ──────────────
    // Multiplies numLivestockToBeOverpopulated on each unique LivestockHerdSetupData
    // ScriptableObject so that the UI slider max and the save-load clamp both use
    // the increased ceiling.  Tracked via HashSet<int> by SO instanceID so shared
    // ScriptableObjects (multiple barns of the same type) are only modified once.
    // If a building's userDefinedMaxLivestock was at the vanilla maximum it is
    // automatically raised to the new maximum so existing saves benefit immediately.
    [HarmonyPatch(typeof(LivestockBuilding), "Start")]
    internal static class Patch_LivestockBuilding_Start
    {
        // Cleared on every scene load so the log fires once per SO per load.
        private static readonly System.Collections.Generic.HashSet<int> _patchedSOs =
            new System.Collections.Generic.HashSet<int>();

        // Never cleared — stores the unmodified vanilla value for each SO.
        // ScriptableObjects persist in memory across scene reloads; without this,
        // the multiplier would compound each reload (8 → 16 → 32 → …).
        private static readonly Dictionary<int, int> _originalValues =
            new Dictionary<int, int>();

        internal static void ClearCache() => _patchedSOs.Clear();

        static void Postfix(LivestockBuilding __instance)
        {
            float mult = RuntimeConfig.LivestockCapacityMultiplier;
            if (mult <= 1f) return;

            var sd = __instance.herdSetupData;
            if (sd == null) return;

            int soId = sd.GetInstanceID();

            // Record the vanilla value the first time we see this SO (survives reloads).
            if (!_originalValues.TryGetValue(soId, out int vanillaOver))
            {
                vanillaOver = sd.numLivestockToBeOverpopulated;
                _originalValues[soId] = vanillaOver;
            }

            int newOver = Mathf.RoundToInt(vanillaOver * mult);
            if (newOver <= vanillaOver) return; // multiplier rounds down to same value

            if (!_patchedSOs.Contains(soId))
            {
                _patchedSOs.Add(soId);
                if (sd.numLivestockToBeOverpopulated != newOver)
                {
                    sd.numLivestockToBeOverpopulated = newOver;
                    MelonLogger.Msg($"[TechRankExpander] {sd.name}: numLivestockToBeOverpopulated "
                        + $"{vanillaOver} -> {newOver} ({mult}x)");
                }
            }

            // If this building's capacity was at the vanilla maximum, raise it to the new maximum.
            if (__instance.userDefinedMaxLivestock == vanillaOver - 1)
                __instance.userDefinedMaxLivestock = newOver - 1;
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: Well.Start — Deep Wells water capacity bonus ───────────────────
    // After Well.Start() initialises the well (including the first SafeAddWater call),
    // we apply a flat bonus to the private `maxWater` field equal to:
    //     Deep Wells curRank × DeepWellsWaterVolumePerRank
    // The well's storage cap is governed solely by `maxWater`, so existing wells
    // will gradually fill to the new maximum over time.  New wells start at the
    // initial water value and fill up from there.
    [HarmonyPatch(typeof(Well), "Start")]
    internal static class Patch_WellStart
    {
        private static System.Reflection.FieldInfo _maxWaterField;

        static void Postfix(Well __instance)
        {
            int bonus = RuntimeConfig.DeepWellsWaterVolumePerRank;
            if (bonus <= 0) return;

            if (!TechCache.ByName.TryGetValue("Deep Wells", out var tech) || tech.curRank <= 0) return;

            if (_maxWaterField == null)
                _maxWaterField = typeof(Well).GetField(
                    "maxWater",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (_maxWaterField == null) return;

            int current = (int)_maxWaterField.GetValue(__instance);
            int newMax  = current + tech.curRank * bonus;
            _maxWaterField.SetValue(__instance, newMax);
            MelonLogger.Msg($"[TechRankExpander] Well '{__instance.name}' maxWater: {current} -> {newMax} (Deep Wells rank {tech.curRank})");
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: Villager.GetCarryCapacity ──────────────────────────────────────
    // Multiplies the final carry weight so villagers can haul more per trip.
    [HarmonyPatch(typeof(Villager), nameof(Villager.GetCarryCapacity))]
    internal static class Patch_VillagerCarryCapacity
    {
        static void Postfix(ref float __result)
        {
            float m = RuntimeConfig.CarryCapacityMultiplier;
            if (m == 1f) return;
            __result *= m;
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: GE_ManufacturingSourceItemModify.UpdateItemDef ─────────────────
    // Each rank of "Wax-Sealed Barrels" calls UpdateItemDef with modify > 0,
    // incrementing wax cost by 1 per rank (rank 1 → 1 wax, rank 20 → 20 wax).
    // This prefix caps the addition so wax never exceeds MaxWaxPerBarrel.
    // Example: MaxWaxPerBarrel=2, currentWax=2 → canAdd=0 → modify=0 → no change.
    // Deactivation (modify < 0) is never intercepted — the game correctly removes
    // excess wax when ranks are refunded.
    [HarmonyPatch(typeof(GE_ManufacturingSourceItemModify), "UpdateItemDef")]
    internal static class Patch_GE_ManufacturingSourceItemModify_UpdateItemDef
    {
        internal static System.Reflection.FieldInfo _itemNameField;
        private  static System.Reflection.FieldInfo _manuDefField;

        static void Prefix(GE_ManufacturingSourceItemModify __instance, ref float modify)
        {
            if (modify <= 0f) return;

            if (_itemNameField == null)
            {
                var t = typeof(GE_ManufacturingSourceItemModify);
                _itemNameField = t.GetField("itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _manuDefField  = t.GetField("manuDef",  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }

            string itemName = _itemNameField?.GetValue(__instance) as string;
            if (itemName != "ItemWax") return;

            ManufactureDefinition manuDef = _manuDefField?.GetValue(__instance) as ManufactureDefinition;
            if (manuDef == null) return;

            int currentTotal = 0;
            foreach (SourceItemDefinition sourceItem in manuDef.sourceItems)
            {
                if (sourceItem.itemName == "ItemWax")
                {
                    currentTotal = sourceItem.numSourceItemsNeeded;
                    break;
                }
            }
            int canAdd = Mathf.Max(0, RuntimeConfig.MaxWaxPerBarrel - currentTotal);
            modify = Mathf.Min(modify, (float)canAdd);
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: Wax-Sealed Barrels tooltip ─────────────────────────────────────
    // Appends the configured wax cap to the tech description in the player's
    // current UI language so the limit is visible in the tech tree.
    [HarmonyPatch(typeof(TechTreeManager), "GetTechTreeNodeDescription")]
    internal static class Patch_GetTechTreeNodeDescription_WaxCap
    {
        private static int _waxTechId = -1;

        private static readonly Dictionary<string, string> _waxLoc = new Dictionary<string, string>
        {
            ["English"]               = "Wax cost capped at {0}.",
            ["German"]                = "Wachskosten auf {0} begrenzt.",
            ["French"]                = "Coût en cire limité à {0}.",
            ["Italian"]               = "Costo in cera limitato a {0}.",
            ["Spanish"]               = "Coste de cera limitado a {0}.",
            ["Portuguese"]            = "Custo de cera limitado a {0}.",
            ["Russian"]               = "Расход воска ограничен до {0}.",
            ["Ukrainian"]             = "Витрата воску обмежена до {0}.",
            ["Polish"]                = "Koszt wosku ograniczony do {0}.",
            ["Czech"]                 = "Náklady na vosk omezeny na {0}.",
            ["Swedish"]               = "Vaxkostnaden begränsad till {0}.",
            ["Japanese"]              = "蠟のコストは {0} に制限されます。",
            ["Korean"]                = "왁스 비용이 {0}으로 제한됩니다.",
            ["Chinese (Simplified)"]  = "蜡的消耗限制为 {0}。",
            ["Chinese (Traditional)"] = "蠟的消耗限制為 {0}。",
        };

        static void Postfix(TechTreeManager __instance, int id, ref string __result)
        {
            if (_waxTechId < 0)
            {
                foreach (TechTreeNodeData node in __instance.techTreeNodeData)
                {
                    if (node.gameEffectsEntries == null) continue;
                    foreach (GameEffectEntry entry in node.gameEffectsEntries)
                    {
                        if (!(entry.gameEffect is GE_ManufacturingSourceItemModify geModify)) continue;
                        // Use cached FieldInfo for itemName
                        string iName = Patch_GE_ManufacturingSourceItemModify_UpdateItemDef
                            ._itemNameField?.GetValue(geModify) as string
                            ?? Traverse.Create(geModify).Field("itemName").GetValue<string>();
                        if (iName != "ItemWax") continue;
                        _waxTechId = node.GetId();
                        break;
                    }
                    if (_waxTechId >= 0) break;
                }
            }
            if (id != _waxTechId || _waxTechId < 0) return;
            string lang = I2.Loc.LocalizationManager.CurrentLanguage ?? "English";
            if (!_waxLoc.TryGetValue(lang, out string text))
                text = _waxLoc["English"];
            __result += "\n\n<b>[Mod] " + string.Format(text, RuntimeConfig.MaxWaxPerBarrel) + "</b>";
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: Deep Wells tooltip ──────────────────────────────────────────────
    // Appends the configured water-volume bonus to the Deep Wells description.
    [HarmonyPatch(typeof(TechTreeManager), "GetTechTreeNodeDescription")]
    internal static class Patch_GetTechTreeNodeDescription_DeepWells
    {
        private static int _deepWellsTechId = -1;

        private static readonly Dictionary<string, (string label, string perRank, string current, string next, string max)> _loc =
            new Dictionary<string, (string, string, string, string, string)>
        {
            ["English"]               = ("Well capacity",         "per rank",        "current",         "next rank",       "max rank"),
            ["Russian"]               = ("Емкость колодца",      "за ранг",          "сейчас",            "след. ранг",       "макс. ранг"),
            ["Ukrainian"]             = ("ємність колодязя",    "за ранг",          "зараз",             "наст. ранг",      "макс. ранг"),
            ["German"]                = ("Brunnenkapazität",      "pro Rang",         "aktuell",          "nächster Rang",   "max Rang"),
            ["French"]                = ("Capacité du puits",    "par rang",         "actuel",           "rang suivant",    "rang max"),
            ["Spanish"]               = ("Capacidad del pozo",   "por rango",        "actual",           "rango siguiente", "rango máx."),
            ["Italian"]               = ("Capac. del pozzo",     "per rango",        "attuale",          "prossimo rango",  "rango max"),
            ["Portuguese"]            = ("Capacidade do poço",  "por nível",        "atual",             "próximo nível",   "nível máx."),
            ["Polish"]                = ("Pojemność studni",     "na poziom",        "obecnie",          "nast. poziom",    "maks. poziom"),
            ["Czech"]                 = ("Kapacita studny",      "za úroveň",        "aktuálně",         "další úroveň",    "max. úroveň"),
            ["Swedish"]               = ("Brunnens kapacitet",   "per rank",         "nuvarande",        "nästa rank",      "max rank"),
            ["Chinese (Simplified)"]  = ("井的容量",             "每级",              "当前",              "下一级",          "最高级"),
            ["Chinese (Traditional)"] = ("井的容量",             "每級",              "目前",              "下一級",          "最高級"),
            ["Japanese"]              = ("井の割増量",          "ランクごと",        "現在",              "次のランク",      "最大ランク"),
            ["Korean"]                = ("우물 용량",            "등급당",           "현재",              "다음 등급",       "최대 등급"),
        };

        static void Postfix(TechTreeManager __instance, int id, ref string __result)
        {
            int bonus = RuntimeConfig.DeepWellsWaterVolumePerRank;
            if (bonus <= 0) return;

            // Cache the Deep Wells tech ID on first call (O(1) lookup from then on)
            if (_deepWellsTechId < 0)
            {
                if (TechCache.ByName.TryGetValue("Deep Wells", out var found))
                    _deepWellsTechId = found.GetId();
            }
            if (id != _deepWellsTechId || _deepWellsTechId < 0) return;

            if (!TechCache.ById.TryGetValue(id, out var tech)) return;

            string lang = I2.Loc.LocalizationManager.CurrentLanguage ?? "English";
            if (!_loc.TryGetValue(lang, out var s)) s = _loc["English"];

            int maxRanks  = tech.GetNumRanks();
            int curRank   = tech.curRank;
            int curBonus  = curRank * bonus;
            int nextBonus = (curRank + 1) * bonus;

            string line;
            if (curRank == 0)
                line = $"\n\n<b>[Mod] {s.label}: <color=#6ECFF6>+{nextBonus}</color> {s.perRank} ({s.next})</b>";
            else if (curRank < maxRanks)
                line = $"\n\n<b>[Mod] {s.label}: <color=#6ECFF6>+{curBonus}</color> ({s.current}) / <color=#6ECFF6>+{nextBonus}</color> {s.perRank} ({s.next})</b>";
            else
                line = $"\n\n<b>[Mod] {s.label}: <color=#6ECFF6>+{curBonus}</color> ({s.max})</b>";

            __result += line;
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(TechTreeNodeData), nameof(TechTreeNodeData.GetNumRanks))]
    internal static class Patch_GetNumRanks
    {
        static void Postfix(TechTreeNodeData __instance, ref int __result)
        {
            if (RuntimeConfig.ActiveRanks.TryGetValue(__instance.GetTechName(), out int overrideRanks))
                __result = overrideRanks;
        }
    }

    // ── Patch: ArePrereqNodesActive ───────────────────────────────────────────
    // Original: requires all prereq techs to have state == Active (all ranks bought).
    // Patched:  requires only curRank >= 1 (at least one rank purchased).
    [HarmonyPatch(typeof(TechTreeManager), "ArePrereqNodesActive")]
    internal static class Patch_ArePrereqNodesActive
    {
        static bool Prefix(TechTreeManager __instance, TechTreeNodeData ttnd, ref bool __result)
        {
            int[] prereqIds = ttnd.GetPrereqNodeIds();
            if (prereqIds == null || prereqIds.Length == 0)
            {
                __result = true;
                return false;
            }

            __result = true;
            foreach (int id in prereqIds)
            {
                // O(1) cache lookup instead of O(n) List.Find
                if (!TechCache.ById.TryGetValue(id, out var prereq) || prereq.curRank < 1)
                {
                    __result = false;
                    break;
                }
            }
            return false; // skip original
        }
    }

    // ── Patch: ActivateTechOrRank ─────────────────────────────────────────────
    // After any rank purchase (not just full completion), re-check prereqs so
    // dependent techs unlock immediately on the first rank bought.
    // Also: on load, call buildManager.ActivateTech for any tech with curRank >= 1
    // so buildings unlocked by tech remain available across saves.
    [HarmonyPatch(typeof(TechTreeManager), nameof(TechTreeManager.ActivateTechOrRank))]
    internal static class Patch_ActivateTechOrRank
    {
        // Signature changed in v1.9.4: ActivateTechOrRank(int id, int knowledgePointCost, bool onLoad)
        static void Postfix(TechTreeManager __instance, int id, int knowledgePointCost, bool onLoad)
        {
            if (!onLoad)
            {
                __instance.UpdatePrereqNodes(true);
            }
            else
            {
                // O(1) cache lookup
                if (TechCache.ById.TryGetValue(id, out var tech) && tech.curRank >= 1)
                {
                    var gm = UnitySingleton<GameManager>.Instance;
                    gm?.buildManager?.ActivateTech(id);
                }
            }
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: UITechTreeOverview confirm buttons ──────────────────────────────
    // buildManager.ActivateTech is only called in vanilla when state == Active.
    // With 20 ranks, state never becomes Active until rank 20, so buildings that
    // depend on a tech (e.g. Stonecutter from Masonry) would stay locked.
    // Fix: after any confirm, activate buildings for all techs with curRank >= 1.
    [HarmonyPatch(typeof(UITechTreeOverview), nameof(UITechTreeOverview.OnKpsUsedConfirm))]
    internal static class Patch_OnKpsUsedConfirm
    {
        static void Postfix()
        {
            TechBuildingHelper.ActivateTechBuildings();
            // Re-evaluate ALL tech prereqs (force:true) so techs like Monument
            // unlock immediately after confirming their prerequisite (e.g. Masonry).
            var gm = UnitySingleton<GameManager>.Instance;
            gm?.techTreeManager?.UpdatePrereqNodes(true);
        }
    }

    [HarmonyPatch(typeof(UITechTreeOverview), "OnConfirmCachedChanges")]
    internal static class Patch_OnConfirmCachedChanges
    {
        static void Postfix()
        {
            TechBuildingHelper.ActivateTechBuildings();
            var gm = UnitySingleton<GameManager>.Instance;
            gm?.techTreeManager?.UpdatePrereqNodes(true);
        }
    }

    internal static class TechBuildingHelper
    {
        internal static void ActivateTechBuildings()
        {
            var gm = UnitySingleton<GameManager>.Instance;
            if (gm?.techTreeManager == null || gm.buildManager == null) return;
            foreach (var tech in gm.techTreeManager.techTreeNodeData)
            {
                if (tech.curRank >= 1)
                    gm.buildManager.ActivateTech(tech.GetId());
            }
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Tech reset / clamp helpers ────────────────────────────────────────────
    // Shared state for the load-time patches below.
    internal static class TechResetHelper
    {
        internal static int AccumulatedKpRefund = 0;
        internal static int AccumulatedKpCost   = 0;
        internal static bool InTechManagerLoad = false;
    }

    // Patch: TechTreeNodeData.Load ─────────────────────────────────────────────
    // Runs after each tech node's data is loaded from the save file.
    // 1) If ResetTechTree flag is set: zero out this node's ranks (full reset).
    // 2) Otherwise: clamp to mod cap and track excess KP for refund.
    // Because this runs before TechTreeManager.Load checks `curRank > 0`,
    // zeroed nodes will NOT have ActivateTechOrRank called, so game effects
    // (work time reductions etc.) are never applied for reset nodes.
    [HarmonyPatch(typeof(TechTreeNodeData), "Load", new System.Type[] { typeof(ES2Reader) })]
    internal static class Patch_TechTreeNodeData_Load
    {
        static void Postfix(TechTreeNodeData __instance)
        {
            if (!TechResetHelper.InTechManagerLoad) return;

            if (RuntimeConfig.ResetTechTree)
            {
                TechResetHelper.AccumulatedKpRefund += __instance.curRank;
                __instance.curRank = 0;
                __instance.state = TechTreeNodeData.State.Locked;
            }
            else if (RuntimeConfig.AllotAllTechs)
            {
                // ── Allot All: fill every tech to its configured cap ──────────
                // Symmetric to ResetTechTree: instead of zeroing ranks and refunding KP,
                // we maximise ranks and deduct the KP cost on load.
                int cap = __instance.GetNumRanks(); // mod-patched value
                if (__instance.curRank < cap)
                {
                    TechResetHelper.AccumulatedKpCost += cap - __instance.curRank;
                    __instance.curRank = cap;
                    __instance.state = TechTreeNodeData.State.Active;
                }
                else if (__instance.curRank >= cap)
                {
                    __instance.state = TechTreeNodeData.State.Active;
                }
            }
            else
            {
                int cap = __instance.GetNumRanks(); // mod-patched value
                if (__instance.curRank > cap)
                {
                    TechResetHelper.AccumulatedKpRefund += __instance.curRank - cap;
                    __instance.curRank = cap;
                    if (__instance.state == TechTreeNodeData.State.Active)
                        __instance.state = TechTreeNodeData.State.PrereqsMet;
                }
                else if (__instance.state == TechTreeNodeData.State.Active && __instance.curRank < cap)
                {
                    // Tech was fully researched in vanilla (state=Active) but mod cap is higher.
                    // Downgrade to PrereqsMet so the player can purchase additional ranks.
                    __instance.state = TechTreeNodeData.State.PrereqsMet;
                }
            }
        }
    }

    // Patch: TechTreeManager.Load ──────────────────────────────────────────────
    // Brackets the load loop so Patch_TechTreeNodeData_Load knows it's active.
    // After load: applies accumulated KP refund and clears the reset flag.
    [HarmonyPatch(typeof(TechTreeManager), "Load", new System.Type[] { typeof(ES2Reader) })]
    internal static class Patch_TechTreeManager_Load
    {
        static void Prefix(TechTreeManager __instance)
        {
            // Re-apply numRanks here as a safety net for game versions where Awake()
            // is protected and Harmony may not hook it reliably (observed in v1.1.2a).
            // Running Apply again is idempotent — it just overwrites the same values.
            NumRanksHelper.Apply(__instance);

            // Reset accumulated values at load start so a previously interrupted load
            // (exception before Postfix ran) cannot corrupt the next load's KP math.
            TechResetHelper.InTechManagerLoad = true;
            TechResetHelper.AccumulatedKpRefund = 0;
            TechResetHelper.AccumulatedKpCost = 0;
        }

        static void Postfix(TechTreeManager __instance)
        {
            TechResetHelper.InTechManagerLoad = false;

            if (TechResetHelper.AccumulatedKpRefund > 0)
            {
                __instance.knowledgePoints += TechResetHelper.AccumulatedKpRefund;
                MelonLogger.Msg($"[TechRankExpander] Refunded {TechResetHelper.AccumulatedKpRefund} KP (reset or cap reduced).");
                TechResetHelper.AccumulatedKpRefund = 0;
            }

            if (RuntimeConfig.ResetTechTree)
            {
                RuntimeConfig.ResetTechTree = false;
                TechRankExpander.Instance?.ClearResetFlag();
                __instance.UpdatePrereqNodes(true);
                MelonLogger.Msg("[TechRankExpander] Tech tree fully reset. Reload the tech tree UI to see changes.");
            }

            if (RuntimeConfig.AllotAllTechs)
            {
                // Deduct the KP cost (clamped to 0 — never go negative).
                int cost = Mathf.Min(TechResetHelper.AccumulatedKpCost, __instance.knowledgePoints);
                __instance.knowledgePoints -= cost;
                MelonLogger.Msg($"[TechRankExpander] Allot All: filled all techs to cap, spent {cost} KP "
                    + $"(remaining: {__instance.knowledgePoints}).");
                TechResetHelper.AccumulatedKpCost = 0;

                __instance.UpdatePrereqNodes(true);
                TechBuildingHelper.ActivateTechBuildings();
            }
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Guard: HappinessManager.GetWorkRateMultiplier(Villager) ──────────────
    // GE_OccupationWorkRate stores occupation bonuses in occupationToWorkRateChanges.
    // The per-villager formula is:  result = happinessCurve(happiness) + techBonus
    // The happiness part is already clamped to >= 0 inside GetWorkRateMultiplier(float),
    // but the final addition of techBonus has NO game-side clamp.
    // A large negative techBonus (e.g. Civic Inspections rank 4+, or any future tech
    // that applies a negative GE_OccupationWorkRate at high ranks) makes the total
    // negative — workers move in reverse or freeze entirely.
    // This Postfix clamps the returned rate to a minimum of 0.01 (1% speed) so the
    // game never receives a zero or negative work-rate regardless of configuration.
    [HarmonyPatch(typeof(HappinessManager), "GetWorkRateMultiplier", new System.Type[] { typeof(Villager) })]
    internal static class Patch_GetWorkRateMultiplier_Clamp
    {
        static void Postfix(ref float __result)
        {
            if (__result < 0.01f)
                __result = 0.01f;
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── NumRanks field writer ────────────────────────────────────────────────
    // GetNumRanks() is a trivial one-liner (return numRanks) that the Mono JIT
    // frequently inlines, so our Harmony Postfix on GetNumRanks() is bypassed in
    // call sites like GetNumKnowledgePointsRemaining() and ActivateTechOrRank().
    // When GetNumKnowledgePointsRemaining() reads the original (vanilla) numRanks,
    // it computes a cap equal to the vanilla total (~152) instead of the extended
    // total, causing the Academy to stop generating KP once 152 points accumulate.
    // Fix: write the override value directly into the numRanks private field on
    // every TechTreeNodeData instance so all code paths see the extended count.
    internal static class NumRanksHelper
    {
        private static System.Reflection.FieldInfo _field;

        internal static void Apply(TechTreeManager ttm)
        {
            if (_field == null)
                _field = typeof(TechTreeNodeData).GetField(
                    "numRanks",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (_field == null || ttm?.techTreeNodeData == null) return;

            // Build the fast lookup cache while we iterate the tech list anyway
            TechCache.Build(ttm);

            // Now that TechCache is ready, localized tech names are available —
            // refresh cfg descriptions so they show the in-game name for the current language.
            TechRankExpander.Instance?.RefreshDescriptions();

            int count = 0;
            var missedActiveRanks = new System.Collections.Generic.HashSet<string>(RuntimeConfig.ActiveRanks.Keys);
            foreach (var tech in ttm.techTreeNodeData)
            {
                string name = tech.GetTechName();
                missedActiveRanks.Remove(name);
                if (RuntimeConfig.ActiveRanks.TryGetValue(name, out int overrideRanks))
                {
                    _field.SetValue(tech, overrideRanks);
                    count++;
                }
            }
            MelonLogger.Msg($"[TechRankExpander] numRanks written on {count} tech nodes; cache built ({TechCache.ById.Count} entries).");
            if (missedActiveRanks.Count > 0)
                MelonLogger.Warning($"[TechRankExpander] {missedActiveRanks.Count} ActiveRanks keys had no matching tech: {string.Join(", ", missedActiveRanks)}");


        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: TechTreeManager.Awake ─────────────────────────────────────────
    // Awake() sets kpUnitsGenerationMultiplier from difficulty level.
    // We add (mult-1) on top so the net effect is exactly mult× speed.
    [HarmonyPatch(typeof(TechTreeManager), "Awake")]
    internal static class Patch_TechTreeManagerAwake
    {
        static void Postfix(TechTreeManager __instance)
        {
            // Write numRanks directly so inlined GetNumRanks() calls also see
            // the extended caps (fixes Academy KP cap at vanilla ~152 limit).
            NumRanksHelper.Apply(__instance);

            float kpMult = RuntimeConfig.KpSpeedMultiplier;
            if (kpMult > 1f)
            {
                float before = __instance.kpUnitsGenerationMultiplier;
                __instance.kpUnitsGenerationMultiplier += (kpMult - 1f);
                MelonLogger.Msg($"[TechRankExpander] KP speed: {before:F2} -> {__instance.kpUnitsGenerationMultiplier:F2} ({kpMult}x)");
            }

            // Apply work speed bonus from Production Management ranks
            WorkSpeedHelper.ApplyWorkSpeed(__instance);
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: TechTreeManager.GetTechTreeNodeDescription ────────────────────
    // Appends work speed bonus line to Production Management tooltip.
    [HarmonyPatch(typeof(TechTreeManager), "GetTechTreeNodeDescription")]
    internal static class Patch_GetTechTreeNodeDescription
    {
        // Translations: label / "at rank 1" / "next rank" / "max rank"
        private static readonly Dictionary<string, (string label, string atRank1, string nextRank, string maxRank)> _loc =
            new Dictionary<string, (string, string, string, string)>
        {
            ["English"]              = ("All Workers Speed",    "at rank 1",         "next rank",       "max rank"),
            ["Russian"]              = ("Скорость всех рабочих","на 1 уровне",        "следующий уровень","макс. уровень"),
            ["Ukrainian"]            = ("Швидкість всіх робітн.","на 1 рівні",        "наступний рівень","макс. рівень"),
            ["German"]               = ("Arbeitsgeschwindigkeit","bei Rang 1",         "nächster Rang",   "max Rang"),
            ["French"]               = ("Vitesse des travaill.", "au rang 1",          "rang suivant",    "rang max"),
            ["Spanish"]              = ("Velocidad trabajadores","en rango 1",          "rango siguiente", "rango máx."),
            ["Italian"]              = ("Velocità lavoratori",  "al rango 1",         "prossimo rango",  "rango max"),
            ["Portuguese"]           = ("Veloc. trabalhadores", "no nível 1",         "próximo nível",   "nível máx."),
            ["Polish"]               = ("Prędkość pracowników", "na poziomie 1",      "następny poziom", "maks. poziom"),
            ["Czech"]                = ("Rychlost pracovníků",  "na úrovni 1",        "další úroveň",    "max. úroveň"),
            ["Swedish"]              = ("Alla arbetares fart",  "vid rank 1",         "nästa rank",      "max rank"),
            ["Chinese (Simplified)"] = ("所有工人速度",           "在第1级",             "下一级",           "最高级"),
            ["Chinese (Traditional)"]= ("所有工人速度",           "在第1級",             "下一級",           "最高級"),
            ["Japanese"]             = ("全作業員の速度",         "ランク1で",           "次のランク",       "最大ランク"),
            ["Korean"]               = ("모든 작업자 속도",        "1등급에서",           "다음 등급",        "최고 등급"),
        };

        private static (string label, string atRank1, string nextRank, string maxRank) GetStrings()
        {
            string lang = I2.Loc.LocalizationManager.CurrentLanguage ?? "English";
            return _loc.TryGetValue(lang, out var t) ? t : _loc["English"];
        }

        static void Postfix(TechTreeManager __instance, int id, ref string __result)
        {
            if (RuntimeConfig.WorkSpeedPerRank <= 0f) return;
            // O(1) lookup via cache
            if (!TechCache.ById.TryGetValue(id, out var tech)) return;
            if (tech.GetTechName() != "Production Management") return;

            int maxRanks = tech.GetNumRanks();
            float perRank = RuntimeConfig.WorkSpeedPerRank * 100f;
            float current = tech.curRank * perRank;
            float next    = (tech.curRank + 1) * perRank;
            var s = GetStrings();

            string line;
            if (tech.curRank == 0)
                line = $"\n\n<b>[Mod] {s.label}: <color=#F4D44D>+{next:F0}%</color> {s.atRank1}</b>";
            else if (tech.curRank < maxRanks)
                line = $"\n\n<b>[Mod] {s.label}: <color=#F4D44D>+{current:F0}%</color> / <color=#F4D44D>+{next:F0}%</color> {s.nextRank}</b>";
            else
                line = $"\n\n<b>[Mod] {s.label}: <color=#F4D44D>+{current:F0}%</color> ({s.maxRank})</b>";

            __result += line;
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Work Speed Helper ────────────────────────────────────────────────────
    // Reads Production Management curRank and applies +WorkSpeedPerRank per rank
    // to ALL worker occupations via happinessManager.ChangeOccupationWorkRate.
    internal static class WorkSpeedHelper
    {
        // Occupations that do productive work (excludes None, Deserter, Child, Disabled)
        private static readonly VillagerOccupation.Occupation[] _workers = new[]
        {
            VillagerOccupation.Occupation.Laborer,
            VillagerOccupation.Occupation.Hunter,
            VillagerOccupation.Occupation.Builder,
            VillagerOccupation.Occupation.Woodcutter,
            VillagerOccupation.Occupation.Sawyer,
            VillagerOccupation.Occupation.Farmer,
            VillagerOccupation.Occupation.Baker,
            VillagerOccupation.Occupation.Tanner,
            VillagerOccupation.Occupation.Miller,
            VillagerOccupation.Occupation.Guard,
            VillagerOccupation.Occupation.Miner,
            VillagerOccupation.Occupation.Foundryman,
            VillagerOccupation.Occupation.Blacksmith,
            VillagerOccupation.Occupation.Fletcher,
            VillagerOccupation.Occupation.Fisherman,
            VillagerOccupation.Occupation.Cobbler,
            VillagerOccupation.Occupation.Smoker,
            VillagerOccupation.Occupation.Weaver,
            VillagerOccupation.Occupation.CharcoalMaker,
            VillagerOccupation.Occupation.Potter,
            VillagerOccupation.Occupation.BoatBuilder,
            VillagerOccupation.Occupation.Forager,
            VillagerOccupation.Occupation.Brewer,
            VillagerOccupation.Occupation.Wainwright,
            VillagerOccupation.Occupation.Publican,
            VillagerOccupation.Occupation.BasketMaker,
            VillagerOccupation.Occupation.WorkCampLaborer,
            VillagerOccupation.Occupation.Trader,
            VillagerOccupation.Occupation.Herder,
            VillagerOccupation.Occupation.Healer,
            VillagerOccupation.Occupation.FurnitureMaker,
            VillagerOccupation.Occupation.SoapMaker,
            VillagerOccupation.Occupation.Chandler,
            VillagerOccupation.Occupation.NightsoilMan,
            VillagerOccupation.Occupation.Teacher,
            VillagerOccupation.Occupation.Glassmaker,
            VillagerOccupation.Occupation.Brickmaker,
            VillagerOccupation.Occupation.Cheesemaker,
            VillagerOccupation.Occupation.Cooper,
            VillagerOccupation.Occupation.Apothecary,
            VillagerOccupation.Occupation.Grocer,
            VillagerOccupation.Occupation.Armourer,
            VillagerOccupation.Occupation.RatCatcher,
            VillagerOccupation.Occupation.Arborist,
            VillagerOccupation.Occupation.Preservist,
            VillagerOccupation.Occupation.Papermaker,
            VillagerOccupation.Occupation.BookBinder,
            VillagerOccupation.Occupation.Librarian,
            VillagerOccupation.Occupation.Priest,
            VillagerOccupation.Occupation.Guildmaster,
            VillagerOccupation.Occupation.Scholar,
            VillagerOccupation.Occupation.Groomer,
            VillagerOccupation.Occupation.Soldier,
        };

        private static float _appliedBonus = 0f;

        internal static void ApplyWorkSpeed(TechTreeManager ttm)
        {
            if (RuntimeConfig.WorkSpeedPerRank <= 0f) return;

            var gm = UnitySingleton<GameManager>.Instance;
            if (gm?.happinessManager == null) return;

            // O(1) lookup via cache; fall back to linear scan if cache not ready yet
            int ranks = 0;
            if (TechCache.ByName.TryGetValue("Production Management", out var tech))
                ranks = tech.curRank;
            else if (ttm?.techTreeNodeData != null)
            {
                var t = ttm.techTreeNodeData.Find(x => x.GetTechName() == "Production Management");
                if (t != null) ranks = t.curRank;
            }

            float targetBonus = ranks * RuntimeConfig.WorkSpeedPerRank;
            float delta = targetBonus - _appliedBonus;

            if (Mathf.Approximately(delta, 0f)) return;

            foreach (var occ in _workers)
                gm.happinessManager.ChangeOccupationWorkRate(occ, delta);

            _appliedBonus = targetBonus;
            MelonLogger.Msg($"[TechRankExpander] Work speed bonus: {_appliedBonus * 100f:F0}% (Production Management rank {ranks})");
        }

        internal static void Reset() { _appliedBonus = 0f; }
    }
    // ── CFG description localisation ──────────────────────────────────────────
    internal static class CfgL10n
    {
        // Returns description text in the current game language, falling back to English.
        internal static string Get(string en, string ru, string de, string fr, string es,
                                   string it, string pt, string pl, string sv,
                                   string ko, string ja, string cs)
        {
            // Use I2Loc if available (in-game language changes), fall back to PlayerPrefs
            // (OnInitializeMelon runs before I2Loc is fully initialized).
            string lang = I2.Loc.LocalizationManager.CurrentLanguage;
            if (string.IsNullOrEmpty(lang))
                lang = UnityEngine.PlayerPrefs.GetString("currentLanguage", "English");
            switch (lang)
            {
                case "Russian":    return ru;
                case "German":     return de;
                case "French":     return fr;
                case "Spanish":    return es;
                case "Italian":    return it;
                case "Portuguese": return pt;
                case "Polish":     return pl;
                case "Swedish":    return sv;
                case "Korean":     return ko;
                case "Japanese":   return ja;
                case "Czech":      return cs;
                default:           return en;
            }
        }

        // Shorthand: single-language lookup table for descriptions used at cfg creation time.
        internal static string KpSpeed() => Get(
            "How many times faster knowledge points are generated (e.g. 5 = 5x faster).",
            "Во сколько раз быстрее генерируются очки знаний (например, 5 = в 5 раз быстрее).",
            "Wie viel mal schneller Wissenspunkte generiert werden (z.B. 5 = 5× schneller).",
            "Combien de fois plus vite les points de connaissance sont générés (ex. 5 = 5× plus vite).",
            "Cuántas veces más rápido se generan los puntos de conocimiento (p.ej. 5 = 5× más rápido).",
            "Quante volte più velocemente vengono generati i punti conoscenza (es. 5 = 5× più veloce).",
            "Quantas vezes mais rápido os pontos de conhecimento são gerados (ex.: 5 = 5× mais rápido).",
            "Ile razy szybciej są generowane punkty wiedzy (np. 5 = 5× szybciej).",
            "Hur många gånger snabbare kunskapspoäng genereras (t.ex. 5 = 5× snabbare).",
            "지식 포인트가 생성되는 속도 배율 (예: 5 = 5배 빠름).",
            "知識ポイントの生成速度の倍率（例：5 = 5倍速）。",
            "Kolikrát rychleji jsou generovány body znalostí (např. 5 = 5× rychleji).");

        internal static string CarryCap() => Get(
            "Multiplier on how much weight each villager can carry per trip (e.g. 3 = 3x more items).",
            "Множитель переносимого веса для каждого жителя за поездку (например, 3 = в 3 раза больше предметов).",
            "Multiplikator für das Gewicht, das jeder Dorfbewohner pro Reise tragen kann (z.B. 3 = 3× mehr).",
            "Multiplicateur de la charge que chaque villageois peut porter par voyage (ex. 3 = 3× plus).",
            "Multiplicador de cuánto peso puede cargar cada aldeano por viaje (p.ej. 3 = 3× más).",
            "Moltiplicatore del peso che ogni abitante può trasportare per viaggio (es. 3 = 3× di più).",
            "Multiplicador de quanto peso cada aldeão pode carregar por viagem (ex.: 3 = 3× mais).",
            "Mnożnik wagi, którą każdy wieśniak może nieść na jedną podróż (np. 3 = 3× więcej).",
            "Multiplikator för hur mycket varje bybor kan bära per resa (t.ex. 3 = 3× mer).",
            "주민 1인당 운반 가능 무게 배율 (예: 3 = 3배 더 많은 아이템).",
            "村人1人が1回の移動で運べる重量の倍率（例：3 = 3倍）。",
            "Kolikrát více váhy může každý vesničan unést na jednu cestu (např. 3 = 3× více).");

        internal static string WorkSpeed() => Get(
            "Bonus to all workers' speed per rank of Production Management purchased (0.01 = +1% per rank).",
            "Бонус к скорости всех рабочих за каждый ранг 'Управления производством' (0.01 = +1% за ранг).",
            "Bonus auf die Arbeitsgeschwindigkeit aller Arbeiter pro Rang 'Produktionsmanagement' (0.01 = +1% pro Rang).",
            "Bonus à la vitesse de tous les travailleurs par rang de Gestion de la Production (0.01 = +1% par rang).",
            "Bono a la velocidad de todos los trabajadores por rango de Gestión de Producción (0.01 = +1% por rango).",
            "Bonus alla velocità di tutti i lavoratori per rango di Gestione della Produzione (0.01 = +1% per rango).",
            "Bônus à velocidade de todos os trabalhadores por nível de Gestão de Produção (0.01 = +1% por nível).",
            "Bonus do prędkości wszystkich pracowników za każdy rząd Zarządzania Produkcją (0.01 = +1% na rząd).",
            "Bonus till alla arbetares hastighet per rang av Produktionsledning (0.01 = +1% per rang).",
            "생산 관리 등급당 모든 노동자 속도 보너스 (0.01 = 등급당 +1%).",
            "生産管理の各ランクごとの全労働者速度ボーナス（0.01 = ランクごとに+1%）。",
            "Bonus k rychlosti všech pracovníků za každý rank Řízení výroby (0.01 = +1% za rank).");

        internal static string ResetTree() => Get(
            "Set to true to refund ALL spent KP and reset ALL tech ranks on the next map load. The flag is automatically cleared after applying.",
            "Установите true для возврата всех потраченных ОЗ и сброса всех рангов технологий при следующей загрузке карты. Флаг очищается автоматически.",
            "Auf true setzen, um alle ausgegebenen WP zurückzuerstatten und alle Tech-Ränge beim nächsten Kartenladen zurückzusetzen. Das Flag wird automatisch gelöscht.",
            "Mettre à true pour rembourser tous les PC dépensés et réinitialiser tous les rangs technologiques au prochain chargement. Le drapeau est effacé automatiquement.",
            "Poner en true para reembolsar todos los PC gastados y restablecer todos los rangos de tecnología en la próxima carga. El indicador se borra automáticamente.",
            "Impostare su true per rimborsare tutti i PC spesi e azzerare tutti i ranghi tecnologici al prossimo caricamento. Il flag viene cancellato automaticamente.",
            "Definir como true para reembolsar todos os PC gastos e redefinir todos os níveis de tecnologia no próximo carregamento. O sinalizador é apagado automaticamente.",
            "Ustaw na true, aby zwrócić wszystkie wydane PW i zresetować wszystkie rangi technologii przy następnym ładowaniu mapy. Flaga jest czyszczona automatycznie.",
            "Sätt till true för att återbetala alla spenderade KP och återställa alla teknikranker vid nästa kartladdning. Flaggan rensas automatiskt.",
            "true로 설정하면 다음 맵 로드 시 모든 KP를 환불하고 기술 등급을 초기화합니다. 플래그는 자동으로 지워집니다.",
            "trueに設定すると、次のマップ読み込み時にすべてのKPを返還し、すべての技術ランクをリセットします。フラグは自動的にクリアされます。",
            "Nastavte na true, abyste vrátili všechny utracené BP a resetovali všechny ranky technologií při příštím načtení mapy. Příznak je automaticky vymazán.");

        internal static string AllotAll() => Get(
            "Set to true to instantly fill ALL tech ranks to their configured caps on the next map load (spends KP). The setting stays enabled until you turn it off manually.",
            "Установите true, чтобы при следующей загрузке карты все ранги технологий были выкуплены до максимума (тратит ОЗ). Настройка остаётся включённой, пока вы не выключите её вручную.",
            "Auf true setzen, um beim nächsten Kartenladen alle Tech-Ränge sofort auf ihre konfigurierten Obergrenzen aufzufüllen (kostet WP). Die Einstellung bleibt aktiv, bis sie manuell deaktiviert wird.",
            "Mettre à true pour remplir instantanément tous les rangs technologiques à leurs limites configurées au prochain chargement (dépense des PC). Le paramètre reste activé jusqu'à ce que vous le désactiviez manuellement.",
            "Poner en true para rellenar instantáneamente todos los rangos de tecnología hasta sus límites configurados en la próxima carga (gasta PC). La configuración permanece habilitada hasta que la desactives manualmente.",
            "Impostare su true per riempire istantaneamente tutti i ranghi tecnologici ai loro limiti configurati al prossimo caricamento (spende PC). L'impostazione rimane abilitata finché non la disabiliti manualmente.",
            "Definir como true para preencher instantaneamente todos os níveis de tecnologia até seus limites configurados no próximo carregamento (gasta PC). A configuração permanece ativa até que você a desative manualmente.",
            "Ustaw na true, aby natychmiast uzupełnić wszystkie rangi technologii do skonfigurowanych limitów przy następnym ładowaniu mapy (wydaje PW). Ustawienie pozostaje włączone, dopóki ręcznie go nie wyłączysz.",
            "Sätt till true för att omedelbart fylla alla teknikranker till deras konfigurerade tak vid nästa kartladdning (spenderar KP). Inställningen förblir aktiverad tills du stänger av den manuellt.",
            "true로 설정하면 다음 맵 로드 시 모든 기술 등급을 설정된 최대치로 즉시 채웁니다 (KP 소모). 수동으로 끌 때까지 설정이 유지됩니다.",
            "trueに設定すると、次のマップ読み込み時にすべての技術ランクを設定された上限まで即座に埋めます（KP消費）。手動でオフにするまで設定は有効です。",
            "Nastavte na true, abyste okamžitě vyplnili všechny ranky technologií na jejich nakonfigurované limity při příštím načtení mapy (utrácí BP). Nastavení zůstane aktivní, dokud ho ručně nevypnete.");

        internal static string DeepWells() => Get(
            "Extra water capacity added to every well per rank of the 'Deep Wells' technology. 50 = +50 water per rank (e.g. rank 5 → +250 capacity). 0 = disabled. Bonus is applied when the map loads.",
            "Дополнительный объём воды в каждом колодце за каждый ранг технологии 'Глубокие колодцы'. 50 = +50 воды за ранг (например, 5 рангов → +250 ёмкости). 0 = отключено. Бонус применяется при загрузке карты.",
            "Zusätzliche Wasserkapazität für jeden Brunnen pro Rang der Technologie 'Tiefe Brunnen'. 50 = +50 Wasser pro Rang. 0 = deaktiviert. Bonus wird beim Kartenladen angewendet.",
            "Capacité d'eau supplémentaire ajoutée à chaque puits par rang de la technologie 'Puits profonds'. 50 = +50 eau par rang. 0 = désactivé. Le bonus est appliqué au chargement de la carte.",
            "Capacidad de agua adicional añadida a cada pozo por rango de la tecnología 'Pozos profundos'. 50 = +50 agua por rango. 0 = desactivado. El bono se aplica al cargar el mapa.",
            "Capacità d'acqua extra aggiunta a ogni pozzo per rango della tecnologia 'Pozzi profondi'. 50 = +50 acqua per rango. 0 = disabilitato. Il bonus viene applicato al caricamento della mappa.",
            "Capacidade de água extra adicionada a cada poço por nível da tecnologia 'Poços Profundos'. 50 = +50 de água por nível. 0 = desativado. O bônus é aplicado ao carregar o mapa.",
            "Dodatkowa pojemność wody dodawana do każdej studni za każdy rząd technologii 'Głębokie studnie'. 50 = +50 wody na rząd. 0 = wyłączone. Bonus jest stosowany podczas ładowania mapy.",
            "Extra vattenkapacitet tillagd till varje brunn per rang av teknologin 'Djupa brunnar'. 50 = +50 vatten per rang. 0 = inaktiverat. Bonusen tillämpas när kartan laddas.",
            "'깊은 우물' 기술 등급당 각 우물에 추가되는 물 용량. 50 = 등급당 +50. 0 = 비활성화. 보너스는 맵 로드 시 적용됩니다.",
            "'深い井戸'技術のランクごとに各井戸に追加される水の容量。50 = ランクごとに+50。0 = 無効。ボーナスはマップ読み込み時に適用されます。",
            "Extra vodní kapacita přidaná ke každé studni za každý rank technologie 'Hluboké studny'. 50 = +50 vody na rank. 0 = zakázáno. Bonus je aplikován při načtení mapy.");

        internal static string KpHotkey() => Get(
            "Press this key in-game to instantly add KP_Hotkey_Amount knowledge points. Valid values: F1-F12, K, Insert, etc. (UnityEngine.KeyCode names).",
            "Нажмите эту клавишу в игре для мгновенного добавления очков знаний. Допустимые значения: F1-F12, K, Insert и т.д. (имена UnityEngine.KeyCode).",
            "Drücken Sie diese Taste im Spiel, um sofort KP_Hotkey_Amount Wissenspunkte hinzuzufügen. Gültige Werte: F1-F12, K, Insert usw. (UnityEngine.KeyCode-Namen).",
            "Appuyez sur cette touche en jeu pour ajouter instantanément des PC. Valeurs valides : F1-F12, K, Insert, etc. (noms UnityEngine.KeyCode).",
            "Presione esta tecla en el juego para agregar instantáneamente PC. Valores válidos: F1-F12, K, Insert, etc. (nombres de UnityEngine.KeyCode).",
            "Premi questo tasto nel gioco per aggiungere istantaneamente PC. Valori validi: F1-F12, K, Insert, ecc. (nomi UnityEngine.KeyCode).",
            "Pressione esta tecla no jogo para adicionar PC instantaneamente. Valores válidos: F1-F12, K, Insert, etc. (nomes UnityEngine.KeyCode).",
            "Naciśnij ten klawisz w grze, aby natychmiast dodać PW. Prawidłowe wartości: F1-F12, K, Insert itp. (nazwy UnityEngine.KeyCode).",
            "Tryck på denna tangent i spelet för att omedelbart lägga till KP. Giltiga värden: F1-F12, K, Insert, etc. (UnityEngine.KeyCode-namn).",
            "게임 내에서 이 키를 눌러 즉시 KP를 추가합니다. 유효한 값: F1-F12, K, Insert 등 (UnityEngine.KeyCode 이름).",
            "ゲーム内でこのキーを押すとKPを即座に追加します。有効な値：F1-F12、K、Insertなど（UnityEngine.KeyCode名）。",
            "Stiskněte tuto klávesu ve hře pro okamžité přidání BP. Platné hodnoty: F1-F12, K, Insert atd. (názvy UnityEngine.KeyCode).");

        internal static string KpHotkeyAmount() => Get(
            "How many knowledge points to add per key press.",
            "Сколько очков знаний добавлять за одно нажатие клавиши.",
            "Wie viele Wissenspunkte pro Tastendruck hinzugefügt werden.",
            "Combien de points de connaissance ajouter par pression de touche.",
            "Cuántos puntos de conocimiento añadir por pulsación de tecla.",
            "Quanti punti conoscenza aggiungere per ogni pressione del tasto.",
            "Quantos pontos de conhecimento adicionar por pressionamento de tecla.",
            "Ile punktów wiedzy dodawać za każde naciśnięcie klawisza.",
            "Hur många kunskapspoäng som läggs till per tangenttryckning.",
            "키를 누를 때마다 추가할 지식 포인트 수.",
            "キーを1回押すごとに追加する知識ポイントの数。",
            "Kolik bodů znalostí přidat za každý stisk klávesy.");

        internal static string MaxWax() => Get(
            "Maximum wax consumed per barrel production. 1 = always 1 wax, 2 = capped at vanilla value (default), 0 = removes wax from recipe entirely.",
            "Максимум воска на производство одной бочки. 1 = всегда 1 воск, 2 = не выше значения ванили (по умолчанию), 0 = убрать воск из рецепта.",
            "Maximales Wachs pro Fassproduktion. 1 = immer 1 Wachs, 2 = begrenzt auf Vanilla-Wert (Standard), 0 = Wachs vollständig aus Rezept entfernen.",
            "Cire maximale consommée par production de tonneau. 1 = toujours 1 cire, 2 = limité à la valeur vanilla (défaut), 0 = retire la cire de la recette.",
            "Cera máxima consumida por producción de barril. 1 = siempre 1 cera, 2 = limitado al valor vanilla (predeterminado), 0 = eliminar cera de la receta.",
            "Cera massima consumata per produzione di botte. 1 = sempre 1 cera, 2 = limitato al valore vanilla (predefinito), 0 = rimuove la cera dalla ricetta.",
            "Cera máxima consumida por produção de barril. 1 = sempre 1 cera, 2 = limitado ao valor vanilla (padrão), 0 = remove a cera da receita.",
            "Maksymalny wosk zużywany na produkcję beczki. 1 = zawsze 1 wosk, 2 = ograniczone do wartości vanilla (domyślnie), 0 = usuwa wosk z przepisu.",
            "Maximalt vax förbrukat per tunnproduktion. 1 = alltid 1 vax, 2 = begränsat till vanilla-värde (standard), 0 = tar bort vax från receptet.",
            "배럴 생산당 소비되는 최대 밀랍. 1 = 항상 1, 2 = 바닐라 값으로 제한 (기본값), 0 = 레시피에서 밀랍 제거.",
            "バレル生産あたりの最大ワックス消費量。1 = 常に1、2 = バニラ値に制限（デフォルト）、0 = レシピからワックスを削除。",
            "Maximální vosk spotřebovaný na výrobu sudu. 1 = vždy 1 vosk, 2 = omezeno na hodnotu vanilla (výchozí), 0 = odstranit vosk z receptu.");

        internal static string LivestockCap() => Get(
            "Multiplier on the maximum number of animals in every barn type. 2 = double capacity, 1 = vanilla. Takes effect on next map load.",
            "Множитель максимального числа животных во всех типах построек. 2 = двойная вместимость, 1 = ваниль. Вступает в силу при следующей загрузке карты.",
            "Multiplikator für die maximale Anzahl von Tieren in jedem Stalltyp. 2 = doppelte Kapazität, 1 = Vanilla. Wirkt beim nächsten Kartenladen.",
            "Multiplicateur du nombre maximum d'animaux dans chaque type d'étable. 2 = double capacité, 1 = vanilla. Prend effet au prochain chargement de la carte.",
            "Multiplicador del número máximo de animales en cada tipo de establo. 2 = doble capacidad, 1 = vanilla. Tiene efecto en la próxima carga del mapa.",
            "Moltiplicatore del numero massimo di animali in ogni tipo di stalla. 2 = doppia capacità, 1 = vanilla. Ha effetto al prossimo caricamento della mappa.",
            "Multiplicador do número máximo de animais em cada tipo de celeiro. 2 = capacidade dupla, 1 = vanilla. Entra em vigor no próximo carregamento do mapa.",
            "Mnożnik maksymalnej liczby zwierząt w każdym typie stajni. 2 = podwójna pojemność, 1 = vanilla. Wchodzi w życie przy następnym ładowaniu mapy.",
            "Multiplikator för maximalt antal djur i varje stalltyp. 2 = dubbel kapacitet, 1 = vanilla. Träder i kraft vid nästa kartladdning.",
            "모든 축사 유형의 최대 동물 수 배율. 2 = 두 배 수용량, 1 = 기본값. 다음 맵 로드 시 적용됩니다.",
            "すべての納屋タイプの最大動物数の倍率。2 = 2倍の収容量、1 = バニラ。次のマップ読み込み時に有効になります。",
            "Násobitel maximálního počtu zvířat v každém typu stáje. 2 = dvojnásobná kapacita, 1 = vanilla. Účinné při příštím načtení mapy.");

        // Static techName → ID map extracted from level1 binary.
        // Allows localized name lookup before TechCache is built (i.e. on game start).
        private static readonly Dictionary<string, int> _techNameToId = new Dictionary<string, int>
        {
            { "Stonecutting",                     3   },
            { "Dendrology",                        6   },  // display name: Silviculture
            { "Natural Philosophy",               7   },
            { "Spotters",                        10   },
            { "Hygiene",                         11   },
            { "Trailblazing",                    12   },
            { "Mortar-Reinforced Palisades",      13   },  // display name: Reinforced Palisades
            { "Artificial Selection",            14   },
            { "Disease Resistance",              15   },
            { "Double-Walled Hives",             16   },
            { "Foothold Traps",                  17   },
            { "Heavy Freight Wagons",            18   },
            { "Stiff-Blade Saw",                 19   },
            { "Wax-Sealed Barrels",              20   },
            { "Spindlewick Production",          21   },
            { "Production Logistics",            22   },
            { "Adjustable Shoe Lasts",           23   },
            { "Cast-Iron Axe Blades",            24   },
            { "Scientific Method",               25   },
            { "Tower Shields",                   26   },
            { "Soldier Training",                27   },
            { "Fortification Engineering",       28   },
            { "Architecture",                    29   },
            { "Deep Wells",                      30   },
            { "Ratting Dogs",                    31   },
            { "Midwives",                        32   },
            { "Drought Tolerance",               33   },
            { "Metallurgy",                      34   },
            { "Steel Surgical Tools",            35   },
            { "Animal Husbandry",                37   },
            { "Selective Breeding: Non-Grain",   38   },
            { "Selective Breeding: Grains",      39   },
            { "Iron-Rimmed Wheels",              40   },
            { "Treadwheel Crane",                41   },
            { "Sustainable Forestry",            42   },
            { "Spring Pole Lathe",               43   },
            { "Fire Assaying",                   44   },
            { "Advanced Metal-Casting",          45   },
            { "Printing Press",                  46   },
            { "Scientific Discovery",            47   },
            { "Wheel-Lock Crossbow",             48   },
            { "Horse Armor",                      49   },  // display name: Horse Barding
            { "Military Logistics",              50   },
            { "Civic Inspections",               51   },
            { "Masonry",                         52   },
            { "Beautification",                  54   },
            { "Coal-Fired Stoves",               55   },
            { "Alcohol Sterilization",           56   },
            { "Variolation",                     57   },
            { "Steel Tools",                     58   },
            { "Favored Nation",                  59   },
            { "Pharmaceutical Study",            60   },
            { "Intensive Animal Farming",        61   },
            { "Deep Mine Ventilation",           62   },
            { "Heat-Treated Halberds",           63   },
            { "Steel Armaments",                 64   },
            { "Conscription",                    66   },
            { "Glass Recycling",                 67   },
            { "Border Policies",                 68   },
            { "Convenience Tax",                 69   },
            { "Prohibition",                     70   },
            { "Rehabilitation",                  71   },
            { "Brigandine Armor",                73   },
            { "Economic Power",                  74   },
            { "Beacon of Culture",               75   },
            { "Gates of Valor",                  76   },
            { "Guilds",                          77   },
            { "Pharmacy",                        78   },
            { "Citadels",                        79   },
            { "Canning",                         81   },
            { "Blast Furnace",                   82   },
            { "Quarry",                          83   },
            { "Woodworking",                     84   },
            { "Caseiculture",                    85   },
            { "Animal Rearing",                  86   },
            { "Brewing",                         87   },
            { "Barrel-Making",                   88   },
            { "Trade Center",                    89   },
            { "Bookbinding",                     90   },
            { "Hospitalization",                 91   },
            { "School of Thought",               92   },
            { "Cavalry",                         93   },
            { "Reinforced Plating",              94   },
            { "Tools of War",                    95   },
            { "Glass Blowing",                   96   },
            { "Grain Crops",                     97   },
            { "Arrow Loops",                     98   },
            { "Arms Production",                 99   },
            { "Smithing",                       100   },
            { "Forging",                        101   },
            { "Structural Engineering",         102   },
            { "Market Forces",                  103   },
            { "Paper Press",                    104   },
            { "Nursing",                        105   },
            { "Stone Battlements",              106   },
            { "Marksman Training",              107   },
            { "Production Management",          108   },
            { "Mining",                         109   },
            { "Forestry",                       110   },
            { "Taxation",                       111   },
            { "Commerce Negotiations",          112   },
            { "Sustainable Farming",            113   },
            { "Battlement Fortifications",      114   },
            { "Iron Shares",                    116   },
            { "Command Structure",              117   },
            { "Vermicast",                      118   },
            { "Woodlore",                         2   },
            { "Sheet Composting",                 1   },
            { "Defensive Barricades",             9   },
            // Rodent-Proofing, Siege Warfare, Wonders of the Frontier — not present in TechTreeManager v1.9.4
        };

        // Returns the tech's localized display name as shown in the tech tree panel,
        // e.g. "Métallurgie" for French. Falls back to the internal techName if not found.
        internal static string GetLocalizedTechName(string techName)
        {
            // First try TechCache (available after map load — most accurate)
            if (TechCache.ByName.TryGetValue(techName, out var node))
            {
                string key1 = "TechTree_Tech" + node.GetId().ToString("D3");
                string loc1 = I2.Loc.LocalizationManager.GetTranslation(key1);
                if (!string.IsNullOrEmpty(loc1)) return loc1;
            }
            // Fall back to static ID map (available immediately on game start)
            if (_techNameToId.TryGetValue(techName, out int id))
            {
                string key2 = "TechTree_Tech" + id.ToString("D3");
                string loc2 = I2.Loc.LocalizationManager.GetTranslation(key2);
                if (!string.IsNullOrEmpty(loc2)) return loc2;
            }
            return techName;
        }

        internal static string RankEntry(string techName)
        {
            string loc = GetLocalizedTechName(techName);
            return Get(
                $"Max ranks for \"{loc}\".",
                $"Макс. рангов для \"{loc}\".",
                $"Maximale Ränge für \"{loc}\".",
                $"Rangs maximum pour \"{loc}\".",
                $"Rangos máximos para \"{loc}\".",
                $"Ranghi massimi per \"{loc}\".",
                $"Níveis máximos para \"{loc}\".",
                $"Maksymalne rangi dla \"{loc}\".",
                $"Maximala ranker för \"{loc}\".",
                $"\"{loc}\"의 최대 등급.",
                $"\"{loc}\"の最大ランク。",
                $"Maximální ranky pro \"{loc}\".");
        }
    }
    // ──────────────────────────────────────────────────────────────────────────
    public class TechRankExpander : MelonMod
    {
        private const string PREF_CATEGORY = "TechRankExpander";

        internal static TechRankExpander Instance { get; private set; }

        private MelonPreferences_Category _cat;
        private MelonPreferences_Entry<float> _kpSpeedEntry;
        private MelonPreferences_Entry<float> _carryCapEntry;
        private MelonPreferences_Entry<float> _workSpeedEntry;
        private MelonPreferences_Entry<bool>   _resetEntry;
        private MelonPreferences_Entry<bool>   _allotEntry;
        private MelonPreferences_Entry<int>     _deepWellsWaterEntry;
        private MelonPreferences_Entry<string>  _kpHotkeyEntry;
        private MelonPreferences_Entry<int>     _kpHotkeyAmountEntry;
        private MelonPreferences_Entry<int>     _maxWaxEntry;
        private MelonPreferences_Entry<float>   _livestockCapEntry;
        private readonly Dictionary<string, MelonPreferences_Entry<int>> _rankEntries =
            new Dictionary<string, MelonPreferences_Entry<int>>();

        public override void OnInitializeMelon()
        {
            Instance = this;
            _cat = MelonPreferences.CreateCategory(PREF_CATEGORY);
            _cat.SetFilePath("UserData/TechRankExpander.cfg");

            _kpSpeedEntry = _cat.CreateEntry(
                "KP_Speed_Multiplier", 5f,
                display_name: "KP Speed Multiplier",
                description: CfgL10n.KpSpeed());

            _carryCapEntry = _cat.CreateEntry(
                "Carry_Capacity_Multiplier", 3f,
                display_name: "Villager Carry Capacity Multiplier",
                description: CfgL10n.CarryCap());

            _workSpeedEntry = _cat.CreateEntry(
                "Work_Speed_Per_Rank", 0.01f,
                display_name: "Work Speed Bonus Per Rank (Production Management)",
                description: CfgL10n.WorkSpeed());

            _resetEntry = _cat.CreateEntry(
                "Reset_Tech_Tree", false,
                display_name: "Reset Tech Tree",
                description: CfgL10n.ResetTree());

            _allotEntry = _cat.CreateEntry(
                "Allot_All_Techs", false,
                display_name: "Allot All Techs",
                description: CfgL10n.AllotAll());

            _deepWellsWaterEntry = _cat.CreateEntry(
                "Deep_Wells_Water_Volume_Per_Rank", 50,
                display_name: "Deep Wells — Bonus Water Volume Per Rank",
                description: CfgL10n.DeepWells());

            _kpHotkeyEntry = _cat.CreateEntry(
                "KP_Hotkey", "F8",
                display_name: "KP Hotkey",
                description: CfgL10n.KpHotkey());

            _kpHotkeyAmountEntry = _cat.CreateEntry(
                "KP_Hotkey_Amount", 1,
                display_name: "KP Hotkey Amount",
                description: CfgL10n.KpHotkeyAmount());

            _maxWaxEntry = _cat.CreateEntry(
                "Max_Wax_Per_Barrel", 2,
                display_name: "Max Wax Per Barrel (Wax-Sealed Barrels)",
                description: CfgL10n.MaxWax());

            _livestockCapEntry = _cat.CreateEntry(
                "Livestock_Capacity_Multiplier", 2f,
                display_name: "Livestock Barn Capacity Multiplier [BETA]",
                description: CfgL10n.LivestockCap());

            foreach (var kv in TechDefaults.DefaultRanks)
            {
                string key = "Ranks_" + kv.Key
                    .Replace(" ", "_")
                    .Replace(":", "")
                    .Replace("-", "_")
                    .Replace("'", "");

                var entry = _cat.CreateEntry(key, kv.Value,
                    display_name: kv.Key,
                    description: CfgL10n.RankEntry(kv.Key));
                _rankEntries[kv.Key] = entry;
            }

            // Remove stale cfg keys written by older mod versions for techs that are now
            // unlock-only (hardcoded to 1). Leaving them causes player confusion — the value
            // appears editable but is silently ignored.
            PruneStaleConfigKeys();

            RefreshDescriptions();
            _cat.SaveToFile();
            RefreshRuntimeConfig();

            MelonLogger.Msg("[TechRankExpander] Config ready -> UserData/TechRankExpander.cfg");
            MelonLogger.Msg($"[TechRankExpander] Ready — {RuntimeConfig.ActiveRanks.Count} techs extended, KP x{RuntimeConfig.KpSpeedMultiplier}");
        }

        private void RefreshRuntimeConfig()
        {
            RuntimeConfig.ActiveRanks.Clear();
            foreach (var kv in _rankEntries)
                RuntimeConfig.ActiveRanks[kv.Key] = kv.Value.Value;

            // Hard-coded caps — not exposed in config to prevent game-breaking bugs.
            // Civic Inspections: -30% firefighter work per rank; rank 4+ = negative work time → firefighters stop.
            RuntimeConfig.ActiveRanks["Civic Inspections"] = 3;
            // Sheet Composting: -30% compost work per rank; rank 4+ = negative work time → Compost Yard breaks.
            RuntimeConfig.ActiveRanks["Sheet Composting"] = 3;
            // Hygiene: each rank is -25% disease probability; rank 4 = -100% (fully eliminated).
            // Rank 5+ would produce negative probability values, breaking disease mechanics.
            RuntimeConfig.ActiveRanks["Hygiene"] = 4;
            // Unlock-only techs — these only unlock a building/resource/unit on rank 1.
            // Unlock-only techs are NOT added to ActiveRanks — mod leaves their vanilla numRanks untouched.
            // Favored Nation: each rank is -10% trading-post sell price; rank 10 = -100% = zero gold from bazaar.
            // Rank 11+ = negative prices (items have negative sell value), breaking trade entirely.
            if (_rankEntries.TryGetValue("Favored Nation", out var favoredNationEntry))
                RuntimeConfig.ActiveRanks["Favored Nation"] = Mathf.Clamp(favoredNationEntry.Value, 1, 9);
            else
                RuntimeConfig.ActiveRanks["Favored Nation"] = 1;
            // Sustainable Farming: compound -25% fertility loss per rank.
            // Rank 3 = -75% loss (safe). Rank 4 = 0% loss (farms never deplete — edge case).
            // Rank 5+ = negative multiplier → fertility RESTORES over time (infinite fertility, broken).
            // Hard max = 3 to keep meaningful fertility loss and avoid rank-4 edge case.
            if (_rankEntries.TryGetValue("Sustainable Farming", out var sfEntry))
            {
                int sfClamped = Mathf.Clamp(sfEntry.Value, 1, 3);
                if (sfClamped != sfEntry.Value)
                    MelonLogger.Warning($"[TechRankExpander] Sustainable Farming rank clamped {sfEntry.Value} → {sfClamped} (rank 4+ eliminates or reverses fertility loss).");
                RuntimeConfig.ActiveRanks["Sustainable Farming"] = sfClamped;
            }
            else
                RuntimeConfig.ActiveRanks["Sustainable Farming"] = 3;

            RuntimeConfig.KpSpeedMultiplier      = _kpSpeedEntry.Value;
            RuntimeConfig.CarryCapacityMultiplier = _carryCapEntry.Value;
            RuntimeConfig.WorkSpeedPerRank        = _workSpeedEntry.Value;
            RuntimeConfig.ResetTechTree           = _resetEntry?.Value ?? false;
            RuntimeConfig.AllotAllTechs           = _allotEntry?.Value ?? false;
            RuntimeConfig.DeepWellsWaterVolumePerRank = _deepWellsWaterEntry?.Value ?? 50;

            if (System.Enum.TryParse(_kpHotkeyEntry.Value, true, out KeyCode kc))
                RuntimeConfig.KpHotkey = kc;
            else
            {
                MelonLogger.Warning($"[TechRankExpander] Unknown KeyCode '{_kpHotkeyEntry.Value}', using F8.");
                RuntimeConfig.KpHotkey = KeyCode.F8;
            }
            RuntimeConfig.KpHotkeyAmount = Mathf.Max(1, _kpHotkeyAmountEntry.Value);
            RuntimeConfig.MaxWaxPerBarrel = Mathf.Max(0, _maxWaxEntry.Value);
            RuntimeConfig.LivestockCapacityMultiplier = Mathf.Max(1f, _livestockCapEntry?.Value ?? 2f);
        }

        private string _lastDescriptionLanguage = null;

        // Updates cfg entry descriptions to the current game language and saves.
        // Skips the update if the language hasn't changed since the last call.
        internal void RefreshDescriptions()
        {
            string lang = I2.Loc.LocalizationManager.CurrentLanguage;
            if (string.IsNullOrEmpty(lang))
                lang = UnityEngine.PlayerPrefs.GetString("currentLanguage", "English");
            if (lang == _lastDescriptionLanguage) return;
            _lastDescriptionLanguage = lang;

            _kpSpeedEntry.Description        = CfgL10n.KpSpeed();
            _carryCapEntry.Description       = CfgL10n.CarryCap();
            _workSpeedEntry.Description      = CfgL10n.WorkSpeed();
            _resetEntry.Description          = CfgL10n.ResetTree();
            _allotEntry.Description          = CfgL10n.AllotAll();
            _deepWellsWaterEntry.Description = CfgL10n.DeepWells();
            _kpHotkeyEntry.Description       = CfgL10n.KpHotkey();
            _kpHotkeyAmountEntry.Description = CfgL10n.KpHotkeyAmount();
            _maxWaxEntry.Description         = CfgL10n.MaxWax();
            if (_livestockCapEntry != null)
                _livestockCapEntry.Description = CfgL10n.LivestockCap();
            foreach (var kv in _rankEntries)
                kv.Value.Description = CfgL10n.RankEntry(kv.Key);
            _cat.SaveToFile();
        }

        // Called from Patch_TechTreeManager_Load after the reset completes,
        // so the flag is cleared in the config file automatically.
        internal void ClearResetFlag()
        {
            if (_resetEntry == null) return;
            _resetEntry.Value = false;
            _cat.SaveToFile();
            MelonLogger.Msg("[TechRankExpander] Reset_Tech_Tree flag cleared in config.");
        }

        // Called from Patch_TechTreeManager_Load after allot-all completes.
        internal void ClearAllotFlag()
        {
            if (_allotEntry == null) return;
            _allotEntry.Value = false;
            _cat.SaveToFile();
            MelonLogger.Msg("[TechRankExpander] Allot_All_Techs flag cleared in config.");
        }

        // Removes cfg entries that exist on disk but are no longer valid (unlock-only techs
        // moved to hardcoded = 1, or techs removed in a game update). Without pruning, old
        // entries sit in the file with user-set values that look editable but are ignored.
        private void PruneStaleConfigKeys()
        {
            // Build set of valid keys: all _rankEntries keys converted to cfg key format
            var validKeys = new System.Collections.Generic.HashSet<string>();
            foreach (var k in _rankEntries.Keys)
            {
                validKeys.Add("Ranks_" + k
                    .Replace(" ", "_")
                    .Replace(":", "")
                    .Replace("-", "_")
                    .Replace("'", ""));
            }

            // Also keep the non-rank entries
            validKeys.Add("KP_Speed_Multiplier");
            validKeys.Add("Carry_Capacity_Multiplier");
            validKeys.Add("Work_Speed_Per_Rank");
            validKeys.Add("Reset_Tech_Tree");
            validKeys.Add("Allot_All_Techs");
            validKeys.Add("Deep_Wells_Water_Volume_Per_Rank");
            validKeys.Add("KP_Hotkey");
            validKeys.Add("KP_Hotkey_Amount");
            validKeys.Add("Max_Wax_Per_Barrel");
            validKeys.Add("Livestock_Capacity_Multiplier");

            var toRemove = new System.Collections.Generic.List<MelonPreferences_Entry>();
            foreach (var entry in _cat.Entries)
            {
                if (!validKeys.Contains(entry.Identifier))
                    toRemove.Add(entry);
            }

            foreach (var entry in toRemove)
            {
                _cat.DeleteEntry(entry.Identifier);
                MelonLogger.Msg($"[TechRankExpander] Pruned stale cfg key: {entry.Identifier}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Map") return;
            WorkSpeedHelper.Reset();
            Patch_LivestockBuilding_Start.ClearCache();
            RefreshRuntimeConfig();
        }

        public override void OnUpdate()
        {
            if (!Input.GetKeyDown(RuntimeConfig.KpHotkey)) return;
            var gm = UnitySingleton<GameManager>.Instance;
            if (gm?.techTreeManager == null) return;

            int before = gm.techTreeManager.knowledgePoints;
            gm.techTreeManager.AddKnowledgePoints(RuntimeConfig.KpHotkeyAmount, silent: true, bonusPoints: false);
            int added = gm.techTreeManager.knowledgePoints - before;

            if (added > 0)
                MelonLogger.Msg($"[TechRankExpander] +{added} KP (total: {gm.techTreeManager.knowledgePoints})");
            else
                MelonLogger.Msg("[TechRankExpander] KP not added — tier cap reached.");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != "Map") return;
            var gm = UnitySingleton<GameManager>.Instance;
            if (gm?.techTreeManager != null)
                WorkSpeedHelper.ApplyWorkSpeed(gm.techTreeManager);
            // Re-activate all tech buildings after full scene load (buildManager is now ready)
            TechBuildingHelper.ActivateTechBuildings();
        }
    }

    // ── Patch: language change → refresh cfg descriptions ────────────────────
    // The UI calls I2.Loc.LocalizationManager.CurrentLanguage = toggle.name directly.
    // Hook the I2Loc setter so cfg descriptions update immediately when the player
    // switches language in the options menu, without requiring a restart.
    [HarmonyPatch(typeof(I2.Loc.LocalizationManager), "set_CurrentLanguage")]
    internal static class Patch_LanguageChanged
    {
        static void Postfix()
        {
            TechRankExpander.Instance?.RefreshDescriptions();
            MelonLogger.Msg($"[TechRankExpander] Language changed → cfg descriptions updated ({I2.Loc.LocalizationManager.CurrentLanguage}).");
        }
    }
}
