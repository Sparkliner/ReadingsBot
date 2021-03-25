using Discord;
using NodaTime;

namespace ReadingsBot.Data
{
    public class BlogPost
    {
        public string BlogName { get; set; }
        public OffsetDateTime PostDateTime { get; set; }
        public string PostTitle { get; set; }
        public string Author { get; set; }
        public string AuthorImageUrl { get; set; }
        public string PostImageUrl { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }

        public EmbedBuilder ToEmbed()
        {

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = this.PostTitle,
                Url = this.Link,
                Author = new EmbedAuthorBuilder().WithName($"{this.BlogName} - {this.Author}").WithIconUrl(this.AuthorImageUrl),
            }
            .WithDescription(this.Description)
            .WithFooter(footer => footer.Text = Utilities.TextUtilities.ParseWebText($"&copy; {this.Author}"));
            //just going to hardcode the copyright because I can't be bothered right now

            if (!string.IsNullOrWhiteSpace(this.PostImageUrl))
            {
                builder = builder.WithImageUrl(this.PostImageUrl);
            }

            return builder;
        }
    }
}
