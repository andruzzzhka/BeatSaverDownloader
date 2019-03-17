using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BeatSaverDownloader.Misc
{
    class Logger
    {
        private static StreamWriter logWriter = new StreamWriter("Downloader.log") { AutoFlush = true };

        private static string _assemblyName;
        public static string AssemblyName
        {
            get
            {
                if (string.IsNullOrEmpty(_assemblyName))
                    _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

                return _assemblyName;
            }
        }

        private static ConsoleColor _lastColor;

        public static void Log(object message)
        {
            _lastColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + AssemblyName + " | LOG] " + message);
            Console.ForegroundColor = _lastColor;
            if (logWriter != null)
                logWriter.WriteLine("[" + AssemblyName + " | LOG] " + message);
        }

        public static void Warning(object message)
        {
            _lastColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[" + AssemblyName + " | WARNING] " + message);
            Console.ForegroundColor = _lastColor;
            if (logWriter != null)
                logWriter.WriteLine("[" + AssemblyName + " | WARNING] " + message);
        }

        public static void Error(object message)
        {
            _lastColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[" + AssemblyName + " | ERROR] " + message);
            Console.ForegroundColor = _lastColor;
            if (logWriter != null)
                logWriter.WriteLine("[" + AssemblyName + " | ERROR] " + message);
        }

        public static void Exception(object message)
        {
            _lastColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + AssemblyName + " | CRITICAL] " + message);
            Console.ForegroundColor = _lastColor;
            if (logWriter != null)
                logWriter.WriteLine("[" + AssemblyName + " | CRITICAL] " + message);
        }
    }
}
