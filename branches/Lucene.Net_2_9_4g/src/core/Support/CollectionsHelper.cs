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

using System.Collections;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Support class used to handle Hashtable addition, which does a check 
    /// first to make sure the added item is unique in the hash.
    /// </summary>
    public class CollectionsHelper
    {
        public static void Add(System.Collections.Hashtable hashtable, System.Object item)
        {
            hashtable.Add(item, item);
        }

        public static void AddIfNotContains(System.Collections.Hashtable hashtable, System.Object item)
        {
            if (hashtable.Contains(item) == false)
            {
                hashtable.Add(item, item);
            }
        }

        public static void AddAllIfNotContains(System.Collections.Generic.IDictionary<string,string> hashtable, System.Collections.Generic.ICollection<string> items)
        {
            foreach (string s in items)
            {
                if (hashtable.ContainsKey(s) == false)
                {
                    hashtable.Add(s, s);
                }
            }
        }
        
        public static System.String CollectionToString<T>(System.Collections.Generic.IList<T> c)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for(int i=0;i<c.Count;i++)
            {
                sb.Append(c[i].ToString());
                if (i != c.Count - 1) sb.Append(", ");
            }
            return sb.ToString();
        }

        public static System.String CollectionToString(System.Collections.Generic.IDictionary<string, string> c)
        {
            Hashtable t = new Hashtable();
            foreach (string key in c.Keys)
            {
                t.Add(key, c[key]);
            }
            return CollectionToString(t);
        }

        /// <summary>
        /// Converts the specified collection to its string representation.
        /// </summary>
        /// <param name="c">The collection to convert to string.</param>
        /// <returns>A string representation of the specified collection.</returns>
        public static System.String CollectionToString(System.Collections.ICollection c)
        {
            System.Text.StringBuilder s = new System.Text.StringBuilder();

            if (c != null)
            {

                System.Collections.ArrayList l = new System.Collections.ArrayList(c);

                bool isDictionary = (c is System.Collections.BitArray || c is System.Collections.Hashtable || c is System.Collections.IDictionary || c is System.Collections.Specialized.NameValueCollection || (l.Count > 0 && l[0] is System.Collections.DictionaryEntry));
                for (int index = 0; index < l.Count; index++)
                {
                    if (l[index] == null)
                        s.Append("null");
                    else if (!isDictionary)
                        s.Append(l[index]);
                    else
                    {
                        isDictionary = true;
                        if (c is System.Collections.Specialized.NameValueCollection)
                            s.Append(((System.Collections.Specialized.NameValueCollection)c).GetKey(index));
                        else
                            s.Append(((System.Collections.DictionaryEntry)l[index]).Key);
                        s.Append("=");
                        if (c is System.Collections.Specialized.NameValueCollection)
                            s.Append(((System.Collections.Specialized.NameValueCollection)c).GetValues(index)[0]);
                        else
                            s.Append(((System.Collections.DictionaryEntry)l[index]).Value);

                    }
                    if (index < l.Count - 1)
                        s.Append(", ");
                }

                if (isDictionary)
                {
                    if (c is System.Collections.ArrayList)
                        isDictionary = false;
                }
                if (isDictionary)
                {
                    s.Insert(0, "{");
                    s.Append("}");
                }
                else
                {
                    s.Insert(0, "[");
                    s.Append("]");
                }
            }
            else
                s.Insert(0, "null");
            return s.ToString();
        }

        public static void Sort<T1>(System.Collections.Generic.IList<T1> list, System.Collections.Generic.IComparer<T1> Comparator)
        {
            if (list.IsReadOnly) throw new System.NotSupportedException();
            if (Comparator == null) ((System.Collections.Generic.List<T1>)list).Sort();
            else ((System.Collections.Generic.List<T1>)list).Sort(Comparator);
        }
                
        /// <summary>
        /// Compares the entire members of one array whith the other one.
        /// </summary>
        /// <param name="array1">The array to be compared.</param>
        /// <param name="array2">The array to be compared with.</param>
        /// <returns>Returns true if the two specified arrays of Objects are equal 
        /// to one another. The two arrays are considered equal if both arrays 
        /// contain the same number of elements, and all corresponding pairs of 
        /// elements in the two arrays are equal. Two objects e1 and e2 are 
        /// considered equal if (e1==null ? e2==null : e1.equals(e2)). In other 
        /// words, the two arrays are equal if they contain the same elements in 
        /// the same order. Also, two array references are considered equal if 
        /// both are null.</returns>
        public static bool Equals(System.Array array1, System.Array array2)
        {
            bool result = false;
            if ((array1 == null) && (array2 == null))
                result = true;
            else if ((array1 != null) && (array2 != null))
            {
                if (array1.Length == array2.Length)
                {
                    int length = array1.Length;
                    result = true;
                    for (int index = 0; index < length; index++)
                    {
                        System.Object o1 = array1.GetValue(index);
                        System.Object o2 = array2.GetValue(index);
                        if (o1 == null && o2 == null)
                            continue;   // they match
                        else if (o1 == null || !o1.Equals(o2))
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            return result;
        }
    }
}