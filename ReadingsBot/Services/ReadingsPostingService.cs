using Discord;
using Discord.WebSocket;
using ReadingsBot.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReadingsBot
{

    public class ReadingsPostingService
    {
        private readonly DiscordSocketClient _client;
        private readonly OcaLivesCacheService _ocaLives;
        private readonly BlogCacheService _blogs;

        private readonly int _postingDelay = 1500;

        public ReadingsPostingService(DiscordSocketClient client, OcaLivesCacheService ocaLives, BlogCacheService blogs)
        {
            _client = client;
            _ocaLives = ocaLives;
            _blogs = blogs;
        }

        public async Task PostLivesAsync(ulong channelId)
        {
            await PostBulkEmbedAsync(
                Modules.ReadingsModule.GetEmbedColor,
                (_ocaLives.GetLives()).GetEmbeds(),
                channelId);
        }

        public async Task<List<BlogSubscription>> PostBlogsAsync(ulong channelId, BlogsReadingInfo blogsReading = null)
        {
            (List<EmbedBuilder> embeds, List<BlogSubscription> newSubs) = _blogs.GetLatestBlogPostEmbeds(blogsReading);
            await PostBulkEmbedAsync(
                Modules.ReadingsModule.GetEmbedColor,
                embeds,
                channelId);
            return newSubs;
        }

        private async Task PostBulkEmbedAsync(Color color, List<EmbedBuilder> embeds, ulong channelId)
        {
            var channel = _client.GetChannel(channelId) as ISocketMessageChannel;
            foreach (EmbedBuilder embed in embeds)
            {
                await channel.SendMessageAsync("", false, embed.WithColor(color).Build());
                await Task.Delay(_postingDelay);
            }
        }

        private async Task PostBulkEmbedAsync(Color color, List<EmbedWithImage> embedWIs, ulong channelId)
        {
            var channel = _client.GetChannel(channelId) as ISocketMessageChannel;
            foreach (EmbedWithImage embedWI in embedWIs)
            {
                if (!string.IsNullOrWhiteSpace(embedWI.ImageFile))
                {
                    await channel.SendFileAsync(embedWI.ImageFile, embed: embedWI.Builder.WithColor(color).Build());
                }
                else
                {
                    await channel.SendMessageAsync("", false, embedWI.Builder.WithColor(color).Build());
                }
                await Task.Delay(_postingDelay);
            }
        }
    }
}
