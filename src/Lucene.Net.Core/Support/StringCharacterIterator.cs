/*
 * Copyright (c) 1996, 2013, Oracle and/or its affiliates. All rights reserved.
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

/*
 * (C) Copyright Taligent, Inc. 1996, 1997 - All Rights Reserved
 * (C) Copyright IBM Corp. 1996 - 1998 - All Rights Reserved
 *
 * The original version of this source code and documentation
 * is copyrighted and owned by Taligent, Inc., a wholly-owned
 * subsidiary of IBM. These materials are provided under terms
 * of a License Agreement between Taligent and Sun. This technology
 * is protected by multiple US and International patents.
 *
 * This notice and attribution to Taligent may not be removed.
 * Taligent is a registered trademark of Taligent, Inc.
 *
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    /// <summary>
    /// <see cref="StringCharacterIterator"/> implements the
    /// <see cref="CharacterIterator"/> protocol for a <see cref="string"/>.
    /// The <see cref="StringCharacterIterator"/> class iterates over the
    /// entire <see cref="string"/>.
    /// </summary>
    /// <seealso cref="CharacterIterator"/>
    public class StringCharacterIterator : CharacterIterator
    {
        private string text;
        private int begin;
        private int end;
        // invariant: begin <= pos <= end
        private int pos;


        public StringCharacterIterator(string text)
            : this(text, 0)
        {
        }

        public StringCharacterIterator(string text, int pos)
            : this(text, 0, text.Length, pos)
        {
        }

        public StringCharacterIterator(string text, int begin, int end, int pos)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            this.text = text;

            if (begin < 0 || begin > end || end > text.Length)
                throw new ArgumentException("Invalid substring range");

            if (pos < begin || pos > end)
                throw new ArgumentException("Invalid position");

            this.begin = begin;
            this.end = end;
            this.pos = pos;
        }

        public void SetText(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            this.text = text;
            this.begin = 0;
            this.end = text.Length;
            this.pos = 0;
        }

        public override char First()
        {
            pos = begin;
            return Current;
        }

        public override char Last()
        {
            if (end != begin)
            {
                pos = end - 1;
            }
            else
            {
                pos = end;
            }
            return Current;
        }

        public override char SetIndex(int position)
        {
            if (position < begin || position > end)
                throw new ArgumentException("Invalid index");
            pos = position;
            return Current;
        }

        public override char Current
        {
            get
            {
                if (pos >= begin && pos < end)
                {
                    return text[pos];
                }
                else
                {
                    return DONE;
                }
            }
        }

        public override char Next()
        {
            if (pos < end - 1)
            {
                pos++;
                return text[pos];
            }
            else
            {
                pos = end;
                return DONE;
            }
        }

        public override char Previous()
        {
            if (pos > begin)
            {
                pos--;
                return text[pos];
            }
            else
            {
                return DONE;
            }
        }


        public override int BeginIndex
        {
            get
            {
                return begin;
            }
        }

        public override int EndIndex
        {
            get
            {
                return end;
            }
        }

        public override int Index
        {
            get
            {
                return pos;
            }
        }

        public override string GetTextAsString()
        {
            return text;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!(obj is StringCharacterIterator))
            return false;

            StringCharacterIterator that = (StringCharacterIterator)obj;

            if (GetHashCode() != that.GetHashCode())
                return false;
            if (!text.Equals(that.text))
                return false;
            if (pos != that.pos || begin != that.begin || end != that.end)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ pos ^ begin ^ end;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
