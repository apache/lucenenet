using System;

namespace Lucene.Net.Util.Fst
{
    public class NoOutputs : Outputs<Object>
    {
        private class NoOutputObject
        {
            public override Int32 GetHashCode()
            {
                return 42;
            }

            public override Boolean Equals(Object other)
            {
                return other == this;
            }
        }

        private static readonly Object NO_OUTPUT = new NoOutputObject();

        private static readonly NoOutputs _singleton;

        static NoOutputs()
        {
            _singleton = new NoOutputs();
            NO_OUTPUT = new Object(); // TODO: handle anonymous type...
        }

        private NoOutputs() {} // can't construct

        public static NoOutputs GetSingleton()
        {
            return _singleton;
        }

        public override Object Add(Object prefix, Object output)
        {
            if (prefix != NO_OUTPUT) throw new ArgumentException("prefix not equal to NO_OUTPUT");
            if (output != NO_OUTPUT) throw new ArgumentException("output not equal to NO_OUTPUT");

            return NO_OUTPUT;
        }

        public override Object Common(Object output1, Object output2)
        {
            if (output1 != NO_OUTPUT) throw new ArgumentException("output1 not equal to NO_OUTPUT");
            if (output2 != NO_OUTPUT) throw new ArgumentException("output2 not equal to NO_OUTPUT");

            return NO_OUTPUT;
        }

        public override Object GetNoOutput()
        {
            return NO_OUTPUT;
        }

        public override String OutputToString(Object output)
        {
            return string.Empty;
        }

        public override Object Read(DataInput dataInput)
        {
            return NO_OUTPUT;
        }

        public override Object Subtract(Object output, Object inc)
        {
            if (output != NO_OUTPUT) throw new ArgumentException("output not equal to NO_OBJECT");
            if (inc != NO_OUTPUT) throw new ArgumentException("inc not equal to NO_OBJECT");

            return NO_OUTPUT;
        }

        public override void Write(Object prefix, DataOutput dataOutput)
        {
            // Empty body
        }
    }
}
