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

using Lucene;
using Lucene.Net.Support;
using Sax;
using System;
using System.IO;

namespace TagSoup
{
    /// <summary>
    /// This class implements a table-driven scanner for HTML, allowing for lots of
    /// defects.  It implements the Scanner interface, which accepts a Reader
    /// object to fetch characters from and a ScanHandler object to report lexical
    /// events to.
    /// </summary>
    public class HTMLScanner : IScanner, ILocator
    {
        // Start of state table
        private const int S_ANAME = 1;
        private const int S_APOS = 2;
        private const int S_AVAL = 3;
        private const int S_BB = 4;
        private const int S_BBC = 5;
        private const int S_BBCD = 6;
        private const int S_BBCDA = 7;
        private const int S_BBCDAT = 8;
        private const int S_BBCDATA = 9;
        private const int S_CDATA = 10;
        private const int S_CDATA2 = 11;
        private const int S_CDSECT = 12;
        private const int S_CDSECT1 = 13;
        private const int S_CDSECT2 = 14;
        private const int S_COM = 15;
        private const int S_COM2 = 16;
        private const int S_COM3 = 17;
        private const int S_COM4 = 18;
        private const int S_DECL = 19;
        private const int S_DECL2 = 20;
        private const int S_DONE = 21;
        private const int S_EMPTYTAG = 22;
        private const int S_ENT = 23;
        private const int S_EQ = 24;
        private const int S_ETAG = 25;
        private const int S_GI = 26;
        private const int S_NCR = 27;
        private const int S_PCDATA = 28;
        private const int S_PI = 29;
        private const int S_PITARGET = 30;
        private const int S_QUOT = 31;
        private const int S_STAGC = 32;
        private const int S_TAG = 33;
        private const int S_TAGWS = 34;
        private const int S_XNCR = 35;
        private const int A_ADUP = 1;
        private const int A_ADUP_SAVE = 2;
        private const int A_ADUP_STAGC = 3;
        private const int A_ANAME = 4;
        private const int A_ANAME_ADUP = 5;
        private const int A_ANAME_ADUP_STAGC = 6;
        private const int A_AVAL = 7;
        private const int A_AVAL_STAGC = 8;
        private const int A_CDATA = 9;
        private const int A_CMNT = 10;
        private const int A_DECL = 11;
        private const int A_EMPTYTAG = 12;
        private const int A_ENTITY = 13;
        private const int A_ENTITY_START = 14;
        private const int A_ETAG = 15;
        private const int A_GI = 16;
        private const int A_GI_STAGC = 17;
        private const int A_LT = 18;
        private const int A_LT_PCDATA = 19;
        private const int A_MINUS = 20;
        private const int A_MINUS2 = 21;
        private const int A_MINUS3 = 22;
        private const int A_PCDATA = 23;
        private const int A_PI = 24;
        private const int A_PITARGET = 25;
        private const int A_PITARGET_PI = 26;
        private const int A_SAVE = 27;
        private const int A_SKIP = 28;
        private const int A_SP = 29;
        private const int A_STAGC = 30;
        private const int A_UNGET = 31;
        private const int A_UNSAVE_PCDATA = 32;
        private static readonly int[] statetable = { // LUCENENET: marked readonly
            S_ANAME, '/', A_ANAME_ADUP, S_EMPTYTAG,
            S_ANAME, '=', A_ANAME, S_AVAL,
            S_ANAME, '>', A_ANAME_ADUP_STAGC, S_PCDATA,
            S_ANAME, 0, A_SAVE, S_ANAME,
            S_ANAME, -1, A_ANAME_ADUP_STAGC, S_DONE,
            S_ANAME, ' ', A_ANAME, S_EQ,
            S_ANAME, '\n', A_ANAME, S_EQ,
            S_ANAME, '\t', A_ANAME, S_EQ,
            S_APOS, '\'', A_AVAL, S_TAGWS,
            S_APOS, 0, A_SAVE, S_APOS,
            S_APOS, -1, A_AVAL_STAGC, S_DONE,
            S_APOS, ' ', A_SP, S_APOS,
            S_APOS, '\n', A_SP, S_APOS,
            S_APOS, '\t', A_SP, S_APOS,
            S_AVAL, '\'', A_SKIP, S_APOS,
            S_AVAL, '"', A_SKIP, S_QUOT,
            S_AVAL, '>', A_AVAL_STAGC, S_PCDATA,
            S_AVAL, 0, A_SAVE, S_STAGC,
            S_AVAL, -1, A_AVAL_STAGC, S_DONE,
            S_AVAL, ' ', A_SKIP, S_AVAL,
            S_AVAL, '\n', A_SKIP, S_AVAL,
            S_AVAL, '\t', A_SKIP, S_AVAL,
            S_BB, 'C', A_SKIP, S_BBC,
            S_BB, 0, A_SKIP, S_DECL,
            S_BB, -1, A_SKIP, S_DONE,
            S_BBC, 'D', A_SKIP, S_BBCD,
            S_BBC, 0, A_SKIP, S_DECL,
            S_BBC, -1, A_SKIP, S_DONE,
            S_BBCD, 'A', A_SKIP, S_BBCDA,
            S_BBCD, 0, A_SKIP, S_DECL,
            S_BBCD, -1, A_SKIP, S_DONE,
            S_BBCDA, 'T', A_SKIP, S_BBCDAT,
            S_BBCDA, 0, A_SKIP, S_DECL,
            S_BBCDA, -1, A_SKIP, S_DONE,
            S_BBCDAT, 'A', A_SKIP, S_BBCDATA,
            S_BBCDAT, 0, A_SKIP, S_DECL,
            S_BBCDAT, -1, A_SKIP, S_DONE,
            S_BBCDATA, '[', A_SKIP, S_CDSECT,
            S_BBCDATA, 0, A_SKIP, S_DECL,
            S_BBCDATA, -1, A_SKIP, S_DONE,
            S_CDATA, '<', A_SAVE, S_CDATA2,
            S_CDATA, 0, A_SAVE, S_CDATA,
            S_CDATA, -1, A_PCDATA, S_DONE,
            S_CDATA2, '/', A_UNSAVE_PCDATA, S_ETAG,
            S_CDATA2, 0, A_SAVE, S_CDATA,
            S_CDATA2, -1, A_UNSAVE_PCDATA, S_DONE,
            S_CDSECT, ']', A_SAVE, S_CDSECT1,
            S_CDSECT, 0, A_SAVE, S_CDSECT,
            S_CDSECT, -1, A_SKIP, S_DONE,
            S_CDSECT1, ']', A_SAVE, S_CDSECT2,
            S_CDSECT1, 0, A_SAVE, S_CDSECT,
            S_CDSECT1, -1, A_SKIP, S_DONE,
            S_CDSECT2, '>', A_CDATA, S_PCDATA,
            S_CDSECT2, 0, A_SAVE, S_CDSECT,
            S_CDSECT2, -1, A_SKIP, S_DONE,
            S_COM, '-', A_SKIP, S_COM2,
            S_COM, 0, A_SAVE, S_COM2,
            S_COM, -1, A_CMNT, S_DONE,
            S_COM2, '-', A_SKIP, S_COM3,
            S_COM2, 0, A_SAVE, S_COM2,
            S_COM2, -1, A_CMNT, S_DONE,
            S_COM3, '-', A_SKIP, S_COM4,
            S_COM3, 0, A_MINUS, S_COM2,
            S_COM3, -1, A_CMNT, S_DONE,
            S_COM4, '-', A_MINUS3, S_COM4,
            S_COM4, '>', A_CMNT, S_PCDATA,
            S_COM4, 0, A_MINUS2, S_COM2,
            S_COM4, -1, A_CMNT, S_DONE,
            S_DECL, '-', A_SKIP, S_COM,
            S_DECL, '[', A_SKIP, S_BB,
            S_DECL, '>', A_SKIP, S_PCDATA,
            S_DECL, 0, A_SAVE, S_DECL2,
            S_DECL, -1, A_SKIP, S_DONE,
            S_DECL2, '>', A_DECL, S_PCDATA,
            S_DECL2, 0, A_SAVE, S_DECL2,
            S_DECL2, -1, A_SKIP, S_DONE,
            S_EMPTYTAG, '>', A_EMPTYTAG, S_PCDATA,
            S_EMPTYTAG, 0, A_SAVE, S_ANAME,
            S_EMPTYTAG, ' ', A_SKIP, S_TAGWS,
            S_EMPTYTAG, '\n', A_SKIP, S_TAGWS,
            S_EMPTYTAG, '\t', A_SKIP, S_TAGWS,
            S_ENT, 0, A_ENTITY, S_ENT,
            S_ENT, -1, A_ENTITY, S_DONE,
            S_EQ, '=', A_SKIP, S_AVAL,
            S_EQ, '>', A_ADUP_STAGC, S_PCDATA,
            S_EQ, 0, A_ADUP_SAVE, S_ANAME,
            S_EQ, -1, A_ADUP_STAGC, S_DONE,
            S_EQ, ' ', A_SKIP, S_EQ,
            S_EQ, '\n', A_SKIP, S_EQ,
            S_EQ, '\t', A_SKIP, S_EQ,
            S_ETAG, '>', A_ETAG, S_PCDATA,
            S_ETAG, 0, A_SAVE, S_ETAG,
            S_ETAG, -1, A_ETAG, S_DONE,
            S_ETAG, ' ', A_SKIP, S_ETAG,
            S_ETAG, '\n', A_SKIP, S_ETAG,
            S_ETAG, '\t', A_SKIP, S_ETAG,
            S_GI, '/', A_SKIP, S_EMPTYTAG,
            S_GI, '>', A_GI_STAGC, S_PCDATA,
            S_GI, 0, A_SAVE, S_GI,
            S_GI, -1, A_SKIP, S_DONE,
            S_GI, ' ', A_GI, S_TAGWS,
            S_GI, '\n', A_GI, S_TAGWS,
            S_GI, '\t', A_GI, S_TAGWS,
            S_NCR, 0, A_ENTITY, S_NCR,
            S_NCR, -1, A_ENTITY, S_DONE,
            S_PCDATA, '&', A_ENTITY_START, S_ENT,
            S_PCDATA, '<', A_PCDATA, S_TAG,
            S_PCDATA, 0, A_SAVE, S_PCDATA,
            S_PCDATA, -1, A_PCDATA, S_DONE,
            S_PI, '>', A_PI, S_PCDATA,
            S_PI, 0, A_SAVE, S_PI,
            S_PI, -1, A_PI, S_DONE,
            S_PITARGET, '>', A_PITARGET_PI, S_PCDATA,
            S_PITARGET, 0, A_SAVE, S_PITARGET,
            S_PITARGET, -1, A_PITARGET_PI, S_DONE,
            S_PITARGET, ' ', A_PITARGET, S_PI,
            S_PITARGET, '\n', A_PITARGET, S_PI,
            S_PITARGET, '\t', A_PITARGET, S_PI,
            S_QUOT, '"', A_AVAL, S_TAGWS,
            S_QUOT, 0, A_SAVE, S_QUOT,
            S_QUOT, -1, A_AVAL_STAGC, S_DONE,
            S_QUOT, ' ', A_SP, S_QUOT,
            S_QUOT, '\n', A_SP, S_QUOT,
            S_QUOT, '\t', A_SP, S_QUOT,
            S_STAGC, '>', A_AVAL_STAGC, S_PCDATA,
            S_STAGC, 0, A_SAVE, S_STAGC,
            S_STAGC, -1, A_AVAL_STAGC, S_DONE,
            S_STAGC, ' ', A_AVAL, S_TAGWS,
            S_STAGC, '\n', A_AVAL, S_TAGWS,
            S_STAGC, '\t', A_AVAL, S_TAGWS,
            S_TAG, '!', A_SKIP, S_DECL,
            S_TAG, '/', A_SKIP, S_ETAG,
            S_TAG, '?', A_SKIP, S_PITARGET,
            S_TAG, '<', A_SAVE, S_TAG,
            S_TAG, 0, A_SAVE, S_GI,
            S_TAG, -1, A_LT_PCDATA, S_DONE,
            S_TAG, ' ', A_LT, S_PCDATA,
            S_TAG, '\n', A_LT, S_PCDATA,
            S_TAG, '\t', A_LT, S_PCDATA,
            S_TAGWS, '/', A_SKIP, S_EMPTYTAG,
            S_TAGWS, '>', A_STAGC, S_PCDATA,
            S_TAGWS, 0, A_SAVE, S_ANAME,
            S_TAGWS, -1, A_STAGC, S_DONE,
            S_TAGWS, ' ', A_SKIP, S_TAGWS,
            S_TAGWS, '\n', A_SKIP, S_TAGWS,
            S_TAGWS, '\t', A_SKIP, S_TAGWS,
            S_XNCR, 0, A_ENTITY, S_XNCR,
            S_XNCR, -1, A_ENTITY, S_DONE,

        };

        // LUCENENET: Never read
        //private static readonly string[] debug_actionnames = { "", "A_ADUP", "A_ADUP_SAVE", "A_ADUP_STAGC", "A_ANAME", "A_ANAME_ADUP", "A_ANAME_ADUP_STAGC", "A_AVAL", "A_AVAL_STAGC", "A_CDATA", "A_CMNT", "A_DECL", "A_EMPTYTAG", "A_ENTITY", "A_ENTITY_START", "A_ETAG", "A_GI", "A_GI_STAGC", "A_LT", "A_LT_PCDATA", "A_MINUS", "A_MINUS2", "A_MINUS3", "A_PCDATA", "A_PI", "A_PITARGET", "A_PITARGET_PI", "A_SAVE", "A_SKIP", "A_SP", "A_STAGC", "A_UNGET", "A_UNSAVE_PCDATA" };
        //private static readonly string[] debug_statenames = { "", "S_ANAME", "S_APOS", "S_AVAL", "S_BB", "S_BBC", "S_BBCD", "S_BBCDA", "S_BBCDAT", "S_BBCDATA", "S_CDATA", "S_CDATA2", "S_CDSECT", "S_CDSECT1", "S_CDSECT2", "S_COM", "S_COM2", "S_COM3", "S_COM4", "S_DECL", "S_DECL2", "S_DONE", "S_EMPTYTAG", "S_ENT", "S_EQ", "S_ETAG", "S_GI", "S_NCR", "S_PCDATA", "S_PI", "S_PITARGET", "S_QUOT", "S_STAGC", "S_TAG", "S_TAGWS", "S_XNCR" };

        // End of state table

        private string thePublicid;         // Locator state
        private string theSystemid;
        private int theLastLine;
        private int theLastColumn;
        private int theCurrentLine;
        private int theCurrentColumn;

        private int theState;                   // Current state
        private int theNextState;               // Next state
        private char[] theOutputBuffer = new char[200]; // Output buffer
        private int theSize;                    // Current buffer size
        private readonly int[] theWinMap = {    // Windows chars map // LUCENENET: marked readonly
            0x20AC, 0xFFFD, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
            0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0xFFFD, 0x017D, 0xFFFD,
            0xFFFD, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
            0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0xFFFD, 0x017E, 0x0178
        };

        /// <summary>
        ///   Index into the state table for [state][input character - 2].
        ///   The state table consists of 4-entry runs on the form
        ///   { current state, input character, action, next state }.
        ///   We precompute the index into the state table for all possible
        ///   { current state, input character } and store the result in
        ///   the statetableIndex array. Since only some input characters
        ///   are present in the state table, we only do the computation for
        ///   characters 0 to the highest character value in the state table.
        ///   An input character of -2 is used to cover all other characters
        ///   as -2 is guaranteed not to match any input character entry
        ///   in the state table.
        ///   <para/>
        ///   When doing lookups, the input character should first be tested
        ///   to be in the range [-1 (inclusive), statetableIndexMaxChar (exclusive)].
        ///   if it isn't use -2 as the input character.
        ///   <para/>
        ///   Finally, add 2 to the input character to cover for the fact that
        ///   Java doesn't support negative array indexes. Then look up
        ///   the value in the statetableIndex. If the value is -1, then
        ///   no action or next state was found for the { state, input } that
        ///   you had. If it isn't -1, then action = statetable[value + 2] and
        ///   next state = statetable[value + 3]. That is, the value points
        ///   to the start of the answer 4-tuple in the statetable.
        /// </summary>
        private static readonly short[][] statetableIndex;

        /// <summary>
        ///   The highest character value seen in the statetable.
        ///   See the doc comment for statetableIndex to see how this
        ///   is used.
        /// </summary>
        private static readonly int statetableIndexMaxChar = LoadStateTableIndexMaxChar(out statetableIndex); // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)

        private static int LoadStateTableIndexMaxChar(out short[][] statetableIndexOut)
        {
            int statetableIndexMaxCharResult;
            int maxState = -1;
            int maxChar = -1;
            for (int i = 0; i < statetable.Length; i += 4)
            {
                if (statetable[i] > maxState)
                {
                    maxState = statetable[i];
                }
                if (statetable[i + 1] > maxChar)
                {
                    maxChar = statetable[i + 1];
                }
            }
            statetableIndexMaxCharResult = maxChar + 1;

            statetableIndexOut = new short[maxState + 1][];

            for (int i = 0; i <= maxState; i++)
            {
                statetableIndexOut[i] = new short[maxChar + 3];
            }
            for (int theState = 0; theState <= maxState; ++theState)
            {
                for (int ch = -2; ch <= maxChar; ++ch)
                {
                    int hit = -1;
                    int action = 0;
                    for (int i = 0; i < statetable.Length; i += 4)
                    {
                        if (theState != statetable[i])
                        {
                            if (action != 0) break;
                            continue;
                        }
                        if (statetable[i + 1] == 0)
                        {
                            hit = i;
                            action = statetable[i + 2];
                        }
                        else if (statetable[i + 1] == ch)
                        {
                            hit = i;
                            //action = statetable[i + 2]; // LUCENENET: IDE0059: Remove unnecessary value assignment
                            break;
                        }
                    }
                    statetableIndexOut[theState][ch + 2] = (short)hit;
                }
            }
            return statetableIndexMaxCharResult;
        }

        // Locator implementation

        public virtual int LineNumber => theLastLine;

        public virtual int ColumnNumber => theLastColumn;

        public virtual string PublicId => thePublicid;

        public virtual string SystemId => theSystemid;


        // Scanner implementation

        /// <summary>
        /// Reset document locator, supplying systemId and publicId.
        /// </summary>
        /// <param name="systemId">System id</param>
        /// <param name="publicId">Public id</param>
        public virtual void ResetDocumentLocator(string publicId, string systemId)
        {
            thePublicid = publicId;
            theSystemid = systemId;
            theLastLine = theLastColumn = theCurrentLine = theCurrentColumn = 0;
        }

        /// <summary>
        /// Scan HTML source, reporting lexical events.
        /// </summary>
        /// <param name="r">Reader that provides characters</param>
        /// <param name="h">ScanHandler that accepts lexical events.</param>
        public virtual void Scan(TextReader r, IScanHandler h)
        {
            theState = S_PCDATA;

            int firstChar = r.Peek();   // Remove any leading BOM
            if (firstChar == '\uFEFF') r.Read();

            while (theState != S_DONE)
            {
                int ch = r.Peek();
                bool unread = false;

                // Process control characters
                if (ch >= 0x80 && ch <= 0x9F) ch = theWinMap[ch - 0x80];

                if (ch == '\r')
                {
                    r.Read();
                    ch = r.Peek();      // expect LF next
                    if (ch != '\n')
                    {
                        unread = true;
                        ch = '\n';
                    }
                }

                if (ch == '\n')
                {
                    theCurrentLine++;
                    theCurrentColumn = 0;
                }
                else
                {
                    theCurrentColumn++;
                }

                if (!(ch >= 0x20 || ch == '\n' || ch == '\t' || ch == -1)) continue;

                // Search state table
                int adjCh = (ch >= -1 && ch < statetableIndexMaxChar) ? ch : -2;
                int statetableRow = statetableIndex[theState][adjCh + 2];
                int action = 0;
                if (statetableRow != -1)
                {
                    action = statetable[statetableRow + 2];
                    theNextState = statetable[statetableRow + 3];
                }

                //			System.err.println("In " + debug_statenames[theState] + " got " + nicechar(ch) + " doing " + debug_actionnames[action] + " then " + debug_statenames[theNextState]);
                switch (action)
                {
                    case 0:
                        throw Error.Create(
                            "HTMLScanner can't cope with " + ch + " in state " +
                            theState);
                    case A_ADUP:
                        h.Adup(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_ADUP_SAVE:
                        h.Adup(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        Save(ch, h);
                        break;
                    case A_ADUP_STAGC:
                        h.Adup(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.STagC(theOutputBuffer, 0, theSize);
                        break;
                    case A_ANAME:
                        h.Aname(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_ANAME_ADUP:
                        h.Aname(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.Adup(theOutputBuffer, 0, theSize);
                        break;
                    case A_ANAME_ADUP_STAGC:
                        h.Aname(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.Adup(theOutputBuffer, 0, theSize);
                        h.STagC(theOutputBuffer, 0, theSize);
                        break;
                    case A_AVAL:
                        h.Aval(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_AVAL_STAGC:
                        h.Aval(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.STagC(theOutputBuffer, 0, theSize);
                        break;
                    case A_CDATA:
                        Mark();
                        // suppress the final "]]" in the buffer
                        if (theSize > 1) theSize -= 2;
                        h.PCDATA(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_ENTITY_START:
                        h.PCDATA(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        Save(ch, h);
                        break;
                    case A_ENTITY:
                        Mark();
                        char ch1 = (char)ch;
                        //				System.out.println("Got " + ch1 + " in state " + ((theState == S_ENT) ? "S_ENT" : ((theState == S_NCR) ? "S_NCR" : "UNK")));
                        if (theState == S_ENT && ch1 == '#')
                        {
                            theNextState = S_NCR;
                            Save(ch, h);
                            break;
                        }
                        else if (theState == S_NCR && (ch1 == 'x' || ch1 == 'X'))
                        {
                            theNextState = S_XNCR;
                            Save(ch, h);
                            break;
                        }
                        else if (theState == S_ENT && char.IsLetterOrDigit(ch1))
                        {
                            Save(ch, h);
                            break;
                        }
                        else if (theState == S_NCR && char.IsDigit(ch1))
                        {
                            Save(ch, h);
                            break;
                        }
                        else if (theState == S_XNCR && (char.IsDigit(ch1) || "abcdefABCDEF".IndexOf(ch1) != -1))
                        {
                            Save(ch, h);
                            break;
                        }

                        // The whole entity reference has been collected
                        //				System.err.println("%%" + new String(theOutputBuffer, 0, theSize));
                        h.Entity(theOutputBuffer, 1, theSize - 1);
                        int ent = h.GetEntity();
                        //				System.err.println("%% value = " + ent);
                        if (ent != 0)
                        {
                            theSize = 0;
                            if (ent >= 0x80 && ent <= 0x9F)
                            {
                                ent = theWinMap[ent - 0x80];
                            }
                            if (ent < 0x20)
                            {
                                // Control becomes space
                                //ent = 0x20; // LUCENENET: IDE0059: Remove unnecessary value assignment
                            }
                            else if (ent >= 0xD800 && ent <= 0xDFFF)
                            {
                                // Surrogates get dropped
                                //ent = 0; // LUCENENET: IDE0059: Remove unnecessary value assignment
                            }
                            else if (ent <= 0xFFFF)
                            {
                                // BMP character
                                Save(ent, h);
                            }
                            else
                            {
                                // Astral converted to two surrogates
                                ent -= 0x10000;
                                Save((ent >> 10) + 0xD800, h);
                                Save((ent & 0x3FF) + 0xDC00, h);
                            }
                            if (ch != ';')
                            {
                                unread = true;
                                theCurrentColumn--;
                            }
                        }
                        else
                        {
                            unread = true;
                            theCurrentColumn--;
                        }
                        theNextState = S_PCDATA;
                        break;
                    case A_ETAG:
                        h.ETag(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_DECL:
                        h.Decl(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_GI:
                        h.GI(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_GI_STAGC:
                        h.GI(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.STagC(theOutputBuffer, 0, theSize);
                        break;
                    case A_LT:
                        Mark();
                        Save('<', h);
                        Save(ch, h);
                        break;
                    case A_LT_PCDATA:
                        Mark();
                        Save('<', h);
                        h.PCDATA(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_PCDATA:
                        Mark();
                        h.PCDATA(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_CMNT:
                        Mark();
                        h.Cmnt(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_MINUS3:
                        Save('-', h);
                        Save(' ', h);
                        break;
                    case A_MINUS2:
                        Save('-', h);
                        Save(' ', h);
                        Save('-', h);
                        Save(ch, h);
                        // fall through into A_MINUS
                        break;
                    case A_MINUS:
                        Save('-', h);
                        Save(ch, h);
                        break;
                    case A_PI:
                        Mark();
                        h.PI(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_PITARGET:
                        h.PITarget(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_PITARGET_PI:
                        h.PITarget(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.PI(theOutputBuffer, 0, theSize);
                        break;
                    case A_SAVE:
                        Save(ch, h);
                        break;
                    case A_SKIP:
                        break;
                    case A_SP:
                        Save(' ', h);
                        break;
                    case A_STAGC:
                        h.STagC(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    case A_EMPTYTAG:
                        Mark();
                        //				System.err.println("%%% Empty tag seen");
                        if (theSize > 0) h.GI(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        h.STagE(theOutputBuffer, 0, theSize);
                        break;
                    case A_UNGET:
                        unread = true;
                        theCurrentColumn--;
                        break;
                    case A_UNSAVE_PCDATA:
                        if (theSize > 0) theSize--;
                        h.PCDATA(theOutputBuffer, 0, theSize);
                        theSize = 0;
                        break;
                    default:
                        throw Error.Create("Can't process state " + action);
                }
                if (!unread)
                {
                    r.Read();
                }
                theState = theNextState;
            }
            h.EOF(theOutputBuffer, 0, 0);
        }

        /// <summary>
        /// Mark the current scan position as a "point of interest" - start of a tag,
        /// cdata, processing instruction etc.
        /// </summary>
        private void Mark()
        {
            theLastColumn = theCurrentColumn;
            theLastLine = theCurrentLine;
        }

        /// <summary>
        /// A callback for the ScanHandler that allows it to force
        /// the lexer state to CDATA content (no markup is recognized except
        /// the end of element.
        /// </summary>
        public virtual void StartCDATA() { theNextState = S_CDATA; }

        private void Save(int ch, IScanHandler h)
        {
            if (theSize >= theOutputBuffer.Length - 20)
            {
                if (theState == S_PCDATA || theState == S_CDATA)
                {
                    // Return a buffer-sized chunk of PCDATA
                    h.PCDATA(theOutputBuffer, 0, theSize);
                    theSize = 0;
                }
                else
                {
                    // Grow the buffer size
                    char[] newOutputBuffer = new char[theOutputBuffer.Length * 2];
                    Arrays.Copy(theOutputBuffer, 0, newOutputBuffer, 0, theSize + 1);
                    theOutputBuffer = newOutputBuffer;
                }
            }
            theOutputBuffer[theSize++] = (char)ch;
        }

        ///// <summary>
        ///// Test procedure.  Reads HTML from the standard input and writes
        ///// PYX to the standard output.
        ///// </summary>
        ///// <param name="argv"></param>
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Optional argument")]
        //public static void Main(string[] argv)
        //{
        //    IScanner s = new HTMLScanner();
        //    TextReader r = Console.In;
        //    TextWriter w = Console.Out;
        //    PYXWriter pw = new PYXWriter(w);
        //    s.Scan(r, pw);
        //}


        //private static string NiceChar(int value) // LUCENENET: IDE0051: Remove unused private member
        //{
        //    if (value == '\n') return "\\n";
        //    if (value < 32) return "0x" + value.ToString("X", CultureInfo.InvariantCulture);
        //    return "'" + ((char)value) + "'";
        //}
    }
}
