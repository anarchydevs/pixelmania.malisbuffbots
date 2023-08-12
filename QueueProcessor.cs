using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisBuffBots
{
    public class QueueProcessor
    {
        internal AutoResetInterval TeamTimeout;
        private AutoResetInterval _gracePeriod;
        public int TeamTrackerId;
        public BuffQueue Queue = new BuffQueue();
        public N3MessageProcessor N3MessageProcessor;   

        public QueueProcessor(int graceTimeMs = 1000)
        {
            Queue = new BuffQueue();
            _gracePeriod = new AutoResetInterval(graceTimeMs);
            TeamTimeout = new AutoResetInterval((int)Main.SettingsJson.Data.TeamTimeoutInSeconds * 1000);
            N3MessageProcessor = new N3MessageProcessor(this);
        }

        public void OnUpdate(object _, double deltaTime)
        {
            try
            {
                if (!_gracePeriod.Elapsed)
                    return;

                if (Team.IsInTeam)
                    ProcessLeaveTeam();

                if (TeamTrackerId != 0 && Main.Ipc.BotCache.IsTeamQueueEmpty(TeamTrackerId))
                    ProcessResetTeamTrackerId();

                if (DynelManager.LocalPlayer.IsCasting)
                    return;

                switch (Queue.Process())
                {
                    case QueueState.Current:
                        ProcessCurrentBuffEntry();
                        break;
                    case QueueState.Dequeue:
                        TeamTimeout.Reset();
                        Main.Ipc.Broadcast(new QueueInfoMessage { Profession = (Profession)DynelManager.LocalPlayer.Profession, Entries = Main.QueueProcessor.Queue.AllEntries });
                        ProcessCurrentBuffEntry();
                        break;
                    case QueueState.Empty:
                        break;
                }
            }
            catch (Exception ex)
            {
                ResetBotQueue();
                Logger.Information(ex.Message);
            }
        }

        private void ProcessLeaveTeam()
        {
            if (!Main.Ipc.BotCache.IsTeamQueueEmpty(TeamTrackerId))
                return;

            if (Queue.Current == null || Queue.Current.NanoEntry.Type != CastType.Team)
                Team.LeaveTeam();
        }

        private void ResetBotQueue()
        {
            Logger.Information("Clearing my queue");
            Team.LeaveTeam();
            Queue.Clear();
            TeamTrackerId = 0;
        }

        private void ProcessResetTeamTrackerId()
        {
            Main.Ipc.BotCache.BroadcastTeamTrackerMessage((Profession)DynelManager.LocalPlayer.Profession, 0);
            TeamTrackerId = 0;
        }

        public void LocalEnqueue(SimpleChar requester, IEnumerable<NanoEntry> entries)
        {
            foreach (var entry in entries.Where(x => DynelManager.LocalPlayer.SpellList.Any(y => x.ContainsId(y))))
            {
                var buffEntry = new BuffEntry { Requester = requester.Identity, NanoEntry = entry };

                if (Queue.AllEntries.Count() > 0 && Queue.AllEntries.Any(x => x.Equals(buffEntry)))
                {
                    Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.AlreadyInQueue(entry.Name));
                    return;
                }

                Queue.Enqueue(buffEntry);
            }
        }

        public void RequestBuffs(Dictionary<Profession, List<NanoEntry>> entries, PlayerChar requester)
        {
            var teamEntries = entries.Where(x => x.Value.Any(y => y.Type == CastType.Team));

            if (teamEntries.Count() > 0)
            {
                List<Profession> orderedEntries = Main.BuffsJson.Entries.OrderBy(kv => kv.Value.Count(entry => entry.Type == CastType.Team)).Select(x => x.Key).ToList();

                var queueData = Main.Ipc.BotCache.NonTeamTrackerBots().OrderBy(kv => orderedEntries.IndexOf(kv.Key));

                if (queueData.Count() == 0)
                {
                    Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.TeamBotsBusy());
                    return;
                }

                if (queueData.FirstOrDefault().Value.Identity == DynelManager.LocalPlayer.Identity)
                {
                    Client.SendPrivateMessage((uint)requester.Identity.Instance, ScriptTemplate.TeamInvite());
                    Team.Invite(requester.Identity);
                    TeamTrackerId = requester.Identity.Instance;
                }

                Main.Ipc.BotCache.BroadcastTeamTrackerMessage(queueData.FirstOrDefault().Key, requester.Identity.Instance);
            }

 
            foreach (var entry in entries)
            {
                FinalizeBuffRequest(entry.Key, entry.Value, requester);
            }
        }

        public void FinalizeBuffRequest(Profession castProf, IEnumerable<NanoEntry> results, PlayerChar requester) => ProcessBuffRequest(castProf, results.ToList(), requester);

        public void FinalizeBuffRequest(Profession castProf, NanoEntry result, PlayerChar requester) => ProcessBuffRequest(castProf, new List<NanoEntry> { result }, requester);

        private void ProcessBuffRequest(Profession castProf, List<NanoEntry> results, PlayerChar requester)
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

        private void EnqueueByBotQueuePriority(IEnumerable<NanoEntry> results, PlayerChar requester)
        {
            Queue<NanoEntry> spells = new Queue<NanoEntry>(results);

            while (spells.Count() > 0)
            {
                int cachedSpellCount = spells.Count();

                foreach (var prof in Main.Ipc.BotCache.OrderByQueueEntries())
                {
                    if (spells.Count == 0)
                        break;

                    var nextSpellToCast = spells.Peek();

                    if (!Main.Ipc.BotCache.ContainsKey(prof.Key))
                        continue;

                    if (!DynelManager.Characters.Any(x => x.Identity == prof.Value.Identity))
                        continue;

                    if (!Main.Ipc.BotCache.ContainsNanoEntry(prof.Key, nextSpellToCast))
                        continue;

                    if (prof.Key == (Profession)DynelManager.LocalPlayer.Profession)
                    {
                        Queue.Enqueue(new BuffEntry { Requester = requester.Identity, NanoEntry = nextSpellToCast });
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

                    spells.Dequeue();
                }

                //Nobody can cast anything that is left
                if (cachedSpellCount == spells.Count())
                {
                    Logger.Warning($"No buffers could cast queued buffs.");
                    spells.Clear();
                }
            }
        }

        private void ProcessCurrentBuffEntry()
        {
            switch (Queue.Current.NanoEntry.Type)
            {
                case CastType.Single:
                    AttemptToBuffTarget();
                    break;
                case CastType.Team:
                    ProcessTeamEntry();
                    break;
            }
        }

        public void ResetCurrentBuffEntry(LdbFeedback? feedback = null)
        {
            if (feedback != null)
            {
                string msg = ScriptTemplate.CreateFeedbackReply(Queue.Current, feedback);
                Client.SendPrivateMessage((uint)Queue.Current.Requester.Instance, msg);
            }

            DynelManager.LocalPlayer.TryRemoveBuffs(Queue.Current.NanoEntry.RemoveNanoIdUponCast);

            Logger.Information("RESET TRIGGERED");
            Queue.ClearCurrent();
            Main.Ipc.BotCache.BroadcastQueueInfoMessage();
        }

        private void AttemptToBuffTarget()
        {
            var buffTarget = DynelManager.Players.FirstOrDefault(x => x.Identity == Queue.Current.Requester);

            if (buffTarget == null)
                return;

            if (buffTarget == null || Queue.Current.Requester == Identity.None)
            {
                Logger.Information($"Cast attempt on UNKNOWN character skipped.");
                ResetCurrentBuffEntry();
                return;
            }

            if (Main.SettingsJson.Data.PvpFlagCheck && buffTarget.IsPvpFlagged())
            {
                Client.SendPrivateMessage((uint)buffTarget.Identity.Instance, ScriptTemplate.CreateFeedbackReply(Queue.Current, "You are flagged."));
                ResetCurrentBuffEntry();
                return;
            }

            Logger.Information($"Attempting to cast '{Queue.Current.NanoEntry.Name}' on '{buffTarget.Name}'");

            var firstAvailableBuff = Queue.Current.NanoEntry.LevelToId.FirstOrDefault(x => x.Level <= buffTarget.Level && DynelManager.LocalPlayer.SpellList.Contains(x.Id));

            if (firstAvailableBuff == null)
            {
                Client.SendPrivateMessage((uint)buffTarget.Identity.Instance, ScriptTemplate.CreateFeedbackReply(Queue.Current, "Your level is too low."));
                ResetCurrentBuffEntry();
                return;
            }

            if (Main.SettingsJson.Data.DanceOnCast)
                Client.Send(new SocialActionCmdMessage { Unknown5 = 0x3E, Unknown = 1, Action = SocialAction.DanceBallet });

            DynelManager.LocalPlayer.Cast(buffTarget, firstAvailableBuff.Id);
        }

        private void ProcessTeamEntry()
        {
            if (Team.IsInTeam)
            {
                if (!Team.Members.Any(x => x.Identity == Queue.Current.Requester))
                {
                    if (TeamTrackerId != 0)
                        TeamTrackerId = 0;

                    Team.LeaveTeam();
                    return;
                }

                AttemptToBuffTarget();
                return;
            }

            var botInTeam = Main.Ipc.BotCache.Entries.Values.FirstOrDefault(x => x.TeamMemberId == Queue.Current.Requester.Instance);

            if (botInTeam != null)
            {
                Main.Ipc.Broadcast(new RequestTeamInviteMessage
                {
                    IsTeamTracker = false,
                    Requester = DynelManager.LocalPlayer.Identity.Instance,
                    Bot = botInTeam.Identity.Instance
                });
            }

            if (TeamTimeout.Elapsed)
            {
                Client.SendPrivateMessage((uint)Queue.Current.Requester.Instance, ScriptTemplate.TeamBuffTimeout(Queue.Current.NanoEntry.Name));
                ResetCurrentBuffEntry();
            }
        }
    }
}