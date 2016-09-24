using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Lucene.Net.Support
{
    public static class StreamUtils
    {
        static readonly BinaryFormatter Formatter = new BinaryFormatter();

        public static void SerializeToStream(object o, Stream outputStream)
        {
            Formatter.Serialize(outputStream, o);
        }

        public static void SerializeToStream(object o, BinaryWriter writer)
        {
            Formatter.Serialize(writer.BaseStream, o);
        }

        public static object DeserializeFromStream(Stream stream)
        {
            object o = Formatter.Deserialize(stream);
            return o;
        }

        public static object DeserializeFromStream(BinaryReader reader)
        {
            object o = Formatter.Deserialize(reader.BaseStream);
            return o;
        }
    }
}
