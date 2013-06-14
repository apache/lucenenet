using Lucene.Net.Store;
using System;

namespace Lucene.Net.Util.Fst
{
    public class NoOutputs : Outputs<object>
    {
        private class NoOutputobject
        {
            public override int GetHashCode()
            {
                return 42;
            }

            public override bool Equals(object other)
            {
                return other == this;
            }
        }

        private static readonly object NO_OUTPUT = new NoOutputobject();

        private static readonly NoOutputs singleton;

        static NoOutputs()
        {
            singleton = new NoOutputs();
            NO_OUTPUT = new object(); // TODO: handle anonymous type...
        }

        private NoOutputs() {} // can't construct

        public static NoOutputs GetSingleton()
        {
            return singleton;
        }

        public override object Add(object prefix, object output)
        {
            if (prefix != NO_OUTPUT) throw new ArgumentException("prefix not equal to NO_OUTPUT");
            if (output != NO_OUTPUT) throw new ArgumentException("output not equal to NO_OUTPUT");

            return NO_OUTPUT;
        }

        public override object Common(object output1, object output2)
        {
            if (output1 != NO_OUTPUT) throw new ArgumentException("output1 not equal to NO_OUTPUT");
            if (output2 != NO_OUTPUT) throw new ArgumentException("output2 not equal to NO_OUTPUT");

            return NO_OUTPUT;
        }

        public override object GetNoOutput()
        {
            return NO_OUTPUT;
        }

        public override string OutputToString(object output)
        {
            return string.Empty;
        }

        public override object Read(DataInput dataInput)
        {
            return NO_OUTPUT;
        }

        public override object Subtract(object output, object inc)
        {
            if (output != NO_OUTPUT) throw new ArgumentException("output not equal to NO_object");
            if (inc != NO_OUTPUT) throw new ArgumentException("inc not equal to NO_object");

            return NO_OUTPUT;
        }

        public override void Write(object prefix, DataOutput dataOutput)
        {
            // Empty body
        }
    }
}
