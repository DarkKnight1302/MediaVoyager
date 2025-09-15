using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TMDbLib.Utilities.Converters
{
    /// <summary>
    /// In some cases, TMDb sends a list of integers as an object
    /// </summary>
    public class TmdbIntArrayAsObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // We only support properties that are intended to hold a list of integers.
            // Typical TMDb fields (e.g. genre_ids) are declared as List<int>.
            if (objectType == typeof(List<int>))
                return true;

            if (objectType == typeof(IList<int>))
                return true;

            if (objectType == typeof(IEnumerable<int>))
                return true;

            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Sometimes the genre_ids is an empty object, instead of an array
            // In these instances, convert it from:
            //  "genre_ids": {}
            //  "genre_ids": [ 1 ]
            // To:
            //  "genre_ids": []
            //  "genre_ids": [ 1 ]

            if (reader.TokenType == JsonToken.StartArray)
                return serializer.Deserialize<List<int>>(reader);

            if (reader.TokenType == JsonToken.StartObject)
            {
                reader.Skip();
                return new List<int>();
            }

            if (reader.TokenType == JsonToken.Null)
                return null;

            throw new Exception("Unable to convert list of integers");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Pass-through
            serializer.Serialize(writer, value);
        }
    }
}