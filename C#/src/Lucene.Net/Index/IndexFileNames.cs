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

namespace Lucene.Net.Index
{
	
	/// <summary> Useful constants representing filenames and extensions used by lucene
	/// 
	/// </summary>
	/// <author>  Bernhard Messer
	/// </author>
	/// <version>  $rcs = ' $Id: Exp $ ' ;
	/// </version>
	sealed public class IndexFileNames
	{
		
		/// <summary>Name of the index segment file </summary>
		public const System.String SEGMENTS = "segments";
		
		/// <summary>Name of the generation reference file name </summary>
		public const System.String SEGMENTS_GEN = "segments.gen";
		
		/// <summary>Name of the index deletable file (only used in
		/// pre-lockless indices) 
		/// </summary>
		public const System.String DELETABLE = "deletable";
		
		/// <summary>Extension of norms file </summary>
		public const System.String NORMS_EXTENSION = "nrm";
		
		/// <summary> This array contains all filename extensions used by
		/// Lucene's index files, with two exceptions, namely the
		/// extension made up from <code>.f</code> + a number and
		/// from <code>.s</code> + a number.  Also note that
		/// Lucene's <code>segments_N</code> files do not have any
		/// filename extension.
		/// </summary>
		public static readonly System.String[] INDEX_EXTENSIONS = new System.String[]{"cfs", "fnm", "fdx", "fdt", "tii", "tis", "frq", "prx", "del", "tvx", "tvd", "tvf", "gen", "nrm"};
		
		/// <summary>File extensions of old-style index files </summary>
		public static readonly System.String[] COMPOUND_EXTENSIONS = new System.String[]{"fnm", "frq", "prx", "fdx", "fdt", "tii", "tis"};
		
		/// <summary>File extensions for term vector support </summary>
		public static readonly System.String[] VECTOR_EXTENSIONS = new System.String[]{"tvx", "tvd", "tvf"};
		
		/// <summary> Computes the full file name from base, extension and
		/// generation.  If the generation is -1, the file name is
		/// null.  If it's 0, the file name is <base><extension>.
		/// If it's > 0, the file name is <base>_<generation><extension>.
		/// 
		/// </summary>
		/// <param name="base">-- main part of the file name
		/// </param>
		/// <param name="extension">-- extension of the filename (including .)
		/// </param>
		/// <param name="gen">-- generation
		/// </param>
		public static System.String FileNameFromGeneration(System.String base_Renamed, System.String extension, long gen)
		{
			if (gen == - 1)
			{
				return null;
			}
			else if (gen == 0)
			{
				return base_Renamed + extension;
			}
			else
			{
				return base_Renamed + "_" + System.Convert.ToString(gen, 16) + extension;
			}
		}
	}
}