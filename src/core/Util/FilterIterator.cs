using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    //LUCENE TO-DO: Iterators are not needed. System.Linq uses enumerators plenty well
    /*
	/// <summary>
	/// Licensed to the Apache Software Foundation (ASF) under one or more
	/// contributor license agreements. See the NOTICE file distributed with this
	/// work for additional information regarding copyright ownership. The ASF
	/// licenses this file to You under the Apache License, Version 2.0 (the
	/// "License"); you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	/// http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
	/// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
	/// License for the specific language governing permissions and limitations under
	/// the License.
	/// </summary>

	/// <summary>
	/// An <seealso cref="Iterator"/> implementation that filters elements with a boolean predicate. </summary>
	/// <seealso cref= #predicateFunction </seealso>
	public abstract class FilterIterator<T> : Iterator<T>
	{

	  private readonly IEnumerator<T> Iterator;
	  private T next = default(T);
	  private bool NextIsSet = false;

	  /// <summary>
	  /// returns true, if this element should be returned by <seealso cref="#next()"/>. </summary>
	  protected internal abstract bool PredicateFunction(T @object);

	  public FilterIterator(IEnumerator<T> baseIterator)
	  {
		this.Iterator = baseIterator;
	  }

	  public override bool HasNext()
	  {
		return NextIsSet || SetNext();
	  }

	  public override T Next()
	  {
		if (!HasNext())
		{
		  throw new NoSuchElementException();
		}
		Debug.Assert(NextIsSet);
		try
		{
		  return next;
		}
		finally
		{
		  NextIsSet = false;
		  next = default(T);
		}
	  }

	  public override void Remove()
	  {
		throw new System.NotSupportedException();
	  }

	  private bool SetNext()
	  {
		while (Iterator.MoveNext())
		{
		  T @object = Iterator.Current;
		  if (PredicateFunction(@object))
		  {
			next = @object;
			NextIsSet = true;
			return true;
		  }
		}
		return false;
	  }
	}
    */
}