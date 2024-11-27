using System;
using BepInEx;
using BepInEx.Configuration;

namespace MIR
{

    [BepInPlugin("com.dvize.MIR", "dvize.MIR", "3.0.0")]
    //[BepInDependency("com.spt-aki.core", "3.7.4")]
    class MIRPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<Boolean> AllowSwapAnyArmorPlate
        {
            get; set;
        }
        private void Awake()
        {
            AllowSwapAnyArmorPlate = Config.Bind("1. Main Settings", "AllowSwapAnyArmorPlate", false,
                new ConfigDescription("Allows swapping different types of armor plates in eachothers slots", null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));
        }
        private void Start()
        {
            new MIRMethod21Patch().Enable();
            new InteractionsHandlerPatch().Enable();
            /// new ItemCheckAction().Enable();
            new EFTInventoryLogicModPatch().Enable();
            new LootItemApplyPatch().Enable();
            new SlotRemoveItemPatch().Enable();
            new CanAcceptRaidPatch().Enable();

            new SlotMethod5Patch().Enable();
            new IsModSuitablePatch().Enable();
            
        }


    }
}
