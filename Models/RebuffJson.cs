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
    public class RebuffJson : JsonFile<Dictionary<Profession, List<BuffInfo>>>
    {
        public readonly Dictionary<Profession, List<BuffInfo>> Entries;

        public List<BuffInfo> LocalProfEntries => TryGetValue((Profession)DynelManager.LocalPlayer.Profession);

        public List<BuffInfo> GenericEntries => TryGetValue(Profession.Generic);

        private List<BuffInfo> TryGetValue(Profession prof)
        {
            if (!Entries.TryGetValue(prof, out List<BuffInfo> entries))
                return null;

            return entries;
        }

        public bool Contains(IEnumerable<string> tags) => Entries.Any(x => x.Value.Any(y => tags.Any(t => y.Buffs.Contains(t))));

        public RebuffJson(string jsonPath) : base(jsonPath) => Entries = _data;
    }

    public class BuffInfo
    {
        public List<string> Buffs;
    }
}