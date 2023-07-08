﻿using AOSharp.Clientless;
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

            Client.MessageReceived += OnMessageReceived;
            Client.OnUpdate -= OnUpdate;
        }

        public void OnMessageReceived(object _, Message msg)
        {
            if (msg.Header.PacketType != PacketType.N3Message)
                return;

            N3Message n3Msg = (N3Message)msg.Body;

            if (n3Msg.Identity.Instance != Client.LocalDynelId)
                return;

            if (n3Msg.N3MessageType != N3MessageType.Buff)
                return;

            ProcessBuffMessage((BuffMessage)n3Msg);
        }

        private void ProcessBuffMessage(BuffMessage buffMsg)
        {
            if (DynelManager.LocalPlayer.Buffs.Contains(buffMsg.Buff.Instance))
                return;

            if (!Main.BuffsJson.FindByIds(new List<int> { buffMsg.Buff.Instance }, out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            var expiredNano = entries.FirstOrDefault();

            foreach (var nano in expiredNano.Value)
            {
                if (!nano.ContainsId(buffMsg.Buff.Instance))
                    continue;

                Main.QueueProcessor.FinalizeBuffRequest(expiredNano.Key, new List<NanoEntry> { nano }, DynelManager.LocalPlayer);
            }
        }

        private void TryFindBuffs(IEnumerable<BuffInfo> buffInfo)
        {
            if (buffInfo == null || buffInfo.Count() == 0)
                return;

            if (!Main.BuffsJson.FindByTags(buffInfo.SelectMany(x => x.Buffs), out Dictionary<Profession, List<NanoEntry>> entries))
                return;

            List<NanoEntry> missingBuffs = entries.Values.SelectMany(x => x).ToList();

            foreach (var entry in missingBuffs.ToList())
            {
                foreach (var buff in DynelManager.LocalPlayer.Buffs.Select(x => x.Id))
                {
                    if (entry.ContainsId(buff) || entry.Type == CastType.Team)
                        missingBuffs.Remove(entry);
                }
            }

            if (Main.BuffsJson.FindByTags(missingBuffs.Select(x => x.Tags.FirstOrDefault()), out Dictionary<Profession, List<NanoEntry>> entrsies))
            {
                foreach (var entry in entrsies)
                    Main.QueueProcessor.FinalizeBuffRequest(entry.Key, entry.Value, DynelManager.LocalPlayer);
            }
        }
    }
}