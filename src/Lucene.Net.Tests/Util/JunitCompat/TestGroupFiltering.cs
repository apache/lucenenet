namespace Lucene.Net.Util.JunitCompat
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

	public class TestGroupFiltering : LuceneTestCase
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = false) public class Foo extends System.Attribute
	  public class Foo : System.Attribute
	  {
		  private readonly TestGroupFiltering OuterInstance;

		  public Foo(TestGroupFiltering outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = false) public class Bar extends System.Attribute
	  public class Bar : System.Attribute
	  {
		  private readonly TestGroupFiltering OuterInstance;

		  public Bar(TestGroupFiltering outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Documented @Inherited @Retention(RetentionPolicy.RUNTIME) @TestGroup(enabled = false) public class Jira extends System.Attribute
	 /* LUCENE TODO: Not used
      public class Jira : System.Attribute
	  {
		  private readonly TestGroupFiltering OuterInstance;

		  public Jira(TestGroupFiltering outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		string bug();
	  }*/

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Foo public void testFoo()
	  public void TestFoo()
	  {
	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Foo @Bar public void testFooBar()
	  public void TestFooBar()
	  {
	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Bar public void testBar()
	  public void TestBar()
	  {
	  }
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Jira(bug = "JIRA bug reference") public void testJira()
	  public void TestJira()
	  {
	  }
	}

}