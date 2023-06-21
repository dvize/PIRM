using System.Collections.Generic;
using System.Reflection;
using Aki.Reflection.Patching;
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



    // THIS METHOD IS Causing an issue of not being able to move items back at all probably.... or maybe need new one for itemspecifcationpanel
    public class PIRMGlass2428Smethod1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GClass2428), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct323<GClass2898> __result)
        {

            if (GClass1756.InRaid)
            {
                __result = GClass2898._;
                return false;
            }

            return true;
        }
    }

    public class SlotViewMethod2 : ModulePatch
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
        public static bool Prefix(IContainer toContainer, ref GStruct323<bool> __result)
        {
            __result = true;
            return false;
        }

    }



}












