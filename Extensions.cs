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

        public static bool NanoLessThan(this LocalPlayer localPlayer, int threshold)
        {
            if (localPlayer.TryGetStat(Stat.CurrentNano, out int maxNano) && maxNano < threshold)
            {
                Logger.Information($"Attempting to use sit kit.");
                return true;
            }

            return false;
        }

        public static void UseItemInFirstSlot(this LocalPlayer client)
        {
            client.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);

            Targeting.SetTarget(client.Identity);

            Client.Send(new GenericCmdMessage
            {
                Count = 0xFF,
                Action = GenericCmdAction.Use,
                User = new Identity(IdentityType.SimpleChar, Client.LocalDynelId),
                Source = Identity.None,
                Target = new Identity(IdentityType.Inventory, 64)
            });

            client.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
        }
    }
}

