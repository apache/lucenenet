using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public abstract class CharacterUtils
    {
        //private static readonly Java4CharacterUtils JAVA_4 = new Java4CharacterUtils();
        //private static readonly Java5CharacterUtils JAVA_5 = new Java5CharacterUtils();

        // .NET Port: we never changed how we handle strings and chars :-)
        private static readonly DotNetCharacterUtils DOTNET = new DotNetCharacterUtils();

        public static CharacterUtils GetInstance(Lucene.Net.Util.Version? matchVersion)
        {
            //return matchVersion.OnOrAfter(Lucene.Net.Util.Version.LUCENE_31) ? JAVA_5 : JAVA_4;
            return DOTNET;
        }

        public abstract int CodePointAt(char[] chars, int offset);

        public abstract int CodePointAt(ICharSequence seq, int offset);

        public abstract int CodePointAt(char[] chars, int offset, int limit);

        public static CharacterBuffer NewCharacterBuffer(int bufferSize)
        {
            if (bufferSize < 2)
            {
                throw new ArgumentException("buffersize must be >= 2");
            }
            return new CharacterBuffer(new char[bufferSize], 0, 0);
        }

        public virtual void ToLowerCase(char[] buffer, int offset, int limit)
        {
            //assert buffer.length >= limit;
            //assert offset <=0 && offset <= buffer.length;
            for (int i = offset; i < limit; )
            {
                i += Character.ToChars(Character.ToLowerCase(CodePointAt(buffer, i)), buffer, i);
            }
        }

        public abstract bool Fill(CharacterBuffer buffer, TextReader reader);

        // .NET Port: instead of the java-specific types here, we can use .NET's support for UTF-16 strings/chars
        private sealed class DotNetCharacterUtils : CharacterUtils
        {

            public override int CodePointAt(char[] chars, int offset)
            {
                return (int)chars[offset];
            }

            public override int CodePointAt(ICharSequence seq, int offset)
            {
                return (int)seq.CharAt(offset);
            }

            public override int CodePointAt(char[] chars, int offset, int limit)
            {
                return (int)chars[offset];
            }

            public override bool Fill(CharacterBuffer buffer, TextReader reader)
            {
                buffer.offset = 0;
                int read = reader.Read(buffer.buffer, 0, buffer.length);
                if (read == -1)
                    return false;
                buffer.length = read;
                return true;
            }
        }

        public sealed class CharacterBuffer
        {
            internal readonly char[] buffer;
            internal int offset;
            internal int length;
            //// NOTE: not private so outer class can access without
            //// $access methods:
            //char lastTrailingHighSurrogate;

            internal CharacterBuffer(char[] buffer, int offset, int length)
            {
                this.buffer = buffer;
                this.offset = offset;
                this.length = length;
            }

            public char[] Buffer
            {
                get
                {
                    return buffer;
                }
            }

            public int Offset
            {
                get
                {
                    return offset;
                }
            }

            public int Length
            {
                get
                {
                    return length;
                }
            }

            public void Reset()
            {
                offset = 0;
                length = 0;
                //lastTrailingHighSurrogate = 0;
            }
        }
    }
}
