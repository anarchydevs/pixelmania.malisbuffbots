using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MalisBuffBots
{
    public enum IPCOpcode
    {
        CastRequest = 0,
        ReceiveQueueInfo = 1,
        RequestQueueInfo = 2,
        ReceiveSpellListInfo = 3,
        RequestSpellListInfo = 4,
    }
}
