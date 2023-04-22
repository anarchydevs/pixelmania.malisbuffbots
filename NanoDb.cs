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
    public class NanoDb : JsonFile<Dictionary<Profession, List<NanoEntry>>>
    {
        private readonly Dictionary<Profession, List<NanoEntry>> _nanoDb;

        public List<NanoEntry> LocalPlayerProfession => _nanoDb[DynelManager.LocalPlayer.Profession];

        public NanoDb(string jsonPath) : base(jsonPath) => _nanoDb = _data;
    }

    public class BuffEntry
    {
        public SimpleChar Character;
        public NanoEntry NanoEntry;

        public BuffEntry(SimpleChar simpleChar, NanoEntry nanoEntry)
        {
            Character = simpleChar;
            NanoEntry = nanoEntry;
        }
    }

    public class NanoEntry
    {
        public string Name;
        public Dictionary<int, int> LevelToId;
        public CastType Type;
        public int TimeOut;
        public int RemoveNanoIdUponCast;
        public List<string> Tags;
    }

    public enum CastType
    {
        Single,
        Team
    }
}
