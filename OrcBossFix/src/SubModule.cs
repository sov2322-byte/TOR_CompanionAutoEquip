using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TOR_OrcBossTwoHandFix
{
    public sealed class SubModule : MBSubModuleBase
    {
        private static Harmony _harmony;
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (_harmony != null) return;
            _harmony = new Harmony("openai.tor.orcboss.twohandfix");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}