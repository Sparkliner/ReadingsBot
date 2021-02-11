using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReadingsBot
{
    public class ReadingsPostingService
    {
        private readonly DiscordSocketClient _client;
        private readonly OCALivesCacheService _ocaLives;

        private readonly int _postingDelay = 1500;

        public enum ReadingType
        {
            OCALives
        }

        public ReadingsPostingService(DiscordSocketClient client, OCALivesCacheService ocaLives)
        {
            _client = client;
            _ocaLives = ocaLives;
        }

        public async Task PostReadings(ReadingType type, ulong channelId)
        {
            switch (type)
            {
                case ReadingType.OCALives:
                    await PostLives(channelId);
                    break;
                default:
                    throw new ArgumentException("This reading type is not yet implemented");
            }
        }

        private async Task PostLives(ulong channelId)
        {
            await PostBulkEmbed(
                Modules.ReadingsModule.GetColor(),
                (await _ocaLives.GetLives()).GetEmbeds(),
                channelId
                );
        }

        public async Task PostBulkEmbed(Color color, List<EmbedBuilder> embeds, ulong channelId)
        {
            var channel = _client.GetChannel(channelId) as ISocketMessageChannel;
            foreach (EmbedBuilder embed in embeds)
            {
                await channel.SendMessageAsync("", false, embed.WithColor(color).Build());
                await Task.Delay(_postingDelay);
            }
        }

        public async Task PostBulkEmbed(Color color, List<EmbedWithImage> embedWIs, ulong channelId)
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
