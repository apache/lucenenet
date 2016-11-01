namespace Lucene.Net.Support
{
    /// <summary>
    /// Equivalent to Java's DataOutut interface
    /// </summary>
    public interface IDataOutput
    {
        void Write(int b);
        void Write(byte[] b);
        void Write(byte[] b, int off, int len);
        void WriteBoolean(bool v);
        void WriteByte(int v);
        void WriteShort(int v);
        void WriteChar(int v);
        void WriteInt(int v);
        void WriteLong(long v);
        void WriteFloat(float v);
        void WriteDouble(double v);
        void WriteBytes(string s);
        void WriteChars(string s);
        void WriteUTF(string s);
    }
}
