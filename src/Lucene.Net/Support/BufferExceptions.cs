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

using System;
#if FEATURE_SERIALIZABLE
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class BufferUnderflowException : Exception
    {
        public BufferUnderflowException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public BufferUnderflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class BufferOverflowException : Exception
    {
        public BufferOverflowException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public BufferOverflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class ReadOnlyBufferException : Exception
    {
        public ReadOnlyBufferException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public ReadOnlyBufferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class InvalidMarkException : Exception
    {
        public InvalidMarkException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public InvalidMarkException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}