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
// Scanner

using System.IO;

namespace TagSoup
{
    /// <summary>
    /// An interface allowing <see cref="Parser"/> to invoke scanners.
    /// </summary>
    public interface IScanner
    {
        /// <summary>
        /// Invoke a scanner.
        /// </summary>
        /// <param name="br">
        /// A source of characters to scan
        /// </param>
        /// <param name="handler">
        /// A <see cref="IScanHandler"/> to report events to
        /// </param>
        void Scan(TextReader br, IScanHandler handler);

        /// <summary>
        /// Reset the embedded locator.
        /// </summary>
        /// <param name="publicId">
        /// The publicId of the source
        /// </param>
        /// <param name="systemId">
        /// The systemId of the source
        /// </param>
        void ResetDocumentLocator(string publicId, string systemId);

        /// <summary>
        /// Signal to the scanner to start CDATA content mode.
        /// </summary>
        void StartCDATA();
    }
}
