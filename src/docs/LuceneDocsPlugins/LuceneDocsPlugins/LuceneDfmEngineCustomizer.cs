using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using Microsoft.DocAsCode.MarkdownLite.Matchers;

namespace LuceneDocsPlugins
{
    [Export(typeof(IDfmEngineCustomizer))]
    public class LuceneDfmEngineCustomizer : IDfmEngineCustomizer
    {
        public void Customize(DfmEngineBuilder builder, IReadOnlyDictionary<string, object> parameters)
        {
            var index = builder.BlockRules.FindIndex(r => r is MarkdownHeadingBlockRule);
            builder.BlockRules = builder.BlockRules.Insert(index, new LuceneNoteBlockRule());
        }
    }
}
