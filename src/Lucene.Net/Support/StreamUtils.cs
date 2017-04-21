#if FEATURE_SERIALIZABLE
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Lucene.Net.Support
{
    public static class StreamUtils
    {
        static readonly BinaryFormatter formatter = new BinaryFormatter();

        public static void SerializeToStream(object o, Stream outputStream)
        {
            formatter.Serialize(outputStream, o);
        }

        public static void SerializeToStream(object o, BinaryWriter writer)
        {
            formatter.Serialize(writer.BaseStream, o);
        }

        public static object DeserializeFromStream(Stream stream)
        {
            object o = formatter.Deserialize(stream);
            return o;
        }

        public static object DeserializeFromStream(BinaryReader reader)
        {
            object o = formatter.Deserialize(reader.BaseStream);
            return o;
        }
    }
}
#endif
