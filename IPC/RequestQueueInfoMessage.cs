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
    [AoContract((int)IPCOpcode.RequestQueueInfo)]
    public class RequestQueueInfoMessage : IPCMessage
    {
        public override short Opcode => (int)IPCOpcode.RequestQueueInfo;
    }
}
