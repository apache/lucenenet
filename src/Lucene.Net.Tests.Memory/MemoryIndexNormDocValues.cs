/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Sharpen;

namespace Org.Apache.Lucene.Index.Memory
{
	/// <lucene.internal></lucene.internal>
	internal class MemoryIndexNormDocValues : NumericDocValues
	{
		private readonly long value;

		public MemoryIndexNormDocValues(long value)
		{
			this.value = value;
		}

		public override long Get(int docID)
		{
			if (docID != 0)
			{
				throw new IndexOutOfRangeException();
			}
			else
			{
				return value;
			}
		}
	}
}
