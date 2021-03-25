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
            wrap.OnReactionAdded += (r) => { _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { _ = Task.Run(() => reactionRemoved(r)); };
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
    public class CommandInfoEqualityComparer : IEqualityComparer<CommandInfo>
    {
        public bool Equals(CommandInfo? x, CommandInfo? y)
        {
            if ((x is null) || (y is null))
                return false;
            return x.Name.Equals(y.Name);
        }
        public int GetHashCode(CommandInfo obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
