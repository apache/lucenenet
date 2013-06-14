using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Fst
{
    public class PairOutputs<A, B> : Outputs<PairOutputs<A, B>.Pair<A, B>>
        where A : class
        where B : class
    {
        private readonly Pair<A, B> NO_OUTPUT;
        private readonly Outputs<A> _outputs1;
        private readonly Outputs<B> _outputs2; 

        public class Pair<A, B>
            where A : class
            where B : class
        {
            private readonly A _output1;
            public A Output1 { get { return _output1; } }

            private readonly B _output2;
            public B Output2 { get { return _output2; } }

            internal Pair(A output1, B output2)
            {
                _output1 = output1;
                _output2 = output2;
            } 

            public override Boolean Equals(Object other)
            {
                if (other == this) return true;
                if (other is Pair<A, B>)
                {
                    var pair = other as Pair<A, B>;
                    return Output1.Equals(pair.Output1) && Output2.Equals(pair.Output2);
                }
                return false;
            }

            public override Int32 GetHashCode()
            {
                return Output1.GetHashCode() + Output2.GetHashCode();
            }
        }

        public PairOutputs(Outputs<A> outputs1, Outputs<B> outputs2)
        {
            _outputs1 = outputs1;
            _outputs2 = outputs2;
            NO_OUTPUT = new Pair<A, B>(outputs1.GetNoOutput(), outputs2.GetNoOutput());
        }

        public Pair<A, B> NewPair(A a, B b)
        {
            if (a.Equals(_outputs1.GetNoOutput()))
                a = _outputs1.GetNoOutput();
            if (b.Equals(_outputs2.GetNoOutput()))
                b = _outputs2.GetNoOutput();

            if (a == _outputs1.GetNoOutput() && b == _outputs2.GetNoOutput())
                return NO_OUTPUT;

            var p = new Pair<A, B>(a, b);
            // TODO: assert correct here?
            Debug.Assert(Valid(p));
            return p;
        } 

        private Boolean Valid(Pair<A, B> pair)
        {
            var noOutput1 = pair.Output1.Equals(_outputs1.GetNoOutput());
            var noOutput2 = pair.Output2.Equals(_outputs2.GetNoOutput());

            if (noOutput1 && pair.Output1 != _outputs1.GetNoOutput())
                return false;
            if (noOutput2 && pair.Output2 != _outputs2.GetNoOutput())
                return false;
            return true;
        }

        public override Pair<A, B> Add(Pair<A, B> prefix, Pair<A, B> output)
        {
            if (!Valid(prefix)) throw new ArgumentException("prefix is not valid");
            if (!Valid(output)) throw new ArgumentException("output is not valid");
            return NewPair(_outputs1.Add(prefix.Output1, output.Output1),
                           _outputs2.Add(prefix.Output2, output.Output2));
        }

        public override Pair<A, B> Common(Pair<A, B> pair1, Pair<A, B> pair2)
        {
            if (!Valid(pair1)) throw new ArgumentException("pair1 is not valid");
            if (!Valid(pair2)) throw new ArgumentException("pair2 is not valid");
            return NewPair(_outputs1.Common(pair1.Output1, pair2.Output1),
                           _outputs2.Common(pair1.Output2, pair2.Output2));
        }

        public override Pair<A, B> GetNoOutput()
        {
            return NO_OUTPUT;
        }

        public override string OutputToString(Pair<A, B> output)
        {
            if (!Valid(output)) throw new ArgumentException("output");
            return "<pair:" + _outputs1.OutputToString(output.Output1) + ", " + _outputs2.OutputToString(output.Output2) + ">";
        }

        public override Pair<A, B> Read(DataInput dataInput)
        {
            var output1 = _outputs1.Read(dataInput);
            var output2 = _outputs2.Read(dataInput);
            return NewPair(output1, output2);
        }

        public override Pair<A, B> Subtract(Pair<A, B> output, Pair<A, B> inc)
        {
            if (!Valid(output)) throw new ArgumentException("output is not valid");
            if (!Valid(inc)) throw new ArgumentException("inc is not valid");
            return NewPair(_outputs1.Subtract(output.Output1, inc.Output1),
                           _outputs2.Subtract(output.Output2, inc.Output2));
        }

        public override void Write(Pair<A, B> output, DataOutput writer)
        {
            if (!Valid(output)) throw new ArgumentException("output is not valid");
            _outputs1.Write(output.Output1, writer);
            _outputs2.Write(output.Output2, writer);
        }
    }
}
