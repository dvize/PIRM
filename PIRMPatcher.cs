using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
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


    //Gets rid of the compatibility check (even when ui highlights the slot as incompatible)
    public class SlotMethod4Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Slot), (nameof(Slot.method_4)));

        [PatchPrefix]
        public static bool Prefix(Item item, bool ignoreRestrictions, bool ignoreMalfunction, ref GStruct416<bool> __result, Slot __instance)
        {
            if (__instance.ContainedItem != null)
            {
                __result = new Slot.GClass3315(item, __instance);
                return false;
            }
            if (ignoreRestrictions)
            {
                __result = true;
                return false;
            }
            if (__instance.Locked)
            {
                __result = new Slot.GClass3308(__instance);
                return false;
            }
            InventoryError inventoryError;
            if (!__instance.CanAcceptRaid(out inventoryError))
            {
                __result = inventoryError;
                return false;
            }
            if (!__instance.Examined(item) && !(item is BulletClass))
            {
                __result = new Slot.GClass3312(item, __instance);
                return false;
            }
            //only do compatibility check if slot is not armor holding component slot
            var armorHolderComponent = __instance.ParentItem.GetItemComponent<ArmorHolderComponent>();
            if (armorHolderComponent == null)
            {
                if (!__instance.CheckCompatibility(item))
                {
                    __result = new Slot.GClass3316(item, __instance);
                    return false;
                }
            }

            if (__instance.BlockerSlots.Count > 0)
            {
                __result = new Slot.GClass3309(item, __instance);
                return false;
            }
            GStruct416<bool> gstruct = __instance.method_3(item);
            if (gstruct.Failed)
            {
                __result = gstruct.Error;
                return false;
            }
            if (item.IsSpecialSlotOnly && !__instance.IsSpecial)
            {
                __result = new Slot.GClass3316(item, __instance);
                return false;
            }
            if (__instance.ConflictingSlots != null)
            {
                using (IEnumerator<Slot> enumerator = __instance.method_2(item).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Slot slot = enumerator.Current;
                        if (slot.ContainedItem != null)
                        {
                            __result = new Slot.GClass3310(item, __instance, slot);
                            return false;
                        }
                    }
                }
            }
            Weapon weapon;
            if (!ignoreMalfunction && (weapon = __instance.ParentItem.GetRootItem() as Weapon) != null && weapon.IncompatibleByMalfunction(item))
            {
                __result = new InteractionsHandlerClass.GClass3328(item, weapon);
                return false;
            }
            Weapon weapon2;
            if ((weapon2 = item as Weapon) != null && __instance.Id != "BuildSlot")
            {
                List<Slot> list = weapon2.MissingVitalParts.ToList<Slot>();
                if (list.Any<Slot>())
                {
                    __result = new Slot.GClass3314(weapon2, __instance, list);
                    return false;
                }
            }
            __result = true;
            return false;
        }
    }
    public class IsModSuitablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ArmorHolderComponent), (nameof(ArmorHolderComponent.IsModSuitable)));

        [PatchPrefix]
        public static bool Prefix(Item item, ArmorHolderComponent __instance, ref bool __result, LootItemClass ___gclass2629_0)
        {
            if (IsArmorPlate(item))
            {
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }
        private static bool IsArmorPlate(Item item)
        {
            var armorComponent = item.GetItemComponent<ArmorComponent>();
            return armorComponent != null &&
                   armorComponent.GetArmorPlateColliders().Any() &&
                   item.GetItemComponent<HelmetComponent>() == null;
        }
    }

    
    public class SlotRemoveItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Slot), nameof(Slot.RemoveItem));

        [PatchPrefix]
        public static bool Prefix(ref GStruct416<bool> __result, Slot __instance)
        {
            //display parent of item for logging purposes
            /* if (__instance.ContainedItem != null)
            {
                UnityEngine.Debug.LogWarning($"Parent of item: {__instance.ContainedItem.Parent}");
            }*/
            if (PIRMPlugin.AllowSwapAnyArmorPlate.Value)
            {
                if (__instance.Locked)
                {
                    __instance.Locked = false;
                }
            }
            
            return true;
        }
    }
    public class LootItemApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LootItemClass), "Apply");

        [PatchPrefix]
        private static bool Prefix(ref LootItemClass __instance, ref GStruct413 __result, TraderControllerClass itemController, Item item, int count, bool simulate)
        {
            if (!item.ParentRecursiveCheck(__instance))
            {
                __result = new GClass3300(item, __instance);
                return false;
            }
            //bool inRaid = GClass1849.InRaid;
            bool inRaid = false;

            Error error = null;
            Error error2 = null;

            Mod mod = item as Mod;
            Slot[] array = ((mod != null && inRaid) ? __instance.VitalParts.ToArray<Slot>() : null);
            Slot.GClass3314 gclass;

            if (inRaid && mod != null && !mod.RaidModdable)
            {
                error2 = new GClass3297(mod);
            }
            else if (!InteractionsHandlerClass.CheckMissingParts(mod, __instance.CurrentAddress, itemController, out gclass))
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
                        Slot.GClass3314 gclass2;
                        if ((gclass2 = error2 as Slot.GClass3314) != null)
                        {
                            error2 = new Slot.GClass3314(gclass2.Item, slot, gclass2.MissingParts);
                        }
                        flag = true;
                    }
                    else if (array != null && array.Contains(slot))
                    {
                        error = new GClass3298(mod);
                    }
                    else
                    {
                        GClass2767 gclass3 = new GClass2767(slot);
                        GStruct414<GClass2786> gstruct = InteractionsHandlerClass.Move(item, gclass3, itemController, simulate);
                        if (gstruct.Succeeded)
                        {
                            __result = gstruct;
                            return false;
                        }
                        GStruct414<GClass2795> gstruct2 = InteractionsHandlerClass.SplitMax(item, int.MaxValue, gclass3, itemController, itemController, simulate);
                        if (gstruct2.Succeeded)
                        {
                            __result = gstruct2;
                            return false;
                        }
                        error = gstruct.Error;
                        if (!GClass747.DisabledForNow && GClass2775.CanSwap(item, slot))
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
            GStruct414<GInterface324> gstruct3 = InteractionsHandlerClass.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<LootItemClass>(), InteractionsHandlerClass.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return false;
            }
            if (!(gstruct3.Error is GClass3293))
            {
                error = gstruct3.Error;
            }
            Error error3;
            if ((error3 = error2) == null)
            {
                error3 = error ?? new GClass3300(item, __instance);
            }
            __result = error3;
            return false;
        }

    }

}












