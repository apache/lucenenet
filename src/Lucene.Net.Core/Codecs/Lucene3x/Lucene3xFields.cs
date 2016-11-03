using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Util;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;

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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Exposes flex API on a pre-flex index, as a codec.
    /// @lucene.experimental </summary>
    /// @deprecated (4.0)
    [Obsolete("(4.0)")]
    public class Lucene3xFields : FieldsProducer
    {
        private const bool DEBUG_SURROGATES = false;

        public TermInfosReader Tis;
        public readonly TermInfosReader TisNoIndex;

        public readonly IndexInput FreqStream;
        public readonly IndexInput ProxStream;
        private readonly FieldInfos FieldInfos;
        private readonly SegmentInfo Si;
        internal readonly SortedDictionary<string, FieldInfo> Fields = new SortedDictionary<string, FieldInfo>();
        internal readonly IDictionary<string, Terms> PreTerms_ = new Dictionary<string, Terms>();
        private readonly Directory Dir;
        private readonly IOContext Context;
        private Directory CfsReader;

        public Lucene3xFields(Directory dir, FieldInfos fieldInfos, SegmentInfo info, IOContext context, int indexDivisor)
        {
            Si = info;

            // NOTE: we must always load terms index, even for
            // "sequential" scan during merging, because what is
            // sequential to merger may not be to TermInfosReader
            // since we do the surrogates dance:
            if (indexDivisor < 0)
            {
                indexDivisor = -indexDivisor;
            }

            bool success = false;
            try
            {
                var r = new TermInfosReader(dir, info.Name, fieldInfos, context, indexDivisor);
                if (indexDivisor == -1)
                {
                    TisNoIndex = r;
                }
                else
                {
                    TisNoIndex = null;
                    Tis = r;
                }
                this.Context = context;
                this.FieldInfos = fieldInfos;

                // make sure that all index files have been read or are kept open
                // so that if an index update removes them we'll still have them
                FreqStream = dir.OpenInput(IndexFileNames.SegmentFileName(info.Name, "", Lucene3xPostingsFormat.FREQ_EXTENSION), context);
                bool anyProx = false;
                foreach (FieldInfo fi in fieldInfos)
                {
                    if (fi.Indexed)
                    {
                        Fields[fi.Name] = fi;
                        PreTerms_[fi.Name] = new PreTerms(this, fi);
                        if (fi.FieldIndexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                        {
                            anyProx = true;
                        }
                    }
                }

                if (anyProx)
                {
                    ProxStream = dir.OpenInput(IndexFileNames.SegmentFileName(info.Name, "", Lucene3xPostingsFormat.PROX_EXTENSION), context);
                }
                else
                {
                    ProxStream = null;
                }
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above. In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    Dispose();
                }
            }
            this.Dir = dir;
        }

        // If this returns, we do the surrogates dance so that the
        // terms are sorted by unicode sort order.  this should be
        // true when segments are used for "normal" searching;
        // it's only false during testing, to create a pre-flex
        // index, using the test-only PreFlexRW.
        protected internal virtual bool SortTermsByUnicode()
        {
            return true;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return Fields.Keys.GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            Terms ret;
            PreTerms_.TryGetValue(field, out ret);
            return ret;
        }

        public override int Size
        {
            get
            {
                Debug.Assert(PreTerms_.Count == Fields.Count);
                return Fields.Count;
            }
        }

        public override long UniqueTermCount
        {
            get
            {
                return TermsDict.Size();
            }
        }

        private TermInfosReader TermsDict
        {
            get
            {
                lock (this)
                {
                    if (Tis != null)
                    {
                        return Tis;
                    }
                    else
                    {
                        return TisNoIndex;
                    }
                }
            }
        }

        public override void Dispose()
        {
            IOUtils.Close(Tis, TisNoIndex, CfsReader, FreqStream, ProxStream);
        }

        private class PreTerms : Terms
        {
            private readonly Lucene3xFields OuterInstance;

            internal readonly FieldInfo fieldInfo;

            internal PreTerms(Lucene3xFields outerInstance, FieldInfo fieldInfo)
            {
                this.OuterInstance = outerInstance;
                this.fieldInfo = fieldInfo;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                var termsEnum = new PreTermsEnum(OuterInstance);
                termsEnum.Reset(fieldInfo);
                return termsEnum;
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    // Pre-flex indexes always sorted in UTF16 order, but
                    // we remap on-the-fly to unicode order
                    if (OuterInstance.SortTermsByUnicode())
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override long Size()
            {
                return -1;
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return -1;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return -1;
                }
            }

            public override int DocCount
            {
                get
                {
                    return -1;
                }
            }

            public override bool HasFreqs()
            {
                return fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
            }

            public override bool HasOffsets()
            {
                // preflex doesn't support this
                Debug.Assert(fieldInfo.FieldIndexOptions < FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
                return false;
            }

            public override bool HasPositions()
            {
                return fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            }

            public override bool HasPayloads()
            {
                return fieldInfo.HasPayloads();
            }
        }

        private class PreTermsEnum : TermsEnum
        {
            private readonly Lucene3xFields OuterInstance;

            public PreTermsEnum(Lucene3xFields outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal SegmentTermEnum TermEnum;
            internal FieldInfo fieldInfo;
            internal string InternedFieldName;
            internal bool SkipNext;
            internal BytesRef Current;

            internal SegmentTermEnum SeekTermEnum;

            private static readonly sbyte UTF8_NON_BMP_LEAD = unchecked((sbyte) 0xf0);
            private static readonly sbyte UTF8_HIGH_BMP_LEAD = unchecked((sbyte) 0xee);

            // Returns true if the unicode char is "after" the
            // surrogates in UTF16, ie >= U+E000 and <= U+FFFF:
            private static bool IsHighBMPChar(byte[] b, int idx)
            {
                return (((sbyte)b[idx]) & UTF8_HIGH_BMP_LEAD) == UTF8_HIGH_BMP_LEAD;
            }

            // Returns true if the unicode char in the UTF8 byte
            // sequence starting at idx encodes a char outside of
            // BMP (ie what would be a surrogate pair in UTF16):
            private static bool IsNonBMPChar(byte[] b, int idx)
            {
                return (((sbyte)b[idx]) & UTF8_NON_BMP_LEAD) == UTF8_NON_BMP_LEAD;
            }

            private readonly sbyte[] Scratch = new sbyte[4];
            private readonly BytesRef PrevTerm = new BytesRef();
            private readonly BytesRef ScratchTerm = new BytesRef();
            private int NewSuffixStart;

            // Swap in S, in place of E:
            internal virtual bool SeekToNonBMP(SegmentTermEnum te, BytesRef term, int pos)
            {
                int savLength = term.Length;

                Debug.Assert(term.Offset == 0);

                // The 3 bytes starting at downTo make up 1
                // unicode character:
                Debug.Assert(IsHighBMPChar(term.Bytes, pos));

                // NOTE: we cannot make this assert, because
                // AutomatonQuery legitimately sends us malformed UTF8
                // (eg the UTF8 bytes with just 0xee)
                // assert term.length >= pos + 3: "term.length=" + term.length + " pos+3=" + (pos+3) + " byte=" + Integer.toHexString(term.bytes[pos]) + " term=" + term.toString();

                // Save the bytes && length, since we need to
                // restore this if seek "back" finds no matching
                // terms
                if (term.Bytes.Length < 4 + pos)
                {
                    term.Grow(4 + pos);
                }

                Scratch[0] = (sbyte)term.Bytes[pos];
                Scratch[1] = (sbyte)term.Bytes[pos + 1];
                Scratch[2] = (sbyte)term.Bytes[pos + 2];

                term.Bytes[pos] = unchecked((byte)0xf0);
                term.Bytes[pos + 1] = unchecked((byte)0x90);
                term.Bytes[pos + 2] = unchecked((byte)0x80);
                term.Bytes[pos + 3] = unchecked((byte)0x80);
                term.Length = 4 + pos;

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("      try seek term=" + UnicodeUtil.ToHexString(term.Utf8ToString()));
                }

                // Seek "back":
                OuterInstance.TermsDict.SeekEnum(te, new Term(fieldInfo.Name, term), true);

                // Test if the term we seek'd to in fact found a
                // surrogate pair at the same position as the E:
                Term t2 = te.Term();

                // Cannot be null (or move to next field) because at
                // "worst" it'd seek to the same term we are on now,
                // unless we are being called from seek
                if (t2 == null || t2.Field != InternedFieldName)
                {
                    return false;
                }

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("      got term=" + UnicodeUtil.ToHexString(t2.Text()));
                }

                // Now test if prefix is identical and we found
                // a non-BMP char at the same position:
                BytesRef b2 = t2.Bytes;
                Debug.Assert(b2.Offset == 0);

                bool matches;
                if (b2.Length >= term.Length && IsNonBMPChar(b2.Bytes, pos))
                {
                    matches = true;
                    for (int i = 0; i < pos; i++)
                    {
                        if (term.Bytes[i] != b2.Bytes[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                }
                else
                {
                    matches = false;
                }

                // Restore term:
                term.Length = savLength;
                term.Bytes[pos] = (byte)Scratch[0];
                term.Bytes[pos + 1] = (byte)Scratch[1];
                term.Bytes[pos + 2] = (byte)Scratch[2];

                return matches;
            }

            // Seek type 2 "continue" (back to the start of the
            // surrogates): scan the stripped suffix from the
            // prior term, backwards. If there was an E in that
            // part, then we try to seek back to S.  If that
            // seek finds a matching term, we go there.
            internal virtual bool DoContinue()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try cont");
                }

                int downTo = PrevTerm.Length - 1;

                bool didSeek = false;

                int limit = Math.Min(NewSuffixStart, ScratchTerm.Length - 1);

                while (downTo > limit)
                {
                    if (IsHighBMPChar(PrevTerm.Bytes, downTo))
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("    found E pos=" + downTo + " vs len=" + PrevTerm.Length);
                        }

                        if (SeekToNonBMP(SeekTermEnum, PrevTerm, downTo))
                        {
                            // TODO: more efficient seek?
                            OuterInstance.TermsDict.SeekEnum(TermEnum, SeekTermEnum.Term(), true);
                            //newSuffixStart = downTo+4;
                            NewSuffixStart = downTo;
                            ScratchTerm.CopyBytes(TermEnum.Term().Bytes);
                            didSeek = true;
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("      seek!");
                            }
                            break;
                        }
                        else
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("      no seek");
                            }
                        }
                    }

                    // Shorten prevTerm in place so that we don't redo
                    // this loop if we come back here:
                    if ((PrevTerm.Bytes[downTo] & 0xc0) == 0xc0 || (PrevTerm.Bytes[downTo] & 0x80) == 0)
                    {
                        PrevTerm.Length = downTo;
                    }

                    downTo--;
                }

                return didSeek;
            }

            // Look for seek type 3 ("pop"): if the delta from
            // prev -> current was replacing an S with an E,
            // we must now seek to beyond that E.  this seek
            // "finishes" the dance at this character
            // position.
            internal virtual bool DoPop()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try pop");
                }

                Debug.Assert(NewSuffixStart <= PrevTerm.Length);
                Debug.Assert(NewSuffixStart < ScratchTerm.Length || NewSuffixStart == 0);

                if (PrevTerm.Length > NewSuffixStart && IsNonBMPChar(PrevTerm.Bytes, NewSuffixStart) && IsHighBMPChar(ScratchTerm.Bytes, NewSuffixStart))
                {
                    // Seek type 2 -- put 0xFF at this position:
                    ScratchTerm.Bytes[NewSuffixStart] = unchecked((byte)0xff);
                    ScratchTerm.Length = NewSuffixStart + 1;

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("    seek to term=" + UnicodeUtil.ToHexString(ScratchTerm.Utf8ToString()) + " " + ScratchTerm.ToString());
                    }

                    // TODO: more efficient seek?  can we simply swap
                    // the enums?
                    OuterInstance.TermsDict.SeekEnum(TermEnum, new Term(fieldInfo.Name, ScratchTerm), true);

                    Term t2 = TermEnum.Term();

                    // We could hit EOF or different field since this
                    // was a seek "forward":
                    if (t2 != null && t2.Field == InternedFieldName)
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("      got term=" + UnicodeUtil.ToHexString(t2.Text()) + " " + t2.Bytes);
                        }

                        BytesRef b2 = t2.Bytes;
                        Debug.Assert(b2.Offset == 0);

                        // Set newSuffixStart -- we can't use
                        // termEnum's since the above seek may have
                        // done no scanning (eg, term was precisely
                        // and index term, or, was in the term seek
                        // cache):
                        ScratchTerm.CopyBytes(b2);
                        SetNewSuffixStart(PrevTerm, ScratchTerm);

                        return true;
                    }
                    else if (NewSuffixStart != 0 || ScratchTerm.Length != 0)
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("      got term=null (or next field)");
                        }
                        NewSuffixStart = 0;
                        ScratchTerm.Length = 0;
                        return true;
                    }
                }

                return false;
            }

            // Pre-flex indices store terms in UTF16 sort order, but
            // certain queries require Unicode codepoint order; this
            // method carefully seeks around surrogates to handle
            // this impedance mismatch

            internal virtual void SurrogateDance()
            {
                if (!UnicodeSortOrder)
                {
                    return;
                }

                // We are invoked after TIS.next() (by UTF16 order) to
                // possibly seek to a different "next" (by unicode
                // order) term.

                // We scan only the "delta" from the last term to the
                // current term, in UTF8 bytes.  We look at 1) the bytes
                // stripped from the prior term, and then 2) the bytes
                // appended to that prior term's prefix.

                // We don't care about specific UTF8 sequences, just
                // the "category" of the UTF16 character.  Category S
                // is a high/low surrogate pair (it non-BMP).
                // Category E is any BMP char > UNI_SUR_LOW_END (and <
                // U+FFFF). Category A is the rest (any unicode char
                // <= UNI_SUR_HIGH_START).

                // The core issue is that pre-flex indices sort the
                // characters as ASE, while flex must sort as AES.  So
                // when scanning, when we hit S, we must 1) seek
                // forward to E and enum the terms there, then 2) seek
                // back to S and enum all terms there, then 3) seek to
                // after E.  Three different seek points (1, 2, 3).

                // We can easily detect S in UTF8: if a byte has
                // prefix 11110 (0xf0), then that byte and the
                // following 3 bytes encode a single unicode codepoint
                // in S.  Similarly, we can detect E: if a byte has
                // prefix 1110111 (0xee), then that byte and the
                // following 2 bytes encode a single unicode codepoint
                // in E.

                // Note that this is really a recursive process --
                // maybe the char at pos 2 needs to dance, but any
                // point in its dance, suddenly pos 4 needs to dance
                // so you must finish pos 4 before returning to pos
                // 2.  But then during pos 4's dance maybe pos 7 needs
                // to dance, etc.  However, despite being recursive,
                // we don't need to hold any state because the state
                // can always be derived by looking at prior term &
                // current term.

                // TODO: can we avoid this copy?
                if (TermEnum.Term() == null || TermEnum.Term().Field != InternedFieldName)
                {
                    ScratchTerm.Length = 0;
                }
                else
                {
                    ScratchTerm.CopyBytes(TermEnum.Term().Bytes);
                }

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  dance");
                    Console.WriteLine("    prev=" + UnicodeUtil.ToHexString(PrevTerm.Utf8ToString()));
                    Console.WriteLine("         " + PrevTerm.ToString());
                    Console.WriteLine("    term=" + UnicodeUtil.ToHexString(ScratchTerm.Utf8ToString()));
                    Console.WriteLine("         " + ScratchTerm.ToString());
                }

                // this code assumes TermInfosReader/SegmentTermEnum
                // always use BytesRef.offset == 0
                Debug.Assert(PrevTerm.Offset == 0);
                Debug.Assert(ScratchTerm.Offset == 0);

                // Need to loop here because we may need to do multiple
                // pops, and possibly a continue in the end, ie:
                //
                //  cont
                //  pop, cont
                //  pop, pop, cont
                //  <nothing>
                //

                while (true)
                {
                    if (DoContinue())
                    {
                        break;
                    }
                    else
                    {
                        if (!DoPop())
                        {
                            break;
                        }
                    }
                }

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  finish bmp ends");
                }

                DoPushes();
            }

            // Look for seek type 1 ("push"): if the newly added
            // suffix contains any S, we must try to seek to the
            // corresponding E.  If we find a match, we go there;
            // else we keep looking for additional S's in the new
            // suffix.  this "starts" the dance, at this character
            // position:
            internal virtual void DoPushes()
            {
                int upTo = NewSuffixStart;
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try push newSuffixStart=" + NewSuffixStart + " scratchLen=" + ScratchTerm.Length);
                }

                while (upTo < ScratchTerm.Length)
                {
                    if (IsNonBMPChar(ScratchTerm.Bytes, upTo) && (upTo > NewSuffixStart || (upTo >= PrevTerm.Length || (!IsNonBMPChar(PrevTerm.Bytes, upTo) && !IsHighBMPChar(PrevTerm.Bytes, upTo)))))
                    {
                        // A non-BMP char (4 bytes UTF8) starts here:
                        Debug.Assert(ScratchTerm.Length >= upTo + 4);

                        int savLength = ScratchTerm.Length;
                        Scratch[0] = (sbyte)ScratchTerm.Bytes[upTo];
                        Scratch[1] = (sbyte)ScratchTerm.Bytes[upTo + 1];
                        Scratch[2] = (sbyte)ScratchTerm.Bytes[upTo + 2];

                        ScratchTerm.Bytes[upTo] = (byte)UTF8_HIGH_BMP_LEAD;
                        ScratchTerm.Bytes[upTo + 1] = unchecked((byte)0x80);
                        ScratchTerm.Bytes[upTo + 2] = unchecked((byte)0x80);
                        ScratchTerm.Length = upTo + 3;

                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("    try seek 1 pos=" + upTo + " term=" + UnicodeUtil.ToHexString(ScratchTerm.Utf8ToString()) + " " + ScratchTerm.ToString() + " len=" + ScratchTerm.Length);
                        }

                        // Seek "forward":
                        // TODO: more efficient seek?
                        OuterInstance.TermsDict.SeekEnum(SeekTermEnum, new Term(fieldInfo.Name, ScratchTerm), true);

                        ScratchTerm.Bytes[upTo] = (byte)Scratch[0];
                        ScratchTerm.Bytes[upTo + 1] = (byte)Scratch[1];
                        ScratchTerm.Bytes[upTo + 2] = (byte)Scratch[2];
                        ScratchTerm.Length = savLength;

                        // Did we find a match?
                        Term t2 = SeekTermEnum.Term();

                        if (DEBUG_SURROGATES)
                        {
                            if (t2 == null)
                            {
                                Console.WriteLine("      hit term=null");
                            }
                            else
                            {
                                Console.WriteLine("      hit term=" + UnicodeUtil.ToHexString(t2.Text()) + " " + (t2 == null ? null : t2.Bytes));
                            }
                        }

                        // Since this was a seek "forward", we could hit
                        // EOF or a different field:
                        bool matches;

                        if (t2 != null && t2.Field == InternedFieldName)
                        {
                            BytesRef b2 = t2.Bytes;
                            Debug.Assert(b2.Offset == 0);
                            if (b2.Length >= upTo + 3 && IsHighBMPChar(b2.Bytes, upTo))
                            {
                                matches = true;
                                for (int i = 0; i < upTo; i++)
                                {
                                    if (ScratchTerm.Bytes[i] != b2.Bytes[i])
                                    {
                                        matches = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                matches = false;
                            }
                        }
                        else
                        {
                            matches = false;
                        }

                        if (matches)
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("      matches!");
                            }

                            // OK seek "back"
                            // TODO: more efficient seek?
                            OuterInstance.TermsDict.SeekEnum(TermEnum, SeekTermEnum.Term(), true);

                            ScratchTerm.CopyBytes(SeekTermEnum.Term().Bytes);

                            // +3 because we don't need to check the char
                            // at upTo: we know it's > BMP
                            upTo += 3;

                            // NOTE: we keep iterating, now, since this
                            // can easily "recurse".  Ie, after seeking
                            // forward at a certain char position, we may
                            // find another surrogate in our [new] suffix
                            // and must then do another seek (recurse)
                        }
                        else
                        {
                            upTo++;
                        }
                    }
                    else
                    {
                        upTo++;
                    }
                }
            }

            internal bool UnicodeSortOrder;

            internal virtual void Reset(FieldInfo fieldInfo)
            {
                //System.out.println("pff.reset te=" + termEnum);
                this.fieldInfo = fieldInfo;
                
                InternedFieldName = StringHelper.Intern(fieldInfo.Name);

                Term term = new Term(InternedFieldName);
                if (TermEnum == null)
                {
                    TermEnum = OuterInstance.TermsDict.Terms(term);
                    SeekTermEnum = OuterInstance.TermsDict.Terms(term);
                    //System.out.println("  term=" + termEnum.term());
                }
                else
                {
                    OuterInstance.TermsDict.SeekEnum(TermEnum, term, true);
                }
                SkipNext = true;

                UnicodeSortOrder = OuterInstance.SortTermsByUnicode();

                Term t = TermEnum.Term();
                if (t != null && t.Field == InternedFieldName)
                {
                    NewSuffixStart = 0;
                    PrevTerm.Length = 0;
                    SurrogateDance();
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    // Pre-flex indexes always sorted in UTF16 order, but
                    // we remap on-the-fly to unicode order
                    if (UnicodeSortOrder)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override void SeekExact(long ord)
            {
                throw new System.NotSupportedException();
            }

            public override long Ord()
            {
                throw new System.NotSupportedException();
            }

            public override SeekStatus SeekCeil(BytesRef term)
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("TE.seek target=" + UnicodeUtil.ToHexString(term.Utf8ToString()));
                }
                SkipNext = false;
                TermInfosReader tis = OuterInstance.TermsDict;
                Term t0 = new Term(fieldInfo.Name, term);

                Debug.Assert(TermEnum != null);

                tis.SeekEnum(TermEnum, t0, false);

                Term t = TermEnum.Term();

                if (t != null && t.Field == InternedFieldName && term.BytesEquals(t.Bytes))
                {
                    // If we found an exact match, no need to do the
                    // surrogate dance
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek exact match");
                    }
                    Current = t.Bytes;
                    return SeekStatus.FOUND;
                }
                else if (t == null || t.Field != InternedFieldName)
                {
                    // TODO: maybe we can handle this like the next()
                    // into null?  set term as prevTerm then dance?

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek hit EOF");
                    }

                    // We hit EOF; try end-case surrogate dance: if we
                    // find an E, try swapping in S, backwards:
                    ScratchTerm.CopyBytes(term);

                    Debug.Assert(ScratchTerm.Offset == 0);

                    for (int i = ScratchTerm.Length - 1; i >= 0; i--)
                    {
                        if (IsHighBMPChar(ScratchTerm.Bytes, i))
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.WriteLine("    found E pos=" + i + "; try seek");
                            }

                            if (SeekToNonBMP(SeekTermEnum, ScratchTerm, i))
                            {
                                ScratchTerm.CopyBytes(SeekTermEnum.Term().Bytes);
                                OuterInstance.TermsDict.SeekEnum(TermEnum, SeekTermEnum.Term(), false);

                                NewSuffixStart = 1 + i;

                                DoPushes();

                                // Found a match
                                // TODO: faster seek?
                                Current = TermEnum.Term().Bytes;
                                return SeekStatus.NOT_FOUND;
                            }
                        }
                    }

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek END");
                    }

                    Current = null;
                    return SeekStatus.END;
                }
                else
                {
                    // We found a non-exact but non-null term; this one
                    // is fun -- just treat it like next, by pretending
                    // requested term was prev:
                    PrevTerm.CopyBytes(term);

                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  seek hit non-exact term=" + UnicodeUtil.ToHexString(t.Text()));
                    }

                    BytesRef br = t.Bytes;
                    Debug.Assert(br.Offset == 0);

                    SetNewSuffixStart(term, br);

                    SurrogateDance();

                    Term t2 = TermEnum.Term();
                    if (t2 == null || t2.Field != InternedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        Debug.Assert(t2 == null || !t2.Field.Equals(InternedFieldName));
                        Current = null;
                        return SeekStatus.END;
                    }
                    else
                    {
                        Current = t2.Bytes;
                        Debug.Assert(!UnicodeSortOrder || term.CompareTo(Current) < 0, "term=" + UnicodeUtil.ToHexString(term.Utf8ToString()) + " vs current=" + UnicodeUtil.ToHexString(Current.Utf8ToString()));
                        return SeekStatus.NOT_FOUND;
                    }
                }
            }

            internal virtual void SetNewSuffixStart(BytesRef br1, BytesRef br2)
            {
                int limit = Math.Min(br1.Length, br2.Length);
                int lastStart = 0;
                for (int i = 0; i < limit; i++)
                {
                    if ((br1.Bytes[br1.Offset + i] & 0xc0) == 0xc0 || (br1.Bytes[br1.Offset + i] & 0x80) == 0)
                    {
                        lastStart = i;
                    }
                    if (br1.Bytes[br1.Offset + i] != br2.Bytes[br2.Offset + i])
                    {
                        NewSuffixStart = lastStart;
                        if (DEBUG_SURROGATES)
                        {
                            Console.WriteLine("    set newSuffixStart=" + NewSuffixStart);
                        }
                        return;
                    }
                }
                NewSuffixStart = limit;
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("    set newSuffixStart=" + NewSuffixStart);
                }
            }

            public override BytesRef Next()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("TE.next()");
                }
                if (SkipNext)
                {
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  skipNext=true");
                    }
                    SkipNext = false;
                    if (TermEnum.Term() == null)
                    {
                        return null;
                        // PreFlex codec interns field names:
                    }
                    else if (TermEnum.Term().Field != InternedFieldName)
                    {
                        return null;
                    }
                    else
                    {
                        return Current = TermEnum.Term().Bytes;
                    }
                }

                // TODO: can we use STE's prevBuffer here?
                PrevTerm.CopyBytes(TermEnum.Term().Bytes);

                if (TermEnum.Next() && TermEnum.Term().Field == InternedFieldName)
                {
                    NewSuffixStart = TermEnum.NewSuffixStart;
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  newSuffixStart=" + NewSuffixStart);
                    }
                    SurrogateDance();
                    Term t = TermEnum.Term();
                    if (t == null || t.Field != InternedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        Debug.Assert(t == null || !t.Field.Equals(InternedFieldName));
                        Current = null;
                    }
                    else
                    {
                        Current = t.Bytes;
                    }
                    return Current;
                }
                else
                {
                    // this field is exhausted, but we have to give
                    // surrogateDance a chance to seek back:
                    if (DEBUG_SURROGATES)
                    {
                        Console.WriteLine("  force cont");
                    }
                    //newSuffixStart = prevTerm.length;
                    NewSuffixStart = 0;
                    SurrogateDance();

                    Term t = TermEnum.Term();
                    if (t == null || t.Field != InternedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        Debug.Assert(t == null || !t.Field.Equals(InternedFieldName));
                        return null;
                    }
                    else
                    {
                        Current = t.Bytes;
                        return Current;
                    }
                }
            }

            public override BytesRef Term()
            {
                return Current;
            }

            public override int DocFreq()
            {
                return TermEnum.DocFreq();
            }

            public override long TotalTermFreq()
            {
                return -1;
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                PreDocsEnum docsEnum;
                if (reuse == null || !(reuse is PreDocsEnum))
                {
                    docsEnum = new PreDocsEnum(OuterInstance);
                }
                else
                {
                    docsEnum = (PreDocsEnum)reuse;
                    if (docsEnum.FreqStream != OuterInstance.FreqStream)
                    {
                        docsEnum = new PreDocsEnum(OuterInstance);
                    }
                }
                return docsEnum.Reset(TermEnum, liveDocs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                PreDocsAndPositionsEnum docsPosEnum;
                if (fieldInfo.FieldIndexOptions != FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    return null;
                }
                else if (reuse == null || !(reuse is PreDocsAndPositionsEnum))
                {
                    docsPosEnum = new PreDocsAndPositionsEnum(OuterInstance);
                }
                else
                {
                    docsPosEnum = (PreDocsAndPositionsEnum)reuse;
                    if (docsPosEnum.FreqStream != OuterInstance.FreqStream)
                    {
                        docsPosEnum = new PreDocsAndPositionsEnum(OuterInstance);
                    }
                }
                return docsPosEnum.Reset(TermEnum, liveDocs);
            }
        }

        private sealed class PreDocsEnum : DocsEnum
        {
            private readonly Lucene3xFields OuterInstance;

            internal readonly SegmentTermDocs Docs;
            internal int DocID_Renamed = -1;

            internal PreDocsEnum(Lucene3xFields outerInstance)
            {
                this.OuterInstance = outerInstance;
                Docs = new SegmentTermDocs(outerInstance.FreqStream, outerInstance.TermsDict, outerInstance.FieldInfos);
            }

            internal IndexInput FreqStream
            {
                get
                {
                    return OuterInstance.FreqStream;
                }
            }

            public PreDocsEnum Reset(SegmentTermEnum termEnum, Bits liveDocs)
            {
                Docs.LiveDocs = liveDocs;
                Docs.Seek(termEnum);
                Docs.Freq_Renamed = 1;
                DocID_Renamed = -1;
                return this;
            }

            public override int NextDoc()
            {
                if (Docs.Next())
                {
                    return DocID_Renamed = Docs.Doc();
                }
                else
                {
                    return DocID_Renamed = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                if (Docs.SkipTo(target))
                {
                    return DocID_Renamed = Docs.Doc();
                }
                else
                {
                    return DocID_Renamed = NO_MORE_DOCS;
                }
            }

            public override int Freq()
            {
                return Docs.Freq();
            }

            public override int DocID()
            {
                return DocID_Renamed;
            }

            public override long Cost()
            {
                return Docs.Df;
            }
        }

        private sealed class PreDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene3xFields OuterInstance;

            internal readonly SegmentTermPositions Pos;
            internal int DocID_Renamed = -1;

            internal PreDocsAndPositionsEnum(Lucene3xFields outerInstance)
            {
                this.OuterInstance = outerInstance;
                Pos = new SegmentTermPositions(outerInstance.FreqStream, outerInstance.ProxStream, outerInstance.TermsDict, outerInstance.FieldInfos);
            }

            internal IndexInput FreqStream
            {
                get
                {
                    return OuterInstance.FreqStream;
                }
            }

            public DocsAndPositionsEnum Reset(SegmentTermEnum termEnum, Bits liveDocs)
            {
                Pos.LiveDocs = liveDocs;
                Pos.Seek(termEnum);
                DocID_Renamed = -1;
                return this;
            }

            public override int NextDoc()
            {
                if (Pos.Next())
                {
                    return DocID_Renamed = Pos.Doc();
                }
                else
                {
                    return DocID_Renamed = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                if (Pos.SkipTo(target))
                {
                    return DocID_Renamed = Pos.Doc();
                }
                else
                {
                    return DocID_Renamed = NO_MORE_DOCS;
                }
            }

            public override int Freq()
            {
                return Pos.Freq();
            }

            public override int DocID()
            {
                return DocID_Renamed;
            }

            public override int NextPosition()
            {
                Debug.Assert(DocID_Renamed != NO_MORE_DOCS);
                return Pos.NextPosition();
            }

            public override int StartOffset()
            {
                return -1;
            }

            public override int EndOffset()
            {
                return -1;
            }

            public override BytesRef Payload
            {
                get
                {
                    return Pos.Payload;
                }
            }

            public override long Cost()
            {
                return Pos.Df;
            }
        }

        public override long RamBytesUsed()
        {
            if (Tis != null)
            {
                return Tis.RamBytesUsed();
            }
            else
            {
                // when there is no index, there is almost nothing loaded into RAM
                return 0L;
            }
        }

        public override void CheckIntegrity()
        {
        }
    }
}