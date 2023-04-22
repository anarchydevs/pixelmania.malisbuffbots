using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Linq;

namespace MalisBuffBots
{
    public static class Extensions
    {
        public static bool IsPvpFlagged(this SimpleChar simpleChar) => simpleChar.Buffs.Any(x => Enum.GetValues(typeof(PvpFlagId)).Cast<int>().ToList().Contains(x.Id));

        public static bool IsInTeam(this LocalPlayer localPlayer) => localPlayer.GetStat(Stat.Team) != 0;

        public static void RemoveBuff(this LocalPlayer localPlayer, int id)
        {
            if (id == 0)
                return;

            Client.Send(new CharacterActionMessage
            {
                Action = CharacterActionType.RemoveFriendlyNano,
                Parameter1 = 53019,
                Parameter2 = id,
            });

            Logger.Information($"Removing buff with id: {id}.");
        }
    }
}

