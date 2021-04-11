using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace ReadingsBot
{
    public class ReadingsBotVersionInfo
    {
        protected readonly string name = Assembly.GetExecutingAssembly().GetName().Name;
        protected readonly string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private readonly DiscordSocketClient _client;

        public string Name => name;
        public string Version => version;

        public EmbedAuthorBuilder Header => new EmbedAuthorBuilder()
            .WithName($"{name} {version}")
            .WithIconUrl(_client.CurrentUser.GetAvatarUrl());

        public ReadingsBotVersionInfo(DiscordSocketClient client)
        {
            _client = client;
        }
    }
}
