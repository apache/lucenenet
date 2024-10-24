using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using J2N.Text;
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
                var antlrTree = GetAntlrComputedExpressionTree();
                BeginCompile();
                RecursiveCompile(antlrTree);
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

        private void RecursiveCompile(JavascriptParser.ExpressionContext context)
        {
            var listener = new JavascriptListener(this);
            listener.EnterExpression(context);
        }

        // LUCENENET-specific: these methods use the ANTLRv4 listener API to
        // be equivalent to what RecursiveCompile was doing previously
        private class JavascriptListener : JavascriptBaseListener
        {
            private readonly JavascriptCompiler _compiler;

            public JavascriptListener(JavascriptCompiler compiler)
            {
                _compiler = compiler;
            }

            public override void EnterExpression(JavascriptParser.ExpressionContext context)
            {
                EnterConditional(context.conditional());
            }

            public override void EnterCall(JavascriptParser.CallContext context)
            {
                ITerminalNode identifier = context.NAMESPACE_ID();
                string call = identifier.GetText();
                // LUCENENET: logic changed to get arguments from parse context
                var arguments = context.arguments().conditional();
                int argumentCount = arguments.Length;
                if (!_compiler.functions.TryGetValue(call, out MethodInfo method) || method is null)
                {
                    throw new ArgumentException("Unrecognized method call (" + call + ").");
                }
                int arity = method.GetParameters().Length;
                if (argumentCount != arity)
                {
                    throw new ArgumentException("Expected (" + arity + ") arguments for method call ("
                                                + call + "), but found (" + argumentCount + ").");
                }
                for (int argument = 0; argument < argumentCount; ++argument) // LUCENENET: was 1 to and including arguments
                {
                    EnterConditional(arguments[argument]);
                }
                _compiler.gen.Emit(OpCodes.Call, method);
            }

            public override void EnterPrimary(JavascriptParser.PrimaryContext context)
            {
                if (context.NAMESPACE_ID() is { } namespaceId)
                {
                    string text = namespaceId.GetText();

                    if (!_compiler.externalsMap.TryGetValue(text, out int index))
                    {
                        _compiler.externalsMap[text] = index = _compiler.externalsMap.Count;
                    }

                    _compiler.gen.Emit(OpCodes.Nop);

                    _compiler.gen.Emit(OpCodes.Ldarg_2);
                    _compiler.gen.Emit(OpCodes.Ldc_I4, index);

                    _compiler.gen.Emit(OpCodes.Ldelem_Ref);
                    _compiler.gen.Emit(OpCodes.Ldarg_1);
                    _compiler.gen.Emit(OpCodes.Callvirt, DOUBLE_VAL_METHOD);
                }
                else if (context.numeric() is { } numeric)
                {
                    EnterNumeric(numeric);
                }
                else if (context.conditional() is { } conditional)
                {
                    EnterConditional(conditional);
                }
                else
                {
                    throw new InvalidOperationException("Unknown primary alternative");
                }
            }

            public override void EnterNumeric(JavascriptParser.NumericContext context)
            {
                string text = context.GetText();

                if (context.HEX() is not null)
                {
                    _compiler.PushInt64(Convert.ToInt64(text, 16));
                }
                else if (context.OCTAL() is not null)
                {
                    _compiler.PushInt64(Convert.ToInt64(text, 8));
                }
                else
                {
                    // decimal
                    //.NET Port. This is a bit hack-y but was needed since .NET can't perform bitwise ops on longs & doubles
                    var bitwiseOps = new[]{ ">>", "<<", "&", "~", "|", "^" };

                    if (bitwiseOps.Any(s => text.Contains(s)))
                    {
                        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                        {
                            _compiler.gen.Emit(OpCodes.Ldc_I4, val);
                        }
                        else
                        {
                            _compiler.gen.Emit(OpCodes.Ldc_I8, long.Parse(text, CultureInfo.InvariantCulture));
                            _compiler.gen.Emit(OpCodes.Conv_Ovf_U4_Un);
                        }
                    }
                    else
                    {
                        _compiler.gen.Emit(OpCodes.Ldc_R8, double.Parse(text, CultureInfo.InvariantCulture));
                    }
                }
            }

            public override void EnterPostfix(JavascriptParser.PostfixContext context)
            {
                if (context.primary() is { } primary)
                {
                    EnterPrimary(primary);
                }
                else if (context.call() is { } call)
                {
                    EnterCall(call);
                }
                else
                {
                    throw new InvalidOperationException("Unknown postfix alternative");
                }
            }

            public override void EnterUnary(JavascriptParser.UnaryContext context)
            {
                if (context.postfix() is { } postfix)
                {
                    EnterPostfix(postfix);
                }
                else if (context.AT_ADD() is not null)
                {
                    // LUCENENET-specific: it appears that 4.8 had a bug where
                    // it would push an "add" opcode here, but that is not correct
                    // for the unary + operator.
                    throw new NotImplementedException("Unary + is not supported");
                }
                else if (context.unary_operator() is { } unaryOperator)
                {
                    EnterUnary(context.unary());

                    if (unaryOperator.AT_SUBTRACT() is not null)
                    {
                        _compiler.gen.Emit(OpCodes.Neg);
                    }
                    else if (unaryOperator.AT_BIT_NOT() is not null)
                    {
                        _compiler.gen.Emit(OpCodes.Not);
                        _compiler.gen.Emit(OpCodes.Conv_R8);
                    }
                    else if (unaryOperator.AT_BOOL_NOT() is not null)
                    {
                        _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                        _compiler.gen.Emit(OpCodes.Ceq);
                        _compiler.gen.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown unary_operator alternative");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unknown unary alternative");
                }
            }

            public override void EnterAdditive(JavascriptParser.AdditiveContext context)
            {
                CompileBinary(context, context.multiplicative, EnterMultiplicative, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_ADD)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Add);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_SUBTRACT)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Sub);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown additive token");
                    }
                });
            }

            public override void EnterMultiplicative(JavascriptParser.MultiplicativeContext context)
            {
                CompileBinary(context, context.unary, EnterUnary, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_MULTIPLY)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Mul);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_DIVIDE)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Div);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_MODULO)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Rem);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown multiplicative token");
                    }
                });
            }

            public override void EnterShift(JavascriptParser.ShiftContext context)
            {
                CompileBinary(context, context.additive, EnterAdditive, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_SHL)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Shl);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_SHR)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Shr);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_SHU)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Shr_Un);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown shift token");
                    }
                });
            }

            public override void EnterRelational(JavascriptParser.RelationalContext context)
            {
                CompileBinary(context, context.shift, EnterShift, terminalNode => {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_LT)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Clt);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_GT)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Cgt);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_LTE)
                    {
                        _compiler.PushCondEq(OpCodes.Cgt);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_GTE)
                    {
                        _compiler.PushCondEq(OpCodes.Clt);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown relational token");
                    }
                });
            }

            public override void EnterEquality(JavascriptParser.EqualityContext context)
            {
                CompileBinary(context, context.relational, EnterRelational, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_EQ)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Ceq);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_NEQ)
                    {
                        _compiler.PushCondEq(OpCodes.Ceq);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown equality token");
                    }
                });
            }

            public override void EnterBitwise_and(JavascriptParser.Bitwise_andContext context)
            {
                CompileBinary(context, context.equality, EnterEquality, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_AND)
                    {
                        _compiler.PushOpWithConvert(OpCodes.And);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown bitwise_and token");
                    }
                });
            }

            public override void EnterBitwise_xor(JavascriptParser.Bitwise_xorContext context)
            {
                CompileBinary(context, context.bitwise_and, EnterBitwise_and, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_XOR)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Xor);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown bitwise_xor token");
                    }
                });
            }

            public override void EnterBitwise_or(JavascriptParser.Bitwise_orContext context)
            {
                CompileBinary(context, context.bitwise_xor, EnterBitwise_xor, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_OR)
                    {
                        _compiler.PushOpWithConvert(OpCodes.Or);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown bitwise_or token");
                    }
                });
            }

            public override void EnterLogical_and(JavascriptParser.Logical_andContext context)
            {
                // Evaluate the first operand and check if it is false
                EnterBitwise_or(context.bitwise_or(0));
                _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                _compiler.gen.Emit(OpCodes.Ceq);

                // Iterate over the remaining operands
                for (int i = 2; i < context.children.Count; i += 2)
                {
                    if (context.children[i] is not JavascriptParser.Bitwise_orContext bitwiseOrContext)
                    {
                        throw new InvalidOperationException("Unexpected child of logical_and");
                    }

                    // Evaluate the next operand and check if it is false
                    EnterBitwise_or(bitwiseOrContext);
                    _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                    _compiler.gen.Emit(OpCodes.Ceq);

                    // Combine the results using OR
                    _compiler.gen.Emit(OpCodes.Or);
                }

                // Check if the combined result is false
                _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                _compiler.gen.Emit(OpCodes.Ceq);

                // Convert the result to a double
                _compiler.gen.Emit(OpCodes.Conv_R8);
            }

            public override void EnterLogical_or(JavascriptParser.Logical_orContext context)
            {
                // Evaluate the first operand and check if it is true
                EnterLogical_and(context.logical_and(0));
                _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                _compiler.gen.Emit(OpCodes.Ceq);
                _compiler.gen.Emit(OpCodes.Ldc_I4_1);
                _compiler.gen.Emit(OpCodes.Xor);

                // Iterate over the remaining operands
                for (int i = 2; i < context.children.Count; i += 2)
                {
                    if (context.children[i] is not JavascriptParser.Logical_andContext logicalAndContext)
                    {
                        throw new InvalidOperationException("Unexpected child of logical_or");
                    }

                    // Evaluate the next operand and check if it is true
                    EnterLogical_and(logicalAndContext);
                    _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                    _compiler.gen.Emit(OpCodes.Ceq);
                    _compiler.gen.Emit(OpCodes.Ldc_I4_1);
                    _compiler.gen.Emit(OpCodes.Xor);

                    // Combine the results using OR
                    _compiler.gen.Emit(OpCodes.Or);
                }

                // Check if the combined result is true
                _compiler.gen.Emit(OpCodes.Ldc_I4_1);
                _compiler.gen.Emit(OpCodes.Ceq);

                // Convert the result to a double
                _compiler.gen.Emit(OpCodes.Conv_R8);
            }

            public override void EnterConditional(JavascriptParser.ConditionalContext context)
            {
                if (context.children.Count == 1)
                {
                    EnterLogical_or(context.logical_or());
                }
                else
                {
                    Label condFalse = _compiler.gen.DefineLabel();
                    Label condEnd = _compiler.gen.DefineLabel();

                    // Evaluate the condition
                    EnterLogical_or(context.logical_or());
                    _compiler.gen.Emit(OpCodes.Ldc_I4_0);
                    _compiler.gen.Emit(OpCodes.Beq, condFalse);

                    // Evaluate the true branch
                    EnterConditional(context.conditional(0));
                    _compiler.gen.Emit(OpCodes.Br_S, condEnd);

                    // Evaluate the false branch
                    _compiler.gen.MarkLabel(condFalse);
                    EnterConditional(context.conditional(1));

                    // Mark the end of the conditional
                    _compiler.gen.MarkLabel(condEnd);
                }
            }

            private static void CompileBinary<TContext, TArg>(
                TContext context,
                Func<int, TArg> getArg,
                Action<TArg> enterArg,
                Action<ITerminalNode> enterOp)
                where TContext : ParserRuleContext
                where TArg : ParserRuleContext
            {
                int argIndex = 0;
                enterArg(getArg(argIndex++));

                for (int i = 1; i < context.children.Count; i += 2)
                {
                    if (context.children[i] is ITerminalNode terminalNode)
                    {
                        enterArg(getArg(argIndex++));
                        enterOp(terminalNode);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected child");
                    }
                }
            }
        }

        private void PushCondEq(OpCode opCode)
        {
            gen.Emit(opCode);
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Xor);
            gen.Emit(OpCodes.Conv_R8);
        }

        private void PushOpWithConvert(OpCode opCode)
        {
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

        private JavascriptParser.ExpressionContext GetAntlrComputedExpressionTree()
        {
            ICharStream input = new AntlrInputStream(sourceText);
            JavascriptLexer lexer = new JavascriptLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            JavascriptParser parser = new JavascriptParser(tokens);
            try
            {
                return parser.expression();
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
            return Collections.AsReadOnly(map);
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
            using var reader = new StreamReader(type.FindAndGetManifestResourceStream(type.Name + ".properties"), Encoding.UTF8);
            settings.LoadProperties(reader);
            return settings;
        }

        private static void CheckFunction(MethodInfo method)
        {
            // do some checks if the signature is "compatible":
            if (!method.IsStatic)
            {
                throw new ArgumentException(method + " is not static.");
            }
            if (!method.IsPublic)
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
