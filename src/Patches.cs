using System;
using HarmonyLib;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Engine.GauntletUI;

namespace TOR_CompanionAutoEquip
{
    [HarmonyPatch(typeof(SPInventoryVM), nameof(SPInventoryVM.RefreshValues))]
    internal static class SPInventoryVMRefreshPatch
    {
        private static void Postfix(SPInventoryVM __instance)
        {
            InventoryVmTracker.Active = __instance;
            InventoryOverlayPatch.RefreshLockState();
        }
    }


    [HarmonyPatch(typeof(SPInventoryVM), "UpdateCurrentCharacterIfPossible")]
    internal static class SPInventoryVMCharacterSwitchPatch
    {
        private static void Postfix(SPInventoryVM __instance, bool __result)
        {
            if (!__result)
                return;

            // Character switching updates SPInventoryVM._currentCharacter inside this method.
            // Refresh the overlay only after that assignment, so returning to a character
            // restores that character's session lock indicators correctly.
            InventoryVmTracker.Active = __instance;
            InventoryOverlayPatch.RefreshLockState();
        }
    }

    [HarmonyPatch(typeof(GauntletInventoryScreen))]
    internal static class InventoryOverlayPatch
    {
        private const int OverlayLayerZOrder = 1000;
        private const string PrefabName = "TORAutoEquipOverlay";

        private static GauntletLayer _layer;
        private static AutoEquipOverlayVM _viewModel;

        internal static void RefreshLockState()
        {
            if (_viewModel != null)
                _viewModel.RefreshLockState();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnInitialize")]
        private static void OnInitializePostfix(GauntletInventoryScreen __instance)
        {
            try
            {
                if (_layer != null)
                    return;

                _viewModel = new AutoEquipOverlayVM();
                _layer = new GauntletLayer("TOR_CompanionAutoEquip", OverlayLayerZOrder, true);
                _layer.InputRestrictions.SetInputRestrictions();
                _layer.LoadMovie(PrefabName, _viewModel);
                __instance.AddLayer(_layer);
                _viewModel.RefreshLockState();
            }
            catch
            {
                _layer = null;
                _viewModel = null;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePrefix(GauntletInventoryScreen __instance)
        {
            try
            {
                if (_layer != null)
                {
                    _layer.InputRestrictions.ResetInputRestrictions();
                    __instance.RemoveLayer(_layer);
                }
            }
            catch
            {
            }
            finally
            {
                _layer = null;
                if (_viewModel != null)
                {
                    try { _viewModel.OnFinalize(); } catch { }
                }
                _viewModel = null;
                InventoryVmTracker.Active = null;
            }
        }
    }
}
