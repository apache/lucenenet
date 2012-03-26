using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Lucene.Net.Index.Memory
{
    /// <summary>
    /// Index data structure for a field; Contains the tokenized term texts
    /// and their positions
    /// </summary>
    internal class Info : ISerializable {
    
        /**
         * Term strings and their positions for this field: Map <String
         * termText, List<Int32> positions>
         */
        public SortedDictionary<String, List<Int32>> SortedTerms { get; private set; }
        
        /** Number of added tokens for this field */
        private int numTokens;
    
        /** Number of overlapping tokens for this field */
        private int numOverlapTokens;
    
        /** Boost factor for hits for this field */
        public float Boost { get; private set; }

        /** Term for this field's fieldName, lazily computed on demand */
        public Term template;

        private long serialVersionUID = 2882195016849084649L;  

        public Info(IDictionary<string, List<int>> terms, int numTokens, int numOverlapTokens, float boost) {
            SortedTerms = new SortedDictionary<string, List<int>>(terms);
            Boost = boost;
        
            this.numTokens = numTokens;
            this.numOverlapTokens = numOverlapTokens;
        }
    
        public List<Int32> GetPositions(string pos)
        {
            return SortedTerms[pos];
        }

        public List<Int32> GetPositions(int pos) {
            return SortedTerms.ElementAt(pos).Value;
        }

        #region Implementation of ISerializable

        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
