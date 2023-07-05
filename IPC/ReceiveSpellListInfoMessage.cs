using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace MalisBuffBots
{
    [AoContract((int)IPCOpcode.ReceiveSpellListInfo)]
    public class ReceiveSpellListInfoMessage : IPCMessage
    {
        public override short Opcode => (int)IPCOpcode.ReceiveSpellListInfo;

        [AoMember(0)]
        public Profession Profession { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int32)]
        public int[] SpellList { get; set; }
    }
}
