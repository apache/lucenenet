using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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

    /// <summary> Base class for Attributes that can be added to a
    /// <see cref="Lucene.Net.Util.AttributeSource" />.
    /// <para/>
    /// Attributes are used to add data in a dynamic, yet type-safe way to a source
    /// of usually streamed objects, e. g. a <see cref="Lucene.Net.Analysis.TokenStream" />.
    /// </summary>
    public abstract class Attribute : IAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary> Clears the values in this <see cref="Attribute"/> and resets it to its
        /// default value. If this implementation implements more than one <see cref="Attribute"/> interface
        /// it clears all.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// This is equivalent to the anonymous class in the Java version of ReflectAsString
        /// </summary>
        private class StringBuilderAttributeReflector : IAttributeReflector
        {
            private readonly StringBuilder buffer;
            private readonly bool prependAttClass;

            public StringBuilderAttributeReflector(StringBuilder buffer, bool prependAttClass)
            {
                this.buffer = buffer;
                this.prependAttClass = prependAttClass;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reflect<T>(string key, object value)
                where T : IAttribute
            {
                Reflect(typeof(T), key, value);
            }

            public void Reflect(Type type, string key, object value)
            {
                if (buffer.Length > 0)
                {
                    buffer.Append(',');
                }
                if (prependAttClass)
                {
                    buffer.Append(type.Name).Append('#');
                }
                buffer.Append(key).Append('=');
                if (value is null)
                    buffer.Append("null");
                else
                    buffer.Append(value);
            }
        }

        /// <summary>
        /// This method returns the current attribute values as a string in the following format
        /// by calling the <see cref="ReflectWith(IAttributeReflector)"/> method:
        /// <list type="bullet">
        ///     <item><term>if <paramref name="prependAttClass"/>=true:</term> <description> <c>"AttributeClass.Key=value,AttributeClass.Key=value"</c> </description></item>
        ///     <item><term>if <paramref name="prependAttClass"/>=false:</term> <description> <c>"key=value,key=value"</c> </description></item>
        /// </list>
        /// </summary>
        /// <seealso cref="ReflectWith(IAttributeReflector)"/>
        public string ReflectAsString(bool prependAttClass)
        {
            StringBuilder buffer = new StringBuilder();

            ReflectWith(new StringBuilderAttributeReflector(buffer, prependAttClass));

            return buffer.ToString();
        }

        /// <summary>
        /// This method is for introspection of attributes, it should simply
        /// add the key/values this attribute holds to the given <see cref="IAttributeReflector"/>.
        /// 
        /// <para/>The default implementation calls <see cref="IAttributeReflector.Reflect(Type, string, object)"/> for all
        /// non-static fields from the implementing class, using the field name as key
        /// and the field value as value. The <see cref="IAttribute"/> class is also determined by Reflection.
        /// Please note that the default implementation can only handle single-Attribute
        /// implementations.
        /// 
        /// <para/>Custom implementations look like this (e.g. for a combined attribute implementation):
        /// <code>
        ///     public void ReflectWith(IAttributeReflector reflector) 
        ///     {
        ///         reflector.Reflect(typeof(ICharTermAttribute), "term", GetTerm());
        ///         reflector.Reflect(typeof(IPositionIncrementAttribute), "positionIncrement", GetPositionIncrement());
        ///     }
        /// </code>
        /// 
        /// <para/>If you implement this method, make sure that for each invocation, the same set of <see cref="IAttribute"/>
        /// interfaces and keys are passed to <see cref="IAttributeReflector.Reflect(Type, string, object)"/> in the same order, but possibly
        /// different values. So don't automatically exclude e.g. <c>null</c> properties!
        /// </summary>
        /// <seealso cref="ReflectAsString(bool)"/>
        public virtual void ReflectWith(IAttributeReflector reflector) // LUCENENET NOTE: This method was abstract in Lucene
        {
            Type clazz = this.GetType();
            LinkedList<WeakReference<Type>> interfaces = AttributeSource.GetAttributeInterfaces(clazz);

            if (interfaces.Count != 1)
            {
                throw UnsupportedOperationException.Create(clazz.Name + " implements more than one Attribute interface, the default ReflectWith() implementation cannot handle this.");
            }

            interfaces.First.Value.TryGetTarget(out Type interf);

            //problem: the interfaces list has weak references that could have expired already

            FieldInfo[] fields = clazz.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            try
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];
                    if (f.IsStatic) continue;
                    reflector.Reflect(interf, f.Name, f.GetValue(this));
                }
            }
            catch (Exception e) when (e.IsIllegalAccessException())
            {
                // this should never happen, because we're just accessing fields
                // from 'this'
                throw RuntimeException.Create(e);
            }
        }

        /// <summary> The default implementation of this method accesses all declared
        /// fields of this object and prints the values in the following syntax:
        ///
        /// <code>
        /// public String ToString() 
        /// {
        ///     return "start=" + startOffset + ",end=" + endOffset;
        /// }
        /// </code>
        ///
        /// This method may be overridden by subclasses.
        /// </summary>
        public override string ToString() // LUCENENET NOTE: This method didn't exist in Lucene
        {
            StringBuilder buffer = new StringBuilder();
            Type clazz = this.GetType();
            FieldInfo[] fields = clazz.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.IsStatic)
                    continue;
                //f.setAccessible(true);   // {{Aroush-2.9}} java.lang.reflect.AccessibleObject.setAccessible
                object value = f.GetValue(this);
                if (buffer.Length > 0)
                {
                    buffer.Append(',');
                }
                if (value is null)
                {
                    buffer.Append(f.Name + "=null");
                }
                else
                {
                    buffer.Append(f.Name + "=" + value);
                }
            }

            return buffer.ToString();
        }

        /// <summary> Copies the values from this <see cref="Attribute"/> into the passed-in
        /// <paramref name="target"/> attribute. The <paramref name="target"/> implementation must support all the
        /// <see cref="IAttribute"/>s this implementation supports.
        /// </summary>
        public abstract void CopyTo(IAttribute target); // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute

        /// <summary> Shallow clone. Subclasses must override this if they
        /// need to clone any members deeply,
        /// </summary>
        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}