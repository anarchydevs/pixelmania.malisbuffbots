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
    public static class Extensions
    {
        public static void RemoveBuff(this LocalPlayer localPlayer, int nanoId)
        {
            Client.Send(new CharacterActionMessage
            {
                Action = CharacterActionType.RemoveFriendlyNano,
                Parameter1 = 53019,
                Parameter2 = nanoId,
            });
        }

        public static bool IsInTeam(this LocalPlayer localPlayer) => localPlayer.GetStat(Stat.Team) != 0;

        public static void RemoveCustomBuff(this LocalPlayer localPlayer, int id)
        {
            if (id == 0)
                return;

            localPlayer.RemoveBuff(id);

            Logger.Information($"Removing buff with id: {id}.");
        }
    }
}

