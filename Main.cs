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
        // NOTE: BuffsDb.json structure was changed in 21th April 2023 update, if you have custom nanos, make sure to update accordingly

        /* 
        TUTORIAL:   

        Use the BuffsDb.json to modify bot casting behavior, example:
        {
            "Name": "NCU Nanos", - name of the entry, used in logging information
            "LevelToId": { - level to id map, it will check players level and cast the given nano id, always order highest level to lowest, if we don't care about this just write a single entry with the key being "0"
            "185": 163095,
            "165": 163094,
            "135": 163087,
            "125": 163085,
            "75": 163083,
            "50": 163081,
            "25": 163079
            },
            "Type": "Team", - Player will be invited to team before casting, other option is "Single" for non team buffs
            "TimeOut": 15, - Timeout period, aka how many seconds the bot will attempt to cast this particular nano before moving to the next entry
            "RemoveNanoIdUponCast": 0, - In cases like engi blocker aura, you can specify a custom nano id to remove that id from your ncu that would otherwise not allow your bot to cast it again, if we don't care about this leave at 0
            "Tags": [ "ncu" ] - Tags used for commands, aka "cast ncu" would trigger this entry
        },

         To use the buffers, type in vicinity "cast <nanoTag1> <nanoTag2> <nanoTag3> .. (multi buffs per line is allowed)
         type "stand" or "sit" to switch their movement states if they are in the wrong initial state
         ORG CHAT: If you want to use org chat for relaying requests, use the Client.Chat.GroupMessageReceived event handler (look in TestPlugin for an example of filtering only org chat messages), and just reroute the commands there
         PRIVATE CHAT: You can use Client.SendPrivateMessage to send messages to people, for command purposes or logging purposes, might want to do it from a single bot to avoid spam

         Use the Settings.json to configure sit kit threshold usage, might have other future uses
         Configure your sit kit item id in the RelevantItems, make sure your character meets the skill requirements to use sit kits if you arent using premium health and nano recharger 
         If you log out your bots / kill the process, wait until they fully leave the server before rebooting them, else there might be issues with certain stats not getting registered for the LocalPlayer
        */

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
            if (_commandManager.Process(msg, out string command, out string[] commandParts, out PlayerChar requester))
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

            if (DynelManager.LocalPlayer.TryGetStat(Stat.CurrentNano, out int currentNano) &&
                currentNano < Settings.SitKitThreshold &&
                !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Treatment))
            {
                if (Inventory.Items.Find((int)ItemId.PremiumHealthAndNanoRecharger, out Item item))
                {
                    var moveComponent = DynelManager.LocalPlayer.MovementComponent;
                    moveComponent.ChangeMovement(MovementAction.SwitchToSit);
                    item.Use();
                    moveComponent.ChangeMovement(MovementAction.LeaveSit);
                }
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

            if (n3Msg.N3MessageType != N3MessageType.CharacterAction || n3Msg.Identity.Instance != Client.LocalDynelId)
                return;

            ProcessCharacterActionMessage((CharacterActionMessage)n3Msg);
        }

        private void ProcessCharacterActionMessage(CharacterActionMessage actionMsg)
        {
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