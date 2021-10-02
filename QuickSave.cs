using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SMLHelper.V2.Handlers;
using UnityEngine;
using UWE;

namespace QuickSave
{
    internal static class QuickSave
    {
        public static Options Options = new Options();

        public const string QuickSaveSlot = "quicksave";

        public static void Initialise()
        {
            OptionsPanelHandler.RegisterModOptions(Options);
        }

        public static string GetOriginalSaveSlot()
        {
            string currentSlot = SaveLoadManager.main.GetCurrentSlot();
            if (currentSlot == QuickSaveSlot)
            {
                return PlayerPrefs.GetString(QuickSaveSlot, QuickSaveSlot);
            } else
            {
                return currentSlot;
            }
        }

        public static IEnumerator Save()
        {
            PlayerPrefs.SetString(QuickSaveSlot, GetOriginalSaveSlot());
            SaveLoadManager.main.SetCurrentSlot(QuickSaveSlot,SaveLoadManager.StoryVersion.Reboot);

            // Runs the savegame function identically to the main menu
            IngameMenu.main.mainPanel.SetActive(false);
            yield return CoroutineHost.StartCoroutine(IngameMenu.main.SaveGameAsync());

            IngameMenu.main.QuitSubscreen(); // Previous call can cause a 'ghost menu' to be brought up and invisible. This closes it.
            
            SaveLoadManager.main.SetCurrentSlot(PlayerPrefs.GetString(QuickSaveSlot),SaveLoadManager.StoryVersion.Reboot);
        }

        public static IEnumerator Load()
        {
            // First we need to load all the save game slot data and wait for it to complete
            yield return CoroutineHost.StartCoroutine(SaveLoadManager.main.LoadSlotsAsync());
            LoadQuickSave();
        }

        /// <summary>
        /// Copied from uGUI_MainMenu.LoadMostRecentSavedGame() and modified to load the quicksave slot
        /// </summary>
        private static void LoadQuickSave()
        {
            if ((SaveLoadManager.main.GetActiveSlotNames() as IEnumerable<string>).Contains(QuickSaveSlot))
            {   // Check that a quicksave exists before attempting to load it!
                SaveLoadManager.GameInfo gameInfo = SaveLoadManager.main.GetGameInfo(QuickSaveSlot);
                if (gameInfo != null)
                {
                    CoroutineHost.StartCoroutine(LoadGameAsync(QuickSaveSlot, gameInfo.changeSet, gameInfo.gameMode));
                }
            }
        }

        private static bool isStartingNewGame = false;
        /// <summary>
        /// Copied from uGUI_MainMenu, altered to work statically
        /// </summary>
        /// <param name="saveGame"></param>
        /// <param name="changeSet"></param>
        /// <param name="gameMode"></param>
        /// <returns></returns>

        public static IEnumerator LoadGameAsync(string saveGame, int changeSet, GameMode gameMode)
        {
            if (isStartingNewGame)
            {
                yield break;
            }
            isStartingNewGame = true;
            FPSInputModule.SelectGroup(null, false);
            uGUI.main.loading.ShowLoadingScreen();
            yield return BatchUpgrade.UpgradeBatches(changeSet, saveGame);
            global::Utils.SetContinueMode(true);
            global::Utils.SetLegacyGameMode(gameMode);
            SaveLoadManager.main.SetCurrentSlot(Path.GetFileName(saveGame),SaveLoadManager.StoryVersion.Original);
            VRLoadingOverlay.Show();
            CoroutineTask<SaveLoadManager.LoadResult> task = SaveLoadManager.main.LoadAsync();
            yield return task;
            SaveLoadManager.LoadResult result = task.GetResult();
            if (!result.success)
            {
                yield return new WaitForSecondsRealtime(1f);
                isStartingNewGame = false;
                uGUI.main.loading.End(false);
                string descriptionText = Language.main.GetFormat<string>("LoadFailed", result.errorMessage);
                if (result.error == SaveLoadManager.Error.OutOfSpace)
                {
                    descriptionText = Language.main.Get("LoadFailedSpace");
                }
                uGUI.main.confirmation.Show(descriptionText, delegate (bool confirmed)
                {
                    OnErrorConfirmed(confirmed, saveGame, changeSet, gameMode);
                });
            }
            else
            {
                FPSInputModule.SelectGroup(null, false);
                uGUI.main.loading.BeginAsyncSceneLoad("Main");
            }
            isStartingNewGame = false;
            yield break;
        }

        /// <summary>
        /// Copied from uGUI_MainMenu, altered to work statically
        /// </summary>
        /// <param name="confirmed"></param>
        /// <param name="saveGame"></param>
        /// <param name="changeSet"></param>
        /// <param name="gameMode"></param>
        private static void OnErrorConfirmed(bool confirmed, string saveGame, int changeSet, GameMode gameMode)
        {
            if (confirmed)
            {
                CoroutineHost.StartCoroutine(LoadGameAsync(saveGame, changeSet, gameMode));
                return;
            }
            FPSInputModule.SelectGroup(null, false);
        }

        public static bool GetAllowSaving()
        {
            return !SaveLoadManager.main.isLoading  // Can't save if we're currently loading
                && IngameMenu.main.GetAllowSaving();// Check that saving is currently allowed (ie. we're not in a cinematic or already saving)
        }

        public static bool GetAllowLoading()
        {
            return !SaveLoadManager.main.isLoading  // Can't load if we're already loading
                && !SaveLoadManager.main.isSaving;  // Can't load if we're currently saving
        }
    }
}
