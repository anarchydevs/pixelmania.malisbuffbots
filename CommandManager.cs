using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisBuffBots
{
    public class CommandManager
    {
        private static readonly Dictionary<string, Func<VicinityMsg, bool>> _commands = new Dictionary<string, Func<VicinityMsg, bool>>()
        {
            { "stand", msg => StandRequestAction(msg) },
            { "sit", msg => SitRequestAction(msg) },
            { "cast", msg => CastActionRequest(msg) },
        };

        public bool Process(VicinityMsg msg, out string command, out string[] commandParts, out PlayerChar requester)
        {
            Logger.Information(msg.Message);

            commandParts = msg.Message.Split(' ');
            command = commandParts[0];
            requester = null;

            commandParts = commandParts.Length > 1 ? commandParts.Skip(1).ToArray() : null;

            if (DynelManager.Find(msg.SenderName, out PlayerChar playerChar))
            {
                requester = playerChar;
            }

            if (!_commands.TryGetValue(command, out var action))
            {
                Logger.Error($"Invalid command '{command}.");
                return false;
            }

            return _commands[command].Invoke(msg);
        }

        private static bool CastActionRequest(VicinityMsg msg)
        {
            if (msg.Message.Split(' ').Length < 2)
            {
                Logger.Error($"Received cast command without valid params.");
                return false;
            }

            return true;
        }

        private static bool SitRequestAction(VicinityMsg msg)
        {
            Logger.Information($"Received sit request from {msg.SenderName}");
            return true;
        }

        private static bool StandRequestAction(VicinityMsg msg)
        {
            Logger.Information($"Received stand request from {msg.SenderName}");
            return true;
        }
    }
}
