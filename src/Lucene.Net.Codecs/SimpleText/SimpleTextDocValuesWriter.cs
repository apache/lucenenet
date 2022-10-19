using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.SimpleText
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

    using BytesRef = Util.BytesRef;
    using DocValuesType = Index.DocValuesType;
    using FieldInfo = Index.FieldInfo;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOutput = Store.IndexOutput;
    using IOUtils = Util.IOUtils;
    using SegmentWriteState = Index.SegmentWriteState;

    public class SimpleTextDocValuesWriter : DocValuesConsumer // LUCENENET NOTE: changed from internal to public because it is subclassed by a public type
    {
        internal static readonly BytesRef END = new BytesRef("END");
        internal static readonly BytesRef FIELD = new BytesRef("field ");
        internal static readonly BytesRef TYPE = new BytesRef("  type ");

        // used for numerics
        internal static readonly BytesRef MINVALUE = new BytesRef("  minvalue ");
        internal static readonly BytesRef PATTERN = new BytesRef("  pattern ");

        // used for bytes
        internal static readonly BytesRef LENGTH = new BytesRef("length ");
        internal static readonly BytesRef MAXLENGTH = new BytesRef("  maxlength ");

        // used for sorted bytes
        internal static readonly BytesRef NUMVALUES = new BytesRef("  numvalues ");
        internal static readonly BytesRef ORDPATTERN = new BytesRef("  ordpattern ");

#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput data;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly BytesRef scratch = new BytesRef();
        private readonly int numDocs;
        private readonly ISet<string> _fieldsSeen = new JCG.HashSet<string>(); // for asserting

        // LUCENENET NOTE: Changed from public to internal because the class had to be made public, but is not for public use.
        internal SimpleTextDocValuesWriter(SegmentWriteState state, string ext)
        {
            data = state.Directory.CreateOutput(
                    IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, ext), state.Context);
            numDocs = state.SegmentInfo.DocCount;
        }

        /// <summary>
        /// For Asserting
        /// </summary>
        private bool FieldSeen(string field)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!_fieldsSeen.Contains(field), "field \"{0}\" was added more than once during flush", field);
            _fieldsSeen.Add(field);
            return true;
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(FieldSeen(field.Name));
                Debugging.Assert(field.DocValuesType == DocValuesType.NUMERIC ||
                             field.NormType == DocValuesType.NUMERIC);
            }
            WriteFieldEntry(field, DocValuesType.NUMERIC);

            // first pass to find min/max
            var minValue = long.MaxValue;
            var maxValue = long.MinValue;
            foreach (var n in values)
            {
                var v = n.GetValueOrDefault();
                minValue = Math.Min(minValue, v);
                maxValue = Math.Max(maxValue, v);
            }

            // write our minimum value to the .dat, all entries are deltas from that
            SimpleTextUtil.Write(data, MINVALUE);
            SimpleTextUtil.Write(data, minValue.ToString(CultureInfo.InvariantCulture), scratch);
            SimpleTextUtil.WriteNewline(data);

            // build up our fixed-width "simple text packed ints" format
            var diffBig = (decimal)maxValue - (decimal)minValue; // LUCENENET specific - use decimal rather than BigInteger

            var maxBytesPerValue = diffBig.ToString(CultureInfo.InvariantCulture).Length;
            var sb = new StringBuilder();
            for (var i = 0; i < maxBytesPerValue; i++)
                sb.Append('0');

            var patternString = sb.ToString(); // LUCENENET specific - only get the string once

            // write our pattern to the .dat
            SimpleTextUtil.Write(data, PATTERN);
            SimpleTextUtil.Write(data, patternString, scratch);
            SimpleTextUtil.WriteNewline(data);

            int numDocsWritten = 0;

            // second pass to write the values
            foreach (var n in values)
            {
                long value = n.GetValueOrDefault();

                if (Debugging.AssertsEnabled) Debugging.Assert(value >= minValue);

                var delta = (decimal)value - (decimal)minValue; // LUCENENET specific - use decimal rather than BigInteger
                string s = delta.ToString(patternString, CultureInfo.InvariantCulture);
                if (Debugging.AssertsEnabled) Debugging.Assert(s.Length == patternString.Length);
                SimpleTextUtil.Write(data, s, scratch);
                SimpleTextUtil.WriteNewline(data);
                SimpleTextUtil.Write(data, n is null ? "F" : "T", scratch);
                SimpleTextUtil.WriteNewline(data);
                numDocsWritten++;
                if (Debugging.AssertsEnabled) Debugging.Assert(numDocsWritten <= numDocs);
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(numDocs == numDocsWritten, "numDocs={0} numDocsWritten={1}", numDocs, numDocsWritten);
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(FieldSeen(field.Name));
                Debugging.Assert(field.DocValuesType == DocValuesType.BINARY);
            }

            var maxLength = 0;
            foreach (var value in values)
            {
                var length = value is null ? 0 : value.Length;
                maxLength = Math.Max(maxLength, length);
            }
            WriteFieldEntry(field, DocValuesType.BINARY);

            // write maxLength
            SimpleTextUtil.Write(data, MAXLENGTH);
            SimpleTextUtil.Write(data, maxLength.ToString(CultureInfo.InvariantCulture), scratch);
            SimpleTextUtil.WriteNewline(data);

            var maxBytesLength = maxLength.ToString(CultureInfo.InvariantCulture).Length;
            var sb = new StringBuilder();
            for (var i = 0; i < maxBytesLength; i++)
            {
                sb.Append('0');
            }
            // write our pattern for encoding lengths
            var patternString = sb.ToString();

            SimpleTextUtil.Write(data, PATTERN);
            SimpleTextUtil.Write(data, patternString, scratch);
            SimpleTextUtil.WriteNewline(data);
            
           
            int numDocsWritten = 0;
            foreach (BytesRef value in values)
            {
                int length = value is null ? 0 : value.Length;
                SimpleTextUtil.Write(data, LENGTH);
                SimpleTextUtil.Write(data, length.ToString(patternString, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(data);

                // write bytes -- don't use SimpleText.Write
                // because it escapes:
                if (value != null)
                {
                    data.WriteBytes(value.Bytes, value.Offset, value.Length);
                }

                // pad to fit
                for (int i = length; i < maxLength; i++)
                {
                    data.WriteByte((byte) ' ');
                }
                SimpleTextUtil.WriteNewline(data);
                SimpleTextUtil.Write(data, value is null ? "F" : "T", scratch);
                SimpleTextUtil.WriteNewline(data);
                numDocsWritten++;
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(numDocs == numDocsWritten);
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(FieldSeen(field.Name));
                Debugging.Assert(field.DocValuesType == DocValuesType.SORTED);
            }
            WriteFieldEntry(field, DocValuesType.SORTED);

            int valueCount = 0;
            int maxLength = -1;
            foreach (BytesRef value in values)
            {
                maxLength = Math.Max(maxLength, value.Length);
                valueCount++;
            }

            // write numValues
            SimpleTextUtil.Write(data, NUMVALUES);
            SimpleTextUtil.Write(data, valueCount.ToString(CultureInfo.InvariantCulture), scratch);
            SimpleTextUtil.WriteNewline(data);

            // write maxLength
            SimpleTextUtil.Write(data, MAXLENGTH);
            SimpleTextUtil.Write(data, maxLength.ToString(CultureInfo.InvariantCulture), scratch);
            SimpleTextUtil.WriteNewline(data);

            int maxBytesLength = maxLength.ToString(CultureInfo.InvariantCulture).Length;
            var sb = new StringBuilder();
            for (int i = 0; i < maxBytesLength; i++)
            {
                sb.Append('0');
            }

            // write our pattern for encoding lengths
            SimpleTextUtil.Write(data, PATTERN);
            SimpleTextUtil.Write(data, sb.ToString(), scratch);
            SimpleTextUtil.WriteNewline(data);

            var encoderFormat = sb.ToString();

            int maxOrdBytes = (valueCount + 1L).ToString(CultureInfo.InvariantCulture).Length;
            sb.Length = 0;
            for (int i = 0; i < maxOrdBytes; i++)
            {
                sb.Append('0');
            }

            // write our pattern for ords
            SimpleTextUtil.Write(data, ORDPATTERN);
            SimpleTextUtil.Write(data, sb.ToString(), scratch);
            SimpleTextUtil.WriteNewline(data);

            var ordEncoderFormat = sb.ToString();

            // for asserts:
            int valuesSeen = 0;

            foreach (BytesRef value in values)
            {
                // write length
                SimpleTextUtil.Write(data, LENGTH);
                SimpleTextUtil.Write(data, value.Length.ToString(encoderFormat, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(data);

                // write bytes -- don't use SimpleText.Write
                // because it escapes:
                data.WriteBytes(value.Bytes, value.Offset, value.Length);

                // pad to fit
                for (int i = value.Length; i < maxLength; i++)
                {
                    data.WriteByte((byte)' ');
                }
                SimpleTextUtil.WriteNewline(data);
                valuesSeen++;
                if (Debugging.AssertsEnabled) Debugging.Assert(valuesSeen <= valueCount);
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(valuesSeen == valueCount);

            foreach (var ord in docToOrd)
            {
                SimpleTextUtil.Write(data, (ord + 1).GetValueOrDefault().ToString(ordEncoderFormat, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(data);
            }
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values,
            IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(FieldSeen(field.Name));
                Debugging.Assert(field.DocValuesType == DocValuesType.SORTED_SET);
            }
            WriteFieldEntry(field, DocValuesType.SORTED_SET);

            long valueCount = 0;
            int maxLength = 0;
            foreach (var value in values)
            {
                maxLength = Math.Max(maxLength, value.Length);
                valueCount++;
            }

            // write numValues
            SimpleTextUtil.Write(data, NUMVALUES);
            SimpleTextUtil.Write(data, valueCount.ToString(CultureInfo.InvariantCulture), scratch);
            SimpleTextUtil.WriteNewline(data);

            // write maxLength
            SimpleTextUtil.Write(data, MAXLENGTH);
            SimpleTextUtil.Write(data, maxLength.ToString(CultureInfo.InvariantCulture), scratch);
            SimpleTextUtil.WriteNewline(data);

            int maxBytesLength = maxLength.ToString(CultureInfo.InvariantCulture).Length;
            var sb = new StringBuilder();
            for (int i = 0; i < maxBytesLength; i++)
            {
                sb.Append('0');
            }

            // write our pattern for encoding lengths
            SimpleTextUtil.Write(data, PATTERN);
            SimpleTextUtil.Write(data, sb.ToString(), scratch);
            SimpleTextUtil.WriteNewline(data);

            string encoderFormat = sb.ToString();

            // compute ord pattern: this is funny, we encode all values for all docs to find the maximum length
            var maxOrdListLength = 0;
            var sb2 = new StringBuilder();
            using (var ordStream = ords.GetEnumerator())
            {
                foreach (var n in docToOrdCount)
                {
                    sb2.Length = 0;
                    var count = (int)n;
                    for (int i = 0; i < count; i++)
                    {
                        ordStream.MoveNext();

                        var ord = ordStream.Current;
                        if (sb2.Length > 0)
                        {
                            sb2.Append(',');
                        }
                        sb2.Append(ord.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
                    }
                    maxOrdListLength = Math.Max(maxOrdListLength, sb2.Length);
                }
            }

            sb2.Length = 0;
            for (int i = 0; i < maxOrdListLength; i++)
            {
                sb2.Append('X');
            }

            // write our pattern for ord lists
            SimpleTextUtil.Write(data, ORDPATTERN);
            SimpleTextUtil.Write(data, sb2.ToString(), scratch);
            SimpleTextUtil.WriteNewline(data);

            // for asserts:
            long valuesSeen = 0;

            foreach (var value in values)
            {
                // write length
                SimpleTextUtil.Write(data, LENGTH);
                SimpleTextUtil.Write(data, value.Length.ToString(encoderFormat, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(data);

                // write bytes -- don't use SimpleText.Write
                // because it escapes:
                data.WriteBytes(value.Bytes, value.Offset, value.Length);

                // pad to fit
                for (var i = value.Length; i < maxLength; i++)
                {
                    data.WriteByte((byte)' ');
                }
                SimpleTextUtil.WriteNewline(data);
                valuesSeen++;
                if (Debugging.AssertsEnabled) Debugging.Assert(valuesSeen <= valueCount);
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(valuesSeen == valueCount);

            using (var ordStream = ords.GetEnumerator())
            {
                // write the ords for each doc comma-separated
                foreach (var n in docToOrdCount)
                {
                    sb2.Length = 0;
                    var count = (int)n;
                    for (var i = 0; i < count; i++)
                    {
                        ordStream.MoveNext();
                        var ord = ordStream.Current;
                        if (sb2.Length > 0)
                            sb2.Append(',');

                        sb2.Append(ord);
                    }
                    // now pad to fit: these are numbers so spaces work well. reader calls trim()
                    var numPadding = maxOrdListLength - sb2.Length;
                    for (var i = 0; i < numPadding; i++)
                    {
                        sb2.Append(' ');
                    }
                    SimpleTextUtil.Write(data, sb2.ToString(), scratch);
                    SimpleTextUtil.WriteNewline(data);
                }
            }
        }

        /// <summary>Write the header for this field. </summary>
        private void WriteFieldEntry(FieldInfo field, DocValuesType type)
        {
            SimpleTextUtil.Write(data, FIELD);
            SimpleTextUtil.Write(data, field.Name, scratch);
            SimpleTextUtil.WriteNewline(data);

            SimpleTextUtil.Write(data, TYPE);
            SimpleTextUtil.Write(data, type.ToString(), scratch);
            SimpleTextUtil.WriteNewline(data);
        }

        protected override void Dispose(bool disposing)
        {
            if (data is null || !disposing) return;
            var success = false;
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(_fieldsSeen.Count > 0);
                // java : sheisty to do this here?
                SimpleTextUtil.Write(data, END);
                SimpleTextUtil.WriteNewline(data);
                SimpleTextUtil.WriteChecksum(data, scratch);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data);
                }
                data = null;
            }
        }
    }
}