/*
 * Copyright (c) 1999, 2008, Oracle and/or its affiliates. All rights reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Oracle designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Oracle in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
 * or visit www.oracle.com if you need additional information or have any
 * questions.
 */

namespace Lucene.Net.Support
{
    /// <summary>
    /// Equivalent to Java's DataInput interface
    /// </summary>
    public interface IDataInput
    {
        void ReadFully(byte[] b);
        void ReadFully(byte[] b, int off, int len);
        int SkipBytes(int n);
        bool ReadBoolean();

        /// <summary>
        /// NOTE: This was readByte() in the JDK
        /// </summary>
        int ReadSByte();

        /// <summary>
        /// NOTE: This was readUnsignedByte() in the JDK
        /// </summary>
        byte ReadByte();

        /// <summary>
        /// NOTE: This was readShort() in the JDK
        /// </summary>
        short ReadInt16();

        /// <summary>
        /// NOTE: This was readUnsignedShort() in the JDK
        /// </summary>
        int ReadUInt16();
        char ReadChar();

        /// <summary>
        /// NOTE: This was readInt() in the JDK
        /// </summary>
        int ReadInt32();

        /// <summary>
        /// NOTE: This was readLong() in the JDK
        /// </summary>
        long ReadInt64();
        float ReadSingle();
        double ReadDouble();
        string ReadLine();
        string ReadUTF();
    }
}
