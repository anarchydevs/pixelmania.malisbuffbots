using AOSharp.Clientless;
using AOSharp.Clientless.Chat;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
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
    public class RebuffProcessor
    {
        public RebuffJson _rebuffInfo;
        private double _initDelay;

        public RebuffProcessor(RebuffJson rebuffInfo, float initDelay = 1f)
        {
            _initDelay = initDelay;
            _rebuffInfo = rebuffInfo;
            Client.OnUpdate += OnUpdate;
            Logger.Information("Rebuff tracking initiated.");
        }

        private void OnUpdate(object sender, double deltaTime)
        {
            _initDelay -= deltaTime;

            if (_initDelay > 0)
                return;

            TryFindBuffs(_rebuffInfo.LocalPlayerRebuffTags());

            BuffStatus.BuffChanged += OnBuffChanged;
            Client.OnUpdate -= OnUpdate;
        }


        private void OnBuffChanged(object sender, BuffChangedArgs buffArgs)
        {
            if (buffArgs.Identity != DynelManager.LocalPlayer.Identity)
                return;

            Logger.Information($"Buff change triggered: {buffArgs.Id}");
            ProcessBuffArgs(buffArgs);
        }

        private void ProcessBuffArgs(BuffChangedArgs buffArgs)
        {
            if (DynelManager.LocalPlayer.Buffs.Find(buffArgs.Id, out Buff buff) && buff.Cooldown.RemainingTime > 0.05f * buff.NanoItem.TotalTime)
                return;

            if (!Main.BuffsJson.FindById(buffArgs.Id, out (Profession, NanoEntry) expiredNano))
            {
                Logger.Information($"Couldn't find buff with id: {buffArgs.Id}");
                return;
            }

            if (!_rebuffInfo.Contains(expiredNano.Item2.Tags))
            {
                DynelManager.LocalPlayer.RemoveBuff(buff.Id);
                Logger.Information($"Buff with id {buffArgs.Id} not found in local RebuffInfo. Removing from ncu.");
                return;
            }

            Main.QueueProcessor.FinalizeBuffRequest(expiredNano.Item1, expiredNano.Item2, DynelManager.LocalPlayer);
        }

        private void TryFindBuffs(IEnumerable<string> buffTags)
        {
            if (buffTags.Count() == 0)
                return;

            if (!Main.BuffsJson.FindMissingBuffs(buffTags, out Dictionary<Profession, List<NanoEntry>> missingBuffs))
                return;

            Main.QueueProcessor.RequestBuffs(missingBuffs, DynelManager.LocalPlayer);
        }
    }
}