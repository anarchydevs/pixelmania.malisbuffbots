using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MalisBuffBots
{
    public class Path
    {
        public static string PLUGIN_DIR = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string SETTINGS_JSON => $"{PLUGIN_DIR}\\JSON\\Settings.json";
        public static string BUFF_JSON => $"{PLUGIN_DIR}\\JSON\\BuffsDb.json";
        public static string REBUFF_JSON => $"{PLUGIN_DIR}\\JSON\\RebuffInfo.json";
        public static string USERRANK_JSON => $"{PLUGIN_DIR}\\JSON\\UserRanks.json";
    }
}

