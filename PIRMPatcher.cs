using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;

namespace PIRM
{
    public class PIRMMethod17Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemSpecificationPanel), "method_17");

        [PatchPrefix]
        public static bool PatchPrefix(ref KeyValuePair<EModLockedState, string> __result, Slot slot)
        {
            string text = ((slot.ContainedItem != null) ? slot.ContainedItem.Name.Localized(null) : string.Empty);
            __result = new KeyValuePair<EModLockedState, string>(EModLockedState.Unlocked, text);

            return false;
        }
    }

    public class PIRMGlass2584Smethod1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GClass2584), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct376<GClass3071> __result)
        {

            if (GClass1716.InRaid)
            {
                __result = GClass3071._;
                return false;
            }

            return true;
        }
    }

    public class ItemCheckAction : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Item), "CheckAction");

        [PatchPrefix]
        public static bool Prefix(ItemAddress location, ref bool __result)
        {
            // Set the result to true and return false to skip the original method
            __result = true;
            return false;
        }

    }


    //need this one
    public class EFTInventoryLogicModPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Mod), "CanBeMoved");

        [PatchPrefix]
        public static bool Prefix(IContainer toContainer, ref GStruct376<bool> __result)
        {
            __result = true;
            return false;
        }

    }


    public class SlotMethod_2Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Slot), "method_2");

        [PatchPrefix]
        private static bool Prefix(ref Item item, ref bool ignoreRestrictions, ref bool ignoreMalfunction)
        {
            ignoreRestrictions = true;
            ignoreMalfunction = true;
            return true;
        }

    }

    public class LootItemApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LootItemClass), "Apply");

        [PatchPrefix]
        private static bool Prefix(ref LootItemClass __instance, ref GStruct374 __result, TraderControllerClass itemController, Item item, int count, bool simulate)
        {
            if (!item.ParentRecursiveCheck(__instance))
            {
                __result = new GClass3027(item, __instance);
                return false;
            }
            //bool inRaid = GClass1819.InRaid;
            bool inRaid = false;

            Error error = null;
            Error error2 = null;

            Mod mod = item as Mod;
            Slot[] array = ((mod != null && inRaid) ? __instance.VitalParts.ToArray<Slot>() : null);
            Slot.GClass3038 gclass;

            if (inRaid && mod != null && !mod.RaidModdable)
            {
                error2 = new GClass3024(mod);
            }
            else if (!GClass2584.CheckMissingParts(mod, __instance.CurrentAddress, itemController, out gclass))
            {
                error2 = gclass;
            }

            bool flag = false;
            foreach (Slot slot in __instance.AllSlots)
            {
                if ((error2 == null || !flag) && slot.CanAccept(item))
                {
                    if (error2 != null)
                    {
                        Slot.GClass3038 gclass2;
                        if ((gclass2 = error2 as Slot.GClass3038) != null)
                        {
                            error2 = new Slot.GClass3038(gclass2.Item, slot, gclass2.MissingParts);
                        }
                        flag = true;
                    }
                    else if (array != null && array.Contains(slot))
                    {
                        error = new GClass3025(mod);
                    }
                    else
                    {
                        GClass2577 gclass3 = new GClass2577(slot);
                        GStruct375<GClass2596> gstruct = GClass2584.Move(item, gclass3, itemController, simulate);
                        if (gstruct.Succeeded)
                        {
                            __result = gstruct;
                            return false;
                        }
                        GStruct375<GClass2605> gstruct2 = GClass2584.SplitMax(item, int.MaxValue, gclass3, itemController, itemController, simulate);
                        if (gstruct2.Succeeded)
                        {
                            __result = gstruct2;
                            return false;
                        }
                        error = gstruct.Error;
                        if (!GClass668.DisabledForNow && GClass2585.CanSwap(item, slot))
                        {
                            __result = null;
                            return false;
                        }
                    }
                }
            }
            if (!flag)
            {
                error2 = null;
            }
            GStruct375<GInterface275> gstruct3 = GClass2584.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<LootItemClass>(), GClass2584.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return false;
            }
            if (!(gstruct3.Error is GClass3020))
            {
                error = gstruct3.Error;
            }
            Error error3;
            if ((error3 = error2) == null)
            {
                error3 = error ?? new GClass3027(item, __instance);
            }
            __result = error3;
            return false;
        }

    }

}












