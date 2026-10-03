using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;

namespace TOR_CompanionAutoEquip
{
    internal static class AutoEquipService
    {
        private const float MageArmorWeightLimit = 10.0f;
        private const int WeightScale = 100;
        private const int WeightCapacity = 1000;

        private static readonly EquipmentIndex[] ArmorSlots =
        {
            EquipmentIndex.Head,
            EquipmentIndex.Cape,
            EquipmentIndex.Body,
            EquipmentIndex.Gloves,
            EquipmentIndex.Leg
        };

        internal static EquipResult EquipMaximumDefense()
        {
            return Execute(weightLimited: false);
        }

        internal static EquipResult EquipMageMaximumDefense()
        {
            return Execute(weightLimited: true);
        }

        private static EquipResult Execute(bool weightLimited)
        {
            SPInventoryVM vm = InventoryVmTracker.Active;
            CharacterObject character = InventoryVmTracker.GetCurrentCharacter();
            InventoryLogic inventoryLogic = InventoryVmTracker.GetInventoryLogic();

            if (vm == null || character == null || inventoryLogic == null)
                return EquipResult.Fail("인벤토리 정보를 읽지 못했습니다.");

            Hero hero = character.HeroObject;
            if (hero == null)
                return EquipResult.Fail("현재 선택된 대상은 영웅/동료가 아닙니다.");

            EquipmentModeSnapshot mode = InventoryVmTracker.GetModeSnapshot();
            Equipment currentEquipment = GetEquipment(hero, mode);
            if (currentEquipment == null)
                return EquipResult.Fail("현재 장비 세트를 읽지 못했습니다.");

            Dictionary<EquipmentIndex, List<ArmorCandidate>> candidatesBySlot =
                BuildCandidates(vm, currentEquipment, mode, character);

            Dictionary<EquipmentIndex, ArmorCandidate> plan = weightLimited
                ? FindBestUnderWeight(candidatesBySlot)
                : FindBestUnrestricted(candidatesBySlot);

            if (plan == null || plan.Count == 0)
                return EquipResult.Fail(weightLimited
                    ? "방어구 총 무게 10 이하 조합을 찾지 못했습니다."
                    : "착용 가능한 방어구를 찾지 못했습니다.");

            float totalWeight = plan.Values.Sum(c => c.Weight);
            int totalDefense = plan.Values.Sum(c => c.Defense);

            if (weightLimited && totalWeight > MageArmorWeightLimit + 0.0001f)
                return EquipResult.Fail("계산된 방어구 무게가 10을 초과해 적용을 취소했습니다.");

            InventoryLogic.InventorySide targetSide = InventoryVmTracker.GetTargetSide();
            List<TransferCommand> commands = new List<TransferCommand>();
            int changedSlots = 0;

            foreach (EquipmentIndex slot in ArmorSlots)
            {
                EquipmentElement current = currentEquipment[slot];
                ArmorCandidate target = plan[slot];

                if (SameElement(current, target.Element))
                    continue;

                if (target.Element.IsEmpty)
                {
                    if (!current.IsEmpty)
                    {
                        commands.Add(TransferCommand.Transfer(
                            amount: 1,
                            fromSide: targetSide,
                            toSide: InventoryLogic.InventorySide.PlayerInventory,
                            elementToTransfer: new ItemRosterElement(current, 1),
                            fromEquipmentIndex: slot,
                            toEquipmentIndex: EquipmentIndex.None,
                            character: character));
                        changedSlots++;
                    }
                    continue;
                }

                ItemRosterElement? rosterElement = inventoryLogic.FindItemFromSide(
                    InventoryLogic.InventorySide.PlayerInventory,
                    target.Element);

                if (rosterElement == null)
                {
                    return EquipResult.Fail(
                        "선택한 장비가 더 이상 인벤토리에 없어 아무 것도 변경하지 않았습니다.");
                }

                commands.Add(TransferCommand.Transfer(
                    amount: 1,
                    fromSide: InventoryLogic.InventorySide.PlayerInventory,
                    toSide: targetSide,
                    elementToTransfer: rosterElement.Value,
                    fromEquipmentIndex: EquipmentIndex.None,
                    toEquipmentIndex: slot,
                    character: character));
                changedSlots++;
            }

            if (commands.Count > 0)
            {
                try
                {
                    inventoryLogic.AddTransferCommands(commands);
                    vm.RefreshValues();
                }
                catch (Exception ex)
                {
                    return EquipResult.Fail("자동 장착 적용 중 오류: " + ex.Message);
                }
            }

            string modeText = GetModeText(mode);
            string label = weightLimited ? "마법사용" : "최고 방어";
            return EquipResult.Ok(
                string.Format(
                    "{0} · {1} · {2}: 방어도 {3}, 방어구 무게 {4:0.00}, 변경 {5}부위",
                    character.Name,
                    modeText,
                    label,
                    totalDefense,
                    totalWeight,
                    changedSlots));
        }

        private static Equipment GetEquipment(Hero hero, EquipmentModeSnapshot mode)
        {
            switch (mode)
            {
                case EquipmentModeSnapshot.Civilian:
                    return hero.CivilianEquipment;
                case EquipmentModeSnapshot.Stealth:
                    return hero.StealthEquipment;
                default:
                    return hero.BattleEquipment;
            }
        }

        private static Dictionary<EquipmentIndex, List<ArmorCandidate>> BuildCandidates(
            SPInventoryVM vm,
            Equipment currentEquipment,
            EquipmentModeSnapshot mode,
            CharacterObject character)
        {
            Dictionary<EquipmentIndex, List<ArmorCandidate>> result =
                ArmorSlots.ToDictionary(slot => slot, slot => new List<ArmorCandidate>());

            foreach (EquipmentIndex slot in ArmorSlots)
                result[slot].Add(ArmorCandidate.Empty(slot, currentEquipment[slot].IsEmpty));

            foreach (EquipmentIndex slot in ArmorSlots)
            {
                EquipmentElement current = currentEquipment[slot];
                if (!current.IsEmpty && current.Item != null && current.Item.HasArmorComponent)
                    AddIfUnique(result[slot], ArmorCandidate.FromCurrent(slot, current));
            }

            AddPlayerInventoryCandidates(vm.LeftItemListVM, result, mode, character);
            AddPlayerInventoryCandidates(vm.RightItemListVM, result, mode, character);

            foreach (EquipmentIndex slot in ArmorSlots)
                result[slot] = ParetoTrim(result[slot]);

            return result;
        }

        private static void AddPlayerInventoryCandidates(
            IEnumerable<SPItemVM> itemList,
            Dictionary<EquipmentIndex, List<ArmorCandidate>> result,
            EquipmentModeSnapshot mode,
            CharacterObject character)
        {
            if (itemList == null)
                return;

            foreach (SPItemVM itemVm in itemList)
            {
                if (itemVm == null
                    || itemVm.InventorySide != InventoryLogic.InventorySide.PlayerInventory
                    || itemVm.ItemRosterElement.IsEmpty
                    || itemVm.ItemCount <= 0
                    || itemVm.IsLocked
                    || !itemVm.IsTransferable
                    || !itemVm.IsEquipableItem
                    || itemVm.IsGenderDifferent
                    || !itemVm.CanCharacterUseItem)
                    continue;

                EquipmentElement element = itemVm.ItemRosterElement.EquipmentElement;
                ItemObject item = element.Item;
                if (item == null || !item.HasArmorComponent || element.IsQuestItem)
                    continue;

                if (mode == EquipmentModeSnapshot.Civilian && !item.IsCivilian)
                    continue;
                if (mode == EquipmentModeSnapshot.Stealth && !item.IsStealthItem)
                    continue;

                if (!CharacterHelper.CanUseItemBasedOnSkill(character, element))
                    continue;

                foreach (EquipmentIndex slot in ArmorSlots)
                {
                    if (!Equipment.IsItemFitsToSlot(slot, item))
                        continue;

                    AddIfUnique(result[slot], ArmorCandidate.FromInventory(slot, element, itemVm.ItemCount));
                }
            }
        }

        private static List<ArmorCandidate> ParetoTrim(List<ArmorCandidate> source)
        {
            List<ArmorCandidate> ordered = source
                .OrderBy(c => c.Weight)
                .ThenByDescending(c => c.Defense)
                .ThenByDescending(c => c.IsCurrent)
                .ToList();

            List<ArmorCandidate> trimmed = new List<ArmorCandidate>();
            int bestDefense = int.MinValue;

            foreach (ArmorCandidate candidate in ordered)
            {
                if (candidate.Defense < bestDefense && !candidate.IsCurrent)
                    continue;

                if (candidate.Defense > bestDefense)
                    bestDefense = candidate.Defense;

                trimmed.Add(candidate);
            }

            return trimmed;
        }

        private static Dictionary<EquipmentIndex, ArmorCandidate> FindBestUnrestricted(
            Dictionary<EquipmentIndex, List<ArmorCandidate>> candidatesBySlot)
        {
            Dictionary<EquipmentIndex, ArmorCandidate> plan =
                new Dictionary<EquipmentIndex, ArmorCandidate>();

            foreach (EquipmentIndex slot in ArmorSlots)
            {
                ArmorCandidate best = candidatesBySlot[slot]
                    .OrderByDescending(c => c.Defense)
                    .ThenByDescending(c => c.IsCurrent)
                    .ThenBy(c => c.Weight)
                    .First();
                plan[slot] = best;
            }

            return plan;
        }

        private static Dictionary<EquipmentIndex, ArmorCandidate> FindBestUnderWeight(
            Dictionary<EquipmentIndex, List<ArmorCandidate>> candidatesBySlot)
        {
            PlanState[] states = new PlanState[WeightCapacity + 1];
            states[0] = new PlanState();

            foreach (EquipmentIndex slot in ArmorSlots)
            {
                PlanState[] next = new PlanState[WeightCapacity + 1];

                for (int used = 0; used <= WeightCapacity; used++)
                {
                    PlanState state = states[used];
                    if (state == null)
                        continue;

                    foreach (ArmorCandidate candidate in candidatesBySlot[slot])
                    {
                        int units = ToWeightUnits(candidate.Weight);
                        int newUsed = used + units;
                        if (newUsed > WeightCapacity)
                            continue;

                        PlanState proposed = state.With(slot, candidate);
                        PlanState existing = next[newUsed];
                        if (IsBetterState(proposed, existing))
                            next[newUsed] = proposed;
                    }
                }

                states = next;
            }

            PlanState best = null;
            for (int used = 0; used <= WeightCapacity; used++)
            {
                PlanState state = states[used];
                if (state == null || state.Selection.Count != ArmorSlots.Length)
                    continue;
                if (state.ActualWeight > MageArmorWeightLimit + 0.0001f)
                    continue;

                if (IsBetterFinalState(state, best))
                    best = state;
            }

            return best == null ? null : best.Selection;
        }

        private static int ToWeightUnits(float weight)
        {
            if (weight <= 0f)
                return 0;
            return (int)Math.Ceiling(weight * WeightScale - 0.00001f);
        }

        private static bool IsBetterState(PlanState proposed, PlanState existing)
        {
            if (existing == null)
                return true;
            if (proposed.TotalDefense != existing.TotalDefense)
                return proposed.TotalDefense > existing.TotalDefense;
            if (Math.Abs(proposed.ActualWeight - existing.ActualWeight) > 0.0001f)
                return proposed.ActualWeight < existing.ActualWeight;
            return proposed.Changes < existing.Changes;
        }

        private static bool IsBetterFinalState(PlanState proposed, PlanState existing)
        {
            if (existing == null)
                return true;
            if (proposed.TotalDefense != existing.TotalDefense)
                return proposed.TotalDefense > existing.TotalDefense;
            if (Math.Abs(proposed.ActualWeight - existing.ActualWeight) > 0.0001f)
                return proposed.ActualWeight < existing.ActualWeight;
            return proposed.Changes < existing.Changes;
        }

        private static int GetDefense(EquipmentIndex slot, EquipmentElement element)
        {
            if (element.IsEmpty || element.Item == null)
                return 0;

            // Match the armor value the player expects for each visible armor slot.
            // This prevents, for example, a "helmet" with 0 head armor but body armor
            // from beating a real helmet just because its cross-body total is larger.
            switch (slot)
            {
                case EquipmentIndex.Head:
                    return element.GetModifiedHeadArmor();
                case EquipmentIndex.Body:
                    return element.GetModifiedBodyArmor();
                case EquipmentIndex.Gloves:
                    return element.GetModifiedArmArmor();
                case EquipmentIndex.Leg:
                    return element.GetModifiedLegArmor();
                case EquipmentIndex.Cape:
                    return element.GetModifiedHeadArmor()
                         + element.GetModifiedBodyArmor()
                         + element.GetModifiedArmArmor()
                         + element.GetModifiedLegArmor();
                default:
                    return 0;
            }
        }

        private static void AddIfUnique(List<ArmorCandidate> list, ArmorCandidate candidate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ArmorCandidate existing = list[i];
                if (SameElement(existing.Element, candidate.Element) && existing.IsCurrent == candidate.IsCurrent)
                    return;
            }
            list.Add(candidate);
        }

        private static bool SameElement(EquipmentElement a, EquipmentElement b)
        {
            if (a.IsEmpty && b.IsEmpty)
                return true;
            if (a.IsEmpty || b.IsEmpty)
                return false;
            return a.Item == b.Item && ReferenceEquals(a.ItemModifier, b.ItemModifier);
        }

        private static string GetModeText(EquipmentModeSnapshot mode)
        {
            switch (mode)
            {
                case EquipmentModeSnapshot.Civilian:
                    return "민간인";
                case EquipmentModeSnapshot.Stealth:
                    return "잠행";
                default:
                    return "전투";
            }
        }

        private sealed class ArmorCandidate
        {
            internal EquipmentIndex Slot;
            internal EquipmentElement Element;
            internal int Defense;
            internal float Weight;
            internal bool IsCurrent;
            internal int InventoryCount;

            internal static ArmorCandidate Empty(EquipmentIndex slot, bool isCurrentEmpty)
            {
                return new ArmorCandidate
                {
                    Slot = slot,
                    Element = EquipmentElement.Invalid,
                    Defense = 0,
                    Weight = 0f,
                    IsCurrent = isCurrentEmpty,
                    InventoryCount = 0
                };
            }

            internal static ArmorCandidate FromCurrent(EquipmentIndex slot, EquipmentElement element)
            {
                return new ArmorCandidate
                {
                    Slot = slot,
                    Element = element,
                    Defense = GetDefense(slot, element),
                    Weight = element.GetEquipmentElementWeight(),
                    IsCurrent = true,
                    InventoryCount = 0
                };
            }

            internal static ArmorCandidate FromInventory(EquipmentIndex slot, EquipmentElement element, int count)
            {
                return new ArmorCandidate
                {
                    Slot = slot,
                    Element = element,
                    Defense = GetDefense(element),
                    Weight = element.GetEquipmentElementWeight(),
                    IsCurrent = false,
                    InventoryCount = count
                };
            }
        }

        private sealed class PlanState
        {
            internal readonly Dictionary<EquipmentIndex, ArmorCandidate> Selection;
            internal readonly int TotalDefense;
            internal readonly float ActualWeight;
            internal readonly int Changes;

            internal PlanState()
            {
                Selection = new Dictionary<EquipmentIndex, ArmorCandidate>();
                TotalDefense = 0;
                ActualWeight = 0f;
                Changes = 0;
            }

            private PlanState(
                Dictionary<EquipmentIndex, ArmorCandidate> selection,
                int defense,
                float actualWeight,
                int changes)
            {
                Selection = selection;
                TotalDefense = defense;
                ActualWeight = actualWeight;
                Changes = changes;
            }

            internal PlanState With(EquipmentIndex slot, ArmorCandidate candidate)
            {
                Dictionary<EquipmentIndex, ArmorCandidate> copy =
                    new Dictionary<EquipmentIndex, ArmorCandidate>(Selection);
                copy[slot] = candidate;

                return new PlanState(
                    copy,
                    TotalDefense + candidate.Defense,
                    ActualWeight + candidate.Weight,
                    Changes + (candidate.IsCurrent ? 0 : 1));
            }
        }
    }

    internal sealed class EquipResult
    {
        internal bool Success { get; private set; }
        internal string Message { get; private set; }

        private EquipResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        internal static EquipResult Ok(string message)
        {
            return new EquipResult(true, message);
        }

        internal static EquipResult Fail(string message)
        {
            return new EquipResult(false, message);
        }
    }
}
