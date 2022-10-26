// Lucene version compatibility level 4.8.1

using J2N;
using J2N.Runtime.CompilerServices;
using J2N.Text;
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.CommonGrams;
using Lucene.Net.Analysis.Compound;
using Lucene.Net.Analysis.Compound.Hyphenation;
using Lucene.Net.Analysis.Hunspell;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.No;
using Lucene.Net.Analysis.Path;
using Lucene.Net.Analysis.Payloads;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Wikipedia;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Tartarus.Snowball;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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

        internal static IList<ConstructorInfo> tokenizers;
        internal static IList<ConstructorInfo> tokenfilters;
        internal static IList<ConstructorInfo> charfilters;

        private interface IPredicate<T>
        {
            bool Apply(T o);
        }

        private static readonly IPredicate<object[]> ALWAYS = new PredicateAnonymousClass();

        private sealed class PredicateAnonymousClass : IPredicate<object[]>
        {
            public PredicateAnonymousClass()
            {
            }

            public bool Apply(object[] args)
            {
                return true;
            }
        }

        private static readonly IDictionary<ConstructorInfo, IPredicate<object[]>> brokenConstructors = new Dictionary<ConstructorInfo, IPredicate<object[]>>();
        // TODO: also fix these and remove (maybe):
        // Classes/options that don't produce consistent graph offsets:
        private static readonly IDictionary<ConstructorInfo, IPredicate<object[]>> brokenOffsetsConstructors = new Dictionary<ConstructorInfo, IPredicate<object[]>>();

        internal static readonly ISet<Type> allowedTokenizerArgs, allowedTokenFilterArgs, allowedCharFilterArgs;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Complexity")]
        static TestRandomChains()
        {
            try
            {
                brokenConstructors[typeof(LimitTokenCountFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int) })] = ALWAYS;
                brokenConstructors[typeof(LimitTokenCountFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int), typeof(bool) })] = new PredicateAnonymousClass2();
                brokenConstructors[typeof(LimitTokenPositionFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int) })] = ALWAYS;
                brokenConstructors[typeof(LimitTokenPositionFilter).GetConstructor(new Type[] { typeof(TokenStream), typeof(int), typeof(bool) })] = new PredicateAnonymousClass3();
                foreach (Type c in new Type[] {
                    // TODO: can we promote some of these to be only
                    // offsets offenders?
                    // doesn't actual reset itself:
                    typeof(CachingTokenFilter),
                    // Not broken, simulates brokenness:
                    typeof(CrankyTokenFilter),
                    // Not broken: we forcefully add this, so we shouldn't
                    // also randomly pick it:
                    typeof(ValidatingTokenFilter),
                })
                {
                    foreach (ConstructorInfo ctor in c.GetConstructors())
                    {
                        brokenConstructors[ctor] = ALWAYS;
                    }
                }
            }
            catch (Exception e) when (e.IsException())
            {
                throw Error.Create(e);
            }
            try
            {
                foreach (Type c in new Type[] {
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
                    typeof(WordDelimiterFilter) })
                {
                    foreach (ConstructorInfo ctor in c.GetConstructors())
                    {
                        brokenOffsetsConstructors[ctor] = ALWAYS;
                    }
                }
            }
            catch (Exception e) when (e.IsException())
            {
                throw Error.Create(e);
            }

            allowedTokenizerArgs = new JCG.HashSet<Type>(IdentityEqualityComparer<Type>.Default);
            allowedTokenizerArgs.addAll(argProducers.Keys);
            allowedTokenizerArgs.Add(typeof(TextReader));
            allowedTokenizerArgs.Add(typeof(AttributeSource.AttributeFactory));
            allowedTokenizerArgs.Add(typeof(AttributeSource));

            allowedTokenFilterArgs = new JCG.HashSet<Type>(IdentityEqualityComparer<Type>.Default);
            allowedTokenFilterArgs.addAll(argProducers.Keys);
            allowedTokenFilterArgs.Add(typeof(TokenStream));
            // TODO: fix this one, thats broken:
            allowedTokenFilterArgs.Add(typeof(CommonGramsFilter));

            allowedCharFilterArgs = new JCG.HashSet<Type>(IdentityEqualityComparer<Type>.Default);
            allowedCharFilterArgs.addAll(argProducers.Keys);
            allowedCharFilterArgs.Add(typeof(TextReader));
        }

        private sealed class PredicateAnonymousClass2 : IPredicate<object[]>
        {
            public PredicateAnonymousClass2()
            {
            }

            public bool Apply(object[] args)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(args.Length == 3);
                return !((bool)args[2]); // args are broken if consumeAllTokens is false
            }
        }

        private sealed class PredicateAnonymousClass3 : IPredicate<object[]>
        {
            public PredicateAnonymousClass3()
            {
            }

            public bool Apply(object[] args)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(args.Length == 3);
                return !((bool)args[2]); // args are broken if consumeAllTokens is false
            }
        }

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            IEnumerable<Type> analysisClasses = typeof(StandardAnalyzer).Assembly.GetTypes()
                .Where(c => {
                    var typeInfo = c;

                    return !typeInfo.IsAbstract && typeInfo.IsPublic && !typeInfo.IsInterface 
                        && typeInfo.IsClass && (typeInfo.GetCustomAttribute<ObsoleteAttribute>() is null)
                        && (typeInfo.IsSubclassOf(typeof(Tokenizer)) || typeInfo.IsSubclassOf(typeof(TokenFilter)) || typeInfo.IsSubclassOf(typeof(CharFilter)));
                })
                .ToArray();
            tokenizers = new JCG.List<ConstructorInfo>();
            tokenfilters = new JCG.List<ConstructorInfo>();
            charfilters = new JCG.List<ConstructorInfo>();
            foreach (Type c in analysisClasses)
            {
                foreach (ConstructorInfo ctor in c.GetConstructors())
                {
                    if (ctor.GetCustomAttribute<ObsoleteAttribute>() != null || (brokenConstructors.ContainsKey(ctor) && brokenConstructors[ctor] == ALWAYS))
                    {
                        continue;
                    }

                    var typeInfo = c;

                    if (typeInfo.IsSubclassOf(typeof(Tokenizer)))
                    {
                        assertTrue(ctor.ToString() + " has unsupported parameter types", 
                            allowedTokenizerArgs.containsAll(ctor.GetParameters().Select(p => p.ParameterType).ToArray()));
                        tokenizers.Add(ctor);
                    }
                    else if (typeInfo.IsSubclassOf(typeof(TokenFilter)))
                    {
                        assertTrue(ctor.ToString() + " has unsupported parameter types", 
                            allowedTokenFilterArgs.containsAll(ctor.GetParameters().Select(p => p.ParameterType).ToArray()));
                        tokenfilters.Add(ctor);
                    }
                    else if (typeInfo.IsSubclassOf(typeof(CharFilter)))
                    {
                        assertTrue(ctor.ToString() + " has unsupported parameter types", 
                            allowedCharFilterArgs.containsAll(ctor.GetParameters().Select(p => p.ParameterType).ToArray()));
                        charfilters.Add(ctor);
                    }
                    else
                    {
                        fail("Cannot get here");
                    }
                }
            }

            IComparer<ConstructorInfo> ctorComp = Comparer<ConstructorInfo>.Create((arg0, arg1)=> arg0.ToString().CompareToOrdinal(arg1.ToString()));
            tokenizers.Sort(ctorComp);
            tokenfilters.Sort(ctorComp);
            charfilters.Sort(ctorComp);
            if (Verbose)
            {
                Console.WriteLine("tokenizers = " + tokenizers);
                Console.WriteLine("tokenfilters = " + tokenfilters);
                Console.WriteLine("charfilters = " + charfilters);
            }
        }
        
        [OneTimeTearDown]
        public override void AfterClass()
        {
            tokenizers = null;
            tokenfilters = null;
            charfilters = null;

            base.AfterClass();
        }


        private interface IArgProducer
        {
            object Create(Random random);
        }

        private static readonly IDictionary<Type, IArgProducer> argProducers = new JCG.Dictionary<Type, IArgProducer>(IdentityEqualityComparer<Type>.Default)
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
            { typeof(ICollection<string>), new StringEnumerableArgProducer() },
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
            { typeof(CharArrayDictionary<string>), new StringCharArrayMapArgProducer() },
            { typeof(StemmerOverrideFilter.StemmerOverrideMap), new StemmerOverrideMapArgProducer() },
            { typeof(SynonymMap), new SynonymMapArgProducer() },
            { typeof(WordDelimiterFlags), new AnonymousProducer((random) => {
                int max = Enum.GetValues(typeof(WordDelimiterFlags)).Cast<int>().Sum();
                return (WordDelimiterFlags)random.Next(0, max + 1);
            }) }, // WordDelimiterFilter
            { typeof(NorwegianStandard), new AnonymousProducer((random) => {
                int max = Enum.GetValues(typeof(NorwegianStandard)).Cast<int>().Sum();
                return (NorwegianStandard)random.Next(0, max + 1);
            }) },
            { typeof(CJKScript), new AnonymousProducer((random) => {
                int max = Enum.GetValues(typeof(CJKScript)).Cast<int>().Sum();
                return (CJKScript)random.Next(0, max + 1);
            }) },
            { typeof(CultureInfo), new AnonymousProducer((random) => {
                return LuceneTestCase.RandomCulture(random);
            }) },
        };

        private class IntArgProducer : IArgProducer
        {
            public object Create(Random random)
            {
                // TODO: could cause huge ram usage to use full int range for some filters
                // (e.g. allocate enormous arrays)
                // return Integer.valueOf(random.nextInt());
                return TestUtil.NextInt32(random, -100, 100);
            }
        }

        private class AnonymousProducer : IArgProducer
        {
            private readonly Func<Random, object> create;

            public AnonymousProducer(Func<Random, object> create)
            {
                this.create = create ?? throw new ArgumentNullException(nameof(create));
            }

            public object Create(Random random)
            {
                return create(random);
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
                return new J2N.Randomizer(random.NextInt64());
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
                ISet<string> set = new JCG.HashSet<string>();
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
                ICollection<char[]> col = new JCG.List<char[]>();
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
                using Stream affixStream = typeof(TestHunspellStemFilter).getResourceAsStream("simple.aff");
                using Stream dictStream = typeof(TestHunspellStemFilter).getResourceAsStream("simple.dic");
                try
                {
                    return new Dictionary(affixStream, dictStream);
                }
                catch (Exception ex) when (ex.IsException())
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
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
                    using Stream @is = typeof(TestCompoundWordTokenFilter).getResourceAsStream("da_UTF8.xml");
                    HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.GetHyphenationTree(@is);
                    return hyphenator;
                }
                catch (Exception ex) when (ex.IsException())
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
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
                catch (Exception ex) when (ex.IsException())
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
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
                ISet<string> keys = new JCG.HashSet<string>();
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
                CharArrayDictionary<string> map = new CharArrayDictionary<string>(TEST_VERSION_CURRENT, num, random.nextBoolean());
                for (int i = 0; i < num; i++)
                {
                    // TODO: make nastier
                    map[TestUtil.RandomSimpleString(random)] = TestUtil.RandomSimpleString(random);
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
                    } while (input.Length == 0); // LUCENENET: CA1820: Test for empty strings using string length
                    string @out = ""; TestUtil.RandomSimpleString(random);
                    do
                    {
                        @out = TestUtil.RandomRealisticUnicodeString(random);
                    } while (@out.Length == 0); // LUCENENET: CA1820: Test for empty strings using string length
                    builder.Add(input, @out);
                }
                try
                {
                    return builder.Build();
                }
                catch (Exception ex) when (ex.IsException())
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
                }
            }
        }

        private class SynonymMapArgProducer : IArgProducer
        {
            // LUCENENET specific: Added functionality to export the source code of the builder with all of the escaped text.
            //
            // USAGE
            //
            // This is intended to be used in conjunction with a fixed random seed after a failure. You should confirm
            // first that you can reproduce a test failure before exporting.
            //
            // 1. Change exportSourceCode to true to export the source code
            // 2. Update the exportPath to a folder on your local system. Note that it won't automatically create directories.
            //    The {0} token will be replaced with a map number. Normally, you would get the contents of the highest number,
            //    which is the last successful map that occurred before there was a test failure.

            private StringBuilder sb = new StringBuilder();
            private static bool exportSourceCode = false;                 // Change to enable source code export.
            private static string exportPath = @"F:\synonym-map-{0}.txt"; // Change as necessary for your system.
            private int mapCount = 0;
            public object Create(Random random)
            {
                if (exportSourceCode) sb.Clear();
                bool dedup = random.NextBoolean();
                SynonymMap.Builder b = new SynonymMap.Builder(dedup);

                if (exportSourceCode) sb.AppendLine($"SynonymMap.Builder b = new SynonymMap.Builder({dedup.ToString().ToLowerInvariant()});");

                int numEntries = AtLeast(10);
                for (int j = 0; j < numEntries; j++)
                {
                    AddSyn(b, RandomNonEmptyString(random), RandomNonEmptyString(random), random.NextBoolean());
                }
                try
                {
                    if (exportSourceCode)
                    {
                        sb.AppendLine("SynonymMap synonymMap = b.Build();");
                        mapCount++;
                        string path = string.Format(exportPath, mapCount);
                        File.WriteAllText(path, sb.ToString());
                    }

                    return b.Build();
                }
                catch (Exception ex) when (ex.IsException())
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
#pragma warning disable 162
                    return null; // unreachable code
#pragma warning restore 162
                }
            }

            private static readonly Regex whiteSpace = new Regex(" +", RegexOptions.Compiled);

            private void AddSyn(SynonymMap.Builder b, string input, string output, bool keepOrig)
            {
                string inputNoSpaces = whiteSpace.Replace(input, "\u0000");
                string outputNoSpaces = whiteSpace.Replace(output, "\u0000");

                if (exportSourceCode)
                {
                    sb.AppendLine($"b.Add(new CharsRef(\"{Escape(inputNoSpaces)}\"),");
                    sb.AppendLine($"    new CharsRef(\"{Escape(outputNoSpaces)}\"),");
                    sb.AppendLine($"    {keepOrig.ToString().ToLowerInvariant()});");
                    sb.AppendLine();
                }

                b.Add(new CharsRef(inputNoSpaces),
                    new CharsRef(outputNoSpaces),
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
            internal readonly long seed;

            public MockRandomAnalyzer(long seed)
            {
                this.seed = seed;
            }

            public bool OffsetsAreCorrect
            {
                get
                {
                    // TODO: can we not do the full chain here!?
                    Random random = new Randomizer(seed);
                    TokenizerSpec tokenizerSpec = NewTokenizer(random, new StringReader(""));
                    TokenFilterSpec filterSpec = NewFilterChain(random, tokenizerSpec.tokenizer, tokenizerSpec.offsetsAreCorrect);
                    return filterSpec.offsetsAreCorrect;
                }
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Random random = new Randomizer(seed);
                TokenizerSpec tokenizerSpec = NewTokenizer(random, reader);
                //System.out.println("seed=" + seed + ",create tokenizer=" + tokenizerSpec.toString);
                TokenFilterSpec filterSpec = NewFilterChain(random, tokenizerSpec.tokenizer, tokenizerSpec.offsetsAreCorrect);
                //System.out.println("seed=" + seed + ",create filter=" + filterSpec.toString);
                return new TokenStreamComponents(tokenizerSpec.tokenizer, filterSpec.stream);
            }

            protected internal override TextReader InitReader(string fieldName, TextReader reader)
            {
                Random random = new Randomizer(seed);
                CharFilterSpec charfilterspec = NewCharFilterChain(random, reader);
                return charfilterspec.reader;
            }


            public override string ToString()
            {
                Random random = new Randomizer(seed);
                StringBuilder sb = new StringBuilder();
                CharFilterSpec charFilterSpec = NewCharFilterChain(random, new StringReader(""));
                sb.Append("\ncharfilters=");
                sb.Append(charFilterSpec.toString);
                // intentional: initReader gets its own separate random
                random = new Randomizer(seed);
                TokenizerSpec tokenizerSpec = NewTokenizer(random, charFilterSpec.reader);
                sb.Append('\n');
                sb.Append("tokenizer=");
                sb.Append(tokenizerSpec.toString);
                TokenFilterSpec tokenFilterSpec = NewFilterChain(random, tokenizerSpec.tokenizer, tokenizerSpec.offsetsAreCorrect);
                sb.Append('\n');
                sb.Append("filters=");
                sb.Append(tokenFilterSpec.toString);
                sb.Append('\n');
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
                catch (Exception ite) when (ite.IsInvocationTargetException())
                {
                    if (ite.InnerException != null && (ite.InnerException.GetType().Equals(typeof(ArgumentException))
                        || ite.InnerException.GetType().Equals(typeof(ArgumentOutOfRangeException))
                        || ite.InnerException.GetType().Equals(typeof(NotSupportedException))))
                    {

                        // thats ok, ignore
                        if (Verbose)
                        {
                            Console.WriteLine("Ignoring IAE/UOE from ctor:");
                            //cause.printStackTrace(System.err);
                        }
                    }
                    else
                    {
                        throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                    }
                }
                // LUCENENET: These are not necessary because all they do is catch and re-throw as an "unchecked"
                // exception type, which .NET doesn't care about. Just let them propagate instead of catching.
                //catch (Exception iae) when (iae.IsIllegalAccessException())
                //{
                //    throw;
                //}
                //catch (Exception ie) when (ie.IsInstantiationException())
                //{
                //    throw;
                //}
                return default; // no success
            }

            private static bool Broken(ConstructorInfo ctor, object[] args) // LUCENENET: CA1822: Mark members as static
            {
                return brokenConstructors.TryGetValue(ctor, out IPredicate<object[]> pred) && pred != null && pred.Apply(args);
            }

            private static bool BrokenOffsets(ConstructorInfo ctor, object[] args) // LUCENENET: CA1822: Mark members as static
            {
                return brokenOffsetsConstructors.TryGetValue(ctor, out IPredicate<object[]> pred) && pred != null && pred.Apply(args);
            }

            // create a new random tokenizer from classpath
            private TokenizerSpec NewTokenizer(Random random, TextReader reader)
            {
                TokenizerSpec spec = new TokenizerSpec();
                while (spec.tokenizer is null)
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

            private CharFilter Input => (CharFilter)this.m_input;

            protected override int Correct(int currentOff)
            {
                return currentOff; // we don't change any offsets
            }

            public override int Read(char[] cbuf, int off, int len)
            {
                readSomething = true;
                return m_input.Read(cbuf, off, len);
            }

            public override int Read()
            {
                readSomething = true;
                return m_input.Read();
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

            public override bool IsMarkSupported => Input.IsMarkSupported;

            public override bool IsReady => Input.IsReady;

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

        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/271#issuecomment-973005744")] // LUCENENET TODO: this test occasionally fails
        public void TestRandomChains_()
        {
            int numIterations = AtLeast(20);
            Random random = Random;
            for (int i = 0; i < numIterations; i++)
            {
                MockRandomAnalyzer a = new MockRandomAnalyzer(random.NextInt64());
                if (Verbose)
                {
                    Console.WriteLine("Creating random analyzer:" + a);
                }
                try
                {
                    CheckRandomData(random, a, 500 * RandomMultiplier, 20, false,
                                    false /* We already validate our own offsets... */);
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine("Exception from random analyzer (iteration {i}): " + a);
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
        }

        // we might regret this decision...
        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/271#issuecomment-973005744")] // LUCENENET TODO: this test occasionally fails
        public void TestRandomChainsWithLargeStrings()
        {
            int numIterations = AtLeast(20);
            Random random = Random;
            for (int i = 0; i < numIterations; i++)
            {
                MockRandomAnalyzer a = new MockRandomAnalyzer(random.NextInt64());
                if (Verbose)
                {
                    Console.WriteLine("Creating random analyzer:" + a);
                }
                try
                {
                    CheckRandomData(random, a, 50 * RandomMultiplier, 128, false,
                                    false /* We already validate our own offsets... */);
                }
                catch (Exception e) when (e.IsThrowable())
                {
                    Console.WriteLine($"Exception from random analyzer (iteration {i}): " + a);
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
        }
    }
}