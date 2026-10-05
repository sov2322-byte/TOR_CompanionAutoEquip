using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_OrcBossTwoHandFix
{
    [HarmonyPatch]
    internal static class TwoHandedKeystonePatch
    {
        private const string ChoiceId = "YouAnWotArmourKeystone";
        private const string EffectId = "armed_to_da_teef";

        private static MethodBase TargetMethod()
        {
            Type choiceType = AccessTools.TypeByName("TOR_Core.CharacterDevelopment.CareerSystem.CareerChoiceObject");
            Type effectType = AccessTools.TypeByName("TOR_Core.BattleMechanics.TriggeredEffect.TriggeredEffectTemplate");
            if (choiceType == null || effectType == null)
                return null;

            return AccessTools.Method(choiceType, "MutateTriggeredEffect", new[] { effectType, typeof(Agent) });
        }

        private static void Prefix(
            object __instance,
            [HarmonyArgument(0)] object effect,
            out int __state)
        {
            __state = GetDamageAmount(effect);
        }

        private static void Postfix(
            object __instance,
            [HarmonyArgument(0)] object effect,
            [HarmonyArgument(1)] Agent agent,
            int __state)
        {
            if (__instance == null || effect == null || agent == null || __state == int.MinValue)
                return;

            string choiceId = Traverse.Create(__instance).Property("StringId").GetValue<string>();
            if (choiceId != ChoiceId)
                return;

            string effectStringId = Traverse.Create(effect).Property("StringID").GetValue<string>();
            if (string.IsNullOrEmpty(effectStringId))
                return;

            string originalId = effectStringId.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (originalId != EffectId)
                return;

            int currentDamage = GetDamageAmount(effect);
            if (currentDamage == int.MinValue)
                return;

            // WITM1.12 bug: the original Replace mutation returns float for an
            // int property, so DamageAmount remains unchanged. If TOR already
            // changed the value, do nothing to avoid double-applying a future fix.
            if (currentDamage != __state)
                return;

            CharacterObject character = agent.Character as CharacterObject;
            Hero hero = character?.HeroObject;
            if (hero == null)
                return;

            int twoHanded = hero.GetSkillValue(DefaultSkills.TwoHanded);
            int bonus = Convert.ToInt32(twoHanded * 0.5f);
            Traverse.Create(effect).Property("DamageAmount").SetValue(__state + bonus);
        }

        private static int GetDamageAmount(object effect)
        {
            if (effect == null)
                return int.MinValue;

            try
            {
                return Traverse.Create(effect).Property("DamageAmount").GetValue<int>();
            }
            catch
            {
                return int.MinValue;
            }
        }
    }
}
