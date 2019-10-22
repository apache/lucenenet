//using Lucene.Net.Analysis;
//using Lucene.Net.Analysis.Util;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Lucene.Net.Analysis.OpenNlp
//{
//    /// <summary>
//    /// Needs the OpenNLP Tokenizer because it creates full streams of punctuation.
//    /// Needs the OpenNLP POS tagger for the POS tags.
//    /// <para/>
//    /// Tagging models are created from tiny test data in opennlp/tools/test-model-data/ and are not very accurate.
//    /// </summary>
//    public class TestOpenNLPChunkerFilterFactory : BaseTokenStreamTestCase
//    {
//        private const String SENTENCES = "Sentence number 1 has 6 words. Sentence number 2, 5 words.";
//        private static readonly String[] SENTENCES_punc
//            = {"Sentence", "number", "1", "has", "6", "words", ".", "Sentence", "number", "2", ",", "5", "words", "."};
//        private static readonly int[] SENTENCES_startOffsets = { 0, 9, 16, 18, 22, 24, 29, 31, 40, 47, 48, 50, 52, 57 };
//        private static readonly int[] SENTENCES_endOffsets = { 8, 15, 17, 21, 23, 29, 30, 39, 46, 48, 49, 51, 57, 58 };
//        private static readonly String[] SENTENCES_chunks
//            = { "B-NP", "I-NP", "I-NP", "B-VP", "B-NP", "I-NP", "O", "B-NP", "I-NP", "I-NP", "O", "B-NP", "I-NP", "O" };

//  private const String sentenceModelFile = "en-test-sent.bin";
//  private const String tokenizerModelFile = "en-test-tokenizer.bin";
//  private const String posTaggerModelFile = "en-test-pos-maxent.bin";
//  private const String chunkerModelFile = "en-test-chunker.bin";


//  //private static byte[][] toPayloads(params string[] strings)
//  //      {
//  //          //return Arrays.stream(strings).map(s->s == null ? null : s.getBytes(StandardCharsets.UTF_8)).toArray(byte[][]::new);
//  //          return strings.SelectMany(s => s == null ? null : Encoding.UTF8.GetBytes(s)).ToArray();
//  //      }

//        [Test]
//        public void TestBasic() 
//        {
//            //    //    CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
//            //    //.withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
//            //    //.addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
//            //    //.addTokenFilter("opennlpChunker", "chunkerModel", chunkerModelFile)
//            //    //.build();

//            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
//            {
//                var loader = new ClasspathResourceLoader(GetType());

//                var opennlpFactory = new OpenNLPTokenizerFactory(new Dictionary<string, string> { { "tokenizerModel", tokenizerModelFile }, { "sentenceModel", sentenceModelFile } });
//                opennlpFactory.Inform(loader);
//                var opennlp = opennlpFactory.Create(AttributeFactory.Def //new OpenNLPTokenizer(reader, new Tools.NLPSentenceDetectorOp(sentenceModelFile), new Tools.NLPTokenizerOp(tokenizerModelFile));
//                var filter1 = new OpenNLPPOSFilter(opennlp, new Tools.NLPPOSTaggerOp(posTaggerModelFile));
//                var filter2 = new OpenNLPChunkerFilter(filter1, new Tools.NLPChunkerOp(chunkerModelFile));
//                return new TokenStreamComponents(opennlp, filter2);
//            });

//            AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_punc, SENTENCES_startOffsets, SENTENCES_endOffsets,
//                SENTENCES_chunks, null, null, true);
//        }

//        [Test]
//        public void TestPayloads() 
//    {
//        //    //CustomAnalyzer analyzer = CustomAnalyzer.builder(new ClasspathResourceLoader(getClass()))
//        //    //.withTokenizer("opennlp", "tokenizerModel", tokenizerModelFile, "sentenceModel", sentenceModelFile)
//        //    //.addTokenFilter("opennlpPOS", "posTaggerModel", posTaggerModelFile)
//        //    //.addTokenFilter("opennlpChunker", "chunkerModel", chunkerModelFile)
//        //    //.addTokenFilter(TypeAsPayloadTokenFilterFactory.class)
//        //    //.build();

//        //    Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
//        //    {
//        //        var opennlp = new OpenNLPTokenizer(reader, new Tools.NLPSentenceDetectorOp(sentenceModelFile), new Tools.NLPTokenizerOp(tokenizerModelFile));
//        //        var filter1 = new OpenNLPPOSFilter(opennlp, new Tools.NLPPOSTaggerOp(posTaggerModelFile));
//        //        var filter2 = new OpenNLPChunkerFilter(filter1, new Tools.NLPChunkerOp(chunkerModelFile));
//        //        var filter3 = new Payloads.TypeAsPayloadTokenFilter(filter2);
//        //        return new TokenStreamComponents(opennlp, filter3);
//        //    });
//        //    AssertAnalyzesTo(analyzer, SENTENCES, SENTENCES_punc, SENTENCES_startOffsets, SENTENCES_endOffsets,
//        //null, null, null, true, toPayloads(SENTENCES_chunks));
//}
//    }
//}
