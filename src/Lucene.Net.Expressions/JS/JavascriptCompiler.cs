using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Lucene.Net.Queries.Function;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JCG = J2N.Collections.Generic;
using J2N;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Expressions.JS
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

    /// <summary>An expression compiler for javascript expressions.</summary>
    /// <remarks>
    /// An expression compiler for javascript expressions.
    /// <para/>
    /// Example:
    /// <code>
    /// Expression foo = JavascriptCompiler.Compile("((0.3*popularity)/10.0)+(0.7*score)");
    /// </code>
    /// <para/>
    /// See the <see cref="Lucene.Net.Expressions.JS">package documentation</see> for
    /// the supported syntax and default functions.
    /// <para>
    /// You can compile with an alternate set of functions via <see cref="Compile(string, IDictionary{string, MethodInfo})"/>.
    /// For example:
    /// <code>
    /// // instantiate and add all the default functions
    /// IDictionary&lt;string, MethodInfo&gt; functions = new Dictionary&lt;string, MethodInfo&gt;(JavascriptCompiler.DEFAULT_FUNCTIONS);
    /// // add sqrt()
    /// functions["sqrt"] = (typeof(Math)).GetMethod("Sqrt", new Type[] { typeof(double) });
    /// // call compile with customized function map
    /// Expression foo = JavascriptCompiler.Compile("sqrt(score)+ln(popularity)", functions);
    /// </code>
    /// </para>
    /// @lucene.experimental
    /// </remarks>
    public class JavascriptCompiler
    {

        private static readonly string COMPILED_EXPRESSION_CLASS = typeof(Expression).Namespace + ".CompiledExpression";

        //private static readonly string COMPILED_EXPRESSION_INTERNAL = COMPILED_EXPRESSION_CLASS.Replace('.', '/'); // LUCENENET: Not used

        private static readonly Type EXPRESSION_TYPE = Type.GetType(typeof(Expression).FullName);

        private static readonly Type FUNCTION_VALUES_TYPE = typeof(FunctionValues);

        private static readonly ConstructorInfo EXPRESSION_CTOR = typeof(Expression).
            GetConstructor(new Type[] { typeof(string), typeof(string[]) });

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

        private readonly string sourceText;

        private readonly IDictionary<string, int> externalsMap = new JCG.LinkedDictionary<string, int>();

        private TypeBuilder dynamicType;

        private readonly IDictionary<string, MethodInfo> functions;

        

        private ILGenerator gen;
        private AssemblyBuilder asmBuilder;
        private MethodBuilder evalMethod;
        private ModuleBuilder modBuilder;


        // This maximum length is theoretically 65535 bytes, but as its CESU-8 encoded we dont know how large it is in bytes, so be safe
        // rcmuir: "If your ranking function is that large you need to check yourself into a mental institution!"
        /// <summary>Compiles the given expression.</summary>
        /// <param name="sourceText">The expression to compile</param>
        /// <returns>A new compiled expression</returns>
        /// <exception cref="ParseException">on failure to compile</exception>
        // LUCENENET TODO: ParseException not being thrown here - need to check
        // where this is thrown in Java and throw the equivalent in .NET
        public static Expression Compile(string sourceText)
        {
            return new JavascriptCompiler(sourceText).CompileExpression();
        }

        /// <summary>Compiles the given expression with the supplied custom functions.</summary>
        /// <remarks>
        /// Compiles the given expression with the supplied custom functions.
        /// <para/>
        /// Functions must be <c>public static</c>, return <see cref="double"/> and
        /// can take from zero to 256 <see cref="double"/> parameters.
        /// </remarks>
        /// <param name="sourceText">The expression to compile</param>
        /// <param name="functions">map of <see cref="string"/> names to functions</param>
        /// <returns>A new compiled expression</returns>
        /// <exception cref="ParseException">on failure to compile</exception>
        public static Expression Compile(string sourceText, IDictionary<string, MethodInfo> functions)
        {
            foreach (MethodInfo m in functions.Values)
            {
                CheckFunction(m);
            }
            return new JavascriptCompiler(sourceText, functions).CompileExpression();
        }

        /// <summary>This method is unused, it is just here to make sure that the function signatures don't change.</summary>
        /// <remarks>
        /// This method is unused, it is just here to make sure that the function signatures don't change.
        /// If this method fails to compile, you also have to change the byte code generator to correctly
        /// use the <see cref="FunctionValues"/> class.
        /// </remarks>
#pragma warning disable IDE0051 // Remove unused private members
        private static void UnusedTestCompile()
#pragma warning restore IDE0051 // Remove unused private members
        {
            FunctionValues f = null;
            /*double ret = */f.DoubleVal(2); // LUCENENET: IDE0059: Remove unnecessary value assignment
        }

        /// <summary>Constructs a compiler for expressions.</summary>
        /// <param name="sourceText">The expression to compile</param>
        private JavascriptCompiler(string sourceText)
            : this(sourceText, DEFAULT_FUNCTIONS)
        {
        }

        /// <summary>Constructs a compiler for expressions with specific set of functions</summary>
        /// <param name="sourceText">The expression to compile</param>
        /// <param name="functions">The set of functions to compile with</param>
        private JavascriptCompiler(string sourceText, IDictionary<string, MethodInfo> functions)
        {
            this.sourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.functions = functions;
        }

        /// <summary>Compiles the given expression with the specified parent classloader</summary>
        /// <returns>A new compiled expression</returns>
        /// <exception cref="ParseException">on failure to compile</exception>
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
                        Activator.CreateInstance(dynamicType.CreateTypeInfo().AsType(), sourceText, externalsMap.Keys.ToArray());

            }
            catch (Exception exception) when (exception.IsInstantiationException() || exception.IsIllegalAccessException() ||
                                              exception.IsNoSuchMethodException()  || exception.IsInvocationTargetException())
            {
                throw IllegalStateException.Create("An internal error occurred attempting to compile the expression (" + sourceText + ").", exception);
            }
        }

        private void BeginCompile()
        {
            var assemblyName = new AssemblyName("Lucene.Net.Expressions.Dynamic" + Math.Abs(new J2N.Randomizer().Next()));
            asmBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

            modBuilder = asmBuilder.DefineDynamicModule(assemblyName.Name + ".dll");

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
                        if (!functions.TryGetValue(call, out MethodInfo method) || method is null)
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
                        if (!externalsMap.TryGetValue(text, out int index))
                        {
                            externalsMap[text] = index = externalsMap.Count;
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
                        PushInt64(Convert.ToInt64(text, 16));
                        break;
                    }
                case JavascriptParser.OCTAL:
                    {
                        PushInt64(Convert.ToInt64(text, 8));
                        break;
                    }

                case JavascriptParser.DECIMAL:
                    {
                        //.NET Port. This is a bit hack-y but was needed since .NET can't perform bitwise ops on longs & doubles
                        var bitwiseOps = new[]{ ">>","<<","&","~","|","^"};

                        if (bitwiseOps.Any(s => sourceText.Contains(s)))
                        {
                            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                            {
                                gen.Emit(OpCodes.Ldc_I4, val);
                            }
                            else
                            {
                                gen.Emit(OpCodes.Ldc_I8, long.Parse(text, CultureInfo.InvariantCulture));
                                gen.Emit(OpCodes.Conv_Ovf_U4_Un);
                            }
                        }
                        else
                        {
                            gen.Emit(OpCodes.Ldc_R8, double.Parse(text, CultureInfo.InvariantCulture));
                        }
                        break;
                    }

                case JavascriptParser.AT_NEGATE:
                    {
                        RecursiveCompile(current.GetChild(0), typeof(double));
                        gen.Emit(OpCodes.Neg);
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
                        PushShift(OpCodes.Shl, current);
                        break;
                    }

                case JavascriptParser.AT_BIT_SHR:
                    {
                        PushShift(OpCodes.Shr, current);
                        break;
                    }

                case JavascriptParser.AT_BIT_SHU:
                    {
                        PushShift(OpCodes.Shr_Un, current);
                        break;
                    }

                case JavascriptParser.AT_BIT_AND:
                    {
                        PushBitwise(OpCodes.And, current);
                        break;
                    }

                case JavascriptParser.AT_BIT_OR:
                    {
                        PushBitwise(OpCodes.Or, current);
                        break;
                    }

                case JavascriptParser.AT_BIT_XOR:
                    {
                        PushBitwise(OpCodes.Xor, current);
                        break;
                    }

                case JavascriptParser.AT_BIT_NOT:
                    {
                        RecursiveCompile(current.GetChild(0), typeof(long));
                        gen.Emit(OpCodes.Not);
                        gen.Emit(OpCodes.Conv_R8);
                        break;
                    }

                case JavascriptParser.AT_COMP_EQ:
                    {
                        PushCond(OpCodes.Ceq, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_NEQ:
                    {
                        PushCondEq(OpCodes.Ceq, current, expected);
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
                        PushCondEq(OpCodes.Cgt, current, expected);
                        break;
                    }

                case JavascriptParser.AT_COMP_GTE:
                    {
                        PushCondEq(OpCodes.Clt, current, expected);
                        break;
                    }

                case JavascriptParser.AT_BOOL_NOT:
                    {
                        RecursiveCompile(current.GetChild(0), typeof(int));
                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);
                        gen.Emit(OpCodes.Conv_R8);
                        break;
                    }

                case JavascriptParser.AT_BOOL_AND:
                    {

                        RecursiveCompile(current.GetChild(0), typeof(int));
                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);
                        RecursiveCompile(current.GetChild(1), typeof(int));

                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);

                        gen.Emit(OpCodes.Or);

                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);

                        gen.Emit(OpCodes.Conv_R8);


                        break;
                    }

                case JavascriptParser.AT_BOOL_OR:
                    {
                        RecursiveCompile(current.GetChild(0), typeof(int));
                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);
                        gen.Emit(OpCodes.Ldc_I4_1);
                        gen.Emit(OpCodes.Xor);
                        RecursiveCompile(current.GetChild(1), typeof(int));

                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);
                        gen.Emit(OpCodes.Ldc_I4_1);
                        gen.Emit(OpCodes.Xor);
                        gen.Emit(OpCodes.Or);

                        gen.Emit(OpCodes.Ldc_I4_1);
                        gen.Emit(OpCodes.Ceq);

                        gen.Emit(OpCodes.Conv_R8);
                        break;
                    }

                case JavascriptParser.AT_COND_QUE:
                    {
                        Label condFalse = gen.DefineLabel();
                        Label condEnd = gen.DefineLabel();
                        RecursiveCompile(current.GetChild(0), typeof(int));
                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Beq,condFalse);
                        RecursiveCompile(current.GetChild(1), expected);
                        gen.Emit(OpCodes.Br_S,condEnd);
                        gen.MarkLabel(condFalse);
                        RecursiveCompile(current.GetChild(2), expected);
                        gen.MarkLabel(condEnd);
                        break;
                    }

                default:
                    {
                        throw IllegalStateException.Create("Unknown operation specified: (" + current.Text + ").");
                    }
            }

        }

        private void PushCondEq(OpCode opCode, ITree current, Type expected)
        {
            RecursiveCompile(current.GetChild(0), expected);
            RecursiveCompile(current.GetChild(1), expected);
            gen.Emit(opCode);
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Xor);
            gen.Emit(OpCodes.Conv_R8);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void PushArith(OpCode op, ITree current, Type expected)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            PushBinaryOp(op, current, typeof(double), typeof(double));
        }

        private void PushShift(OpCode op, ITree current)
        {
            PushBinaryShiftOp(op, current, typeof(int), typeof(int));
        }

        private void PushBinaryShiftOp(OpCode op, ITree current, Type arg1, Type arg2)
        {
            gen.Emit(OpCodes.Nop);
            RecursiveCompile(current.GetChild(0), arg1);
            RecursiveCompile(current.GetChild(1), arg2);
            gen.Emit(op);
            gen.Emit(OpCodes.Conv_R8);
        }

        private void PushBitwise(OpCode op, ITree current)
        {
            PushBinaryOp(op, current, typeof(long), typeof(long));
        }

        private void PushBinaryOp(OpCode op, ITree current, Type arg1, Type arg2)
        {
            gen.Emit(OpCodes.Nop);
            RecursiveCompile(current.GetChild(0), arg1);
            RecursiveCompile(current.GetChild(1), arg2);
            gen.Emit(op);
            gen.Emit(OpCodes.Conv_R8);
        }

        private void PushCond(OpCode opCode, ITree current, Type expected)
        {
            RecursiveCompile(current.GetChild(0), expected);
            RecursiveCompile(current.GetChild(1), expected);
            gen.Emit(opCode);
            gen.Emit(OpCodes.Conv_R8);
        }

        /// <summary>
        /// NOTE: This was pushLong() in Lucene
        /// </summary>
        private void PushInt64(long i)
        {
            gen.Emit(OpCodes.Ldc_I8,i);
            if (!sourceText.Contains("<<"))
            {
                gen.Emit(OpCodes.Conv_R8);
            }
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
            // LUCENENET: Antlr 3.5.1 doesn't ever wrap ParseException, so the other catch block
            // for RuntimeException that wraps ParseException would have
            // been completely unnecesary in Java and is also unnecessary here.
        }

        /// <summary>The default set of functions available to expressions.</summary>
        /// <remarks>
        /// The default set of functions available to expressions.
        /// <para/>
        /// See the <see cref="Lucene.Net.Expressions.JS">package documentation</see> for a list.
        /// </remarks>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly IDictionary<string, MethodInfo> DEFAULT_FUNCTIONS = LoadDefaultFunctions();

        private static IDictionary<string, MethodInfo> LoadDefaultFunctions() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            IDictionary<string, MethodInfo> map = new Dictionary<string, MethodInfo>();
            try
            {
                foreach (var property in GetDefaultSettings())
                {
                    string[] vals = property.Value.Split(',').TrimEnd();
                    if (vals.Length != 3)
                    {
                        throw Error.Create("Error reading Javascript functions from settings");
                    }
                    string typeName = vals[0];

                    Type clazz;

                    if (vals[0].Contains("Lucene.Net"))
                    {
                        clazz = GetType(vals[0] + ", Lucene.Net");
                    }
                    else
                    {
                        clazz = GetType(typeName);
                    }

                    string methodName = vals[1].Trim();
                    int arity = int.Parse(vals[2], CultureInfo.InvariantCulture);
                    Type[] args = new Type[arity];
                    Arrays.Fill(args, typeof(double));
                    MethodInfo method = clazz.GetMethod(methodName, args);
                    CheckFunction(method);
                    map[property.Key] = method;
                }
            }
            catch (Exception e) when (e.IsNoSuchMethodException() || e.IsClassNotFoundException() || e.IsIOException())
            {
                throw Error.Create("Cannot resolve function", e);
            }
            return map.AsReadOnly();
        }

        private static Type GetType(string typeName)
        {
            try
            {
                return Type.GetType(typeName, true);
            }
            catch
            {
                return null;
            }
        }

        private static IDictionary<string, string> GetDefaultSettings()
        {
            var settings = new Dictionary<string, string>();
            var type = typeof(JavascriptCompiler);
            using  (var reader = new StreamReader(type.FindAndGetManifestResourceStream(type.Name + ".properties"), Encoding.UTF8))
                settings.LoadProperties(reader);
            return settings;
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
