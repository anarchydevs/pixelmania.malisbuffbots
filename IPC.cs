using System;
using System.IO;
using System.Linq;
using AOSharp.Common.GameData;
using System.Collections.Generic;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using AOSharp.Clientless;
using AOSharp.Clientless.Logging;

namespace MalisBuffBots
{
    public class IPC : IPCChannel
    {
        public Dictionary<Profession, int> QueueData = new Dictionary<Profession, int>();
        public Dictionary<Profession, int[]> SpellData = new Dictionary<Profession, int[]>();
        private double _updateInterval;
        private double _cachedUpdateInterval;

        public IPC(byte channelId, double updateInterval) : base(channelId)
        {
            _updateInterval = updateInterval;
            _cachedUpdateInterval = _updateInterval;

            RegisterCallback((int)IPCOpcode.CastRequest, OnActionRequestReceived);
            RegisterCallback((int)IPCOpcode.ReceiveQueueInfo, OnDisplayQueueInfoReceived);
            RegisterCallback((int)IPCOpcode.RequestQueueInfo, OnRequestQueueInfoReceived);
            RegisterCallback((int)IPCOpcode.RequestSpellListInfo, OnRequestSpellListInfoReceived);
            RegisterCallback((int)IPCOpcode.ReceiveSpellListInfo, OnDisplaySpellListInfoReceived);
        }

        public void AddQueueDataEntry(Profession prof, int entries)
        {
            if (!QueueData.ContainsKey(prof))
                QueueData.Add(prof, entries);

            QueueData[prof] = entries;
        }

        public void AddSpellDataEntry(Profession prof, int[] spells)
        {
            if (!SpellData.ContainsKey(prof))
                SpellData.Add(prof, spells);

            SpellData[prof] = spells;
        }

        public void OnUpdate(object _, double deltaTime)
        {
            _updateInterval -= deltaTime;

            if (_updateInterval > 0)
                return;

            QueueData = new Dictionary<Profession, int>();
            QueueData.Add((Profession)DynelManager.LocalPlayer.Profession, Main.QueueProcessor.Entries);
            Main.Ipc.Broadcast(new RequestQueueInfoMessage());

            _updateInterval = _cachedUpdateInterval;
        }

        private void OnActionRequestReceived(int sender, IPCMessage msg)
        {
            CastRequestMessage cMsg = (CastRequestMessage)msg;

            if (DynelManager.LocalPlayer == null)
                return;

            if ((Profession)DynelManager.LocalPlayer.Profession != cMsg.Caster)
                return;

            var requester = DynelManager.Players.FirstOrDefault(x => x.Identity.Instance == cMsg.Requester);

            if (requester == null)
                Logger.Error($"Could not find {cMsg.Requester}. Canceling request");

            Main.QueueProcessor.FinalizeBuffRequest((Profession)DynelManager.LocalPlayer.Profession, cMsg.Entries, requester);
        }

        private void OnDisplayQueueInfoReceived(int sender, IPCMessage msg)
        {
            ReceiveQueueInfoMessage qMsg = (ReceiveQueueInfoMessage)msg;

            Main.Ipc.AddQueueDataEntry(qMsg.Caster, qMsg.Entries);
        }

        private void OnRequestQueueInfoReceived(int sender, IPCMessage msg)
        {
            if (!Client.InPlay)
                return;

            Main.Ipc.Broadcast(new ReceiveQueueInfoMessage { Caster = (Profession)DynelManager.LocalPlayer.Profession, Entries = Main.QueueProcessor.Entries });
        }

        private void OnRequestSpellListInfoReceived(int sender, IPCMessage msg)
        {
            Main.Ipc.Broadcast(new ReceiveSpellListInfoMessage { Profession = (Profession)DynelManager.LocalPlayer.Profession, SpellList = DynelManager.LocalPlayer.SpellList });
        }

        private void OnDisplaySpellListInfoReceived(int sender, IPCMessage msg)
        {
            ReceiveSpellListInfoMessage sMsg = (ReceiveSpellListInfoMessage)msg;
            Main.Ipc.AddSpellDataEntry(sMsg.Profession, sMsg.SpellList);
        }

    }
}