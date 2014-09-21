package codecs.memory;


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

import codecs.FieldsConsumer;
import codecs.FieldsProducer;
import codecs.PostingsFormat;
import codecs.PostingsReaderBase;
import codecs.PostingsWriterBase;
import codecs.lucene41.Lucene41PostingsWriter;
import codecs.lucene41.Lucene41PostingsReader;
import index.FieldInfo.IndexOptions;
import index.SegmentReadState;
import index.SegmentWriteState;
import util.IOUtils;

/**
 * FST term dict + Lucene41PBF
 */

public final class FSTPostingsFormat extends PostingsFormat {
  public FSTPostingsFormat() {
    super("FST41");
  }

  @Override
  public String toString() {
    return getName();
  }

  @Override
  public FieldsConsumer fieldsConsumer(SegmentWriteState state)  {
    PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);

    bool success = false;
    try {
      FieldsConsumer ret = new FSTTermsWriter(state, postingsWriter);
      success = true;
      return ret;
    } finally {
      if (!success) {
        IOUtils.closeWhileHandlingException(postingsWriter);
      }
    }
  }

  @Override
  public FieldsProducer fieldsProducer(SegmentReadState state)  {
    PostingsReaderBase postingsReader = new Lucene41PostingsReader(state.directory,
                                                                state.fieldInfos,
                                                                state.segmentInfo,
                                                                state.context,
                                                                state.segmentSuffix);
    bool success = false;
    try {
      FieldsProducer ret = new FSTTermsReader(state, postingsReader);
      success = true;
      return ret;
    } finally {
      if (!success) {
        IOUtils.closeWhileHandlingException(postingsReader);
      }
    }
  }
}
