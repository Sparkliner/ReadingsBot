using Discord;

namespace ReadingsBot.Modules
{
    public abstract class InfoEmbedModule : ColorModule
    {
        protected readonly ReadingsBotVersionInfo _versionInfo;

        protected InfoEmbedModule(ReadingsBotVersionInfo versionInfo, Color color)
            : base(color)
        {
            _versionInfo = versionInfo;
        }

        protected override EmbedBuilder BasicEmbedBuilder => base.BasicEmbedBuilder.WithAuthor(_versionInfo.Header);
    }
}