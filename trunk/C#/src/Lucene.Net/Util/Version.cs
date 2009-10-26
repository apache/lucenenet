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
	
	/// <summary> Use by certain classes to match version compatibility
	/// across releases of Lucene.
	/// </summary>
	[Serializable]
	public sealed class Version:Parameter
	{
		
		/// <summary>Use this to get the latest & greatest settings, bug
		/// fixes, etc, for Lucene.
		/// 
		/// <p><b>WARNING</b>: if you use this setting, and then
		/// upgrade to a newer release of Lucene, sizable changes
		/// may happen.  If precise back compatibility is important
		/// then you should instead explicitly specify an actual
		/// version.
		/// </summary>
		public static readonly Version LUCENE_CURRENT = new Version("LUCENE_CURRENT", 0);
		
		/// <summary>Match settings and bugs in Lucene's 2.4 release.</summary>
		/// <deprecated> This will be removed in 3.0 
		/// </deprecated>
		public static readonly Version LUCENE_24 = new Version("LUCENE_24", 2400);
		
		/// <summary>Match settings and bugs in Lucene's 2.9 release.</summary>
		/// <deprecated> This will be removed in 3.0 
		/// </deprecated>
		public static readonly Version LUCENE_29 = new Version("LUCENE_29", 2900);
		
		private int v;
		
		public Version(System.String name, int v):base(name)
		{
			this.v = v;
		}
		
		public bool OnOrAfter(Version other)
		{
			return v == 0 || v >= other.v;
		}
	}
}