using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Fst
{
    public class ForwardBytesReader : FST.BytesReader
    {
        private readonly SByte[] bytes;
        private readonly Int32 pos;

        public ForwardBytesReader(SByte[] bytes)
        {
            this.bytes = bytes;
        }

        public override long GetPosition()
        {
            throw new NotImplementedException();
        }

        public override void SetPosition()
        {
            throw new NotImplementedException();
        }

        public override bool Reversed()
        {
            throw new NotImplementedException();
        }

        public override void SkipBytes(int count)
        {
            throw new NotImplementedException();
        }
    }
}
