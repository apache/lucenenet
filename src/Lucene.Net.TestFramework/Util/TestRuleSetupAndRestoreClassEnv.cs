using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Lucene.Net.Util
{
    //using AssumptionViolatedException = org.junit.@internal.AssumptionViolatedException;
    using Lucene.Net.Randomized.Generators;

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
    /*
	import static Lucene.Net.Util.LuceneTestCase.INFOSTREAM;
	import static Lucene.Net.Util.LuceneTestCase.TEST_CODEC;
	import static Lucene.Net.Util.LuceneTestCase.TEST_DOCVALUESFORMAT;
	import static Lucene.Net.Util.LuceneTestCase.TEST_POSTINGSFORMAT;
	import static Lucene.Net.Util.LuceneTestCase.VERBOSE;
	import static Lucene.Net.Util.LuceneTestCase.assumeFalse;
	import static Lucene.Net.Util.LuceneTestCase.localeForName;
	import static Lucene.Net.Util.LuceneTestCase.random;
	import static Lucene.Net.Util.LuceneTestCase.randomLocale;
	import static Lucene.Net.Util.LuceneTestCase.randomTimeZone;*/

    using Codec = Lucene.Net.Codecs.Codec;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;

    //using CheapBastardCodec = Lucene.Net.Codecs.cheapbastard.CheapBastardCodec;
    //using MockRandomPostingsFormat = Lucene.Net.Codecs.mockrandom.MockRandomPostingsFormat;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;

    //using SimpleTextCodec = Lucene.Net.Codecs.simpletext.SimpleTextCodec;
    using RandomCodec = Lucene.Net.Index.RandomCodec;
    using RandomSimilarityProvider = Lucene.Net.Search.RandomSimilarityProvider;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    //using RandomizedContext = com.carrotsearch.randomizedtesting.RandomizedContext;

    /// <summary>
    /// Setup and restore suite-level environment (fine grained junk that
    /// doesn't fit anywhere else).
    /// </summary>
    internal sealed class TestRuleSetupAndRestoreClassEnv// : AbstractBeforeAfterRule
    {
        /// <summary>
        /// Restore these system property values.
        /// </summary>
        private Dictionary<string, string> RestoreProperties = new Dictionary<string, string>();

        private Codec SavedCodec;
        private CultureInfo SavedLocale;
        private InfoStream SavedInfoStream;

        internal CultureInfo Locale;
        internal Similarity Similarity;
        internal Codec Codec;

        /// <seealso cref= SuppressCodecs </seealso>
        internal HashSet<string> AvoidCodecs;

        public TestRuleSetupAndRestoreClassEnv()
        {
            /*// if verbose: print some debugging stuff about which codecs are loaded.
            if (LuceneTestCase.VERBOSE)
            {
                ISet<string> codecs = Codec.AvailableCodecs();
                foreach (string codec in codecs)
                {
                    Console.WriteLine("Loaded codec: '" + codec + "': " + Codec.ForName(codec).GetType().Name);
                }

                ISet<string> postingsFormats = PostingsFormat.AvailablePostingsFormats();
                foreach (string postingsFormat in postingsFormats)
                {
                    Console.WriteLine("Loaded postingsFormat: '" + postingsFormat + "': " + PostingsFormat.ForName(postingsFormat).GetType().Name);
                }
            }

            SavedInfoStream = InfoStream.Default;
            Random random = RandomizedContext.Current.Random;
            bool v = random.NextBoolean();
            if (LuceneTestCase.INFOSTREAM)
            {
                InfoStream.Default = new ThreadNameFixingPrintStreamInfoStream(Console.Out);
            }
            else if (v)
            {
                InfoStream.Default = new NullInfoStream();
            }

            Type targetClass = RandomizedContext.Current.GetTargetType;
            AvoidCodecs = new HashSet<string>();

            // set back to default
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;

            SavedCodec = Codec.Default;
            int randomVal = random.Next(10);
            if ("Lucene3x".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 3 && !ShouldAvoidCodec("Lucene3x"))) // preflex-only setup
            {
                Codec = Codec.ForName("Lucene3x");
                Debug.Assert((Codec is PreFlexRWCodec), "fix your classpath to have tests-framework.jar before lucene-core.jar");
                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            }
            else if ("Lucene40".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && randomVal == 0 && !ShouldAvoidCodec("Lucene40"))) // 4.0 setup
            {
                Codec = Codec.ForName("Lucene40");
                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
                Debug.Assert(Codec is Lucene40RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
                Debug.Assert((PostingsFormat.ForName("Lucene40") is Lucene40RWPostingsFormat), "fix your classpath to have tests-framework.jar before lucene-core.jar");
            }
            else if ("Lucene41".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 1 && !ShouldAvoidCodec("Lucene41")))
            {
                Codec = Codec.ForName("Lucene41");
                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
                Debug.Assert(Codec is Lucene41RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
            }
            else if ("Lucene42".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 2 && !ShouldAvoidCodec("Lucene42")))
            {
                Codec = Codec.ForName("Lucene42");
                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
                Debug.Assert(Codec is Lucene42RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
            }
            else if ("Lucene45".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 5 && !ShouldAvoidCodec("Lucene45")))
            {
                Codec = Codec.ForName("Lucene45");
                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
                Debug.Assert(Codec is Lucene45RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
            }
            else if (("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) == false) || ("random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) == false))
            {
                // the user wired postings or DV: this is messy
                // refactor into RandomCodec....

                PostingsFormat format;
                if ("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
                {
                    format = PostingsFormat.ForName("Lucene41");
                }
                else
                {
                    format = PostingsFormat.ForName(LuceneTestCase.TEST_POSTINGSFORMAT);
                }

                DocValuesFormat dvFormat;
                if ("random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT))
                {
                    dvFormat = DocValuesFormat.ForName("Lucene45");
                }
                else
                {
                    dvFormat = DocValuesFormat.ForName(LuceneTestCase.TEST_DOCVALUESFORMAT);
                }

                Codec = new Lucene46CodecAnonymousInnerClassHelper(this, format, dvFormat);
            }
            else if ("Asserting".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && randomVal == 6 && !ShouldAvoidCodec("Asserting")))
            {
                Codec = new AssertingCodec();
            }
            else if ("Compressing".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && randomVal == 5 && !ShouldAvoidCodec("Compressing")))
            {
                Codec = CompressingCodec.RandomInstance(random);
            }
            else if (!"random".Equals(LuceneTestCase.TEST_CODEC))
            {
                Codec = Codec.ForName(LuceneTestCase.TEST_CODEC);
            }
            else if ("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
            {
                Codec = new RandomCodec(random, AvoidCodecs);
            }
            else
            {
                Debug.Assert(false);
            }
            Codec.Default = Codec;
            */
            Random random = new Random(1);
            Similarity = random.NextBoolean() ? (Similarity)new DefaultSimilarity() : new RandomSimilarityProvider(new Random(1));
            /*
            // Check codec restrictions once at class level.
            try
            {
                CheckCodecRestrictions(Codec);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("NOTE: " + e.Message + " Suppressed codecs: " + Arrays.ToString(AvoidCodecs.ToArray()));
                throw e;
            }*/
        }

        /*~TestRuleSetupAndRestoreClassEnv()
        {
            foreach (KeyValuePair<string, string> e in RestoreProperties)
            {
                if (e.Value == null)
                {
                    System.ClearProperty(e.Key);
                }
                else
                {
                    System.setProperty(e.Key, e.Value);
                }
            }
            RestoreProperties.Clear();

            Codec.Default = SavedCodec;
            InfoStream.Default = SavedInfoStream;
            if (SavedLocale != null)
            {
                Locale = SavedLocale;
            }
            if (SavedTimeZone != null)
            {
                TimeZone = SavedTimeZone;
            }
        }*/

        internal class ThreadNameFixingPrintStreamInfoStream : PrintStreamInfoStream
        {
            public ThreadNameFixingPrintStreamInfoStream(TextWriter @out)
                : base(@out)
            {
            }

            public override void Message(string component, string message)
            {
                if ("TP".Equals(component))
                {
                    return; // ignore test points!
                }
                string name;
                if (Thread.CurrentThread.Name != null && Thread.CurrentThread.Name.StartsWith("TEST-"))
                {
                    // The name of the main thread is way too
                    // long when looking at IW verbose output...
                    name = "main";
                }
                else
                {
                    name = Thread.CurrentThread.Name;
                }
                Stream.WriteLine(component + " " + MessageID + " [" + DateTime.Now + "; " + name + "]: " + message);
            }
        }

        /*protected internal override void Before()
        {
          // enable this by default, for IDE consistency with ant tests (as its the default from ant)
          // TODO: really should be in solr base classes, but some extend LTC directly.
          // we do this in beforeClass, because some tests currently disable it
          RestoreProperties["solr.directoryFactory"] = System.getProperty("solr.directoryFactory");
          if (System.getProperty("solr.directoryFactory") == null)
          {
            System.setProperty("solr.directoryFactory", "org.apache.solr.core.MockDirectoryFactory");
          }

          // Restore more Solr properties.
          RestoreProperties["solr.solr.home"] = System.getProperty("solr.solr.home");
          RestoreProperties["solr.data.dir"] = System.getProperty("solr.data.dir");

          // if verbose: print some debugging stuff about which codecs are loaded.
          if (LuceneTestCase.VERBOSE)
          {
            ISet<string> codecs = Codec.AvailableCodecs();
            foreach (string codec in codecs)
            {
              Console.WriteLine("Loaded codec: '" + codec + "': " + Codec.ForName(codec).GetType().Name);
            }

            ISet<string> postingsFormats = PostingsFormat.AvailablePostingsFormats();
            foreach (string postingsFormat in postingsFormats)
            {
              Console.WriteLine("Loaded postingsFormat: '" + postingsFormat + "': " + PostingsFormat.ForName(postingsFormat).GetType().Name);
            }
          }

          SavedInfoStream = InfoStream.Default;
          Random random = RandomizedContext.Current.Random;
          bool v = random.NextBoolean();
          if (LuceneTestCase.INFOSTREAM)
          {
            InfoStream.Default = new ThreadNameFixingPrintStreamInfoStream(Console.Out);
          }
          else if (v)
          {
            InfoStream.Default = new NullInfoStream();
          }

          Type targetClass = RandomizedContext.Current.GetTargetType;
          AvoidCodecs = new HashSet<string>();
          if (targetClass.isAnnotationPresent(typeof(SuppressCodecs)))
          {
            SuppressCodecs a = targetClass.getAnnotation(typeof(SuppressCodecs));
            AvoidCodecs.AddAll(Arrays.AsList(a.Value()));
          }

          // set back to default
          LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;

          SavedCodec = Codec.Default;
          int randomVal = random.Next(10);
          if ("Lucene3x".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 3 && !ShouldAvoidCodec("Lucene3x"))) // preflex-only setup
          {
            Codec = Codec.ForName("Lucene3x");
            Debug.Assert((Codec is PreFlexRWCodec), "fix your classpath to have tests-framework.jar before lucene-core.jar");
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
          }
          else if ("Lucene40".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && randomVal == 0 && !ShouldAvoidCodec("Lucene40"))) // 4.0 setup
          {
            Codec = Codec.ForName("Lucene40");
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            Debug.Assert(Codec is Lucene40RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
            Debug.Assert((PostingsFormat.ForName("Lucene40") is Lucene40RWPostingsFormat), "fix your classpath to have tests-framework.jar before lucene-core.jar");
          }
          else if ("Lucene41".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 1 && !ShouldAvoidCodec("Lucene41")))
          {
            Codec = Codec.ForName("Lucene41");
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            Debug.Assert(Codec is Lucene41RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
          }
          else if ("Lucene42".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 2 && !ShouldAvoidCodec("Lucene42")))
          {
            Codec = Codec.ForName("Lucene42");
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            Debug.Assert(Codec is Lucene42RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
          }
          else if ("Lucene45".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 5 && !ShouldAvoidCodec("Lucene45")))
          {
            Codec = Codec.ForName("Lucene45");
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            Debug.Assert(Codec is Lucene45RWCodec, "fix your classpath to have tests-framework.jar before lucene-core.jar");
          }
          else if (("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) == false) || ("random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) == false))
          {
            // the user wired postings or DV: this is messy
            // refactor into RandomCodec....

            PostingsFormat format;
            if ("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
            {
              format = PostingsFormat.ForName("Lucene41");
            }
            else if ("MockRandom".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
            {
              format = new MockRandomPostingsFormat(new Random(random.Next()));
            }
            else
            {
                format = PostingsFormat.ForName(LuceneTestCase.TEST_POSTINGSFORMAT);
            }

            DocValuesFormat dvFormat;
            if ("random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT))
            {
              dvFormat = DocValuesFormat.ForName("Lucene45");
            }
            else
            {
                dvFormat = DocValuesFormat.ForName(LuceneTestCase.TEST_DOCVALUESFORMAT);
            }

            Codec = new Lucene46CodecAnonymousInnerClassHelper(this, format, dvFormat);
          }
          else if ("SimpleText".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && randomVal == 9 && LuceneTestCase.Rarely(random) && !ShouldAvoidCodec("SimpleText")))
          {
            Codec = new SimpleTextCodec();
          }
          else if ("CheapBastard".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && randomVal == 8 && !ShouldAvoidCodec("CheapBastard") && !ShouldAvoidCodec("Lucene41")))
          {
            // we also avoid this codec if Lucene41 is avoided, since thats the postings format it uses.
            Codec = new CheapBastardCodec();
          }
          else if ("Asserting".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && randomVal == 6 && !ShouldAvoidCodec("Asserting")))
          {
            Codec = new AssertingCodec();
          }
          else if ("Compressing".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase.TEST_CODEC) && randomVal == 5 && !ShouldAvoidCodec("Compressing")))
          {
            Codec = CompressingCodec.RandomInstance(random);
          }
          else if (!"random".Equals(LuceneTestCase.TEST_CODEC))
          {
              Codec = Codec.ForName(LuceneTestCase.TEST_CODEC);
          }
          else if ("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
          {
            Codec = new RandomCodec(random, AvoidCodecs);
          }
          else
          {
            Debug.Assert(false);
          }
          Codec.Default = Codec;

          // Initialize locale/ timezone.
          string testLocale = System.getProperty("tests.locale", "random");
          string testTimeZone = System.getProperty("tests.timezone", "random");

          // Always pick a random one for consistency (whether tests.locale was specified or not).
          SavedLocale = Locale.Default;
          Locale randomLocale = RandomLocale(random);
          Locale = testLocale.Equals("random") ? randomLocale : localeForName(testLocale);
          Locale.Default = Locale;

          // TimeZone.getDefault will set user.timezone to the default timezone of the user's locale.
          // So store the original property value and restore it at end.
          RestoreProperties["user.timezone"] = System.getProperty("user.timezone");
          SavedTimeZone = TimeZone.Default;
          TimeZone randomTimeZone = RandomTimeZone(random);
          TimeZone = testTimeZone.Equals("random") ? randomTimeZone : TimeZone.getTimeZone(testTimeZone);
          TimeZone.Default = TimeZone;
          Similarity = random.NextBoolean() ? (Similarity)new DefaultSimilarity() : new RandomSimilarityProvider(new Random());

          // Check codec restrictions once at class level.
          try
          {
            CheckCodecRestrictions(Codec);
          }
          catch (Exception e)
          {
            Console.Error.WriteLine("NOTE: " + e.Message + " Suppressed codecs: " + Arrays.ToString(AvoidCodecs.ToArray()));
            throw e;
          }
        }*/

        private class Lucene46CodecAnonymousInnerClassHelper : Lucene46Codec
        {
            private readonly TestRuleSetupAndRestoreClassEnv OuterInstance;

            private PostingsFormat Format;
            private DocValuesFormat DvFormat;

            public Lucene46CodecAnonymousInnerClassHelper(TestRuleSetupAndRestoreClassEnv outerInstance, PostingsFormat format, DocValuesFormat dvFormat)
            {
                this.OuterInstance = outerInstance;
                this.Format = format;
                this.DvFormat = dvFormat;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return Format;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return DvFormat;
            }

            public override string ToString()
            {
                return base.ToString() + ": " + Format.ToString() + ", " + DvFormat.ToString();
            }
        }

        /// <summary>
        /// Check codec restrictions.
        /// </summary>
        /// <exception cref="AssumptionViolatedException"> if the class does not work with a given codec. </exception>
        private void CheckCodecRestrictions(Codec codec)
        {
            LuceneTestCase.AssumeFalse("Class not allowed to use codec: " + codec.Name + ".", ShouldAvoidCodec(codec.Name));

            if (codec is RandomCodec && AvoidCodecs.Count > 0)
            {
                foreach (string name in ((RandomCodec)codec).FormatNames)
                {
                    LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + name + ".", ShouldAvoidCodec(name));
                }
            }

            PostingsFormat pf = codec.PostingsFormat();
            LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + pf.Name + ".", ShouldAvoidCodec(pf.Name));

            LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + LuceneTestCase.TEST_POSTINGSFORMAT + ".", ShouldAvoidCodec(LuceneTestCase.TEST_POSTINGSFORMAT));
        }

        /// <summary>
        /// After suite cleanup (always invoked).
        /// </summary>
        /*protected internal override void After()
        {
          foreach (KeyValuePair<string, string> e in RestoreProperties)
          {
            if (e.Value == null)
            {
              System.ClearProperty(e.Key);
            }
            else
            {
              System.setProperty(e.Key, e.Value);
            }
          }
          RestoreProperties.Clear();

          Codec.Default = SavedCodec;
          InfoStream.Default = SavedInfoStream;
          if (SavedLocale != null)
          {
              Locale = SavedLocale;
          }
          if (SavedTimeZone != null)
          {
              TimeZone = SavedTimeZone;
          }
        }*/

        /// <summary>
        /// Should a given codec be avoided for the currently executing suite?
        /// </summary>
        private bool ShouldAvoidCodec(string codec)
        {
            return AvoidCodecs.Count > 0 && AvoidCodecs.Contains(codec);
        }
    }
}