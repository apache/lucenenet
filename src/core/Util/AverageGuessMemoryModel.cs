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

namespace Lucene.Net.Util
{
	
	/// <summary> An average, best guess, MemoryModel that should work okay on most systems.
	/// 
	/// </summary>
	public class AverageGuessMemoryModel:MemoryModel
	{
        public AverageGuessMemoryModel()
        {
            InitBlock();
        }

	    private void  InitBlock()
	    {
	        sizes = new IdentityDictionary<Type, int>()
	                    {
	                        {typeof (bool), 1},
	                        {typeof (byte), 1},
                            {typeof(sbyte), 1},
	                        {typeof (char), 2},
	                        {typeof (short), 2},
	                        {typeof (int), 4},
	                        {typeof (float), 4},
	                        {typeof (double), 8},
	                        {typeof (long), 8}
	                    };
	    }
		// best guess primitive sizes
        private System.Collections.Generic.Dictionary<Type, int> sizes;
		
		/*
		* (non-Javadoc)
		* 
		* <see cref="Lucene.Net.Util.MemoryModel.getArraySize()"/>
		*/

	    public override int ArraySize
	    {
	        get { return 16; }
	    }

	    /*
		* (non-Javadoc)
		* 
		* <see cref="Lucene.Net.Util.MemoryModel.getClassSize()"/>
		*/

	    public override int ClassSize
	    {
	        get { return 8; }
	    }

	    /* (non-Javadoc)
		* <see cref="Lucene.Net.Util.MemoryModel.getPrimitiveSize(java.lang.Class)"/>
		*/
		public override int GetPrimitiveSize(Type clazz)
		{
			return sizes[clazz];
		}
		
		/* (non-Javadoc)
		* <see cref="Lucene.Net.Util.MemoryModel.getReferenceSize()"/>
		*/

	    public override int ReferenceSize
	    {
	        get { return 4; }
	    }
	}
}