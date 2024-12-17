#! /usr/bin/env python

# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the "License"); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# ## LUCENENET PORTING NOTES
# This script was originally written for Python 2, but has been tested against Python 3.
# No changes were necessary to run this script in Python 3.

HEADER="""using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

// This file has been automatically generated, DO NOT EDIT

namespace Lucene.Net.Util.Packed
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

    using DataInput = Lucene.Net.Store.DataInput;

"""

TYPES = {8: "byte", 16: "short"}
DOTNET_READ_TYPES = {8: "Byte", 16: "Int16"} # LUCENENET specific
MASKS = {8: " & 0xFFL", 16: " & 0xFFFFL", 32: " & 0xFFFFFFFFL", 64: ""}
CASTS = {8: "(byte)", 16: "(short)", 32: "(int)", 64: ""} # LUCENENET specific - removed space from casts to match existing C# port style

if __name__ == '__main__':
  for bpv in TYPES.keys():
    type
    f = open("Packed%dThreeBlocks.cs" %bpv, 'w')
    f.write(HEADER)
    f.write("""    /// <summary>
    /// Packs integers into 3 %ss (%d bits per value).
    /// <para/>
    /// @lucene.internal
    /// </summary>\n""" %(TYPES[bpv], bpv*3))
    f.write("    internal sealed class Packed%dThreeBlocks : PackedInt32s.MutableImpl\n" %bpv)
    f.write("    {\n")
    f.write("        private readonly %s[] blocks;\n\n" %TYPES[bpv])

    f.write("        public const int MAX_SIZE = int.MaxValue / 3;\n\n")

    f.write("        internal Packed%dThreeBlocks(int valueCount)\n" %bpv)
    f.write("            : base(valueCount, %d)\n" %(bpv*3))
    f.write("        {\n")
    f.write("            if (valueCount > MAX_SIZE)\n")
    f.write("            {\n")
    f.write("                throw new ArgumentOutOfRangeException(nameof(valueCount), \"MAX_SIZE exceeded\");\n")
    f.write("            }\n")
    f.write("            blocks = new %s[valueCount * 3];\n" %TYPES[bpv])
    f.write("        }\n\n")

    f.write("        internal Packed%dThreeBlocks(int packedIntsVersion, DataInput @in, int valueCount)\n" %bpv)
    f.write("            : this(valueCount)\n")
    f.write("        {\n")
    if bpv == 8:
      f.write("            @in.ReadBytes(blocks, 0, 3 * valueCount);\n")
    else:
      f.write("            for (int i = 0; i < 3 * valueCount; ++i)\n")
      f.write("            {\n")
      f.write("                blocks[i] = @in.Read%s();\n" %DOTNET_READ_TYPES[bpv].title()) # LUCENENET specific
      f.write("            }\n")
    f.write("            // because packed ints have not always been byte-aligned\n")
    f.write("            int remaining = (int)(PackedInt32s.Format.PACKED.ByteCount(packedIntsVersion, valueCount, %d) - 3L * valueCount * %d);\n" %(3 * bpv, bpv / 8))
    f.write("            for (int i = 0; i < remaining; ++i)\n")
    f.write("            {\n")
    f.write("                @in.ReadByte();\n")
    f.write("            }\n")
    f.write("        }\n")

    f.write("""
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Get(int index)
        {
            int o = index * 3;
            return (blocks[o]%s) << %d | (blocks[o + 1]%s) << %d | (blocks[o + 2]%s);
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < m_valueCount);
                Debugging.Assert(off + len <= arr.Length);
            }

            int gets = Math.Min(m_valueCount - index, len);
            for (int i = index * 3, end = (index + gets) * 3; i < end; i += 3)
            {
                arr[off++] = (blocks[i]%s) << %d | (blocks[i + 1]%s) << %d | (blocks[i + 2]%s);
            }
            return gets;
        }

        public override void Set(int index, long value)
        {
            int o = index * 3;
            blocks[o] = %s(value >>> %d);
            blocks[o + 1] = %s(value >>> %d);
            blocks[o + 2] = %svalue;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < m_valueCount);
                Debugging.Assert(off + len <= arr.Length);
            }

            int sets = Math.Min(m_valueCount - index, len);
            for (int i = off, o = index * 3, end = off + sets; i < end; ++i)
            {
                long value = arr[i];
                blocks[o++] = %s(value >>> %d);
                blocks[o++] = %s(value >>> %d);
                blocks[o++] = %svalue;
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            %s block1 = %s(val >>> %d);
            %s block2 = %s(val >>> %d);
            %s block3 = %sval;
            for (int i = fromIndex * 3, end = toIndex * 3; i < end; i += 3)
            {
                blocks[i] = block1;
                blocks[i + 1] = block2;
                blocks[i + 2] = block3;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Clear()
        {
            Arrays.Fill(blocks, %s0);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER
                + 2 * RamUsageEstimator.NUM_BYTES_INT32     // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // blocks ref
                + RamUsageEstimator.SizeOf(blocks);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(bitsPerValue=" + m_bitsPerValue + ", size=" + Count + ", elements.length=" + blocks.Length + ")";
        }
    }
}
""" %(MASKS[bpv], 2*bpv, MASKS[bpv], bpv, MASKS[bpv], MASKS[bpv], 2*bpv, MASKS[bpv], bpv, MASKS[bpv], CASTS[bpv], 2*bpv, CASTS[bpv], bpv, CASTS[bpv], CASTS[bpv],
      2*bpv, CASTS[bpv], bpv, CASTS[bpv], TYPES[bpv], CASTS[bpv], 2*bpv, TYPES[bpv],
      CASTS[bpv], bpv, TYPES[bpv], CASTS[bpv], CASTS[bpv]))

    f.close()
