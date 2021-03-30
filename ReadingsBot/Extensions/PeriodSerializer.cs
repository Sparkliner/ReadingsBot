using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NodaTime;
using NodaTime.Text;
using System;

namespace ReadingsBot.Extensions
{
    class PeriodSerializer : IBsonSerializer<Period>
    {
        public Type ValueType => typeof(Period);
        public static PeriodSerializer Instance { get; } = new PeriodSerializer();

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var period = (Period)value;

            this.Serialize(context, period);
        }

        void IBsonSerializer<Period>.Serialize(BsonSerializationContext context, BsonSerializationArgs args, Period value)
        {
            context.Writer.WriteString(
                PeriodPattern.Roundtrip
                .Format(value));
        }

        public Period Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType != BsonType.String)
            {
                throw new InvalidOperationException($"Cannot deserialize Period from BsonType {bsonType}");
            }

            string periodString = context.Reader.ReadString();
            var parseResult = PeriodPattern
                .Roundtrip
                .Parse(periodString);

            if (!parseResult.Success)
            {
                throw parseResult.Exception;
            }

            return parseResult.Value;
        }
    }
}
