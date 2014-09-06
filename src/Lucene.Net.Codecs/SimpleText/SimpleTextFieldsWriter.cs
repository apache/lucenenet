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

import org.apache.lucene.codecs.FieldsConsumer;
import org.apache.lucene.codecs.PostingsConsumer;
import org.apache.lucene.codecs.TermStats;
import org.apache.lucene.codecs.TermsConsumer;
import org.apache.lucene.index.FieldInfo.IndexOptions;
import org.apache.lucene.index.FieldInfo;
import org.apache.lucene.index.SegmentWriteState;
import org.apache.lucene.store.IndexOutput;
import org.apache.lucene.util.BytesRef;

class SimpleTextFieldsWriter extends FieldsConsumer {
  
  private IndexOutput out;
  private final BytesRef scratch = new BytesRef(10);

  final static BytesRef END          = new BytesRef("END");
  final static BytesRef FIELD        = new BytesRef("field ");
  final static BytesRef TERM         = new BytesRef("  term ");
  final static BytesRef DOC          = new BytesRef("    doc ");
  final static BytesRef FREQ         = new BytesRef("      freq ");
  final static BytesRef POS          = new BytesRef("      pos ");
  final static BytesRef START_OFFSET = new BytesRef("      startOffset ");
  final static BytesRef END_OFFSET   = new BytesRef("      endOffset ");
  final static BytesRef PAYLOAD      = new BytesRef("        payload ");

  public SimpleTextFieldsWriter(SegmentWriteState state)  {
    final String fileName = SimpleTextPostingsFormat.getPostingsFileName(state.segmentInfo.name, state.segmentSuffix);
    out = state.directory.createOutput(fileName, state.context);
  }

  private void write(String s)  {
    SimpleTextUtil.write(out, s, scratch);
  }

  private void write(BytesRef b)  {
    SimpleTextUtil.write(out, b);
  }

  private void newline()  {
    SimpleTextUtil.writeNewline(out);
  }

  @Override
  public TermsConsumer addField(FieldInfo field)  {
    write(FIELD);
    write(field.name);
    newline();
    return new SimpleTextTermsWriter(field);
  }

  private class SimpleTextTermsWriter extends TermsConsumer {
    private final SimpleTextPostingsWriter postingsWriter;
    
    public SimpleTextTermsWriter(FieldInfo field) {
      postingsWriter = new SimpleTextPostingsWriter(field);
    }

    @Override
    public PostingsConsumer startTerm(BytesRef term)  {
      return postingsWriter.reset(term);
    }

    @Override
    public void finishTerm(BytesRef term, TermStats stats)  {
    }

    @Override
    public void finish(long sumTotalTermFreq, long sumDocFreq, int docCount)  {
    }

    @Override
    public Comparator<BytesRef> getComparator() {
      return BytesRef.getUTF8SortedAsUnicodeComparator();
    }
  }

  private class SimpleTextPostingsWriter extends PostingsConsumer {
    private BytesRef term;
    private bool wroteTerm;
    private final IndexOptions indexOptions;
    private final bool writePositions;
    private final bool writeOffsets;

    // for Debug.Assert(:
    private int lastStartOffset = 0;

    public SimpleTextPostingsWriter(FieldInfo field) {
      this.indexOptions = field.getIndexOptions();
      writePositions = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
      writeOffsets = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
      //System.out.println("writeOffsets=" + writeOffsets);
      //System.out.println("writePos=" + writePositions);
    }

    @Override
    public void startDoc(int docID, int termDocFreq)  {
      if (!wroteTerm) {
        // we lazily do this, in case the term had zero docs
        write(TERM);
        write(term);
        newline();
        wroteTerm = true;
      }

      write(DOC);
      write(Integer.toString(docID));
      newline();
      if (indexOptions != IndexOptions.DOCS_ONLY) {
        write(FREQ);
        write(Integer.toString(termDocFreq));
        newline();
      }

      lastStartOffset = 0;
    }
    
    public PostingsConsumer reset(BytesRef term) {
      this.term = term;
      wroteTerm = false;
      return this;
    }

    @Override
    public void addPosition(int position, BytesRef payload, int startOffset, int endOffset)  {
      if (writePositions) {
        write(POS);
        write(Integer.toString(position));
        newline();
      }

      if (writeOffsets) {
        Debug.Assert( endOffset >= startOffset;
        Debug.Assert( startOffset >= lastStartOffset: "startOffset=" + startOffset + " lastStartOffset=" + lastStartOffset;
        lastStartOffset = startOffset;
        write(START_OFFSET);
        write(Integer.toString(startOffset));
        newline();
        write(END_OFFSET);
        write(Integer.toString(endOffset));
        newline();
      }

      if (payload != null && payload.length > 0) {
        Debug.Assert( payload.length != 0;
        write(PAYLOAD);
        write(payload);
        newline();
      }
    }

    @Override
    public void finishDoc() {
    }
  }

  @Override
  public void close()  {
    if (out != null) {
      try {
        write(END);
        newline();
        SimpleTextUtil.writeChecksum(out, scratch);
      } finally {
        out.close();
        out = null;
      }
    }
  }
}
