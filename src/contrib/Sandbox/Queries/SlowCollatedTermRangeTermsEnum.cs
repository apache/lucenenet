using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public class SlowCollatedTermRangeTermsEnum : FilteredTermsEnum
    {
        private StringComparer collator;
        private string upperTermText;
        private string lowerTermText;
        private bool includeLower;
        private bool includeUpper;

        public SlowCollatedTermRangeTermsEnum(TermsEnum tenum, string lowerTermText, string upperTermText, bool includeLower, bool includeUpper, StringComparer collator)
            : base(tenum)
        {
            this.collator = collator;
            this.upperTermText = upperTermText;
            this.lowerTermText = lowerTermText;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
            if (this.lowerTermText == null)
            {
                this.lowerTermText = @"";
                this.includeLower = true;
            }

            BytesRef startBytesRef = new BytesRef("");
            InitialSeekTerm = startBytesRef;
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            if ((includeLower ? collator.Compare(term.Utf8ToString(), lowerTermText) >= 0 : collator.Compare(term.Utf8ToString(), lowerTermText) > 0) && (upperTermText == null || (includeUpper ? collator.Compare(term.Utf8ToString(), upperTermText) <= 0 : collator.Compare(term.Utf8ToString(), upperTermText) < 0)))
            {
                return AcceptStatus.YES;
            }

            return AcceptStatus.NO;
        }
    }
}
