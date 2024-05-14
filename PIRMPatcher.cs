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
        public static bool PatchPrefix(ref KeyValuePair<EModLockedState, ModSlotView.GStruct399> __result, Slot slot)
        {
            string itemName = slot.ContainedItem != null ? slot.ContainedItem.Name.Localized() : string.Empty;
            ModSlotView.GStruct399 structValue = new ModSlotView.GStruct399
            {
                ItemName = itemName,
                Error = string.Empty
            };

            __result = new KeyValuePair<EModLockedState, ModSlotView.GStruct399>(EModLockedState.Unlocked, structValue);

            return false;
        }
    }

    public class InteractionsHandlerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionsHandlerClass), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct416<GClass3348> __result)
        {

            if (GClass1849.InRaid)
            {
                __result = GClass3348._;
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
        public static bool Prefix(IContainer toContainer, ref GStruct416<bool> __result)
        {
            __result = true;
            return false;
        }
    }
    public class CanAcceptRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GClass2511), (nameof(GClass2511.CanAcceptRaid)));

        [PatchPostfix]
        public static void Postfix(ref bool __result, ref InventoryError error)
        {
            __result = true;
            error = null;
        }
    }
    public class SlotMethod_4Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Slot), nameof(Slot.method_4));

        [PatchPostfix]
        private static void Postfix(ref GStruct416<bool> __result, Item item, bool ignoreRestrictions = false, bool ignoreMalfunction = false)
        {
            if (__result.GetType() == typeof(Slot.GClass3308))
            {
                __result = true;
            }

        }

    }
    public class SlotRemoveItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Slot), nameof(Slot.RemoveItem));

        [PatchPrefix]
        public static bool Prefix(ref GStruct416<bool> __result, Slot __instance)
        {
            //display parent of item for logging purposes
            if (__instance.ContainedItem != null)
            {
                UnityEngine.Debug.LogWarning($"Parent of item: {__instance.ContainedItem.Parent}");
            }

            if (__instance.Locked)
            {
                __instance.Locked = false;

                /*Item containedItem = __instance.ContainedItem;

                if (!(containedItem is BulletClass) && !__instance.Examined(containedItem))
                {
                    __result = new Slot.GClass3313(containedItem, __instance);
                    return false;
                }
                Weapon weapon;

                if ((weapon = __instance.ParentItem.GetRootItem() as Weapon) != null && weapon.IncompatibleByMalfunction(containedItem))
                {
                    __result = new InteractionsHandlerClass.GClass3328(containedItem, weapon);
                    return false;
                }

                foreach (Slot slot in __instance.method_2(containedItem))
                {
                    slot.BlockerSlots.Remove(__instance);
                }

                containedItem.CurrentAddress = null;
                __instance.ContainedItem = null;
                
                var onAddOrRemoveItemField = AccessTools.Field(typeof(Slot), "OnAddOrRemoveItem");
                Action<Item, bool> onAddOrRemoveItem = (Action<Item, bool>)onAddOrRemoveItemField.GetValue(__instance);

                if (onAddOrRemoveItem != null)
                {
                    onAddOrRemoveItem(containedItem, false);
                }

                __result = true;
                return false;*/
            }

            return true;
        }
    }
    public class LootItemApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LootItemClass), nameof(LootItemClass.Apply));

        [PatchPrefix]
        public static void Postfix(ref GStruct413 __result, TraderControllerClass itemController, Item item, int count, bool simulate, LootItemClass __instance)
        {
            foreach (Slot slot in __instance.AllSlots)
            {
                GClass2767 gclass3 = new GClass2767(slot);

                GStruct414<GClass2786> gstruct = InteractionsHandlerClass.Move(item, gclass3, itemController, simulate);
                if (gstruct.Succeeded)
                {
                    __result = gstruct;
                    return;
                }

                GStruct414<GClass2795> gstruct2 = InteractionsHandlerClass.SplitMax(item, int.MaxValue, gclass3, itemController, itemController, simulate);
                if (gstruct2.Succeeded)
                {
                    __result = gstruct2;
                    return;
                }
            }

            GStruct414<GInterface324> gstruct3 = InteractionsHandlerClass.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable(), InteractionsHandlerClass.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return;
            }

        }

    }
}












