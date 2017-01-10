using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.CommonGrams;
using Lucene.Net.Analysis.Compound;
using Lucene.Net.Analysis.Compound.Hyphenation;
using Lucene.Net.Analysis.Hunspell;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Ngram;
using Lucene.Net.Analysis.Path;
using Lucene.Net.Analysis.Payloads;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Wikipedia;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Tartarus.Snowball;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Core
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    /// <summary>
    /// tests random analysis chains </summary>
    public class TestRandomChains : BaseTokenStreamTestCase
    {

        internal static List<ConstructorInfo> tokenizers;
        internal static List<ConstructorInfo> tokenfilters;
        internal static List<ConstructorInfo> charfilters;

        private interface IPredicate<T>
        {
            bool Apply(T o);
        }

        private static readonly IPredicate<object[]> ALWAYS = new PredicateAnonymousInnerClassHelper();

        private class PredicateAnonymousInnerClassHelper : IPredicate<object[]>
        {
            public PredicateAnonymousInnerClassHelper()
            {
            }

            public virtual bool Apply(object[] args)
            {
                return true;
            }
        }

        private static readonly IDictionary<ConstructorInfo, IPredicate<object[]>> brokenConstructors = new Dictionary<ConstructorInfo, IPredicate<object[]>>();
        // TODO: also fix these and remove (maybe):
        // Classes/options that don't produce consistent graph offsets:
        private static readonly IDictionary<ConstructorInfo, IPredicate<object[]>> brokenOffsetsConstructors = new Dictionary<ConstructorInfo, IPredicate<object[]>>();

        internal static readonly ISet<Type> allowedTokenizerArgs, allowedTokenFilterArgs, allowedCharFilterArgs;
        static TestRandomChains()
        {
            try
            {
                brokenConstructors[typeof(LimitTokenCountFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int) })] = ALWAYS;
                brokenConstructors[typeof(LimitTokenCountFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int), typeof(bool) })] = new PredicateAnonymousInnerClassHelper2();
                brokenConstructors[typeof(LimitTokenPositionFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int) })] = ALWAYS;
                brokenConstructors[typeof(LimitTokenPositionFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int), typeof(bool) })] = new PredicateAnonymousInnerClassHelper3();
                foreach (Type c in Arrays.AsList(
                    // TODO: can we promote some of these to be only
                    // offsets offenders?
                    // doesn't actual reset itself:
                    typeof(CachingTokenFilter),
                    // Not broken: we forcefully add this, so we shouldn't
                    // also randomly pick it:
                    typeof(ValidatingTokenFilter)))
                {
                    foreach (ConstructorInfo ctor in c.GetConstructors())
                    {
                        brokenConstructors[ctor] = ALWAYS;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
            try
            {
                foreach (Type c in Arrays.AsList(
                    typeof(ReversePathHierarchyTokenizer),
                    typeof(PathHierarchyTokenizer),
                    // TODO: it seems to mess up offsets!?
                    typeof(WikipediaTokenizer),
                    // TODO: doesn't handle graph inputs
                    typeof(CJKBigramFilter),
                    // TODO: doesn't handle graph inputs (or even look at positionIncrement)
                    typeof(HyphenatedWordsFilter),
                    // TODO: LUCENE-4983
                    typeof(CommonGramsFilter),
                    // TODO: doesn't handle graph inputs
                    typeof(CommonGramsQueryFilter),
                    // TODO: probably doesnt handle graph inputs, too afraid to try
                    typeof(WordDelimiterFilter)))
                {
                    foreach (ConstructorInfo ctor in c.GetConstructors())
                    {
                        brokenOffsetsConstructors[ctor] = ALWAYS;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            allowedTokenizerArgs = new HashSet<Type>(); // Collections.NewSetFromMap(new IdentityHashMap<Type, bool?>());
            allowedTokenizerArgs.addAll(argProducers.Keys);
            allowedTokenizerArgs.Add(typeof(TextReader));
            allowedTokenizerArgs.Add(typeof(AttributeSource.AttributeFactory));
            allowedTokenizerArgs.Add(typeof(AttributeSource));

            allowedTokenFilterArgs = new HashSet<Type>();  //Collections.newSetFromMap(new IdentityHashMap<Type, bool?>());
            allowedTokenFilterArgs.addAll(argProducers.Keys);
            allowedTokenFilterArgs.Add(typeof(TokenStream));
            // TODO: fix this one, thats broken:
            allowedTokenFilterArgs.Add(typeof(CommonGramsFilter));

            allowedCharFilterArgs = new HashSet<Type>(); //Collections.newSetFromMap(new IdentityHashMap<Type, bool?>());
            allowedCharFilterArgs.addAll(argProducers.Keys);
            allowedCharFilterArgs.Add(typeof(TextReader));
        }

        private class PredicateAnonymousInnerClassHelper2 : IPredicate<object[]>
        {
            public PredicateAnonymousInnerClassHelper2()
            {
            }

            public virtual bool Apply(object[] args)
            {
                Debug.Assert(args.Length == 3);
                return !((bool)args[2]); // args are broken if consumeAllTokens is false
            }
        }

        private class PredicateAnonymousInnerClassHelper3 : IPredicate<object[]>
        {
            public PredicateAnonymousInnerClassHelper3()
            {
            }

            public virtual bool Apply(object[] args)
            {
                Debug.Assert(args.Length == 3);
                return !((bool)args[2]); // args are broken if consumeAllTokens is false
            }
        }

        [TestFixtureSetUp]
        public static void BeforeClass()
        {
            IEnumerable<Type> analysisClasses = typeof(StandardAnalyzer).GetTypeInfo().Assembly.GetTypes()
                .Where(c => {
                    var typeInfo = c.GetTypeInfo();

                    return !typeInfo.IsAbstract && typeInfo.IsPublic && !typeInfo.IsInterface 
                        && typeInfo.IsClass && (typeInfo.GetCustomAttribute<ObsoleteAttribute>() == null)
                        && (typeInfo.IsSubclassOf(typeof(Tokenizer)) || typeInfo.IsSubclassOf(typeof(TokenFilter)) || typeInfo.IsSubclassOf(typeof(CharFilter)));
                })
                .ToArray();
            tokenizers = new List<ConstructorInfo>();
            tokenfilters = new List<ConstructorInfo>();
            charfilters = new List<ConstructorInfo>();
            foreach (Type c in analysisClasses)
            {
                foreach (ConstructorInfo ctor in c.GetConstructors())
                {
                    if (ctor.GetCustomAttribute<ObsoleteAttribute>() != null || (brokenConstructors.ContainsKey(ctor) && brokenConstructors[ctor] == ALWAYS))
                    {
                        continue;
                    }

                    var typeInfo = c.GetTypeInfo();

                    if (typeInfo.IsSubclassOf(typeof(Tokenizer)))
                    {
                        assertTrue(ctor.ToString() + " has unsupported parameter types", 
                            allowedTokenizerArgs.containsAll(Arrays.AsList(ctor.GetParameters().Select(p => p.ParameterType).ToArray())));
                        tokenizers.Add(ctor);
                    }
                    else if (typeInfo.IsSubclassOf(typeof(TokenFilter)))
                    {
                        assertTrue(ctor.ToString() + " has unsupported parameter types", 
                            allowedTokenFilterArgs.containsAll(Arrays.AsList(ctor.GetParameters().Select(p => p.ParameterType).ToArray())));
                        tokenfilters.Add(ctor);
                    }
                    else if (typeInfo.IsSubclassOf(typeof(CharFilter)))
                    {
                        assertTrue(ctor.ToString() + " has unsupported parameter types", 
                            allowedCharFilterArgs.containsAll(Arrays.AsList(ctor.GetParameters().Select(p => p.ParameterType).ToArray())));
                        charfilters.Add(ctor);
                    }
                    else
                    {
                        fail("Cannot get here");
                    }
                }
            }

            IComparer<ConstructorInfo> ctorComp = new ComparatorAnonymousInnerClassHelper();
            tokenizers.Sort(ctorComp);
            tokenfilters.Sort(ctorComp);
            charfilters.Sort(ctorComp);
            if (VERBOSE)
            {
                Console.WriteLine("tokenizers = " + tokenizers);
                Console.WriteLine("tokenfilters = " + tokenfilters);
                Console.WriteLine("charfilters = " + charfilters);
            }
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<ConstructorInfo>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(ConstructorInfo arg0, ConstructorInfo arg1)
            {
                // LUCENENET TODO: Need to ensure we have the right sort order
                // original: arg0.toGenericString().compareTo(arg1.toGenericString());
                return arg0.ToString().CompareTo(arg1.ToString());
            }
        }

        [TestFixtureTearDown]
        public static void AfterClass()
        {
            tokenizers = null;
            tokenfilters = null;
            charfilters = null;
        }


        private interface IArgProducer
        {
            object Create(Random random);
        }

        private static readonly IDictionary<Type, IArgProducer> argProducers = new IdentityHashMap<Type, IArgProducer>()
        {
            { typeof(int), new IntArgProducer() },
            { typeof(char), new CharArgProducer() },
            { typeof(float), new FloatArgProducer() },
            { typeof(bool), new BooleanArgProducer() },
            { typeof(byte), new ByteArgProducer() },
            { typeof(byte[]), new ByteArrayArgProducer() },
            { typeof(sbyte[]), new SByteArrayArgProducer() },
            { typeof(Random), new RandomArgProducer() },
            { typeof(LuceneVersion), new VersionArgProducer() },
            { typeof(IEnumerable<string>), new StringEnumerableArgProducer() },
            { typeof(ICollection<char[]>), new CharArrayCollectionArgProducer() },// CapitalizationFilter
            { typeof(CharArraySet), new CharArraySetArgProducer() },
            { typeof(Regex), new RegexArgProducer() },
            { typeof(Regex[]), new RegexArrayArgProducer() },
            { typeof(IPayloadEncoder), new PayloadEncoderArgProducer() },
            { typeof(Dictionary), new DictionaryArgProducer() },
            #pragma warning disable 612, 618 
            { typeof(Lucene43EdgeNGramTokenizer.Side), new Lucene43SideArgProducer() },
            #pragma warning restore 612, 618 
            { typeof(EdgeNGramTokenFilter.Side), new SideArgProducer() },
            { typeof(HyphenationTree), new HyphenationTreeArgProducer() },
            { typeof(SnowballProgram), new SnowballProgramArgProducer() },
            { typeof(string), new StringArgProducer() },
            { typeof(NormalizeCharMap), new NormalizeCharMapArgProducer() },
            { typeof(CharacterRunAutomaton), new CharacterRunAutomatonArgProducer() },
            { typeof(CharArrayMap<string>), new StringCharArrayMapArgProducer() },
            { typeof(StemmerOverrideFilter.StemmerOverrideMap), new StemmerOverrideMapArgProducer() },
            { typeof(SynonymMap), new SynonymMapArgProducer() },
        };

        private class IntArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: could cause huge ram usage to use full int range for some filters
                // (e.g. allocate enormous arrays)
                // return Integer.valueOf(random.nextInt());
                return TestUtil.NextInt(random, -100, 100);
            }
        }

        private class CharArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: fix any filters that care to throw IAE instead.
                // also add a unicode validating filter to validate termAtt?
                // return Character.valueOf((char)random.nextInt(65536));
                while (true)
                {
                    char c = (char)random.nextInt(65536);
                    if (c < '\uD800' || c > '\uDFFF')
                    {
                        return c;
                    }
                }
            }
        }

        private class FloatArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return (float)random.NextDouble();
            }
        }

        private class BooleanArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return random.nextBoolean();
            }
        }

        private class ByteArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // this wraps to negative when casting to byte
                return (byte)random.nextInt(256);
            }
        }

        private class ByteArrayArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                byte[] bytes = new byte[random.nextInt(256)];
                random.NextBytes(bytes);
                return bytes;
            }
        }

        private class SByteArrayArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                byte[] bytes = new byte[random.nextInt(256)];
                random.NextBytes(bytes);
                return (sbyte[])(Array)bytes;
            }
        }

        private class RandomArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return new Random(random.Next());
            }
        }

        private class VersionArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // we expect bugs in emulating old versions
                return TEST_VERSION_CURRENT;
            }
        }

        private class StringEnumerableArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TypeTokenFilter
                ISet<string> set = new HashSet<string>();
                int num = random.nextInt(5);
                for (int i = 0; i < num; i++)
                {
                    set.Add(StandardTokenizer.TOKEN_TYPES[random.nextInt(StandardTokenizer.TOKEN_TYPES.Length)]);
                }
                return set;
            }
        }
        private class CharArrayCollectionArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // CapitalizationFilter
                ICollection<char[]> col = new List<char[]>();
                int num = random.nextInt(5);
                for (int i = 0; i < num; i++)
                {
                    col.Add(TestUtil.RandomSimpleString(random).toCharArray());
                }
                return col;
            }
        }

        private class CharArraySetArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                int num = random.nextInt(10);
                CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, num, random.nextBoolean());
                for (int i = 0; i < num; i++)
                {
                    // TODO: make nastier
                    set.add(TestUtil.RandomSimpleString(random));
                }
                return set;
            }
        }

        private class RegexArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: don't want to make the exponentially slow ones Dawid documents
                // in TestPatternReplaceFilter, so dont use truly random patterns (for now)
                return new Regex("a", RegexOptions.Compiled);
            }
        }

        private class RegexArrayArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return new Regex[] { new Regex("([a-z]+)", RegexOptions.Compiled), new Regex("([0-9]+)", RegexOptions.Compiled) };
            }
        }

        private class PayloadEncoderArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return new IdentityEncoder(); // the other encoders will throw exceptions if tokens arent numbers?
            }
        }

        private class DictionaryArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: make nastier
                using (Stream affixStream = typeof(TestHunspellStemFilter).getResourceAsStream("simple.aff"))
                {
                    using (Stream dictStream = typeof(TestHunspellStemFilter).getResourceAsStream("simple.dic"))
                    {
                        try
                        {
                            return new Dictionary(affixStream, dictStream);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
#pragma warning disable 162
                            return null; // unreachable code
#pragma warning restore 162
                        }
                    }
                }
            }
        }

        private class Lucene43SideArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return random.nextBoolean()
#pragma warning disable 612, 618
                    ? Lucene43EdgeNGramTokenizer.Side.FRONT
                    : Lucene43EdgeNGramTokenizer.Side.BACK;
#pragma warning restore 612, 618
            }
        }

        private class SideArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                return random.nextBoolean()
                    ? EdgeNGramTokenFilter.Side.FRONT
#pragma warning disable 612, 618
                    : EdgeNGramTokenFilter.Side.BACK;
#pragma warning restore 612, 618
            }
        }

        private class HyphenationTreeArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: make nastier
                try
                {
                    using (Stream @is = typeof(TestCompoundWordTokenFilter).getResourceAsStream("da_UTF8.xml"))
                    {
                        HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);
                        return hyphenator;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
                }
            }
        }

        private class SnowballProgramArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                try
                {
                    string lang = TestSnowball.SNOWBALL_LANGS[random.nextInt(TestSnowball.SNOWBALL_LANGS.Length)];
                    Type clazz = Type.GetType("Lucene.Net.Tartarus.Snowball.Ext." + lang + "Stemmer, Lucene.Net.Analysis.Common");
                    return clazz.GetConstructor(new Type[0]).Invoke(new object[0]);
                }
                catch (Exception ex)
                {
                    throw ex;
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
                }
            }
        }

        private class StringArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: make nastier
                if (random.nextBoolean())
                {
                    // a token type
                    return StandardTokenizer.TOKEN_TYPES[random.nextInt(StandardTokenizer.TOKEN_TYPES.Length)];
                }
                else
                {
                    return TestUtil.RandomSimpleString(random);
                }
            }
        }

        private class NormalizeCharMapArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
                // we can't add duplicate keys, or NormalizeCharMap gets angry
                ISet<string> keys = new HashSet<string>();
                int num = random.nextInt(5);
                //System.out.println("NormalizeCharMap=");
                for (int i = 0; i < num; i++)
                {
                    string key = TestUtil.RandomSimpleString(random);
                    if (!keys.contains(key) && key.Length > 0)
                    {
                        string value = TestUtil.RandomSimpleString(random);
                        builder.Add(key, value);
                        keys.add(key);
                        //System.out.println("mapping: '" + key + "' => '" + value + "'");
                    }
                }
                return builder.Build();
            }
        }

        private class CharacterRunAutomatonArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: could probably use a purely random automaton
                switch (random.nextInt(5))
                {
                    case 0: return MockTokenizer.KEYWORD;
                    case 1: return MockTokenizer.SIMPLE;
                    case 2: return MockTokenizer.WHITESPACE;
                    case 3: return MockTokenFilter.EMPTY_STOPSET;
                    default: return MockTokenFilter.ENGLISH_STOPSET;
                }
            }
        }

        private class StringCharArrayMapArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                int num = random.nextInt(10);
                CharArrayMap<string> map = new CharArrayMap<string>(TEST_VERSION_CURRENT, num, random.nextBoolean());
                for (int i = 0; i < num; i++)
                {
                    // TODO: make nastier
                    map.Put(TestUtil.RandomSimpleString(random), TestUtil.RandomSimpleString(random));
                }
                return map;
            }
        }

        private class StemmerOverrideMapArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                int num = random.nextInt(10);
                StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(random.nextBoolean());
                for (int i = 0; i < num; i++)
                {
                    string input = "";
                    do
                    {
                        input = TestUtil.RandomRealisticUnicodeString(random);
                    } while (input == string.Empty);
                    string @out = ""; TestUtil.RandomSimpleString(random);
                    do
                    {
                        @out = TestUtil.RandomRealisticUnicodeString(random);
                    } while (@out == string.Empty);
                    builder.Add(input, @out);
                }
                try
                {
                    return builder.Build();
                }
                catch (Exception ex)
                {
                    throw ex;
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
                }
            }
        }

        private class SynonymMapArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                SynonymMap.Builder b = new SynonymMap.Builder(random.nextBoolean());
                int numEntries = AtLeast(10);
                for (int j = 0; j < numEntries; j++)
                {
                    AddSyn(b, RandomNonEmptyString(random), RandomNonEmptyString(random), random.nextBoolean());
                }
                try
                {
                    return b.Build();
                }
                catch (Exception ex)
                {
                    throw ex;
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
                }
            }

            private void AddSyn(SynonymMap.Builder b, string input, string output, bool keepOrig)
            {
                b.Add(new CharsRef(input.Replace(" +", "\u0000")),
                      new CharsRef(output.Replace(" +", "\u0000")),
                      keepOrig);
            }

            private string RandomNonEmptyString(Random random)
            {
                while (true)
                {
                    string s = TestUtil.RandomUnicodeString(random).Trim();
                    if (s.Length != 0 && s.IndexOf('\u0000') == -1)
                    {
                        return s;
                    }
                }
            }
        }



        internal static T NewRandomArg<T>(Random random, Type paramType)
        {
            IArgProducer producer = argProducers[paramType];
            assertNotNull("No producer for arguments of type " + paramType + " found", producer);
            return (T)producer.Create(random);
        }

        internal static object[] NewTokenizerArgs(Random random, TextReader reader, Type[] paramTypes)
        {
            object[] args = new object[paramTypes.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Type paramType = paramTypes[i];
                if (paramType == typeof(TextReader))
                {
                    args[i] = reader;
                }
                else if (paramType == typeof(AttributeSource.AttributeFactory))
                {
                    // TODO: maybe the collator one...???
                    args[i] = AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY;
                }
                else if (paramType == typeof(AttributeSource))
                {
                    // TODO: args[i] = new AttributeSource();
                    // this is currently too scary to deal with!
                    args[i] = null; // force IAE
                }
                else
                {
                    args[i] = NewRandomArg<object>(random, paramType);
                }
            }
            return args;
        }

        internal static object[] NewCharFilterArgs(Random random, TextReader reader, Type[] paramTypes)
        {
            object[] args = new object[paramTypes.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Type paramType = paramTypes[i];
                if (paramType == typeof(TextReader))
                {
                    args[i] = reader;
                }
                else
                {
                    args[i] = NewRandomArg<object>(random, paramType);
                }
            }
            return args;
        }

        static object[] NewFilterArgs(Random random, TokenStream stream, Type[] paramTypes)
        {
            object[] args = new object[paramTypes.Length];
            for (int i = 0; i < args.Length; i++)
            {
                Type paramType = paramTypes[i];
                if (paramType == typeof(TokenStream))
                {
                    args[i] = stream;
                }
                else if (paramType == typeof(CommonGramsFilter))
                {
                    // TODO: fix this one, thats broken: CommonGramsQueryFilter takes this one explicitly
                    args[i] = new CommonGramsFilter(TEST_VERSION_CURRENT, stream, NewRandomArg<CharArraySet>(random, typeof(CharArraySet)));
                }
                else
                {
                    args[i] = NewRandomArg<object>(random, paramType);
                }
            }
            return args;
        }

        private class MockRandomAnalyzer : Analyzer
        {
            internal readonly int seed;


            public MockRandomAnalyzer(int seed)
            {
                this.seed = seed;
            }

            public bool OffsetsAreCorrect
            {
                get
                {
                    // TODO: can we not do the full chain here!?
                    Random random = new Random(seed);
                    TokenizerSpec tokenizerSpec = NewTokenizer(random, new StringReader(""));
                    TokenFilterSpec filterSpec = NewFilterChain(random, tokenizerSpec.tokenizer, tokenizerSpec.offsetsAreCorrect);
                    return filterSpec.offsetsAreCorrect;
                }
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Random random = new Random(seed);
                TokenizerSpec tokenizerSpec = NewTokenizer(random, reader);
                //System.out.println("seed=" + seed + ",create tokenizer=" + tokenizerSpec.toString);
                TokenFilterSpec filterSpec = NewFilterChain(random, tokenizerSpec.tokenizer, tokenizerSpec.offsetsAreCorrect);
                //System.out.println("seed=" + seed + ",create filter=" + filterSpec.toString);
                return new TokenStreamComponents(tokenizerSpec.tokenizer, filterSpec.stream);
            }

            public override TextReader InitReader(string fieldName, TextReader reader)
            {
                Random random = new Random(seed);
                CharFilterSpec charfilterspec = NewCharFilterChain(random, reader);
                return charfilterspec.reader;
            }


            public override string ToString()
            {
                Random random = new Random(seed);
                StringBuilder sb = new StringBuilder();
                CharFilterSpec charFilterSpec = NewCharFilterChain(random, new StringReader(""));
                sb.Append("\ncharfilters=");
                sb.Append(charFilterSpec.toString);
                // intentional: initReader gets its own separate random
                random = new Random(seed);
                TokenizerSpec tokenizerSpec = NewTokenizer(random, charFilterSpec.reader);
                sb.Append("\n");
                sb.Append("tokenizer=");
                sb.Append(tokenizerSpec.toString);
                TokenFilterSpec tokenFilterSpec = NewFilterChain(random, tokenizerSpec.tokenizer, tokenizerSpec.offsetsAreCorrect);
                sb.Append("\n");
                sb.Append("filters=");
                sb.Append(tokenFilterSpec.toString);
                sb.Append("\n");
                sb.Append("offsetsAreCorrect=" + tokenFilterSpec.offsetsAreCorrect);
                return sb.ToString();
            }

            private T CreateComponent<T>(ConstructorInfo ctor, object[] args, StringBuilder descr)
            {
                try
                {
                    T instance = (T)ctor.Invoke(args);
                    /*
                    if (descr.length() > 0) {
                      descr.append(",");
                    }
                    */
                    descr.append("\n  ");
                    descr.append(ctor.DeclaringType.Name);
                    string @params = Arrays.ToString(args);
                    //@params = @params.Substring(1, (@params.Length - 1) - 1); // LUCENENET - This is causing truncation of types
                    descr.append("(").append(@params).append(")");
                    return instance;
                }
                catch (TargetInvocationException ite)
                {
                    if (ite.InnerException != null && (ite.InnerException.GetType().Equals(typeof(ArgumentException))
                        || ite.InnerException.GetType().Equals(typeof(ArgumentOutOfRangeException))
                        || ite.InnerException.GetType().Equals(typeof(NotSupportedException))))
                    {

                        // thats ok, ignore
                        if (VERBOSE)
                        {
                            Console.WriteLine("Ignoring IAE/UOE from ctor:");
                            //cause.printStackTrace(System.err);
                        }
                    }
                    else
                    {
                        throw ite;
                    }
                }
                //catch (IllegalAccessException iae)
                //{
                //    Rethrow.rethrow(iae);
                //}
                //catch (InstantiationException ie)
                //{
                //    Rethrow.rethrow(ie);
                //}
                return default(T); // no success
            }

            private bool Broken(ConstructorInfo ctor, object[] args)
            {
                IPredicate<object[]> pred = brokenConstructors.ContainsKey(ctor) ? brokenConstructors[ctor] : null;
                return pred != null && pred.Apply(args);
            }

            private bool BrokenOffsets(ConstructorInfo ctor, object[] args)
            {
                IPredicate<object[]> pred = brokenOffsetsConstructors.ContainsKey(ctor) ? brokenOffsetsConstructors[ctor] : null;
                return pred != null && pred.Apply(args);
            }

            // create a new random tokenizer from classpath
            private TokenizerSpec NewTokenizer(Random random, TextReader reader)
            {
                TokenizerSpec spec = new TokenizerSpec();
                while (spec.tokenizer == null)
                {
                    ConstructorInfo ctor = tokenizers[random.nextInt(tokenizers.size())];
                    StringBuilder descr = new StringBuilder();
                    CheckThatYouDidntReadAnythingReaderWrapper wrapper = new CheckThatYouDidntReadAnythingReaderWrapper(reader);
                    object[] args = NewTokenizerArgs(random, wrapper, ctor.GetParameters().Select(p => p.ParameterType).ToArray());
                    if (Broken(ctor, args))
                    {
                        continue;
                    }
                    spec.tokenizer = CreateComponent<Tokenizer>(ctor, args, descr);
                    if (spec.tokenizer != null)
                    {
                        spec.offsetsAreCorrect &= !BrokenOffsets(ctor, args);
                        spec.toString = descr.toString();
                    }
                    else
                    {
                        assertFalse(ctor.DeclaringType.Name + " has read something in ctor but failed with UOE/IAE", wrapper.readSomething);
                    }
                }
                return spec;
            }

            private CharFilterSpec NewCharFilterChain(Random random, TextReader reader)
            {
                CharFilterSpec spec = new CharFilterSpec();
                spec.reader = reader;
                StringBuilder descr = new StringBuilder();
                int numFilters = random.nextInt(3);
                for (int i = 0; i < numFilters; i++)
                {
                    while (true)
                    {
                        ConstructorInfo ctor = charfilters[random.nextInt(charfilters.size())];
                        object[] args = NewCharFilterArgs(random, spec.reader, ctor.GetParameters().Select(p => p.ParameterType).ToArray());
                        if (Broken(ctor, args))
                        {
                            continue;
                        }
                        reader = CreateComponent<TextReader>(ctor, args, descr);
                        if (reader != null)
                        {
                            spec.reader = reader;
                            break;
                        }
                    }
                }
                spec.toString = descr.toString();
                return spec;
            }

            private TokenFilterSpec NewFilterChain(Random random, Tokenizer tokenizer, bool offsetsAreCorrect)
            {
                TokenFilterSpec spec = new TokenFilterSpec();
                spec.offsetsAreCorrect = offsetsAreCorrect;
                spec.stream = tokenizer;
                StringBuilder descr = new StringBuilder();
                int numFilters = random.nextInt(5);
                for (int i = 0; i < numFilters; i++)
                {

                    // Insert ValidatingTF after each stage so we can
                    // catch problems right after the TF that "caused"
                    // them:
                    spec.stream = new ValidatingTokenFilter(spec.stream, "stage " + i, spec.offsetsAreCorrect);

                    while (true)
                    {
                        ConstructorInfo ctor = tokenfilters[random.nextInt(tokenfilters.size())];

                        // hack: MockGraph/MockLookahead has assertions that will trip if they follow
                        // an offsets violator. so we cant use them after e.g. wikipediatokenizer
                        if (!spec.offsetsAreCorrect &&
                            (ctor.DeclaringType.Equals(typeof(MockGraphTokenFilter)))
                                || ctor.DeclaringType.Equals(typeof(MockRandomLookaheadTokenFilter)))
                        {
                            continue;
                        }

                        object[] args = NewFilterArgs(random, spec.stream, ctor.GetParameters().Select(p => p.ParameterType).ToArray());
                        if (Broken(ctor, args))
                        {
                            continue;
                        }
                        TokenFilter flt = CreateComponent<TokenFilter>(ctor, args, descr);
                        if (flt != null)
                        {
                            spec.offsetsAreCorrect &= !BrokenOffsets(ctor, args);
                            spec.stream = flt;
                            break;
                        }
                    }
                }

                // Insert ValidatingTF after each stage so we can
                // catch problems right after the TF that "caused"
                // them:
                spec.stream = new ValidatingTokenFilter(spec.stream, "last stage", spec.offsetsAreCorrect);

                spec.toString = descr.toString();
                return spec;
            }
        }


        internal class CheckThatYouDidntReadAnythingReaderWrapper : CharFilter
        {
            internal bool readSomething;

            public CheckThatYouDidntReadAnythingReaderWrapper(TextReader @in)
                : base(@in)
            { }

            private CharFilter Input
            {
                get { return (CharFilter)this.input; }
            }

            protected override int Correct(int currentOff)
            {
                return currentOff; // we don't change any offsets
            }

            public override int Read(char[] cbuf, int off, int len)
            {
                readSomething = true;
                return input.Read(cbuf, off, len);
            }

            public override int Read()
            {
                readSomething = true;
                return input.Read();
            }

            // LUCENENET: TextReader dosn't support this overload 
            //public int read(char[] cbuf)
            //{
            //    readSomething = true;
            //    return input.read(cbuf);
            //}

            public override long Skip(int n)
            {
                readSomething = true;
                return Input.Skip(n);
            }

            public override void Mark(int readAheadLimit)
            {
                Input.Mark(readAheadLimit);
            }

            public override bool IsMarkSupported
            {
                get
                {
                    return Input.IsMarkSupported;
                }
            }

            public override bool Ready()
            {
                return Input.Ready();
            }

            public override void Reset()
            {
                Input.Reset();
            }
        }

        internal class TokenizerSpec
        {
            internal Tokenizer tokenizer;
            internal string toString;
            internal bool offsetsAreCorrect = true;
        }

        internal class TokenFilterSpec
        {
            internal TokenStream stream;
            internal string toString;
            internal bool offsetsAreCorrect = true;
        }

        internal class CharFilterSpec
        {
            internal TextReader reader;
            internal string toString;
        }

        [Test, LongRunningTest]
        [DtdProcessingTest]
        public void TestRandomChains_()
        {
            int numIterations = AtLeast(20);
            Random random = Random();
            for (int i = 0; i < numIterations; i++)
            {
                MockRandomAnalyzer a = new MockRandomAnalyzer(random.Next());
                if (VERBOSE)
                {
                    Console.WriteLine("Creating random analyzer:" + a);
                }
                try
                {
                    CheckRandomData(random, a, 500 * RANDOM_MULTIPLIER, 20, false,
                                    false /* We already validate our own offsets... */);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception from random analyzer: " + a);
                    throw e;
                }
            }
        }

        // we might regret this decision...
        [Test, LongRunningTest]
        public void TestRandomChainsWithLargeStrings()
        {
            int numIterations = AtLeast(20);
            Random random = Random();
            for (int i = 0; i < numIterations; i++)
            {
                MockRandomAnalyzer a = new MockRandomAnalyzer(random.Next());
                if (VERBOSE)
                {
                    Console.WriteLine("Creating random analyzer:" + a);
                }
                try
                {
                    CheckRandomData(random, a, 50 * RANDOM_MULTIPLIER, 128, false,
                                    false /* We already validate our own offsets... */);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception from random analyzer: " + a);
                    throw e;
                }
            }
        }
    }
}
