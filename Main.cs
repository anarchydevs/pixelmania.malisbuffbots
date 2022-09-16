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
        // Use the BuffsDb.json to reference/change tags, timeout periods for nanos:
        // To use the buffers, type in vicinity "cast <nanoTag1> <nanoTag2> <nanoTag3> .. 
        // Supports multi buff per line requests and team buffs
        // type "stand" or "sit" to switch their movement states

        // Use the Settings.json to configure sit kit threshold usage,
        // your sit kit must be in the first inventory slot for this to work
        // to properly set it there, make sure to clean your inventory completely
        // with a clean inventory, withdraw the sit kit to your inventory

        // if your character spam sits / stands up and doesnt use the kit
        // you either haven't correctly put it in the first slot or
        // the character doesn't meet the requirements to use the item

        private Dictionary<Profession, List<NanoEntry>> _nanoDb;
        private Dictionary<Settings, int> _settings;
        private List<BuffEntry> _buffEntries;
        private BuffEntry _currentBuffEntry;
        private double _waitTime;
        private double _graceTime;
        private bool _isInTeam = false;
        private bool _sentTeamRequest = false;

        public override void Init(string pluginDir)
        {
            Logger.Information("- Mali's Clientless Buffbots -");

            Client.SuppressDeserializationErrors();
            Client.OnUpdate += OnUpdate;
            Client.MessageReceived += OnMessageReceived;
            Client.Chat.VicinityMessageReceived += (e, msg) => HandleVicinityMessage(msg);
            _buffEntries = new List<BuffEntry>();
            _nanoDb = JsonConvert.DeserializeObject<Dictionary<Profession, List<NanoEntry>>>(File.ReadAllText($@"{Utils.PluginDir}\BuffsDb.json"));
            _settings = JsonConvert.DeserializeObject<Dictionary<Settings, int>>(File.ReadAllText($@"{Utils.PluginDir}\Settings.json"));

            _graceTime = 0.5f;
        }

        private void HandleVicinityMessage(VicinityMsg msg)
        {
            string[] commandParts = msg.Message.Split(' ');

            switch (commandParts[0])
            {
                case "cast":
                    {
                        if (commandParts.Length < 2)
                        {
                            Logger.Error($"Received cast command without valid params.");
                            return;
                        }

                        if (!DynelManager.Find(msg.SenderName, out PlayerChar requestor))
                        {
                            Logger.Error($"Unable to locate requestor.");
                            return;
                        }

                        ProcessBuffRequest(commandParts, requestor);
                    }
                    break;
                case "stand":
                    Logger.Information($"Received stand request from {msg.SenderName}");
                    DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.LeaveSit);
                    break;
                case "sit":
                    Logger.Information($"Received sit request from {msg.SenderName}");
                    DynelManager.LocalPlayer.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);
                    break;
                default:
                    {
                        Logger.Warning($"Received unknown command: {msg.Message}");
                        break;
                    }
            }
        }

        private void OnUpdate(object _, double deltaTime)
        {
            if (_buffEntries.Count == 0)
                return;

            _graceTime -= deltaTime;

            if (_graceTime < 0)
            {
                if (DynelManager.LocalPlayer.NanoLessThan(_settings[Settings.SitKitThreshold]))
                    DynelManager.LocalPlayer.UseItemInFirstSlot();
                else
                    ProcessCurrentBuffEntry();
            }

            _waitTime -= deltaTime;

            if (_waitTime < 0)
                ProcessNextBuffEntry();
        }

        private void OnMessageReceived(object _, Message msg)
        {
            if (msg.Header.PacketType != PacketType.N3Message)
                return;

            N3Message n3Msg = (N3Message)msg.Body;

            if (n3Msg.N3MessageType != N3MessageType.CharacterAction)
                return;

            CharacterActionMessage charActionMessage = (CharacterActionMessage)n3Msg;

            if (charActionMessage.Action == CharacterActionType.AcceptTeamRequest)
            {
                Logger.Information($" Team invite accepted from '{_currentBuffEntry.Character.Name}'");
                _isInTeam = true;
            }
            else if (charActionMessage.Action == CharacterActionType.FinishNanoCasting)
            {
                if (charActionMessage.Identity.Instance != Client.LocalDynelId)
                    return;

                Logger.Information($"Finished casting '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}'");
                DeleteCurrentBuffEntry();

                if (charActionMessage.Parameter2 == Nanos.SloughingCombatField) 
                    DynelManager.LocalPlayer.RemoveBuff(231055);
            }
        }

        private void ProcessBuffRequest(string[] commandParts, PlayerChar requestor)
        {
            foreach (var reqNano in commandParts.Skip(1).Distinct())
            {
                var dictNano = _nanoDb[DynelManager.LocalPlayer.Profession].Where(x => x.Tags.Contains(reqNano));

                if (dictNano == null || dictNano.Count() == 0)
                    continue;

                foreach (var nano in dictNano)
                {
                    _buffEntries.Add(new BuffEntry
                    {
                        Character = requestor,
                        NanoEntry = nano
                    });
                }

                Logger.Information($"Received cast request from '{requestor.Name}'");
            }
        }

        private void ProcessNextBuffEntry()
        {
            if (_currentBuffEntry != null)
            {
                Logger.Warning($"Casting '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}' failed. Removing entry.");
                DeleteCurrentBuffEntry();
            }

            BuffEntry firstBuffEntry = _buffEntries.FirstOrDefault();

            if (firstBuffEntry == null)
                return;

            _currentBuffEntry = firstBuffEntry;

            if (Utils.IsNcuNano(_currentBuffEntry.NanoEntry.Id))
                _currentBuffEntry.NanoEntry.Id = Utils.FindBestNcuNano(_currentBuffEntry.Character.Level);

            _waitTime = _currentBuffEntry.NanoEntry.TimeOut;
        }

        private void DeleteCurrentBuffEntry()
        {
            _buffEntries.RemoveAt(0);
            Team.LeaveTeam();
            _isInTeam = false;
            _sentTeamRequest = false;
            _currentBuffEntry = null;
            _waitTime = 0;
        }

        private void ProcessCurrentBuffEntry()
        {
            if (_currentBuffEntry == null)
                return;

            if (_currentBuffEntry.NanoEntry.Type == CastType.Team)
            {
                AttemptToTeamInvite();

                if (!_isInTeam)
                    return;
            }

            AttemptToBuffTarget();
        }

        private void AttemptToBuffTarget()
        {
            Logger.Information($"Attempting to cast '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}', Remaining time: {Math.Round(_waitTime, 2)} seconds.");
            DynelManager.LocalPlayer.Cast(_currentBuffEntry.Character, _currentBuffEntry.NanoEntry.Id);
            _graceTime = 0.5f;
        }

        private void AttemptToTeamInvite()
        {
            if (_sentTeamRequest)
                return;

            Logger.Information($" Team invite sent to '{_currentBuffEntry.Character.Name}'");
            Team.Invite(_currentBuffEntry.Character);
            _sentTeamRequest = true;
        }
    }
}