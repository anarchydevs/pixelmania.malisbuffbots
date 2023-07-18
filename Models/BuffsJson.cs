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
    public class BuffsJson : JsonFile<Dictionary<Profession, List<NanoEntry>>>
    {
        public readonly Dictionary<Profession, List<NanoEntry>> Entries;

        public BuffsJson(string jsonPath) : base(jsonPath)
        {
            Entries = _data == null ? DataMigrate.ConvertToNewDb() : _data;
        }

        public bool FindByTags(IEnumerable<string> tags, out Dictionary<Profession, List<NanoEntry>> result)
        {
            result = new Dictionary<Profession, List<NanoEntry>>();

            if (tags == null || tags.Count() == 0)
                return false;

            var distinctTags = tags.Distinct();

            foreach (var entriesByProf in Entries)
            {
                List<NanoEntry> results = entriesByProf.Value
                    .Where(x => distinctTags.Any(y => x.Tags.Contains(y) || int.TryParse(y, out int id) && x.ContainsId(id)))
                    .ToList();

                if (results.Count() == 0)
                    continue;

                result.Add(entriesByProf.Key, results);
            }

            return result.Count > 0;
        }

        public bool FindByIds(IEnumerable<int> ids, out Dictionary<Profession, List<NanoEntry>> result)
        {
            result = new Dictionary<Profession, List<NanoEntry>>();

            if (ids == null || ids.Count() == 0)
                return false;

            foreach (var entriesByProf in Entries)
            {
                List<NanoEntry> results = new List<NanoEntry>();

                foreach (var nanoEntry in entriesByProf.Value)
                {
                    if (!ids.Any(x => nanoEntry.LevelToId.Any(y=>y.Id == x)))
                        continue;

                    results.Add(nanoEntry);
                }

                if (results.Count() == 0)
                    continue;

                result.Add(entriesByProf.Key, results);
            }

            return result.Count > 0;
        }   
    }

    public class BuffEntry
    {
        public SimpleChar Character;
        public NanoEntry NanoEntry;

        public BuffEntry(SimpleChar simpleChar, NanoEntry nanoEntry)
        {
            Character = simpleChar;
            NanoEntry = nanoEntry;
        }
    }
}
