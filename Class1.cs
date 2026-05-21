using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(TechRankExpanderMod.TechRankExpander), "TechRankExpander", "1.9.0-beta", "Modder")]
[assembly: MelonGame("Crate Entertainment", "Farthest Frontier")]

namespace TechRankExpanderMod
{
    internal static class TechDefaults
    {
        internal static readonly Dictionary<string, int> DefaultRanks = new Dictionary<string, int>
        {
            { "Vermicast",                       20 },
            { "Command Structure",               20 },
            { "Iron Shares",                     20 },
            { "Sustainable Farming",             3 },  // -25% fertility loss per rank; >3 reverses fertility loss (infinite fertility)
            { "Taxation",                        20 },
            { "Production Management",           20 },
            { "Marksman Training",               20 },
            { "Structural Engineering",          20 },
            { "Animal Rearing",                  20 },
            { "Rehabilitation",                  20 },
            { "Border Policies",                 20 },
            { "Glass Recycling",                 20 },
            { "Steel Armaments",                 20 },
            { "Heat-Treated Halberds",           20 },
            { "Deep Mine Ventilation",           20 },
            { "Pharmaceutical Study",            20 },
            { "Favored Nation",                 1 },  // -10% sell price per rank; default 1 keeps trade safe.
            { "Steel Tools",                     9 },  // -10% item work per rank; >9 makes crafting work negative
            { "Variolation",                     20 },
            { "Alcohol Sterilization",           20 },
            { "Beautification",                  20 },
            { "Masonry",                          5 },  // -25% bricks per rank; at 4 ranks = -100% (free buildings)
            // "Civic Inspections" hardcoded to 3 in code — NOT configurable.
            // Each rank is -30% firefighter work time; rank 4 = -120% (negative) breaks firefighters.
            { "Military Logistics",               9 },  // -10% item work per rank; >9 makes crafting work negative
            { "Horse Armor",                     20 },
            { "Wheel-Lock Crossbow",             20 },
            { "Scientific Discovery",            20 },
            { "Printing Press",                  1 },  // -50% item work per rank; rank 2 = -100% = zero work
            { "Advanced Metal-Casting",          20 },
            { "Fire Assaying",                   20 },
            { "Spring Pole Lathe",                4 },  // -20% item work per rank; >4 makes crafting work zero/negative
            { "Sustainable Forestry",            20 },
            { "Treadwheel Crane",                20 },
            { "Iron-Rimmed Wheels",              20 },
            { "Selective Breeding: Grains",      20 },
            { "Selective Breeding: Non-Grain",   20 },
            { "Steel Surgical Tools",            20 },
            { "Metallurgy",                       9 },  // -10% item work per rank; >9 makes crafting work zero/negative
            { "Drought Tolerance",               20 },
            { "Midwives",                        20 },
            { "Ratting Dogs",                    20 },
            { "Deep Wells",                      20 },
            { "Architecture",                    20 },
            { "Fortification Engineering",       20 },
            { "Soldier Training",                20 },
            { "Tower Shields",                   20 },
            { "Scientific Method",               20 },
            { "Cast-Iron Axe Blades",            20 },
            { "Adjustable Shoe Lasts",            3 },  // -25% item work per rank; >3 makes crafting work zero/negative
            { "Production Logistics",             9 },  // -10% item work per rank; >9 makes crafting work negative
            { "Spindlewick Production",          20 },
            { "Wax-Sealed Barrels",              20 },
            { "Stiff-Blade Saw",                  4 },  // -20% item work per rank; >4 makes crafting work zero/negative
            { "Heavy Freight Wagons",            20 },
            { "Foothold Traps",                  20 },
            { "Double-Walled Hives",             20 },
            { "Disease Resistance",              20 },
            { "Artificial Selection",            20 },
            { "Mortar-Reinforced Palisades",     20 },
            { "Trailblazing",                    20 },
            // Hygiene capped at 4 in code — not configurable (rank 5+ breaks disease probability)
            // { "Hygiene",                         20 },
            { "Spotters",                        20 },
            { "Defensive Barricades",            20 },
            { "Militia",                         20 },
            { "Natural Philosophy",              20 },
            { "Dendrology",                      20 },
            { "Sustainable Fishing",             20 },
            { "Venting Chambers",                6 },  // -15% item work per rank; >6 makes crafting work negative
            { "Stonecutting",                     5 },  // -20% per rank; >5 makes mining time negative
            { "Woodlore",                        20 },
            // "Sheet Composting" hardcoded to 3 in code — NOT configurable.
            // Each rank is -30% compost work time; rank 4 = -120% (negative) breaks the Compost Yard.
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
        // Cleared on every scene load so a fresh map always re-applies the multiplier.
        private static readonly System.Collections.Generic.HashSet<int> _patchedSOs =
            new System.Collections.Generic.HashSet<int>();

        internal static void ClearCache() => _patchedSOs.Clear();

        static void Postfix(LivestockBuilding __instance)
        {
            float mult = RuntimeConfig.LivestockCapacityMultiplier;
            if (mult <= 1f) return;

            var sd = __instance.herdSetupData;
            if (sd == null) return;

            int soId = sd.GetInstanceID();
            int oldOver = sd.numLivestockToBeOverpopulated;

            if (!_patchedSOs.Contains(soId))
            {
                _patchedSOs.Add(soId);
                int newOver = Mathf.RoundToInt(oldOver * mult);
                if (newOver <= oldOver) return;   // multiplier rounds down to same value
                sd.numLivestockToBeOverpopulated = newOver;
                MelonLogger.Msg($"[TechRankExpander] {sd.name}: numLivestockToBeOverpopulated "
                    + $"{oldOver} -> {newOver} ({mult}x)");
            }

            // If this building's capacity was at the old vanilla max, raise it to the new max.
            int oldVanillaMax = oldOver - 1;
            if (__instance.userDefinedMaxLivestock == oldVanillaMax)
                __instance.userDefinedMaxLivestock = sd.numLivestockToBeOverpopulated - 1;
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
        static void Postfix(TechTreeManager __instance, int id, bool onLoad)
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
        static void Prefix() { TechResetHelper.InTechManagerLoad = true; }

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

            int count = 0;
            foreach (var tech in ttm.techTreeNodeData)
            {
                if (RuntimeConfig.ActiveRanks.TryGetValue(tech.GetTechName(), out int overrideRanks))
                {
                    _field.SetValue(tech, overrideRanks);
                    count++;
                }
            }
            MelonLogger.Msg($"[TechRankExpander] numRanks written on {count} tech nodes; cache built ({TechCache.ById.Count} entries).");
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
                description: "How many times faster knowledge points are generated (e.g. 5 = 5x faster). "
                           + "/ Во сколько раз быстрее генерируются очки знаний (например, 5 = в 5 раз быстрее).");

            _carryCapEntry = _cat.CreateEntry(
                "Carry_Capacity_Multiplier", 3f,
                display_name: "Villager Carry Capacity Multiplier",
                description: "Multiplier on how much weight each villager can carry per trip (e.g. 3 = 3x more items). "
                           + "/ Множитель переносимого веса для каждого жителя за поездку (например, 3 = в 3 раза больше предметов).");

            _workSpeedEntry = _cat.CreateEntry(
                "Work_Speed_Per_Rank", 0.01f,
                display_name: "Work Speed Bonus Per Rank (Production Management)",
                description: "Bonus to all workers' speed per rank of Production Management purchased (0.01 = +1% per rank). "
                           + "/ Бонус к скорости всех рабочих за каждый ранг 'Управления производством' (0.01 = +1% за ранг).");

            _resetEntry = _cat.CreateEntry(
                "Reset_Tech_Tree", false,
                display_name: "Reset Tech Tree",
                description: "Set to true to refund ALL spent KP and reset ALL tech ranks on the next map load. "
                           + "The flag is automatically cleared after applying. "
                           + "/ Установите true для возврата всех потраченных ОЗ и сброса всех рангов технологий при следующей загрузке карты. Флаг очищается автоматически.");

            _allotEntry = _cat.CreateEntry(
                "Allot_All_Techs", false,
                display_name: "Allot All Techs",
                description: "Set to true to instantly fill ALL tech ranks to their configured caps on the next map load (spends KP). "
                           + "The setting stays enabled until you turn it off manually. "
                           + "/ Установите true, чтобы при следующей загрузке карты все ранги технологий были выкуплены до максимума (тратит ОЗ). Настройка остаётся включённой, пока вы не выключите её вручную.");

            _deepWellsWaterEntry = _cat.CreateEntry(
                "Deep_Wells_Water_Volume_Per_Rank", 50,
                display_name: "Deep Wells — Bonus Water Volume Per Rank",
                description: "Extra water capacity added to every well per rank of the 'Deep Wells' technology. "
                           + "50 = +50 water per rank (e.g. rank 5 → +250 capacity). 0 = disabled. "
                           + "Bonus is applied when the map loads; buy more ranks and reload to see the change. "
                           + "/ Дополнительный объём воды в каждом колодце за каждый ранг технологии 'Глубокие колодцы'. "
                           + "50 = +50 воды за ранг (например, 5 рангов → +250 ёмкости). 0 = отключено. "
                           + "Бонус применяется при загрузке карты; купите ещё рангов и перезагрузите карту.");

            _kpHotkeyEntry = _cat.CreateEntry(
                "KP_Hotkey", "F8",
                display_name: "KP Hotkey",
                description: "Press this key in-game to instantly add KP_Hotkey_Amount knowledge points. "
                           + "Valid values: F1-F12, K, Insert, etc. (UnityEngine.KeyCode names). "
                           + "/ Нажмите эту клавишу в игре для мгновенного добавления очков знаний. "
                           + "Допустимые значения: F1-F12, K, Insert и т.д. (имена UnityEngine.KeyCode).");

            _kpHotkeyAmountEntry = _cat.CreateEntry(
                "KP_Hotkey_Amount", 1,
                display_name: "KP Hotkey Amount",
                description: "How many knowledge points to add per key press. "
                           + "/ Сколько очков знаний добавлять за одно нажатие клавиши.");

            _maxWaxEntry = _cat.CreateEntry(
                "Max_Wax_Per_Barrel", 2,
                display_name: "Max Wax Per Barrel (Wax-Sealed Barrels)",
                description: "Maximum wax (ItemWax) consumed per barrel production. "
                           + "1 = always 1 wax, 2 = capped at vanilla rank-1 value (default), 0 = removes wax from recipe entirely. "
                           + "/ Максимум воска (ItemWax) на производство одной бочки. "
                           + "1 = всегда 1 воск, 2 = не выше первого ранга ванили (по умолчанию), 0 = убрать воск из рецепта.");

            _livestockCapEntry = _cat.CreateEntry(
                "Livestock_Capacity_Multiplier", 2f,
                display_name: "Livestock Barn Capacity Multiplier [BETA]",
                description: "Multiplier on the maximum number of animals in every barn type "
                           + "(Barn, Stable, GoatBarn, ChickenCoop, Kennel). "
                           + "2 = double capacity (e.g. Barn 7 -> 14), 1 = vanilla. "
                           + "Change takes effect on next map load. "
                           + "/ Множитель максимального числа животных во всех типах построек "
                           + "(Амбар, Конюшня, Козлятник, Курятник, Псарня). "
                           + "2 = двойная вместимость, 1 = ваниль. Вступает в силу при следующей загрузке карты.");

            foreach (var kv in TechDefaults.DefaultRanks)
            {
                string key = "Ranks_" + kv.Key
                    .Replace(" ", "_")
                    .Replace(":", "")
                    .Replace("-", "_")
                    .Replace("'", "");

                var entry = _cat.CreateEntry(key, kv.Value,
                    display_name: kv.Key,
                    description: $"Max ranks for \"{kv.Key}\" (original game value: 1-3). / Макс. рангов для \"{kv.Key}\" (значение в ванили: 1-3).");
                _rankEntries[kv.Key] = entry;
            }

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
            // Favored Nation: each rank is -10% trading-post sell price; rank 10 = -100% = zero gold from bazaar.
            // Rank 11+ = negative prices (items have negative sell value), breaking trade entirely.
            if (_rankEntries.TryGetValue("Favored Nation", out var favoredNationEntry))
                RuntimeConfig.ActiveRanks["Favored Nation"] = Mathf.Clamp(favoredNationEntry.Value, 1, 9);
            else
                RuntimeConfig.ActiveRanks["Favored Nation"] = 1;

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
}
