using System;

namespace Lucene.Net.Support
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

    public static class MathExtensions
    {
        /// <summary>
        /// Converts an angle measured in degrees to an approximately equivalent angle 
        /// measured in radians. The conversion from degrees to radians is generally inexact.
        /// </summary>
        /// <param name="degrees">An angle in degrees to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static double ToRadians(this double degrees)
        {
            return degrees / 180 * Math.PI;
        }

        /// <summary>
        /// Converts an angle measured in degrees to an approximately equivalent angle 
        /// measured in radians. The conversion from degrees to radians is generally inexact.
        /// </summary>
        /// <param name="degrees">An angle in degrees to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static decimal ToRadians(this decimal degrees)
        {
            return degrees / (decimal)(180 * Math.PI);
        }

        /// <summary>
        /// Converts an angle measured in degrees to an approximately equivalent angle 
        /// measured in radians. The conversion from degrees to radians is generally inexact.
        /// </summary>
        /// <param name="degrees">An angle in degrees to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static double ToRadians(this int degrees)
        {
            return ((double)degrees) / 180 * Math.PI;
        }

        /// <summary>
        /// Converts an angle measured in radians to an approximately equivalent angle 
        /// measured in degrees. The conversion from radians to degrees is generally 
        /// inexact; users should not expect Cos((90.0).ToRadians()) to exactly equal 0.0.
        /// </summary>
        /// <param name="radians">An angle in radians to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static double ToDegrees(this double radians)
        {
            return radians * 180 / Math.PI;
        }

        /// <summary>
        /// Converts an angle measured in radians to an approximately equivalent angle 
        /// measured in degrees. The conversion from radians to degrees is generally 
        /// inexact; users should not expect Cos((90.0).ToRadians()) to exactly equal 0.0.
        /// </summary>
        /// <param name="radians">An angle in radians to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static decimal ToDegrees(this decimal radians)
        {
            return radians * 180 / (decimal)Math.PI;
        }

        /// <summary>
        /// Converts an angle measured in radians to an approximately equivalent angle 
        /// measured in degrees. The conversion from radians to degrees is generally 
        /// inexact; users should not expect Cos((90.0).ToRadians()) to exactly equal 0.0.
        /// </summary>
        /// <param name="radians">An angle in radians to convert to radians</param>
        /// <returns>The value in radians</returns>
        public static double ToDegrees(this int radians)
        {
            return ((double)radians) * 180 / Math.PI;
        }
    }
}