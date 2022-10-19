using J2N;
using J2N.IO;
using J2N.Text;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.PerField;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support.IO;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Directory = Lucene.Net.Store.Directory;
using JCG = J2N.Collections.Generic;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;

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
    /// General utility methods for Lucene unit tests.
    /// </summary>
    public static class TestUtil // LUCENENET specific - made static rather than making a private constructor
    {
        /// <summary>
        /// Deletes one or more files or directories (and everything underneath it).
        /// </summary>
        /// <exception cref="IOException">If any of the given files (or their subhierarchy files in case
        /// of directories) cannot be removed.</exception>
        public static void Rm(params FileSystemInfo[] locations)
        {
            ISet<FileSystemInfo> unremoved = Rm(new JCG.HashSet<FileSystemInfo>(), locations);
            if (unremoved.Count > 0)
            {
                StringBuilder b = new StringBuilder("Could not remove the following files (in the order of attempts):\n");
                foreach (var f in unremoved)
                {
                    b.Append("   ")
                     .Append(f.FullName)
                     .Append('\n');
                }
                throw new IOException(b.ToString());
            }
        }

        private static ISet<FileSystemInfo> Rm(ISet<FileSystemInfo> unremoved, params FileSystemInfo[] locations)
        {
            foreach (FileSystemInfo location in locations)
            {
                // LUCENENET: Refresh the state of the FileSystemInfo object so we can be sure
                // the Exists property is (somewhat) current.
                location.Refresh();

                if (location.Exists)
                {
                    if (location is DirectoryInfo directory)
                    {
                        // Try to delete all of the files and folders in the directory
                        Rm(unremoved, directory.GetFileSystemInfos());
                    }

                    try
                    {
                        location.Delete();
                    }
                    catch (Exception)
                    {
                        unremoved.Add(location);
                    }
                }
            }
            return unremoved;
        }

        /// <summary>
        /// Convenience method unzipping <paramref name="zipFileStream"/> into <paramref name="destDir"/>, cleaning up
        /// <paramref name="destDir"/> first. 
        /// </summary>
        public static void Unzip(Stream zipFileStream, DirectoryInfo destDir)
        {
            Rm(destDir);
            destDir.Create();

            using ZipArchive zip = new ZipArchive(zipFileStream);
            foreach (var entry in zip.Entries)
            {
                // Ignore internal folders - these are tacked onto the FullName anyway
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                {
                    continue;
                }
                using Stream input = entry.Open();
                FileInfo targetFile = new FileInfo(CorrectPath(Path.Combine(destDir.FullName, entry.FullName)));
                if (!targetFile.Directory.Exists)
                {
                    targetFile.Directory.Create();
                }

                using Stream output = new FileStream(targetFile.FullName, FileMode.Create, FileAccess.Write);
                input.CopyTo(output);
            }
        }

        /// <summary>
        /// LUCENENET specific method for normalizing file path names
        /// for the current operating system.
        /// </summary>
        private static string CorrectPath(string input)
        {
            if (Path.DirectorySeparatorChar.Equals('/'))
            {
                return input.Replace('\\', '/');
            }
            return input.Replace('/', '\\');
        }

        public static void SyncConcurrentMerges(IndexWriter writer)
        {
            SyncConcurrentMerges(writer.Config.MergeScheduler);
        }

        public static void SyncConcurrentMerges(IMergeScheduler ms)
        {
            if (ms is IConcurrentMergeScheduler concurrentMergeScheduler)
            {
                concurrentMergeScheduler.Sync();
            }
        }

        /// <summary>
        /// This runs the <see cref="Index.CheckIndex"/> tool on the index in.  If any
        /// issues are hit, an <see cref="Exception"/> is thrown; else,
        /// true is returned.
        /// </summary>
        public static CheckIndex.Status CheckIndex(Directory dir)
        {
            return CheckIndex(dir, true);
        }

        public static CheckIndex.Status CheckIndex(Directory dir, bool crossCheckTermVectors)
        {
            ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
            CheckIndex checker = new CheckIndex(dir);
            checker.CrossCheckTermVectors = crossCheckTermVectors;
            checker.InfoStream = new StreamWriter(bos, Encoding.UTF8);
            CheckIndex.Status indexStatus = checker.DoCheckIndex(null);
            if (indexStatus is null || indexStatus.Clean == false)
            {
                Console.WriteLine("CheckIndex failed");
                checker.FlushInfoStream();
                Console.WriteLine(bos.ToString());
                throw RuntimeException.Create("CheckIndex failed");
            }
            else
            {
                if (LuceneTestCase.UseInfoStream)
                {
                    checker.FlushInfoStream(); 
                    Console.WriteLine(bos.ToString());
                }
                return indexStatus;
            }
        }

        /// <summary>
        /// This runs the <see cref="Index.CheckIndex"/> tool on the <see cref="IndexReader"/>.  If any
        /// issues are hit, an <see cref="Exception"/> is thrown.
        /// </summary>
        public static void CheckReader(IndexReader reader)
        {
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                CheckReader(context.AtomicReader, true);
            }
        }

        public static void CheckReader(AtomicReader reader, bool crossCheckTermVectors)
        {
            ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
            StreamWriter infoStream = new StreamWriter(bos, Encoding.UTF8);

            reader.CheckIntegrity();
            CheckIndex.Status.FieldNormStatus fieldNormStatus = Index.CheckIndex.TestFieldNorms(reader, infoStream);
            CheckIndex.Status.TermIndexStatus termIndexStatus = Index.CheckIndex.TestPostings(reader, infoStream);
            CheckIndex.Status.StoredFieldStatus storedFieldStatus = Index.CheckIndex.TestStoredFields(reader, infoStream);
            CheckIndex.Status.TermVectorStatus termVectorStatus = Index.CheckIndex.TestTermVectors(reader, infoStream, false, crossCheckTermVectors);
            CheckIndex.Status.DocValuesStatus docValuesStatus = Index.CheckIndex.TestDocValues(reader, infoStream);

            if (fieldNormStatus.Error != null || termIndexStatus.Error != null || storedFieldStatus.Error != null || termVectorStatus.Error != null || docValuesStatus.Error != null)
            {
                Console.WriteLine("CheckReader failed");
                infoStream.Flush();
                Console.WriteLine(bos.ToString());
                throw RuntimeException.Create("CheckReader failed");
            }
            else
            {
                if (LuceneTestCase.UseInfoStream)
                {
                    Console.WriteLine(bos.ToString());
                }
            }
        }

        /// <summary>
        /// Returns a random <see cref="int"/> from <paramref name="minValue"/> (inclusive) to <paramref name="maxValue"/> (inclusive).
        /// </summary>
        /// <param name="random">A <see cref="Random"/> instance.</param>
        /// <param name="minValue">The inclusive start of the range.</param>
        /// <param name="maxValue">The inclusive end of the range.</param>
        /// <returns>A random <see cref="int"/> from <paramref name="minValue"/> (inclusive) to <paramref name="maxValue"/> (inclusive).</returns>
        /// <exception cref="ArgumentException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextInt32(Random random, int minValue, int maxValue)
        {
            return RandomInts.RandomInt32Between(random, minValue, maxValue); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random <see cref="long"/> from <paramref name="minValue"/> to <paramref name="maxValue"/> (inclusive).
        /// </summary>
        /// <param name="random">A <see cref="Random"/> instance.</param>
        /// <param name="minValue">The inclusive start of the range.</param>
        /// <param name="maxValue">The inclusive end of the range.</param>
        /// <returns>A random <see cref="long"/> from <paramref name="minValue"/> to <paramref name="maxValue"/> (inclusive).</returns>
        /// <exception cref="ArgumentException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NextInt64(Random random, long minValue, long maxValue)
        {
            return RandomInts.RandomInt64Between(random, minValue, maxValue); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z'. May be an empty string.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="maxLength">The maximum length of the string to return (inclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomSimpleString(Random random, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextSimpleString(random, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z'. May be an empty string.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="minLength">The minimum length of the string to return (inclusive).</param>
        /// <param name="maxLength">The maximum length of the string to return (inclusive).</param>
        /// <exception cref="ArgumentException"><paramref name="minLength"/> is greater than <paramref name="maxLength"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> or <paramref name="maxLength"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomSimpleString(Random random, int minLength, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextSimpleString(random, minLength, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random string consisting only of characters between <paramref name="minChar"/> (inclusive)
        /// and <paramref name="maxChar"/> (inclusive).
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="minChar">The minimum <see cref="char"/> value of the range (inclusive).</param>
        /// <param name="maxChar">The maximum <see cref="char"/> value of the range (inclusive).</param>
        /// <param name="maxLength">The maximum length of the string to generate.</param>
        /// <returns>a random string consisting only of characters between <paramref name="minChar"/> (inclusive)
        /// and <paramref name="maxChar"/> (inclusive).</returns>
        /// <exception cref="ArgumentException"><paramref name="minChar"/> is greater than <paramref name="maxChar"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minChar"/> or <paramref name="maxChar"/> is not in
        /// the range between <see cref="char.MinValue"/> and <see cref="char.MaxValue"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="maxLength"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomSimpleStringRange(Random random, char minChar, char maxChar, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextSimpleStringRange(random, minChar, maxChar, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z',
        /// between 0 and 10 characters in length.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomSimpleString(Random random)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextSimpleString(random); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns random string with up to 20 characters, including full unicode range.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomUnicodeString(Random random)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextUnicodeString(random); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random string up to a certain length.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="maxLength">The maximum length of the string to return.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomUnicodeString(Random random, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextUnicodeString(random, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Fills provided <see cref="T:char[]"/> with valid random unicode code
        /// unit sequence.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="chars">A <see cref="T:char[]"/> with preallocated space to put the characters.</param>
        /// <param name="startIndex">The index of <paramref name="chars"/> to begin populating with characters.</param>
        /// <param name="length">The number of characters to populate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> or <paramref name="length"/> is less than 0.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> + <paramref name="length"/> refers to a position outside of the range of <paramref name="chars"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="chars"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RandomFixedLengthUnicodeString(Random random, char[] chars, int startIndex, int length)
        {
            RandomizedTesting.Generators.RandomExtensions.NextFixedLengthUnicodeString(random, chars, startIndex, length); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a <see cref="string"/> thats "regexish" (contains lots of operators typically found in regular expressions)
        /// If you call this enough times, you might get a valid regex!
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomRegexishString(Random random) // LUCENENET: Renamed from regexpish to make the name .NET-like
        {
            return RandomizedTesting.Generators.RandomExtensions.NextRegexishString(random); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a <see cref="string"/> thats "regexish" (contains lots of operators typically found in regular expressions)
        /// If you call this enough times, you might get a valid regex!
        ///
        /// <para/>Note: to avoid practically endless backtracking patterns we replace asterisk and plus
        /// operators with bounded repetitions. See LUCENE-4111 for more info.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="maxLength"> A hint about maximum length of the regexpish string. It may be exceeded by a few characters. </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomRegexpishString(Random random, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextRegexishString(random, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns a random HTML-like string.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="numElements">The maximum number of HTML elements to include in the string.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="numElements"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomHtmlishString(Random random, int numElements)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextHtmlishString(random, numElements); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Randomly upcases, downcases, or leaves intact each code point in the given string in the current culture.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="value">The string to recase randomly.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="value"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomlyRecaseString(Random random, string value)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextStringRecasing(random, value); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Randomly upcases, downcases, or leaves intact each code point in the given string in the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="value">The string to recase randomly.</param>
        /// <param name="culture">The culture to use when recasing the string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/>, <paramref name="value"/> or <paramref name="culture"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomlyRecaseString(Random random, string value, CultureInfo culture) // LUCENENET specific - allow a way to pass culture
        {
            return RandomizedTesting.Generators.RandomExtensions.NextStringRecasing(random, value, culture); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns random string of length between 0-20 codepoints, all codepoints within the same unicode block.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomRealisticUnicodeString(Random random)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextRealisticUnicodeString(random); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns random string of length up to maxLength codepoints, all codepoints within the same unicode block.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="maxLength">The maximum length of the string to return (inclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomRealisticUnicodeString(Random random, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextRealisticUnicodeString(random, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns random string of length between min and max codepoints, all codepoints within the same unicode block.
        /// </summary>
        /// <param name="random">This <see cref="Random"/>.</param>
        /// <param name="minLength">The minimum length of the string to return (inclusive).</param>
        /// <param name="maxLength">The maximum length of the string to return (inclusive).</param>
        /// <exception cref="ArgumentException"><paramref name="minLength"/> is greater than <paramref name="maxLength"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> or <paramref name="maxLength"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomRealisticUnicodeString(Random random, int minLength, int maxLength)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextRealisticUnicodeString(random, minLength, maxLength); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Returns random string, with a given UTF-8 byte <paramref name="length"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RandomFixedByteLengthUnicodeString(Random random, int length)
        {
            return RandomizedTesting.Generators.RandomExtensions.NextFixedByteLengthUnicodeString(random, length); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        /// <summary>
        /// Return a <see cref="Codec"/> that can read any of the
        /// default codecs and formats, but always writes in the specified
        /// format.
        /// </summary>
        public static Codec AlwaysPostingsFormat(PostingsFormat format)
        {
            // TODO: we really need for postings impls etc to announce themselves
            // (and maybe their params, too) to infostream on flush and merge.
            // otherwise in a real debugging situation we won't know whats going on!
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("forcing postings format to:" + format);
            }
            return new Lucene46CodecAnonymousClass(format);
        }

        private sealed class Lucene46CodecAnonymousClass : Lucene46Codec
        {
            private readonly PostingsFormat format;

            public Lucene46CodecAnonymousClass(PostingsFormat format)
            {
                this.format = format;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return format;
            }
        }

        /// <summary>
        /// Return a <see cref="Codec"/> that can read any of the
        /// default codecs and formats, but always writes in the specified
        /// format.
        /// </summary>
        public static Codec AlwaysDocValuesFormat(DocValuesFormat format)
        {
            // TODO: we really need for docvalues impls etc to announce themselves
            // (and maybe their params, too) to infostream on flush and merge.
            // otherwise in a real debugging situation we won't know whats going on!
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("forcing docvalues format to:" + format);
            }
            return new Lucene46CodecAnonymousClass2(format);
        }

        private sealed class Lucene46CodecAnonymousClass2 : Lucene46Codec
        {
            private readonly DocValuesFormat format;

            public Lucene46CodecAnonymousClass2(DocValuesFormat format)
            {
                this.format = format;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return format;
            }
        }

        // TODO: generalize all 'test-checks-for-crazy-codecs' to
        // annotations (LUCENE-3489)
        public static string GetPostingsFormat(string field)
        {
            return GetPostingsFormat(Codec.Default, field);
        }

        public static string GetPostingsFormat(Codec codec, string field)
        {
            PostingsFormat p = codec.PostingsFormat;
            if (p is PerFieldPostingsFormat perFieldPostingsFormat)
            {
                return perFieldPostingsFormat.GetPostingsFormatForField(field).Name;
            }
            else
            {
                return p.Name;
            }
        }

        public static string GetDocValuesFormat(string field)
        {
            return GetDocValuesFormat(Codec.Default, field);
        }

        public static string GetDocValuesFormat(Codec codec, string field)
        {
            DocValuesFormat f = codec.DocValuesFormat;
            if (f is PerFieldDocValuesFormat perFieldDocValuesFormat)
            {
                return perFieldDocValuesFormat.GetDocValuesFormatForField(field).Name;
            }
            else
            {
                return f.Name;
            }
        }

        // TODO: remove this, push this test to Lucene40/Lucene42 codec tests
        public static bool FieldSupportsHugeBinaryDocValues(string field)
        {
            string dvFormat = GetDocValuesFormat(field);
            if (dvFormat.Equals("Lucene40", StringComparison.Ordinal) 
                || dvFormat.Equals("Lucene42", StringComparison.Ordinal) 
                || dvFormat.Equals("Memory", StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }

        public static bool AnyFilesExceptWriteLock(Directory dir)
        {
            string[] files = dir.ListAll();
            if (files.Length > 1 || (files.Length == 1 && !files[0].Equals("write.lock", StringComparison.Ordinal)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Just tries to configure things to keep the open file
        /// count lowish.
        /// </summary>
        public static void ReduceOpenFiles(IndexWriter w)
        {
            // keep number of open files lowish
            MergePolicy mp = w.Config.MergePolicy;
            if (mp is LogMergePolicy lmp)
            {
                lmp.MergeFactor = Math.Min(5, lmp.MergeFactor);
                lmp.NoCFSRatio = 1.0;
            }
            else if (mp is TieredMergePolicy tmp)
            {
                tmp.MaxMergeAtOnce = Math.Min(5, tmp.MaxMergeAtOnce);
                tmp.SegmentsPerTier = Math.Min(5, tmp.SegmentsPerTier);
                tmp.NoCFSRatio = 1.0;
            }
            IMergeScheduler ms = w.Config.MergeScheduler;
            if (ms is IConcurrentMergeScheduler concurrentMergeScheduler)
            {
                // wtf... shouldnt it be even lower since its 1 by default?!?!
                concurrentMergeScheduler.SetMaxMergesAndThreads(3, 2);
            }
        }

        /// <summary>
        /// Checks some basic behaviour of an <see cref="Attribute"/>. </summary>
        /// <param name="att"><see cref="Attribute"/> to reflect</param>
        /// <param name="reflectedValues"> Contains a <see cref="IDictionary{String, Object}"/> with "AttributeSubclassType/key" as values.</param>
        public static void AssertAttributeReflection(Attribute att, IDictionary<string, object> reflectedValues)
        {
            IDictionary<string, object> map = new JCG.Dictionary<string, object>();
            att.ReflectWith(new AttributeReflectorAnonymousClass(map));
            Assert.AreEqual(reflectedValues, map, aggressive: false, "Reflection does not produce same map");
        }

        private sealed class AttributeReflectorAnonymousClass : IAttributeReflector
        {
            private readonly IDictionary<string, object> map;

            public AttributeReflectorAnonymousClass(IDictionary<string, object> map)
            {
                this.map = map;
            }

            public void Reflect<T>(string key, object value)
                where T : IAttribute
            {
                Reflect(typeof(T), key, value);
            }

            public void Reflect(Type attClass, string key, object value)
            {
                map[attClass.Name + '#' + key] = value;
            }
        }

        public static void AssertEquals(TopDocs expected, TopDocs actual)
        {
            Assert.AreEqual(expected.TotalHits, actual.TotalHits, "wrong total hits");
            Assert.AreEqual(expected.MaxScore, actual.MaxScore, "wrong maxScore");
            Assert.AreEqual(expected.ScoreDocs.Length, actual.ScoreDocs.Length, "wrong hit count");
            for (int hitIDX = 0; hitIDX < expected.ScoreDocs.Length; hitIDX++)
            {
                ScoreDoc expectedSD = expected.ScoreDocs[hitIDX];
                ScoreDoc actualSD = actual.ScoreDocs[hitIDX];
                Assert.AreEqual(expectedSD.Doc, actualSD.Doc, "wrong hit docID");
                Assert.AreEqual(expectedSD.Score, actualSD.Score, "wrong hit score");
                if (expectedSD is FieldDoc expectedFieldDoc)
                {
                    Assert.IsTrue(actualSD is FieldDoc);
                    Assert.AreEqual(expectedFieldDoc.Fields, ((FieldDoc)actualSD).Fields, "wrong sort field values");
                }
                else
                {
                    Assert.IsFalse(actualSD is FieldDoc);
                }
            }
        }

        // NOTE: this is likely buggy, and cannot clone fields
        // with tokenStreamValues, etc.  Use at your own risk!!

        // TODO: is there a pre-existing way to do this!!!
        public static Document CloneDocument(Document doc1)
        {
            Document doc2 = new Document();
            foreach (IIndexableField f in doc1.Fields)
            {
                Field field1 = (Field)f;
                Field field2;
                DocValuesType dvType = field1.FieldType.DocValueType;
                NumericType numType = field1.FieldType.NumericType;
                if (dvType != DocValuesType.NONE)
                {
                    switch (dvType)
                    {
                        case DocValuesType.NUMERIC:
                            field2 = new NumericDocValuesField(field1.Name, field1.GetInt64Value().Value);
                            break;

                        case DocValuesType.BINARY:
                            field2 = new BinaryDocValuesField(field1.Name, field1.GetBinaryValue());
                            break;

                        case DocValuesType.SORTED:
                            field2 = new SortedDocValuesField(field1.Name, field1.GetBinaryValue());
                            break;

                        default:
                            throw IllegalStateException.Create("unknown Type: " + dvType);
                    }
                }
                else if (numType != NumericType.NONE)
                {
                    switch (numType)
                    {
                        case NumericType.INT32:
                            field2 = new Int32Field(field1.Name, field1.GetInt32Value().Value, field1.FieldType);
                            break;

                        case NumericType.SINGLE:
                            field2 = new SingleField(field1.Name, field1.GetInt32Value().Value, field1.FieldType);
                            break;

                        case NumericType.INT64:
                            field2 = new Int64Field(field1.Name, field1.GetInt32Value().Value, field1.FieldType);
                            break;

                        case NumericType.DOUBLE:
                            field2 = new DoubleField(field1.Name, field1.GetInt32Value().Value, field1.FieldType);
                            break;

                        default:
                            throw IllegalStateException.Create("unknown Type: " + numType);
                    }
                }
                else
                {
                    field2 = new Field(field1.Name, field1.GetStringValue(), field1.FieldType);
                }
                doc2.Add(field2);
            }

            return doc2;
        }

        /// <summary>
        /// Returns a <see cref="DocsEnum"/>, but randomly sometimes uses a
        /// <see cref="MultiDocsEnum"/>, <see cref="DocsAndPositionsEnum"/>.  Returns null
        /// if field/term doesn't exist.
        /// </summary>
        public static DocsEnum Docs(Random random, IndexReader r, string field, BytesRef term, IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            Terms terms = MultiFields.GetTerms(r, field);
            if (terms is null)
            {
                return null;
            }
            TermsEnum termsEnum = terms.GetEnumerator();
            if (!termsEnum.SeekExact(term))
            {
                return null;
            }
            return Docs(random, termsEnum, liveDocs, reuse, flags);
        }

        /// <summary>
        /// Returns a <see cref="DocsEnum"/> from a positioned <see cref="TermsEnum"/>, but
        /// randomly sometimes uses a <see cref="MultiDocsEnum"/>, <see cref="DocsAndPositionsEnum"/>.
        /// </summary>
        public static DocsEnum Docs(Random random, TermsEnum termsEnum, IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            if (random.NextBoolean())
            {
                if (random.NextBoolean())
                {
                    DocsAndPositionsFlags posFlags;
                    switch (random.Next(4))
                    {
                        case 0:
                            posFlags = 0;
                            break;

                        case 1:
                            posFlags = DocsAndPositionsFlags.OFFSETS;
                            break;

                        case 2:
                            posFlags = DocsAndPositionsFlags.PAYLOADS;
                            break;

                        default:
                            posFlags = DocsAndPositionsFlags.OFFSETS | DocsAndPositionsFlags.PAYLOADS;
                            break;
                    }
                    // TODO: cast to DocsAndPositionsEnum?
                    DocsAndPositionsEnum docsAndPositions = termsEnum.DocsAndPositions(liveDocs, null, posFlags);
                    if (docsAndPositions != null)
                    {
                        return docsAndPositions;
                    }
                }
                flags |= DocsFlags.FREQS;
            }
            return termsEnum.Docs(liveDocs, reuse, flags);
        }

        public static ICharSequence StringToCharSequence(string @string, Random random)
        {
            return BytesToCharSequence(new BytesRef(@string), random);
        }

        public static ICharSequence BytesToCharSequence(BytesRef @ref, Random random)
        {
            switch (random.Next(5))
            {
                case 4:
                    CharsRef chars = new CharsRef(@ref.Length);
                    UnicodeUtil.UTF8toUTF16(@ref.Bytes, @ref.Offset, @ref.Length, chars);
                    return chars;
                case 3:
                    return CharBuffer.Wrap(@ref.Utf8ToString());
                default:
                    return new StringCharSequence(@ref.Utf8ToString());
            }
        }

        ///// <summary>
        ///// Shutdown <see cref="TaskScheduler"/> and wait for its.
        ///// </summary>
        //public static void ShutdownExecutorService(TaskScheduler ex) // LUCENENET: TaskScheduler doesn't have a way to terminate in .NET
        //{
        //    /*if (ex != null)
        //    {
        //      try
        //      {
        //        ex.shutdown();
        //        ex.awaitTermination(1, TimeUnit.SECONDS);
        //      }
        //      catch (Exception e) when (e.IsInterruptedException())
        //      {
        //        // Just report it on the syserr.
        //        Console.Error.WriteLine("Could not properly shutdown executor service.");
        //        Console.Error.WriteLine(e.StackTrace);
        //      }
        //    }*/
        //}

        /// <summary>
        /// Returns a valid (compiling) <see cref="Regex"/> instance with random stuff inside. Be careful
        /// when applying random patterns to longer strings as certain types of patterns
        /// may explode into exponential times in backtracking implementations (such as Java's).
        /// </summary>        
        public static Regex RandomRegex(Random random) // LUCENENET specific - renamed from RandomPattern()
        {
            return RandomizedTesting.Generators.RandomExtensions.NextRegex(random); // LUCENENET: Moved general random data generation to RandomizedTesting.Generators
        }

        public static FilteredQuery.FilterStrategy RandomFilterStrategy(Random random)
        {
            switch (random.Next(6))
            {
                case 5:
                case 4:
                    return new RandomAccessFilterStrategyAnonymousClass();

                case 3:
                    return FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY;

                case 2:
                    return FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY;

                case 1:
                    return FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY;

                case 0:
                    return FilteredQuery.QUERY_FIRST_FILTER_STRATEGY;

                default:
                    return FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY;
            }
        }

        private sealed class RandomAccessFilterStrategyAnonymousClass : FilteredQuery.RandomAccessFilterStrategy
        {
            protected override bool UseRandomAccess(IBits bits, int firstFilterDoc)
            {
                return LuceneTestCase.Random.NextBoolean();
            }
        }

        /// <summary>
        /// Returns a random string in the specified length range consisting
        /// entirely of whitespace characters.
        /// </summary>
        /// <seealso cref="WHITESPACE_CHARACTERS"/>
        public static string RandomWhitespace(Random random, int minLength, int maxLength)
        {
            int end = NextInt32(random, minLength, maxLength);
            StringBuilder @out = new StringBuilder();
            for (int i = 0; i < end; i++)
            {
                int offset = NextInt32(random, 0, WHITESPACE_CHARACTERS.Length - 1);
                char c = WHITESPACE_CHARACTERS[offset];
                // sanity check
                Assert.IsTrue(char.IsWhiteSpace(c), "Not really whitespace? (@" + offset + "): " + c);
                @out.Append(c);
            }
            return @out.ToString();
        }

        public static string RandomAnalysisString(Random random, int maxLength, bool simple)
        {
            Assert.True(maxLength >= 0);
            maxLength = maxLength == 0 ? 0 : random.Next(maxLength); // LUCENENET: Lucene bug - random.Next(int) in Java must be > 0, and so must J2N.Randomizer. So, just pass through 0 as .NET would.

            // sometimes just a purely random string
            if (random.Next(31) == 0)
            {
                // LUCENENET specific - We need to pass the value from the random class or 0, just like the remainder of the code
                return RandomSubString(random, maxLength, simple);
            }

            // otherwise, try to make it more realistic with 'words' since most tests use MockTokenizer
            // first decide how big the string will really be: 0..n
            int avgWordLength = TestUtil.NextInt32(random, 3, 8);
            StringBuilder sb = new StringBuilder();
            while (sb.Length < maxLength)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                int wordLength = -1;
                while (wordLength < 0)
                {
                    wordLength = (int)(random.NextGaussian() * 3 + avgWordLength);
                }
                wordLength = Math.Min(wordLength, maxLength - sb.Length);
                sb.Append(RandomSubString(random, wordLength, simple));
            }
            return sb.ToString();
        }

        public static string RandomSubString(Random random, int wordLength, bool simple)
        {
            if (wordLength == 0)
            {
                return "";
            }

            int evilness = TestUtil.NextInt32(random, 0, 20);

            StringBuilder sb = new StringBuilder();
            while (sb.Length < wordLength)
            {
                ;
                if (simple)
                {
                    sb.Append(random.NextBoolean() ? TestUtil.RandomSimpleString(random, wordLength) : TestUtil.RandomHtmlishString(random, wordLength));
                }
                else
                {
                    if (evilness < 10)
                    {
                        sb.Append(TestUtil.RandomSimpleString(random, wordLength));
                    }
                    else if (evilness < 15)
                    {
                        Assert.AreEqual(0, sb.Length); // we should always get wordLength back!
                        sb.Append(TestUtil.RandomRealisticUnicodeString(random, wordLength, wordLength));
                    }
                    else if (evilness == 16)
                    {
                        sb.Append(TestUtil.RandomHtmlishString(random, wordLength));
                    }
                    else if (evilness == 17)
                    {
                        // gives a lot of punctuation
                        sb.Append(TestUtil.RandomRegexpishString(random, wordLength));
                    }
                    else
                    {
                        sb.Append(TestUtil.RandomUnicodeString(random, wordLength));
                    }
                }
            }
            if (sb.Length > wordLength)
            {
                sb.Length = wordLength;
                if (char.IsHighSurrogate(sb[wordLength - 1]))
                {
                    sb.Length = wordLength - 1;
                }
            }

            if (random.Next(17) == 0)
            {
                // mix up case
                string mixedUp = TestUtil.RandomlyRecaseString(random, sb.ToString());
                Assert.True(mixedUp.Length == sb.Length, "Lengths are not the same: mixedUp = " + mixedUp + ", length = " + mixedUp.Length + ", sb = " + sb + ", length = " + sb.Length);
                return mixedUp;
            }
            else
            {
                return sb.ToString();
            }
        }

        /// <summary>
        /// List of characters that match <see cref="char.IsWhiteSpace(char)"/>.</summary>
        public static readonly char[] WHITESPACE_CHARACTERS = new char[]
        {
            // :TODO: is this list exhaustive?
            '\u0009',
            '\n',
            '\u000B',
            '\u000C',
            '\r',
            '\u001C',
            '\u001D',
            '\u001E',
            '\u001F',
            '\u0020',
            // '\u0085', faild sanity check?
            '\u1680',
            '\u180E',
            '\u2000',
            '\u2001',
            '\u2002',
            '\u2003',
            '\u2004',
            '\u2005',
            '\u2006',
            '\u2008',
            '\u2009',
            '\u200A',
            '\u2028',
            '\u2029',
            '\u205F',
            '\u3000'
        };
    }
}