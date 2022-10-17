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
// Scanner handler

namespace TagSoup
{
    /// <summary>
    /// An interface that Scanners use to report events in the input stream.
    /// </summary>
    public interface IScanHandler
    {
        /// <summary>
        /// Reports an attribute name without a value.
        /// </summary>
        void Adup(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports an attribute name; a value will follow.
        /// </summary>
        void Aname(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports an attribute value.
        /// </summary>
        void Aval(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports the content of a CDATA section (not a CDATA element)
        /// </summary>
        void CDSect(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports a &lt;!....&gt; declaration - typically a DOCTYPE
        /// </summary>
        void Decl(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports an entity reference or character reference.
        /// </summary>
        void Entity(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports EOF.
        /// </summary>
        void EOF(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports an end-tag.
        /// </summary>
        void ETag(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports the general identifier (element type name) of a start-tag.
        /// </summary>
        void GI(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports character content.
        /// </summary>
        void PCDATA(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports the data part of a processing instruction.
        /// </summary>
        void PI(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports the target part of a processing instruction.
        /// </summary>
        void PITarget(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports the close of a start-tag.
        /// </summary>
        void STagC(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports the close of an empty-tag.
        /// </summary>
        void STagE(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Reports a comment.
        /// </summary>
        void Cmnt(char[] buffer, int startIndex, int length);

        /// <summary>
        /// Returns the value of the last entity or character reference reported.
        /// </summary>
        /// <returns>The value of the last entity or character reference reported.</returns>
        int GetEntity();
    }
}
