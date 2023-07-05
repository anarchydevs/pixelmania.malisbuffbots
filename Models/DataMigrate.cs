using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MalisBuffBots
{
    // This class is purely used for converting old buffdb / settings structure to new
    public class DataMigrate
    {
        public static Dictionary<Profession, List<NanoEntry>> ConvertToNewDb()
        {
            try
            {
                var oldDb = JsonConvert.DeserializeObject<Dictionary<Profession, List<NanoEntryOld>>>(File.ReadAllText($"{Utils.PluginDir}\\JSON\\BuffsDb.json"));
                Logger.Information("Old database detected. Converting to new");
                var newDb = new Dictionary<Profession, List<NanoEntry>>();

                foreach (var entry in oldDb)
                {
                    List<NanoEntry> newNanoEntry = new List<NanoEntry>();

                    foreach (var nanEntryOld in entry.Value)
                        newNanoEntry.Add(nanEntryOld.Migrate());

                    newDb.Add(entry.Key, newNanoEntry);
                }

                File.WriteAllText($"{Utils.PluginDir}\\JSON\\BuffsDb.json", JsonConvert.SerializeObject(newDb, Formatting.Indented));
                return newDb;
            }
            catch
            {
                return JsonConvert.DeserializeObject<Dictionary<Profession, List<NanoEntry>>>(File.ReadAllText($"{Utils.PluginDir}\\JSON\\BuffsDb.json"));
            }
        }
        public static bool ElementExists(JToken token, string elementKey)
        {
            // If the token is an object, iterate through its properties
            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty property in token.Children<JProperty>())
                {
                    // If the current property's key matches the desired element key, return true
                    if (property.Name == elementKey)
                        return true;

                    // Recursively check for the element in the property's value
                    if (ElementExists(property.Value, elementKey))
                        return true;
                }
            }
            // If the token is an array, iterate through its items
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken item in token.Children())
                {
                    // Recursively check for the element in the array item
                    if (ElementExists(item, elementKey))
                        return true;
                }
            }

            // If the element was not found, return false
            return false;
        }
        public static Config ConvertToNewSettings()
        {
            try
            {
                var oldSettings = JsonConvert.DeserializeObject<Dictionary<SettingsEnum, int>>(File.ReadAllText($"{Utils.PluginDir}\\JSON\\Settings.json"));            
                Logger.Information("Old settings detected. Converting to new.");
                var newSettings = new Config();

                newSettings.SitKitThreshold = oldSettings[SettingsEnum.SitKitThreshold];
                newSettings.PvpFlagCheck = oldSettings[SettingsEnum.PvpFlagCheck] == 1;

                File.WriteAllText($@"{Utils.PluginDir}\\JSON\\Settings.json", JsonConvert.SerializeObject(newSettings, Formatting.Indented));
                return newSettings;
            }
            catch
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText($"{Utils.PluginDir}\\JSON\\Settings.json"));

            }
        }

        private class NanoEntryOld
        {
            public string Name;
            public Dictionary<int, int> LevelToId;
            public CastType Type;
            public int TimeOut;
            public int RemoveNanoIdUponCast;
            public List<string> Tags;

            public NanoEntry Migrate()
            {
                LevelToIdMap[] lvlToIdDict = new LevelToIdMap[LevelToId.Count];
                int i = 0;

                foreach (var levelToId in LevelToId)
                {
                    lvlToIdDict[i] = new LevelToIdMap { Level = levelToId.Key, Id = levelToId.Value };
                    i++;
                }

                return new NanoEntry
                {
                    Name = Name,
                    LevelToId = lvlToIdDict,
                    Type = Type,
                    Tags = Tags.ToArray(),
                    TimeOut = TimeOut,
                    RemoveNanoIdUponCast = RemoveNanoIdUponCast
                };
            }
        }

        private class SettingsOld : JsonFile<Dictionary<SettingsEnum, int>>
        {
            private readonly Dictionary<SettingsEnum, int> _settings;

            public int SitKitThreshold => _settings[SettingsEnum.SitKitThreshold];
            public bool PvpFlagCheck => _settings[SettingsEnum.PvpFlagCheck] == 1;

            public SettingsOld(string jsonPath) : base(jsonPath) => _settings = _data;
        }

        private enum SettingsEnum
        {
            SitKitThreshold,
            PvpFlagCheck
        }
    }
}

