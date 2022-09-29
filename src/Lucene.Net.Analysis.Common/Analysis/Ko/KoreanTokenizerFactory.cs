using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Ko
{
    public class KoreanTokenizerFactory : TokenizerFactory, IResourceLoaderAware
    {
        public static readonly string NAME = "korean";

        private static readonly string USER_DICT_PATH = "userDictionary";
        private static readonly string USER_DICT_ENCODING = "userDictionaryEncoding";
        private static readonly string DECOMPOUND_MODE = "decompoundMode";
        private static readonly string OUTPUT_UNKNOWN_UNIGRAMS = "outputUnknownUnigrams";
        private static readonly string DISCARD_PUNCTUATION = "discardPunctuation";

        private readonly string userDictionaryPath;
        private readonly string userDictionaryEncoding;
        private UserDictionary userDictionary;

        private readonly KoreanTokenizer.DecompoundMode mode;
        private readonly bool outputUnknownUnigrams;
        private readonly bool discardPunctuation;

        public KoreanTokenizerFactory(Dictionary<string, string> args)
            : base(args)
        {
            userDictionaryPath = Get(args, USER_DICT_PATH);
            userDictionaryEncoding = Get(args, USER_DICT_ENCODING);
            KoreanTokenizer.DecompoundMode.TryParse(
                Get(args, DECOMPOUND_MODE, KoreanTokenizer.DEFAULT_DECOMPOUND.ToString()), out mode);
            outputUnknownUnigrams = GetBoolean(args, OUTPUT_UNKNOWN_UNIGRAMS, false);
            discardPunctuation = GetBoolean(args, DISCARD_PUNCTUATION, true);

            if (args.Count > 0)
            {
                throw new IllegalArgumentException("Unknown parameters: " + args);
            }
        }

        static KoreanTokenizerFactory()
        {
#if FEATURE_ENCODINGPROVIDERS
            // Support for EUC-JP encoding. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            var encodingProvider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(encodingProvider);
#endif
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (userDictionary != null)
            {
                Stream stream = loader.OpenResource(userDictionaryPath);
                string encoding = userDictionaryEncoding;
                if (encoding is null)
                {
                    encoding = Encoding.UTF8.WebName;
                }
                Encoding decoder = Encoding.GetEncoding(encoding);
                TextReader reader = new StreamReader(stream, decoder);
                userDictionary = new UserDictionary(reader);
            }
            else
            {
                userDictionary = null;
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            return new KoreanTokenizer(factory, input, userDictionary, mode, outputUnknownUnigrams, discardPunctuation);
        }
    }
}