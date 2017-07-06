using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Cli
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

    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> OptionalParameters<T>(this IEnumerable<IEnumerable<T>> input)
        {
            if (input.Count() == 0)
                yield break;
            else
            {
                var rest = OptionalParameters(input.Skip(1));
                foreach (var p in input.First())
                {
                    yield return new[] { p };
                    foreach (var r in rest)
                        yield return r.Prepend(p);
                }
                foreach (var r in rest)
                    yield return r;
            }
        }

        public static IEnumerable<IEnumerable<T>> RequiredParameters<T>(this IEnumerable<IEnumerable<T>> input)
        {
            int count = input.Count();
            if (count == 1)
                foreach (var p in input.First())
                    yield return new[] { p };
            else if (count > 1)
            {
                var rest = RequiredParameters(input.Skip(1));
                foreach (var p in input.First())
                {
                    foreach (var r in rest)
                        yield return r.Prepend(p);
                }
            }
        }
    }
}
