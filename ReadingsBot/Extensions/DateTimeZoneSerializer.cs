using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NodaTime;
using System;

namespace ReadingsBot.Extensions
{
    class DateTimeZoneSerializer : IBsonSerializer<DateTimeZone>
    {
        public Type ValueType => typeof(DateTimeZone);
        public static DateTimeZoneSerializer Instance { get; } = new DateTimeZoneSerializer();

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var dateTimeZone = (DateTimeZone)value;

            this.Serialize(context, dateTimeZone);
        }

        void IBsonSerializer<DateTimeZone>.Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTimeZone value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            context.Writer.WriteString(value.Id);
        }

        public DateTimeZone Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType != BsonType.String)
            {
                throw new InvalidOperationException($"Cannot deserialize DateTimeZone from BsonType {bsonType}");
            }

            string dateTimeZoneString = context.Reader.ReadString();
            DateTimeZone parseResult = DateTimeZoneProviders.Tzdb.GetZoneOrNull(dateTimeZoneString);

            if (parseResult is null)
            {
                throw new TimeZoneNotFoundException();
            }

            return parseResult;
        }
    }
}
