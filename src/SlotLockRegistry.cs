using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TOR_CompanionAutoEquip
{
    // Session-only slot locks. Nothing is written to the campaign save.
    internal static class SlotLockRegistry
    {
        private static readonly Dictionary<Hero, Dictionary<EquipmentModeSnapshot, HashSet<EquipmentIndex>>> Locks =
            new Dictionary<Hero, Dictionary<EquipmentModeSnapshot, HashSet<EquipmentIndex>>>();

        internal static bool IsLocked(Hero hero, EquipmentModeSnapshot mode, EquipmentIndex slot)
        {
            if (hero == null)
                return false;

            Dictionary<EquipmentModeSnapshot, HashSet<EquipmentIndex>> byMode;
            HashSet<EquipmentIndex> slots;
            return Locks.TryGetValue(hero, out byMode)
                && byMode.TryGetValue(mode, out slots)
                && slots.Contains(slot);
        }

        internal static bool Toggle(Hero hero, EquipmentModeSnapshot mode, EquipmentIndex slot)
        {
            if (hero == null)
                return false;

            Dictionary<EquipmentModeSnapshot, HashSet<EquipmentIndex>> byMode;
            if (!Locks.TryGetValue(hero, out byMode))
            {
                byMode = new Dictionary<EquipmentModeSnapshot, HashSet<EquipmentIndex>>();
                Locks[hero] = byMode;
            }

            HashSet<EquipmentIndex> slots;
            if (!byMode.TryGetValue(mode, out slots))
            {
                slots = new HashSet<EquipmentIndex>();
                byMode[mode] = slots;
            }

            if (slots.Contains(slot))
            {
                slots.Remove(slot);
                return false;
            }

            slots.Add(slot);
            return true;
        }
    }
}
