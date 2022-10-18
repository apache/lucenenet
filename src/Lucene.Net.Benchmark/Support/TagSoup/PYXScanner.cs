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
// This file is part of TagSoup.
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.  You may also distribute
// and/or modify it under version 2.1 of the Academic Free License.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
// 
// 
// PYX Scanner

using System;
using System.IO;

namespace TagSoup
{
    /// <summary>
    /// A <see cref="IScanner"/> that accepts PYX format instead of HTML.
    /// Useful primarily for debugging.
    /// </summary>
    public class PYXScanner : IScanner
    {
        public virtual void ResetDocumentLocator(string publicId, string systemId)
        {
            // Need this method for interface compatibility, but note
            // that PyxScanner does not implement Locator.
        }

        public virtual void Scan(TextReader br, IScanHandler h)
        {
            // LUCENENET: Added guard clauses
            if (br is null)
                throw new ArgumentNullException(nameof(br));
            if (h is null)
                throw new ArgumentNullException(nameof(h));

            string s;
            char[] buffer = null;
            bool instag = false;
            while ((s = br.ReadLine()) != null)
            {
                int size = s.Length;
                buffer = s.ToCharArray(0, size);
                if (buffer.Length < size)
                {
                    buffer = new char[size];
                }
                switch (buffer[0])
                {
                    case '(':
                        if (instag)
                        {
                            h.STagC(buffer, 0, 0);
                            //instag = false; // LUCENENET: IDE0059: Remove unnecessary value assignment
                        }
                        h.GI(buffer, 1, size - 1);
                        instag = true;
                        break;
                    case ')':
                        if (instag)
                        {
                            h.STagC(buffer, 0, 0);
                            instag = false;
                        }
                        h.ETag(buffer, 1, size - 1);
                        break;
                    case '?':
                        if (instag)
                        {
                            h.STagC(buffer, 0, 0);
                            instag = false;
                        }
                        h.PI(buffer, 1, size - 1);
                        break;
                    case 'A':
                        int sp = s.IndexOf(' ');
                        h.Aname(buffer, 1, sp - 1);
                        h.Aval(buffer, sp + 1, size - sp - 1);
                        break;
                    case '-':
                        if (instag)
                        {
                            h.STagC(buffer, 0, 0);
                            instag = false;
                        }
                        if (s.Equals("-\\n", StringComparison.Ordinal))
                        {
                            buffer[0] = '\n';
                            h.PCDATA(buffer, 0, 1);
                        }
                        else
                        {
                            // FIXME:
                            // Does not decode \t and \\ in input
                            h.PCDATA(buffer, 1, size - 1);
                        }
                        break;
                    case 'E':
                        if (instag)
                        {
                            h.STagC(buffer, 0, 0);
                            instag = false;
                        }
                        h.Entity(buffer, 1, size - 1);
                        break;
                    default:
                        //				System.err.print("Gotcha ");
                        //				System.err.print(s);
                        //				System.err.print('\n');
                        break;
                }
            }
            h.EOF(buffer, 0, 0);
        }

        public virtual void StartCDATA()
        {
            // LUCENENET: Intentionally blank
        }

        //public static void main(string[] argv)  {
        //  IScanner s = new PYXScanner();
        //  TextReader r = new StreamReader(System.Console.OpenStandardInput(), Encoding.UTF8);
        //  TextWriter w = new StreamWriter(System.Console.OpenStandardOutput(), Encoding.UTF8));
        //  s.Scan(r, new PYXWriter(w));
        //  }
    }
}
