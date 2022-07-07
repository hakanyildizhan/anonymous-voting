using Newtonsoft.Json;

namespace VotingApp.Models.JsonHandling
{
    public class BouncyCastleBigIntegerConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Org.BouncyCastle.Math.BigInteger));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return new Org.BouncyCastle.Math.BigInteger(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString());
        }
    }
}
