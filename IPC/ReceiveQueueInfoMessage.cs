﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace MalisBuffBots
{
    [AoContract((int)IPCOpcode.ReceiveQueueInfo)]
    public class ReceiveQueueInfoMessage : IPCMessage
    {
        public override short Opcode => (int)IPCOpcode.ReceiveQueueInfo;

        [AoMember(0)]
        public Profession Caster { get; set; }

        [AoMember(1)]
        public QueueData QueueData { get; set; }
    }
}
