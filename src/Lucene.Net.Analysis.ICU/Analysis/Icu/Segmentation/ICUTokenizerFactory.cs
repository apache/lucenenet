// LUCENENET TODO: Port issues - missing dependencies

//using Icu;
//using Lucene.Net.Analysis.Util;
//using Lucene.Net.Support;
//using Lucene.Net.Util;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.Segmentation
//{

//    public class ICUTokenizerFactory : TokenizerFactory, IResourceLoaderAware
//    {
//        internal static readonly string RULEFILES = "rulefiles";
//        private readonly IDictionary<int, string> tailored;
//        private ICUTokenizerConfig config;
//        private readonly bool cjkAsWords;

//        /// <summary>Creates a new ICUTokenizerFactory</summary>
//        public ICUTokenizerFactory(IDictionary<string, string> args)
//            : base(args)
//        {
//            tailored = new Dictionary<int, string>();
//            string rulefilesArg = Get(args, RULEFILES);
//            if (rulefilesArg != null)
//            {
//                IList<string> scriptAndResourcePaths = SplitFileNames(rulefilesArg);
//                foreach (string scriptAndResourcePath in scriptAndResourcePaths)
//                {
//                    int colonPos = scriptAndResourcePath.IndexOf(":");
//                    string scriptCode = scriptAndResourcePath.Substring(0, colonPos - 0).Trim();
//                    string resourcePath = scriptAndResourcePath.Substring(colonPos + 1).Trim();
//                    tailored[UCharacter.getPropertyValueEnum(UProperty.SCRIPT, scriptCode)] = resourcePath;
//                }
//            }
//            cjkAsWords = GetBoolean(args, "cjkAsWords", true);
//            if (args.Count != 0)
//            {
//                throw new ArgumentException("Unknown parameters: " + args);
//            }
//        }

//        public virtual void Inform(IResourceLoader loader)
//        {
//            Debug.Assert(tailored != null, "init must be called first!");
//            if (tailored.Count == 0)
//            {
//                config = new DefaultICUTokenizerConfig(cjkAsWords);
//            }
//            else
//            {
//                config = new DefaultICUTokenizerConfigAnonymousHelper(cjkAsWords, tailored, loader);

//                //BreakIterator[] breakers = new BreakIterator[UScript.CODE_LIMIT];
//                //foreach (var entry in tailored)
//                //{
//                //    int code = entry.Key;
//                //    string resourcePath = entry.Value;
//                //    breakers[code] = ParseRules(resourcePath, loader);
//                //}
//                //            config = new DefaultICUTokenizerConfig(cjkAsWords)
//                //            {

//                //    public override BreakIterator GetBreakIterator(int script)
//                //    {
//                //        if (breakers[script] != null)
//                //        {
//                //            return (BreakIterator)breakers[script].clone();
//                //        }
//                //        else
//                //        {
//                //            return base.GetBreakIterator(script);
//                //        }
//                //    }
//                //    // TODO: we could also allow codes->types mapping
//                //};
//            }
//        }

//        private class DefaultICUTokenizerConfigAnonymousHelper : DefaultICUTokenizerConfig
//        {
//            private readonly Icu.BreakIterator[] breakers;
//            public DefaultICUTokenizerConfigAnonymousHelper(bool cjkAsWords, IDictionary<int, string> tailored, IResourceLoader loader)
//                : base(cjkAsWords)
//            {
//                breakers = new Icu.BreakIterator[UScript.CODE_LIMIT];
//                foreach (var entry in tailored)
//                {
//                    int code = entry.Key;
//                    string resourcePath = entry.Value;
//                    breakers[code] = ParseRules(resourcePath, loader);
//                }
//            }

//            public override Icu.BreakIterator GetBreakIterator(int script)
//            {
//                if (breakers[script] != null)
//                {
//                    return (Icu.BreakIterator)breakers[script].Clone();
//                }
//                else
//                {
//                    return base.GetBreakIterator(script);
//                }
//            }

//            private Icu.BreakIterator ParseRules(string filename, IResourceLoader loader)
//            {
//                StringBuilder rules = new StringBuilder();
//                Stream rulesStream = loader.OpenResource(filename);
//                using (TextReader reader = IOUtils.GetDecodingReader(rulesStream, Encoding.UTF8))
//                {
//                    string line = null;
//                    while ((line = reader.ReadLine()) != null)
//                    {
//                        if (!line.StartsWith("#", StringComparison.Ordinal))
//                        {
//                            rules.Append(line);
//                        }
//                        rules.Append('\n');
//                    }
//                }
//                return new RuleBasedBreakIterator(rules.ToString());
//            }
//        }

//        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
//        {
//            Debug.Assert(config != null, "inform must be called first!");
//            return new ICUTokenizer(factory, input, config);
//        }
//    }
//}
