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
        private RebuffJson _rebuffInfo;
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

            TryFindBuffs(_rebuffInfo.GenericEntries);
            TryFindBuffs(_rebuffInfo.LocalProfEntries);

            BuffStatus.BuffChanged += OnBuffChanged;
            Client.OnUpdate -= OnUpdate;
        }


        private void OnBuffChanged(object sender, BuffChangedArgs buffArgs)
        {
            if (buffArgs.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (buffArgs.Status == BuffState.Refreshed)
            {
                Logger.Information($"Somebody refreshed my buff with id: {buffArgs.Id}");
                return;
            }

            ProcessBuffArgs(buffArgs);
        }

        private void ProcessBuffArgs(BuffChangedArgs buffArgs)
        {
            if (!Main.BuffsJson.FindById(buffArgs.Id, out (Profession, NanoEntry) expiredNano))
                return;

            if (!_rebuffInfo.Contains(expiredNano.Item2.Tags))
            {
                Logger.Information($"Buff with id {buffArgs.Id} not found in local RebuffInfo. Skipping rebuff attempt.");
                return;
            }

            Main.QueueProcessor.FinalizeBuffRequest(expiredNano.Item1, expiredNano.Item2, DynelManager.LocalPlayer);
        }

        private void TryFindBuffs(IEnumerable<BuffInfo> buffInfo)
        {
            if (buffInfo == null || buffInfo.Count() == 0)
                return;

            if (!Main.BuffsJson.FindByTags(buffInfo.SelectMany(x => x.Buffs), out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            Dictionary<Profession, List<NanoEntry>> missingBuffs = entries.ToDictionary(entry => entry.Key, entry => entry.Value);

            foreach (var entry in entries)
            {
                foreach (var buff in DynelManager.LocalPlayer.Buffs.Select(x => x.Id))
                {
                    var buffToRemove = entry.Value.FirstOrDefault(x => x.ContainsId(buff));

                    if (buffToRemove == null || buffToRemove.Type == CastType.Team)
                        continue;

                    missingBuffs[entry.Key].Remove(buffToRemove);
                }
            }

            foreach (var entry in missingBuffs)
                Main.QueueProcessor.FinalizeBuffRequest(entry.Key, entry.Value, DynelManager.LocalPlayer);
        }
    }
}