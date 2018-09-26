using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatSaverDownloader
{
    class Logger
    {
        private string loggerName;

        public Logger(string _name)
        {
            loggerName = _name;
        }
        
        public static void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[BeatSaverDownloader @ " + DateTime.Now.ToString("HH:mm")+"] "+message);
        }

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[BeatSaverDownloader @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[BeatSaverDownloader @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

        public static void Exception(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[BeatSaverDownloader @ " + DateTime.Now.ToString("HH:mm") + "] " + message);
        }

    }
}
