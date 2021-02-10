using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private readonly GuildService _guildService;
        public CommandHandler(
            DiscordSocketClient client,
            CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider,
            GuildService guildService)
        {
            _client = client;
            _commands = commands;
            _config = config;
            _provider = provider;
            _guildService = guildService;

            _client.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) 
                return; //check message is from user/bot

            if (msg.Author.Id == _client.CurrentUser.Id) 
                return; //ignore self

            if (msg.Author.IsBot)
                return; //ignore other bots

            var context = new SocketCommandContext(_client, msg);

            //check for command prefix
            int argPos = 0;

            if (msg.HasStringPrefix(await _guildService.GetGuildPrefix(context.Guild.Id), ref argPos) || msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                //Execute command
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                //reply with error on failure
                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}
