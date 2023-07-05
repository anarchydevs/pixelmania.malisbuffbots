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
    public enum PvpFlagId
    {
        OneMin = 216382,
        TenMin = 214879,
        FifteenMin = 284620,
        OneHr = 202732,
    }

    public enum Profession : uint
    {
        Generic,
        Soldier,
        MartialArtist,
        Engineer,
        Fixer,
        Agent,
        Adventurer,
        Trader,
        Bureaucrat,
        Enforcer,
        Doctor,
        NanoTechnician,
        Metaphysicist,
        Monster,
        Keeper,
        Shade
    }

    public enum CastType
    {
        Single,
        Team
    }
}

