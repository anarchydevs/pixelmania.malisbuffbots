using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisBuffBots
{
    public class Main : ClientlessPluginEntry
    {
        // Use the BuffsDb.json to reference/change tags for nanos:
        // _waitTime - wait period before canceling the request,
        // Custom _waitTime can be set for individual nanos (look at line 144 onward)
        // To use the buffers, type in vicinity "cast <nanoTag1> <nanoTag2> <nanoTag3> .. 
        // Supports multi buff per line requests and team buffs
        // type "stand" or "sit" to switch their movement states
        // _sitKitThreshold is used for auto using sit kits below the given number,
        // your sit kit must be in the first inventory slot for this to work properly
        // to properly set it there, make sure to clean your inventory completely
        // with a clean inventory, withdraw the sit kit to your inventory
        // if your character spam sits / stands up and doesnt use the kit
        // you either haven't correctly put it in the first slot or
        // the character doesn't meet the requirements to use the item

        private static Dictionary<Profession, List<NanoEntry>> _nanoDb;
        private List<BuffEntry> _buffEntries;
        private double _waitTime;
        private double _graceTime;
        private BuffEntry _currentBuffEntry;
        private bool _isInTeam = false;
        private bool _sentTeamRequest = false;
        private int _sitKitThreshold = 1000;

        public override void Init(string pluginDir)
        {
            Logger.Information("- Mali's Clientless Buffbots -");

            Client.SuppressDeserializationErrors();
            Client.OnUpdate += OnUpdate;
            Client.MessageReceived += OnMessageReceived;
            Client.Chat.VicinityMessageReceived += (e, msg) => HandleVicinityMessage(msg);
            _buffEntries = new List<BuffEntry>();
            _nanoDb = JsonConvert.DeserializeObject<Dictionary<Profession, List<NanoEntry>>>(File.ReadAllText($@"{Extensions.PluginDir}\BuffsDb.json"));
            _graceTime = 0.5f;
        }

        private void HandleVicinityMessage(VicinityMsg msg)
        {
            string[] commandParts = msg.Message.Split(' ');

            switch (commandParts[0])
            {
                case "cast":
                    {
                        Logger.Information($"Received cast request from {msg.SenderName}");

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

                        foreach (var reqNano in commandParts.Skip(1).Distinct())
                        {
                            DynelManager.Find(new Identity(IdentityType.SimpleChar, Client.LocalDynelId), out PlayerChar playerChar);

                            var dictNano = _nanoDb[DynelManager.LocalPlayer.Profession].Where(x => x.Tags.Contains(reqNano));

                            if (dictNano == null)
                                continue;

                            foreach (var nano in dictNano)
                            {
                                _buffEntries.Add(new BuffEntry
                                {
                                    Character = requestor,
                                    NanoEntry = nano
                                });
                            }
                        }
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
                if (DynelManager.LocalPlayer.TryGetStat(Stat.CurrentNano, out int maxNano) && maxNano < _sitKitThreshold)
                {
                    Extensions.UseSitKit();
                    return;
                }

                AttemptToBuff(_currentBuffEntry);
            }

            _waitTime -= deltaTime;

            if (_waitTime < 0)
                NextBuff();
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
                _isInTeam = true;
            }
            else if (charActionMessage.Action == CharacterActionType.FinishNanoCasting)
            {
                if (charActionMessage.Identity.Instance != Client.LocalDynelId)
                    return;

                _buffEntries.RemoveAt(0);
                _currentBuffEntry = null;
                _waitTime = 0;

                if (_isInTeam)
                {
                    Team.LeaveTeam();
                    _sentTeamRequest = false;
                    _isInTeam = false;
                }

                if (charActionMessage.Parameter2 == Nanos.SloughingCombatField) 
                    Extensions.RemoveBuff(231055);
            }
        }

        private void NextBuff()
        {
            if (_currentBuffEntry != null)
                _buffEntries.RemoveAt(0);

            _currentBuffEntry = _buffEntries.FirstOrDefault();

            switch (_currentBuffEntry.NanoEntry.Id)
            {
                case Nanos.SloughingCombatField:
                    {
                        _waitTime = 40f;
                        break;
                    }
                case Nanos.RetoolNCU:
                    {
                        _currentBuffEntry.NanoEntry.Id = Extensions.FindBestNcuNano(_currentBuffEntry.Character.Level);
                        _waitTime = 12f;
                        break;
                    }
                default:
                    {
                        _waitTime = 10f;
                        break;
                    }
            }
        }

        private void AttemptToBuff(BuffEntry buffEntry)
        {
            if (buffEntry == null)
                return;

            if (buffEntry.NanoEntry.Type == CastType.Team)
            {
                if (!_sentTeamRequest)
                {
                    Team.Invite(buffEntry.Character);
                    _sentTeamRequest = true;  
                }

                if (!_isInTeam)
                    return;
            }

            DynelManager.LocalPlayer.Cast(buffEntry.Character, buffEntry.NanoEntry.Id);
            _graceTime = 0.5f;
        }
    }
}