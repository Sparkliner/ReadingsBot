using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly GuildService _guildService;
        public CommandHandler(
            DiscordSocketClient client,
            CommandService commands,
            IServiceProvider provider,
            GuildService guildService)
        {
            _client = client;
            _commands = commands;
            _provider = provider;
            _guildService = guildService;

            _client.MessageReceived += OnMessageReceivedAsync;

            _commands.CommandExecuted += OnCommandExecutedAsync;
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg)
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
                await _commands.ExecuteAsync(context, argPos, _provider);
            }
        }
    }
}
