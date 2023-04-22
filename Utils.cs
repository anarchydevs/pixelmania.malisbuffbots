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
    public class Utils
    {
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void LeaveTeam()
        {
            Client.Send(new CharacterActionMessage
            {
                Action = (CharacterActionType)24
            });
        }
    }
}

