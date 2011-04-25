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
using System.Collections.Generic;

using AttributeSource = Lucene.Net.Util.AttributeSource;

namespace Lucene.Net.Index
{
	
	/// <summary>This is a DocFieldConsumer that inverts each field,
	/// separately, from a Document, and accepts a
	/// InvertedTermsConsumer to process those terms. 
	/// </summary>
	
	sealed class DocInverter:DocFieldConsumer
	{
		
		internal InvertedDocConsumer consumer;
		internal InvertedDocEndConsumer endConsumer;
		
		public DocInverter(InvertedDocConsumer consumer, InvertedDocEndConsumer endConsumer)
		{
			this.consumer = consumer;
			this.endConsumer = endConsumer;
		}
		
		internal override void  SetFieldInfos(FieldInfos fieldInfos)
		{
			base.SetFieldInfos(fieldInfos);
			consumer.SetFieldInfos(fieldInfos);
			endConsumer.SetFieldInfos(fieldInfos);
		}

        public override void Flush(SupportClass.Dictionary<DocFieldConsumerPerThread, IList<DocFieldConsumerPerField>> threadsAndFields, SegmentWriteState state)
		{

            SupportClass.Dictionary<InvertedDocConsumerPerThread, IList<InvertedDocConsumerPerField>> childThreadsAndFields = new SupportClass.Dictionary<InvertedDocConsumerPerThread, IList<InvertedDocConsumerPerField>>();
            SupportClass.Dictionary<InvertedDocEndConsumerPerThread, IList<InvertedDocEndConsumerPerField>> endChildThreadsAndFields = new SupportClass.Dictionary<InvertedDocEndConsumerPerThread, IList<InvertedDocEndConsumerPerField>>();

            foreach (KeyValuePair<DocFieldConsumerPerThread, IList<DocFieldConsumerPerField>> entry in new SupportClass.Dictionary<DocFieldConsumerPerThread, IList<DocFieldConsumerPerField>>(threadsAndFields))
            {
                DocInverterPerThread perThread = (DocInverterPerThread)entry.Key;
                                
                List<InvertedDocConsumerPerField> childFields = new List<InvertedDocConsumerPerField>();
                List<InvertedDocEndConsumerPerField> endChildFields = new List<InvertedDocEndConsumerPerField>();
                foreach (DocFieldConsumerPerField field in entry.Value)
                {
                    DocInverterPerField perField = (DocInverterPerField)field;
                    childFields.Add(perField.consumer);
                    endChildFields.Add(perField.endConsumer);
                }

                childThreadsAndFields[perThread.consumer] = childFields;
                endChildThreadsAndFields[perThread.endConsumer] = endChildFields;
            }
			
			consumer.Flush(childThreadsAndFields, state);
			endConsumer.Flush(endChildThreadsAndFields, state);
		}
		
		public override void  CloseDocStore(SegmentWriteState state)
		{
			consumer.CloseDocStore(state);
			endConsumer.CloseDocStore(state);
		}
		
		public override void  Abort()
		{
			consumer.Abort();
			endConsumer.Abort();
		}
		
		public override bool FreeRAM()
		{
			return consumer.FreeRAM();
		}
		
		public override DocFieldConsumerPerThread AddThread(DocFieldProcessorPerThread docFieldProcessorPerThread)
		{
			return new DocInverterPerThread(docFieldProcessorPerThread, this);
		}
	}
}