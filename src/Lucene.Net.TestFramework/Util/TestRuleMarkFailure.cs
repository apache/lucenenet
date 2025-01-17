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

// LUCENENET NOTE: This class was not ported because it is not required (and problematic) in .NET.
//
// NUnit tracks test result in NUnit.Framework.Internal.TestExectionContext.CurrentContext.TestResult.ResultState
// and NUnit.Framework.TestContext.CurrentContext.Result.Outcome. It is much more comprehensive than checking for exceptions because
// in NUnit failures aren't always exceptions and success may throw an exception. Furthermore, these are context sensitive so
// so it is more understandable to have them in a global context than to try to track the state of both the test and the class result
// elsewhere. NUnit also does it in a threadsafe way, so skipping this is one thing less to change if we ever try to run tests in
// parallel in the future. When inside of a test, it is possible to check the class-level by doing a recursive call on
// NUnit.Framework.Internal.TestExectionContext.CurrentContext.CurrentTest.Parent and checking to see whether it is a class using
// our TestExtensions.IsTestClass() extension method. Once the class abstraction has been located, simply read the result
// in NUnit.Framework.Internal.TestExectionContext.CurrentContext.TestResult.ResultState. Users have access to this state
// already, so there is no need for us to add custom properties to LuceneTestCase to try to read it because that muddies the
// fact that this state is context sensitive.
