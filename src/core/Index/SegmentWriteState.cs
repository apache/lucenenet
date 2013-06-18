/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{	
	public class SegmentWriteState
	{
        public readonly InfoStream infoStream;
        public readonly Directory directory;
        public readonly SegmentInfo segmentInfo;
        public readonly FieldInfos fieldInfos;
        public int delCountOnFlush;
        public readonly BufferedDeletes segDeletes;
        public IMutableBits liveDocs;
        public readonly string segmentSuffix;
        public int termIndexInterval;
        public readonly IOContext context;

        public SegmentWriteState(InfoStream infoStream, Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos,
            int termIndexInterval, BufferedDeletes segDeletes, IOContext context)
        {
            this.infoStream = infoStream;
            this.segDeletes = segDeletes;
            this.directory = directory;
            this.segmentInfo = segmentInfo;
            this.fieldInfos = fieldInfos;
            this.termIndexInterval = termIndexInterval;
            segmentSuffix = "";
            this.context = context;
        }

        public SegmentWriteState(SegmentWriteState state, String segmentSuffix)
        {
            infoStream = state.infoStream;
            directory = state.directory;
            segmentInfo = state.segmentInfo;
            fieldInfos = state.fieldInfos;
            termIndexInterval = state.termIndexInterval;
            context = state.context;
            this.segmentSuffix = segmentSuffix;
            segDeletes = state.segDeletes;
            delCountOnFlush = state.delCountOnFlush;
        }
	}
}