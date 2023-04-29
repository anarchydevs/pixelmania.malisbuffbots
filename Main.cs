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
    public class Main : ClientlessPluginEntry
    {
        private NanoDb _nanoDb;
        public static Settings Settings;
        private BuffQueue _buffQueue;
        private CommandManager _commandManager;

        public override void Init(string pluginDir)
        {
            Logger.Information("- Mali's Clientless Buffbots -");

            Client.SuppressDeserializationErrors();
            Client.OnUpdate += OnUpdate;
            Client.MessageReceived += OnMessageReceived;
            Client.Chat.VicinityMessageReceived += (e, msg) => HandleVicinityMessage(msg);

            _commandManager = new CommandManager();
            _buffQueue = new BuffQueue();
            _nanoDb = new NanoDb($@"{Utils.PluginDir}\BuffsDb.json");
            Settings = new Settings($@"{Utils.PluginDir}\Settings.json");
        }

        private void HandleVicinityMessage(VicinityMsg msg)
        {
            if (_commandManager.TryProcess(msg, out string command, out string[] commandParts, out PlayerChar requester))
            {
                switch (command)
                {
                    case "cast":
                        if (requester == null)
                        {
                            Logger.Error($"Unable to locate requester.");
                            break;
                        }
                        ProcessBuffRequest(commandParts, requester);
                        break;
                    case "stand":
                        DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
                        break;
                    case "sit":
                        DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);
                        break;
                }
            }
        }

        private void OnUpdate(object _, double deltaTime)
        {
            if (_buffQueue.IsEmpty())
                return;

            if (!_buffQueue.TimerExpired(deltaTime))
                return;

            if (DynelManager.LocalPlayer.ShouldUseSitKit(out Item item))
            {
                var moveComponent = DynelManager.LocalPlayer.MovementComponent;
                moveComponent.ChangeMovement(MovementAction.SwitchToSit);
                item.Use();
                moveComponent.ChangeMovement(MovementAction.LeaveSit);
            }
            else
            {
                _buffQueue.ProcessCurrentBuffEntry();
            }
        }

        private void OnMessageReceived(object _, Message msg)
        {
            if (msg.Header.PacketType != PacketType.N3Message)
                return;

            N3Message n3Msg = (N3Message)msg.Body;

            if (n3Msg.Identity.Instance != Client.LocalDynelId)
                return;

            if (n3Msg.N3MessageType != N3MessageType.CharacterAction)
                return;

            ProcessCharacterActionMessage((CharacterActionMessage)n3Msg);
        }

        private void ProcessCharacterActionMessage(CharacterActionMessage actionMsg)
        {
            if (_buffQueue.CurrentBuffEntry == null)
                return;

            switch (actionMsg.Action)
            {
                case CharacterActionType.AcceptTeamRequest:
                    Logger.Information($"Team invite accepted from '{_buffQueue.CurrentBuffEntry.Character.Name}'");
                    break;
                case CharacterActionType.FinishNanoCasting:
                    Logger.Information($"Finished casting '{_buffQueue.CurrentBuffEntry.NanoEntry.Name}' on '{_buffQueue.CurrentBuffEntry.Character.Name}'");
                    _buffQueue.ResetCurrentBuffEntry();
                    break;
            }
        }

        private void ProcessBuffRequest(string[] commandParts, PlayerChar requestor)
        {
            foreach (var reqNano in commandParts.Distinct())
            {
                var dbResult = _nanoDb.LocalPlayerProfession.Where(x => x.Tags.Contains(reqNano));

                if (dbResult.Count() == 0)
                    continue;

                foreach (var nano in dbResult)
                    _buffQueue.Enqueue(new BuffEntry(requestor, nano));

                Logger.Information($"Received cast request from '{requestor.Name}'");
            }
        }
    }
}

public enum ItemId
{
    PremiumHealthAndNanoRecharger = 297274
}

public enum PvpFlagId
{
    OneMin = 216382,
    TenMin = 214879,
    FifteenMin = 284620,
    OneHr = 202732,
}