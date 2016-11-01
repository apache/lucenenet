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
        short ReadShort();
        int ReadUnsignedShort();
        char ReadChar();
        int ReadInt();
        long ReadLong();
        float ReadFloat();
        double ReadDouble();
        string ReadLine();
        string ReadUTF();
    }
}
