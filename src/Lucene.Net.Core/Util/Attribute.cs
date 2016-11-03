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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Util
{
    /// <summary> Base class for Attributes that can be added to a
    /// <see cref="Lucene.Net.Util.AttributeSource" />.
    /// <p/>
    /// Attributes are used to add data in a dynamic, yet type-safe way to a source
    /// of usually streamed objects, e. g. a <see cref="Lucene.Net.Analysis.TokenStream" />.
    /// </summary>
    public abstract class Attribute : IAttribute
    {
        /// <summary> Clears the values in this Attribute and resets it to its
        /// default value. If this implementation implements more than one Attribute interface
        /// it clears all.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// This is equivalent to the anonymous class in the java version of ReflectWithString
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
                buffer.Append(key).Append('=').Append(object.ReferenceEquals(value, null) ? (object)"null" : value);
            }
        }

        public virtual string ReflectAsString(bool prependAttClass)
        {
            StringBuilder buffer = new StringBuilder();

            ReflectWith(new StringBuilderAttributeReflector(buffer, prependAttClass));

            return buffer.ToString();
        }

        public virtual void ReflectWith(IAttributeReflector reflector)
        {
            Type clazz = this.GetType();
            LinkedList<WeakReference> interfaces = AttributeSource.GetAttributeInterfaces(clazz);

            if (interfaces.Count != 1)
            {
                throw new NotSupportedException(clazz.Name + " implements more than one Attribute interface, the default ReflectWith() implementation cannot handle this.");
            }

            Type interf = (System.Type)interfaces.First().Target;

            /*object target = interfaces.First.Value;

            if (target == null)
                return;

            Type interf = target.GetType();// as Type;*/

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
            catch (UnauthorizedAccessException uae)
            {
                throw new Exception(uae.Message, uae);
            }
        }

        /// <summary> The default implementation of this method accesses all declared
        /// fields of this object and prints the values in the following syntax:
        ///
        /// <code>
        /// public String toString() {
        /// return "start=" + startOffset + ",end=" + endOffset;
        /// }
        /// </code>
        ///
        /// This method may be overridden by subclasses.
        /// </summary>
        public override System.String ToString()
        {
            System.Text.StringBuilder buffer = new System.Text.StringBuilder();
            System.Type clazz = this.GetType();
            System.Reflection.FieldInfo[] fields = clazz.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                System.Reflection.FieldInfo f = fields[i];
                if (f.IsStatic)
                    continue;
                //f.setAccessible(true);   // {{Aroush-2.9}} java.lang.reflect.AccessibleObject.setAccessible
                System.Object value_Renamed = f.GetValue(this);
                if (buffer.Length > 0)
                {
                    buffer.Append(',');
                }
                if (value_Renamed == null)
                {
                    buffer.Append(f.Name + "=null");
                }
                else
                {
                    buffer.Append(f.Name + "=" + value_Renamed);
                }
            }

            return buffer.ToString();
        }

        /// <summary> Copies the values from this Attribute into the passed-in
        /// target attribute. The target implementation must support all the
        /// Attributes this implementation supports.
        /// </summary>
        public abstract void CopyTo(Attribute target);

        /// <summary> Shallow clone. Subclasses must override this if they
        /// need to clone any members deeply,
        /// </summary>
        public virtual System.Object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}