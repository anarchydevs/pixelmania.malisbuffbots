using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisBuffBots
{
    public class SettingsJson : JsonFile<Config>
    {
        public Config Data;

        public SettingsJson(string jsonPath) : base(jsonPath)
        {         
            Data = !DataMigrate.ElementExists(JToken.Parse(Raw), "InitDelay") ? DataMigrate.ConvertToNewSettings() : _data;
        }
    }

    public class Config
    {
        public int SitKitThreshold = 1000;
        public bool PvpFlagCheck = true;
        public int SitKitItemId = 297274;
        public double InitConnectionDelay = 20;
    }
}