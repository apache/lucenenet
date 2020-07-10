namespace Lucene.Net.Codecs.SimpleText
{
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

    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;

    /// <summary>
    /// Plain text doc values format.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// <para>
    /// The .dat file contains the data.
    /// For numbers this is a "fixed-width" file, for example a single byte range:
    /// <code>
    ///  field myField
    ///    type NUMERIC
    ///    minvalue 0
    ///    pattern 000
    ///  005
    ///  T
    ///  234
    ///  T
    ///  123
    ///  T
    ///  ...
    /// </code>
    /// So a document's value (delta encoded from minvalue) can be retrieved by 
    /// seeking to startOffset + (1+pattern.length()+2)*docid. The extra 1 is the newline. 
    /// The extra 2 is another newline and 'T' or 'F': true if the value is real, false if missing.
    ///  
    /// for bytes this is also a "fixed-width" file, for example:
    /// <code>
    ///  field myField
    ///    type BINARY
    ///    maxlength 6
    ///    pattern 0
    ///  length 6
    ///  foobar[space][space]
    ///  T
    ///  length 3
    ///  baz[space][space][space][space][space]
    ///  T
    ///  ...
    /// </code>
    /// So a doc's value can be retrieved by seeking to startOffset + (9+pattern.length+maxlength+2)*doc
    /// the extra 9 is 2 newlines, plus "length " itself.
    /// The extra 2 is another newline and 'T' or 'F': true if the value is real, false if missing.
    ///  
    /// For sorted bytes this is a fixed-width file, for example:
    /// <code>
    ///  field myField
    ///    type SORTED
    ///    numvalues 10
    ///    maxLength 8
    ///    pattern 0
    ///    ordpattern 00
    ///  length 6
    ///  foobar[space][space]
    ///  length 3
    ///  baz[space][space][space][space][space]
    ///  ...
    ///  03
    ///  06
    ///  01
    ///  10
    ///  ...
    /// </code>
    /// So the "ord section" begins at startOffset + (9+pattern.length+maxlength)*numValues.
    /// A document's ord can be retrieved by seeking to "ord section" + (1+ordpattern.length())*docid
    /// an ord's value can be retrieved by seeking to startOffset + (9+pattern.length+maxlength)*ord
    ///  
    /// For sorted set this is a fixed-width file very similar to the SORTED case, for example:
    /// <code>
    ///  field myField
    ///    type SORTED_SET
    ///    numvalues 10
    ///    maxLength 8
    ///    pattern 0
    ///    ordpattern XXXXX
    ///  length 6
    ///  foobar[space][space]
    ///  length 3
    ///  baz[space][space][space][space][space]
    ///  ...
    ///  0,3,5   
    ///  1,2
    ///  
    ///  10
    ///  ...
    /// </code>
    /// So the "ord section" begins at startOffset + (9+pattern.length+maxlength)*numValues.
    /// A document's ord list can be retrieved by seeking to "ord section" + (1+ordpattern.length())*docid
    /// this is a comma-separated list, and its padded with spaces to be fixed width. so trim() and split() it.
    /// and beware the empty string!
    /// An ord's value can be retrieved by seeking to startOffset + (9+pattern.length+maxlength)*ord
    /// <para/> 
    /// The reader can just scan this file when it opens, skipping over the data blocks
    /// and saving the offset/etc for each field.
    /// <para/>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    [DocValuesFormatName("SimpleText")] // LUCENENET specific - using DocValuesFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class SimpleTextDocValuesFormat : DocValuesFormat
    {
        public SimpleTextDocValuesFormat() 
            : base()
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new SimpleTextDocValuesWriter(state, "dat");
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new SimpleTextDocValuesReader(state, "dat");
        }
    }
}