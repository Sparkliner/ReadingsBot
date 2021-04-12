using Discord;
using Discord.Commands;
using ReadingsBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadingsBot.Modules
{
    [Name("Help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commands;
        private readonly GuildService _guildService;

        private static readonly Color _color = new(114, 137, 218);

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

            foreach (var module in _commands.Modules.OrderBy(module => module.Name))
            {
                StringBuilder description = new();

                List<CommandInfo> commands = module.Commands.OrderBy(c => c.Aliases[0])
                    .Distinct(new CommandInfoEqualityComparer())
                    .ToList();

                foreach (var cmd in commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description.Append($"{prefix}{cmd.Aliases[0]}\n");
                }

                string descString = description.ToString();

                if (!string.IsNullOrWhiteSpace(descString))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = descString;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        [Summary("Get more information about a command.")]
        [Alias("h", "man")]
        public async Task HelpAsync([Remainder][Summary("The name of the command")] string command)
        {
            var result = _commands.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

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
