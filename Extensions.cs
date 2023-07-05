using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisBuffBots
{
    public static class Extensions
    {
        public static bool IsPvpFlagged(this SimpleChar simpleChar) => simpleChar.Buffs.Any(x => Enum.GetValues(typeof(PvpFlagId)).Cast<int>().ToList().Contains(x.Id));

        public static bool IsInTeam(this LocalPlayer localPlayer) => localPlayer.GetStat(Stat.Team) != 0;

        public static bool ShouldUseSitKit(this LocalPlayer localPlayer,out Item item)
        {
            item = null;

            return localPlayer.TryGetStat(Stat.CurrentNano, out int currentNano) &&
                 currentNano < Main.SettingsJson.Data.SitKitThreshold &&
                 !localPlayer.Cooldowns.ContainsKey(Stat.Treatment) &&
                 Inventory.Items.Find((int)Main.SettingsJson.Data.SitKitItemId, out item);
        }

        public static void TryRemoveBuff(this LocalPlayer localPlayer, int id)
        {
            if (id == 0)
                return;

            Client.Send(new CharacterActionMessage
            {
                Action = CharacterActionType.RemoveFriendlyNano,
                Parameter1 = (int)IdentityType.NanoProgram,
                Parameter2 = id,
            });

            Logger.Information($"Removing buff with id: {id}.");
        }

        public static string ToTitleCase(this string text) => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }
}

