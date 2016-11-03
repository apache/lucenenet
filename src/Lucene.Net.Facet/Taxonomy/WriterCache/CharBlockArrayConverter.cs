using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
{
    internal class CharBlockArrayConverter : JsonConverter
    {
        private const string BlockSize = "blockSize";
        private const string Contents = "contents";

        public override bool CanConvert(Type objectType)
        {
            return typeof(CharBlockArray).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObect = JObject.Load(reader);
            var properties = jsonObect.Properties().ToArray();

            int blockSize = -1;
            string contents = null;

            Func<bool> arePropertiesSet = () => blockSize > 0 && !string.IsNullOrEmpty(contents);

            foreach (var property in properties)
            {
                if (property.Name.Equals(BlockSize, StringComparison.OrdinalIgnoreCase))
                {
                    blockSize = property.Value.Value<int>();
                }
                else if (property.Name.Equals(Contents, StringComparison.OrdinalIgnoreCase))
                {
                    contents = property.Value.Value<string>();
                }

                if (arePropertiesSet())
                {
                    break;
                }
            }

            if (!arePropertiesSet())
            {
                return null;
            }

            var deserialized = new CharBlockArray(blockSize);

            deserialized.Append(contents);

            return deserialized;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var charBlockArray = value as CharBlockArray;

            if (charBlockArray == null)
            {
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(BlockSize);
            serializer.Serialize(writer, charBlockArray.blockSize);

            writer.WritePropertyName(Contents);
            serializer.Serialize(writer, charBlockArray.ToString());

            writer.WriteEndObject();
        }
    }
}
