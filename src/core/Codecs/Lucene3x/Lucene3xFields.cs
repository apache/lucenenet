using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xFields : FieldsProducer
    {
        private static readonly bool DEBUG_SURROGATES = false;

        public TermInfosReader tis;
        public readonly TermInfosReader tisNoIndex;

        public readonly IndexInput freqStream;
        public readonly IndexInput proxStream;
        private readonly FieldInfos fieldInfos;
        private readonly SegmentInfo si;
        internal readonly IDictionary<String, FieldInfo> fields = new TreeMap<String, FieldInfo>();
        internal readonly IDictionary<String, Terms> preTerms = new HashMap<String, Terms>();
        private readonly Directory dir;
        private readonly IOContext context;
        private Directory cfsReader;

        public Lucene3xFields(Directory dir, FieldInfos fieldInfos, SegmentInfo info, IOContext context, int indexDivisor)
        {

            si = info;

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
                TermInfosReader r = new TermInfosReader(dir, info.name, fieldInfos, context, indexDivisor);
                if (indexDivisor == -1)
                {
                    tisNoIndex = r;
                }
                else
                {
                    tisNoIndex = null;
                    tis = r;
                }
                this.context = context;
                this.fieldInfos = fieldInfos;

                // make sure that all index files have been read or are kept open
                // so that if an index update removes them we'll still have them
                freqStream = dir.OpenInput(IndexFileNames.SegmentFileName(info.name, "", Lucene3xPostingsFormat.FREQ_EXTENSION), context);
                bool anyProx = false;
                foreach (FieldInfo fi in fieldInfos)
                {
                    if (fi.IsIndexed)
                    {
                        fields[fi.name] = fi;
                        preTerms[fi.name] = new PreTerms(this, fi);
                        if (fi.IndexOptionsValue == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                        {
                            anyProx = true;
                        }
                    }
                }

                if (anyProx)
                {
                    proxStream = dir.OpenInput(IndexFileNames.SegmentFileName(info.name, "", Lucene3xPostingsFormat.PROX_EXTENSION), context);
                }
                else
                {
                    proxStream = null;
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
            this.dir = dir;
        }

        // If this returns, we do the surrogates dance so that the
        // terms are sorted by unicode sort order.  This should be
        // true when segments are used for "normal" searching;
        // it's only false during testing, to create a pre-flex
        // index, using the test-only PreFlexRW.
        protected virtual bool SortTermsByUnicode
        {
            get
            {
                return true;
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            return preTerms[field];
        }

        public override int Size
        {
            get
            {
                //assert preTerms.size() == fields.size();
                return fields.Count;
            }
        }

        public override long UniqueTermCount
        {
            get
            {
                return TermsDict.Size;
            }
        }

        private TermInfosReader TermsDict
        {
            get
            {
                lock (this)
                {
                    if (tis != null)
                    {
                        return tis;
                    }
                    else
                    {
                        return tisNoIndex;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Close(tis, tisNoIndex, cfsReader, freqStream, proxStream);
            }
        }

        private class PreTerms : Terms
        {
            internal readonly FieldInfo fieldInfo;
            private readonly Lucene3xFields parent;

            internal PreTerms(Lucene3xFields parent, FieldInfo fieldInfo)
            {
                this.parent = parent;
                this.fieldInfo = fieldInfo;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                PreTermsEnum termsEnum = new PreTermsEnum(parent);
                termsEnum.Reset(fieldInfo);
                return termsEnum;
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    // Pre-flex indexes always sorted in UTF16 order, but
                    // we remap on-the-fly to unicode order
                    if (parent.SortTermsByUnicode)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override long Size
            {
                get { return -1; }
            }

            public override long SumTotalTermFreq
            {
                get { return -1; }
            }

            public override long SumDocFreq
            {
                get { return -1; }
            }

            public override int DocCount
            {
                get { return -1; }
            }

            public override bool HasOffsets
            {
                get
                {
                    // preflex doesn't support this
                    //assert fieldInfo.getIndexOptions().compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) < 0;
                    return false;
                }
            }

            public override bool HasPositions
            {
                get
                {
                    return fieldInfo.IndexOptionsValue >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                }
            }

            public override bool HasPayloads
            {
                get { return fieldInfo.HasPayloads; }
            }
        }

        private class PreTermsEnum : TermsEnum
        {
            private readonly Lucene3xFields parent;

            private SegmentTermEnum termEnum;
            private FieldInfo fieldInfo;
            private String internedFieldName;
            private bool skipNext;
            private BytesRef current;

            private SegmentTermEnum seekTermEnum;

            private const sbyte UTF8_NON_BMP_LEAD = unchecked((sbyte)0xf0);
            private const sbyte UTF8_HIGH_BMP_LEAD = unchecked((sbyte)0xee);

            public PreTermsEnum(Lucene3xFields parent)
            {
                this.parent = parent;
            }

            // Returns true if the unicode char is "after" the
            // surrogates in UTF16, ie >= U+E000 and <= U+FFFF:
            private bool IsHighBMPChar(sbyte[] b, int idx)
            {
                return (b[idx] & UTF8_HIGH_BMP_LEAD) == UTF8_HIGH_BMP_LEAD;
            }

            // Returns true if the unicode char in the UTF8 byte
            // sequence starting at idx encodes a char outside of
            // BMP (ie what would be a surrogate pair in UTF16):
            private bool IsNonBMPChar(sbyte[] b, int idx)
            {
                return (b[idx] & UTF8_NON_BMP_LEAD) == UTF8_NON_BMP_LEAD;
            }

            private readonly sbyte[] scratch = new sbyte[4];
            private readonly BytesRef prevTerm = new BytesRef();
            private readonly BytesRef scratchTerm = new BytesRef();
            private int newSuffixStart;

            // Swap in S, in place of E:
            private bool SeekToNonBMP(SegmentTermEnum te, BytesRef term, int pos)
            {
                int savLength = term.length;

                //assert term.offset == 0;

                // The 3 bytes starting at downTo make up 1
                // unicode character:
                //assert isHighBMPChar(term.bytes, pos);

                // NOTE: we cannot make this assert, because
                // AutomatonQuery legitimately sends us malformed UTF8
                // (eg the UTF8 bytes with just 0xee)
                // assert term.length >= pos + 3: "term.length=" + term.length + " pos+3=" + (pos+3) + " byte=" + Integer.toHexString(term.bytes[pos]) + " term=" + term.toString();

                // Save the bytes && length, since we need to
                // restore this if seek "back" finds no matching
                // terms
                if (term.bytes.Length < 4 + pos)
                {
                    term.Grow(4 + pos);
                }

                scratch[0] = term.bytes[pos];
                scratch[1] = term.bytes[pos + 1];
                scratch[2] = term.bytes[pos + 2];

                term.bytes[pos] = unchecked((sbyte)0xf0);
                term.bytes[pos + 1] = unchecked((sbyte)0x90);
                term.bytes[pos + 2] = unchecked((sbyte)0x80);
                term.bytes[pos + 3] = unchecked((sbyte)0x80);
                term.length = 4 + pos;

                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("      try seek term=" + UnicodeUtil.ToHexString(term.Utf8ToString()));
                }

                // Seek "back":
                parent.TermsDict.SeekEnum(te, new Term(fieldInfo.name, term), true);

                // Test if the term we seek'd to in fact found a
                // surrogate pair at the same position as the E:
                Term t2 = te.Term;

                // Cannot be null (or move to next field) because at
                // "worst" it'd seek to the same term we are on now,
                // unless we are being called from seek
                if (t2 == null || t2.Field != internedFieldName)
                {
                    return false;
                }

                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("      got term=" + UnicodeUtil.ToHexString(t2.Text));
                }

                // Now test if prefix is identical and we found
                // a non-BMP char at the same position:
                BytesRef b2 = t2.Bytes;
                //assert b2.offset == 0;

                bool matches;
                if (b2.length >= term.length && IsNonBMPChar(b2.bytes, pos))
                {
                    matches = true;
                    for (int i = 0; i < pos; i++)
                    {
                        if (term.bytes[i] != b2.bytes[i])
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
                term.length = savLength;
                term.bytes[pos] = scratch[0];
                term.bytes[pos + 1] = scratch[1];
                term.bytes[pos + 2] = scratch[2];

                return matches;
            }

            // Seek type 2 "continue" (back to the start of the
            // surrogates): scan the stripped suffix from the
            // prior term, backwards. If there was an E in that
            // part, then we try to seek back to S.  If that
            // seek finds a matching term, we go there.
            private bool DoContinue()
            {

                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("  try cont");
                }

                int downTo = prevTerm.length - 1;

                bool didSeek = false;

                int limit = Math.Min(newSuffixStart, scratchTerm.length - 1);

                while (downTo > limit)
                {

                    if (IsHighBMPChar(prevTerm.bytes, downTo))
                    {

                        if (DEBUG_SURROGATES)
                        {
                            Console.Out.WriteLine("    found E pos=" + downTo + " vs len=" + prevTerm.length);
                        }

                        if (SeekToNonBMP(seekTermEnum, prevTerm, downTo))
                        {
                            // TODO: more efficient seek?
                            parent.TermsDict.SeekEnum(termEnum, seekTermEnum.Term, true);
                            //newSuffixStart = downTo+4;
                            newSuffixStart = downTo;
                            scratchTerm.CopyBytes(termEnum.Term.Bytes);
                            didSeek = true;
                            if (DEBUG_SURROGATES)
                            {
                                Console.Out.WriteLine("      seek!");
                            }
                            break;
                        }
                        else
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.Out.WriteLine("      no seek");
                            }
                        }
                    }

                    // Shorten prevTerm in place so that we don't redo
                    // this loop if we come back here:
                    if ((prevTerm.bytes[downTo] & 0xc0) == 0xc0 || (prevTerm.bytes[downTo] & 0x80) == 0)
                    {
                        prevTerm.length = downTo;
                    }

                    downTo--;
                }

                return didSeek;
            }

            // Look for seek type 3 ("pop"): if the delta from
            // prev -> current was replacing an S with an E,
            // we must now seek to beyond that E.  This seek
            // "finishes" the dance at this character
            // position.
            private bool DoPop()
            {

                if (DEBUG_SURROGATES)
                {
                    Console.WriteLine("  try pop");
                }

                //assert newSuffixStart <= prevTerm.length;
                //assert newSuffixStart < scratchTerm.length || newSuffixStart == 0;

                if (prevTerm.length > newSuffixStart &&
                    IsNonBMPChar(prevTerm.bytes, newSuffixStart) &&
                    IsHighBMPChar(scratchTerm.bytes, newSuffixStart))
                {

                    // Seek type 2 -- put 0xFF at this position:
                    scratchTerm.bytes[newSuffixStart] = unchecked((sbyte)0xff);
                    scratchTerm.length = newSuffixStart + 1;

                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("    seek to term=" + UnicodeUtil.ToHexString(scratchTerm.Utf8ToString()) + " " + scratchTerm.ToString());
                    }

                    // TODO: more efficient seek?  can we simply swap
                    // the enums?
                    parent.TermsDict.SeekEnum(termEnum, new Term(fieldInfo.name, scratchTerm), true);

                    Term t2 = termEnum.Term;

                    // We could hit EOF or different field since this
                    // was a seek "forward":
                    if (t2 != null && t2.Field == internedFieldName)
                    {

                        if (DEBUG_SURROGATES)
                        {
                            Console.Out.WriteLine("      got term=" + UnicodeUtil.ToHexString(t2.Text) + " " + t2.Bytes);
                        }

                        BytesRef b2 = t2.Bytes;
                        //assert b2.offset == 0;


                        // Set newSuffixStart -- we can't use
                        // termEnum's since the above seek may have
                        // done no scanning (eg, term was precisely
                        // and index term, or, was in the term seek
                        // cache):
                        scratchTerm.CopyBytes(b2);
                        SetNewSuffixStart(prevTerm, scratchTerm);

                        return true;
                    }
                    else if (newSuffixStart != 0 || scratchTerm.length != 0)
                    {
                        if (DEBUG_SURROGATES)
                        {
                            Console.Out.WriteLine("      got term=null (or next field)");
                        }
                        newSuffixStart = 0;
                        scratchTerm.length = 0;
                        return true;
                    }
                }

                return false;
            }

            // Pre-flex indices store terms in UTF16 sort order, but
            // certain queries require Unicode codepoint order; this
            // method carefully seeks around surrogates to handle
            // this impedance mismatch

            private void SurrogateDance()
            {

                if (!unicodeSortOrder)
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
                if (termEnum.Term == null || termEnum.Term.Field != internedFieldName)
                {
                    scratchTerm.length = 0;
                }
                else
                {
                    scratchTerm.CopyBytes(termEnum.Term.Bytes);
                }

                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("  dance");
                    Console.Out.WriteLine("    prev=" + UnicodeUtil.ToHexString(prevTerm.Utf8ToString()));
                    Console.Out.WriteLine("         " + prevTerm.ToString());
                    Console.Out.WriteLine("    term=" + UnicodeUtil.ToHexString(scratchTerm.Utf8ToString()));
                    Console.Out.WriteLine("         " + scratchTerm.ToString());
                }

                // This code assumes TermInfosReader/SegmentTermEnum
                // always use BytesRef.offset == 0
                //assert prevTerm.offset == 0;
                //assert scratchTerm.offset == 0;

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
                    Console.Out.WriteLine("  finish bmp ends");
                }

                DoPushes();
            }

            // Look for seek type 1 ("push"): if the newly added
            // suffix contains any S, we must try to seek to the
            // corresponding E.  If we find a match, we go there;
            // else we keep looking for additional S's in the new
            // suffix.  This "starts" the dance, at this character
            // position:
            private void DoPushes()
            {

                int upTo = newSuffixStart;
                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("  try push newSuffixStart=" + newSuffixStart + " scratchLen=" + scratchTerm.length);
                }

                while (upTo < scratchTerm.length)
                {
                    if (IsNonBMPChar(scratchTerm.bytes, upTo) &&
                        (upTo > newSuffixStart ||
                         (upTo >= prevTerm.length ||
                          (!IsNonBMPChar(prevTerm.bytes, upTo) &&
                           !IsHighBMPChar(prevTerm.bytes, upTo)))))
                    {

                        // A non-BMP char (4 bytes UTF8) starts here:
                        //assert scratchTerm.length >= upTo + 4;

                        int savLength = scratchTerm.length;
                        scratch[0] = scratchTerm.bytes[upTo];
                        scratch[1] = scratchTerm.bytes[upTo + 1];
                        scratch[2] = scratchTerm.bytes[upTo + 2];

                        scratchTerm.bytes[upTo] = UTF8_HIGH_BMP_LEAD;
                        scratchTerm.bytes[upTo + 1] = unchecked((sbyte)0x80);
                        scratchTerm.bytes[upTo + 2] = unchecked((sbyte)0x80);
                        scratchTerm.length = upTo + 3;

                        if (DEBUG_SURROGATES)
                        {
                            Console.Out.WriteLine("    try seek 1 pos=" + upTo + " term=" + UnicodeUtil.ToHexString(scratchTerm.Utf8ToString()) + " " + scratchTerm.ToString() + " len=" + scratchTerm.length);
                        }

                        // Seek "forward":
                        // TODO: more efficient seek?
                        parent.TermsDict.SeekEnum(seekTermEnum, new Term(fieldInfo.name, scratchTerm), true);

                        scratchTerm.bytes[upTo] = scratch[0];
                        scratchTerm.bytes[upTo + 1] = scratch[1];
                        scratchTerm.bytes[upTo + 2] = scratch[2];
                        scratchTerm.length = savLength;

                        // Did we find a match?
                        Term t2 = seekTermEnum.Term;

                        if (DEBUG_SURROGATES)
                        {
                            if (t2 == null)
                            {
                                Console.Out.WriteLine("      hit term=null");
                            }
                            else
                            {
                                Console.Out.WriteLine("      hit term=" + UnicodeUtil.ToHexString(t2.Text) + " " + (t2 == null ? null : t2.Bytes));
                            }
                        }

                        // Since this was a seek "forward", we could hit
                        // EOF or a different field:
                        bool matches;

                        if (t2 != null && t2.Field == internedFieldName)
                        {
                            BytesRef b2 = t2.Bytes;
                            //assert b2.offset == 0;
                            if (b2.length >= upTo + 3 && IsHighBMPChar(b2.bytes, upTo))
                            {
                                matches = true;
                                for (int i = 0; i < upTo; i++)
                                {
                                    if (scratchTerm.bytes[i] != b2.bytes[i])
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
                                Console.Out.WriteLine("      matches!");
                            }

                            // OK seek "back"
                            // TODO: more efficient seek?
                            parent.TermsDict.SeekEnum(termEnum, seekTermEnum.Term, true);

                            scratchTerm.CopyBytes(seekTermEnum.Term.Bytes);

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

            private bool unicodeSortOrder;

            internal void Reset(FieldInfo fieldInfo)
            {
                //Console.Out.WriteLine("pff.reset te=" + termEnum);
                this.fieldInfo = fieldInfo;
                internedFieldName = string.Intern(fieldInfo.name);
                Term term = new Term(internedFieldName);
                if (termEnum == null)
                {
                    termEnum = parent.TermsDict.Terms(term);
                    seekTermEnum = parent.TermsDict.Terms(term);
                    //Console.Out.WriteLine("  term=" + termEnum.term());
                }
                else
                {
                    parent.TermsDict.SeekEnum(termEnum, term, true);
                }
                skipNext = true;

                unicodeSortOrder = parent.SortTermsByUnicode;

                Term t = termEnum.Term;
                if (t != null && t.Field == internedFieldName)
                {
                    newSuffixStart = 0;
                    prevTerm.length = 0;
                    SurrogateDance();
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    // Pre-flex indexes always sorted in UTF16 order, but
                    // we remap on-the-fly to unicode order
                    if (unicodeSortOrder)
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
                throw new NotSupportedException();
            }

            public override long Ord
            {
                get { throw new NotSupportedException(); }
            }

            public override SeekStatus SeekCeil(BytesRef term, bool useCache)
            {
                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("TE.seek target=" + UnicodeUtil.ToHexString(term.Utf8ToString()));
                }
                skipNext = false;
                TermInfosReader tis = parent.TermsDict;
                Term t0 = new Term(fieldInfo.name, term);

                //assert termEnum != null;

                tis.SeekEnum(termEnum, t0, useCache);

                Term t = termEnum.Term;

                if (t != null && t.Field == internedFieldName && term.BytesEquals(t.Bytes))
                {
                    // If we found an exact match, no need to do the
                    // surrogate dance
                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  seek exact match");
                    }
                    current = t.Bytes;
                    return SeekStatus.FOUND;
                }
                else if (t == null || t.Field != internedFieldName)
                {

                    // TODO: maybe we can handle this like the next()
                    // into null?  set term as prevTerm then dance?

                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  seek hit EOF");
                    }

                    // We hit EOF; try end-case surrogate dance: if we
                    // find an E, try swapping in S, backwards:
                    scratchTerm.CopyBytes(term);

                    //assert scratchTerm.offset == 0;

                    for (int i = scratchTerm.length - 1; i >= 0; i--)
                    {
                        if (IsHighBMPChar(scratchTerm.bytes, i))
                        {
                            if (DEBUG_SURROGATES)
                            {
                                Console.Out.WriteLine("    found E pos=" + i + "; try seek");
                            }

                            if (SeekToNonBMP(seekTermEnum, scratchTerm, i))
                            {

                                scratchTerm.CopyBytes(seekTermEnum.Term.Bytes);
                                parent.TermsDict.SeekEnum(termEnum, seekTermEnum.Term, useCache);

                                newSuffixStart = 1 + i;

                                DoPushes();

                                // Found a match
                                // TODO: faster seek?
                                current = termEnum.Term.Bytes;
                                return SeekStatus.NOT_FOUND;
                            }
                        }
                    }

                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  seek END");
                    }

                    current = null;
                    return SeekStatus.END;
                }
                else
                {

                    // We found a non-exact but non-null term; this one
                    // is fun -- just treat it like next, by pretending
                    // requested term was prev:
                    prevTerm.CopyBytes(term);

                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  seek hit non-exact term=" + UnicodeUtil.ToHexString(t.Text));
                    }

                    BytesRef br = t.Bytes;
                    //assert br.offset == 0;

                    SetNewSuffixStart(term, br);

                    SurrogateDance();

                    Term t2 = termEnum.Term;
                    if (t2 == null || t2.Field != internedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        //assert t2 == null || !t2.field().equals(internedFieldName);
                        current = null;
                        return SeekStatus.END;
                    }
                    else
                    {
                        current = t2.Bytes;
                        //assert !unicodeSortOrder || term.compareTo(current) < 0 : "term=" + UnicodeUtil.toHexString(term.utf8ToString()) + " vs current=" + UnicodeUtil.toHexString(current.utf8ToString());
                        return SeekStatus.NOT_FOUND;
                    }
                }
            }

            private void SetNewSuffixStart(BytesRef br1, BytesRef br2)
            {
                int limit = Math.Min(br1.length, br2.length);
                int lastStart = 0;
                for (int i = 0; i < limit; i++)
                {
                    if ((br1.bytes[br1.offset + i] & 0xc0) == 0xc0 || (br1.bytes[br1.offset + i] & 0x80) == 0)
                    {
                        lastStart = i;
                    }
                    if (br1.bytes[br1.offset + i] != br2.bytes[br2.offset + i])
                    {
                        newSuffixStart = lastStart;
                        if (DEBUG_SURROGATES)
                        {
                            Console.Out.WriteLine("    set newSuffixStart=" + newSuffixStart);
                        }
                        return;
                    }
                }
                newSuffixStart = limit;
                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("    set newSuffixStart=" + newSuffixStart);
                }
            }

            public override BytesRef Next()
            {
                if (DEBUG_SURROGATES)
                {
                    Console.Out.WriteLine("TE.next()");
                }
                if (skipNext)
                {
                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  skipNext=true");
                    }
                    skipNext = false;
                    if (termEnum.Term == null)
                    {
                        return null;
                        // PreFlex codec interns field names:
                    }
                    else if (termEnum.Term.Field != internedFieldName)
                    {
                        return null;
                    }
                    else
                    {
                        return current = termEnum.Term.Bytes;
                    }
                }

                // TODO: can we use STE's prevBuffer here?
                prevTerm.CopyBytes(termEnum.Term.Bytes);

                if (termEnum.Next() && termEnum.Term.Field == internedFieldName)
                {
                    newSuffixStart = termEnum.newSuffixStart;
                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  newSuffixStart=" + newSuffixStart);
                    }
                    SurrogateDance();
                    Term t = termEnum.Term;
                    if (t == null || t.Field != internedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        //assert t == null || !t.field().equals(internedFieldName);
                        current = null;
                    }
                    else
                    {
                        current = t.Bytes;
                    }
                    return current;
                }
                else
                {
                    // This field is exhausted, but we have to give
                    // surrogateDance a chance to seek back:
                    if (DEBUG_SURROGATES)
                    {
                        Console.Out.WriteLine("  force cont");
                    }
                    //newSuffixStart = prevTerm.length;
                    newSuffixStart = 0;
                    SurrogateDance();

                    Term t = termEnum.Term;
                    if (t == null || t.Field != internedFieldName)
                    {
                        // PreFlex codec interns field names; verify:
                        //assert t == null || !t.field().equals(internedFieldName);
                        return null;
                    }
                    else
                    {
                        current = t.Bytes;
                        return current;
                    }
                }
            }

            public override BytesRef Term
            {
                get { return current; }
            }

            public override int DocFreq
            {
                get { return termEnum.DocFreq; }
            }

            public override long TotalTermFreq
            {
                get { return -1; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                PreDocsEnum docsEnum;
                if (reuse == null || !(reuse is PreDocsEnum))
                {
                    docsEnum = new PreDocsEnum(parent);
                }
                else
                {
                    docsEnum = (PreDocsEnum)reuse;
                    if (docsEnum.FreqStream != parent.freqStream)
                    {
                        docsEnum = new PreDocsEnum(parent);
                    }
                }
                return docsEnum.Reset(termEnum, liveDocs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                PreDocsAndPositionsEnum docsPosEnum;
                if (fieldInfo.IndexOptionsValue != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    return null;
                }
                else if (reuse == null || !(reuse is PreDocsAndPositionsEnum))
                {
                    docsPosEnum = new PreDocsAndPositionsEnum(parent);
                }
                else
                {
                    docsPosEnum = (PreDocsAndPositionsEnum)reuse;
                    if (docsPosEnum.FreqStream != parent.freqStream)
                    {
                        docsPosEnum = new PreDocsAndPositionsEnum(parent);
                    }
                }
                return docsPosEnum.Reset(termEnum, liveDocs);
            }
        }

        private sealed class PreDocsEnum : DocsEnum
        {
            private readonly Lucene3xFields parent;
            private readonly SegmentTermDocs docs;
            private int docID = -1;

            internal PreDocsEnum(Lucene3xFields parent)
            {
                this.parent = parent;
                docs = new SegmentTermDocs(parent.freqStream, parent.TermsDict, parent.fieldInfos);
            }

            internal IndexInput FreqStream
            {
                get
                {
                    return parent.freqStream;
                }
            }

            public PreDocsEnum Reset(SegmentTermEnum termEnum, IBits liveDocs)
            {
                docs.LiveDocs = liveDocs;
                docs.Seek(termEnum);
                docs.freq = 1;
                docID = -1;
                return this;
            }

            public override int NextDoc()
            {
                if (docs.Next())
                {
                    return docID = docs.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                if (docs.SkipTo(target))
                {
                    return docID = docs.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Freq
            {
                get { return docs.Freq; }
            }

            public override int DocID
            {
                get { return docID; }
            }

            public override long Cost
            {
                get { return docs.df; }
            }
        }

        private sealed class PreDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene3xFields parent;

            private readonly SegmentTermPositions pos;
            private int docID = -1;

            internal PreDocsAndPositionsEnum(Lucene3xFields parent)
            {
                this.parent = parent;
                pos = new SegmentTermPositions(parent.freqStream, parent.proxStream, parent.TermsDict, parent.fieldInfos);
            }

            internal IndexInput FreqStream
            {
                get
                {
                    return parent.freqStream;
                }
            }

            public DocsAndPositionsEnum Reset(SegmentTermEnum termEnum, IBits liveDocs)
            {
                pos.LiveDocs = liveDocs;
                pos.Seek(termEnum);
                docID = -1;
                return this;
            }

            public override int NextDoc()
            {
                if (pos.Next())
                {
                    return docID = pos.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Advance(int target)
            {
                if (pos.SkipTo(target))
                {
                    return docID = pos.Doc;
                }
                else
                {
                    return docID = NO_MORE_DOCS;
                }
            }

            public override int Freq
            {
                get { return pos.Freq; }
            }

            public override int DocID
            {
                get { return docID; }
            }

            public override int NextPosition()
            {
                //assert docID != NO_MORE_DOCS;
                return pos.NextPosition();
            }

            public override int StartOffset
            {
                get { return -1; }
            }

            public override int EndOffset
            {
                get { return -1; }
            }

            public override BytesRef Payload
            {
                get { return pos.Payload; }
            }

            public override long Cost
            {
                get { return pos.df; }
            }
        }
    }
}
