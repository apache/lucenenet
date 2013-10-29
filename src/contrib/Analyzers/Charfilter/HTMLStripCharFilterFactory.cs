using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Charfilter
{
    public sealed class HTMLStripCharFilterFactory : CharFilterFactory
    {
        internal readonly ISet<string> _escapedTags;
        static readonly Regex TAG_NAME_PATTERN = new Regex("[^\\s,]+");

        public HTMLStripCharFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            _escapedTags = GetSet(args, "escapedTags");
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override HTMLStripCharFilter Create(TextReader input)
        {
            HTMLStripCharFilter charFilter;
            charFilter = _escapedTags == null ? new HTMLStripCharFilter(input) : new HTMLStripCharFilter(input, _escapedTags);
            return charFilter;
        }
    }
}
