using System.Reflection;
using HarmonyLib;

namespace QuickSave
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var harmony = new Harmony("com.oldark.subnautica.quicksave.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            QuickSave.Initialise();
        }
    }
}
