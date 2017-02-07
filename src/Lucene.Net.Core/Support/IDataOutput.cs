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

        /// <summary>
        /// NOTE: This was writeShort() in the JDK
        /// </summary>
        void WriteInt16(int v);
        void WriteChar(int v);

        /// <summary>
        /// NOTE: This was writeInt() in the JDK
        /// </summary>
        void WriteInt32(int v);

        /// <summary>
        /// NOTE: This was writeInt64() in the JDK
        /// </summary>
        void WriteInt64(long v);

        /// <summary>
        /// NOTE: This was writeSingle() in the JDK
        /// </summary>
        void WriteSingle(float v);
        void WriteDouble(double v);
        void WriteBytes(string s);
        void WriteChars(string s);
        void WriteUTF(string s);
    }
}
