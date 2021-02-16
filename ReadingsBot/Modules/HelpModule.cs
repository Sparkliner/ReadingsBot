using Discord;
using Discord.Commands;
using ReadingsBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReadingsBot.Modules
{
    [Name("Help")]
    public class HelpModule: ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commands;
        private readonly GuildService _guildService;

        private static readonly Color _color = new Color(114, 137, 218);

        public HelpModule(CommandService commands, GuildService guildService)
        {
            _commands = commands;
            _guildService = guildService;
        }

        public static Color GetColor()
        {
            return _color;
        }

        [Command("help")]
        [Summary("Get a list of all commands.")]
        [Alias("h")]
        public async Task HelpAsync()
        {
            string prefix = await _guildService.GetGuildPrefix(Context.Guild.Id);
            var builder = new EmbedBuilder()
            {
                Color = _color,
                Description = "These are the commands you can use"
            };

            foreach (var module in _commands.Modules)
            {
                string description = null;

                List<CommandInfo> commands = module.Commands.ToList()
                    .OrderBy(c => c.Aliases[0])
                    .Distinct(new CommandInfoEqualityComparer())
                    .ToList();

                foreach (var cmd in commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Aliases[0]}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        [Summary("Get more information about a command.")]
        [Alias("h", "man")]
        public async Task HelpAsync([Remainder] [Summary("The name of the command")] string command)
        {
            var result = _commands.Search(Context, command);

            if(!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            //string prefix = await _guildService.GetGuildPrefix(Context.Guild.Id);
            var builder = new EmbedBuilder()
            {
                Color = _color,
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = (cmd.Parameters.Count > 0 ? $"Parameters:\n\u2003{string.Join("\n\u2003", cmd.Parameters.Select(p => p.Name + ": " + p.Summary))}\n" : "") +
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
