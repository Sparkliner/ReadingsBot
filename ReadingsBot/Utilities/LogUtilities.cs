using Discord;
using System;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class LogUtilities
    {
        public static event Func<LogMessage, Task> Log;

        public static void WriteLog(LogSeverity severity, string message)
        {
            LogMessage msg = new LogMessage(severity, "ReadingsBot", message);
            Log(msg);
        }
    }
}
