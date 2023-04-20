using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MalisBuffBots
{
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

    public enum Settings
    {
        SitKitThreshold
    }
}

