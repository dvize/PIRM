﻿using System;
using System.Diagnostics;
using BepInEx;
using VersionChecker;

namespace PIRM
{

    [BepInPlugin("com.dvize.PIRM", "dvize.PIRM", "1.5.5")]
    class PIRMPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            CheckEftVersion();
        }
        private void Start()
        {
            new PIRMMethod17Patch().Enable();
            new PIRMGlass2428Smethod1().Enable();
            new SlotViewMethod2().Enable();
            new EFTInventoryLogicModPatch().Enable();

        }

        private void CheckEftVersion()
        {
            // Make sure the version of EFT being run is the correct version
            int currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
            int buildVersion = TarkovVersion.BuildVersion;
            if (currentVersion != buildVersion)
            {
                Logger.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                EFT.UI.ConsoleScreen.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                throw new Exception($"Invalid EFT Version ({currentVersion} != {buildVersion})");
            }
        }
    }
}
