using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Asserting;
using Lucene.Net.Codecs.CheapBastard;
using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.MockRandom;
using Lucene.Net.Codecs.SimpleText;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using RandomizedTesting.Generators;
using AssumptionViolatedException = NUnit.Framework.InconclusiveException;

namespace Lucene.Net.Util
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
    /// Setup and restore suite-level environment (fine grained junk that
    /// doesn't fit anywhere else).
    /// </summary>
    // LUCENENET specific: This class was refactored to be called directly from LuceneTestCase, since
    // we didn't port over the entire test suite from Java.
    internal sealed class TestRuleSetupAndRestoreClassEnv : AbstractBeforeAfterRule
    {
        ///// <summary>
        ///// Restore these system property values.
        ///// </summary>
        //private Dictionary<string, string> restoreProperties = new Dictionary<string, string>(); // LUCENENET: Never read

        private Codec savedCodec;
        private CultureInfo savedLocale;
        private InfoStream savedInfoStream;
        private TimeZoneInfo savedTimeZone;

        internal CultureInfo locale;
        internal TimeZoneInfo timeZone;
        internal Similarity similarity;
        internal Codec codec;

        /// <seealso cref="LuceneTestCase.SuppressCodecsAttribute"/>
        internal ISet<string> avoidCodecs;

        internal class ThreadNameFixingPrintStreamInfoStream : TextWriterInfoStream
        {
            public ThreadNameFixingPrintStreamInfoStream(TextWriter @out)
                : base(@out)
            {
            }

            public override void Message(string component, string message)
            {
                if ("TP".Equals(component, StringComparison.Ordinal))
                {
                    return; // ignore test points!
                }
                string name;
                if (Thread.CurrentThread.Name != null && Thread.CurrentThread.Name.StartsWith("TEST-", StringComparison.Ordinal))
                {
                    // The name of the main thread is way too
                    // long when looking at IW verbose output...
                    name = "main";
                }
                else
                {
                    name = Thread.CurrentThread.Name;
                }
                m_stream.WriteLine(component + " " + m_messageID + " [" + DateTime.Now + "; " + name + "]: " + message);
            }
        }

        public override void Before()
        {
            // LUCENENET specific - SOLR setup code removed

            // if verbose: print some debugging stuff about which codecs are loaded.
            if (LuceneTestCase.Verbose)
            {
                // LUCENENET: Only list the services if the underlying ICodecFactory
                // implements IServiceListable
                if (Codec.GetCodecFactory() is IServiceListable)
                {
                    ICollection<string> codecs = Codec.AvailableCodecs;
                    foreach (string codec in codecs)
                    {
                        Console.WriteLine("Loaded codec: '" + codec + "': " + Codec.ForName(codec).GetType().Name);
                    }
                }

                // LUCENENET: Only list the services if the underlying IPostingsFormatFactory
                // implements IServiceListable
                if (PostingsFormat.GetPostingsFormatFactory() is IServiceListable)
                {
                    ICollection<string> postingsFormats = PostingsFormat.AvailablePostingsFormats;
                    foreach (string postingsFormat in postingsFormats)
                    {
                        Console.WriteLine("Loaded postingsFormat: '" + postingsFormat + "': " + PostingsFormat.ForName(postingsFormat).GetType().Name);
                    }
                }
            }

            savedInfoStream = InfoStream.Default;
            Random random = LuceneTestCase.Random; 
            bool v = random.NextBoolean();
            if (LuceneTestCase.UseInfoStream)
            {
                InfoStream.Default = new ThreadNameFixingPrintStreamInfoStream(Console.Out);
            }
            else if (v)
            {
                InfoStream.Default = new NullInfoStream();
            }

            Type targetClass = LuceneTestCase.TestType;
            avoidCodecs = new JCG.HashSet<string>();
            var suppressCodecsAttribute = targetClass.GetCustomAttribute<LuceneTestCase.SuppressCodecsAttribute>();
            if (suppressCodecsAttribute != null)
            {
                avoidCodecs.UnionWith(suppressCodecsAttribute.Value);
            }

            // set back to default
            LuceneTestCase.OldFormatImpersonationIsActive = false;

            savedCodec = Codec.Default;
            int randomVal = random.Next(10);
            if ("Lucene3x".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) &&
                                                                "random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal) &&
                                                                "random".Equals(LuceneTestCase.TestDocValuesFormat, StringComparison.Ordinal) &&
                                                                randomVal == 3 &&
                                                                !ShouldAvoidCodec("Lucene3x"))) // preflex-only setup
            {
                codec = Codec.ForName("Lucene3x");
                if (Debugging.AssertsEnabled) Debugging.Assert((codec is PreFlexRWCodec), "fix your ICodecFactory to scan Lucene.Net.Tests before Lucene.Net.TestFramework");
                LuceneTestCase.OldFormatImpersonationIsActive = true;
            }
            else if ("Lucene40".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal) &&
                                                                    randomVal == 0 &&
                                                                    !ShouldAvoidCodec("Lucene40"))) // 4.0 setup
            {
                codec = Codec.ForName("Lucene40");
                LuceneTestCase.OldFormatImpersonationIsActive = true;
                if (Debugging.AssertsEnabled) Debugging.Assert((codec is Lucene40RWCodec), "fix your ICodecFactory to scan Lucene.Net.Tests before Lucene.Net.TestFramework");
                if (Debugging.AssertsEnabled) Debugging.Assert((PostingsFormat.ForName("Lucene40") is Lucene40RWPostingsFormat), "fix your IPostingsFormatFactory to scan Lucene.Net.Tests before Lucene.Net.TestFramework");
            }
            else if ("Lucene41".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestDocValuesFormat, StringComparison.Ordinal) &&
                                                                    randomVal == 1 &&
                                                                    !ShouldAvoidCodec("Lucene41")))
            {
                codec = Codec.ForName("Lucene41");
                LuceneTestCase.OldFormatImpersonationIsActive = true;
                if (Debugging.AssertsEnabled) Debugging.Assert((codec is Lucene41RWCodec), "fix your ICodecFactory to scan Lucene.Net.Tests before Lucene.Net.TestFramework");
            }
            else if ("Lucene42".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestDocValuesFormat, StringComparison.Ordinal) &&
                                                                    randomVal == 2 &&
                                                                    !ShouldAvoidCodec("Lucene42")))
            {
                codec = Codec.ForName("Lucene42");
                LuceneTestCase.OldFormatImpersonationIsActive = true;
                if (Debugging.AssertsEnabled) Debugging.Assert((codec is Lucene42RWCodec), "fix your ICodecFactory to scan Lucene.Net.Tests before Lucene.Net.TestFramework");
            }
            else if ("Lucene45".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal) &&
                                                                    "random".Equals(LuceneTestCase.TestDocValuesFormat, StringComparison.Ordinal) &&
                                                                    randomVal == 5 &&
                                                                    !ShouldAvoidCodec("Lucene45")))
            {
                codec = Codec.ForName("Lucene45");
                LuceneTestCase.OldFormatImpersonationIsActive = true;
                if (Debugging.AssertsEnabled) Debugging.Assert((codec is Lucene45RWCodec), "fix your ICodecFactory to scan Lucene.Net.Tests before Lucene.Net.TestFramework");
            }
            else if (("random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal) == false) 
                || ("random".Equals(LuceneTestCase.TestDocValuesFormat, StringComparison.Ordinal) == false))
            {
                // the user wired postings or DV: this is messy
                // refactor into RandomCodec....

                PostingsFormat format;
                if ("random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal))
                {
                    format = PostingsFormat.ForName("Lucene41");
                }
                else if ("MockRandom".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal))
                {
                    format = new MockRandomPostingsFormat(new J2N.Randomizer(random.NextInt64()));
                }
                else
                {
                    format = PostingsFormat.ForName(LuceneTestCase.TestPostingsFormat);
                }

                DocValuesFormat dvFormat;
                if ("random".Equals(LuceneTestCase.TestDocValuesFormat, StringComparison.Ordinal))
                {
                    dvFormat = DocValuesFormat.ForName("Lucene45");
                }
                else
                {
                    dvFormat = DocValuesFormat.ForName(LuceneTestCase.TestDocValuesFormat);
                }

                codec = new Lucene46CodecAnonymousClass(format, dvFormat);
            }
            else if ("SimpleText".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) 
                || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) && randomVal == 9 && LuceneTestCase.Rarely(random) && !ShouldAvoidCodec("SimpleText")))
            {
                codec = new SimpleTextCodec();
            }
            else if ("CheapBastard".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) 
                || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) && randomVal == 8 && !ShouldAvoidCodec("CheapBastard") && !ShouldAvoidCodec("Lucene41")))
            {
                // we also avoid this codec if Lucene41 is avoided, since thats the postings format it uses.
                codec = new CheapBastardCodec();
            }
            else if ("Asserting".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) 
                || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) && randomVal == 6 && !ShouldAvoidCodec("Asserting")))
            {
                codec = new AssertingCodec();
            }
            else if ("Compressing".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) 
                || ("random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal) && randomVal == 5 && !ShouldAvoidCodec("Compressing")))
            {
                codec = CompressingCodec.RandomInstance(random);
            }
            else if (!"random".Equals(LuceneTestCase.TestCodec, StringComparison.Ordinal))
            {
                codec = Codec.ForName(LuceneTestCase.TestCodec);
            }
            else if ("random".Equals(LuceneTestCase.TestPostingsFormat, StringComparison.Ordinal))
            {
                codec = new RandomCodec(random, avoidCodecs);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(false);
            }
            Codec.Default = codec;

            // Initialize locale/ timezone.
            
            string testTimeZone = SystemProperties.GetProperty("tests:timezone", "random");  // LUCENENET specific - reformatted with :

            // LUCENENET: We need to ensure our random generator stays consistent here so we can repeat the session exactly,
            // so always call this whether the return value is useful or not.
            CultureInfo randomLocale = LuceneTestCase.RandomCulture(random);

            // LUCENENET: Allow NUnit properties to set the culture and respect that culture over our random one.
            // Note that SetCultureAttribute may also be on the test method, but we don't need to deal with that here.
            if (targetClass.Assembly.HasAttribute<NUnit.Framework.SetCultureAttribute>(inherit: true)
                || targetClass.HasAttribute<NUnit.Framework.SetCultureAttribute>(inherit: true))
            {
                locale = CultureInfo.CurrentCulture;
            }
            else
            {
                // LUCENENET: Accept either tests:culture or tests:locale (culture preferred).
                string testLocale = SystemProperties.GetProperty("tests:culture", SystemProperties.GetProperty("tests:locale", "random")); // LUCENENET specific - reformatted with :

                // Always pick a random one for consistency (whether tests.locale was specified or not).
                savedLocale = CultureInfo.CurrentCulture;
                
                locale = testLocale.Equals("random", StringComparison.Ordinal) ? randomLocale : LuceneTestCase.CultureForName(testLocale);
#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
                CultureInfo.CurrentCulture = locale;
#else
                Thread.CurrentThread.CurrentCulture = locale;
#endif
            }

            // TimeZone.getDefault will set user.timezone to the default timezone of the user's locale.
            // So store the original property value and restore it at end.
            // LUCENENET specific - commented
            //restoreProperties["user:timezone"] = SystemProperties.GetProperty("user:timezone");
            savedTimeZone = TimeZoneInfo.Local;
            TimeZoneInfo randomTimeZone = LuceneTestCase.RandomTimeZone(random);
            timeZone = testTimeZone.Equals("random", StringComparison.Ordinal) ? randomTimeZone : TimeZoneInfo.FindSystemTimeZoneById(testTimeZone);
            //TimeZone.Default = TimeZone; // LUCENENET NOTE: There doesn't seem to be an equivalent to this, but I don't think we need it.
            similarity = random.NextBoolean() ? (Similarity)new DefaultSimilarity() : new RandomSimilarityProvider(random);

            // Check codec restrictions once at class level.
            try
            {
                CheckCodecRestrictions(codec);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("NOTE: " + e.Message + " Suppressed codecs: " + avoidCodecs);
                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
            }
        }

        private sealed class Lucene46CodecAnonymousClass : Lucene46Codec
        {
            private readonly PostingsFormat format;
            private readonly DocValuesFormat dvFormat;

            public Lucene46CodecAnonymousClass(PostingsFormat format, DocValuesFormat dvFormat)
            {
                this.format = format;
                this.dvFormat = dvFormat;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return format;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return dvFormat;
            }

            public override string ToString()
            {
                return base.ToString() + ": " + format.ToString() + ", " + dvFormat.ToString();
            }
        }

        /// <summary>
        /// Check codec restrictions.
        /// </summary>
        /// <exception cref="AssumptionViolatedException"> if the class does not work with a given codec. </exception>
        private void CheckCodecRestrictions(Codec codec)
        {
            LuceneTestCase.AssumeFalse("Class not allowed to use codec: " + codec.Name + ".", ShouldAvoidCodec(codec.Name));

            if (codec is RandomCodec randomCodec && avoidCodecs.Count > 0)
            {
                foreach (string name in randomCodec.FormatNames)
                {
                    LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + name + ".", ShouldAvoidCodec(name));
                }
            }

            PostingsFormat pf = codec.PostingsFormat;
            LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + pf.Name + ".", ShouldAvoidCodec(pf.Name));

            LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + LuceneTestCase.TestPostingsFormat + ".", ShouldAvoidCodec(LuceneTestCase.TestPostingsFormat));
        }

        /// <summary>
        /// After suite cleanup (always invoked).
        /// </summary>
        public override void After()
        {
            // LUCENENT specific - Not used in .NET
            //foreach (KeyValuePair<string, string> e in restoreProperties)
            //{
            //    SystemProperties.SetProperty(e.Key, e.Value);
            //}
            //restoreProperties.Clear();

            Codec.Default = savedCodec;
            InfoStream.Default = savedInfoStream;
            if (savedLocale != null)
            {
                locale = savedLocale;
#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
                CultureInfo.CurrentCulture = savedLocale;
#else
                Thread.CurrentThread.CurrentCulture = savedLocale;
#endif
            }
            if (savedTimeZone != null)
            {
                timeZone = savedTimeZone;
            }
        }

        /// <summary>
        /// Should a given codec be avoided for the currently executing suite?
        /// </summary>
        private bool ShouldAvoidCodec(string codec)
        {
            return avoidCodecs.Count > 0 && avoidCodecs.Contains(codec);
        }
    }
}