using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;

namespace MIR
{
    public class MIRMethod21Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemSpecificationPanel), "method_21");

        [PatchPrefix]
        public static bool PatchPrefix(ref KeyValuePair<EModLockedState, ModSlotView.GStruct430> __result, Slot slot)
        {
            string itemName = slot.ContainedItem != null ? slot.ContainedItem.Name.Localized() : string.Empty;
            ModSlotView.GStruct430 structValue = new ModSlotView.GStruct430
            {
                ItemName = itemName,
                Error = string.Empty
            };

            __result = new KeyValuePair<EModLockedState, ModSlotView.GStruct430>(EModLockedState.Unlocked, structValue);

            return false;
        }
    }

    public class InteractionsHandlerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionsHandlerClass), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct448<GClass3759> __result)
        {
            if (GClass2064.InRaid)
            {
                __result = GClass3759._;
                return false;
            }
            return true;
        }
    }

    public class ItemCheckAction : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Item), "CheckAction");

        [PatchPrefix]
        public static GStruct447 Prefix(ItemAddress location, ref GStruct447 __result, Item __instance)
        {
            __result = default(GStruct447);
            return new InteractionsHandlerClass.GClass3736(null);
        }
    }


    //need this one
    public class EFTInventoryLogicModPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Mod), "CanBeMoved");

        [PatchPrefix]
        public static bool Prefix(IContainer toContainer, ref GStruct448<bool> __result)
        {
            __result = true;
            return false;
        }
    }
    public class CanAcceptRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GClass2862), (nameof(GClass2862.CanAcceptRaid)));

        [PatchPostfix]
        public static void Postfix(ref bool __result, ref InventoryError error)
        {
            __result = true;
            error = null;
        }
    }

    //Gets rid of the compatibility check (even when ui highlights the slot as incompatible)
    public class SlotMethod5Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Slot), (nameof(Slot.method_5)));

        [PatchPrefix]
        public static bool Prefix(Item item, bool ignoreRestrictions, bool ignoreMalfunction, ref GStruct448<bool> __result, Slot __instance)
        {
            if (__instance.ContainedItem != null)
            {
                __result = new Slot.GClass3722(item, __instance);
                return false;
            }
            if (ignoreRestrictions)
            {
                __result = true;
                return false;
            }
            if (__instance.Locked)
            {
                __result = new Slot.GClass3715(__instance);
                return false;
            }
            InventoryError inventoryError;
            if (!__instance.CanAcceptRaid(out inventoryError))
            {
                __result = inventoryError;
                return false;
            }
            if (!__instance.method_2(item) && !(item is AmmoItemClass))
            {
                __result = new Slot.GClass3719(item, __instance);
                return false;
            }
            if (__instance.BlockerSlots.Count > 0)
            {
                __result = new Slot.GClass3716(item, __instance);
                return false;
            }
            GStruct448<bool> gstruct = __instance.method_4(item);
            if (gstruct.Failed)
            {
                __result = gstruct.Error;
                return false;
            }
            if (item.IsSpecialSlotOnly && !__instance.IsSpecial)
            {
                __result = new Slot.GClass3723(item, __instance);
                return false;
            }
            if (__instance.ConflictingSlots != null)
            {
                using (IEnumerator<Slot> enumerator = __instance.method_3(item).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Slot slot = enumerator.Current;
                        if (slot.ContainedItem != null)
                        {
                            __result = new Slot.GClass3717(item, __instance, slot);
                            return false;
                        }
                    }
                }
            }

            //check at parent level if we are dealing with armor by checking for ArmorHolderComponent
            var armorHolderComponent = __instance.ParentItem.GetItemComponent<ArmorHolderComponent>();
#if DEBUG
            bool isArmorHolderComponentBool = armorHolderComponent != null;
            //Logger.LogWarning("Checking Slot Compatibility: " + __instance.Name + " ArmorHolderComponent: " + isArmorHolderComponentBool + " Item: " + item.Name.Localized() + " IsArmorMod: " + item.IsArmorMod());
#endif 
            if (armorHolderComponent == null)
            {
                if (!__instance.CheckCompatibility(item))
                {
                    __result = new Slot.GClass3723(item, __instance);
                    return false;
                }
            }
            else
            {
                //we know it has armor holder component so we need to deal with child mod slots logic to make sure it can be placed in the slot

                //if the currently checked slot has colliders then it is an armor slot
                bool isArmorSlot = __instance.ArmorColliders.Length > 0;

                //we want to check if its acceptable for slot only if PIRMPlugin.AllowSwapAnyArmorPlate is false
#if DEBUG
                Logger.LogError("ArmorSlot: " + isArmorSlot + " IsArmorMod: " + item.IsArmorMod() + " IsModSuitable: " + armorHolderComponent.IsModSuitable(item));
#endif

                if (isArmorSlot && armorHolderComponent.IsModSuitable(item))
                {
                    //if no other item is there return true since we don't need to check weapons
                    if (__instance.ContainedItem == null)
                    {
                        __result = true;
                        return false;
                    }
                }
                else
                {
                    if (!__instance.CheckCompatibility(item))
                    {
                        __result = new Slot.GClass3723(item, __instance);
                        return false;
                    }
                }

            }

            Weapon weapon;
            if (!ignoreMalfunction && (weapon = __instance.ParentItem.GetRootItem() as Weapon) != null && weapon.IncompatibleByMalfunction(item))
            {
                __result = new InteractionsHandlerClass.GClass3734(item, weapon);
                return false;
            }
            Weapon weapon2;
            if ((weapon2 = item as Weapon) != null && __instance.ID != "BuildSlot")
            {
                List<Slot> list = weapon2.MissingVitalParts.ToList<Slot>();
                if (list.Any<Slot>())
                {
                    __result = new Slot.GClass3721(weapon2, __instance, list);
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
        public static bool Prefix(Item item, ArmorHolderComponent __instance, ref bool __result, CompoundItem ___compoundItem_0)
        {
            //if armormod and MIRPlugin.AllowSwapAnyArmorPlate is true then assume its suitable
            if (item.IsArmorMod() && MIRPlugin.AllowSwapAnyArmorPlate.Value)
            {
                __result = true;
                return false;
            }

            //return true to check the original logic
            return true;
        }
    }

    public class SlotRemoveItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(EFT.InventoryLogic.Slot), "RemoveItemInternal");
        private static bool Prefix(ref Slot __instance, ref GStruct446<GClass3134> __result, bool simulate, bool ignoreRestrictions)
        {
            return true;            
        }
    }

    public class LootItemApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(CompoundItem), "Apply");

        [PatchPrefix]
        private static bool Prefix(ref CompoundItem __instance, ref GStruct445 __result, TraderControllerClass itemController, Item item, int count, bool simulate) /// GStruct425 >> GStruct445
        {
            if (!item.ParentRecursiveCheck(__instance))
            {
                __result = new GClass3701(item, __instance);
                return false;
            }
            //bool inRaid = GClass1849.InRaid; /// GClass1849 >> GClass2064
            bool inRaid = false;

            Error error = null;
            Error error2 = null;

            Mod mod = item as Mod;
            Slot[] array = ((mod != null && inRaid) ? __instance.VitalParts.ToArray<Slot>() : null);
            Slot.GClass3721 gclass;

            if (inRaid && mod != null && !mod.RaidModdable)
            {
                error2 = new GClass3698(mod);
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
                        Slot.GClass3721 gclass2;
                        if ((gclass2 = error2 as Slot.GClass3721) != null)
                        {
                            error2 = new Slot.GClass3721(gclass2.Item, slot, gclass2.MissingParts);
                        }
                        flag = true;
                    }
                    else if (array != null && array.Contains(slot))
                    {
                        error = new GClass3699(mod);
                    }
                    else
                    {
                        ItemAddress to = slot.CreateItemAddress();
                        GStruct446<GClass3132> gstruct = InteractionsHandlerClass.Move(item, to, itemController, simulate);
                        if (gstruct.Succeeded)
                        {
                            __result = gstruct;
                            return false;
                        }
                        GStruct446<GClass3145> gstruct2 = InteractionsHandlerClass.SplitMax(item, int.MaxValue, to, itemController, itemController, simulate);
                        if (gstruct2.Succeeded)
                        {
                            __result = gstruct2;
                            return false;
                        }
                        error = gstruct.Error;
                        if (!GClass810.DisabledForNow && GClass3117.CanSwap(item, slot))
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
            GStruct446<GInterface385> gstruct3 = InteractionsHandlerClass.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<CompoundItem>(), InteractionsHandlerClass.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return false;
            }
            if (!(gstruct3.Error is GClass3601))
            {
                error = gstruct3.Error;
            }
            Error error3;
            if ((error3 = error2) == null)
            {
                error3 = error ?? new GClass3701(item, __instance);
            }
            __result = error3;
            return false;
        }
    }
}