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
    public class QueueProcessor
    {
        public int Entries => _queue.Count;
        private BuffEntry _currentBuffEntry;
        private Queue<BuffEntry> _queue = new Queue<BuffEntry>();
        private double _graceTime;
        private double _waitTime;
        private bool _teamRequestSent = false;

        public QueueProcessor(double graceTime = 0.5f)
        {
            _queue = new Queue<BuffEntry>();
            _graceTime = graceTime;
        }

        public void LocalEnqueue(SimpleChar requester, IEnumerable<NanoEntry> entries)
        {
            foreach (var entry in entries.Where(x => DynelManager.LocalPlayer.SpellList.Any(y => x.ContainsId(y))))
                _queue.Enqueue(new BuffEntry(requester, entry));
        }

        public void Enqueue(SimpleChar requester, NanoEntry entry) => _queue.Enqueue(new BuffEntry(requester, entry));

        public void FinalizeBuffRequest(Profession castProf, IEnumerable<NanoEntry> results, PlayerChar requester)
        {
            if (castProf == Profession.Generic) // We handle generic buffs by distributing the results evenly amongst all buffers
            {
                EnqueueByBotQueuePriority(results, requester);
            }
            else if (castProf == (Profession)DynelManager.LocalPlayer.Profession) // If the caster is our local player, enqueue buffs
            {
                LocalEnqueue(requester, results);
            }
            else // If the caster is not our local player, broadcast to the required profession
            {
                Main.Ipc.Broadcast(new CastRequestMessage { Caster = castProf, Requester = requester.Identity.Instance, Entries = results.ToArray() });
            }
        }

        public void OnUpdate(object _, double deltaTime)
        {
            if (IsEmpty())
                return;

            if (!TimerExpired(deltaTime))
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
                ProcessCurrentBuffEntry();
            }
        }

        public void OnMessageReceived(object _, Message msg)
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

        private void EnqueueByBotQueuePriority(IEnumerable<NanoEntry> results, PlayerChar requester)
        {
            List<NanoEntry> spells = results.ToList();

            while (spells.Count() > 0)
            {
                int cachedSpellCount = spells.Count();

                foreach (var prof in Main.Ipc.QueueData.OrderByQueueEntries())
                {
                    if (!Main.Ipc.SpellData.ContainsKey(prof.Key))
                        continue;

                    if (!DynelManager.Characters.Any(x => x.Identity == prof.Value.Identity))
                        continue;

                    var nextSpellToCast = spells.FirstOrDefault();

                    if (nextSpellToCast == null)
                        break;

                    if (!Main.Ipc.SpellData.ContainsNanoEntry(prof.Key, nextSpellToCast))
                        continue;

                    if (prof.Key == (Profession)DynelManager.LocalPlayer.Profession)
                    {
                        Enqueue(requester, nextSpellToCast);
                    }
                    else
                    {
                        Main.Ipc.Broadcast(new CastRequestMessage
                        {
                            Caster = prof.Key,
                            Requester = requester.Identity.Instance,
                            Entries = new NanoEntry[1] { nextSpellToCast }
                        });
                    }

                    spells.Remove(nextSpellToCast);
                }

                //Nobody can cast anything that is left
                if (cachedSpellCount == spells.Count())
                {
                    Logger.Warning($"No buffers could cast queued buffs.");
                    spells.Clear();
                }
            }
        }

        private void ProcessCharacterActionMessage(CharacterActionMessage actionMsg)
        {
            if (_currentBuffEntry == null)
                return;

            switch (actionMsg.Action)
            {
                case CharacterActionType.AcceptTeamRequest:
                    Logger.Information($"Team invite accepted from '{_currentBuffEntry.Character.Name}'");
                    break;
                case CharacterActionType.FinishNanoCasting:
                    Logger.Information($"Finished casting '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}'");
                    ResetCurrentBuffEntry();
                    break;
            }
        }

        private bool IsEmpty() => _queue.Count == 0 && _currentBuffEntry == null;

        private bool TimerExpired(double deltaTime)
        {
            _graceTime -= deltaTime;
            _waitTime -= deltaTime;

            if (_graceTime >= 0)
                return false;

            if (_waitTime < 0)
            {
                _currentBuffEntry = GetNextBuffEntry();
                _waitTime = _currentBuffEntry == null ? 0 : _currentBuffEntry.NanoEntry.TimeOut;
            }

            _graceTime = 0.5f;
            return true;
        }

        private void ProcessCurrentBuffEntry()
        {
            if (_currentBuffEntry == null)
                return;

            switch (_currentBuffEntry.NanoEntry.Type)
            {
                case CastType.Single:
                    AttemptToBuffTarget();
                    break;
                case CastType.Team:
                    ProcessTeamEntry();
                    break;
            }
        }

        private BuffEntry GetNextBuffEntry()
        {
            if (_currentBuffEntry != null)
            {
                Logger.Warning($"Casting '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}' failed. Removing entry.");
                ResetCurrentBuffEntry();
            }

            if (_queue.Count == 0)
                return null;

            Logger.Information("Obtaining new buff entry");

            return _queue.Dequeue();
        }

        private void ResetCurrentBuffEntry()
        {
            Team.LeaveTeam();
            DynelManager.LocalPlayer.TryRemoveBuff(_currentBuffEntry.NanoEntry.RemoveNanoIdUponCast);
            _teamRequestSent = false;
            _currentBuffEntry = null;
            _waitTime = 0;
        }

        private void AttemptToBuffTarget()
        {
            if (_currentBuffEntry.Character == null)
            {
                Logger.Information($"Cast attempt on UNKNOWN character skipped.");
                ResetCurrentBuffEntry();
                return;
            }

            if (Main.SettingsJson.Data.PvpFlagCheck && _currentBuffEntry.Character.IsPvpFlagged())
            {
                Logger.Information($"Cast attempt '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}' skipped (character is flagged)");
                ResetCurrentBuffEntry();
                return;
            }

            Logger.Information($"Attempting to cast '{_currentBuffEntry.NanoEntry.Name}' on '{_currentBuffEntry.Character.Name}', Remaining time: {Math.Round(_waitTime, 2)} seconds.");

            var firstAvailableBuff =  _currentBuffEntry.NanoEntry.LevelToId.FirstOrDefault(x => x.Level <= _currentBuffEntry.Character.Level && DynelManager.LocalPlayer.SpellList.Contains(x.Id));

            if (firstAvailableBuff == null)
            {
                ResetCurrentBuffEntry();
                return;
            }
                DynelManager.LocalPlayer.Cast(_currentBuffEntry.Character, firstAvailableBuff.Id);
            _graceTime = 0.5f;
        }

        private void ProcessTeamEntry()
        {
            if (DynelManager.LocalPlayer.IsInTeam())
            {
                AttemptToBuffTarget();
                return;
            }

            if (!_teamRequestSent)
            {
                Logger.Information($"Team invite sent to '{_currentBuffEntry.Character.Name}'");
                Team.Invite(_currentBuffEntry.Character);
                _teamRequestSent = true;
            }
        }
    }
}