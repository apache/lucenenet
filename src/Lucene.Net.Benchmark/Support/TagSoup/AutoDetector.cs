// This file is part of TagSoup and is Copyright 2002-2008 by John Cowan.
//
// TagSoup is licensed under the Apache License,
// Version 2.0.  You may obtain a copy of this license at
// http://www.apache.org/licenses/LICENSE-2.0 .  You may also have
// additional legal rights not granted by this license.
//
// TagSoup is distributed in the hope that it will be useful, but
// unless required by applicable law or agreed to in writing, TagSoup
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, either express or implied; not even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// 
// 
// Interface to objects that translate InputStreams to Readers by auto-detection

using System.IO;

namespace TagSoup
{
    /// <summary>
    /// Classes which accept an <see cref="Stream"/> and provide a <see cref="TextReader"/> which figures
    /// out the encoding of the <see cref="Stream"/> and reads characters from it should
    /// conform to this interface.
    /// </summary>
    /// <seealso cref="Stream" />
    /// <seealso cref="TextReader" />
    public interface IAutoDetector
    {
        /// <summary>
        /// Given a <see cref="Stream"/>, return a suitable <see cref="TextReader"/> that understands
        /// the presumed character encoding of that <see cref="Stream"/>.
        /// If bytes are consumed from the <see cref="Stream"/> in the process, they
        /// <i>must</i> be pushed back onto the InputStream so that they can be
        /// reinterpreted as characters.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/></param>
        /// <returns>A <see cref="TextReader"/> that reads from the <see cref="Stream"/></returns>
        TextReader AutoDetectingReader(Stream stream);
    }
}
