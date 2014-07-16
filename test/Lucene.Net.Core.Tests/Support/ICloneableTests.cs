/**
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */


namespace Lucene.Net.Support
{
    using Lucene.Net.TestFramework;
    using System;

    /// <summary>
    /// Demonstrates how to use Lucene.Net's ICloneable
    /// </summary>
    public class ICloneableTests : LuceneTestCase
    {
	    
        [Test]
        public void CloneShallow()
        {
            var shallow = new Imitation() { Name = "Original" };
            var clone = shallow.CloneAndCast();

            var namesAreSame = Object.ReferenceEquals(shallow.Name, clone.Name);
            var referencesAreSame = Object.ReferenceEquals(shallow.Reference, clone.Reference);
            var message = "The clone {0} property must reference the same object in a shallow clone.";

            Ok(namesAreSame, message, "Name");
            Ok(referencesAreSame, message, "Reference");

            // When a deep clone is not supported, it should throw DeepCloneNotSupportedException.
            Throws<DeepCloneNotSupportedException>(() => {
                clone.CloneAndCast(true);
            });
        }

        [Test]
        public void DeepClone()
        {
            var shadow = new ShadowClone() { Name = "Knockoff" };
            var clone = shadow.CloneAndCast(true);

            var namesAreNotSame = !Object.ReferenceEquals(shadow.Name, clone.Name);
            var referencesAreNotSame = !Object.ReferenceEquals(shadow.Name, clone.Name);
            var message = "The clone {0} property must not reference the same object in a deep clone.";

            Ok(namesAreNotSame, message, "Name");
            Ok(referencesAreNotSame, message, "Reference");

            Throws<ShallowCloneNotSupportedException>(() => {
                clone.CloneAndCast(false);
            });
        }


        private class ShadowClone : Lucene.Net.Support.ICloneable
        {
            public ShadowClone()
            {
                this.Reference = new Reference() { Id = 2 };
            }

            public string Name { get; set; }

            public Reference Reference { get; set; }

            public object Clone(bool deepClone = false)
            {
                if (!deepClone)
                    throw new ShallowCloneNotSupportedException(this.GetType());

                var name = this.Name;
                if (name != null)
                    name = new String(name.ToCharArray());

                return new ShadowClone()
                {
                    Name = name,
                    Reference = new Reference()
                    {
                        Id = this.Reference.Id
                    }
                };
            }
        }

        private class Imitation : Lucene.Net.Support.ICloneable
        {
            public Imitation()
            {
                this.Reference = new Reference();
            }

            public string Name { get; set; }

            public Reference Reference { get; set; }

            public object Clone(bool deepClone = false)
            {
                if (deepClone)
                    throw new DeepCloneNotSupportedException(this.GetType());

                return this.MemberwiseClone(); 
            }
        }

        private class Reference
        {
            public Reference()
            {
                this.Id = 1;
            }

            public int Id { get; set; }
        }
    }
}