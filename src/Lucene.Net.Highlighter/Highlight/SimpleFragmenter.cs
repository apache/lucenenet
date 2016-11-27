using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Search.Highlight
{
    public class SimpleFragmenter : IFragmenter
    {
        private static readonly int DEFAULT_FRAGMENT_SIZE = 100;
        private int currentNumFrags;
        public int FragmentSize { get; set; }
        private IOffsetAttribute offsetAtt;

        public SimpleFragmenter() : this(DEFAULT_FRAGMENT_SIZE) { }

        public SimpleFragmenter(int fragmentSize)
        {
            FragmentSize = fragmentSize;
        }

        public void Start(string originalText, TokenStream stream)
        {
            offsetAtt = stream.AddAttribute<IOffsetAttribute>();
            currentNumFrags = 1;
        }

        public bool IsNewFragment()
        {
            bool isNewFrag = offsetAtt.EndOffset() >= (FragmentSize*currentNumFrags);
            if (isNewFrag)
            {
                currentNumFrags++;
            }
            return isNewFrag;
        }
    }
}
