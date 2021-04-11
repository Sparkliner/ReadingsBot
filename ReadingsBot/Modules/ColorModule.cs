using Discord;
using Discord.Commands;

namespace ReadingsBot.Modules
{
    public abstract class ColorModule : ModuleBase<SocketCommandContext>
    {
        protected Color EmbedColor { get; }

        protected ColorModule(Color color)
        {
            EmbedColor = color;
        }

        protected virtual EmbedBuilder BasicEmbedBuilder => new EmbedBuilder().WithColor(EmbedColor);
    }
}