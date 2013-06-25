using Lucene.Net.Codecs;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal sealed class NormsConsumer : InvertedDocEndConsumer
    {
        public override void Abort()
        {
        }

        public override void Flush(IDictionary<string, InvertedDocEndConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            bool success = false;
            DocValuesConsumer normsConsumer = null;
            try
            {
                if (state.fieldInfos.HasNorms)
                {
                    NormsFormat normsFormat = state.segmentInfo.Codec.NormsFormat();
                    //assert normsFormat != null;
                    normsConsumer = normsFormat.NormsConsumer(state);

                    foreach (FieldInfo fi in state.fieldInfos)
                    {
                        NormsConsumerPerField toWrite = (NormsConsumerPerField)fieldsToFlush[fi.name];
                        // we must check the final value of omitNorms for the fieldinfo, it could have 
                        // changed for this field since the first time we added it.
                        if (!fi.OmitsNorms)
                        {
                            if (toWrite != null && !toWrite.IsEmpty)
                            {
                                toWrite.Flush(state, normsConsumer);
                                //assert fi.getNormType() == DocValuesType.NUMERIC;
                            }
                            else if (fi.IsIndexed)
                            {
                                //assert fi.getNormType() == null: "got " + fi.getNormType() + "; field=" + fi.name;
                            }
                        }
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(normsConsumer);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)normsConsumer);
                }
            }
        }

        public override void FinishDocument()
        {
        }

        public override void StartDocument()
        {
        }

        public override InvertedDocEndConsumerPerField AddField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo)
        {
            return new NormsConsumerPerField(docInverterPerField, fieldInfo, this);
        }
    }
}
