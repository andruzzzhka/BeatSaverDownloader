using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeatSaverDownloader
{
    static class Debug
    {
#if DEBUG
        static StreamWriter logWriter = new StreamWriter("bsd_log.txt", false);
#endif


        static public void Log(string log)
        {
#if DEBUG
            logWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + log);
            logWriter.Flush();
#else
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + log);
#endif
        }
    }
}
