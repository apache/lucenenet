using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    [Export(typeof(IDfmCustomizedRendererPartProvider))]
    public class LuceneRendererPartProvider : IDfmCustomizedRendererPartProvider
    {
        public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
        {
            yield return new LuceneTokenRendererPart();
        }
    }

    public sealed class LuceneTokenRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, LuceneNoteBlockToken, MarkdownBlockContext>
    {
        public override string Name => "LabelRendererPart";

        public override bool Match(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) => true;

        public override StringBuffer Render(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) 
            => "<div class=\"" + token.NoteType.ToUpper() + "\">HELLO WORLD</div>";
    }

    public class LuceneNoteBlockToken : DfmNoteBlockToken
    {
        public LuceneNoteBlockToken(IMarkdownRule rule, IMarkdownContext context, string noteType, string content, SourceInfo sourceInfo) 
            : base(rule, context, noteType, content, sourceInfo)
        {
        }
    }

    public class EscapedDotMatcher : Matcher
    {
        private readonly Matcher _stringMatcher = String("\\.");
        public override int Match(MatchContent content)
        {
            return _stringMatcher.Match(content);
        }

        /// <summary>
        /// Override since the base string matcher will escape this and we don't want that!
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "\\.";
        }
    }

    public class LuceneNoteBlockRule : DfmNoteBlockRule
    {
        private static readonly Matcher NoteMatcherInternal =
            Matcher.String("@lucene") + Matcher.Char('.') + 
            (
                Matcher.String("experimental") |
                Matcher.String("internal")
            ).ToGroup("notetype") + 
            Matcher.EndOfString;

        public override Matcher NoteMatcher => NoteMatcherInternal;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {            
            //original source:
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)parser.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }

            var match = context.Match(NoteMatcher);
            
            Logger.LogInfo("LuceneNoteBlockRule :: " + NoteMatcher);

            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new DfmNoteBlockToken(this, parser.Context, match["notetype"].GetValue(), sourceInfo.Markdown, sourceInfo);
            }
            return null;
        }

        public override string Name => "LuceneNote";
    }
}
