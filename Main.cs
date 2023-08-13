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
        public static IPC Ipc;                              // Used to communicate between different bots
        public static SettingsJson SettingsJson;            // Behavior related settings (configurable in JSON/Settings.json)
        public static BuffsJson BuffsJson;                  // All bot nanos (configurable in JSON/BuffsDb.json)
        public static RebuffJson RebuffJson;                // Rebuff info (configurable in JSON/RebuffInfo.json)
        public static QueueProcessor QueueProcessor;        // Queue processing logic
        public static RebuffProcessor RebuffProcessor;      // Rebuff processing logic
        public static CommandProcessor _commandProcessor;   // Command processing logic

        public override void Init(string pluginDir)
        {
            try
            {
                Logger.Information("- Mali's Clientless Buffbots -");

                //Client.SuppressDeserializationErrors();
                Client.Chat.PrivateMessageReceived += (e, msg) => HandlePrivateMessage(msg);

                SettingsJson = new SettingsJson(Path.SETTINGS_JSON); 
                Ipc = new IPC(SettingsJson.Data.IPCChannelId, 5000);
                BuffsJson = new BuffsJson(Path.BUFF_JSON);
                RebuffJson = new RebuffJson(Path.REBUFF_JSON);

                _commandProcessor = new CommandProcessor(new UserRank(Path.USERRANK_JSON));
                QueueProcessor = new QueueProcessor();

                Client.OnUpdate += OnUpdate;
            }

            catch (Exception ex)
            {
                Logger.Information(ex.Message);
            }
        }

        private void HandlePrivateMessage(PrivateMessage msg)
        {
            if (!_commandProcessor.TryProcess(msg, out Command command, out string[] commandParts, out int requester))
            {
                return;
            }

            if (!DynelManager.Find(new Identity { Type = IdentityType.SimpleChar, Instance = requester }, out PlayerChar simpleChar))
            {
                Logger.Error($"Unable to locate requester.");
                return;
            }

            if (SettingsJson.Data.InitConnectionDelay > 0)
            {
                Client.SendPrivateMessage((uint)requester, ScriptTemplate.RequestRejected());
                return;
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
                case Command.Debug:
                    ProcessDebugRequest(requester);
                    break;
                case Command.Clear:
                    ProcessClearRequest(requester);
                    break;
            }
        }

        private void ProcessClearRequest(int requester)
        {
            QueueProcessor.ResetBotQueue();
            Client.SendPrivateMessage((uint)requester, "Clearing my queue");
        }

        private void ProcessDebugRequest(int requester)
        {
            string currentQueue = "";

            if (QueueProcessor.Queue.Current != null)
                currentQueue = $"CurrQueue {QueueProcessor.Queue.Current.NanoEntry.Name}";

            Client.SendPrivateMessage((uint)requester, $"{DynelManager.LocalPlayer.Name}\n " +
                $"teamTrackerId: {QueueProcessor.TeamTrackerId}\n" +
                $"QueueData.Entries.Count: {Ipc.BotCache.Entries.Count}\n " +
                $"IPCBotCache.BotData.Count: {Ipc.BotCache.Entries.Count}\n" +
                $"QueueData.Entries.Values.All: {Ipc.BotCache.Entries.Values.All(x => x.Queue.Count() == 0)}\n" +
                $"Team.IsInTeam: {Team.IsInTeam}\n " +
                $"QueueData.IsTeamQueueEmpty(trackId): {Ipc.BotCache.IsTeamQueueEmpty(QueueProcessor.TeamTrackerId)}\n " +
                $"{currentQueue}");
        }

        private void OnUpdate(object sender, double delta)
        {
            if ((SettingsJson.Data.InitConnectionDelay -= delta) < 0)
            {
                DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
                Ipc.Init();
                RebuffProcessor = new RebuffProcessor(RebuffJson);
                // Client.OnUpdate += Ipc.OnUpdate; TODO
                Client.OnUpdate += QueueProcessor.OnUpdate; 
                Client.OnUpdate -= OnUpdate;
            }
        }

        public void ProcessCastRequest(string[] nanoTags, PlayerChar requester)
        {
            if (!BuffsJson.FindByTags(nanoTags, out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            QueueProcessor.RequestBuffs(entries, requester);

            Logger.Information($"Received cast request from '{requester.Name}'");
        }

        private void ProcessRebuffRequest(PlayerChar requester)
        {
            var requesterBuffs = requester.Buffs;

            if (requesterBuffs.Count == 0)
                return;

            if (!BuffsJson.FindByIds(requesterBuffs.Select(x => x.Id), out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            QueueProcessor.RequestBuffs(entries, requester);

            Logger.Information($"Received rebuff request from '{requester.Name}'");
        }


        private void ProcessBuffmacroRequest(PlayerChar requester)
        {
            var requesterBuffs = requester.Buffs;

            if (requesterBuffs.Count == 0)
                return;

            List<string> buffsByTag = new List<string>();

            if (!BuffsJson.FindByIds(requesterBuffs.Select(x=>x.Id), out List<string> tags))
                return;

            Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.Buffmacro(DynelManager.LocalPlayer.Name, tags));
        }

        private void ProcessHelpRequest(PlayerChar requester)
        {
            Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.RetrievingBuffs());
            Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.HelpMenu());
        }
    }
}