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
        private static readonly Dictionary<Command, Func<PrivateMessage, bool>> _commands = new Dictionary<Command, Func<PrivateMessage, bool>>()
        {
            { Command.Stand, msg => StandAction(msg) },
            { Command.Sit, msg => SitAction(msg) },
            { Command.Cast, msg => CastRequest(msg) },
            { Command.Rebuff, msg => RebuffRequest(msg) },
            { Command.Buffmacro, msg => BuffMacroRequest(msg) },
            { Command.Help, msg => HelpRequest(msg) },
        };

        public bool TryProcess(PrivateMessage msg, out Command command, out string[] commandParts, out int requesterId)
        {
            commandParts = msg.Message.ToLower().Split(' ');
            requesterId = (int)msg.SenderId;

            if (!Enum.TryParse(commandParts[0].ToTitleCase(), out command))
                return false;

            commandParts = commandParts.Length > 1 ? commandParts.Skip(1).ToArray() : null;

            if (!_commands.TryGetValue(command, out var action))
            {
                Logger.Error($"Invalid command '{command}.");
                return false;
            }

            return _commands[command].Invoke(msg);
        }

        private static bool CastRequest(PrivateMessage msg)
        {
            if (msg.Message.Split(' ').Length < 2)
            {
                Logger.Error($"Received cast command without valid params.");
                return false;
            }

            return true;
        }

        private static bool SitAction(PrivateMessage msg)
        {
            Logger.Information($"Received sit request from {msg.SenderName}");
            return true;
        }

        private static bool StandAction(PrivateMessage msg)
        {
            Logger.Information($"Received stand request from {msg.SenderName}");
            return true;
        }

        private static bool RebuffRequest(PrivateMessage msg)
        {
            Logger.Information($"Received rebuff request from {msg.SenderName}");
            return true;
        }

        private static bool BuffMacroRequest(PrivateMessage msg)
        {
            Logger.Information($"Received ncu scan request from {msg.SenderName}");
            return true;
        }

        private static bool HelpRequest(PrivateMessage msg)
        {
            Logger.Information($"Received help request from {msg.SenderName}"); 
            return true;
        }
    }

    public enum Command
    {
        Stand,
        Sit,
        Cast,
        Rebuff,
        Buffmacro,
        Help
    }
}
