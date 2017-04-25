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
    /// Equivalent to Java's DataOutut interface
    /// </summary>
    public interface IDataOutput
    {
        void Write(int b);
        void Write(byte[] b);
        void Write(byte[] b, int off, int len);
        void WriteBoolean(bool v);
        void WriteByte(int v);

        /// <summary>
        /// NOTE: This was writeShort() in the JDK
        /// </summary>
        void WriteInt16(int v);
        void WriteChar(int v);

        /// <summary>
        /// NOTE: This was writeInt() in the JDK
        /// </summary>
        void WriteInt32(int v);

        /// <summary>
        /// NOTE: This was writeInt64() in the JDK
        /// </summary>
        void WriteInt64(long v);

        /// <summary>
        /// NOTE: This was writeSingle() in the JDK
        /// </summary>
        void WriteSingle(float v);
        void WriteDouble(double v);
        void WriteBytes(string s);
        void WriteChars(string s);
        void WriteUTF(string s);
    }
}
