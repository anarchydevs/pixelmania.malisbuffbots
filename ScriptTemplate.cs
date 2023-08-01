using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using MalisBuffBots;
using Scriban;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MalisBuffBots
{
    public class ScriptTemplate
    {
        public static string Create()
        {
            var template = Template.Parse(File.ReadAllText($"{Utils.PluginDir}\\Templates\\HelpTemplate.txt"));

            return template.Render(new
            {
                Buffinfo = new
                {
                    Botname = DynelManager.LocalPlayer.Name,
                    Db = Main.BuffsJson.Entries.Select(kv => new
                    {
                        Prof = kv.Key,
                        Id = (int)kv.Key,
                        Status = Main.Ipc.QueueData.ContainsKey(kv.Key),
                        Entries = kv.Value.Select(entry => new
                        {
                            Tag = entry.Tags[0],
                            Nanoname = entry.Name,
                            ncu = new Buff(entry.LevelToId[0].Id).NanoItem.NCU
                        }).ToList()
                    }).ToList()
                },
            });
        }
    }
}