using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NodaTime;
using NodaTime.Text;
using System;

namespace ReadingsBot.Extensions
{
    class ZonedDateTimeSerializer : IBsonSerializer<ZonedDateTime>
    {
        public Type ValueType => typeof(ZonedDateTime);
        public static ZonedDateTimeSerializer Instance { get; } = new ZonedDateTimeSerializer();

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var zonedDateTime = (ZonedDateTime)value;

            this.Serialize(context, zonedDateTime);
        }

        void IBsonSerializer<ZonedDateTime>.Serialize(BsonSerializationContext context, BsonSerializationArgs args, ZonedDateTime value)
        {
            context.Writer.WriteString(
                ZonedDateTimePattern.CreateWithInvariantCulture("G", DateTimeZoneProviders.Tzdb)
                .Format(value));
        }

        public ZonedDateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType != BsonType.String)
            {
                throw new InvalidOperationException($"Cannot deserialize ZonedDateTime from BsonType {bsonType}");
            }

            string zonedDateTimeString = context.Reader.ReadString();
            var parseResult = ZonedDateTimePattern
                .CreateWithInvariantCulture("G", DateTimeZoneProviders.Tzdb)
                .Parse(zonedDateTimeString);

            if (!parseResult.Success)
            {
                throw parseResult.Exception;
            }

            return parseResult.Value;
        }
    }
}
