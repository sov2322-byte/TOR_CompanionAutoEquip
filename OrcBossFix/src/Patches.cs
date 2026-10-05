using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_OrcBossTwoHandFix
{
    [HarmonyPatch(typeof(CareerChoiceObject), nameof(CareerChoiceObject.MutateTriggeredEffect))]
    internal static class TwoHandedKeystonePatch
    {
        private const string ChoiceId = "YouAnWotArmourKeystone";
        private const string EffectId = "armed_to_da_teef";

        private static void Prefix(CareerChoiceObject __instance, TriggeredEffectTemplate effect, out int __state)
        {
            __state = effect?.DamageAmount ?? int.MinValue;
        }

        private static void Postfix(CareerChoiceObject __instance, TriggeredEffectTemplate effect, Agent agent, int __state)
        {
            if (__instance == null || effect == null || agent == null || __state == int.MinValue)
                return;
            if (__instance.StringId != ChoiceId)
                return;

            string originalId = effect.StringID?.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (originalId != EffectId)
                return;

            // If TOR fixes this upstream and its Replace mutation starts working,
            // do nothing. WITM1.12 leaves DamageAmount unchanged because a float
            // is returned for an int property.
            if (effect.DamageAmount != __state)
                return;

            CharacterObject character = agent.Character as CharacterObject;
            Hero hero = character?.HeroObject;
            if (hero == null)
                return;

            int twoHanded = hero.GetSkillValue(DefaultSkills.TwoHanded);
            int bonus = Convert.ToInt32(twoHanded * 0.5f);
            effect.DamageAmount = __state + bonus;
        }
    }
}