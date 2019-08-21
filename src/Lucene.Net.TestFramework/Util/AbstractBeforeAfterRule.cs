#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete
using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{

	/*using After = org.junit.After;
	using AfterClass = org.junit.AfterClass;
	using RuleChain = org.junit.rules.RuleChain;
	using TestRule = org.junit.rules.TestRule;
	using Description = org.junit.runner.Description;
	using MultipleFailureException = org.junit.runners.model.MultipleFailureException;
	using Statement = org.junit.runners.model.Statement;*/

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
	/// A <seealso cref="TestRule"/> that guarantees the execution of <seealso cref="#after"/> even
	/// if an exception has been thrown from delegate <seealso cref="Statement"/>. this is much
	/// like <seealso cref="AfterClass"/> or <seealso cref="After"/> annotations but can be used with
	/// <seealso cref="RuleChain"/> to guarantee the order of execution.
	/// </summary>
	internal abstract class AbstractBeforeAfterRule : TestRule
	{
	  public override Statement Apply(Statement s, Description d)
	  {
		return new StatementAnonymousInnerClassHelper(this, s);
	  }

	  private class StatementAnonymousInnerClassHelper : Statement
	  {
		  private readonly AbstractBeforeAfterRule OuterInstance;

		  private Statement s;

		  public StatementAnonymousInnerClassHelper(AbstractBeforeAfterRule outerInstance, Statement s)
		  {
			  this.OuterInstance = outerInstance;
			  this.s = s;
		  }

		  public override void Evaluate()
		  {
			List<Exception> errors = new List<Exception>();

			try
			{
			  OuterInstance.Before();
			  s.evaluate();
			}
			catch (Exception t)
			{
			  errors.Add(t);
			}

			try
			{
                OuterInstance.After();
			}
			catch (Exception t)
			{
			  errors.Add(t);
			}

			MultipleFailureException.assertEmpty(errors);
		  }
	  }

	  protected internal virtual void Before()
	  {
	  }
	  protected internal virtual void After()
	  {
	  }
	}

}
#endif