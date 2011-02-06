/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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

namespace Lucene.Net.Index
{
	
	/// <summary> Useful constants representing filenames and extensions used by lucene
	/// 
	/// </summary>
	/// <author>  Bernhard Messer
	/// </author>
	/// <version>  $rcs = ' $Id: Exp $ ' ;
	/// </version>
	sealed class IndexFileNames
	{
		
		/// <summary>Name of the index segment file </summary>
		internal const System.String SEGMENTS = "segments";
		
		/// <summary>Name of the index deletable file </summary>
		internal const System.String DELETABLE = "deletable";
		
		/// <summary> This array contains all filename extensions used by Lucene's index files, with
		/// one exception, namely the extension made up from <code>.f</code> + a number.
		/// Also note that two of Lucene's files (<code>deletable</code> and
		/// <code>segments</code>) don't have any filename extension.
		/// </summary>
		internal static readonly System.String[] INDEX_EXTENSIONS = new System.String[]{"cfs", "fnm", "fdx", "fdt", "tii", "tis", "frq", "prx", "del", "tvx", "tvd", "tvf", "tvp"};
		
		/// <summary>File extensions of old-style index files </summary>
		internal static readonly System.String[] COMPOUND_EXTENSIONS = new System.String[]{"fnm", "frq", "prx", "fdx", "fdt", "tii", "tis"};
		
		/// <summary>File extensions for term vector support </summary>
		internal static readonly System.String[] VECTOR_EXTENSIONS = new System.String[]{"tvx", "tvd", "tvf"};
	}
}