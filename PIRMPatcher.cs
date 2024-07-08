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

namespace PIRM
{
    public class PIRMMethod17Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemSpecificationPanel), "method_17");

        [PatchPrefix]
        public static bool PatchPrefix(ref KeyValuePair<EModLockedState, ModSlotView.GStruct398> __result, Slot slot)
        {
            string itemName = slot.ContainedItem != null ? slot.ContainedItem.Name.Localized() : string.Empty;
            ModSlotView.GStruct398 structValue = new ModSlotView.GStruct398
            {
                ItemName = itemName,
                Error = string.Empty
            };

            __result = new KeyValuePair<EModLockedState, ModSlotView.GStruct398>(EModLockedState.Unlocked, structValue);

            return false;
        }
    }

    public class InteractionsHandlerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InteractionsHandlerClass), "smethod_1");

        [PatchPrefix]
        public static bool Prefix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct416<GClass3372> __result)
        {
            if (GClass1864.InRaid)
            {
                __result = GClass3372._;
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
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GClass2525), (nameof(GClass2525.CanAcceptRaid)));

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
                __result = new Slot.GClass3339(item, __instance);
                return false;
            }
            if (ignoreRestrictions)
            {
                __result = true;
                return false;
            }
            if (__instance.Locked)
            {
                __result = new Slot.GClass3332(__instance);
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
                __result = new Slot.GClass3336(item, __instance);
                return false;
            }
            if (__instance.BlockerSlots.Count > 0)
            {
                __result = new Slot.GClass3333(item, __instance);
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
                __result = new Slot.GClass3340(item, __instance);
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
                            __result = new Slot.GClass3334(item, __instance, slot);
                            return false;
                        }
                    }
                }
            }

            //check at parent level if we are dealing with armor by checking for ArmorHolderComponent
            var armorHolderComponent = __instance.ParentItem.GetItemComponent<ArmorHolderComponent>();
#if DEBUG
            bool isArmorHolderComponentBool = armorHolderComponent != null;
            Logger.LogWarning("Checking Slot Compatibility: " + __instance.Name + " ArmorHolderComponent: " + isArmorHolderComponentBool + " Item: " + item.Name.Localized() + " IsArmorMod: " + item.IsArmorMod());
#endif 
            if (armorHolderComponent == null)
            {
                if (!__instance.CheckCompatibility(item))
                {
                    __result = new Slot.GClass3340(item, __instance);
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
                        __result = new Slot.GClass3340(item, __instance);
                        return false;
                    }
                }

            }

            Weapon weapon;
            if (!ignoreMalfunction && (weapon = __instance.ParentItem.GetRootItem() as Weapon) != null && weapon.IncompatibleByMalfunction(item))
            {
                __result = new InteractionsHandlerClass.GClass3352(item, weapon);
                return false;
            }
            Weapon weapon2;
            if ((weapon2 = item as Weapon) != null && __instance.Id != "BuildSlot")
            {
                List<Slot> list = weapon2.MissingVitalParts.ToList<Slot>();
                if (list.Any<Slot>())
                {
                    __result = new Slot.GClass3338(weapon2, __instance, list);
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
        public static bool Prefix(Item item, ArmorHolderComponent __instance, ref bool __result, LootItemClass ___lootItemClass)
        {
            //if armormod and PIRMPlugin.AllowSwapAnyArmorPlate is true then assume its suitable
            if (item.IsArmorMod() && PIRMPlugin.AllowSwapAnyArmorPlate.Value)
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
                __result = new GClass3324(item, __instance);
                return false;
            }
            //bool inRaid = GClass1849.InRaid;
            bool inRaid = false;

            Error error = null;
            Error error2 = null;

            Mod mod = item as Mod;
            Slot[] array = ((mod != null && inRaid) ? __instance.VitalParts.ToArray<Slot>() : null);
            Slot.GClass3338 gclass;

            if (inRaid && mod != null && !mod.RaidModdable)
            {
                error2 = new GClass3321(mod);
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
                        Slot.GClass3338 gclass2;
                        if ((gclass2 = error2 as Slot.GClass3338) != null)
                        {
                            error2 = new Slot.GClass3338(gclass2.Item, slot, gclass2.MissingParts);
                        }
                        flag = true;
                    }
                    else if (array != null && array.Contains(slot))
                    {
                        error = new GClass3322(mod);
                    }
                    else
                    {
                        GClass2783 gclass3 = new GClass2783(slot);
                        GStruct414<GClass2802> gstruct = InteractionsHandlerClass.Move(item, gclass3, itemController, simulate);
                        if (gstruct.Succeeded)
                        {
                            __result = gstruct;
                            return false;
                        }
                        GStruct414<GClass2811> gstruct2 = InteractionsHandlerClass.SplitMax(item, int.MaxValue, gclass3, itemController, itemController, simulate);
                        if (gstruct2.Succeeded)
                        {
                            __result = gstruct2;
                            return false;
                        }
                        error = gstruct.Error;
                        if (!GClass748.DisabledForNow && GClass2791.CanSwap(item, slot))
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
            GStruct414<GInterface339> gstruct3 = InteractionsHandlerClass.QuickFindAppropriatePlace(item, itemController, __instance.ToEnumerable<LootItemClass>(), InteractionsHandlerClass.EMoveItemOrder.Apply, simulate);
            if (gstruct3.Succeeded)
            {
                __result = gstruct3;
                return false;
            }
            if (!(gstruct3.Error is GClass3317))
            {
                error = gstruct3.Error;
            }
            Error error3;
            if ((error3 = error2) == null)
            {
                error3 = error ?? new GClass3324(item, __instance);
            }
            __result = error3;
            return false;
        }

    }

}












