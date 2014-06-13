using System.Collections.Generic;

namespace Lucene.Net.Codecs
{

    using Lucene.Net.Support;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

	/// <summary>
	/// a utility class to write missing values for SORTED as if they were the empty string
	/// (to simulate pre-Lucene4.5 dv behavior for testing old codecs)
	/// </summary>
	public class MissingOrdRemapper
	{

	  /// <summary>
	  /// insert an empty byte[] to the front of this iterable </summary>
	  public static IEnumerable<BytesRef> InsertEmptyValue(IEnumerable<BytesRef> iterable)
	  {
		return new IterableAnonymousInnerClassHelper(iterable);
	  }

      private class IterableAnonymousInnerClassHelper : IEnumerable<BytesRef>
	  {
		  private IEnumerable<BytesRef> Iterable;

		  public IterableAnonymousInnerClassHelper(IEnumerable<BytesRef> iterable)
		  {
			  this.Iterable = iterable;
		  }

		  public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			return new IteratorAnonymousInnerClassHelper(this);
		  }

		  private class IteratorAnonymousInnerClassHelper : IEnumerator<BytesRef>
		  {
			  private readonly IterableAnonymousInnerClassHelper OuterInstance;

			  public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance)
			  {
				  this.OuterInstance = outerInstance;
				  seenEmpty = false;
				  @in = outerInstance.Iterable.GetEnumerator();
			  }

			  internal bool seenEmpty;
			  internal IEnumerator<BytesRef> @in;

			  public virtual bool HasNext()
			  {
				return !seenEmpty || @in.hasNext();
			  }

			  public virtual BytesRef Next()
			  {
				if (!seenEmpty)
				{
				  seenEmpty = true;
				  return new BytesRef();
				}
				else
				{
				  return @in.next();
				}
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException();
			  }
		  }
	  }

	  /// <summary>
	  /// remaps ord -1 to ord 0 on this iterable. </summary>
	  public static IEnumerable<long> MapMissingToOrd0(IEnumerable<long> iterable)
	  {
		return new IterableAnonymousInnerClassHelper2(iterable);
	  }

      private class IterableAnonymousInnerClassHelper2 : IEnumerable<long>
	  {
          private IEnumerable<long> Iterable;

          public IterableAnonymousInnerClassHelper2(IEnumerable<long> iterable)
		  {
			  this.Iterable = iterable;
		  }

          public virtual IEnumerator<long> GetEnumerator()
		  {
			return new IteratorAnonymousInnerClassHelper2(this);
		  }

          private class IteratorAnonymousInnerClassHelper2 : IEnumerator<long>
		  {
			  private readonly IterableAnonymousInnerClassHelper2 OuterInstance;

			  public IteratorAnonymousInnerClassHelper2(IterableAnonymousInnerClassHelper2 outerInstance)
			  {
				  this.OuterInstance = outerInstance;
				  @in = outerInstance.Iterable.GetEnumerator();
			  }

              internal IEnumerator<long> @in;

			  public virtual bool HasNext()
			  {
				return @in.hasNext();
			  }

              public virtual long Next()
			  {
				long n = @in.next();
				if ((long)n == -1)
				{
				  return 0;
				}
				else
				{
				  return n;
				}
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException();
			  }
		  }
	  }

	  /// <summary>
	  /// remaps every ord+1 on this iterable </summary>
      public static IEnumerable<long> MapAllOrds(IEnumerable<long> iterable)
	  {
		return new IterableAnonymousInnerClassHelper3(iterable);
	  }

      private class IterableAnonymousInnerClassHelper3 : IEnumerable<long>
	  {
          private IEnumerable<long> Iterable;

          public IterableAnonymousInnerClassHelper3(IEnumerable<long> iterable)
		  {
			  this.Iterable = iterable;
		  }

          public virtual IEnumerator<long> GetEnumerator()
		  {
			return new IteratorAnonymousInnerClassHelper3(this);
		  }

          private class IteratorAnonymousInnerClassHelper3 : IEnumerator<long>
		  {
			  private readonly IterableAnonymousInnerClassHelper3 OuterInstance;

			  public IteratorAnonymousInnerClassHelper3(IterableAnonymousInnerClassHelper3 outerInstance)
			  {
				  this.OuterInstance = outerInstance;
				  @in = outerInstance.Iterable.GetEnumerator();
			  }

              internal IEnumerator<long> @in;

			  public virtual bool HasNext()
			  {
				return @in.hasNext();
			  }

              public virtual long Next()
			  {
				long n = @in.next();
				return (long)n + 1;
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException();
			  }
		  }
	  }
	}

}