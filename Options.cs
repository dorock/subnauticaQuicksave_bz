using System;
using System.IO;
using System.Reflection;
using System.Text;
using LitJson;
using SMLHelper.V2.Options;
using UnityEngine;

namespace QuickSave
{
    internal struct OptionsObject
    {
        public KeyCode QuickSaveKey { get; set; }
        public KeyCode QuickLoadKey { get; set; }
    }

    internal class Options : ModOptions
    {
        public KeyCode QuickSaveKey = KeyCode.F2;
        public KeyCode QuickLoadKey = KeyCode.F9;

        private string OldConfigPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.txt");
        private string ConfigPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");

        public Options() : base("QuickSave")
        {
            InitEvents();
            LoadDefaults();
        }

        private void InitEvents()
        {
            KeybindChanged += Options_KeybindChanged;
        }

        private void Options_KeybindChanged(object sender, KeybindChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "quicksave":
                    QuickSaveKey = e.Key;
                    break;
                case "quickload":
                    QuickLoadKey = e.Key;
                    break;
            }
            UpdateJSON();
        }

        private void LoadDefaults()
        {
            if (!File.Exists(ConfigPath))
            {
                UpdateJSON();
            }
            else
            {
                ReadOptionsFromJSON();
            }
        }

        private void UpdateJSON()
        {
            OptionsObject options = new OptionsObject
            {
                QuickSaveKey = QuickSaveKey,
                QuickLoadKey = QuickLoadKey
            };

            var stringBuilder = new StringBuilder();
            var jsonWriter = new JsonWriter(stringBuilder)
            {
                PrettyPrint = true
            };
            JsonMapper.ToJson(options, jsonWriter);

            string optionsJSON = stringBuilder.ToString();
            File.WriteAllText(ConfigPath, optionsJSON);
        }

        private void ReadOptionsFromJSON()
        {
            if (File.Exists(OldConfigPath))
            {   // Parse and delete the old config.txt
                try
                {
                    string oldConfigText = File.ReadAllText(OldConfigPath);
                    string quickSaveKey = oldConfigText.Substring(14);
                    QuickSaveKey = SMLHelper.V2.Utility.KeyCodeUtils.StringToKeyCode(quickSaveKey);
                }
                catch (Exception) { } // We don't care if it fails
                finally
                {   // Delete the old config.txt
                    File.Delete(OldConfigPath);
                }
            }

            if (File.Exists(ConfigPath))
            {   // Parse and load options from the new config.json
                try
                {
                    string optionsJSON = File.ReadAllText(ConfigPath);
                    var options = JsonMapper.ToObject<OptionsObject>(optionsJSON);
                    var data = JsonMapper.ToObject(optionsJSON);

                    QuickSaveKey = data.ContainsKey("QuickSaveKey") ? options.QuickSaveKey : QuickSaveKey;
                    QuickLoadKey = data.ContainsKey("QuickLoadKey") ? options.QuickLoadKey : QuickLoadKey;

                    if (!data.ContainsKey("QuickSaveKey") || !data.ContainsKey("QuickLoadKey"))
                    {
                        UpdateJSON();
                    }
                }
                catch (Exception)
                {   // JSON was invalid, create default values
                    UpdateJSON();
                }
            }
            else
            {   // Create the config.json with default values
                UpdateJSON();
            }
        }

        public override void BuildModOptions()
        {
            AddKeybindOption("quicksave", "QuickSave", GameInput.GetPrimaryDevice(), QuickSaveKey);
            AddKeybindOption("quickload", "QuickLoad", GameInput.GetPrimaryDevice(), QuickLoadKey);
        }
    }
}
