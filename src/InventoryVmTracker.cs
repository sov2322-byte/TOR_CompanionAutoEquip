using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

namespace TOR_CompanionAutoEquip
{
    internal static class InventoryVmTracker
    {
        private static readonly FieldInfo CurrentCharacterField =
            typeof(SPInventoryVM).GetField("_currentCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo InventoryLogicField =
            typeof(SPInventoryVM).GetField("_inventoryLogic", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static SPInventoryVM Active { get; set; }

        internal static CharacterObject GetCurrentCharacter()
        {
            SPInventoryVM vm = Active;
            if (vm == null || CurrentCharacterField == null)
                return null;

            try
            {
                return CurrentCharacterField.GetValue(vm) as CharacterObject;
            }
            catch
            {
                return null;
            }
        }

        internal static InventoryLogic GetInventoryLogic()
        {
            SPInventoryVM vm = Active;
            if (vm == null || InventoryLogicField == null)
                return null;

            try
            {
                return InventoryLogicField.GetValue(vm) as InventoryLogic;
            }
            catch
            {
                return null;
            }
        }

        internal static InventoryLogic.InventorySide GetTargetSide()
        {
            SPInventoryVM vm = Active;
            if (vm == null)
                return InventoryLogic.InventorySide.BattleEquipment;

            switch ((SPInventoryVM.EquipmentModes)vm.EquipmentMode)
            {
                case SPInventoryVM.EquipmentModes.Civilian:
                    return InventoryLogic.InventorySide.CivilianEquipment;
                case SPInventoryVM.EquipmentModes.Stealth:
                    return InventoryLogic.InventorySide.StealthEquipment;
                default:
                    return InventoryLogic.InventorySide.BattleEquipment;
            }
        }

        internal static EquipmentModeSnapshot GetModeSnapshot()
        {
            SPInventoryVM vm = Active;
            if (vm == null)
                return EquipmentModeSnapshot.Battle;

            switch ((SPInventoryVM.EquipmentModes)vm.EquipmentMode)
            {
                case SPInventoryVM.EquipmentModes.Civilian:
                    return EquipmentModeSnapshot.Civilian;
                case SPInventoryVM.EquipmentModes.Stealth:
                    return EquipmentModeSnapshot.Stealth;
                default:
                    return EquipmentModeSnapshot.Battle;
            }
        }
    }

    internal enum EquipmentModeSnapshot
    {
        Civilian,
        Battle,
        Stealth
    }
}
