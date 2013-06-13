using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
{
    public class PositiveIntOutputs : Outputs<Int64>
    {
        private static readonly Int64 NO_OUTPUT = 0;

        private static Boolean _doShare;

        private static readonly PositiveIntOutputs _singletonShare;
        private static readonly PositiveIntOutputs _singletonNoShare;

        static PositiveIntOutputs()
        {
            _singletonShare = new PositiveIntOutputs(true);
            _singletonNoShare = new PositiveIntOutputs(false);
        }

        private PositiveIntOutputs(Boolean doShare)
        {
            _doShare = doShare;
        }

        public static PositiveIntOutputs GetSingleton()
        {
            return GetSingleton(true);
        }

        public static PositiveIntOutputs GetSingleton(Boolean doShare)
        {
            return doShare ? _singletonShare : _singletonNoShare;
        }

        public override Int64 Add(Int64 prefix, Int64 output)
        {
            if (!Valid(prefix)) throw new ArgumentException("prefix is not valid");
            if (!Valid(output)) throw new ArgumentException("output is not valid");

            if (prefix == NO_OUTPUT) return output;
            if (output == NO_OUTPUT) return prefix;
            return prefix + output;
        }

        public override Int64 Common(Int64 output1, Int64 output2)
        {
            if (!Valid(output1)) throw new ArgumentException("output1 is not valid");
            if (!Valid(output2)) throw new ArgumentException("output2 is not valid");

            if (output1 == NO_OUTPUT || output2 == NO_OUTPUT) return NO_OUTPUT;
            if (_doShare)
            {
                // TODO: assert correct here?
                Debug.Assert(output1 > 0);
                Debug.Assert(output2 > 0);
                return Math.Min(output1, output2);
            }
            return output1.Equals(output2) ? output1 : NO_OUTPUT;
        }

        public override Int64 GetNoOutput()
        {
            return NO_OUTPUT;
        }

        public override String OutputToString(Int64 output)
        {
            return output.ToString();
        }

        public override Int64 Read(DataInput dataInput)
        {
            var v = dataInput.ReadVLong();
            return v == 0 ? NO_OUTPUT : v;
        }

        public override Int64 Subtract(Int64 output, Int64 inc)
        {
            if (!Valid(output)) throw new ArgumentException("output is not valid");
            if (!Valid(inc)) throw new ArgumentException("inc is not valid");
            if (!(output >= inc)) throw new ArgumentException("output must be greater than or equal to inc");

            if (inc == NO_OUTPUT) return output;
            if (output.Equals(inc)) return NO_OUTPUT;
            return output - inc;
        }

        public override void Write(Int64 output, DataOutput dataOutput)
        {
            if (!Valid(output)) throw new ArgumentException("output is not valid");
            dataOutput.WriteVLong(output);
        }

        public override string ToString()
        {
            return "PositiveIntOutputs(doShare=" + _doShare + ")";
        }

        private Boolean Valid(Int64 o)
        {
            if (!(o == NO_OUTPUT || o > 0)) throw new ArgumentException("o=" + o);
            return true;
        }
    }
}
