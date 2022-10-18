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
// PYX Writer
// FIXME: does not do escapes in attribute values
// FIXME: outputs entities as bare '&' character

using Sax;
using Sax.Ext;
using System;
using System.IO;

namespace TagSoup
{
    /// <summary>
    /// A <see cref="IContentHandler"/> that generates PYX format instead of XML.
    /// Primarily useful for debugging.
    /// </summary>
    public class PYXWriter : IScanHandler, IContentHandler, ILexicalHandler
    {
        private readonly TextWriter theWriter; // where we Write to
        //private static char[] dummy = new char[1]; // LUCENENET: Never read
        private string attrName; // saved attribute name

        // ScanHandler implementation

        public virtual void Adup(char[] buffer, int startIndex, int length)
        {
            theWriter.WriteLine(attrName);
            attrName = null;
        }

        public virtual void Aname(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theWriter.Write('A');
            theWriter.Write(buffer, startIndex, length);
            theWriter.Write(' ');
            attrName = new string(buffer, startIndex, length);
        }

        public virtual void Aval(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theWriter.Write(buffer, startIndex, length);
            theWriter.WriteLine();
            attrName = null;
        }

        public virtual void Cmnt(char[] buffer, int startIndex, int length)
        {
            //		theWriter.Write('!');
            //		theWriter.Write(buffer, startIndex, length);
            //		theWriter.WriteLine();
        }

        public virtual void Entity(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual int GetEntity()
        {
            return 0;
        }

        public virtual void EOF(char[] buffer, int startIndex, int length)
        {
            theWriter.Dispose();
        }

        public virtual void ETag(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theWriter.Write(')');
            theWriter.Write(buffer, startIndex, length);
            theWriter.WriteLine();
        }

        public virtual void Decl(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void GI(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theWriter.Write('(');
            theWriter.Write(buffer, startIndex, length);
            theWriter.WriteLine();
        }

        public virtual void CDSect(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            PCDATA(buffer, startIndex, length);
        }

        public virtual void PCDATA(char[] buffer, int startIndex, int length)
        {
            if (length == 0)
            {
                return; // nothing to do
            }
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            bool inProgress = false;
            length += startIndex;
            for (int i = startIndex; i < length; i++)
            {
                if (buffer[i] == '\n')
                {
                    if (inProgress)
                    {
                        theWriter.WriteLine();
                    }
                    theWriter.WriteLine("-\\n");
                    inProgress = false;
                }
                else
                {
                    if (!inProgress)
                    {
                        theWriter.Write('-');
                    }
                    switch (buffer[i])
                    {
                        case '\t':
                            theWriter.Write("\\t");
                            break;
                        case '\\':
                            theWriter.Write("\\\\");
                            break;
                        default:
                            theWriter.Write(buffer[i]);
                            break;
                    }
                    inProgress = true;
                }
            }
            if (inProgress)
            {
                theWriter.WriteLine();
            }
        }

        public virtual void PITarget(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theWriter.Write('?');
            theWriter.Write(buffer, startIndex, length);
            theWriter.Write(' ');
        }

        public virtual void PI(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            theWriter.Write(buffer, startIndex, length);
            theWriter.WriteLine();
        }

        public virtual void STagC(char[] buffer, int startIndex, int length)
        {
            //		theWriter.WriteLine("!");			// FIXME
        }

        public virtual void STagE(char[] buffer, int startIndex, int length)
        {
            theWriter.WriteLine("!"); // FIXME
        }

        // SAX ContentHandler implementation

        public virtual void Characters(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            PCDATA(buffer, startIndex, length);
        }

        public virtual void EndDocument()
        {
            theWriter.Dispose();
        }

        public virtual void EndElement(string uri, string localname, string qname)
        {
            if (qname.Length == 0)
            {
                qname = localname;
            }
            theWriter.Write(')');
            theWriter.WriteLine(qname);
        }

        public virtual void EndPrefixMapping(string prefix)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void IgnorableWhitespace(char[] buffer, int startIndex, int length)
        {
            // LUCENENET: Added guard clauses
            Guard.BufferAndRangeCheck(buffer, startIndex, length);

            Characters(buffer, startIndex, length);
        }

        public virtual void ProcessingInstruction(string target, string data)
        {
            theWriter.Write('?');
            theWriter.Write(target);
            theWriter.Write(' ');
            theWriter.WriteLine(data);
        }

        public virtual void SetDocumentLocator(ILocator locator)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void SkippedEntity(string name)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void StartDocument()
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void StartElement(string uri, string localname, string qname, IAttributes atts)
        {
            // LUCENENET: Added guard clauses
            if (qname is null)
                throw new ArgumentNullException(nameof(qname));
            if (atts is null)
                throw new ArgumentNullException(nameof(atts));

            if (qname.Length == 0)
            {
                qname = localname;
            }

            theWriter.Write('(');
            theWriter.WriteLine(qname);
            int length = atts.Length;
            for (int i = 0; i < length; i++)
            {
                qname = atts.GetQName(i);
                if (qname.Length == 0)
                {
                    qname = atts.GetLocalName(i);
                }
                theWriter.Write('A');
                //			theWriter.Write(atts.getType(i));	// DEBUG
                theWriter.Write(qname);
                theWriter.Write(' ');
                theWriter.WriteLine(atts.GetValue(i));
            }
        }

        public virtual void StartPrefixMapping(string prefix, string uri)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void Comment(char[] ch, int start, int length)
        {
            Cmnt(ch, start, length);
        }

        public virtual void EndCDATA()
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void EndDTD()
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void EndEntity(string name)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void StartCDATA()
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void StartDTD(string name, string publicId, string systemId)
        {
            // LUCENENET: Intentionally blank
        }

        public virtual void StartEntity(string name)
        {
            // LUCENENET: Intentionally blank
        }

        // Constructor

        public PYXWriter(TextWriter writer)
        {
            theWriter = writer ?? throw new ArgumentNullException(nameof(writer));
        }
    }
}
