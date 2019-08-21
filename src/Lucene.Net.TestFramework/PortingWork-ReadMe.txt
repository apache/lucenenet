# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.


IMPORTANT - PLEASE READ IF YOU ARE PORTING test-framework CODE FROM JAVA

If porting new code to Lucene.Net.TestFramework, make sure you NEVER call 
System.Diagnostics.Debug.Assert. The design of the test framework expects 
asserts to be included in the release, so we have added Lucene.Net.Diagnostics.Debug.Assert 
methods to ensure this happens.

This wasn't done to intentionally be confusing, it was done to keep the syntax similar
to Java and so if an assert is added later you don't really have to think about what
method to call. The only exception to this is when adding a new class file with asserts in it.
Make sure you place this import at the top of the file, comment included:

using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!

And then in the code, call Debug.Assert just like you normally would.


By the same token (pardon the pun) we are using our own Lucene.Net.Diagnostics.AssertionException
class instead of throwing the test framework's (i.e. NUnit or xUnit) exception. This is to normalize the behavior so
it doesn't change regardless of the test framework. Never throw an exception from the test framework
itself because it might not have compatible constructors or might be given special behaviors by the
test framework that are incompatible with Lucene.Net.TestFramework.

If you need to throw an "AssertionError", put this declaration at the top of the class:

using AssertionError = Lucene.Net.Diagnostics.AssertionException;


Do note that this exception is thrown and caught within Lucene.Net.TestFramework by some test mocks.