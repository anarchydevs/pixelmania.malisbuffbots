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
using System.Threading.Tasks;

namespace MalisBuffBots
{
    public class Main : ClientlessPluginEntry
    {
        public static SettingsJson SettingsJson;        // Behavior related settings (configurable in JSON/Settings.json)
        public static BuffsJson BuffsJson;          // All bot nanos (configurable in JSON/BuffsDb.json)
        public static RebuffJson RebuffJson;    // Rebuff info (configurable in JSON/RebuffInfo.json)
        public static QueueProcessor QueueProcessor;
        public static RebuffProcessor RebuffProcessor;
        public static IPC Ipc;
        private static CommandManager _commandManager; // Command processor

        public override void Init(string pluginDir)
        {
            Logger.Information("- Mali's Clientless Buffbots -");

            Client.SuppressDeserializationErrors();
            Client.Chat.PrivateMessageReceived += (e, msg) => HandlePrivateMessage(msg);
            Client.Chat.NetworkMessageReceived += (e, msg) => Test(msg);

            Ipc = new IPC(225, 1);

            _commandManager = new CommandManager();

            BuffsJson = new BuffsJson($"{Utils.PluginDir}\\JSON\\BuffsDb.json");
            SettingsJson = new SettingsJson($"{Utils.PluginDir}\\JSON\\Settings.json");
            RebuffJson = new RebuffJson($"{Utils.PluginDir}\\JSON\\RebuffInfo.json");

            QueueProcessor = new QueueProcessor();

            Client.OnUpdate += OnUpdate;
            Client.MessageReceived += OnMessageReceived;
            Client.OnUpdate += Ipc.OnUpdate;
        }

        private void HandlePrivateMessage(PrivateMessage msg)
        {
            if (!_commandManager.TryProcess(msg, out Command command, out string[] commandParts, out PlayerChar requester))
                return;

            if (SettingsJson.Data.InitConnectionDelay > 0 && requester != null)
                Client.SendPrivateMessage((uint)requester.Identity.Instance, "I am still loading. Your request will be processed shortly.");

            // Command logic execution
            switch (command)
            {
                case Command.Cast:
                    if (requester == null)
                    {
                        Logger.Error($"Unable to locate requester.");
                        break;
                    }
                    ProcessCastRequest(commandParts, requester);
                    break;
                case Command.Rebuff:
                    if (requester == null)
                    {
                        Logger.Error($"Unable to locate requester.");
                        break;
                    }
                    ProcessRebuffRequest(requester);
                    break;
                case Command.Buffmacro:
                    if (requester == null)
                    {
                        Logger.Error($"Unable to locate requester.");
                        break;
                    }
                    ProcessBuffmacroRequest(requester);
                    break;
                case Command.Stand:
                    DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
                    break;
                case Command.Sit:
                    DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);
                    break;
            }
        }

        private void OnUpdate(object sender, double delta)
        {
            SettingsJson.Data.InitConnectionDelay -= delta;

            if (SettingsJson.Data.InitConnectionDelay > 0)
                return;

            Ipc.Broadcast(new RequestSpellListInfoMessage());
            DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
            Ipc.AddSpellDataEntry((Profession)DynelManager.LocalPlayer.Profession, DynelManager.LocalPlayer.SpellList);
            RebuffProcessor = new RebuffProcessor(RebuffJson);

            Client.OnUpdate += QueueProcessor.OnUpdate;
            Client.MessageReceived += QueueProcessor.OnMessageReceived;
            Client.OnUpdate -= OnUpdate;
        }

        private void OnMessageReceived(object _, Message msg)
        {
            if (msg.Header.PacketType == PacketType.PingMessage)
                Logger.Debug($"Received ping message from GAME server.");
        }

        private void Test(ChatMessage msg)
        {
            if (msg.Header.PacketType == ChatMessageType.Ping)
                Logger.Debug($"Received ping message from CHAT server.");
        }

        public void ProcessCastRequest(string[] nanoTags, PlayerChar requester)
        {
            if (!BuffsJson.FindByTags(nanoTags, out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            foreach (var entry in entries)
                QueueProcessor.FinalizeBuffRequest(entry.Key, entry.Value, requester);

            Logger.Information($"Received cast request from '{requester.Name}'");
        }

        private void ProcessRebuffRequest(PlayerChar requester)
        {
            List<int> requesterBuffs = requester.Buffs.Select(x => x.Id).ToList();

            if (!BuffsJson.FindByIds(requesterBuffs, out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            foreach (var entry in entries)
                QueueProcessor.FinalizeBuffRequest(entry.Key, entry.Value, requester);

            Logger.Information($"Received rebuff request from '{requester.Name}'");
        }


        private void ProcessBuffmacroRequest(PlayerChar requester)
        {
            List<string> buffsByTag = new List<string>();

            if (!BuffsJson.FindByIds(requester.Buffs.Select(x=>x.Id), out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            // Skipping team casts
            foreach (var entry in entries.Values.SelectMany(x => x))
            {
                if (entry.Type == CastType.Team)
                    continue;

                buffsByTag.Add(entry.Tags.FirstOrDefault());
            }

            Client.SendPrivateMessage((uint)requester.Identity.Instance, $"/macro buffpreset /tell {DynelManager.LocalPlayer.Name} cast {string.Join(" ", buffsByTag)}");
        }
    }
}