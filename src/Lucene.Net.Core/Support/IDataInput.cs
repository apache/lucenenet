namespace Lucene.Net.Support
{
    /// <summary>
    /// Equivalent to Java's DataInput interface
    /// </summary>
    public interface IDataInput
    {
        void ReadFully(byte[] b);
        void ReadFully(byte[] b, int off, int len);
        int SkipBytes(int n);
        bool ReadBoolean();
        byte ReadByte();
        int ReadUnsignedByte();

        /// <summary>
        /// NOTE: This was readShort() in the JDK
        /// </summary>
        short ReadInt16();

        /// <summary>
        /// NOTE: This was readUnsignedShort() in the JDK
        /// </summary>
        int ReadUInt16();
        char ReadChar();

        /// <summary>
        /// NOTE: This was readInt() in the JDK
        /// </summary>
        int ReadInt32();

        /// <summary>
        /// NOTE: This was readLong() in the JDK
        /// </summary>
        long ReadInt64();
        float ReadSingle();
        double ReadDouble();
        string ReadLine();
        string ReadUTF();
    }
}
