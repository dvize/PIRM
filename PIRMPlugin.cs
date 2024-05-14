using BepInEx;

namespace PIRM
{

    [BepInPlugin("com.dvize.PIRM", "dvize.PIRM", "1.8.0")]
    //[BepInDependency("com.spt-aki.core", "3.7.4")]
    class PIRMPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
        }
        private void Start()
        {
            new PIRMMethod17Patch().Enable();
            new InteractionsHandlerPatch().Enable();
            new ItemCheckAction().Enable();
            new EFTInventoryLogicModPatch().Enable();
            new LootItemApplyPatch().Enable();
            new SlotMethod_4Patch().Enable();
            new SlotRemoveItemPatch().Enable();
            new CanAcceptRaidPatch().Enable();
        }


    }
}
