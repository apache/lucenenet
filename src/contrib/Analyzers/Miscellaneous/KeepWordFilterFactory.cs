using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    /// <summary>
    /// Factory for KeepWordFilter. 
    /// </summary>
    public class KeepWordFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public bool IgnoreCase { get; set; }
        public bool EnablePositionIncrements { get; set; }
        private readonly String wordFiles;
        public CharArraySet Words { get; set; }

        public KeepWordFilterFactory(IDictionary<string, string> args) : base(args)
        {
            AssureMatchVersion();
            wordFiles = Get(args, "words");
            IgnoreCase = GetBoolean(args, "ignoreCase", false);
            EnablePositionIncrements = GetBoolean(args, "enablePositionIncrements", false);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            // if the set is null, it means it was empty
            return Words == null ? input : new KeepWordFilter(EnablePositionIncrements, input, Words);
        }

        public void Inform(IResourceLoader loader)
        {
            if (wordFiles != null)
            {
                Words = GetWordSet(loader, wordFiles, IgnoreCase);
            }
        }
    }
}
