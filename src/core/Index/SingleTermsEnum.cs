using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class SingleTermsEnum : FilteredTermsEnum
    {
        private readonly BytesRef singleRef;

        public SingleTermsEnum(TermsEnum tenum, BytesRef termText)
            : base(tenum)
        {
            singleRef = termText;
            InitialSeekTerm = termText;
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            return term.Equals(singleRef) ? AcceptStatus.YES : AcceptStatus.END;
        }
    }
}
