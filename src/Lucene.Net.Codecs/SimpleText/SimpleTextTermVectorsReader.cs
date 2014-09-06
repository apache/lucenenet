package org.apache.lucene.codecs.simpletext;

/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

import java.io.IOException;
import java.util.Collections;
import java.util.Comparator;
import java.util.Iterator;
import java.util.Map;
import java.util.SortedMap;
import java.util.TreeMap;

import org.apache.lucene.codecs.TermVectorsReader;
import org.apache.lucene.index.DocsAndPositionsEnum;
import org.apache.lucene.index.DocsEnum;
import org.apache.lucene.index.Fields;
import org.apache.lucene.index.IndexFileNames;
import org.apache.lucene.index.SegmentInfo;
import org.apache.lucene.index.Terms;
import org.apache.lucene.index.TermsEnum;
import org.apache.lucene.store.AlreadyClosedException;
import org.apache.lucene.store.BufferedChecksumIndexInput;
import org.apache.lucene.store.ChecksumIndexInput;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.IOContext;
import org.apache.lucene.store.IndexInput;
import org.apache.lucene.util.ArrayUtil;
import org.apache.lucene.util.Bits;
import org.apache.lucene.util.BytesRef;
import org.apache.lucene.util.CharsRef;
import org.apache.lucene.util.IOUtils;
import org.apache.lucene.util.StringHelper;
import org.apache.lucene.util.UnicodeUtil;
import static org.apache.lucene.codecs.simpletext.SimpleTextTermVectorsWriter.*;

/**
 * Reads plain-text term vectors.
 * <p>
 * <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
 * @lucene.experimental
 */
public class SimpleTextTermVectorsReader extends TermVectorsReader {
  private long offsets[]; /* docid -> offset in .vec file */
  private IndexInput in;
  private BytesRef scratch = new BytesRef();
  private CharsRef scratchUTF16 = new CharsRef();
  
  public SimpleTextTermVectorsReader(Directory directory, SegmentInfo si, IOContext context)  {
    bool success = false;
    try {
      in = directory.openInput(IndexFileNames.segmentFileName(si.name, "", VECTORS_EXTENSION), context);
      success = true;
    } finally {
      if (!success) {
        try {
          close();
        } catch (Throwable t) {} // ensure we throw our original exception
      }
    }
    readIndex(si.getDocCount());
  }
  
  // used by clone
  SimpleTextTermVectorsReader(long offsets[], IndexInput in) {
    this.offsets = offsets;
    this.in = in;
  }
  
  // we don't actually write a .tvx-like index, instead we read the 
  // vectors file in entirety up-front and save the offsets 
  // so we can seek to the data later.
  private void readIndex(int maxDoc)  {
    ChecksumIndexInput input = new BufferedChecksumIndexInput(in);
    offsets = new long[maxDoc];
    int upto = 0;
    while (!scratch.equals(END)) {
      SimpleTextUtil.readLine(input, scratch);
      if (StringHelper.startsWith(scratch, DOC)) {
        offsets[upto] = input.getFilePointer();
        upto++;
      }
    }
    SimpleTextUtil.checkFooter(input);
    Debug.Assert( upto == offsets.length;
  }
  
  @Override
  public Fields get(int doc)  {
    SortedMap<String,SimpleTVTerms> fields = new TreeMap<>();
    in.seek(offsets[doc]);
    readLine();
    Debug.Assert( StringHelper.startsWith(scratch, NUMFIELDS);
    int numFields = parseIntAt(NUMFIELDS.length);
    if (numFields == 0) {
      return null; // no vectors for this doc
    }
    for (int i = 0; i < numFields; i++) {
      readLine();
      Debug.Assert( StringHelper.startsWith(scratch, FIELD);
      // skip fieldNumber:
      parseIntAt(FIELD.length);
      
      readLine();
      Debug.Assert( StringHelper.startsWith(scratch, FIELDNAME);
      String fieldName = readString(FIELDNAME.length, scratch);
      
      readLine();
      Debug.Assert( StringHelper.startsWith(scratch, FIELDPOSITIONS);
      bool positions = bool.parsebool(readString(FIELDPOSITIONS.length, scratch));
      
      readLine();
      Debug.Assert( StringHelper.startsWith(scratch, FIELDOFFSETS);
      bool offsets = bool.parsebool(readString(FIELDOFFSETS.length, scratch));
      
      readLine();
      Debug.Assert( StringHelper.startsWith(scratch, FIELDPAYLOADS);
      bool payloads = bool.parsebool(readString(FIELDPAYLOADS.length, scratch));
      
      readLine();
      Debug.Assert( StringHelper.startsWith(scratch, FIELDTERMCOUNT);
      int termCount = parseIntAt(FIELDTERMCOUNT.length);
      
      SimpleTVTerms terms = new SimpleTVTerms(offsets, positions, payloads);
      fields.put(fieldName, terms);
      
      for (int j = 0; j < termCount; j++) {
        readLine();
        Debug.Assert( StringHelper.startsWith(scratch, TERMTEXT);
        BytesRef term = new BytesRef();
        int termLength = scratch.length - TERMTEXT.length;
        term.grow(termLength);
        term.length = termLength;
        System.arraycopy(scratch.bytes, scratch.offset+TERMTEXT.length, term.bytes, term.offset, termLength);
        
        SimpleTVPostings postings = new SimpleTVPostings();
        terms.terms.put(term, postings);
        
        readLine();
        Debug.Assert( StringHelper.startsWith(scratch, TERMFREQ);
        postings.freq = parseIntAt(TERMFREQ.length);
        
        if (positions || offsets) {
          if (positions) {
            postings.positions = new int[postings.freq];
            if (payloads) {
              postings.payloads = new BytesRef[postings.freq];
            }
          }
        
          if (offsets) {
            postings.startOffsets = new int[postings.freq];
            postings.endOffsets = new int[postings.freq];
          }
          
          for (int k = 0; k < postings.freq; k++) {
            if (positions) {
              readLine();
              Debug.Assert( StringHelper.startsWith(scratch, POSITION);
              postings.positions[k] = parseIntAt(POSITION.length);
              if (payloads) {
                readLine();
                Debug.Assert( StringHelper.startsWith(scratch, PAYLOAD);
                if (scratch.length - PAYLOAD.length == 0) {
                  postings.payloads[k] = null;
                } else {
                  byte payloadBytes[] = new byte[scratch.length - PAYLOAD.length];
                  System.arraycopy(scratch.bytes, scratch.offset+PAYLOAD.length, payloadBytes, 0, payloadBytes.length);
                  postings.payloads[k] = new BytesRef(payloadBytes);
                }
              }
            }
            
            if (offsets) {
              readLine();
              Debug.Assert( StringHelper.startsWith(scratch, STARTOFFSET);
              postings.startOffsets[k] = parseIntAt(STARTOFFSET.length);
              
              readLine();
              Debug.Assert( StringHelper.startsWith(scratch, ENDOFFSET);
              postings.endOffsets[k] = parseIntAt(ENDOFFSET.length);
            }
          }
        }
      }
    }
    return new SimpleTVFields(fields);
  }

  @Override
  public TermVectorsReader clone() {
    if (in == null) {
      throw new AlreadyClosedException("this TermVectorsReader is closed");
    }
    return new SimpleTextTermVectorsReader(offsets, in.clone());
  }
  
  @Override
  public void close()  {
    try {
      IOUtils.close(in); 
    } finally {
      in = null;
      offsets = null;
    }
  }

  private void readLine()  {
    SimpleTextUtil.readLine(in, scratch);
  }
  
  private int parseIntAt(int offset) {
    UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset+offset, scratch.length-offset, scratchUTF16);
    return ArrayUtil.parseInt(scratchUTF16.chars, 0, scratchUTF16.length);
  }
  
  private String readString(int offset, BytesRef scratch) {
    UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset+offset, scratch.length-offset, scratchUTF16);
    return scratchUTF16.toString();
  }
  
  private class SimpleTVFields extends Fields {
    private final SortedMap<String,SimpleTVTerms> fields;
    
    SimpleTVFields(SortedMap<String,SimpleTVTerms> fields) {
      this.fields = fields;
    }

    @Override
    public Iterator<String> iterator() {
      return Collections.unmodifiableSet(fields.keySet()).iterator();
    }

    @Override
    public Terms terms(String field)  {
      return fields.get(field);
    }

    @Override
    public int size() {
      return fields.size();
    }
  }
  
  private static class SimpleTVTerms extends Terms {
    final SortedMap<BytesRef,SimpleTVPostings> terms;
    final bool hasOffsets;
    final bool hasPositions;
    final bool hasPayloads;
    
    SimpleTVTerms(bool hasOffsets, bool hasPositions, bool hasPayloads) {
      this.hasOffsets = hasOffsets;
      this.hasPositions = hasPositions;
      this.hasPayloads = hasPayloads;
      terms = new TreeMap<>();
    }
    
    @Override
    public TermsEnum iterator(TermsEnum reuse)  {
      // TODO: reuse
      return new SimpleTVTermsEnum(terms);
    }

    @Override
    public Comparator<BytesRef> getComparator() {
      return BytesRef.getUTF8SortedAsUnicodeComparator();
    }

    @Override
    public long size()  {
      return terms.size();
    }

    @Override
    public long getSumTotalTermFreq()  {
      return -1;
    }

    @Override
    public long getSumDocFreq()  {
      return terms.size();
    }

    @Override
    public int getDocCount()  {
      return 1;
    }

    @Override
    public bool hasFreqs() {
      return true;
    }

    @Override
    public bool hasOffsets() {
      return hasOffsets;
    }

    @Override
    public bool hasPositions() {
      return hasPositions;
    }
    
    @Override
    public bool hasPayloads() {
      return hasPayloads;
    }
  }
  
  private static class SimpleTVPostings {
    private int freq;
    private int positions[];
    private int startOffsets[];
    private int endOffsets[];
    private BytesRef payloads[];
  }
  
  private static class SimpleTVTermsEnum extends TermsEnum {
    SortedMap<BytesRef,SimpleTVPostings> terms;
    Iterator<Map.Entry<BytesRef,SimpleTextTermVectorsReader.SimpleTVPostings>> iterator;
    Map.Entry<BytesRef,SimpleTextTermVectorsReader.SimpleTVPostings> current;
    
    SimpleTVTermsEnum(SortedMap<BytesRef,SimpleTVPostings> terms) {
      this.terms = terms;
      this.iterator = terms.entrySet().iterator();
    }
    
    @Override
    public SeekStatus seekCeil(BytesRef text)  {
      iterator = terms.tailMap(text).entrySet().iterator();
      if (!iterator.hasNext()) {
        return SeekStatus.END;
      } else {
        return next().equals(text) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
      }
    }

    @Override
    public void seekExact(long ord)  {
      throw new UnsupportedOperationException();
    }

    @Override
    public BytesRef next()  {
      if (!iterator.hasNext()) {
        return null;
      } else {
        current = iterator.next();
        return current.getKey();
      }
    }

    @Override
    public BytesRef term()  {
      return current.getKey();
    }

    @Override
    public long ord()  {
      throw new UnsupportedOperationException();
    }

    @Override
    public int docFreq()  {
      return 1;
    }

    @Override
    public long totalTermFreq()  {
      return current.getValue().freq;
    }

    @Override
    public DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)  {
      // TODO: reuse
      SimpleTVDocsEnum e = new SimpleTVDocsEnum();
      e.reset(liveDocs, (flags & DocsEnum.FLAG_FREQS) == 0 ? 1 : current.getValue().freq);
      return e;
    }

    @Override
    public DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)  {
      SimpleTVPostings postings = current.getValue();
      if (postings.positions == null && postings.startOffsets == null) {
        return null;
      }
      // TODO: reuse
      SimpleTVDocsAndPositionsEnum e = new SimpleTVDocsAndPositionsEnum();
      e.reset(liveDocs, postings.positions, postings.startOffsets, postings.endOffsets, postings.payloads);
      return e;
    }

    @Override
    public Comparator<BytesRef> getComparator() {
      return BytesRef.getUTF8SortedAsUnicodeComparator();
    }
  }
  
  // note: these two enum classes are exactly like the Default impl...
  private static class SimpleTVDocsEnum extends DocsEnum {
    private bool didNext;
    private int doc = -1;
    private int freq;
    private Bits liveDocs;

    @Override
    public int freq()  {
      Debug.Assert( freq != -1;
      return freq;
    }

    @Override
    public int docID() {
      return doc;
    }

    @Override
    public int nextDoc() {
      if (!didNext && (liveDocs == null || liveDocs.get(0))) {
        didNext = true;
        return (doc = 0);
      } else {
        return (doc = NO_MORE_DOCS);
      }
    }

    @Override
    public int advance(int target)  {
      return slowAdvance(target);
    }

    public void reset(Bits liveDocs, int freq) {
      this.liveDocs = liveDocs;
      this.freq = freq;
      this.doc = -1;
      didNext = false;
    }
    
    @Override
    public long cost() {
      return 1;
    }
  }
  
  private static class SimpleTVDocsAndPositionsEnum extends DocsAndPositionsEnum {
    private bool didNext;
    private int doc = -1;
    private int nextPos;
    private Bits liveDocs;
    private int[] positions;
    private BytesRef[] payloads;
    private int[] startOffsets;
    private int[] endOffsets;

    @Override
    public int freq()  {
      if (positions != null) {
        return positions.length;
      } else {
        Debug.Assert( startOffsets != null;
        return startOffsets.length;
      }
    }

    @Override
    public int docID() {
      return doc;
    }

    @Override
    public int nextDoc() {
      if (!didNext && (liveDocs == null || liveDocs.get(0))) {
        didNext = true;
        return (doc = 0);
      } else {
        return (doc = NO_MORE_DOCS);
      }
    }

    @Override
    public int advance(int target)  {
      return slowAdvance(target);
    }

    public void reset(Bits liveDocs, int[] positions, int[] startOffsets, int[] endOffsets, BytesRef payloads[]) {
      this.liveDocs = liveDocs;
      this.positions = positions;
      this.startOffsets = startOffsets;
      this.endOffsets = endOffsets;
      this.payloads = payloads;
      this.doc = -1;
      didNext = false;
      nextPos = 0;
    }

    @Override
    public BytesRef getPayload() {
      return payloads == null ? null : payloads[nextPos-1];
    }

    @Override
    public int nextPosition() {
      Debug.Assert( (positions != null && nextPos < positions.length) ||
        startOffsets != null && nextPos < startOffsets.length;
      if (positions != null) {
        return positions[nextPos++];
      } else {
        nextPos++;
        return -1;
      }
    }

    @Override
    public int startOffset() {
      if (startOffsets == null) {
        return -1;
      } else {
        return startOffsets[nextPos-1];
      }
    }

    @Override
    public int endOffset() {
      if (endOffsets == null) {
        return -1;
      } else {
        return endOffsets[nextPos-1];
      }
    }
    
    @Override
    public long cost() {
      return 1;
    }
  }

  @Override
  public long ramBytesUsed() {
    return 0;
  }

  @Override
  public void checkIntegrity()  {}
}
