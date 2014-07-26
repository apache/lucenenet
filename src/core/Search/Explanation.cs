using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search
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
    /// Expert: Describes the score computation for document and query. </summary>
    public class Explanation
    {
        private float Value_Renamed; // the value of this node
        private string Description_Renamed; // what it represents
        private List<Explanation> Details_Renamed; // sub-explanations

        public Explanation()
        {
        }

        public Explanation(float value, string description)
        {
            this.Value_Renamed = value;
            this.Description_Renamed = description;
        }

        /// <summary>
        /// Indicates whether or not this Explanation models a good match.
        ///
        /// <p>
        /// By default, an Explanation represents a "match" if the value is positive.
        /// </p> </summary>
        /// <seealso cref= #getValue </seealso>
        public virtual bool IsMatch
        {
            get
            {
                return (0.0f < Value);
            }
        }

        /// <summary>
        /// The value assigned to this explanation node. </summary>
        public virtual float Value
        {
            get
            {
                return Value_Renamed;
            }
            set
            {
                this.Value_Renamed = value;
            }
        }

        /// <summary>
        /// A description of this explanation node. </summary>
        public virtual string Description
        {
            get
            {
                return Description_Renamed;
            }
            set
            {
                this.Description_Renamed = value;
            }
        }

        /// <summary>
        /// A short one line summary which should contain all high level
        /// information about this Explanation, without the "Details"
        /// </summary>
        protected internal virtual string Summary
        {
            get
            {
                return Value + " = " + Description;
            }
        }

        /// <summary>
        /// The sub-nodes of this explanation node. </summary>
        public virtual Explanation[] Details
        {
            get
            {
                if (Details_Renamed == null)
                {
                    return null;
                }
                return Details_Renamed.ToArray();
            }
        }

        /// <summary>
        /// Adds a sub-node to this explanation node. </summary>
        public virtual void AddDetail(Explanation detail)
        {
            if (Details_Renamed == null)
            {
                Details_Renamed = new List<Explanation>();
            }
            Details_Renamed.Add(detail);
        }

        /// <summary>
        /// Render an explanation as text. </summary>
        public override string ToString()
        {
            return ToString(0);
        }

        protected internal virtual string ToString(int depth)
        {
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                buffer.Append("  ");
            }
            buffer.Append(Summary);
            buffer.Append("\n");

            Explanation[] details = Details;
            if (details != null)
            {
                for (int i = 0; i < details.Length; i++)
                {
                    buffer.Append(details[i].ToString(depth + 1));
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Render an explanation as HTML. </summary>
        public virtual string ToHtml()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("<ul>\n");

            buffer.Append("<li>");
            buffer.Append(Summary);
            buffer.Append("<br />\n");

            Explanation[] details = Details;
            if (details != null)
            {
                for (int i = 0; i < details.Length; i++)
                {
                    buffer.Append(details[i].ToHtml());
                }
            }

            buffer.Append("</li>\n");
            buffer.Append("</ul>\n");

            return buffer.ToString();
        }
    }
}