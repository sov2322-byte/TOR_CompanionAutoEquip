using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_CompanionAutoEquip
{
    internal sealed class AutoEquipOverlayVM : ViewModel
    {
        private string _statusText = "";
        private bool _headLocked;
        private bool _capeLocked;
        private bool _bodyLocked;
        private bool _glovesLocked;
        private bool _legLocked;

        [DataSourceProperty]
        public string MaxDefenseText
        {
            get { return "최고 방어구"; }
        }

        [DataSourceProperty]
        public string MageDefenseText
        {
            get { return "마법사 ≤10"; }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText == value)
                    return;
                _statusText = value;
                OnPropertyChangedWithValue(value, nameof(StatusText));
            }
        }

        [DataSourceProperty]
        public bool HeadLocked
        {
            get { return _headLocked; }
            set
            {
                if (_headLocked == value) return;
                _headLocked = value;
                OnPropertyChangedWithValue(value, nameof(HeadLocked));
            }
        }

        [DataSourceProperty]
        public bool CapeLocked
        {
            get { return _capeLocked; }
            set
            {
                if (_capeLocked == value) return;
                _capeLocked = value;
                OnPropertyChangedWithValue(value, nameof(CapeLocked));
            }
        }

        [DataSourceProperty]
        public bool BodyLocked
        {
            get { return _bodyLocked; }
            set
            {
                if (_bodyLocked == value) return;
                _bodyLocked = value;
                OnPropertyChangedWithValue(value, nameof(BodyLocked));
            }
        }

        [DataSourceProperty]
        public bool GlovesLocked
        {
            get { return _glovesLocked; }
            set
            {
                if (_glovesLocked == value) return;
                _glovesLocked = value;
                OnPropertyChangedWithValue(value, nameof(GlovesLocked));
            }
        }

        [DataSourceProperty]
        public bool LegLocked
        {
            get { return _legLocked; }
            set
            {
                if (_legLocked == value) return;
                _legLocked = value;
                OnPropertyChangedWithValue(value, nameof(LegLocked));
            }
        }

        public void ExecuteMaxDefense()
        {
            EquipResult result = AutoEquipService.EquipMaximumDefense();
            StatusText = result.Message;
            RefreshLockState();
        }

        public void ExecuteMageDefense()
        {
            EquipResult result = AutoEquipService.EquipMageMaximumDefense();
            StatusText = result.Message;
            RefreshLockState();
        }

        public void ExecuteToggleHead() { Toggle(EquipmentIndex.Head, "머리"); }
        public void ExecuteToggleCape() { Toggle(EquipmentIndex.Cape, "망토"); }
        public void ExecuteToggleBody() { Toggle(EquipmentIndex.Body, "몸"); }
        public void ExecuteToggleGloves() { Toggle(EquipmentIndex.Gloves, "손"); }
        public void ExecuteToggleLeg() { Toggle(EquipmentIndex.Leg, "발"); }

        private void Toggle(EquipmentIndex slot, string label)
        {
            CharacterObject character = InventoryVmTracker.GetCurrentCharacter();
            Hero hero = character == null ? null : character.HeroObject;
            if (hero == null)
            {
                StatusText = "선택 캐릭터 없음";
                return;
            }

            EquipmentModeSnapshot mode = InventoryVmTracker.GetModeSnapshot();
            bool locked = SlotLockRegistry.Toggle(hero, mode, slot);
            StatusText = label + (locked ? " 잠금" : " 해제");
            RefreshLockState();
        }

        internal void RefreshLockState()
        {
            CharacterObject character = InventoryVmTracker.GetCurrentCharacter();
            Hero hero = character == null ? null : character.HeroObject;
            EquipmentModeSnapshot mode = InventoryVmTracker.GetModeSnapshot();

            HeadLocked = SlotLockRegistry.IsLocked(hero, mode, EquipmentIndex.Head);
            CapeLocked = SlotLockRegistry.IsLocked(hero, mode, EquipmentIndex.Cape);
            BodyLocked = SlotLockRegistry.IsLocked(hero, mode, EquipmentIndex.Body);
            GlovesLocked = SlotLockRegistry.IsLocked(hero, mode, EquipmentIndex.Gloves);
            LegLocked = SlotLockRegistry.IsLocked(hero, mode, EquipmentIndex.Leg);
        }
    }
}
