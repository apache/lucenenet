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

namespace Lucene.Net.ApiCheck.Extensions;

/// <summary>
/// Extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    public static IEnumerable<string> SortJavaModifiers(this IEnumerable<string> modifiers)
    {
        return modifiers.OrderBy(i => i switch
        {
            "public" => 1,
            "protected" => 2,
            "private" => 3,
            "abstract" => 4,
            "static" => 5,
            "final" => 6,
            "synchronized" => 7,
            "volatile" => 8,
            "transient" => 9,
            "native" => 10,
            "strictfp" => 11,
            _ => 12
        });
    }

    public static IEnumerable<string> SortDotNetModifiers(this IEnumerable<string> modifiers)
    {
        return modifiers.OrderBy(i => i switch
        {
            "public" => 1,
            "protected" => 2,
            "private" => 3,
            "internal" => 4,
            "protected internal" => 5,
            "private protected" => 6,
            "abstract" => 7,
            "sealed" => 8,
            "static" => 9,
            "readonly" => 10,
            "volatile" => 11,
            "extern" => 12,
            "unsafe" => 13,
            "new" => 14,
            "virtual" => 15,
            "override" => 16,
            "async" => 17,
            "const" => 18,
            _ => 22
        });
    }
}
