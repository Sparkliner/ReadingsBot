using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using ReadingsBot.Extensions;
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

        public ReadingsPostingService(DiscordSocketClient client, OCALivesCacheService ocaLives)
        {
            _client = client;
            _ocaLives = ocaLives;
        }

        public async Task PostLives(ulong channelId)
        {
            await PostBulkEmbed(
                Modules.ReadingsModule.GetColor(),
                (await _ocaLives.GetLives()).GetEmbeds(),
                channelId
                );
        }

        private async Task PostBulkEmbed(Color color, List<EmbedBuilder> embeds, ulong channelId)
        {
            var channel = _client.GetChannel(channelId) as ISocketMessageChannel;
            foreach (EmbedBuilder embed in embeds)
            {
                await channel.SendMessageAsync("", false, embed.WithColor(color).Build());
                await Task.Delay(_postingDelay);
            }
        }

        private async Task PostBulkEmbed(Color color, List<EmbedWithImage> embedWIs, ulong channelId)
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
