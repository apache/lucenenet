using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using Int64 = J2N.Numerics.Int64;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Suggest.Analyzing
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
    /// Suggester that first analyzes the surface form, adds the
    /// analyzed form to a weighted FST, and then does the same
    /// thing at lookup time.  This means lookup is based on the
    /// analyzed form while suggestions are still the surface
    /// form(s).
    /// 
    /// <para>
    /// This can result in powerful suggester functionality.  For
    /// example, if you use an analyzer removing stop words, 
    /// then the partial text "ghost chr..." could see the
    /// suggestion "The Ghost of Christmas Past". Note that
    /// position increments MUST NOT be preserved for this example
    /// to work, so you should call the constructor with 
    /// <see cref="preservePositionIncrements"/> parameter set to 
    /// false
    /// 
    /// </para>
    /// <para>
    /// If SynonymFilter is used to map wifi and wireless network to
    /// hotspot then the partial text "wirele..." could suggest
    /// "wifi router".  Token normalization like stemmers, accent
    /// removal, etc., would allow suggestions to ignore such
    /// variations.
    /// 
    /// </para>
    /// <para>
    /// When two matching suggestions have the same weight, they
    /// are tie-broken by the analyzed form.  If their analyzed
    /// form is the same then the order is undefined.
    /// 
    /// </para>
    /// <para>
    /// There are some limitations:
    /// <list type="number">
    /// 
    ///   <item><description> A lookup from a query like "net" in English won't
    ///        be any different than "net " (ie, user added a
    ///        trailing space) because analyzers don't reflect
    ///        when they've seen a token separator and when they
    ///        haven't.</description></item>
    /// 
    ///   <item><description> If you're using <see cref="Analysis.Core.StopFilter"/>, and the user will
    ///        type "fast apple", but so far all they've typed is
    ///        "fast a", again because the analyzer doesn't convey whether
    ///        it's seen a token separator after the "a",
    ///        <see cref="Analysis.Core.StopFilter"/> will remove that "a" causing
    ///        far more matches than you'd expect.</description></item>
    /// 
    ///   <item><description> Lookups with the empty string return no results
    ///        instead of all results.</description></item>
    /// </list>
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class AnalyzingSuggester : Lookup
    {

        /// <summary>
        /// FST(Weight,Surface):
        /// input is the analyzed form, with a null byte between terms
        /// weights are encoded as costs: (<see cref="int.MaxValue"/> - weight)
        /// surface is the original, unanalyzed form.
        /// </summary>
        private FST<PairOutputs<Int64, BytesRef>.Pair> fst = null;

        /// <summary>
        /// Analyzer that will be used for analyzing suggestions at
        /// index time.
        /// </summary>
        private readonly Analyzer indexAnalyzer;

        /// <summary>
        /// Analyzer that will be used for analyzing suggestions at
        /// query time.
        /// </summary>
        private readonly Analyzer queryAnalyzer;

        /// <summary>
        /// True if exact match suggestions should always be returned first.
        /// </summary>
        private readonly bool exactFirst;

        /// <summary>
        /// True if separator between tokens should be preserved.
        /// </summary>
        private readonly bool preserveSep;

        /// <summary>
        /// Represents the separation between tokens, if
        /// <see cref="SuggesterOptions.PRESERVE_SEP"/> was specified 
        /// </summary>
        private const int SEP_LABEL = '\u001F';

        /// <summary>
        /// Marks end of the analyzed input and start of dedup
        ///  byte. 
        /// </summary>
        private const int END_BYTE = 0x0;

        /// <summary>
        /// Maximum number of dup surface forms (different surface
        ///  forms for the same analyzed form). 
        /// </summary>
        private readonly int maxSurfaceFormsPerAnalyzedForm;

        /// <summary>
        /// Maximum graph paths to index for a single analyzed
        ///  surface form.  This only matters if your analyzer
        ///  makes lots of alternate paths (e.g. contains
        ///  SynonymFilter). 
        /// </summary>
        private readonly int maxGraphExpansions;

        /// <summary>
        /// Highest number of analyzed paths we saw for any single
        ///  input surface form.  For analyzers that never create
        ///  graphs this will always be 1. 
        /// </summary>
        private int maxAnalyzedPathsForOneInput;

        private bool hasPayloads;

        private const int PAYLOAD_SEP = '\u001f';

        /// <summary>
        /// Whether position holes should appear in the automaton. </summary>
        private readonly bool preservePositionIncrements; // LUCENENET: marked readonly

        /// <summary>
        /// Number of entries the lookup was built with </summary>
        private long count = 0;

        /// <summary>
        /// Calls <see cref="AnalyzingSuggester(Analyzer, Analyzer, SuggesterOptions, int, int, bool)">
        /// AnalyzingSuggester(analyzer, analyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true)
        /// </see>
        /// </summary>
        public AnalyzingSuggester(Analyzer analyzer)
            : this(analyzer, analyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true)
        {
        }

        /// <summary>
        /// Calls <see cref="AnalyzingSuggester(Analyzer,Analyzer,SuggesterOptions,int,int,bool)">
        /// AnalyzingSuggester(indexAnalyzer, queryAnalyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true)
        /// </see>
        /// </summary>
        public AnalyzingSuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer)
            : this(indexAnalyzer, queryAnalyzer, SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP, 256, -1, true)
        {
        }

        /// <summary>
        /// Creates a new suggester.
        /// </summary>
        /// <param name="indexAnalyzer"> Analyzer that will be used for
        ///   analyzing suggestions while building the index. </param>
        /// <param name="queryAnalyzer"> Analyzer that will be used for
        ///   analyzing query text during lookup </param>
        /// <param name="options"> see <see cref="SuggesterOptions.EXACT_FIRST"/>, <see cref="SuggesterOptions.PRESERVE_SEP"/> </param>
        /// <param name="maxSurfaceFormsPerAnalyzedForm"> Maximum number of
        ///   surface forms to keep for a single analyzed form.
        ///   When there are too many surface forms we discard the
        ///   lowest weighted ones. </param>
        /// <param name="maxGraphExpansions"> Maximum number of graph paths
        ///   to expand from the analyzed form.  Set this to -1 for
        ///   no limit. </param>
        /// <param name="preservePositionIncrements"> Whether position holes
        ///   should appear in the automata </param>
        public AnalyzingSuggester(Analyzer indexAnalyzer, Analyzer queryAnalyzer, SuggesterOptions options,
            int maxSurfaceFormsPerAnalyzedForm, int maxGraphExpansions, bool preservePositionIncrements)
        {
            this.indexAnalyzer = indexAnalyzer;
            this.queryAnalyzer = queryAnalyzer;
            if ((options & ~(SuggesterOptions.EXACT_FIRST | SuggesterOptions.PRESERVE_SEP)) != 0)
            {
                throw new ArgumentException("options should only contain SuggesterOptions.EXACT_FIRST and SuggesterOptions.PRESERVE_SEP; got " +
                                                   options);
            }
            this.exactFirst = (options & SuggesterOptions.EXACT_FIRST) != 0;
            this.preserveSep = (options & SuggesterOptions.PRESERVE_SEP) != 0;

            // NOTE: this is just an implementation limitation; if
            // somehow this is a problem we could fix it by using
            // more than one byte to disambiguate ... but 256 seems
            // like it should be way more then enough.
            if (maxSurfaceFormsPerAnalyzedForm <= 0 || maxSurfaceFormsPerAnalyzedForm > 256)
            {
                throw new ArgumentException("maxSurfaceFormsPerAnalyzedForm must be > 0 and < 256 (got: " +
                                                   maxSurfaceFormsPerAnalyzedForm + ")");
            }
            this.maxSurfaceFormsPerAnalyzedForm = maxSurfaceFormsPerAnalyzedForm;

            if (maxGraphExpansions < 1 && maxGraphExpansions != -1)
            {
                throw new ArgumentException("maxGraphExpansions must -1 (no limit) or > 0 (got: " +
                                                   maxGraphExpansions + ")");
            }
            this.maxGraphExpansions = maxGraphExpansions;
            this.preservePositionIncrements = preservePositionIncrements;
        }

        /// <summary>
        /// Returns byte size of the underlying FST. </summary>
        public override long GetSizeInBytes()
        {
            return fst is null ? 0 : fst.GetSizeInBytes();
        }

        private static void CopyDestTransitions(State from, State to, IList<Transition> transitions) // LUCENENET: CA1822: Mark members as static
        {
            if (to.Accept)
            {
                from.Accept = true;
            }
            foreach (Transition t in to.GetTransitions())
            {
                transitions.Add(t);
            }
        }

        // Replaces SEP with epsilon or remaps them if
        // we were asked to preserve them:
        private void ReplaceSep(Automaton a)
        {

            State[] states = a.GetNumberedStates();

            // Go in reverse topo sort so we know we only have to
            // make one pass:
            for (int stateNumber = states.Length - 1; stateNumber >= 0; stateNumber--)
            {
                State state = states[stateNumber];
                IList<Transition> newTransitions = new JCG.List<Transition>();
                foreach (Transition t in state.GetTransitions())
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(t.Min == t.Max);
                    if (t.Min == TokenStreamToAutomaton.POS_SEP)
                    {
                        if (preserveSep)
                        {
                            // Remap to SEP_LABEL:
                            newTransitions.Add(new Transition(SEP_LABEL, t.Dest));
                        }
                        else
                        {
                            CopyDestTransitions(state, t.Dest, newTransitions);
                            a.IsDeterministic = false;
                        }
                    }
                    else if (t.Min == TokenStreamToAutomaton.HOLE)
                    {

                        // Just remove the hole: there will then be two
                        // SEP tokens next to each other, which will only
                        // match another hole at search time.  Note that
                        // it will also match an empty-string token ... if
                        // that's somehow a problem we can always map HOLE
                        // to a dedicated byte (and escape it in the
                        // input).
                        CopyDestTransitions(state, t.Dest, newTransitions);
                        a.IsDeterministic = false;
                    }
                    else
                    {
                        newTransitions.Add(t);
                    }
                }
                state.SetTransitions(newTransitions.ToArray());
            }
        }

        /// <summary>
        /// Used by subclass to change the lookup automaton, if
        ///  necessary. 
        /// </summary>
        protected internal virtual Automaton ConvertAutomaton(Automaton a)
        {
            return a;
        }

        internal virtual TokenStreamToAutomaton GetTokenStreamToAutomaton()
        {
            return new TokenStreamToAutomaton { PreservePositionIncrements = preservePositionIncrements };
        }

        private sealed class AnalyzingComparer : IComparer<BytesRef>
        {
            private readonly bool hasPayloads;

            public AnalyzingComparer(bool hasPayloads)
            {
                this.hasPayloads = hasPayloads;
            }

            private readonly ByteArrayDataInput readerA = new ByteArrayDataInput();
            private readonly ByteArrayDataInput readerB = new ByteArrayDataInput();
            private readonly BytesRef scratchA = new BytesRef();
            private readonly BytesRef scratchB = new BytesRef();

            public int Compare(BytesRef a, BytesRef b)
            {

                // First by analyzed form:
                readerA.Reset(a.Bytes, a.Offset, a.Length);
                scratchA.Length = (ushort)readerA.ReadInt16();
                scratchA.Bytes = a.Bytes;
                scratchA.Offset = readerA.Position;

                readerB.Reset(b.Bytes, b.Offset, b.Length);
                scratchB.Bytes = b.Bytes;
                scratchB.Length = (ushort)readerB.ReadInt16();
                scratchB.Offset = readerB.Position;

                int cmp = scratchA.CompareTo(scratchB);
                if (cmp != 0)
                {
                    return cmp;
                }
                readerA.SkipBytes(scratchA.Length);
                readerB.SkipBytes(scratchB.Length);

                // Next by cost:
                long aCost = readerA.ReadInt32();
                long bCost = readerB.ReadInt32();
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(DecodeWeight(aCost) >= 0);
                    Debugging.Assert(DecodeWeight(bCost) >= 0);
                }
                if (aCost < bCost)
                {
                    return -1;
                }
                else if (aCost > bCost)
                {
                    return 1;
                }

                // Finally by surface form:
                if (hasPayloads)
                {
                    scratchA.Length = (ushort)readerA.ReadInt16();
                    scratchB.Length = (ushort)readerB.ReadInt16();
                    scratchA.Offset = readerA.Position;
                    scratchB.Offset = readerB.Position;
                }
                else
                {
                    scratchA.Offset = readerA.Position;
                    scratchB.Offset = readerB.Position;
                    scratchA.Length = a.Length - scratchA.Offset;
                    scratchB.Length = b.Length - scratchB.Offset;
                }

                return scratchA.CompareTo(scratchB);
            }
        }

        public override void Build(IInputEnumerator enumerator)
        {
            // LUCENENET: Added guard clause for null
            if (enumerator is null)
                throw new ArgumentNullException(nameof(enumerator));

            if (enumerator.HasContexts)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            string prefix = this.GetType().Name;
            var directory = OfflineSorter.DefaultTempDir;
            // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
            using var tempInput = FileSupport.CreateTempFileAsStream(prefix, ".input", directory);
            using var tempSorted = FileSupport.CreateTempFileAsStream(prefix, ".sorted", directory);

            hasPayloads = enumerator.HasPayloads;

            var writer = new OfflineSorter.ByteSequencesWriter(tempInput);
            OfflineSorter.ByteSequencesReader reader = null;
            var scratch = new BytesRef();

            TokenStreamToAutomaton ts2a = GetTokenStreamToAutomaton();

            bool success = false;
            count = 0;
            byte[] buffer = new byte[8];
            try
            {
                var output = new ByteArrayDataOutput(buffer);
                BytesRef surfaceForm;

                while (enumerator.MoveNext())
                {
                    surfaceForm = enumerator.Current;
                    ISet<Int32sRef> paths = ToFiniteStrings(surfaceForm, ts2a);

                    maxAnalyzedPathsForOneInput = Math.Max(maxAnalyzedPathsForOneInput, paths.Count);

                    foreach (Int32sRef path in paths)
                    {

                        Util.Fst.Util.ToBytesRef(path, scratch);

                        // length of the analyzed text (FST input)
                        if (scratch.Length > ushort.MaxValue - 2)
                        {
                            throw new ArgumentException("cannot handle analyzed forms > " + (ushort.MaxValue - 2) +
                                                               " in length (got " + scratch.Length + ")");
                        }
                        ushort analyzedLength = (ushort)scratch.Length;

                        // compute the required length:
                        // analyzed sequence + weight (4) + surface + analyzedLength (short)
                        int requiredLength = analyzedLength + 4 + surfaceForm.Length + 2;

                        BytesRef payload;

                        if (hasPayloads)
                        {
                            if (surfaceForm.Length > (ushort.MaxValue - 2))
                            {
                                throw new ArgumentException("cannot handle surface form > " + (ushort.MaxValue - 2) +
                                                            " in length (got " + surfaceForm.Length + ")");
                            }
                            payload = enumerator.Payload;
                            // payload + surfaceLength (short)
                            requiredLength += payload.Length + 2;
                        }
                        else
                        {
                            payload = null;
                        }

                        buffer = ArrayUtil.Grow(buffer, requiredLength);

                        output.Reset(buffer);

                        output.WriteInt16((short)analyzedLength);

                        output.WriteBytes(scratch.Bytes, scratch.Offset, scratch.Length);

                        output.WriteInt32(EncodeWeight(enumerator.Weight));

                        if (hasPayloads)
                        {
                            for (int i = 0; i < surfaceForm.Length; i++)
                            {
                                if (surfaceForm.Bytes[i] == PAYLOAD_SEP)
                                {
                                    throw new ArgumentException(
                                        "surface form cannot contain unit separator character U+001F; this character is reserved");
                                }
                            }
                            output.WriteInt16((short)surfaceForm.Length);
                            output.WriteBytes(surfaceForm.Bytes, surfaceForm.Offset, surfaceForm.Length);
                            output.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                        }
                        else
                        {
                            output.WriteBytes(surfaceForm.Bytes, surfaceForm.Offset, surfaceForm.Length);
                        }

                        if (Debugging.AssertsEnabled) Debugging.Assert(output.Position == requiredLength, "{0} vs {1}", output.Position, requiredLength);

                        writer.Write(buffer, 0, output.Position);
                    }
                    count++;
                }

                tempInput.Position = 0;

                // Sort all input/output pairs (required by FST.Builder):
                (new OfflineSorter(new AnalyzingComparer(hasPayloads))).Sort(tempInput, tempSorted);

                // Free disk space:
                writer.Dispose(); // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.

                reader = new OfflineSorter.ByteSequencesReader(tempSorted);

                var outputs = new PairOutputs<Int64, BytesRef>(PositiveInt32Outputs.Singleton,
                    ByteSequenceOutputs.Singleton);
                var builder = new Builder<PairOutputs<Int64, BytesRef>.Pair>(FST.INPUT_TYPE.BYTE1, outputs);

                // Build FST:
                BytesRef previousAnalyzed = null;
                BytesRef analyzed = new BytesRef();
                BytesRef surface = new BytesRef();
                Int32sRef scratchInts = new Int32sRef();
                var input = new ByteArrayDataInput();

                // Used to remove duplicate surface forms (but we
                // still index the hightest-weight one).  We clear
                // this when we see a new analyzed form, so it cannot
                // grow unbounded (at most 256 entries):
                var seenSurfaceForms = new JCG.HashSet<BytesRef>();

                var dedup = 0;
                while (reader.Read(scratch))
                {
                    input.Reset(scratch.Bytes, scratch.Offset, scratch.Length);
                    ushort analyzedLength = (ushort)input.ReadInt16();
                    analyzed.Grow(analyzedLength + 2);
                    input.ReadBytes(analyzed.Bytes, 0, analyzedLength);
                    analyzed.Length = analyzedLength;

                    long cost = input.ReadInt32();

                    surface.Bytes = scratch.Bytes;
                    if (hasPayloads)
                    {
                        surface.Length = (ushort)input.ReadInt16();
                        surface.Offset = input.Position;
                    }
                    else
                    {
                        surface.Offset = input.Position;
                        surface.Length = scratch.Length - surface.Offset;
                    }

                    if (previousAnalyzed is null)
                    {
                        previousAnalyzed = new BytesRef();
                        previousAnalyzed.CopyBytes(analyzed);
                        seenSurfaceForms.Add(BytesRef.DeepCopyOf(surface));
                    }
                    else if (analyzed.Equals(previousAnalyzed))
                    {
                        dedup++;
                        if (dedup >= maxSurfaceFormsPerAnalyzedForm)
                        {
                            // More than maxSurfaceFormsPerAnalyzedForm
                            // dups: skip the rest:
                            continue;
                        }
                        if (seenSurfaceForms.Contains(surface))
                        {
                            continue;
                        }
                        seenSurfaceForms.Add(BytesRef.DeepCopyOf(surface));
                    }
                    else
                    {
                        dedup = 0;
                        previousAnalyzed.CopyBytes(analyzed);
                        seenSurfaceForms.Clear();
                        seenSurfaceForms.Add(BytesRef.DeepCopyOf(surface));
                    }

                    // TODO: I think we can avoid the extra 2 bytes when
                    // there is no dup (dedup==0), but we'd have to fix
                    // the exactFirst logic ... which would be sort of
                    // hairy because we'd need to special case the two
                    // (dup/not dup)...

                    // NOTE: must be byte 0 so we sort before whatever
                    // is next
                    analyzed.Bytes[analyzed.Offset + analyzed.Length] = 0;
                    analyzed.Bytes[analyzed.Offset + analyzed.Length + 1] = (byte)dedup;
                    analyzed.Length += 2;

                    Util.Fst.Util.ToInt32sRef(analyzed, scratchInts);
                    //System.out.println("ADD: " + scratchInts + " -> " + cost + ": " + surface.utf8ToString());
                    if (!hasPayloads)
                    {
                        builder.Add(scratchInts, outputs.NewPair(cost, BytesRef.DeepCopyOf(surface)));
                    }
                    else
                    {
                        int payloadOffset = input.Position + surface.Length;
                        int payloadLength = scratch.Length - payloadOffset;
                        BytesRef br = new BytesRef(surface.Length + 1 + payloadLength);
                        Arrays.Copy(surface.Bytes, surface.Offset, br.Bytes, 0, surface.Length);
                        br.Bytes[surface.Length] = PAYLOAD_SEP;
                        Arrays.Copy(scratch.Bytes, payloadOffset, br.Bytes, surface.Length + 1, payloadLength);
                        br.Length = br.Bytes.Length;
                        builder.Add(scratchInts, outputs.NewPair(cost, br));
                    }
                }
                fst = builder.Finish();

                //Util.dotToFile(fst, "/tmp/suggest.dot");

                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(reader, writer);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(reader, writer);
                }
                // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
            }
        }

        public override bool Store(DataOutput output)
        {
            output.WriteVInt64(count);
            if (fst is null)
            {
                return false;
            }

            fst.Save(output);
            output.WriteVInt32(maxAnalyzedPathsForOneInput);
            output.WriteByte((byte)(hasPayloads ? 1 : 0));
            return true;
        }

        public override bool Load(DataInput input)
        {
            count = input.ReadVInt64();
            this.fst = new FST<PairOutputs<Int64, BytesRef>.Pair>(input, new PairOutputs<Int64, BytesRef>(PositiveInt32Outputs.Singleton, ByteSequenceOutputs.Singleton));
            maxAnalyzedPathsForOneInput = input.ReadVInt32();
            hasPayloads = input.ReadByte() == 1;
            return true;
        }

        private LookupResult GetLookupResult(long? output1, BytesRef output2, CharsRef spare)
        {
            LookupResult result;
            if (hasPayloads)
            {
                int sepIndex = -1;
                for (int i = 0; i < output2.Length; i++)
                {
                    if (output2.Bytes[output2.Offset + i] == PAYLOAD_SEP)
                    {
                        sepIndex = i;
                        break;
                    }
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(sepIndex != -1);
                spare.Grow(sepIndex);

                int payloadLen = output2.Length - sepIndex - 1;
                UnicodeUtil.UTF8toUTF16(output2.Bytes, output2.Offset, sepIndex, spare);
                BytesRef payload = new BytesRef(payloadLen);
                Arrays.Copy(output2.Bytes, sepIndex + 1, payload.Bytes, 0, payloadLen);
                payload.Length = payloadLen;
                result = new LookupResult(spare.ToString(), DecodeWeight(output1.GetValueOrDefault()), payload);
            }
            else
            {
                spare.Grow(output2.Length);
                UnicodeUtil.UTF8toUTF16(output2, spare);
                result = new LookupResult(spare.ToString(), DecodeWeight(output1.GetValueOrDefault()));
            }

            return result;
        }

        private bool SameSurfaceForm(BytesRef key, BytesRef output2)
        {
            if (hasPayloads)
            {
                // output2 has at least PAYLOAD_SEP byte:
                if (key.Length >= output2.Length)
                {
                    return false;
                }
                for (int i = 0; i < key.Length; i++)
                {
                    if (key.Bytes[key.Offset + i] != output2.Bytes[output2.Offset + i])
                    {
                        return false;
                    }
                }
                return output2.Bytes[output2.Offset + key.Length] == PAYLOAD_SEP;
            }
            else
            {
                return key.BytesEquals(output2);
            }
        }

        public override IList<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(num > 0);

            // LUCENENET: Added guard clause for null
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (onlyMorePopular)
            {
                throw new ArgumentException("this suggester only works with onlyMorePopular=false");
            }
            if (contexts != null)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            if (fst is null)
            {
                return Collections.EmptyList<LookupResult>();
            }

            //System.out.println("lookup key=" + key + " num=" + num);
            for (var i = 0; i < key.Length; i++)
            {
                if (key[i] == 0x1E)
                {
                    throw new ArgumentException(
                        "lookup key cannot contain HOLE character U+001E; this character is reserved");
                }
                if (key[i] == 0x1F)
                {
                    throw new ArgumentException(
                        "lookup key cannot contain unit separator character U+001F; this character is reserved");
                }
            }

            var utf8Key = new BytesRef(key);
            try
            {

                Automaton lookupAutomaton = ToLookupAutomaton(key);

                var spare = new CharsRef();

                //System.out.println("  now intersect exactFirst=" + exactFirst);

                // Intersect automaton w/ suggest wFST and get all
                // prefix starting nodes & their outputs:
                //final PathIntersector intersector = getPathIntersector(lookupAutomaton, fst);

                //System.out.println("  prefixPaths: " + prefixPaths.size());

                FST.BytesReader bytesReader = fst.GetBytesReader();

                var scratchArc = new FST.Arc<PairOutputs<Int64, BytesRef>.Pair>();

                IList<LookupResult> results = new JCG.List<LookupResult>();

                IList<FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair>> prefixPaths =
                    FSTUtil.IntersectPrefixPaths(ConvertAutomaton(lookupAutomaton), fst);

                if (exactFirst)
                {

                    int count = 0;
                    foreach (FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair> path in prefixPaths)
                    {
                        if (fst.FindTargetArc(END_BYTE, path.FstNode, scratchArc, bytesReader) != null)
                        {
                            // This node has END_BYTE arc leaving, meaning it's an
                            // "exact" match:
                            count++;
                        }
                    }

                    // Searcher just to find the single exact only
                    // match, if present:
                    Util.Fst.Util.TopNSearcher<PairOutputs<Int64, BytesRef>.Pair> searcher;
                    searcher = new Util.Fst.Util.TopNSearcher<PairOutputs<Int64, BytesRef>.Pair>(fst, count * maxSurfaceFormsPerAnalyzedForm,
                        count * maxSurfaceFormsPerAnalyzedForm, weightComparer);

                    // NOTE: we could almost get away with only using
                    // the first start node.  The only catch is if
                    // maxSurfaceFormsPerAnalyzedForm had kicked in and
                    // pruned our exact match from one of these nodes
                    // ...:
                    foreach (var path in prefixPaths)
                    {
                        if (fst.FindTargetArc(END_BYTE, path.FstNode, scratchArc, bytesReader) != null)
                        {
                            // This node has END_BYTE arc leaving, meaning it's an
                            // "exact" match:
                            searcher.AddStartPaths(scratchArc, fst.Outputs.Add(path.Output, scratchArc.Output), false,
                                path.Input);
                        }
                    }

                    var completions = searcher.Search();
                    if (Debugging.AssertsEnabled) Debugging.Assert(completions.IsComplete);

                    // NOTE: this is rather inefficient: we enumerate
                    // every matching "exactly the same analyzed form"
                    // path, and then do linear scan to see if one of
                    // these exactly matches the input.  It should be
                    // possible (though hairy) to do something similar
                    // to getByOutput, since the surface form is encoded
                    // into the FST output, so we more efficiently hone
                    // in on the exact surface-form match.  Still, I
                    // suspect very little time is spent in this linear
                    // seach: it's bounded by how many prefix start
                    // nodes we have and the
                    // maxSurfaceFormsPerAnalyzedForm:
                    foreach (var completion in completions)
                    {
                        BytesRef output2 = completion.Output.Output2;
                        if (SameSurfaceForm(utf8Key, output2))
                        {
                            results.Add(GetLookupResult(completion.Output.Output1, output2, spare));
                            break;
                        }
                    }

                    if (results.Count == num)
                    {
                        // That was quick:
                        return results;
                    }
                }

                Util.Fst.Util.TopNSearcher<PairOutputs<Int64, BytesRef>.Pair> searcher2;
                searcher2 = new TopNSearcherAnonymousClass(this, fst, num - results.Count,
                    num * maxAnalyzedPathsForOneInput, weightComparer, utf8Key, results);

                prefixPaths = GetFullPrefixPaths(prefixPaths, lookupAutomaton, fst);

                foreach (FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair> path in prefixPaths)
                {
                    searcher2.AddStartPaths(path.FstNode, path.Output, true, path.Input);
                }

                var completions2 = searcher2.Search();
                if (Debugging.AssertsEnabled) Debugging.Assert(completions2.IsComplete);

                foreach (Util.Fst.Util.Result<PairOutputs<Int64, BytesRef>.Pair> completion in completions2)
                {

                    LookupResult result = GetLookupResult(completion.Output.Output1, completion.Output.Output2, spare);

                    // TODO: for fuzzy case would be nice to return
                    // how many edits were required

                    //System.out.println("    result=" + result);
                    results.Add(result);

                    if (results.Count == num)
                    {
                        // In the exactFirst=true case the search may
                        // produce one extra path
                        break;
                    }
                }

                return results;
            }
            catch (Exception bogus) when (bogus.IsIOException())
            {
                throw RuntimeException.Create(bogus);
            }
        }

        private sealed class TopNSearcherAnonymousClass : Util.Fst.Util.TopNSearcher<PairOutputs<Int64, BytesRef>.Pair>
        {
            private readonly AnalyzingSuggester outerInstance;

            private readonly BytesRef utf8Key;
            private readonly IList<LookupResult> results;

            public TopNSearcherAnonymousClass(
                AnalyzingSuggester outerInstance,
                FST<PairOutputs<Int64, BytesRef>.Pair> fst,
                int topN,
                int maxQueueDepth,
                IComparer<PairOutputs<Int64, BytesRef>.Pair> comparer,
                BytesRef utf8Key,
                IList<LookupResult> results)
                : base(fst, topN, maxQueueDepth, comparer)
            {
                this.outerInstance = outerInstance;
                this.utf8Key = utf8Key;
                this.results = results;
                seen = new JCG.HashSet<BytesRef>();
            }

            private readonly ISet<BytesRef> seen;

            protected override bool AcceptResult(Int32sRef input, PairOutputs<Int64, BytesRef>.Pair output)
            {

                // Dedup: when the input analyzes to a graph we
                // can get duplicate surface forms:
                if (seen.Contains(output.Output2))
                {
                    return false;
                }
                seen.Add(output.Output2);

                if (!outerInstance.exactFirst)
                {
                    return true;
                }
                else
                {
                    // In exactFirst mode, don't accept any paths
                    // matching the surface form since that will
                    // create duplicate results:
                    if (outerInstance.SameSurfaceForm(utf8Key, output.Output2))
                    {
                        // We found exact match, which means we should
                        // have already found it in the first search:
                        if (Debugging.AssertsEnabled) Debugging.Assert(results.Count == 1);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        public override long Count => count;

        /// <summary>
        /// Returns all prefix paths to initialize the search.
        /// </summary>
        protected virtual IList<FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair>> GetFullPrefixPaths(
            IList<FSTUtil.Path<PairOutputs<Int64, BytesRef>.Pair>> prefixPaths, Automaton lookupAutomaton,
            FST<PairOutputs<Int64, BytesRef>.Pair> fst)
        {
            return prefixPaths;
        }

        internal ISet<Int32sRef> ToFiniteStrings(BytesRef surfaceForm, TokenStreamToAutomaton ts2a)
        {
            // Analyze surface form:
            Automaton automaton = null;
            TokenStream ts = indexAnalyzer.GetTokenStream("", surfaceForm.Utf8ToString());
            try
            {

                // Create corresponding automaton: labels are bytes
                // from each analyzed token, with byte 0 used as
                // separator between tokens:
                automaton = ts2a.ToAutomaton(ts);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }

            ReplaceSep(automaton);
            automaton = ConvertAutomaton(automaton);

            if (Debugging.AssertsEnabled) Debugging.Assert(SpecialOperations.IsFinite(automaton));

            // Get all paths from the automaton (there can be
            // more than one path, eg if the analyzer created a
            // graph using SynFilter or WDF):

            // TODO: we could walk & add simultaneously, so we
            // don't have to alloc [possibly biggish]
            // intermediate HashSet in RAM:
            return SpecialOperations.GetFiniteStrings(automaton, maxGraphExpansions);
        }

        internal Automaton ToLookupAutomaton(string key)
        {
            // TODO: is there a Reader from a CharSequence?
            // Turn tokenstream into automaton:
            Automaton automaton = null;
            TokenStream ts = queryAnalyzer.GetTokenStream("", key);
            try
            {
                automaton = (GetTokenStreamToAutomaton()).ToAutomaton(ts);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }

            // TODO: we could use the end offset to "guess"
            // whether the final token was a partial token; this
            // would only be a heuristic ... but maybe an OK one.
            // This way we could eg differentiate "net" from "net ",
            // which we can't today...

            ReplaceSep(automaton);

            // TODO: we can optimize this somewhat by determinizing
            // while we convert
            BasicOperations.Determinize(automaton);
            return automaton;
        }

        /// <summary>
        /// Returns the weight associated with an input string,
        /// or null if it does not exist.
        /// </summary>
        public virtual object Get(string key)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// cost -> weight </summary>
        private static int DecodeWeight(long encoded)
        {
            return (int)(int.MaxValue - encoded);
        }

        /// <summary>
        /// weight -> cost </summary>
        private static int EncodeWeight(long value)
        {
            if (value < 0 || value > int.MaxValue)
            {
                throw UnsupportedOperationException.Create("cannot encode value: " + value);
            }
            return int.MaxValue - (int)value;
        }

        internal static readonly IComparer<PairOutputs<Int64, BytesRef>.Pair> weightComparer =
            Comparer<PairOutputs<Int64, BytesRef>.Pair>.Create((left, right) => Comparer<Int64>.Default.Compare(left.Output1, right.Output1));
    }
}