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
        public IPCQueueData QueueData = new IPCQueueData();

        public IPCSpellData SpellData = new IPCSpellData();
        private double _updateInterval;
        private double _cachedUpdateInterval;


        public class IPCSpellData
        {
            public Dictionary<Profession, int[]> SpellData = new Dictionary<Profession, int[]>();

            public bool ContainsKey(Profession prof) => SpellData.ContainsKey(prof);

            public void AddOrUpdate(Profession prof, int[] spellList)
            {
                if (!SpellData.ContainsKey(prof))
                    SpellData.Add(prof, spellList);

                SpellData[prof] = spellList;
            }

            public int[] GetValue(Profession proffesion) => SpellData[proffesion];

            public bool ContainsNanoEntry(Profession prof, NanoEntry nanoEntry)
            {
                if (!SpellData.TryGetValue(prof, out var spellData))
                    return false;

                return spellData.Any(s => nanoEntry.ContainsId(s));
            }
        }

        public class IPCQueueData
        {
            public Dictionary<Profession, QueueData> QueueData = new Dictionary<Profession, QueueData>();

            public bool ContainsKey(Profession prof) => QueueData.ContainsKey(prof);
            public void AddOrUpdate(Profession prof, QueueData queueData)
            {
                if (!QueueData.ContainsKey(prof))
                    QueueData.Add(prof, queueData);

                QueueData[prof] = queueData;
            }

            public IOrderedEnumerable<KeyValuePair<Profession, QueueData>> OrderByQueueEntries() => QueueData.OrderBy(x => x.Value.Entries);
        }

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

        public void AddQueueDataEntry(Profession prof, QueueData queueData)
        {
            QueueData.AddOrUpdate(prof, queueData);
        }

        public void AddSpellDataEntry(Profession prof, int[] spells)
        {
            SpellData.AddOrUpdate(prof, spells);
        }

        public void OnUpdate(object _, double deltaTime)
        {
            _updateInterval -= deltaTime;

            if (_updateInterval > 0)
                return;

            QueueData = new IPCQueueData();
            QueueData.AddOrUpdate((Profession)DynelManager.LocalPlayer.Profession, new QueueData { Identity = DynelManager.LocalPlayer.Identity, Entries = Main.QueueProcessor.Entries });
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

            Main.Ipc.AddQueueDataEntry(qMsg.Caster, qMsg.QueueData);
        }

        private void OnRequestQueueInfoReceived(int sender, IPCMessage msg)
        {
            if (!Client.InPlay)
                return;

            Main.Ipc.Broadcast(new ReceiveQueueInfoMessage
            {
                Caster = (Profession)DynelManager.LocalPlayer.Profession,
                QueueData = new QueueData 
                { 
                    Entries = Main.QueueProcessor.Entries, 
                    Identity = DynelManager.LocalPlayer.Identity 
                }
            });
        }

        private void OnRequestSpellListInfoReceived(int sender, IPCMessage msg)
        {
            Main.Ipc.Broadcast(new ReceiveSpellListInfoMessage 
            { 
                Profession = (Profession)DynelManager.LocalPlayer.Profession, 
                SpellList = DynelManager.LocalPlayer.SpellList 
            });
        }

        private void OnDisplaySpellListInfoReceived(int sender, IPCMessage msg)
        {
            ReceiveSpellListInfoMessage sMsg = (ReceiveSpellListInfoMessage)msg;
            Main.Ipc.AddSpellDataEntry(sMsg.Profession, sMsg.SpellList);
        }
    }
}