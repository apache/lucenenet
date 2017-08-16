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
        private static char[] dummy = new char[1];
        private string attrName; // saved attribute name

        // ScanHandler implementation

        public void Adup(char[] buff, int offset, int length)
        {
            theWriter.WriteLine(attrName);
            attrName = null;
        }

        public void Aname(char[] buff, int offset, int length)
        {
            theWriter.Write('A');
            theWriter.Write(buff, offset, length);
            theWriter.Write(' ');
            attrName = new string(buff, offset, length);
        }

        public void Aval(char[] buff, int offset, int length)
        {
            theWriter.Write(buff, offset, length);
            theWriter.WriteLine();
            attrName = null;
        }

        public void Cmnt(char[] buff, int offset, int length)
        {
            //		theWriter.Write('!');
            //		theWriter.Write(buff, offset, length);
            //		theWriter.WriteLine();
        }

        public void Entity(char[] buff, int offset, int length)
        {
        }

        public int GetEntity()
        {
            return 0;
        }

        public void EOF(char[] buff, int offset, int length)
        {
            theWriter.Dispose();
        }

        public void ETag(char[] buff, int offset, int length)
        {
            theWriter.Write(')');
            theWriter.Write(buff, offset, length);
            theWriter.WriteLine();
        }

        public void Decl(char[] buff, int offset, int length)
        {
        }

        public void GI(char[] buff, int offset, int length)
        {
            theWriter.Write('(');
            theWriter.Write(buff, offset, length);
            theWriter.WriteLine();
        }

        public void CDSect(char[] buff, int offset, int length)
        {
            PCDATA(buff, offset, length);
        }

        public void PCDATA(char[] buff, int offset, int length)
        {
            if (length == 0)
            {
                return; // nothing to do
            }
            bool inProgress = false;
            length += offset;
            for (int i = offset; i < length; i++)
            {
                if (buff[i] == '\n')
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
                    switch (buff[i])
                    {
                        case '\t':
                            theWriter.Write("\\t");
                            break;
                        case '\\':
                            theWriter.Write("\\\\");
                            break;
                        default:
                            theWriter.Write(buff[i]);
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

        public void PITarget(char[] buff, int offset, int length)
        {
            theWriter.Write('?');
            theWriter.Write(buff, offset, length);
            theWriter.Write(' ');
        }

        public void PI(char[] buff, int offset, int length)
        {
            theWriter.Write(buff, offset, length);
            theWriter.WriteLine();
        }

        public void STagC(char[] buff, int offset, int length)
        {
            //		theWriter.WriteLine("!");			// FIXME
        }

        public void STagE(char[] buff, int offset, int length)
        {
            theWriter.WriteLine("!"); // FIXME
        }

        // SAX ContentHandler implementation

        public void Characters(char[] buff, int offset, int length)
        {
            PCDATA(buff, offset, length);
        }

        public void EndDocument()
        {
            theWriter.Dispose();
        }

        public void EndElement(string uri, string localname, string qname)
        {
            if (qname.Length == 0)
            {
                qname = localname;
            }
            theWriter.Write(')');
            theWriter.WriteLine(qname);
        }

        public void EndPrefixMapping(string prefix)
        {
        }

        public void IgnorableWhitespace(char[] buff, int offset, int length)
        {
            Characters(buff, offset, length);
        }

        public void ProcessingInstruction(string target, string data)
        {
            theWriter.Write('?');
            theWriter.Write(target);
            theWriter.Write(' ');
            theWriter.WriteLine(data);
        }

        public void SetDocumentLocator(ILocator locator)
        {
        }

        public void SkippedEntity(string name)
        {
        }

        public void StartDocument()
        {
        }

        public void StartElement(string uri, string localname, string qname, IAttributes atts)
        {
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

        public void StartPrefixMapping(string prefix, string uri)
        {
        }

        public void Comment(char[] ch, int start, int length)
        {
            Cmnt(ch, start, length);
        }

        public void EndCDATA()
        {
        }

        public void EndDTD()
        {
        }

        public void EndEntity(string name)
        {
        }

        public void StartCDATA()
        {
        }

        public void StartDTD(string name, string publicId, string systemId)
        {
        }

        public void StartEntity(string name)
        {
        }

        // Constructor

        public PYXWriter(TextWriter w)
        {
            theWriter = w;
        }
    }
}
