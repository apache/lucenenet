using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public abstract class DocsEnum : DocIdSetIterator
    {
        public const int FLAG_NONE = 0x0;

        public const int FLAG_FREQS = 0x1;

        private AttributeSource atts = null;

        protected DocsEnum()
        {
        }

        public abstract int Freq { get; }

        public virtual AttributeSource Attributes
        {
            get
            {
                if (atts == null) atts = new AttributeSource();
                return atts;
            }
        }
    }
}
