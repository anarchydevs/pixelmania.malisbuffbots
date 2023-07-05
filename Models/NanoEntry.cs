using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisBuffBots
{
    public class NanoEntry
    {
        [AoMember(0, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int32)]
        public LevelToIdMap[] LevelToId { get; set; }

        [AoMember(2)]
        public CastType Type { get; set; }

        [AoMember(3)]
        public int TimeOut { get; set; }

        [AoMember(4)]
        public int RemoveNanoIdUponCast { get; set; }

        [AoMember(5, SerializeSize = ArraySizeType.Int32)]
        public string[] Tags { get; set; }

        public bool ContainsId(int id) => LevelToId.Any(x=>x.Id == id);
    }

    public class LevelToIdMap
    {
        [AoMember(0)]
        public int Level { get; set; }

        [AoMember(1)]
        public int Id { get; set; }  
    }
}
