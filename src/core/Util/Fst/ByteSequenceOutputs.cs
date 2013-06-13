using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
{
    public class ByteSequenceOutputs : Outputs<BytesRef>
    {
        private static readonly BytesRef NO_OUTPUT;
        private static readonly ByteSequenceOutputs _singleton;

        private ByteSequenceOutputs() { } // can't construct

        static ByteSequenceOutputs()
        {
            NO_OUTPUT = new BytesRef();
            _singleton = new ByteSequenceOutputs();
        }

        public static ByteSequenceOutputs GetSingleton()
        {
            return _singleton;
        }

        public override BytesRef Common(BytesRef output1, BytesRef output2)
        {
            if (output1 == null) throw new ArgumentNullException("output1");
            if (output2 == null) throw new ArgumentNullException("output2");

            var pos1 = output1.offset;
            var pos2 = output2.offset;
            var stopAt1 = pos1 + Math.Min(output1.length, output2.length);
            while (pos1 < stopAt1)
            {
                if (output1.bytes[pos1] != output2.bytes[pos2])
                    break;
                pos1++;
                pos2++;
            }

            if (pos1 == output1.offset) return NO_OUTPUT;
            if (pos1 == output1.offset + output1.length) return output1;
            if (pos2 == output2.offset + output2.length) return output2;
            return new BytesRef(output1.bytes, output1.offset, pos1 - output1.offset);
        }

        public override BytesRef Subtract(BytesRef output, BytesRef inc)
        {
            if (output == null) throw new ArgumentNullException("output");
            if (inc == null) throw new ArgumentNullException("inc");

            if (inc == NO_OUTPUT) return output;
            if (inc.length == output.length) return NO_OUTPUT;

            // TODO: is assert correct here?
            Debug.Assert(inc.length < output.length, "inc.length=",
                         inc.length + " vs output.length=" + output.length);
            Debug.Assert(inc.length > 0);
            return new BytesRef(output.bytes, output.offset + inc.length, output.length - inc.length);
        }

        public override BytesRef Add(BytesRef prefix, BytesRef output)
        {
            if (prefix == null) throw new ArgumentNullException("prefix");
            if (output == null) throw new ArgumentNullException("output");

            if (prefix == NO_OUTPUT) return output;
            if (output == NO_OUTPUT) return prefix;

            // TODO: is assert correct here?
            Debug.Assert(prefix.length > 0);
            Debug.Assert(output.length > 0);
            var result = new BytesRef(prefix.length + output.length);
            Array.Copy(prefix.bytes, prefix.offset, result.bytes, 0, prefix.length);
            Array.Copy(output.bytes, output.offset, result.bytes, prefix.length, output.length);
            result.length = prefix.length + output.length;
            return result;
        }

        public override void Write(BytesRef prefix, DataOutput dataOutput)
        {
            if (prefix == null) throw new ArgumentNullException("prefix");
            dataOutput.WriteVInt(prefix.length);
            dataOutput.WriteBytes(prefix.bytes, prefix.offset, prefix.length);
        }

        public override BytesRef Read(DataInput dataInput)
        {
            var len = dataInput.ReadVInt();
            if (len == 0) return NO_OUTPUT;

            var output = new BytesRef(len);
            dataInput.ReadBytes(output.bytes, 0, len);
            output.length = len;
            return output;
        }

        public override BytesRef GetNoOutput()
        {
            return NO_OUTPUT;
        }

        public override String OutputToString(BytesRef output)
        {
            return output.ToString();
        }
    }
}
