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

namespace Lucene.Net.ApiCheck.Models;

public record MavenCoordinates(
    string GroupId,
    string ArtifactId,
    string Version)
{
    public static MavenCoordinates Parse(string mavenCoordinates)
    {
        string[] parts = mavenCoordinates.Split(':');
        if (parts.Length != 3)
        {
            throw new ArgumentException("Invalid Maven coordinates. Expected format: groupId:artifactId:version");
        }
        return new MavenCoordinates(parts[0], parts[1], parts[2]);
    }

    public override string ToString() => $"{GroupId}:{ArtifactId}:{Version}";
}
