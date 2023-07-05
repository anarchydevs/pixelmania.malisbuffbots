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
    public class JsonFile<T>
    {
        protected readonly T _data;
        protected string Raw;

        public JsonFile(string jsonPath)
        {
            try
            {
                Raw = File.ReadAllText(jsonPath);
                _data = JsonConvert.DeserializeObject<T>(Raw);
            }
            catch
            {
                _data = default(T);
            }
        }
    }
}
