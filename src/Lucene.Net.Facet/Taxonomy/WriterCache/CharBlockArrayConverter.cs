/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

#if !FEATURE_SERIALIZABLE
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
{
    internal class CharBlockArrayConverter : JsonConverter
    {
        private const string BLOCK_SIZE = "blockSize";
        private const string CONTENTS = "contents";

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
                if (property.Name.Equals(CharBlockArrayConverter.BLOCK_SIZE, StringComparison.OrdinalIgnoreCase))
                {
                    blockSize = property.Value.Value<int>();
                }
                else if (property.Name.Equals(CharBlockArrayConverter.CONTENTS, StringComparison.OrdinalIgnoreCase))
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

            writer.WritePropertyName(BLOCK_SIZE);
            serializer.Serialize(writer, charBlockArray.blockSize);

            writer.WritePropertyName(CONTENTS);
            serializer.Serialize(writer, charBlockArray.ToString());

            writer.WriteEndObject();
        }
    }
}
#endif
