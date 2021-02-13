using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ReadingsBot.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReadingsBot.Extensions
{
    public static class Extensions
    {
        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Func<SocketReaction, Task> reactionAdded, Func<SocketReaction, Task> reactionRemoved = null)
        {
            if (reactionRemoved == null)
                reactionRemoved = _ => Task.CompletedTask;

            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { var _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { var _ = Task.Run(() => reactionRemoved(r)); };
            return wrap;
        }

        public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage)
        {
            if (lastPage != null)
                return embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}"));
            else
                return embed.WithFooter(efb => efb.WithText(curPage.ToString()));
        }
    }
    public class CommandInfoEqualityComparer: IEqualityComparer<CommandInfo>
    {
        public bool Equals(CommandInfo? cmd1, CommandInfo? cmd2)
        {
            if ((cmd1 is null) || (cmd2 is null))
                return false;
            return cmd1.Name.Equals(cmd2.Name);
        }
        public int GetHashCode(CommandInfo cmd)
        {
            return cmd.Name.GetHashCode();
        }
    }

    public class EmbedWithImage
    {
        public EmbedBuilder Builder;
        public string ImageFile;
    }

}
