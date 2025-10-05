using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Brad522.ModUtils.Harmony;

namespace Erenshor_LootRestriction
{
    [BepInPlugin(ModGUID, ModDescription, ModVersion)]
    public class LootRestriction : BaseUnityPlugin
    {
        internal const string ModName = "LootRestriction (Auto-Remove Corpses)";
        internal const string ModVersion = "1.0.0";
        internal const string ModDescription = "Loot Restriction Auto-Remove Corpses";
        internal const string Author = "Brad522";
        private const string ModGUID = Author + "." + ModName;

        //public static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private readonly Harmony harmony = new Harmony(ModGUID);

        public void Awake()
        {
            harmony.PatchAll();

            Logger.LogMessage("LootRestriction loaded successfully!");
        }

        [HarmonyPatch(typeof(Character))]
        [HarmonyPatch("DoDeath")]
        [HarmonyPriority(Priority.Last)]
        class LootRestrictionPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var matcher = new CodeMatcher(instructions, il);

                if (matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_3),
                    new CodeMatch(OpCodes.Brfalse),
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Call),
                    new CodeMatch(OpCodes.Brfalse),
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Dup),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Add)).IsValid)
                {
                    matcher.Advance(1)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Dup))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(
                            OpCodes.Call,
                            AccessTools.Method(typeof(Character_Extension), nameof(Character_Extension.SetExtra), new[] { typeof(bool), typeof(Character) })
                        ));
                }

                TranspilerUtils.FixLeaveNops(matcher);

                return matcher.InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(Character))]
        public static class Character_Extension
        {
            private static readonly ConditionalWeakTable<Character, Holder> extraData = new ConditionalWeakTable<Character, Holder>();

            private class Holder
            {
                public bool PlayerInvolved;
            }

            public static void SetExtra(bool value, Character instance)
                => extraData.GetOrCreateValue(instance).PlayerInvolved = value;

            public static bool GetExtra(Character instance)
                => extraData.TryGetValue(instance, out var h) && h.PlayerInvolved;
        }

        //[HarmonyPatch(typeof(RotChest))]
        //[HarmonyPatch("FixedUpdate")]
        //public static class RotChestPatch
        //{
        //    public static bool Prefix(RotChest __instance)
        //    {
        //        var go = __instance.gameObject;
        //        if (go == null)
        //            return true;

        //        var character = go.GetComponent<Character>() ?? go.GetComponentInParent<Character>();

        //        if (character != null)
        //        {
        //            if (!Character_Extension.GetExtra(character))
        //            {
        //                CorpseDataManager.RemoveCorpseData(character.savedCorpse);
        //                Object.Destroy(go);
        //                return false;
        //            }
        //            return true;
        //        }

        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(NPC))]
        [HarmonyPatch("Update")]
        public static class NPCPatch
        {
            public static void Postfix(NPC __instance)
            {
                if (__instance == null)
                    return;

                if (__instance.CheckLiving())
                    return;

                var npcChar = __instance.GetChar();

                bool playerInvolved = Character_Extension.GetExtra(npcChar);

                if (playerInvolved)
                    return;

                bool shouldDelete =
                    (!__instance.SimPlayer ||
                    (__instance.SimPlayer &&
                    !__instance.ThisSim.InGroup &&
                    !GameData.SimMngr.IsSimGrouped(GameData.SimMngr.Sims[__instance.ThisSim.myIndex])));

                if (shouldDelete)
                {
                    if (GameData.AttackingPlayer.Contains(__instance))
                    {
                        GameData.AttackingPlayer.Remove(__instance);
                    }
                    if (GameData.GroupMatesInCombat.Contains(__instance))
                    {
                        GameData.GroupMatesInCombat.Remove(__instance);
                    }
                    CorpseDataManager.RemoveCorpseData(npcChar.savedCorpse);
                    Object.Destroy(__instance.gameObject);
                }
            }
        }
    }
}
