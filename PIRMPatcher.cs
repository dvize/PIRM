using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
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

    public class PIRMGlass2428Smethod1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GClass2672), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct372<GClass3145> __result)
        {

            if (GClass1819.InRaid)
            {
                __result = GClass3145._;
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
        public static bool Prefix(IContainer toContainer, ref GStruct372<bool> __result)
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
        private static bool Prefix(ref LootItemClass __instance, ref GStruct370 __result, TraderControllerClass itemController, Item item, int count, bool simulate)
        {
            if (!item.ParentRecursiveCheck(__instance))
            {
                __result = new GClass3102(item, __instance);
                return false;
            }
            //bool inRaid = GClass1819.InRaid;
            bool inRaid = false;
            
            GClass3070 gclass = null;
            GClass3070 gclass2 = null;
            Mod mod = item as Mod;
            Slot[] array = ((mod != null && inRaid) ? __instance.VitalParts.ToArray<Slot>() : null);
            Slot.GClass3113 gclass3;
            if (inRaid && mod != null && !mod.RaidModdable)
            {
                gclass2 = new GClass3099(mod);
            }
            else if (!GClass2672.CheckMissingParts(mod, __instance.CurrentAddress, itemController, out gclass3))
            {
                gclass2 = gclass3;
            }
            bool flag = false;
            foreach (Slot slot in __instance.AllSlots)
            {
                if ((gclass2 == null || !flag) && slot.CanAccept(item))
                {
                    if (gclass2 != null)
                    {
                        Slot.GClass3113 gclass4;
                        if ((gclass4 = gclass2 as Slot.GClass3113) != null)
                        {
                            gclass2 = new Slot.GClass3113(gclass4.Item, slot, gclass4.MissingParts);
                        }
                        flag = true;
                    }
                    else if (array != null && array.Contains(slot))
                    {
                        gclass = new GClass3100(mod);
                    }
                    else
                    {
                        GClass2665 gclass5 = new GClass2665(slot);
                        GStruct371<GClass2684> gstruct = GClass2672.Move(item, gclass5, itemController, simulate);
                        if (gstruct.Succeeded)
                        {
                            __result = gstruct;
                            return false;
                        }
                        GStruct371<GClass2693> gstruct2 = GClass2672.SplitMax(item, int.MaxValue, gclass5, itemController, itemController, simulate);
                        if (gstruct2.Succeeded)
                        {
                            __result = gstruct2;
                            return false;
                        }
                        gclass = gstruct.Error;
                        if (!GClass780.DisabledForNow && GClass2673.CanSwap(item, slot))
                        {
                            __result = null;
                            return false;
                        }
                    }
                }
            }
            if (!flag)
            {
                gclass2 = null;
            }
            GStruct371<GInterface277> gstruct3 = GClass2672.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<LootItemClass>(), GClass2672.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return false;
            }
            if (!(gstruct3.Error is GClass3095))
            {
                gclass = gstruct3.Error;
            }
            GClass3070 gclass6;
            if ((gclass6 = gclass2) == null)
            {
                gclass6 = gclass ?? new GClass3102(item, __instance);
            }
            __result = gclass6;
            return false;
        }

    }

}












