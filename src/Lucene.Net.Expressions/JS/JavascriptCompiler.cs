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
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
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
    /// <see cref="Compile(string, System.Collections.Generic.IDictionary{K, V})
    /// 	">Compile(string, System.Collections.Generic.IDictionary&lt;K, V&gt;)
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

        private static readonly string COMPILED_EXPRESSION_CLASS = typeof(Expression).Namespace + ".CompiledExpression";

        private static readonly string COMPILED_EXPRESSION_INTERNAL = COMPILED_EXPRESSION_CLASS.Replace('.', '/');

        private static readonly Type EXPRESSION_TYPE = Type.GetType(typeof(Expression).FullName);

        private static readonly Type FUNCTION_VALUES_TYPE = typeof(FunctionValues);

        private static readonly ConstructorInfo EXPRESSION_CTOR = typeof(Expression).
            GetConstructor(new Type[] { typeof(String), typeof(String[]) });

        private static readonly MethodInfo EVALUATE_METHOD = GetMethod(EXPRESSION_TYPE, "Evaluate",
            new[] { typeof(int), typeof(FunctionValues[]) });

        private static readonly MethodInfo DOUBLE_VAL_METHOD = GetMethod(FUNCTION_VALUES_TYPE, "DoubleVal",
            new[] { typeof(int) });


        // We use the same class name for all generated classes as they all have their own class loader.
        // The source code is displayed as "source file name" in stack trace.
        // to work around import clash:
        private static MethodInfo GetMethod(Type type, string method, Type[] parms)
        {
            return type.GetMethod(method, parms);
        }

        private const int MAX_SOURCE_LENGTH = 16384;

        private readonly string sourceText;

        private readonly IDictionary<string, int> externalsMap = new HashMap<string, int>();



        private TypeBuilder dynamicType;

        private readonly IDictionary<string, MethodInfo> functions;

        /// <summary>The default set of functions available to expressions.</summary>
        /// <remarks>
        /// The default set of functions available to expressions.
        /// <p>
        /// See the
        /// <see cref="Lucene.Net.Expressions.JS">package documentation</see>
        /// for a list.
        /// </remarks>
        public static readonly IDictionary<string, MethodInfo> DEFAULT_FUNCTIONS;

        private ILGenerator gen;
        private AssemblyBuilder asmBuilder;
        private string fileName;
        private ISymbolDocumentWriter debugDoc;
        private int lineNum = 1;
        private StreamWriter file;
        private MethodBuilder evalMethod;
        private bool negate;

        // This maximum length is theoretically 65535 bytes, but as its CESU-8 encoded we dont know how large it is in bytes, so be safe
        // rcmuir: "If your ranking function is that large you need to check yourself into a mental institution!"
        /// <summary>Compiles the given expression.</summary>
        /// <remarks>Compiles the given expression.</remarks>
        /// <param name="sourceText">The expression to compile</param>
        /// <returns>A new compiled expression</returns>

        public static Expression Compile(string sourceText)
        {
            return new JavascriptCompiler(sourceText).CompileExpression();
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

        public static Expression Compile(string sourceText, IDictionary<string, MethodInfo> functions)
        {

            foreach (MethodInfo m in functions.Values)
            {
                CheckFunction(m);
            }
            return new JavascriptCompiler(sourceText, functions).CompileExpression();
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

        /// <param name="sourceText">The expression to compile</param>
        private JavascriptCompiler(string sourceText)
            : this(sourceText, DEFAULT_FUNCTIONS
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

        private Expression CompileExpression()
        {
            try
            {

                ITree antlrTree = GetAntlrComputedExpressionTree();
                BeginCompile();
                RecursiveCompile(antlrTree, typeof(double));
                EndCompile();
                return
                    (Expression)
                        Activator.CreateInstance(dynamicType.CreateType(), sourceText, externalsMap.Keys.ToArray());

            }

            catch (MemberAccessException exception)
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
            var assemblyName = new AssemblyName("Lucene.Net.Expressions.Dynamic" + new Random().Next());
            asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(assemblyName.Name + ".dll");
            dynamicType = modBuilder.DefineType(COMPILED_EXPRESSION_CLASS,
                TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.Public | TypeAttributes.Class |
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, EXPRESSION_TYPE);


            ConstructorBuilder constructorBuilder = dynamicType.DefineConstructor(MethodAttributes.Public,
                CallingConventions.HasThis,
                new[] { typeof(string), typeof(string[]) });

            ILGenerator ctorGen = constructorBuilder.GetILGenerator();
            ctorGen.Emit(OpCodes.Ldarg_0);
            ctorGen.Emit(OpCodes.Ldarg_1);
            ctorGen.Emit(OpCodes.Ldarg_2);
            ctorGen.Emit(OpCodes.Call, EXPRESSION_CTOR);
            ctorGen.Emit(OpCodes.Nop);
            ctorGen.Emit(OpCodes.Nop);
            ctorGen.Emit(OpCodes.Ret);

            evalMethod = dynamicType.DefineMethod("Evaluate", MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(double), new[] { typeof(int), typeof(FunctionValues[]) });
            gen = evalMethod.GetILGenerator();
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
                            RecursiveCompile(current.GetChild(argument), typeof(double));
                        }
                        gen.Emit(OpCodes.Call, method);



                        break;
                    }

                case JavascriptParser.NAMESPACE_ID:
                    {
                        int index;
                        if (externalsMap.ContainsKey(text))
                        {
                            index = externalsMap[text];
                        }
                        else
                        {
                            index = externalsMap.Count;
                            externalsMap[text] = index;
                        }
                        gen.Emit(OpCodes.Nop);

                        gen.Emit(OpCodes.Ldarg_2);
                        gen.Emit(OpCodes.Ldc_I4, index);

                        gen.Emit(OpCodes.Ldelem_Ref);
                        gen.Emit(OpCodes.Ldarg_1);
                        gen.Emit(OpCodes.Callvirt, DOUBLE_VAL_METHOD);


                        break;
                    }

                case JavascriptParser.HEX:
                    {
                        PushLong(expected, Convert.ToInt64(text.Substring(2), 16));
                        break;
                    }

                case JavascriptParser.OCTAL:
                    {
                        PushLong(expected, Convert.ToInt64(text.Substring(2), 8));
                        break;
                    }

                case JavascriptParser.DECIMAL:
                    {
                        gen.Emit(OpCodes.Ldc_R8, double.Parse(text));
                        if (negate)
                        {
                            gen.Emit(OpCodes.Neg);
                            negate = false;
                        }
                        break;
                    }

                case JavascriptParser.AT_NEGATE:
                    {
                        negate = true;
                        RecursiveCompile(current.GetChild(0), typeof(double));

                        break;
                    }

                case JavascriptParser.AT_ADD:
                    {
                        PushArith(OpCodes.Add, current, expected);
                        break;
                    }

                case JavascriptParser.AT_SUBTRACT:
                    {
                        PushArith(OpCodes.Sub, current, expected);
                        break;
                    }

                case JavascriptParser.AT_MULTIPLY:
                    {
                        PushArith(OpCodes.Mul, current, expected);
                        break;
                    }

                case JavascriptParser.AT_DIVIDE:
                    {
                        PushArith(OpCodes.Div, current, expected);
                        break;
                    }

                case JavascriptParser.AT_MODULO:
                    {
                        PushArith(OpCodes.Rem, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_SHL:
                    {
                        PushShift(OpCodes.Shl, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_SHR:
                    {
                        PushShift(OpCodes.Shr, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_SHU:
                    {
                        PushShift(OpCodes.Shr_Un, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_AND:
                    {
                        PushBitwise(OpCodes.And, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_OR:
                    {
                        PushBitwise(OpCodes.Or, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_XOR:
                    {
                        PushBitwise(OpCodes.Xor, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BIT_NOT:
                    {
                        RecursiveCompile(current.GetChild(0), typeof(long));
                        gen.Emit(OpCodes.Ldc_I4_M1);
                        //dynamicType.Push(-1L);
                        //dynamicType.VisitInsn(Opcodes.LXOR);
                        //dynamicType.Cast(typeof(long), expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_EQ:
                    {
                        PushCond(OpCodes.Ceq, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_NEQ:
                    {
                        //PushCond(OpCodes, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_LT:
                    {
                        PushCond(OpCodes.Clt, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_GT:
                    {
                        PushCond(OpCodes.Cgt, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_LTE:
                    {
                        //PushCond(OpCodes.Clt | OpCodes.Ceq, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_GTE:
                    {
                        //PushCond(GeneratorAdapter.GE, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BOOL_NOT:
                    {
                        /*Label labelNotTrue = new Label();
					Label labelNotReturn = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					dynamicType.VisitJumpInsn(Opcodes.IFEQ, labelNotTrue);
					PushBoolean(expected, false);
					dynamicType.GoTo(labelNotReturn);
					dynamicType.VisitLabel(labelNotTrue);
					PushBoolean(expected, true);
					dynamicType.VisitLabel(labelNotReturn);*/
                        break;
                    }

                case JavascriptParser.AT_BOOL_AND:
                    {
                        /*Label andFalse = new Label();
					Label andEnd = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					dynamicType.VisitJumpInsn(Opcodes.IFEQ, andFalse);
					RecursiveCompile(current.GetChild(1), Type.INT_TYPE);
					dynamicType.VisitJumpInsn(Opcodes.IFEQ, andFalse);
					PushBoolean(expected, true);
					dynamicType.GoTo(andEnd);
					dynamicType.VisitLabel(andFalse);
					PushBoolean(expected, false);
					dynamicType.VisitLabel(andEnd);*/
                        break;
                    }

                case JavascriptParser.AT_BOOL_OR:
                    {
                        Label orTrue = new Label();
                        Label orEnd = new Label();
                        /*RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					dynamicType.VisitJumpInsn(Opcodes.IFNE, orTrue);
					RecursiveCompile(current.GetChild(1), Type.INT_TYPE);
					dynamicType.VisitJumpInsn(Opcodes.IFNE, orTrue);
					PushBoolean(expected, false);
					dynamicType.GoTo(orEnd);
					dynamicType.VisitLabel(orTrue);
					PushBoolean(expected, true);
					dynamicType.VisitLabel(orEnd);*/
                        break;
                    }

                case JavascriptParser.AT_COND_QUE:
                    {
                        /*Label condFalse = new Label();
					Label condEnd = new Label();
					RecursiveCompile(current.GetChild(0), Type.INT_TYPE);
					dynamicType.VisitJumpInsn(Opcodes.IFEQ, condFalse);
					RecursiveCompile(current.GetChild(1), expected);
					dynamicType.GoTo(condEnd);
					dynamicType.VisitLabel(condFalse);
					RecursiveCompile(current.GetChild(2), expected);
					dynamicType.VisitLabel(condEnd);*/
                        break;
                    }

                default:
                    {
                        throw new InvalidOperationException("Unknown operation specified: (" + current.Text + ").");
                    }
            }

        }

        private void PushArith(OpCode op, ITree current, Type expected)
        {
            PushBinaryOp(op, current, expected, typeof(double), typeof(double), typeof(double));
        }

        private void PushShift(OpCode op, ITree current, Type expected)
        {
            PushBinaryOp(op, current, expected, typeof(long), typeof(int), typeof(long));
        }

        private void PushBitwise(OpCode op, ITree current, Type expected)
        {
            PushBinaryOp(op, current, expected, typeof(long), typeof(long), typeof(long));
        }

        private void PushBinaryOp(OpCode op, ITree current, Type expected, Type arg1, Type arg2, Type returnType)
        {
            gen.Emit(OpCodes.Nop);
            RecursiveCompile(current.GetChild(0), arg1);
            RecursiveCompile(current.GetChild(1), arg2);
            /* gen.Emit(OpCodes.Add, debugDoc, file, lineNum++);
             gen.Emit(OpCodes.Stloc_0, debugDoc, file, lineNum++);
             gen.Emit(OpCodes.Br_S, debugDoc, file, lineNum++);
             gen.Emit(OpCodes.Ldloc_0, debugDoc, file, lineNum++);*/
            gen.Emit(op);
            /*dynamicType.VisitInsn(op);
			dynamicType.Cast(returnType, expected);*/
        }

        private void PushCond(OpCode @operator, ITree current, Type expected)
        {
            Label labelTrue = new Label();
            Label labelReturn = new Label();
            RecursiveCompile(current.GetChild(0), typeof(double));
            RecursiveCompile(current.GetChild(1), typeof(double));
            /*dynamicType.IfCmp(typeof(double), @operator, labelTrue);
			PushBoolean(expected, false);
			dynamicType.GoTo(labelReturn);
			dynamicType.VisitLabel(labelTrue);
			PushBoolean(expected, true);
			dynamicType.VisitLabel(labelReturn);*/
        }

        /*
                private void PushBoolean(Type expected, bool truth)
                {
                    switch (expected.GetSort())
                    {
                        case Type.INT:
                        {
                            dynamicType.Push(truth);
                            break;
                        }

                        case Type.LONG:
                        {
                            dynamicType.Push(truth ? 1L : 0L);
                            break;
                        }

                        case Type.DOUBLE:
                        {
                            dynamicType.Push(truth ? 1. : 0.);
                            break;
                        }

                        default:
                        {
                            throw new InvalidOperationException("Invalid expected type: " + expected);
                        }
                    }
                }
        */

        private void PushLong(Type expected, long i)
        {
            /*switch (expected.GetSort())
			{
				case Type.INT:
				{
					dynamicType.Push((int)i);
					break;
				}

				case Type.LONG:
				{
					dynamicType.Push(i);
					break;
				}

				case Type.DOUBLE:
				{
					dynamicType.Push((double)i);
					break;
				}

				default:
				{
					throw new InvalidOperationException("Invalid expected type: " + expected);
				}
			}*/
            gen.Emit(OpCodes.Ldarg_0);
        }

        private void EndCompile()
        {
            gen.Emit(OpCodes.Ret);
            dynamicType.DefineMethodOverride(evalMethod, EVALUATE_METHOD);
        }



        private ITree GetAntlrComputedExpressionTree()
        {
            ICharStream input = new ANTLRStringStream(sourceText);
            JavascriptLexer lexer = new JavascriptLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            JavascriptParser parser = new JavascriptParser(tokens);
            try
            {
                return parser.Expression().Tree;
            }
            catch (RecognitionException re)
            {
                throw new ArgumentException(re.Message, re);
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



        static JavascriptCompiler()
        {
            IDictionary<string, MethodInfo> map = new Dictionary<string, MethodInfo>();
            try
            {
                var props = Properties.Settings.Default;
                foreach (SettingsProperty property in props.Properties)
                {
                    string[] vals = props[property.Name].ToString().Split(',');
                    if (vals.Length != 3)
                    {
                        throw new Exception("Error reading Javascript functions from settings");
                    }
                    string typeName = vals[0];
                    if (vals[0].Contains("Lucene.Net"))
                    {
                        typeName = vals[0] + ",Lucene.Net";
                    }
                    Type clazz = Type.GetType(typeName, true);
                    string methodName = vals[1].Trim();
                    int arity = int.Parse(vals[2]);
                    Type[] args = new Type[arity];
                    Arrays.Fill(args, typeof(double));
                    MethodInfo method = clazz.GetMethod(methodName, args);
                    CheckFunction(method);
                    map[property.Name] = method;
                }


            }
            catch (Exception e)
            {
                throw new Exception("Cannot resolve function", e);
            }
            DEFAULT_FUNCTIONS = map;
        }

        private static void CheckFunction(MethodInfo method)
        {

            // do some checks if the signature is "compatible":
            if (!(method.IsStatic))
            {
                throw new ArgumentException(method + " is not static.");
            }
            if (!(method.IsPublic))
            {
                throw new ArgumentException(method + " is not public.");
            }
            if (!method.DeclaringType.IsPublic)
            {
                //.NET Port. Inner class is being returned as not public even when declared public
                if (method.DeclaringType.IsNestedAssembly)
                {
                    throw new ArgumentException(method.DeclaringType.FullName + " is not public.");
                }
            }
            if (method.GetParameters().Any(parmType => parmType.ParameterType != (typeof(double))))
            {
                throw new ArgumentException(method + " must take only double parameters");
            }
            if (method.ReturnType != typeof(double))
            {
                throw new ArgumentException(method + " does not return a double.");
            }
        }
    }

    
}
