using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NodaTime;
using System;

namespace ReadingsBot.Extensions
{
    class InstantSerializer : IBsonSerializer<Instant>
    {
        public Type ValueType => typeof(Instant);
        public static InstantSerializer Instance { get; } = new InstantSerializer();

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var instant = (Instant)value;

            this.Serialize(context, instant);
        }

        void IBsonSerializer<Instant>.Serialize(BsonSerializationContext context, BsonSerializationArgs args, Instant value)
        {
            context.Writer.WriteDateTime(value.ToUnixTimeMilliseconds());
        }

        public Instant Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType != BsonType.DateTime)
            {
                throw new InvalidOperationException($"Cannot deserialize Instant from BsonType {bsonType}");
            }

            var unixTimeMilliseconds = context.Reader.ReadDateTime();

            return Instant.FromUnixTimeMilliseconds(unixTimeMilliseconds);
        }
    }
}
