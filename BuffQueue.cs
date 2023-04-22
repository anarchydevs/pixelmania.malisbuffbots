using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisBuffBots
{
    public class BuffQueue 
    {
        public BuffEntry CurrentBuffEntry;
        private Queue<BuffEntry> _queue;
        private double _graceTime;
        private double _waitTime;
        private bool _teamRequestSent = false;

        public BuffQueue(double graceTime = 0.5f)
        {
            _queue = new Queue<BuffEntry>();
            _graceTime = graceTime;
        }

        public void Enqueue(BuffEntry buffEntry) => _queue.Enqueue(buffEntry);

        public bool IsEmpty() => _queue.Count == 0 && CurrentBuffEntry == null;

        public bool TimerExpired(double deltaTime)
        {
            _graceTime -= deltaTime;
            _waitTime -= deltaTime;

            if (_graceTime >= 0)
                return false;

            if (_waitTime < 0)
            {
                Logger.Information("Obtanining new buff entry");
                CurrentBuffEntry = GetNextBuffEntry();
                _waitTime = CurrentBuffEntry == null ? 0 : CurrentBuffEntry.NanoEntry.TimeOut;
            }

            _graceTime = 0.5f;
            return true;
        }

        public void ProcessCurrentBuffEntry()
        {
            if (CurrentBuffEntry == null)
                return;

            switch (CurrentBuffEntry.NanoEntry.Type)
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
            if (CurrentBuffEntry != null)
            {
                Logger.Warning($"Casting '{CurrentBuffEntry.NanoEntry.Name}' on '{CurrentBuffEntry.Character.Name}' failed. Removing entry.");
                ResetCurrentBuffEntry();
            }

            if (_queue.Count == 0)
                return null;

            return _queue.Dequeue();
        }

        public void ResetCurrentBuffEntry()
        {
            Utils.LeaveTeam();
            DynelManager.LocalPlayer.RemoveBuff(CurrentBuffEntry.NanoEntry.RemoveNanoIdUponCast);
            _teamRequestSent = false;
            CurrentBuffEntry = null;
            _waitTime = 0;
        }

        private void AttemptToBuffTarget()
        {
            if (Main.Settings.PvpFlagCheck && CurrentBuffEntry.Character.IsPvpFlagged())
            {
                Logger.Information($"Cast attempt '{CurrentBuffEntry.NanoEntry.Name}' on '{CurrentBuffEntry.Character.Name}' skipped (character is flagged)");
                ResetCurrentBuffEntry();
                return;
            }

            Logger.Information($"Attempting to cast '{CurrentBuffEntry.NanoEntry.Name}' on '{CurrentBuffEntry.Character.Name}', Remaining time: {Math.Round(_waitTime, 2)} seconds.");
            var levelToId = CurrentBuffEntry.NanoEntry.LevelToId.First(x => x.Key <= CurrentBuffEntry.Character.Level).Value;
            DynelManager.LocalPlayer.Cast(CurrentBuffEntry.Character, levelToId);
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
                Logger.Information($"Team invite sent to '{CurrentBuffEntry.Character.Name}'");
                Team.Invite(CurrentBuffEntry.Character);
                _teamRequestSent = true;
            }
        }
    }
}