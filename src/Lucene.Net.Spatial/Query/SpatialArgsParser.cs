using J2N.Text;
using Spatial4n.Context;
using Spatial4n.Exceptions;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lucene.Net.Spatial.Queries
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

    /// <summary>
    /// Parses a string that usually looks like "OPERATION(SHAPE)" into a <see cref="SpatialArgs"/>
    /// object. The set of operations supported are defined in <see cref="SpatialOperation"/>, such
    /// as "Intersects" being a common one. The shape portion is defined by WKT <see cref="Spatial4n.IO.WktShapeParser"/>,
    /// but it can be overridden/customized via <see cref="ParseShape(string, SpatialContext)"/>.
    /// There are some optional name-value pair parameters that follow the closing parenthesis.  Example:
    /// <code>
    ///   Intersects(ENVELOPE(-10,-8,22,20)) distErrPct=0.025
    /// </code>
    /// <para/>
    /// In the future it would be good to support something at least semi-standardized like a
    /// variant of <a href="http://docs.geoserver.org/latest/en/user/filter/ecql_reference.html#spatial-predicate">
    ///   [E]CQL</a>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class SpatialArgsParser
    {
        public const string DIST_ERR_PCT = "distErrPct";
        public const string DIST_ERR = "distErr";

        /// <summary>
        /// Writes a close approximation to the parsed input format.
        /// </summary>
        public static string WriteSpatialArgs(SpatialArgs args)
        {
            // LUCENENET specific - added guard clause
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            var str = new StringBuilder();
            str.Append(args.Operation.Name);
            str.Append('(');
            str.Append(args.Shape);
            if (args.DistErrPct != null)
                str.Append(" distErrPct=").Append(string.Format("{0:0.00}%", args.DistErrPct * 100d, CultureInfo.InvariantCulture));
            if (args.DistErr != null)
                str.Append(" distErr=").Append(args.DistErr);
            str.Append(')');
            return str.ToString();
        }

        /// <summary>
        /// Parses a string such as "Intersects(ENVELOPE(-10,-8,22,20)) distErrPct=0.025".
        /// </summary>
        /// <param name="v">The string to parse. Mandatory.</param>
        /// <param name="ctx">The spatial context. Mandatory.</param>
        /// <returns>Not null.</returns>
        /// <exception cref="ArgumentException">if the parameters don't make sense or an add-on parameter is unknown.</exception>
        /// <exception cref="Spatial4n.Exceptions.ParseException">If there is a problem parsing the string.</exception>
        /// <exception cref="InvalidShapeException">When the coordinates are invalid for the shape.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="v"/> or <paramref name="ctx"/> is <c>null</c>.</exception>
        public virtual SpatialArgs Parse(string v, SpatialContext ctx)
        {
            // LUCENENET specific - added guard clauses
            if (v is null)
                throw new ArgumentNullException(nameof(v));
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            int idx = v.IndexOf('(');
            int edx = v.LastIndexOf(')');

            if (idx < 0 || idx > edx)
            {
                throw new Spatial4n.Exceptions.ParseException("missing parens: " + v, -1);
            }

            SpatialOperation op = SpatialOperation.Get(v.Substring(0, idx - 0).Trim());

            //Substring in .NET is (startPosn, length), But in Java it's (startPosn, endPosn)
            //see http://docs.oracle.com/javase/1.4.2/docs/api/java/lang/String.html#substring(int, int)
            string body = v.Substring(idx + 1, edx - (idx + 1)).Trim();
            if (body.Length < 1)
            {
                throw new Spatial4n.Exceptions.ParseException("missing body : " + v, idx + 1);
            }

            var shape = ParseShape(body, ctx);
            var args = NewSpatialArgs(op, shape);

            if (v.Length > (edx + 1))
            {
                body = v.Substring(edx + 1).Trim();
                if (body.Length > 0)
                {
                    IDictionary<string, string> aa = ParseMap(body);
                    ReadNameValuePairs(args, aa);
                    if (aa.Count == 0)
                    {
                        throw new ArgumentException("unused parameters: " + aa);
                    }
                }
            }
            args.Validate();
            return args;
        }

        protected virtual SpatialArgs NewSpatialArgs(SpatialOperation op, IShape shape)
        {
            return new SpatialArgs(op, shape);
        }

        /// <exception cref="ArgumentNullException"><paramref name="args"/> or <paramref name="nameValPairs"/> is <c>null</c>.</exception>
        protected virtual void ReadNameValuePairs(SpatialArgs args, IDictionary<string, string> nameValPairs)
        {
            // LUCENENET specific - added guard clause
            if (args is null)
                throw new ArgumentNullException(nameof(args));
            if (nameValPairs is null)
                throw new ArgumentNullException(nameof(nameValPairs));

            nameValPairs.TryGetValue(DIST_ERR_PCT, out string? distErrPctStr);
            nameValPairs.TryGetValue(DIST_ERR, out string? distErrStr);
            args.DistErrPct = ReadDouble(distErrPctStr);
            nameValPairs.Remove(DIST_ERR_PCT);
            args.DistErr = ReadDouble(distErrStr);
            nameValPairs.Remove(DIST_ERR);
        }

        /// <exception cref="ArgumentNullException"><paramref name="str"/> or <paramref name="ctx"/> is <c>null</c>.</exception>
        protected virtual IShape ParseShape(string str, SpatialContext ctx) 
        {
            // LUCENENET specific - added guard clauses
            if (str is null)
                throw new ArgumentNullException(nameof(str));
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));
            
            //return ctx.readShape(str);//still in Spatial4n 0.4 but will be deleted
            return ctx.ReadShapeFromWkt(str);
        }

        protected static double? ReadDouble(string? v)
        {
            return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double val) ? val : (double?)null;
        }

        protected static bool ReadBool(string? v, bool defaultValue)
        {
            return bool.TryParse(v, out bool ret) ? ret : defaultValue;
        }

        /// <summary>
        /// Parses "a=b c=d f" (whitespace separated) into name-value pairs. If there
        /// is no '=' as in 'f' above then it's short for f=f.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <c>null</c>.</exception>
        protected static IDictionary<string, string> ParseMap(string body)
        {
            // LUCENENET specific - added guard clause
            if (body is null)
                throw new ArgumentNullException(nameof(body));

            var map = new Dictionary<string, string>();
            StringTokenizer st = new StringTokenizer(body, " \n\t");

            while (st.MoveNext())
            {
                string a = st.Current;
                int idx = a.IndexOf('=');
                if (idx > 0)
                {
                    string k = a.Substring(0, idx - 0);
                    string v = a.Substring(idx + 1);
                    map[k] = v;
                }
                else
                {
                    map[a] = a;
                }
            }

            return map;
        }
    }
}
