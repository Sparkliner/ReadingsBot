namespace ReadingsBot.Data
{
    public class GuildEntity
    {
        public MongoDB.Bson.ObjectId Id { get; set; }
        public ulong GuildID { get; set; }
        public string Prefix { get; set; } = null!;
    }
}
