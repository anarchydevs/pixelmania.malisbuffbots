using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace MalisBuffBots
{
    public class Extensions
    {
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void RemoveBuff(int nanoId)
        {
            Client.Send(new CharacterActionMessage
            {
                Action = CharacterActionType.RemoveFriendlyNano,
                Parameter1 = 53019,
                Parameter2 = nanoId,
            });
        }

        public static int FindBestNcuNano(int playerLevel)
        {
            if (25 <= playerLevel && playerLevel < 50) { return Nanos.RetoolNCU; }
            else if (50 <= playerLevel && playerLevel < 75) { return Nanos.JuryRiggedNCUAnalyzer; }
            else if (75 <= playerLevel && playerLevel < 125) { return Nanos.DeckRecoder; }
            else if (125 <= playerLevel && playerLevel < 135) { return Nanos.RecompilingMemoryAnalyzer; }
            else if (135 <= playerLevel && playerLevel < 165) { return Nanos.QuarkStorNCUCOre; }
            else if (165 <= playerLevel && playerLevel < 185) { return Nanos.ActiveViralCompressor; }
            else return Nanos.SentinentViralRecoder;
        }

        public static bool IsNcuNano(int nanoId)
        {
            if (nanoId == Nanos.RetoolNCU ||
                nanoId == Nanos.JuryRiggedNCUAnalyzer ||
                nanoId == Nanos.DeckRecoder ||
                nanoId == Nanos.RecompilingMemoryAnalyzer ||
                nanoId == Nanos.QuarkStorNCUCOre ||
                nanoId == Nanos.ActiveViralCompressor ||
                nanoId == Nanos.SentinentViralRecoder)
                return true;
            return false;
        }
    }

    public class BuffEntry
    {
        public SimpleChar Character;
        public NanoEntry NanoEntry;
    }

    public class NanoEntry
    {
        public string Name;
        public int Id;
        public CastType Type;
        public List<string> Tags;
    }

    public enum CastType
    {
        Single,
        Team
    }

    public class Nanos
    {
        public const int BeaconWarp = 154914;
        public const int TeamBeaconWarp = 154913;
        public const int RetoolNCU = 163079;
        public const int JuryRiggedNCUAnalyzer = 163081;
        public const int DeckRecoder = 163083;
        public const int RecompilingMemoryAnalyzer = 163085;
        public const int QuarkStorNCUCOre = 163087;
        public const int ActiveViralCompressor = 163094;
        public const int SentinentViralRecoder = 163095;
        public const int SuperiorOmniMedEnhancement = 95709;
        public const int SloughingCombatField = 204422;
        public const int GridspaceFreedomTeam = 162595;
    }
}

