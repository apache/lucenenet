using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using J2N;
using J2N.Text;
using Lucene.Net.Queries.Function;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using JCG = J2N.Collections.Generic;

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
            /*double ret = */
            f.DoubleVal(2); // LUCENENET: IDE0059: Remove unnecessary value assignment
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
                                              exception.IsNoSuchMethodException() || exception.IsInvocationTargetException())
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
            private readonly JavascriptCompiler compiler;

            public JavascriptListener(JavascriptCompiler compiler)
            {
                this.compiler = compiler;
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
                if (!compiler.functions.TryGetValue(call, out MethodInfo method) || method is null)
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
                compiler.Emit(OpCodes.Call, method);
            }

            public override void EnterPrimary(JavascriptParser.PrimaryContext context)
            {
                if (context.NAMESPACE_ID() is { } namespaceId)
                {
                    string text = namespaceId.GetText();

                    if (!compiler.externalsMap.TryGetValue(text, out int index))
                    {
                        compiler.externalsMap[text] = index = compiler.externalsMap.Count;
                    }

                    compiler.Emit(OpCodes.Nop);

                    compiler.Emit(OpCodes.Ldarg_2);
                    compiler.Emit(OpCodes.Ldc_I4, index);

                    compiler.Emit(OpCodes.Ldelem_Ref);
                    compiler.Emit(OpCodes.Ldarg_1);
                    compiler.Emit(OpCodes.Callvirt, DOUBLE_VAL_METHOD);
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
                    throw new ParseException("Unknown primary alternative", context.Start.StartIndex);
                }
            }

            public override void EnterNumeric(JavascriptParser.NumericContext context)
            {
                string text = context.GetText();

                if (context.HEX() is not null)
                {
                    compiler.PushInt64(Convert.ToInt64(text, 16));
                }
                else if (context.OCTAL() is not null)
                {
                    compiler.PushInt64(Convert.ToInt64(text, 8));
                }
                else
                {
                    compiler.Emit(OpCodes.Ldc_R8, double.Parse(text, CultureInfo.InvariantCulture));
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
                    throw new ParseException("Unknown postfix alternative", context.Start.StartIndex);
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
                    EnterUnary(context.unary());
                    compiler.Emit(OpCodes.Conv_R8);
                }
                else if (context.unary_operator() is { } unaryOperator)
                {
                    EnterUnary(context.unary());

                    if (unaryOperator.AT_SUBTRACT() is not null)
                    {
                        compiler.Emit(OpCodes.Neg);
                    }
                    else if (unaryOperator.AT_BIT_NOT() is not null)
                    {
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                        compiler.Emit(OpCodes.Not);
                        compiler.Emit(OpCodes.Conv_R8);
                    }
                    else if (unaryOperator.AT_BOOL_NOT() is not null)
                    {
                        compiler.Emit(OpCodes.Ldc_I4_0);
                        compiler.Emit(OpCodes.Ceq);
                        compiler.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        throw new ParseException("Unknown unary_operator alternative", context.Start.StartIndex);
                    }
                }
                else
                {
                    throw new ParseException("Unknown unary alternative", context.Start.StartIndex);
                }
            }

            public override void EnterAdditive(JavascriptParser.AdditiveContext context)
            {
                CompileBinary(context, context.multiplicative, EnterMultiplicative, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_ADD)
                    {
                        compiler.PushOpWithConvert(OpCodes.Add);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_SUBTRACT)
                    {
                        compiler.PushOpWithConvert(OpCodes.Sub);
                    }
                    else
                    {
                        throw new ParseException("Unknown additive token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterMultiplicative(JavascriptParser.MultiplicativeContext context)
            {
                CompileBinary(context, context.unary, EnterUnary, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_MULTIPLY)
                    {
                        compiler.PushOpWithConvert(OpCodes.Mul);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_DIVIDE)
                    {
                        compiler.PushOpWithConvert(OpCodes.Div);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_MODULO)
                    {
                        compiler.PushOpWithConvert(OpCodes.Rem);
                    }
                    else
                    {
                        throw new ParseException("Unknown multiplicative token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterShift(JavascriptParser.ShiftContext context)
            {
                EnterAdditive(context.additive(0));

                if (context.children.Count == 1) // if we don't have a shift token
                {
                    return;
                }

                compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                int argIndex = 1;

                for (int i = 1; i < context.children.Count; i += 2)
                {
                    if (context.children[i] is ITerminalNode terminalNode)
                    {
                        EnterAdditive(context.additive(argIndex++));
                        compiler.Emit(OpCodes.Conv_I4); // cast to int (truncate)

                        // mask off 63 to prevent overflow (fixes issue on x86 .NET Framework, #1034)
                        compiler.Emit(OpCodes.Ldc_I4, 0x3F);
                        compiler.Emit(OpCodes.And);

                        if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_SHL)
                        {
                            compiler.PushOpWithConvert(OpCodes.Shl);
                        }
                        else if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_SHR)
                        {
                            compiler.PushOpWithConvert(OpCodes.Shr);
                        }
                        else if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_SHU)
                        {
                            compiler.PushOpWithConvert(OpCodes.Shr_Un);
                        }
                        else
                        {
                            throw new ParseException("Unknown shift token", context.Start.StartIndex);
                        }
                    }
                    else
                    {
                        throw new ParseException("Unexpected child", context.Start.StartIndex);
                    }
                }
            }

            public override void EnterRelational(JavascriptParser.RelationalContext context)
            {
                CompileBinary(context, context.shift, EnterShift, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_LT)
                    {
                        compiler.PushOpWithConvert(OpCodes.Clt);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_GT)
                    {
                        compiler.PushOpWithConvert(OpCodes.Cgt);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_LTE)
                    {
                        compiler.PushCondEq(OpCodes.Cgt);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_GTE)
                    {
                        compiler.PushCondEq(OpCodes.Clt);
                    }
                    else
                    {
                        throw new ParseException("Unknown relational token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterEquality(JavascriptParser.EqualityContext context)
            {
                CompileBinary(context, context.relational, EnterRelational, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_EQ)
                    {
                        compiler.PushOpWithConvert(OpCodes.Ceq);
                    }
                    else if (terminalNode.Symbol.Type == JavascriptParser.AT_COMP_NEQ)
                    {
                        compiler.PushCondEq(OpCodes.Ceq);
                    }
                    else
                    {
                        throw new ParseException("Unknown equality token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterBitwise_and(JavascriptParser.Bitwise_andContext context)
            {
                CompileBinary(context, context.equality, equalityContext =>
                {
                    EnterEquality(equalityContext);

                    if (context.children.Count > 1) // if we have a bitwise token
                    {
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                    }
                }, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_AND)
                    {
                        compiler.PushOpWithConvert(OpCodes.And);
                    }
                    else
                    {
                        throw new ParseException("Unknown bitwise_and token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterBitwise_xor(JavascriptParser.Bitwise_xorContext context)
            {
                CompileBinary(context, context.bitwise_and, andContext =>
                {
                    EnterBitwise_and(andContext);

                    if (context.children.Count > 1) // if we have a bitwise token
                    {
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                    }
                }, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_XOR)
                    {
                        compiler.PushOpWithConvert(OpCodes.Xor);
                    }
                    else
                    {
                        throw new ParseException("Unknown bitwise_xor token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterBitwise_or(JavascriptParser.Bitwise_orContext context)
            {
                CompileBinary(context, context.bitwise_xor, xorContext =>
                {
                    EnterBitwise_xor(xorContext);

                    if (context.children.Count > 1) // if we have a bitwise token
                    {
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                    }
                }, terminalNode =>
                {
                    if (terminalNode.Symbol.Type == JavascriptParser.AT_BIT_OR)
                    {
                        compiler.PushOpWithConvert(OpCodes.Or);
                    }
                    else
                    {
                        throw new ParseException("Unknown bitwise_or token", context.Start.StartIndex);
                    }
                });
            }

            public override void EnterLogical_and(JavascriptParser.Logical_andContext context)
            {
                if (context.children.Count == 1)
                {
                    EnterBitwise_or(context.bitwise_or(0));
                }
                else
                {
                    // Evaluate the first operand and check if it is false
                    EnterBitwise_or(context.bitwise_or(0));
                    compiler.Emit(OpCodes.Ldc_I4_0);
                    compiler.Emit(OpCodes.Ceq);

                    // Iterate over the remaining operands
                    for (int i = 2; i < context.children.Count; i += 2)
                    {
                        if (context.children[i] is not JavascriptParser.Bitwise_orContext bitwiseOrContext)
                        {
                            throw new ParseException("Unexpected child of logical_and", context.Start.StartIndex);
                        }

                        // Evaluate the next operand and check if it is false
                        EnterBitwise_or(bitwiseOrContext);
                        compiler.Emit(OpCodes.Ldc_I4_0);
                        compiler.Emit(OpCodes.Ceq);

                        // Combine the results using OR
                        compiler.Emit(OpCodes.Or);
                    }

                    // Check if the combined result is false
                    compiler.Emit(OpCodes.Ldc_I4_0);
                    compiler.Emit(OpCodes.Ceq);

                    // Convert the result to a double
                    compiler.Emit(OpCodes.Conv_R8);
                }
            }

            public override void EnterLogical_or(JavascriptParser.Logical_orContext context)
            {
                if (context.children.Count == 1)
                {
                    EnterLogical_and(context.logical_and(0));
                }
                else
                {
                    // Evaluate the first operand and check if it is true
                    EnterLogical_and(context.logical_and(0));
                    compiler.Emit(OpCodes.Ldc_I4_0);
                    compiler.Emit(OpCodes.Ceq);
                    compiler.Emit(OpCodes.Ldc_I4_1);
                    compiler.Emit(OpCodes.Xor);

                    // Iterate over the remaining operands
                    for (int i = 2; i < context.children.Count; i += 2)
                    {
                        if (context.children[i] is not JavascriptParser.Logical_andContext logicalAndContext)
                        {
                            throw new ParseException("Unexpected child of logical_or", context.Start.StartIndex);
                        }

                        // Evaluate the next operand and check if it is true
                        EnterLogical_and(logicalAndContext);
                        compiler.Emit(OpCodes.Ldc_I4_0);
                        compiler.Emit(OpCodes.Ceq);
                        compiler.Emit(OpCodes.Ldc_I4_1);
                        compiler.Emit(OpCodes.Xor);

                        // Combine the results using OR
                        compiler.Emit(OpCodes.Or);
                    }

                    // Check if the combined result is true
                    compiler.Emit(OpCodes.Ldc_I4_1);
                    compiler.Emit(OpCodes.Ceq);

                    // Convert the result to a double
                    compiler.Emit(OpCodes.Conv_R8);
                }
            }

            public override void EnterConditional(JavascriptParser.ConditionalContext context)
            {
                if (context.children.Count == 1)
                {
                    EnterLogical_or(context.logical_or());
                }
                else
                {
                    Label condFalse = compiler.gen.DefineLabel();
                    Label condEnd = compiler.gen.DefineLabel();

                    // Evaluate the condition
                    EnterLogical_or(context.logical_or());
                    compiler.Emit(OpCodes.Ldc_I4_0);
                    compiler.Emit(OpCodes.Beq, condFalse);

                    // Evaluate the true branch
                    EnterConditional(context.conditional(0));
                    compiler.Emit(OpCodes.Br_S, condEnd);

                    // Evaluate the false branch
                    compiler.gen.MarkLabel(condFalse);
                    EnterConditional(context.conditional(1));

                    // Mark the end of the conditional
                    compiler.gen.MarkLabel(condEnd);
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
                        throw new ParseException("Unexpected child", context.Start.StartIndex);
                    }
                }
            }
        }

        private void PushCondEq(OpCode opCode)
        {
            Emit(opCode);
            Emit(OpCodes.Ldc_I4_1);
            Emit(OpCodes.Xor);
            Emit(OpCodes.Conv_R8);
        }

        private void PushOpWithConvert(OpCode opCode)
        {
            Emit(opCode);
            Emit(OpCodes.Conv_R8);
        }

        /// <summary>
        /// NOTE: This was pushLong() in Lucene
        /// </summary>
        private void PushInt64(long i)
        {
            Emit(OpCodes.Ldc_I8, i);
            if (!sourceText.Contains("<<"))
            {
                Emit(OpCodes.Conv_R8);
            }
        }

        // LUCENENET-specific - wrapping Emit methods which is helpful for debugging
        private void Emit(OpCode opcode)
        {
            // Console.WriteLine(opcode);
            gen.Emit(opcode);
        }

        private void Emit(OpCode opcode, Label label)
        {
            // Console.WriteLine(opcode + " " + label);
            gen.Emit(opcode, label);
        }

        private void Emit(OpCode opcode, double arg)
        {
            // Console.WriteLine(opcode + " " + arg);
            gen.Emit(opcode, arg);
        }

        private void Emit(OpCode opcode, long arg)
        {
            // Console.WriteLine(opcode + " " + arg);
            gen.Emit(opcode, arg);
        }

        private void Emit(OpCode opcode, MethodInfo arg)
        {
            // Console.WriteLine(opcode + " " + arg);
            gen.Emit(opcode, arg);
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
                parser.ErrorHandler = new ParseExceptionErrorStrategy(); // LUCENENET-specific
                return parser.expression();
            }
            catch (RecognitionException re)
            {
                throw new ArgumentException(re.Message, re);
            }
        }

        // LUCENENET-specific: Throw a ParseException in the event of parsing errors
        private class ParseExceptionErrorStrategy : DefaultErrorStrategy
        {
            public override void Recover(Parser recognizer, RecognitionException e)
            {
                int errorOffset = -1;

                for (ParserRuleContext parserRuleContext = recognizer.Context; parserRuleContext != null; parserRuleContext = (ParserRuleContext)parserRuleContext.Parent)
                {
                    if (errorOffset < 0)
                    {
                        errorOffset = parserRuleContext.Start.StartIndex;
                    }

                    parserRuleContext.exception = e;
                }

                throw new ParseException(e.Message, errorOffset);
            }

            public override IToken RecoverInline(Parser recognizer)
            {
                InputMismatchException cause = new InputMismatchException(recognizer);
                int errorOffset = -1;

                for (ParserRuleContext parserRuleContext = recognizer.Context; parserRuleContext != null; parserRuleContext = (ParserRuleContext)parserRuleContext.Parent)
                {
                    if (errorOffset < 0)
                    {
                        errorOffset = parserRuleContext.Start.StartIndex;
                    }

                    parserRuleContext.exception = cause;
                }

                throw new ParseException(cause.Message, errorOffset);
            }

            public override void Sync(Parser recognizer)
            {
            }
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
