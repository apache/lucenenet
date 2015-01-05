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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Lucene.Net.Queries.Function;
using Lucene.Net.Support;

namespace Lucene.Net.Expressions.JS
{
	/// <summary>An expression compiler for javascript expressions.</summary>
	/// <remarks>
	/// An expression compiler for javascript expressions.
	/// <p>
	/// Example:
	/// <pre class="prettyprint">
	/// Expression foo = JavascriptCompiler.compile("((0.3*popularity)/10.0)+(0.7*score)");
	/// </pre>
	/// <p>
	/// See the
	/// <see cref="Lucene.Net.Expressions.JS">package documentation</see>
	/// for
	/// the supported syntax and default functions.
	/// <p>
	/// You can compile with an alternate set of functions via
	/// <see cref="Compile(string, System.Collections.Generic.IDictionary{K, V}, Sharpen.ClassLoader)
	/// 	">Compile(string, System.Collections.Generic.IDictionary&lt;K, V&gt;, Sharpen.ClassLoader)
	/// 	</see>
	/// .
	/// For example:
	/// <pre class="prettyprint">
	/// Map&lt;String,Method&gt; functions = new HashMap&lt;String,Method&gt;();
	/// // add all the default functions
	/// functions.putAll(JavascriptCompiler.DEFAULT_FUNCTIONS);
	/// // add cbrt()
	/// functions.put("cbrt", Math.class.getMethod("cbrt", double.class));
	/// // call compile with customized function map
	/// Expression foo = JavascriptCompiler.compile("cbrt(score)+ln(popularity)",
	/// functions,
	/// getClass().getClassLoader());
	/// </pre>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class JavascriptCompiler
	{
		internal sealed class Loader : ClassLoader
		{
			protected internal Loader(ClassLoader parent) : base(parent)
			{
			}

			public Type Define(string className, byte[] bytecode)
			{
				return DefineClass(className, bytecode, 0, bytecode.Length).AsSubclass<Expression>();
			}
		}

		private const int CLASSFILE_VERSION = Opcodes.V1_7;

		private static readonly string COMPILED_EXPRESSION_CLASS = typeof(JavascriptCompiler).FullName + "$CompiledExpression";

		private static readonly string COMPILED_EXPRESSION_INTERNAL = COMPILED_EXPRESSION_CLASS.Replace('.', '/');

		private static readonly Type EXPRESSION_TYPE = Type.GetType(typeof(Expression).FullName);

		private static readonly Type FUNCTION_VALUES_TYPE = Type.GetType(typeof(FunctionValues).FullName);

		private static readonly MethodInfo EXPRESSION_CTOR = GetMethod("void <init>(String, String[])"
			);

		private static readonly MethodInfo EVALUATE_METHOD = GetMethod("double evaluate(int, "
			 + typeof(FunctionValues).FullName + "[])");

		private static readonly MethodInfo DOUBLE_VAL_METHOD = GetMethod("double doubleVal(int)"
			);

		// We use the same class name for all generated classes as they all have their own class loader.
		// The source code is displayed as "source file name" in stack trace.
		// to work around import clash:
		private static MethodInfo GetMethod(string method)
		{
			return Method.GetMethod(method);
		}

		private const int MAX_SOURCE_LENGTH = 16384;

		private readonly string sourceText;

		private readonly IDictionary<string, int> externalsMap = new LinkedHashMap<string
			, int>();

		private readonly ClassWriter classWriter = new System.Linq.Expressions. ClassWriter(ClassWriter.COMPUTE_FRAMES
			 | ClassWriter.COMPUTE_MAXS);

		private GeneratorAdapter gen;

		private readonly IDictionary<string, MethodInfo> functions;

		// This maximum length is theoretically 65535 bytes, but as its CESU-8 encoded we dont know how large it is in bytes, so be safe
		// rcmuir: "If your ranking function is that large you need to check yourself into a mental institution!"
		/// <summary>Compiles the given expression.</summary>
		/// <remarks>Compiles the given expression.</remarks>
		/// <param name="sourceText">The expression to compile</param>
		/// <returns>A new compiled expression</returns>
		/// <exception cref="Sharpen.ParseException">on failure to compile</exception>
		public static Expression Compile(string sourceText)
		{
			return new JavascriptCompiler(sourceText).CompileExpression(typeof(JavascriptCompiler
				).GetClassLoader());
		}

		/// <summary>Compiles the given expression with the supplied custom functions.</summary>
		/// <remarks>
		/// Compiles the given expression with the supplied custom functions.
		/// <p>
		/// Functions must be
		/// <code>public static</code>
		/// , return
		/// <code>double</code>
		/// and
		/// can take from zero to 256
		/// <code>double</code>
		/// parameters.
		/// </remarks>
		/// <param name="sourceText">The expression to compile</param>
		/// <param name="functions">map of String names to functions</param>
		/// <param name="parent">
		/// a
		/// <code>ClassLoader</code>
		/// that should be used as the parent of the loaded class.
		/// It must contain all classes referred to by the given
		/// <code>functions</code>
		/// .
		/// </param>
		/// <returns>A new compiled expression</returns>
		/// <exception cref="Sharpen.ParseException">on failure to compile</exception>
		public static Expression Compile(string sourceText, IDictionary<string, MethodInfo
			> functions, ClassLoader parent)
		{
			if (parent == null)
			{
				throw new ArgumentNullException("A parent ClassLoader must be given.");
			}
			foreach (MethodInfo m in functions.Values)
			{
				CheckFunction(m, parent);
			}
			return new JavascriptCompiler(sourceText, functions).CompileExpression(parent);
		}

		/// <summary>This method is unused, it is just here to make sure that the function signatures don't change.
		/// 	</summary>
		/// <remarks>
		/// This method is unused, it is just here to make sure that the function signatures don't change.
		/// If this method fails to compile, you also have to change the byte code generator to correctly
		/// use the FunctionValues class.
		/// </remarks>
		private static void UnusedTestCompile()
		{
			FunctionValues f = null;
			double ret = f.DoubleVal(2);
		}

		/// <summary>Constructs a compiler for expressions.</summary>
		/// <remarks>Constructs a compiler for expressions.</remarks>
		/// <param name="sourceText">The expression to compile</param>
		private JavascriptCompiler(string sourceText) : this(sourceText, DEFAULT_FUNCTIONS
			)
		{
		}

		/// <summary>Constructs a compiler for expressions with specific set of functions</summary>
		/// <param name="sourceText">The expression to compile</param>
		private JavascriptCompiler(string sourceText, IDictionary<string, MethodInfo> functions
			)
		{
			if (sourceText == null)
			{
				throw new ArgumentNullException();
			}
			this.sourceText = sourceText;
			this.functions = functions;
		}

		/// <summary>Compiles the given expression with the specified parent classloader</summary>
		/// <returns>A new compiled expression</returns>
		
		private Expression CompileExpression(ClassLoader parent)
		{
			try
			{
                

				ITree antlrTree = GetAntlrComputedExpressionTree();
				BeginCompile();
				RecursiveCompile(antlrTree, Type.DOUBLE_TYPE);
				EndCompile();
				Type evaluatorClass = new Loader(parent).Define(COMPILED_EXPRESSION_CLASS, classWriter.ToByteArray());
				Constructor<Expression> constructor = evaluatorClass.GetConstructor(typeof(string
					), typeof(string[]));
				return constructor.NewInstance(sourceText, Sharpen.Collections.ToArray(externalsMap
					.Keys, new string[externalsMap.Count]));
			}
			catch (InstantiationException exception)
			{
				throw new InvalidOperationException("An internal error occurred attempting to compile the expression ("
					 + sourceText + ").", exception);
			}
			catch (MemberAccessException exception)
			{
				throw new InvalidOperationException("An internal error occurred attempting to compile the expression ("
					 + sourceText + ").", exception);
			}
			catch (NoSuchMethodException exception)
			{
				throw new InvalidOperationException("An internal error occurred attempting to compile the expression ("
					 + sourceText + ").", exception);
			}
			catch (TargetInvocationException exception)
			{
				throw new InvalidOperationException("An internal error occurred attempting to compile the expression ("
					 + sourceText + ").", exception);
			}
		}

		private void BeginCompile()
		{
			classWriter.Visit(CLASSFILE_VERSION, Opcodes.ACC_PUBLIC | Opcodes.ACC_SUPER | Opcodes
				.ACC_FINAL | Opcodes.ACC_SYNTHETIC, COMPILED_EXPRESSION_INTERNAL, null, EXPRESSION_TYPE
				.GetInternalName(), null);
			string clippedSourceText = (sourceText.Length <= MAX_SOURCE_LENGTH) ? sourceText : 
				(Sharpen.Runtime.Substring(sourceText, 0, MAX_SOURCE_LENGTH - 3) + "...");
			classWriter.VisitSource(clippedSourceText, null);
			GeneratorAdapter constructor = new GeneratorAdapter(Opcodes.ACC_PUBLIC | Opcodes.
				ACC_SYNTHETIC, EXPRESSION_CTOR, null, null, classWriter);
			constructor.LoadThis();
			constructor.LoadArgs();
			constructor.InvokeConstructor(EXPRESSION_TYPE, EXPRESSION_CTOR);
			constructor.ReturnValue();
			constructor.EndMethod();
			gen = new GeneratorAdapter(Opcodes.ACC_PUBLIC | Opcodes.ACC_SYNTHETIC, EVALUATE_METHOD
				, null, null, classWriter);
		}

		private void RecursiveCompile(ITree current, Type expected)
		{
			int type = current.Type;
			string text = current.Text;
			switch (type)
			{
				case JavascriptParser.AT_CALL:
				{
					ITree identifier = current.GetChild(0);
					string call = identifier.Text;
					int arguments = current.ChildCount - 1;
					MethodInfo method = functions[call];
					if (method == null)
					{
						throw new ArgumentException("Unrecognized method call (" + call + ").");
					}
					int arity = method.GetParameters().Length;
					if (arguments != arity)
					{
						throw new ArgumentException("Expected (" + arity + ") arguments for method call ("
							 + call + "), but found (" + arguments + ").");
					}
					for (int argument = 1; argument <= arguments; ++argument)
					{
						RecursiveCompile(current.GetChild(argument), Type.DOUBLE_TYPE);
					}
					gen.InvokeStatic(Type.GetType(method.DeclaringType), Method.GetMethod(method));
					gen.Cast(Type.DOUBLE_TYPE, expected);
					break;
				}

				case JavascriptParser.NAMESPACE_ID:
				{
					int index;
					if (externalsMap.ContainsKey(text))
					{
						index = externalsMap.Get(text);
					}
					else
					{
						index = externalsMap.Count;
						externalsMap.Put(text, index);
					}
					gen.LoadArg(1);
					gen.Push(index);
					gen.ArrayLoad(FUNCTION_VALUES_TYPE);
					gen.LoadArg(0);
					gen.InvokeVirtual(FUNCTION_VALUES_TYPE, DOUBLE_VAL_METHOD);
					gen.Cast(Type.DOUBLE_TYPE, expected);
					break;
				}

				case JavascriptParser.HEX:
				{
					PushLong(expected, long.Parse(Sharpen.Runtime.Substring(text, 2), 16));
					break;
				}

				case JavascriptParser.OCTAL:
				{
					PushLong(expected, long.Parse(Sharpen.Runtime.Substring(text, 1), 8));
					break;
				}

				case JavascriptParser.DECIMAL:
				{
					gen.Push(double.ParseDouble(text));
					gen.Cast(Type.DOUBLE_TYPE, expected);
					break;
				}

				case JavascriptParser.AT_NEGATE:
				{
					RecursiveCompile(current.GetChild(0), Type.DOUBLE_TYPE);
					gen.VisitInsn(Opcodes.DNEG);
					gen.Cast(Type.DOUBLE_TYPE, expected);
					break;
				}

				case JavascriptParser.AT_ADD:
				{
					PushArith(Opcodes.DADD, current, expected);
					break;
				}

				case JavascriptParser.AT_SUBTRACT:
				{
					PushArith(Opcodes.DSUB, current, expected);
					break;
				}

				case JavascriptParser.AT_MULTIPLY:
				{
					PushArith(Opcodes.DMUL, current, expected);
					break;
				}

				case JavascriptParser.AT_DIVIDE:
				{
					PushArith(Opcodes.DDIV, current, expected);
					break;
				}

				case JavascriptParser.AT_MODULO:
				{
					PushArith(Opcodes.DREM, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_SHL:
				{
					PushShift(Opcodes.LSHL, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_SHR:
				{
					PushShift(Opcodes.LSHR, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_SHU:
				{
					PushShift(Opcodes.LUSHR, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_AND:
				{
					PushBitwise(Opcodes.LAND, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_OR:
				{
					PushBitwise(Opcodes.LOR, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_XOR:
				{
					PushBitwise(Opcodes.LXOR, current, expected);
					break;
				}

				case JavascriptParser.AT_BIT_NOT:
				{
					RecursiveCompile(current.GetChild(0), Type.LONG_TYPE);
					gen.Push(-1L);
					gen.VisitInsn(Opcodes.LXOR);
					gen.Cast(Type.LONG_TYPE, expected);
					break;
				}

				case JavascriptParser.AT_COMP_EQ:
				{
					PushCond(GeneratorAdapter.EQ, current, expected);
					break;
				}

				case JavascriptParser.AT_COMP_NEQ:
				{
					PushCond(GeneratorAdapter.NE, current, expected);
					break;
				}

				case JavascriptParser.AT_COMP_LT:
				{
					PushCond(GeneratorAdapter.LT, current, expected);
					break;
				}

				case JavascriptParser.AT_COMP_GT:
				{
					PushCond(GeneratorAdapter.GT, current, expected);
					break;
				}

				case JavascriptParser.AT_COMP_LTE:
				{
					PushCond(GeneratorAdapter.LE, current, expected);
					break;
				}

				case JavascriptParser.AT_COMP_GTE:
				{
					PushCond(GeneratorAdapter.GE, current, expected);
					break;
				}

				case JavascriptParser.AT_BOOL_NOT:
				{
					Label labelNotTrue = new Label();
					Label labelNotReturn = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					gen.VisitJumpInsn(Opcodes.IFEQ, labelNotTrue);
					PushBoolean(expected, false);
					gen.GoTo(labelNotReturn);
					gen.VisitLabel(labelNotTrue);
					PushBoolean(expected, true);
					gen.VisitLabel(labelNotReturn);
					break;
				}

				case JavascriptParser.AT_BOOL_AND:
				{
					Label andFalse = new Label();
					Label andEnd = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					gen.VisitJumpInsn(Opcodes.IFEQ, andFalse);
					RecursiveCompile(current.GetChild(1), Type.INT_TYPE);
					gen.VisitJumpInsn(Opcodes.IFEQ, andFalse);
					PushBoolean(expected, true);
					gen.GoTo(andEnd);
					gen.VisitLabel(andFalse);
					PushBoolean(expected, false);
					gen.VisitLabel(andEnd);
					break;
				}

				case JavascriptParser.AT_BOOL_OR:
				{
					Label orTrue = new Label();
					Label orEnd = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					gen.VisitJumpInsn(Opcodes.IFNE, orTrue);
					RecursiveCompile(current.GetChild(1), Type.INT_TYPE);
					gen.VisitJumpInsn(Opcodes.IFNE, orTrue);
					PushBoolean(expected, false);
					gen.GoTo(orEnd);
					gen.VisitLabel(orTrue);
					PushBoolean(expected, true);
					gen.VisitLabel(orEnd);
					break;
				}

				case JavascriptParser.AT_COND_QUE:
				{
					Label condFalse = new Label();
					Label condEnd = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					gen.VisitJumpInsn(Opcodes.IFEQ, condFalse);
					RecursiveCompile(current.GetChild(1), expected);
					gen.GoTo(condEnd);
					gen.VisitLabel(condFalse);
					RecursiveCompile(current.GetChild(2), expected);
					gen.VisitLabel(condEnd);
					break;
				}

				default:
				{
					throw new InvalidOperationException("Unknown operation specified: (" + current.GetText
						() + ").");
				}
			}
		}

		private void PushArith(int op, ITree current, Type expected
			)
		{
			PushBinaryOp(op, current, expected, Type.DOUBLE_TYPE, Type.DOUBLE_TYPE, Type
				.DOUBLE_TYPE);
		}

		private void PushShift(int op,ITree current, Type expected
			)
		{
			PushBinaryOp(op, current, expected, Type.LONG_TYPE, Type.INT_TYPE, Type.LONG_TYPE
				);
		}

		private void PushBitwise(int op, ITree current, Type
			 expected)
		{
			PushBinaryOp(op, current, expected, Type.LONG_TYPE, Type.LONG_TYPE, Type.LONG_TYPE
				);
		}

		private void PushBinaryOp(int op, ITree current, Type expected, Type arg1, Type arg2, Type returnType)
		{
			RecursiveCompile(current.GetChild(0), arg1);
			RecursiveCompile(current.GetChild(1), arg2);
			gen.VisitInsn(op);
			gen.Cast(returnType, expected);
		}

		private void PushCond(int @operator, ITree current, Type expected)
		{
			Label labelTrue = new Label();
			Label labelReturn = new Label();
			RecursiveCompile(current.GetChild(0), Type.DOUBLE_TYPE);
			RecursiveCompile(current.GetChild(1), Type.DOUBLE_TYPE);
			gen.IfCmp(Type.DOUBLE_TYPE, @operator, labelTrue);
			PushBoolean(expected, false);
			gen.GoTo(labelReturn);
			gen.VisitLabel(labelTrue);
			PushBoolean(expected, true);
			gen.VisitLabel(labelReturn);
		}

		private void PushBoolean(Type expected, bool truth)
		{
			switch (expected.GetSort())
			{
				case Type.INT:
				{
					gen.Push(truth);
					break;
				}

				case Type.LONG:
				{
					gen.Push(truth ? 1L : 0L);
					break;
				}

				case Type.DOUBLE:
				{
					gen.Push(truth ? 1. : 0.);
					break;
				}

				default:
				{
					throw new InvalidOperationException("Invalid expected type: " + expected);
				}
			}
		}

		private void PushLong(Type expected, long i)
		{
			switch (expected.GetSort())
			{
				case Type.INT:
				{
					gen.Push((int)i);
					break;
				}

				case Type.LONG:
				{
					gen.Push(i);
					break;
				}

				case Type.DOUBLE:
				{
					gen.Push((double)i);
					break;
				}

				default:
				{
					throw new InvalidOperationException("Invalid expected type: " + expected);
				}
			}
		}

		private void EndCompile()
		{
			gen.ReturnValue();
			gen.EndMethod();
			classWriter.VisitEnd();
		}

		
		private ITree GetAntlrComputedExpressionTree()
		{
			ICharStream input = new ANTLRStringStream(sourceText);
			JavascriptLexer lexer = new JavascriptLexer(input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			JavascriptParser parser = new JavascriptParser(tokens);
			try
			{
				return parser.Expression().tree;
			}
			catch (RecognitionException re)
			{
				throw new ArgumentException(re.Message,re);
			}
			catch (SystemException exception)
			{
                //TODO: Uncomment after implementing ParseException in QueryParsers
                //if (exception.InnerException is ParseException)
                //{
                //    throw (ParseException)exception.InnerException;
                //}
				throw;
			}
		}

		/// <summary>The default set of functions available to expressions.</summary>
		/// <remarks>
		/// The default set of functions available to expressions.
		/// <p>
		/// See the
		/// <see cref="Lucene.Net.Expressions.JS">package documentation</see>
		/// for a list.
		/// </remarks>
		public static readonly IDictionary<string, MethodInfo> DEFAULT_FUNCTIONS;

		static JavascriptCompiler()
		{
			IDictionary<string, MethodInfo> map = new Dictionary<string, MethodInfo>();
			try
			{
				Properties props = new Properties();
				props.Load(@in);
				foreach (string call in props.StringPropertyNames())
				{
					string[] vals = props.GetProperty(call).Split(",");
					if (vals.Length != 3)
					{
						throw new Error("Syntax error while reading Javascript functions from resource");
					}
					Type clazz = Sharpen.Runtime.GetType(vals[0].Trim());
					string methodName = vals[1].Trim();
					int arity = System.Convert.ToInt32(vals[2].Trim());
					Type[] args = new Type[arity];
					Arrays.Fill(args, typeof(double));
					MethodInfo method = clazz.GetMethod(methodName, args);
					CheckFunction(method, typeof(JavascriptCompiler).GetClassLoader());
					map.Put(call, method);
				}
			}
			catch (Exception e)
			{
				throw new Error("Cannot resolve function", e);
			}
			DEFAULT_FUNCTIONS = Sharpen.Collections.UnmodifiableMap(map);
		}

		private static void CheckFunction(MethodInfo method, ClassLoader parent)
		{
			// We can only call the function if the given parent class loader of our compiled class has access to the method:
			ClassLoader functionClassloader = method.DeclaringType.GetClassLoader();
			if (functionClassloader != null)
			{
				// it is a system class iff null!
				bool found = false;
				while (parent != null)
				{
					if (parent == functionClassloader)
					{
						found = true;
						break;
					}
					parent = parent.GetParent();
				}
				if (!found)
				{
					throw new ArgumentException(method + " is not declared by a class which is accessible by the given parent ClassLoader."
						);
				}
			}
			// do some checks if the signature is "compatible":
			if (!Modifier.IsStatic(method.GetModifiers()))
			{
				throw new ArgumentException(method + " is not static.");
			}
			if (!Modifier.IsPublic(method.GetModifiers()))
			{
				throw new ArgumentException(method + " is not public.");
			}
			if (!Modifier.IsPublic(method.DeclaringType.GetModifiers()))
			{
				throw new ArgumentException(method.DeclaringType.FullName + " is not public.");
			}
			foreach (Type clazz in Sharpen.Runtime.GetParameterTypes(method))
			{
				if (!clazz.Equals(typeof(double)))
				{
					throw new ArgumentException(method + " must take only double parameters");
				}
			}
			if (method.ReturnType != typeof(double))
			{
				throw new ArgumentException(method + " does not return a double.");
			}
		}
	}
}
