using TaleWorlds.Library;

namespace TOR_CompanionAutoEquip
{
    internal sealed class AutoEquipOverlayVM : ViewModel
    {
        private string _statusText = "현재 선택 캐릭터만 적용";

        [DataSourceProperty]
        public string MaxDefenseText
        {
            get { return "최고 방어구 자동 장착"; }
        }

        [DataSourceProperty]
        public string MageDefenseText
        {
            get { return "마법사용 자동 장착 (무게 ≤ 10)"; }
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

        public void ExecuteMaxDefense()
        {
            EquipResult result = AutoEquipService.EquipMaximumDefense();
            StatusText = result.Message;
        }

        public void ExecuteMageDefense()
        {
            EquipResult result = AutoEquipService.EquipMageMaximumDefense();
            StatusText = result.Message;
        }
    }
}
