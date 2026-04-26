using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(TechRankExpanderMod.TechRankExpander), "TechRankExpander", "1.3.0", "Modder")]
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
            { "Favored Nation",                  19 },  // -5% trade price per rank; rank 20 = -100% = zero export prices
            { "Steel Tools",                     9 },  // -10% item work per rank; >9 makes crafting work negative
            { "Variolation",                     20 },
            { "Alcohol Sterilization",           20 },
            { "Beautification",                  20 },
            { "Masonry",                          5 },  // -25% bricks per rank; at 4 ranks = -100% (free buildings)
            { "Civic Inspections",               20 },
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
            { "Hygiene",                         20 },
            { "Spotters",                        20 },
            { "Defensive Barricades",            20 },
            { "Militia",                         20 },
            { "Natural Philosophy",              20 },
            { "Dendrology",                      20 },
            { "Sustainable Fishing",             20 },
            { "Venting Chambers",                6 },  // -15% item work per rank; >6 makes crafting work negative
            { "Stonecutting",                     5 },  // -20% per rank; >5 makes mining time negative
            { "Woodlore",                        20 },
            // "Sheet Composting" excluded — has no additional effect beyond unlocking the building
        };
    }

    internal static class RuntimeConfig
    {
        internal static Dictionary<string, int> ActiveRanks = new Dictionary<string, int>();
        internal static float KpSpeedMultiplier = 5f;
        internal static float CarryCapacityMultiplier = 3f;
        internal static float WorkSpeedPerRank = 0.01f;  // +1% work speed per rank of Production Management
        internal static bool ResetTechTree = false;
        internal static KeyCode KpHotkey = KeyCode.F8;
        internal static int KpHotkeyAmount = 1;
    }

    // ── Patch: Villager.GetCarryCapacity ──────────────────────────────────────
    // Multiplies the final carry weight so villagers can haul more per trip.
    [HarmonyPatch(typeof(Villager), nameof(Villager.GetCarryCapacity))]
    internal static class Patch_VillagerCarryCapacity
    {
        static void Postfix(ref float __result)
        {
            __result *= RuntimeConfig.CarryCapacityMultiplier;
        }
    }
    // ──────────────────────────────────────────────────────────────────────────

    // ── Patch: GE_ManufacturingSourceItemModify.Activate ─────────────────────
    // "Wax Sealed Barrels" calls Activate(1) for every rank, incrementing the
    // wax-per-barrel requirement by 1 each time (rank 1 → 1 wax, rank 20 → 20).
    // This prefix skips the call when ItemWax is already present in the recipe,
    // keeping the requirement permanently at 1 regardless of rank count.
    [HarmonyPatch(typeof(GE_ManufacturingSourceItemModify), "Activate")]
    internal static class Patch_GE_ManufacturingSourceItemModify_Activate
    {
        static bool Prefix(GE_ManufacturingSourceItemModify __instance, float value)
        {
            if (value <= 0f) return true; // allow Deactivate path
            string itemName = Traverse.Create(__instance).Field("itemName").GetValue<string>();
            if (itemName != "ItemWax") return true; // only intercept wax
            ManufactureDefinition manuDef = Traverse.Create(__instance).Field("manuDef").GetValue<ManufactureDefinition>();
            if (manuDef == null) return true;
            foreach (SourceItemDefinition src in manuDef.sourceItems)
            {
                if (src.itemName == itemName)
                    return false; // already in recipe at 1 — skip
            }
            return true; // first activation: allow rank 1 to add wax (1)
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
                var prereq = __instance.techTreeNodeData.Find(x => x.GetId() == id);
                if (prereq == null || prereq.curRank < 1)
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
                // force: true so both Locked AND Unlocked techs are re-evaluated.
                // Without this, a tech stuck in Unlocked state (e.g. Monument)
                // would never be promoted to PrereqsMet.
                __instance.UpdatePrereqNodes(true);
            }
            else
            {
                var tech = __instance.techTreeNodeData.Find(x => x.GetId() == id);
                if (tech != null && tech.curRank >= 1)
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
            var tech = __instance.techTreeNodeData.Find(x => x.GetId() == id);
            if (tech == null || tech.GetTechName() != "Production Management") return;

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
            if (ttm.techTreeNodeData == null) return;

            var tech = ttm.techTreeNodeData.Find(x => x.GetTechName() == "Production Management");
            int ranks = (tech != null) ? tech.curRank : 0;
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
        private MelonPreferences_Entry<string>  _kpHotkeyEntry;
        private MelonPreferences_Entry<int>     _kpHotkeyAmountEntry;
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
                description: "How many times faster knowledge points are generated (e.g. 5 = 5x faster).");

            _carryCapEntry = _cat.CreateEntry(
                "Carry_Capacity_Multiplier", 3f,
                display_name: "Villager Carry Capacity Multiplier",
                description: "Multiplier on how much weight each villager can carry per trip (e.g. 3 = 3x more items).");

            _workSpeedEntry = _cat.CreateEntry(
                "Work_Speed_Per_Rank", 0.01f,
                display_name: "Work Speed Bonus Per Rank (Production Management)",
                description: "Bonus to all workers' speed per rank of Production Management purchased (0.01 = +1% per rank).");

            _resetEntry = _cat.CreateEntry(
                "Reset_Tech_Tree", false,
                display_name: "Reset Tech Tree",
                description: "Set to true to refund ALL spent KP and reset ALL tech ranks on the next map load. "
                           + "The flag is automatically cleared after applying. "
                           + "Use this when mod rank caps have changed and you want to redistribute points.");

            _kpHotkeyEntry = _cat.CreateEntry(
                "KP_Hotkey", "F8",
                display_name: "KP Hotkey",
                description: "Press this key in-game to instantly add KP_Hotkey_Amount knowledge points. "
                           + "Valid values: F1-F12, K, Insert, etc. (UnityEngine.KeyCode names).");

            _kpHotkeyAmountEntry = _cat.CreateEntry(
                "KP_Hotkey_Amount", 1,
                display_name: "KP Hotkey Amount",
                description: "How many knowledge points to add per key press.");

            foreach (var kv in TechDefaults.DefaultRanks)
            {
                string key = "Ranks_" + kv.Key
                    .Replace(" ", "_")
                    .Replace(":", "")
                    .Replace("-", "_")
                    .Replace("'", "");

                var entry = _cat.CreateEntry(key, kv.Value,
                    display_name: kv.Key,
                    description: $"Max ranks for \"{kv.Key}\" (original game value: 1-3).");
                _rankEntries[kv.Key] = entry;
            }

            _cat.SaveToFile();
            RefreshRuntimeConfig();

            MelonLogger.Msg("[TechRankExpander] Config ready -> UserData/TechRankExpander.cfg");
            MelonLogger.Msg($"[TechRankExpander] Patching GetNumRanks() for {RuntimeConfig.ActiveRanks.Count} techs, KP x{RuntimeConfig.KpSpeedMultiplier}");
        }

        private void RefreshRuntimeConfig()
        {
            RuntimeConfig.ActiveRanks.Clear();
            foreach (var kv in _rankEntries)
                RuntimeConfig.ActiveRanks[kv.Key] = kv.Value.Value;
            RuntimeConfig.KpSpeedMultiplier      = _kpSpeedEntry.Value;
            RuntimeConfig.CarryCapacityMultiplier = _carryCapEntry.Value;
            RuntimeConfig.WorkSpeedPerRank        = _workSpeedEntry.Value;
            RuntimeConfig.ResetTechTree           = _resetEntry?.Value ?? false;

            if (System.Enum.TryParse(_kpHotkeyEntry.Value, true, out KeyCode kc))
                RuntimeConfig.KpHotkey = kc;
            else
            {
                MelonLogger.Warning($"[TechRankExpander] Unknown KeyCode '{_kpHotkeyEntry.Value}', using F8.");
                RuntimeConfig.KpHotkey = KeyCode.F8;
            }
            RuntimeConfig.KpHotkeyAmount = Mathf.Max(1, _kpHotkeyAmountEntry.Value);
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

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Map") return;
            WorkSpeedHelper.Reset();
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