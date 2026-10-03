# TOR Companion Auto Equip v0.2.1

Target: Mount & Blade II: Bannerlord 1.3.15 + The Old Realms (TOR_Core v1.3.15).

## 기능
- 인벤토리 화면에서 현재 선택된 캐릭터 1명만 자동 장착합니다.
- 다른 캐릭터가 착용 중인 장비는 절대 가져오지 않습니다.
- 후보는 PlayerInventory 안의 장비 + 현재 선택 캐릭터가 이미 입고 있는 해당 슬롯 장비뿐입니다.
- 무기/방패/탄약/말/마갑은 건드리지 않습니다.
- 전투/민간인/잠행 장비 화면에서 현재 보고 있는 세트만 변경합니다.
- "최고 방어구 자동 장착": 수정치까지 반영한 방어도 합을 최대로 맞춥니다.
- "마법사용 자동 장착 (무게 ≤ 10)": 방어구 5부위 총중량 10.00 이하에서 방어도 합이 최대인 조합을 찾습니다.
- 잠긴 인벤토리 아이템은 제외합니다.
- 세이브용 CampaignBehavior나 커스텀 저장 데이터는 추가하지 않습니다.

## 설치 완성본 구조
```
Modules/
  TOR_CompanionAutoEquip/
    SubModule.xml
    GUI/Prefabs/TORAutoEquipOverlay.xml
    bin/Win64_Shipping_Client/TOR_CompanionAutoEquip.dll
```

Bannerlord.Harmony와 TOR_Core 뒤에 로드하세요.

## 빌드 비용 안전장치
CI는 공개 저장소일 때만 표준 `windows-latest` 러너를 요청합니다. 저장소가 private이면 job 자체가 skip됩니다.
