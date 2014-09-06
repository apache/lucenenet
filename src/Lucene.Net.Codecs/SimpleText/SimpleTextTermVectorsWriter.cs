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
import java.util.Comparator;

import org.apache.lucene.codecs.TermVectorsWriter;
import org.apache.lucene.index.FieldInfo;
import org.apache.lucene.index.FieldInfos;
import org.apache.lucene.index.IndexFileNames;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.IOContext;
import org.apache.lucene.store.IndexOutput;
import org.apache.lucene.util.BytesRef;
import org.apache.lucene.util.IOUtils;

/**
 * Writes plain-text term vectors.
 * <p>
 * <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
 * @lucene.experimental
 */
public class SimpleTextTermVectorsWriter extends TermVectorsWriter {
  
  static final BytesRef END                = new BytesRef("END");
  static final BytesRef DOC                = new BytesRef("doc ");
  static final BytesRef NUMFIELDS          = new BytesRef("  numfields ");
  static final BytesRef FIELD              = new BytesRef("  field ");
  static final BytesRef FIELDNAME          = new BytesRef("    name ");
  static final BytesRef FIELDPOSITIONS     = new BytesRef("    positions ");
  static final BytesRef FIELDOFFSETS       = new BytesRef("    offsets   ");
  static final BytesRef FIELDPAYLOADS      = new BytesRef("    payloads  ");
  static final BytesRef FIELDTERMCOUNT     = new BytesRef("    numterms ");
  static final BytesRef TERMTEXT           = new BytesRef("    term ");
  static final BytesRef TERMFREQ           = new BytesRef("      freq ");
  static final BytesRef POSITION           = new BytesRef("      position ");
  static final BytesRef PAYLOAD            = new BytesRef("        payload ");
  static final BytesRef STARTOFFSET        = new BytesRef("        startoffset ");
  static final BytesRef ENDOFFSET          = new BytesRef("        endoffset ");

  static final String VECTORS_EXTENSION = "vec";
  
  private final Directory directory;
  private final String segment;
  private IndexOutput out;
  private int numDocsWritten = 0;
  private final BytesRef scratch = new BytesRef();
  private bool offsets;
  private bool positions;
  private bool payloads;

  public SimpleTextTermVectorsWriter(Directory directory, String segment, IOContext context)  {
    this.directory = directory;
    this.segment = segment;
    bool success = false;
    try {
      out = directory.createOutput(IndexFileNames.segmentFileName(segment, "", VECTORS_EXTENSION), context);
      success = true;
    } finally {
      if (!success) {
        abort();
      }
    }
  }
  
  @Override
  public void startDocument(int numVectorFields)  {
    write(DOC);
    write(Integer.toString(numDocsWritten));
    newLine();
    
    write(NUMFIELDS);
    write(Integer.toString(numVectorFields));
    newLine();
    numDocsWritten++;
  }

  @Override
  public void startField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)  {  
    write(FIELD);
    write(Integer.toString(info.number));
    newLine();
    
    write(FIELDNAME);
    write(info.name);
    newLine();
    
    write(FIELDPOSITIONS);
    write(bool.toString(positions));
    newLine();
    
    write(FIELDOFFSETS);
    write(bool.toString(offsets));
    newLine();
    
    write(FIELDPAYLOADS);
    write(bool.toString(payloads));
    newLine();
    
    write(FIELDTERMCOUNT);
    write(Integer.toString(numTerms));
    newLine();
    
    this.positions = positions;
    this.offsets = offsets;
    this.payloads = payloads;
  }

  @Override
  public void startTerm(BytesRef term, int freq)  {
    write(TERMTEXT);
    write(term);
    newLine();
    
    write(TERMFREQ);
    write(Integer.toString(freq));
    newLine();
  }

  @Override
  public void addPosition(int position, int startOffset, int endOffset, BytesRef payload)  {
    Debug.Assert( positions || offsets;
    
    if (positions) {
      write(POSITION);
      write(Integer.toString(position));
      newLine();
      
      if (payloads) {
        write(PAYLOAD);
        if (payload != null) {
          Debug.Assert( payload.length > 0;
          write(payload);
        }
        newLine();
      }
    }
    
    if (offsets) {
      write(STARTOFFSET);
      write(Integer.toString(startOffset));
      newLine();
      
      write(ENDOFFSET);
      write(Integer.toString(endOffset));
      newLine();
    }
  }

  @Override
  public void abort() {
    try {
      close();
    } catch (Throwable ignored) {}
    IOUtils.deleteFilesIgnoringExceptions(directory, IndexFileNames.segmentFileName(segment, "", VECTORS_EXTENSION));
  }

  @Override
  public void finish(FieldInfos fis, int numDocs)  {
    if (numDocsWritten != numDocs) {
      throw new RuntimeException("mergeVectors produced an invalid result: mergedDocs is " + numDocs + " but vec numDocs is " + numDocsWritten + " file=" + out.toString() + "; now aborting this merge to prevent index corruption");
    }
    write(END);
    newLine();
    SimpleTextUtil.writeChecksum(out, scratch);
  }
  
  @Override
  public void close()  {
    try {
      IOUtils.close(out);
    } finally {
      out = null;
    }
  }
  
  @Override
  public Comparator<BytesRef> getComparator()  {
    return BytesRef.getUTF8SortedAsUnicodeComparator();
  }
  
  private void write(String s)  {
    SimpleTextUtil.write(out, s, scratch);
  }
  
  private void write(BytesRef bytes)  {
    SimpleTextUtil.write(out, bytes);
  }
  
  private void newLine()  {
    SimpleTextUtil.writeNewline(out);
  }
}
