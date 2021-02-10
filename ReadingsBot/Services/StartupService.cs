using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public StartupService(
            IServiceProvider provider,
            DiscordSocketClient client,
            CommandService commands,
            IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _client = client;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            string discordToken = _config["token"];
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Bot token missing from the environment variables.");

            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();

            //Load commands and modules
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }
    }
}
