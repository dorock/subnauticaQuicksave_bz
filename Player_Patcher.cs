using HarmonyLib;
using UnityEngine;
using UWE;

namespace QuickSave
{
    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("Update")]
    internal class Player_Update_Patch
    {
        public static void Postfix()
        {
            if (AvatarInputHandler.main.IsEnabled()) // Ignore inputs when main InputHandler is disabled, ie. user is in
            {                                        // options menu, dev console, rebinding keys, etc.
                if (Input.GetKeyDown(QuickSave.Options.QuickSaveKey))
                {
                    if (QuickSave.GetAllowSaving())
                    {
                        CoroutineHost.StartCoroutine(QuickSave.Save());
                    }
                    else
                    {   // Let the player know we didn't save
                        ErrorMessage.AddWarning("Saving is not permitted at this time.");
                    }
                }
                else if (Input.GetKeyDown(QuickSave.Options.QuickLoadKey))
                {
                    if (QuickSave.GetAllowLoading())
                    {   // Start asynchronously loading the most recent save
                        CoroutineHost.StartCoroutine(QuickSave.Load());
                    }
                    else
                    {   // Let the player know we cannot load right now
                        ErrorMessage.AddWarning("Loading is not permitted at this time.");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("Awake")]
    internal class Player_Awake_Patch
    {
        public static void Postfix()
        {
            SaveLoadManager.main.SetCurrentSlot(QuickSave.GetOriginalSaveSlot(),SaveLoadManager.StoryVersion.Reboot);
        }
    }
}
