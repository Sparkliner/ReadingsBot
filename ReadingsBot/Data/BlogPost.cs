using Discord;
using NodaTime;
using System;
using System.Text;

namespace ReadingsBot.Data
{
    public sealed class BlogPost : IEquatable<BlogPost>
    {
        public BlogId BId { get; }
        public PostId PId { get; }
        public string BlogName => BId.BlogName;
        public string Author => BId.Author;
        public string PostTitle => PId.PostTitle;
        public OffsetDateTime PostDateTime => PId.PostDateTime;       
        public string AuthorImageUrl { get; }
        public string PostImageUrl { get; }
        public string Link { get; }
        public string Description { get; }

        public BlogPost(string blogName, string author, string postTitle, OffsetDateTime postDateTime, string authorImageUrl, string postImageUrl, string link, string description)
        {
            BId = new BlogId(blogName, author);
            PId = new PostId(postTitle, postDateTime);
            AuthorImageUrl = authorImageUrl;
            PostImageUrl = postImageUrl;
            Link = link;
            Description = description;
        }

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

            if (!string.IsNullOrWhiteSpace(this.PostImageUrl))
            {
                builder = builder.WithImageUrl(this.PostImageUrl);
            }

            return builder;
        }

        public override bool Equals(object? obj)
        {
            if (obj is BlogPost post)
            {
                return this.Equals(post);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(BlogPost? other)
        {
            if (other is null)
                return false;

            return this.BId == other.BId && this.PId == other.PId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine<BlogId, PostId>(BId, PId);
        }
    }

    public readonly struct PostId : IEquatable<PostId>
    {
        public string PostTitle { get; }
        public OffsetDateTime PostDateTime { get; }
        
        public PostId(string postTitle, OffsetDateTime postDateTime)
        {
            PostTitle = postTitle;
            PostDateTime = postDateTime;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PostId id)
            {
                return this.Equals(id);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(PostId other)
        {
            return this.PostTitle == other.PostTitle && this.PostDateTime.Equals(other.PostDateTime);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine<string, OffsetDateTime>(PostTitle, PostDateTime);
        }

        public static bool operator ==(PostId left, PostId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PostId left, PostId right)
        {
            return !(left == right);
        }
    }
}
