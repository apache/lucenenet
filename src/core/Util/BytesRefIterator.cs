using System.Collections.Generic;

namespace Lucene.Net.Util
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

    // LUCENE TO-DO rewrote entire class

    public interface IBytesRefIterator
    {
        BytesRef Next();

        IComparer<BytesRef> Comparator { get; }
    }

    // .NET Port: in Java, you can have static fields and anonymous classes inside interfaces.
    // Here, we're naming a static class similarly to the Java interface to mimic this behavior.
    public static class BytesRefIterator
    {
        public static readonly IBytesRefIterator EMPTY = new EmptyBytesRefIterator();

        private class EmptyBytesRefIterator : IBytesRefIterator
        {
            public BytesRef Next()
            {
                return null;
            }

            public IComparer<BytesRef> Comparator
            {
                get { return null; }
            }
        }
    }

    /*
    public class BytesRefIterator
    {
        /// <summary>
        /// Increments the iteration to the next <seealso cref="BytesRef"/> in the iterator.
        /// Returns the resulting <seealso cref="BytesRef"/> or <code>null</code> if the end of
        /// the iterator is reached. The returned BytesRef may be re-used across calls
        /// to next. After this method returns null, do not call it again: the results
        /// are undefined.
        /// </summary>
        /// <returns> the next <seealso cref="BytesRef"/> in the iterator or <code>null</code> if
        ///         the end of the iterator is reached. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public abstract BytesRef Next();

        /// <summary>
	    /// Return the <seealso cref="BytesRef"/> Comparator used to sort terms provided by the
	    /// iterator. this may return null if there are no items or the iterator is not
	    /// sorted. Callers may invoke this method many times, so it's best to cache a
	    /// single instance & reuse it.
	    /// </summary>
        public abstract IComparer<BytesRef> Comparator { get; }

        public static BytesRefIteratorHelper EMPTY = BytesRefIteratorHelper.CreateBytesRefIteratorHelper();
    }

    /// <summary>
    /// Singleton BytesRefIterator that iterates over 0 BytesRefs. </summary>
    public class BytesRefIteratorHelper : BytesRefIterator
    {
        private BytesRefIteratorHelper()
        {
            me = null;
        }
        private static BytesRefIteratorHelper me;
        public static BytesRefIteratorHelper CreateBytesRefIteratorHelper()
        {
            if (BytesRefIteratorHelper.me == null)
                return new BytesRefIteratorHelper();
            return me;
        }

        public override BytesRef Next()
        {
            return null;
        }

        public override IComparer<BytesRef> Comparator
        {
            get;
        }
    }*/

    /*

	/// <summary>
	/// A simple iterator interface for <seealso cref="BytesRef"/> iteration.
	/// </summary>
	public interface BytesRefIterator
	{
	  /// <summary>
	  /// Increments the iteration to the next <seealso cref="BytesRef"/> in the iterator.
	  /// Returns the resulting <seealso cref="BytesRef"/> or <code>null</code> if the end of
	  /// the iterator is reached. The returned BytesRef may be re-used across calls
	  /// to next. After this method returns null, do not call it again: the results
	  /// are undefined.
	  /// </summary>
	  /// <returns> the next <seealso cref="BytesRef"/> in the iterator or <code>null</code> if
	  ///         the end of the iterator is reached. </returns>
	  /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
	  BytesRef Next();

	  /// <summary>
	  /// Return the <seealso cref="BytesRef"/> Comparator used to sort terms provided by the
	  /// iterator. this may return null if there are no items or the iterator is not
	  /// sorted. Callers may invoke this method many times, so it's best to cache a
	  /// single instance & reuse it.
	  /// </summary>
	  IComparer<BytesRef> Comparator {get;}

	  /// <summary>
	  /// Singleton BytesRefIterator that iterates over 0 BytesRefs. </summary>
//	  public static final BytesRefIterator EMPTY = new BytesRefIteratorAnonymousInnerClassHelper();
	}

	public static class BytesRefIterator_Fields
	{
		  //public static readonly return Null;

		  private class BytesRefIteratorAnonymousInnerClassHelper : BytesRefIterator
		  {
			  public BytesRefIteratorAnonymousInnerClassHelper()
			  {
			  }

			  public override BytesRef Next()
			  {
			  }

			  public override IComparer<BytesRef> Comparator
			  {
				  get
				  {
				  }
			  }
		  }
		  //public static readonly return Null;
	}
    */
}