using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisBuffBots
{
    public class Settings : JsonFile<Dictionary<SettingsEnum, int>>
    {
        private readonly Dictionary<SettingsEnum, int> _settings;

        public int SitKitThreshold => _settings[SettingsEnum.SitKitThreshold];
        public bool PvpFlagCheck => _settings[SettingsEnum.PvpFlagCheck] == 1;

        public Settings(string jsonPath) : base(jsonPath) => _settings = _data;
    }
     
    public enum SettingsEnum
    {
        SitKitThreshold,
        PvpFlagCheck
    }
}