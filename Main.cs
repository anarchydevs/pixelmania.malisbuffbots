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
        public static BuffsJson BuffsJson;              // All bot nanos (configurable in JSON/BuffsDb.json)
        public static RebuffJson RebuffJson;            // Rebuff info (configurable in JSON/RebuffInfo.json)
        public static QueueProcessor QueueProcessor;
        public static RebuffProcessor RebuffProcessor;
        public static IPC Ipc;
        private static CommandManager _commandManager;  // Command processor

        public override void Init(string pluginDir)
        {
            Logger.Information("- Mali's Clientless Buffbots -");

            Client.SuppressDeserializationErrors();
            Client.Chat.PrivateMessageReceived += (e, msg) => HandlePrivateMessage(msg);

            SettingsJson = new SettingsJson($"{Utils.PluginDir}\\JSON\\Settings.json");

            Ipc = new IPC(SettingsJson.Data.IPCChannelId, 1);

            _commandManager = new CommandManager(new UserRank($"{Utils.PluginDir}\\JSON\\UserRanks.json"));

            BuffsJson = new BuffsJson($"{Utils.PluginDir}\\JSON\\BuffsDb.json");
            RebuffJson = new RebuffJson($"{Utils.PluginDir}\\JSON\\RebuffInfo.json");

            QueueProcessor = new QueueProcessor();

            Client.OnUpdate += OnUpdate;
            Client.OnUpdate += Ipc.OnUpdate;
        }

        private void HandlePrivateMessage(PrivateMessage msg)
        {
            if (!_commandManager.TryProcess(msg, out Command command, out string[] commandParts, out int requester))
            {
                return;
            }

            if (!DynelManager.Find(new Identity { Type = IdentityType.SimpleChar,Instance = requester }, out PlayerChar simpleChar))
            {
                Logger.Error($"Unable to locate requester.");
                return;
            }

            if (SettingsJson.Data.InitConnectionDelay > 0)
            {
                Client.SendPrivateMessage((uint)requester, "I am still loading. Your request will be processed shortly.");
            }

            // Command logic execution
            switch (command)
            {
                case Command.Cast:
                    ProcessCastRequest(commandParts, simpleChar);
                    break;
                case Command.Rebuff:
                    ProcessRebuffRequest(simpleChar);
                    break;
                case Command.Buffmacro:
                    ProcessBuffmacroRequest(simpleChar);
                    break;
                case Command.Stand:
                    DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
                    break;
                case Command.Sit:
                    DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);
                    break;
                case Command.Help:
                    ProcessHelpRequest(simpleChar);
                    break;
            }
        }

        private void OnUpdate(object sender, double delta)
        {
            SettingsJson.Data.InitConnectionDelay -= delta;

            if (SettingsJson.Data.InitConnectionDelay > 0)
                return;

            InitBot();
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

        private void ProcessHelpRequest(PlayerChar requester)
        {
            Client.SendPrivateMessage((uint)requester.Identity.Instance, "Retrieving available buffs. Just a moment.");
            Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.Create());
        }

        private void InitBot()
        {
            Ipc.Broadcast(new RequestSpellListInfoMessage());
            DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
            Ipc.AddSpellDataEntry((Profession)DynelManager.LocalPlayer.Profession, DynelManager.LocalPlayer.SpellList);
            RebuffProcessor = new RebuffProcessor(RebuffJson);
            Client.OnUpdate += QueueProcessor.OnUpdate;
            Client.MessageReceived += QueueProcessor.OnMessageReceived;
            Client.OnUpdate -= OnUpdate;
        }
    }
}