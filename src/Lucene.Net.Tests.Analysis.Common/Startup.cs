// Lucene version compatibility level 4.8.1
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

using Lucene.Net.Util;

public class Startup : LuceneTestFrameworkInitializer
{
    protected override void TestFrameworkSetUp()
    {
#if FEATURE_ENCODINGPROVIDERS
        // LUCENENET NOTE: Hunspell manual tests require additional encoding types. End users may
        // require it to be added as well when using Hunspell, but there is no reason to load
        // the code pages by default in Lucene.Net.Analysis.Common. It should be added by consumers
        // or Hunspell that require it.
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
#endif
    }
}