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
using System.Runtime.CompilerServices;
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

        // LUCENENET specific: OrderedDictionary<TKey, TValue> is a replacement for LinkedHashMap<K, V> in the JDK
        private readonly IDictionary<string, int> externalsMap = new JCG.OrderedDictionary<string, int>();

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
                JavascriptNode tree = JavascriptExpressionParser.Parse(sourceText);
                BeginCompile();
                new JavascriptNodeCompiler(this).Compile(tree);
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

        // LUCENENET specific: compiles the tree built by JavascriptExpressionParser, emitting
        // the same IL as the former ANTLR listener did for each rule of the grammar (see
        // Lucene.Net.Expressions.Benchmarks/Antlr/AntlrJavascriptCompiler.cs).
        private sealed class JavascriptNodeCompiler
        {
            private readonly JavascriptCompiler compiler;

            public JavascriptNodeCompiler(JavascriptCompiler compiler)
            {
                this.compiler = compiler;
            }

            public void Compile(JavascriptNode node)
            {
                // The parser bounds the nesting it recurses on, and the chains it reads in a
                // loop are compiled in a loop: this only turns a deeper tree into an exception
                // rather than a stack overflow, which would terminate the process.
                RuntimeHelpers.EnsureSufficientExecutionStack();

                switch (node)
                {
                    case ConditionalNode conditional:
                        CompileConditional(conditional);
                        break;
                    case ChainNode chain:
                        CompileChain(chain);
                        break;
                    case UnaryNode unary:
                        CompileUnary(unary);
                        break;
                    case CallNode call:
                        CompileCall(call);
                        break;
                    case VariableNode variable:
                        CompileVariable(variable);
                        break;
                    case NumberNode number:
                        CompileNumber(number);
                        break;
                    default:
                        throw IllegalStateException.Create("Unknown node " + node);
                }
            }

            private void CompileCall(CallNode node)
            {
                string call = node.Name;
                JavascriptNode[] arguments = node.Arguments;
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
                for (int argument = 0; argument < argumentCount; ++argument)
                {
                    Compile(arguments[argument]);
                }
                compiler.Emit(OpCodes.Call, method);
            }

            private void CompileVariable(VariableNode node)
            {
                string text = node.Name;

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

            private void CompileNumber(NumberNode node)
            {
                string text = node.Text;

                switch (node.Kind)
                {
                    case NumberKind.Hex:
                        compiler.PushInt64(Convert.ToInt64(text, 16));
                        break;
                    case NumberKind.Octal:
                        compiler.PushInt64(Convert.ToInt64(text, 8));
                        break;
                    default:
                        compiler.Emit(OpCodes.Ldc_R8, double.Parse(text, CultureInfo.InvariantCulture));
                        break;
                }
            }

            private void CompileUnary(UnaryNode node)
            {
                if (node.Operand is not UnaryNode)
                {
                    Compile(node.Operand);
                    EmitUnaryOperator(node.Operator);
                    return;
                }

                // The parser reads a chain of unary operators such as "---1" in a loop, so it
                // can be longer than the stack allows to recurse: compile it in a loop too,
                // the operand first and then the operators from the innermost one.
                var chain = new List<UnaryOperator>();
                JavascriptNode operand = node;
                while (operand is UnaryNode unary)
                {
                    chain.Add(unary.Operator);
                    operand = unary.Operand;
                }

                Compile(operand);

                for (int i = chain.Count - 1; i >= 0; i--)
                {
                    EmitUnaryOperator(chain[i]);
                }
            }

            private void EmitUnaryOperator(UnaryOperator op)
            {
                switch (op)
                {
                    case UnaryOperator.Plus:
                        compiler.Emit(OpCodes.Conv_R8);
                        break;
                    case UnaryOperator.Negate:
                        compiler.Emit(OpCodes.Neg);
                        break;
                    case UnaryOperator.BitNot:
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                        compiler.Emit(OpCodes.Not);
                        compiler.Emit(OpCodes.Conv_R8);
                        break;
                    case UnaryOperator.BoolNot:
                        compiler.Emit(OpCodes.Ldc_I4_0);
                        compiler.Emit(OpCodes.Ceq);
                        compiler.Emit(OpCodes.Conv_R8);
                        break;
                }
            }

            private void CompileChain(ChainNode node)
            {
                JavascriptNode[] operands = node.Operands;
                BinaryOperator[] operators = node.Operators;

                switch (node.Level)
                {
                    case ChainLevel.LogicalOr:
                        // Evaluate each operand and check if it is true, combining the results using OR
                        for (int i = 0; i < operands.Length; i++)
                        {
                            Compile(operands[i]);
                            compiler.Emit(OpCodes.Ldc_I4_0);
                            compiler.Emit(OpCodes.Ceq);
                            compiler.Emit(OpCodes.Ldc_I4_1);
                            compiler.Emit(OpCodes.Xor);
                            if (i > 0)
                            {
                                compiler.Emit(OpCodes.Or);
                            }
                        }

                        // Check if the combined result is true and convert it to a double
                        compiler.Emit(OpCodes.Ldc_I4_1);
                        compiler.Emit(OpCodes.Ceq);
                        compiler.Emit(OpCodes.Conv_R8);
                        break;

                    case ChainLevel.LogicalAnd:
                        // Evaluate each operand and check if it is false, combining the results using OR
                        for (int i = 0; i < operands.Length; i++)
                        {
                            Compile(operands[i]);
                            compiler.Emit(OpCodes.Ldc_I4_0);
                            compiler.Emit(OpCodes.Ceq);
                            if (i > 0)
                            {
                                compiler.Emit(OpCodes.Or);
                            }
                        }

                        // Check if the combined result is false and convert it to a double
                        compiler.Emit(OpCodes.Ldc_I4_0);
                        compiler.Emit(OpCodes.Ceq);
                        compiler.Emit(OpCodes.Conv_R8);
                        break;

                    case ChainLevel.BitwiseOr:
                    case ChainLevel.BitwiseXor:
                    case ChainLevel.BitwiseAnd:
                        Compile(operands[0]);
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                        for (int i = 0; i < operators.Length; i++)
                        {
                            Compile(operands[i + 1]);
                            compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                            compiler.PushOpWithConvert(GetOpCode(operators[i]));
                        }
                        break;

                    case ChainLevel.Shift:
                        Compile(operands[0]);
                        compiler.Emit(OpCodes.Conv_I8); // cast to long (truncate)
                        for (int i = 0; i < operators.Length; i++)
                        {
                            Compile(operands[i + 1]);
                            compiler.Emit(OpCodes.Conv_I4); // cast to int (truncate)

                            // mask off 63 to prevent overflow (fixes issue on x86 .NET Framework, #1034)
                            compiler.Emit(OpCodes.Ldc_I4, 0x3F);
                            compiler.Emit(OpCodes.And);

                            compiler.PushOpWithConvert(GetOpCode(operators[i]));
                        }
                        break;

                    default:
                        Compile(operands[0]);
                        for (int i = 0; i < operators.Length; i++)
                        {
                            Compile(operands[i + 1]);
                            switch (operators[i])
                            {
                                case BinaryOperator.LessThanOrEqual:
                                    compiler.PushCondEq(OpCodes.Cgt);
                                    break;
                                case BinaryOperator.GreaterThanOrEqual:
                                    compiler.PushCondEq(OpCodes.Clt);
                                    break;
                                case BinaryOperator.NotEqual:
                                    compiler.PushCondEq(OpCodes.Ceq);
                                    break;
                                default:
                                    compiler.PushOpWithConvert(GetOpCode(operators[i]));
                                    break;
                            }
                        }
                        break;
                }
            }

            private static OpCode GetOpCode(BinaryOperator op)
            {
                return op switch
                {
                    BinaryOperator.BitOr => OpCodes.Or,
                    BinaryOperator.BitXor => OpCodes.Xor,
                    BinaryOperator.BitAnd => OpCodes.And,
                    BinaryOperator.Equal => OpCodes.Ceq,
                    BinaryOperator.LessThan => OpCodes.Clt,
                    BinaryOperator.GreaterThan => OpCodes.Cgt,
                    BinaryOperator.ShiftLeft => OpCodes.Shl,
                    BinaryOperator.ShiftRight => OpCodes.Shr,
                    BinaryOperator.ShiftRightUnsigned => OpCodes.Shr_Un,
                    BinaryOperator.Add => OpCodes.Add,
                    BinaryOperator.Subtract => OpCodes.Sub,
                    BinaryOperator.Multiply => OpCodes.Mul,
                    BinaryOperator.Divide => OpCodes.Div,
                    BinaryOperator.Modulo => OpCodes.Rem,
                    _ => throw IllegalStateException.Create("Unexpected operator " + op),
                };
            }

            private void CompileConditional(ConditionalNode node)
            {
                // The parser reads the false branches of "a ? b : c ? d : e" in a loop, so
                // they can nest deeper than the stack allows to recurse: compile them in a
                // loop too, and mark the ends of the conditionals from the innermost one.
                List<Label> ends = null;
                JavascriptNode whenFalse = node;
                while (whenFalse is ConditionalNode conditional)
                {
                    Label condFalse = compiler.gen.DefineLabel();
                    Label condEnd = compiler.gen.DefineLabel();

                    // Evaluate the condition
                    Compile(conditional.Condition);
                    compiler.Emit(OpCodes.Ldc_I4_0);
                    compiler.Emit(OpCodes.Beq, condFalse);

                    // Evaluate the true branch
                    Compile(conditional.WhenTrue);
                    compiler.Emit(OpCodes.Br_S, condEnd);

                    // Evaluate the false branch
                    compiler.gen.MarkLabel(condFalse);
                    (ends ??= new List<Label>()).Add(condEnd);
                    whenFalse = conditional.WhenFalse;
                }

                Compile(whenFalse);

                // Mark the ends of the conditionals
                for (int i = ends.Count - 1; i >= 0; i--)
                {
                    compiler.gen.MarkLabel(ends[i]);
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
            IDictionary<string, MethodInfo> map = new JCG.Dictionary<string, MethodInfo>();
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
            var settings = new JCG.Dictionary<string, string>();
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
