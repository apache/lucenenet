using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal sealed class SegmentTermEnum : ICloneable, IDisposable
    {
        private IndexInput input;
        internal FieldInfos fieldInfos;
        internal long size;
        internal long position = -1;

        // Changed strings to true utf8 with length-in-bytes not
        // length-in-chars
        public const int FORMAT_VERSION_UTF8_LENGTH_IN_BYTES = -4;

        // NOTE: always change this if you switch to a new format!
        // whenever you add a new format, make it 1 smaller (negative version logic)!
        public const int FORMAT_CURRENT = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        // when removing support for old versions, leave the last supported version here
        public const int FORMAT_MINIMUM = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        private TermBuffer termBuffer = new TermBuffer();
        private TermBuffer prevBuffer = new TermBuffer();
        private TermBuffer scanBuffer = new TermBuffer(); // used for scanning

        internal TermInfo termInfo = new TermInfo();

        private int format;
        private bool isIndex = false;
        internal long indexPointer = 0;
        internal int indexInterval;
        internal int skipInterval;
        internal int newSuffixStart;
        internal int maxSkipLevels;
        private bool first = true;

        internal SegmentTermEnum(IndexInput i, FieldInfos fis, bool isi)
        {
            input = i;
            fieldInfos = fis;
            isIndex = isi;
            maxSkipLevels = 1; // use single-level skip lists for formats > -3 

            int firstInt = input.ReadInt();
            if (firstInt >= 0)
            {
                // original-format file, without explicit format version number
                format = 0;
                size = firstInt;

                // back-compatible settings
                indexInterval = 128;
                skipInterval = int.MaxValue; // switch off skipTo optimization
            }
            else
            {
                // we have a format version number
                format = firstInt;

                // check that it is a format we can understand
                if (format > FORMAT_MINIMUM)
                    throw new IndexFormatTooOldException(input, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                if (format < FORMAT_CURRENT)
                    throw new IndexFormatTooNewException(input, format, FORMAT_MINIMUM, FORMAT_CURRENT);

                size = input.ReadLong();                    // read the size

                indexInterval = input.ReadInt();
                skipInterval = input.ReadInt();
                maxSkipLevels = input.ReadInt();
                //assert indexInterval > 0: "indexInterval=" + indexInterval + " is negative; must be > 0";
                //assert skipInterval > 0: "skipInterval=" + skipInterval + " is negative; must be > 0";
            }
        }

        public object Clone()
        {
            SegmentTermEnum clone = null;
            try
            {
                clone = (SegmentTermEnum)base.MemberwiseClone();
            }
            catch { }

            clone.input = (IndexInput)input.Clone();
            clone.termInfo = new TermInfo(termInfo);

            clone.termBuffer = (TermBuffer)termBuffer.Clone();
            clone.prevBuffer = (TermBuffer)prevBuffer.Clone();
            clone.scanBuffer = new TermBuffer();

            return clone;
        }

        internal void Seek(long pointer, long p, Term t, TermInfo ti)
        {
            input.Seek(pointer);
            position = p;
            termBuffer.set(t);
            prevBuffer.reset();
            //System.out.println("  ste doSeek prev=" + prevBuffer.toTerm() + " this=" + this);
            termInfo.Set(ti);
            first = p == -1;
        }

        public bool Next()
        {
            prevBuffer.Set(termBuffer);
            //System.out.println("  ste setPrev=" + prev() + " this=" + this);

            if (position++ >= size - 1)
            {
                termBuffer.Reset();
                //System.out.println("    EOF");
                return false;
            }

            termBuffer.Read(input, fieldInfos);
            newSuffixStart = termBuffer.newSuffixStart;

            termInfo.docFreq = input.ReadVInt();    // read doc freq
            termInfo.freqPointer += input.ReadVLong();    // read freq pointer
            termInfo.proxPointer += input.ReadVLong();    // read prox pointer

            if (termInfo.docFreq >= skipInterval)
                termInfo.skipOffset = input.ReadVInt();

            if (isIndex)
                indexPointer += input.ReadVLong();    // read index pointer

            //System.out.println("  ste ret term=" + term());
            return true;
        }

        internal int ScanTo(Term term)
        {
            scanBuffer.Set(term);
            int count = 0;
            if (first)
            {
                // Always force initial next() in case term is
                // Term("", "")
                Next();
                first = false;
                count++;
            }
            while (scanBuffer.CompareTo(termBuffer) > 0 && Next())
            {
                count++;
            }
            return count;
        }

        public Term Term
        {
            get
            {
                return termBuffer.ToTerm();
            }
        }

        internal Term Prev
        {
            get
            {
                return prevBuffer.ToTerm();
            }
        }

        internal TermInfo TermInfo
        {
            get
            {
                return new TermInfo(termInfo);
            }
        }

        internal void TermInfo(TermInfo ti)
        {
            ti.Set(termInfo);
        }

        public int DocFreq
        {
            get
            {
                return termInfo.docFreq;
            }
        }

        internal long FreqPointer
        {
            get
            {
                return termInfo.freqPointer;
            }
        }

        internal long ProxPointer
        {
            get
            {
                return termInfo.proxPointer;
            }
        }

        public void Dispose()
        {
            input.Dispose();
        }
    }
}
