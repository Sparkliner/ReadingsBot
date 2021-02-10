using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        private string LogDirectory { get; }
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        public LoggingService(DiscordSocketClient client, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _client = client;
            _commands = commands;

            _client.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
            LogUtilities.Log += OnLogAsync;
        }

        private async Task OnLogAsync(LogMessage msg)
        {
            if (msg.Exception is CommandException commandException)
            {
                await commandException.Context.Channel.SendMessageAsync("A catastrophic error occured, please let the dev know!");
            }

            //create dir and file
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();

            //log to file
            string logText = $"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            await File.AppendAllTextAsync(LogFile, logText + "\n");

            //log to console
            await Console.Out.WriteLineAsync(logText);
        }
    }
}
