using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Util
{


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
	  private T Next_Renamed = null;
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
		  return Next_Renamed;
		}
		finally
		{
		  NextIsSet = false;
		  Next_Renamed = null;
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
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final T object = iterator.Current;
		  T @object = Iterator.Current;
		  if (PredicateFunction(@object))
		  {
			Next_Renamed = @object;
			NextIsSet = true;
			return true;
		  }
		}
		return false;
	  }
	}

}