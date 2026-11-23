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

namespace Lucene.Net.ApiCheck.Models.Diff;

/// <summary>
/// Captures a member that exists on both sides but differs in some way: type
/// (field type, return type, parameter types) and/or modifiers.
/// </summary>
public class MemberDiff
{
    public required ComparisonPair<MemberReference> MatchedMember { get; set; }

    /// <summary>
    /// Set when the member's "type" (field type, property type, or method/ctor
    /// parameter or return types) differs between the two sides.
    /// </summary>
    public bool HasTypeMismatch { get; set; }

    /// <summary>
    /// Set when the member's modifiers differ in a way not accounted for by the
    /// Java↔.NET equivalence rules.
    /// </summary>
    public bool HasModifierMismatch { get; set; }
}
