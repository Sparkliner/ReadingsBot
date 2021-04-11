using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NodaTime;
using NodaTime.Text;
using System;

namespace ReadingsBot.Serialization.Bson
{
    class LocalTimeSerializer : IBsonSerializer<LocalTime>
    {
        public Type ValueType => typeof(LocalTime);
        public static LocalTimeSerializer Instance { get; } = new LocalTimeSerializer();

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var localTime = (LocalTime)value;

            this.Serialize(context, localTime);
        }

        void IBsonSerializer<LocalTime>.Serialize(BsonSerializationContext context, BsonSerializationArgs args, LocalTime value)
        {
            context.Writer.WriteString(
                LocalTimePattern.CreateWithInvariantCulture("t")
                .Format(value));
        }

        public LocalTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType != BsonType.String)
            {
                throw new InvalidOperationException($"Cannot deserialize LocalTime from BsonType {bsonType}");
            }

            string localTimeString = context.Reader.ReadString();
            var parseResult = LocalTimePattern
                .CreateWithInvariantCulture("t")
                .Parse(localTimeString);

            if (!parseResult.Success)
            {
                throw parseResult.Exception;
            }

            return parseResult.Value;
        }
    }
}
