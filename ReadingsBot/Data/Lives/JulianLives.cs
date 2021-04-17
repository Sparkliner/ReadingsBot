using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ReadingsBot.Data
{
    public class JulianLives : ISaintsLives<JulianLife>
    {
        public string Link { get; }
        public string DateFull { get; }
        [JsonPropertyName("date_rfc")]
        public string DateWithOffset { get; }
        public List<JulianLife> Commemorations { get; }

        public JulianLives(string link,
                string dateFull,
                string dateWithOffset,
                List<JulianLife> commemorations)
        {
            Link = link;
            DateFull = dateFull;
            DateWithOffset = dateWithOffset;
            Commemorations = commemorations;
        }


        public List<EmbedWithImage> GetEmbeds()
        {
            return Commemorations.Select(s => s.ToEmbed()).ToList();
        }
    }

    public class JulianLife : ISaintsLife
    {
        [JsonPropertyName("title")]
        public string Name { get; }
        [JsonPropertyName("link")]
        public string Link { get; }
        [JsonPropertyName("image")]
        public string ImageUrl { get; set; }
        [JsonPropertyName("text")]
        public string Content { get; }

        [JsonConstructor]
        public JulianLife(string name, string link, string imageUrl, string content)
        {
            Name = name;
            this.Link = link;
            this.ImageUrl = imageUrl;
            if (string.IsNullOrWhiteSpace(content))
            {
                this.Content = "No information available at this time.";
            }
            else
            {
                this.Content = content;
            }
        }

        public JulianLife(string commemoration):
            this(commemoration, "", "", "")
        {}

        public EmbedWithImage ToEmbed()
        {
            EmbedWithImage embed = new();
            string life_text = Utilities.TextUtilities.ParseWebText(Content);
            embed.ImageFile = ImageUrl;
            embed.Builder = new EmbedBuilder()
            {
                Title = Utilities.TextUtilities.ParseWebText(Name),
                Url = Link,
            }
            .WithDescription(
                (life_text.Length <= 128 ? life_text : life_text.Substring(0, 128) + $"...[Read more]({Link})"))
            .WithFooter(footer => footer.Text = Utilities.TextUtilities.ParseWebText("&copy; 1996-2001 by translator Fr. S. Janos.\nWith permission from Holy Trinity Russian Orthodox Church."));
            //just going to hardcode the copyright because I can't be bothered right now

            if (!string.IsNullOrWhiteSpace(ImageUrl))
            {
                string ImageName = ImageUrl[ImageUrl.LastIndexOf("image")..];
                embed.Builder = embed.Builder.WithImageUrl($"attachment://{ImageName}");
            }

            return embed;
        }
    }
}
