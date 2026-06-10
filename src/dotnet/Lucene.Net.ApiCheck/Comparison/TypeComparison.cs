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

using J2N;
using J2N.Text;
using Lucene.Net.ApiCheck.Extensions;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Reflection;
using Lucene.Net.Util;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Lucene.Net.ApiCheck.Comparison;

public static class TypeComparison
{
    private static readonly Dictionary<Type, HashSet<string>> WellKnownEquivalentTypes = new()
    {
        [typeof(object)] = ["java.lang.Object"],
        [typeof(ValueType)] = ["java.lang.Object"],
        [typeof(Enum)] = ["java.lang.Enum"],
        [typeof(Type)] = ["java.lang.Class"],
        [typeof(IOException)] = ["java.io.IOException"],
        [typeof(FileNotFoundException)] = ["java.io.FileNotFoundException"],
        // Java 'Throwable' is the root of all errors/exceptions; in .NET, Exception is the
        // typical analogue. Java 'Error' subclasses also map to Exception in practice.
        [typeof(Exception)] = ["java.lang.RuntimeException", "java.lang.Exception", "java.lang.Throwable", "java.lang.Error"],
        [typeof(IDisposable)] = ["java.lang.AutoCloseable", "java.io.Closeable"],
        // Lucene.NET's Lucene.Net.Util.ICloseable is the .NET analogue of the Java
        // close()-bearing marker interfaces (the #271 work item). Map it the same way as
        // IDisposable so types that implement it satisfy a Java Closeable/AutoCloseable.
        [typeof(ICloseable)] = ["java.io.Closeable", "java.lang.AutoCloseable"],
        [typeof(ICharSequence)] = ["java.lang.CharSequence"],
        [typeof(IAppendable)] = ["java.lang.Appendable"],
        [typeof(string)] = ["java.lang.String", "java.lang.CharSequence"],
        [typeof(TextReader)] = ["java.io.Reader"],
        // Lucene.NET ports java.io.PrintStream to TextWriter (see Support/IO/SafeTextWriterWrapper)
        // since both are character-oriented sinks. PrintStream is rare in the public API.
        [typeof(TextWriter)] = ["java.io.Writer", "java.io.PrintStream"],
        [typeof(Stream)] = ["java.io.InputStream", "java.io.OutputStream"],
        // java.io.File is a path that can refer to either a file or a directory; .NET ports
        // typically use FileInfo or DirectoryInfo depending on intent.
        [typeof(FileInfo)] = ["java.io.File"],
        [typeof(DirectoryInfo)] = ["java.io.File"],
        [typeof(Random)] = ["java.util.Random"],
        [typeof(Randomizer)] = ["java.util.Random"],
        [typeof(Regex)] = ["java.util.regex.Pattern"],
        // Boxed Java numeric types: Lucene.NET ports usually expose these as the underlying
        // value type or a nullable thereof. We can't represent nullable in this dictionary
        // (it'd require runtime-constructed Nullable<T>), so we map the value-type form.
        [typeof(int)] = ["java.lang.Integer"],
        [typeof(long)] = ["java.lang.Long"],
        [typeof(short)] = ["java.lang.Short"],
        [typeof(byte)] = ["java.lang.Byte"],
        [typeof(float)] = ["java.lang.Float"],
        [typeof(double)] = ["java.lang.Double"],
        [typeof(bool)] = ["java.lang.Boolean"],
        [typeof(char)] = ["java.lang.Character"],
        // Open-generic collection mappings: a Java erased List/Set/Map type can correspond
        // to several .NET collection types depending on how the porter chose to expose it.
        [typeof(IEnumerable<>)] = ["java.util.List", "java.util.Collection", "java.lang.Iterable", "java.util.Set"],
        [typeof(IEnumerable)] = ["java.util.List", "java.util.Collection", "java.lang.Iterable", "java.util.Set"],
        [typeof(ICollection<>)] = ["java.util.List", "java.util.Collection", "java.util.Set"],
        [typeof(IList<>)] = ["java.util.List"],
        [typeof(IReadOnlyCollection<>)] = ["java.util.List", "java.util.Collection", "java.util.Set"],
        [typeof(IReadOnlyList<>)] = ["java.util.List"],
        [typeof(List<>)] = ["java.util.List"],
        [typeof(ISet<>)] = ["java.util.Set"],
        [typeof(HashSet<>)] = ["java.util.HashSet", "java.util.Set"],
        [typeof(SortedSet<>)] = ["java.util.TreeSet", "java.util.SortedSet", "java.util.NavigableSet"],
        [typeof(IDictionary<,>)] = ["java.util.Map"],
        // Non-generic IDictionary: Lucene.NET sometimes exposes a non-generic map where Java has
        // the erased java.util.Map. The generic form above doesn't cover these.
        [typeof(IDictionary)] = ["java.util.Map"],
        [typeof(IReadOnlyDictionary<,>)] = ["java.util.Map"],
        [typeof(Dictionary<,>)] = ["java.util.HashMap", "java.util.Map"],
        [typeof(SortedDictionary<,>)] = ["java.util.TreeMap", "java.util.SortedMap", "java.util.NavigableMap"],
        [typeof(IEnumerator<>)] = ["java.util.Iterator"],
        [typeof(IEnumerator)] = ["java.util.Iterator"],
        [typeof(KeyValuePair<,>)] = ["java.util.Map$Entry"],
        [typeof(IComparer<>)] = ["java.util.Comparator"],
        [typeof(IComparer)] = ["java.util.Comparator"],
        [typeof(IComparable<>)] = ["java.lang.Comparable"],
        [typeof(IComparable)] = ["java.lang.Comparable"],
        // StringBuilder is a 1:1 BCL equivalent of java.lang.StringBuilder.
        [typeof(System.Text.StringBuilder)] = ["java.lang.StringBuilder"],
        // Lucene.NET maps Java locale/timezone/charset/text-format types to their BCL analogues.
        [typeof(System.Globalization.CultureInfo)] = ["java.util.Locale"],
        [typeof(TimeZoneInfo)] = ["java.util.TimeZone"],
        [typeof(System.Text.Encoding)] = ["java.nio.charset.Charset"],
        [typeof(DateTime)] = ["java.util.Date"],
        // Lucene.NET ports java.io.PrintWriter to TextWriter (alongside java.io.Writer/PrintStream
        // already mapped above) since all are character sinks.
        [typeof(TextWriter)] = ["java.io.Writer", "java.io.PrintStream", "java.io.PrintWriter"],
        [typeof(System.Xml.XmlElement)] = ["org.w3c.dom.Element"],
        // J2N boxed-numeric replacements for java.lang.* wrapper types. These are the J2N.Numerics
        // forms Lucene.NET exposes where Java has the boxed wrapper; the primitive forms are mapped
        // separately above via the BCL value types.
        [typeof(J2N.Numerics.Int32)] = ["java.lang.Integer"],
        [typeof(J2N.Numerics.Int64)] = ["java.lang.Long"],
        [typeof(J2N.Numerics.Int16)] = ["java.lang.Short"],
        [typeof(J2N.Numerics.Byte)] = ["java.lang.Byte"],
        [typeof(J2N.Numerics.SByte)] = ["java.lang.Byte"],
        [typeof(J2N.Numerics.Single)] = ["java.lang.Float"],
        [typeof(J2N.Numerics.Double)] = ["java.lang.Double"],
        [typeof(J2N.Numerics.Number)] = ["java.lang.Number"],
        // J2N atomics for the java.util.concurrent.atomic.* family.
        [typeof(J2N.Threading.Atomic.AtomicInt32)] = ["java.util.concurrent.atomic.AtomicInteger"],
        [typeof(J2N.Threading.Atomic.AtomicInt64)] = ["java.util.concurrent.atomic.AtomicLong"],
        [typeof(J2N.Threading.Atomic.AtomicBoolean)] = ["java.util.concurrent.atomic.AtomicBoolean"],
        // J2N IO and collections replacements.
        [typeof(J2N.IO.IDataInput)] = ["java.io.DataInput"],
        [typeof(J2N.IO.IDataOutput)] = ["java.io.DataOutput"],
        [typeof(J2N.Collections.BitSet)] = ["java.util.BitSet"],
    };

    // Equivalences keyed by .NET full type name, for types in assemblies the ApiCheck project does
    // not reference at compile time (so they can't be expressed via typeof). Spatial4n is the port
    // of the Java spatial4j library (package com.spatial4j.core); ICU4N ports a few java.text types.
    private static readonly Dictionary<string, HashSet<string>> WellKnownEquivalentTypeNames = new(StringComparer.Ordinal)
    {
        ["Spatial4n.Context.SpatialContext"] = ["com.spatial4j.core.context.SpatialContext"],
        ["Spatial4n.Shapes.IShape"] = ["com.spatial4j.core.shape.Shape"],
        ["Spatial4n.Shapes.IPoint"] = ["com.spatial4j.core.shape.Point"],
        ["Spatial4n.Shapes.IRectangle"] = ["com.spatial4j.core.shape.Rectangle"],
        ["Spatial4n.Shapes.SpatialRelation"] = ["com.spatial4j.core.shape.SpatialRelation"],
        ["Spatial4n.Distance.IDistanceCalculator"] = ["com.spatial4j.core.distance.DistanceCalculator"],
        ["ICU4N.Text.CharacterIterator"] = ["java.text.CharacterIterator"],
    };

    public static bool TypeMatchesFullName(Type? dotNetType, string? javaTypeName, string? javaTypeKind)
    {
        if (dotNetType is null && javaTypeName is null)
        {
            return true;
        }

        // handle classes that are mapped to interfaces in .NET, which don't have base types
        if (dotNetType is null && javaTypeName is not null && javaTypeName.Equals("java.lang.Object", StringComparison.Ordinal))
        {
            return true;
        }

        if (dotNetType is null || javaTypeName is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(javaTypeKind))
        {
            throw new ArgumentNullException(nameof(javaTypeKind));
        }

        var nameParts = javaTypeName.Split(".");

        var packageName = string.Join(".", nameParts[..^1]);

        var javaType = new TypeMetadata(packageName, javaTypeKind, nameParts[^1], javaTypeName, null, [], [], [], []);

        return TypesMatch(dotNetType, javaType);
    }

    /// <summary>
    /// Checks whether a .NET type matches a Java type name when the Java kind is not known
    /// (e.g., when comparing parameter types). This tries the common kinds and is more lenient.
    /// </summary>
    public static bool TypeMatchesFullNameAnyKind(Type? dotNetType, string? javaTypeName)
    {
        if (dotNetType is null && javaTypeName is null)
        {
            return true;
        }

        if (dotNetType is null || javaTypeName is null)
        {
            return false;
        }

        // Derive a likely Java kind from the .NET type so TypesMatch's kind check passes
        // for inferred LuceneType info matches.
        var likelyKind = dotNetType.GetTypeKind() switch
        {
            "interface" => "interface",
            "enum" => "enum",
            "struct" => "class",
            _ => "class"
        };

        return TypeMatchesFullName(dotNetType, javaTypeName, likelyKind);
    }

    public static bool TypesMatch(Type dotNetType, TypeMetadata javaType)
    {
        // Java generic types are erased, so the well-known mappings are keyed by the open
        // generic definition (e.g. IComparable<>, IEnumerator<>). Reduce a closed generic
        // (IComparable<Term>) to its definition before the lookup so it still matches.
        var lookupType = dotNetType.IsGenericType && !dotNetType.IsGenericTypeDefinition
            ? dotNetType.GetGenericTypeDefinition()
            : dotNetType;

        if (WellKnownEquivalentTypes.TryGetValue(lookupType, out HashSet<string>? wellKnownEquivalentTypes))
        {
            return wellKnownEquivalentTypes.Contains(javaType.FullName);
        }

        if (lookupType.FullName is { } lookupFullName
            && WellKnownEquivalentTypeNames.TryGetValue(lookupFullName, out HashSet<string>? wellKnownEquivalentTypeNames))
        {
            return wellKnownEquivalentTypeNames.Contains(javaType.FullName);
        }

        var javaFullNameParts = javaType.FullName.Split(".");
        var javaName = javaFullNameParts[^1].Replace("$", ".");

        if (dotNetType.GetCustomAttribute<LuceneTypeAttribute>() is { } luceneEquivalent
            && luceneEquivalent.PackageName.Equals(javaType.PackageName, StringComparison.Ordinal)
            && luceneEquivalent.TypeName.Equals(javaName, StringComparison.Ordinal))
        {
            return true;
        }

        var luceneTypeInfo = dotNetType.GetLuceneTypeInfo();

        if (luceneTypeInfo is null)
        {
            return false;
        }

        var cleanJavaName = javaType.Kind.Equals("class", StringComparison.Ordinal)
                            && javaName.EndsWith("Impl", StringComparison.Ordinal)
            ? javaName[..^4]
            : javaName;

        var equivalentJavaKind = dotNetType.GetTypeKind() switch
        {
            "interface" => "interface",
            "class" => "class",
            "enum" => "enum",
            "struct" => "class",
            _ => null
        };

        if (!string.Equals(equivalentJavaKind, javaType.Kind, StringComparison.Ordinal)
            || !string.Equals(luceneTypeInfo.PackageName, javaType.PackageName, StringComparison.Ordinal))
        {
            return false;
        }

        // Match against the Impl-stripped Java name (the common case: Java FooImpl <-> .NET Foo),
        // and also against the un-stripped name. The strip is one-directional (it only adjusts the
        // Java side), so a type kept as 'FooImpl' identically on both sides would otherwise fail to
        // match because GetInferredLuceneTypeName never strips Impl from the .NET name.
        if (string.Equals(luceneTypeInfo.TypeName, cleanJavaName, StringComparison.Ordinal)
            || string.Equals(luceneTypeInfo.TypeName, NormalizeNestedTypeName(cleanJavaName), StringComparison.Ordinal)
            || string.Equals(luceneTypeInfo.TypeName, javaName, StringComparison.Ordinal)
            || string.Equals(luceneTypeInfo.TypeName, NormalizeNestedTypeName(javaName), StringComparison.Ordinal))
        {
            return true;
        }

        // De-nesting: Lucene.NET frequently promotes a Java nested type (Outer$Inner) to a
        // top-level type to avoid naming collisions (e.g. BooleanClause.Occur -> Occur,
        // FieldInfo.IndexOptions -> IndexOptions). The Java name is then "Outer.Inner" while
        // the .NET inferred name is just "Inner". When the Java type is nested and the .NET
        // type is top-level, match on the innermost segment, still gated on the package and
        // kind equality checked above so the candidate must live in the same package.
        if (cleanJavaName.Contains('.') && !luceneTypeInfo.TypeName.Contains('.'))
        {
            var javaInnerName = cleanJavaName[(cleanJavaName.LastIndexOf('.') + 1)..];
            return string.Equals(luceneTypeInfo.TypeName, javaInnerName, StringComparison.Ordinal)
                || string.Equals(luceneTypeInfo.TypeName, NormalizeNestedTypeName(javaInnerName), StringComparison.Ordinal);
        }

        return false;
    }

    // Lucene.NET renames Java primitive type words inside type names (Int→Int32,
    // Long→Int64, Short→Int16, Float→Single) the same way it does for member names.
    // Type names can also contain '.' separators when a Java nested type has been
    // flattened to "Outer.Inner" form, so apply the rename to each segment so that
    // 'PackedInts.Header' becomes 'PackedInt32s.Header' rather than failing the
    // boundary lookahead at the '.'.
    private static string NormalizeNestedTypeName(string name)
    {
        if (!name.Contains('.'))
        {
            return MemberComparison.NormalizeJavaTypeWordsToDotNet(name);
        }

        var segments = name.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = MemberComparison.NormalizeJavaTypeWordsToDotNet(segments[i]);
        }
        return string.Join('.', segments);
    }

    // JVM marker interfaces that carry no members and have no .NET counterpart. Lucene.NET
    // does not (and cannot) implement these, so they should not count as a missing interface.
    private static readonly HashSet<string> MarkerInterfacesWithoutDotNetEquivalent = new(StringComparer.Ordinal)
    {
        "java.lang.Cloneable",
        "java.io.Serializable",
    };

    public static bool InterfacesMatch(IReadOnlyList<Type> dotNetInterfaces, IReadOnlyList<string> javaInterfaces)
    {
        // The Java extractor reports only the directly-declared interfaces, while .NET reflection
        // reports the full transitive interface set (one java.lang.Iterable becomes IEnumerable<T> +
        // IEnumerable; one java.util.List explodes to several BCL interfaces). Lucene.NET also
        // intentionally enriches the surface (IFormattable, IEquatable<T>, IDisposable, ...). So an
        // equal-count or ".NET subset of Java" rule is wrong: the correct invariant is that every
        // Java interface is represented somewhere on the .NET side. .NET-only additions are benign.
        return javaInterfaces
            // java.lang.Cloneable and java.io.Serializable are JVM marker interfaces Lucene.NET
            // intentionally drops; they have no .NET counterpart to match against.
            .Where(j => !MarkerInterfacesWithoutDotNetEquivalent.Contains(j))
            .All(j => dotNetInterfaces.Any(i => TypeMatchesFullName(i, j, "interface")));
    }

    /// <summary>
    /// Determines whether a .NET type's base type corresponds to the Java type's base type,
    /// accounting for two Lucene.NET porting idioms in addition to a direct match.
    /// </summary>
    public static bool BaseTypesMatch(Type dotNetType, string? javaBaseType)
    {
        // Direct match (including the WellKnownEquivalentTypes / LuceneType mappings).
        if (TypeMatchesFullName(dotNetType.BaseType, javaBaseType, "class"))
        {
            return true;
        }

        if (javaBaseType is null)
        {
            return false;
        }

        // Interface-ification: Lucene.NET frequently turns a Java abstract base class (e.g.
        // org.apache.lucene.search.Collector) into a .NET interface (ICollector) that the
        // concrete type implements, so the .NET class now derives from object. Accept when the
        // .NET type derives from object (or has no base) and implements an interface whose
        // Lucene name matches the Java base class.
        var baseIsObjectOrNone = dotNetType.BaseType is null || dotNetType.BaseType == typeof(object);
        if (baseIsObjectOrNone
            && dotNetType.GetImplementedInterfaces().Any(i => TypeMatchesFullName(i, javaBaseType, "interface")))
        {
            return true;
        }

        // Generic/non-generic split: a .NET generic type (e.g. FieldComparer<T>) often derives
        // from its own non-generic same-named twin (FieldComparer) that holds the shared static
        // surface, while the Java type derives from java.lang.Object. Accept when the Java base
        // is Object and the .NET base type is this type's own non-generic counterpart.
        if (javaBaseType.Equals("java.lang.Object", StringComparison.Ordinal)
            && dotNetType.IsGenericType
            && dotNetType.BaseType is { IsGenericType: false } baseType)
        {
            var ownName = StripGenericArity(dotNetType.Name);
            var baseName = StripGenericArity(baseType.Name);
            if (string.Equals(ownName, baseName, StringComparison.Ordinal)
                && string.Equals(dotNetType.Namespace, baseType.Namespace, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripGenericArity(string typeName)
    {
        var index = typeName.IndexOf('`');
        return index < 0 ? typeName : typeName[..index];
    }
}
