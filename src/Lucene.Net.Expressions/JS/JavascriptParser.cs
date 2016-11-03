using System;
using Antlr.Runtime;
using Antlr.Runtime.Tree;


namespace Lucene.Net.Expressions.JS
{
    internal class JavascriptParser : Parser
    {
        public static readonly string[] tokenNames =
		{ "<invalid>", "<EOR>", 
		    "<DOWN>", "<UP>", "AT_ADD", "AT_BIT_AND", "AT_BIT_NOT", "AT_BIT_OR", "AT_BIT_SHL"
		    , "AT_BIT_SHR", "AT_BIT_SHU", "AT_BIT_XOR", "AT_BOOL_AND", "AT_BOOL_NOT", "AT_BOOL_OR"
		    , "AT_CALL", "AT_COLON", "AT_COMMA", "AT_COMP_EQ", "AT_COMP_GT", "AT_COMP_GTE", 
		    "AT_COMP_LT", "AT_COMP_LTE", "AT_COMP_NEQ", "AT_COND_QUE", "AT_DIVIDE", "AT_DOT"
		    , "AT_LPAREN", "AT_MODULO", "AT_MULTIPLY", "AT_NEGATE", "AT_RPAREN", "AT_SUBTRACT"
		    , "DECIMAL", "DECIMALDIGIT", "DECIMALINTEGER", "EXPONENT", "HEX", "HEXDIGIT", "ID"
		    , "NAMESPACE_ID", "OCTAL", "OCTALDIGIT", "WS" };

        public const int EOF = -1;

        public const int AT_ADD = 4;

        public const int AT_BIT_AND = 5;

        public const int AT_BIT_NOT = 6;

        public const int AT_BIT_OR = 7;

        public const int AT_BIT_SHL = 8;

        public const int AT_BIT_SHR = 9;

        public const int AT_BIT_SHU = 10;

        public const int AT_BIT_XOR = 11;

        public const int AT_BOOL_AND = 12;

        public const int AT_BOOL_NOT = 13;

        public const int AT_BOOL_OR = 14;

        public const int AT_CALL = 15;

        public const int AT_COLON = 16;

        public const int AT_COMMA = 17;

        public const int AT_COMP_EQ = 18;

        public const int AT_COMP_GT = 19;

        public const int AT_COMP_GTE = 20;

        public const int AT_COMP_LT = 21;

        public const int AT_COMP_LTE = 22;

        public const int AT_COMP_NEQ = 23;

        public const int AT_COND_QUE = 24;

        public const int AT_DIVIDE = 25;

        public const int AT_DOT = 26;

        public const int AT_LPAREN = 27;

        public const int AT_MODULO = 28;

        public const int AT_MULTIPLY = 29;

        public const int AT_NEGATE = 30;

        public const int AT_RPAREN = 31;

        public const int AT_SUBTRACT = 32;

        public const int DECIMAL = 33;

        public const int DECIMALDIGIT = 34;

        public const int DECIMALINTEGER = 35;

        public const int EXPONENT = 36;

        public const int HEX = 37;

        public const int HEXDIGIT = 38;

        public const int ID = 39;

        public const int NAMESPACE_ID = 40;

        public const int OCTAL = 41;

        public const int OCTALDIGIT = 42;

        public const int WS = 43;

        // ANTLR GENERATED CODE: DO NOT EDIT
        // delegates
        public virtual Parser[] GetDelegates()
        {
            return new Parser[] { };
        }

        public JavascriptParser(CommonTokenStream input)
            : this(input, new RecognizerSharedState())
        {
        }

        public JavascriptParser(CommonTokenStream input, RecognizerSharedState state)
            : base(input, state)
        {
        }

        protected internal ITreeAdaptor adaptor = new CommonTreeAdaptor();

        public virtual ITreeAdaptor TreeAdaptor
        {
            get { return adaptor; }
            set { adaptor = value; }
        }

        // delegators


        public override string[] TokenNames
        {
            get { return tokenNames; }
        }

        public override string GrammarFileName
        {
            get { return "src/java/org/apache/lucene/expressions/js/Javascript.g"; }
        }

        public override void DisplayRecognitionError(string[] tokenNames, RecognitionException re)
        {
            string message;
            if (re.Token == null)
            {
                message = " unknown error (missing token).";
            }
            else
            {
                if (re is UnwantedTokenException)
                {
                    message = " extraneous " + GetReadableTokenString(re.Token) + " at position (" + re.CharPositionInLine + ").";
                }
                else
                {
                    if (re is MissingTokenException)
                    {
                        message = " missing " + GetReadableTokenString(re.Token) + " at position (" + re.CharPositionInLine + ").";
                    }
                    else
                    {
                        if (re is NoViableAltException)
                        {
                            switch (re.Token.Type)
                            {
                                case EOF:
                                    {
                                        message = " unexpected end of expression.";
                                        break;
                                    }

                                default:
                                    {
                                        message = " invalid sequence of tokens near " + GetReadableTokenString(re.Token)
                                            + " at position (" + re.CharPositionInLine + ").";
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            message = " unexpected token " + GetReadableTokenString(re.Token) + " at position ("
                                 + re.CharPositionInLine + ").";
                        }
                    }
                }
            }
            //ParseException parseException = new ParseException(message, re.CharPositionInLine);

            throw new InvalidOperationException(message);
        }

        public static string GetReadableTokenString(IToken token)
        {
            if (token == null)
            {
                return "unknown token";
            }
            switch (token.Type)
            {
                case AT_LPAREN:
                    {
                        return "open parenthesis '('";
                    }

                case AT_RPAREN:
                    {
                        return "close parenthesis ')'";
                    }

                case AT_COMP_LT:
                    {
                        return "less than '<'";
                    }

                case AT_COMP_LTE:
                    {
                        return "less than or equal '<='";
                    }

                case AT_COMP_GT:
                    {
                        return "greater than '>'";
                    }

                case AT_COMP_GTE:
                    {
                        return "greater than or equal '>='";
                    }

                case AT_COMP_EQ:
                    {
                        return "equal '=='";
                    }

                case AT_NEGATE:
                    {
                        return "negate '!='";
                    }

                case AT_BOOL_NOT:
                    {
                        return "boolean not '!'";
                    }

                case AT_BOOL_AND:
                    {
                        return "boolean and '&&'";
                    }

                case AT_BOOL_OR:
                    {
                        return "boolean or '||'";
                    }

                case AT_COND_QUE:
                    {
                        return "conditional '?'";
                    }

                case AT_ADD:
                    {
                        return "addition '+'";
                    }

                case AT_SUBTRACT:
                    {
                        return "subtraction '-'";
                    }

                case AT_MULTIPLY:
                    {
                        return "multiplication '*'";
                    }

                case AT_DIVIDE:
                    {
                        return "division '/'";
                    }

                case AT_MODULO:
                    {
                        return "modulo '%'";
                    }

                case AT_BIT_SHL:
                    {
                        return "bit shift left '<<'";
                    }

                case AT_BIT_SHR:
                    {
                        return "bit shift right '>>'";
                    }

                case AT_BIT_SHU:
                    {
                        return "unsigned bit shift right '>>>'";
                    }

                case AT_BIT_AND:
                    {
                        return "bitwise and '&'";
                    }

                case AT_BIT_OR:
                    {
                        return "bitwise or '|'";
                    }

                case AT_BIT_XOR:
                    {
                        return "bitwise xor '^'";
                    }

                case AT_BIT_NOT:
                    {
                        return "bitwise not '~'";
                    }

                case ID:
                    {
                        return "identifier '" + token.Text + "'";
                    }

                case DECIMAL:
                    {
                        return "decimal '" + token.Text + "'";
                    }

                case OCTAL:
                    {
                        return "octal '" + token.Text + "'";
                    }

                case HEX:
                    {
                        return "hex '" + token.Text + "'";
                    }

                case EOF:
                    {
                        return "end of expression";
                    }

                default:
                    {
                        return "'" + token.Text + "'";
                        break;
                    }
            }
        }

        internal class ExpressionReturn<TToken> : ParserRuleReturnScope<TToken>
        {

        }

        // $ANTLR start "expression"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:250:1: expression : conditional EOF !;

        public AstParserRuleReturnScope<ITree, IToken> Expression()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root = null;
            IToken EOF2 = null;
            AstParserRuleReturnScope<ITree, IToken> conditional1;
            CommonTree EOF2_tree = null;
            try
            {
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:251:5: ( conditional EOF !)
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:251:7: conditional EOF !
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_conditional_in_expression737);
                    conditional1 = Conditional();
                    state._fsp--;
                    adaptor.AddChild(root, conditional1.Tree);
                    EOF2 = (IToken)Match(input, EOF, FOLLOW_EOF_in_expression739);
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re);
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "conditional"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:254:1: conditional : logical_or ( AT_COND_QUE ^ conditional AT_COLON ! conditional )? ;
        /// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
        public AstParserRuleReturnScope<ITree, IToken> Conditional()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root_0;
            IToken AT_COND_QUE4;
            IToken AT_COLON6 = null;
            AstParserRuleReturnScope<ITree, IToken> logical_or3;
            AstParserRuleReturnScope<ITree, IToken> conditional5;
            AstParserRuleReturnScope<ITree, IToken> conditional7;
            CommonTree AT_COND_QUE4_tree;
            CommonTree AT_COLON6_tree = null;
            try
            {
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:255:5: ( logical_or ( AT_COND_QUE ^ conditional AT_COLON ! conditional )? )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:255:7: logical_or ( AT_COND_QUE ^ conditional AT_COLON ! conditional )?
                    root_0 = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_logical_or_in_conditional757);
                    logical_or3 = Logical_or();
                    state._fsp--;
                    adaptor.AddChild(root_0, logical_or3.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:255:18: ( AT_COND_QUE ^ conditional AT_COLON ! conditional )?
                    int alt1 = 2;
                    int LA1_0 = input.LA(1);
                    if ((LA1_0 == AT_COND_QUE))
                    {
                        alt1 = 1;
                    }
                    switch (alt1)
                    {
                        case 1:
                            {
                                // src/java/org/apache/lucene/expressions/js/Javascript.g:255:19: AT_COND_QUE ^ conditional AT_COLON ! conditional
                                AT_COND_QUE4 = (IToken)Match(input, AT_COND_QUE, FOLLOW_AT_COND_QUE_in_conditional760);
                                AT_COND_QUE4_tree = (CommonTree)adaptor.Create(AT_COND_QUE4);
                                root_0 = (CommonTree)adaptor.BecomeRoot(AT_COND_QUE4_tree, root_0);
                                PushFollow(FOLLOW_conditional_in_conditional763);
                                conditional5 = Conditional();
                                state._fsp--;
                                adaptor.AddChild(root_0, conditional5.Tree);
                                AT_COLON6 = (IToken)Match(input, AT_COLON, FOLLOW_AT_COLON_in_conditional765);
                                PushFollow(FOLLOW_conditional_in_conditional768);
                                conditional7 = Conditional();
                                state._fsp--;
                                adaptor.AddChild(root_0, conditional7.Tree);
                                break;
                            }
                    }
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root_0);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re);
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "logical_or"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:258:1: logical_or : logical_and ( AT_BOOL_OR ^ logical_and )* ;

        public AstParserRuleReturnScope<ITree, IToken> Logical_or()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root_0;
            IToken AT_BOOL_OR9;
            AstParserRuleReturnScope<ITree, IToken> logical_and8;
            AstParserRuleReturnScope<ITree, IToken> logical_and10;
            CommonTree AT_BOOL_OR9_tree;
            try
            {
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:259:5: ( logical_and ( AT_BOOL_OR ^ logical_and )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:259:7: logical_and ( AT_BOOL_OR ^ logical_and )*
                    root_0 = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_logical_and_in_logical_or787);
                    logical_and8 = Logical_and();
                    state._fsp--;
                    adaptor.AddChild(root_0, logical_and8.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:259:19: ( AT_BOOL_OR ^ logical_and )*
                    while (true)
                    {
                        int alt2 = 2;
                        int LA2_0 = input.LA(1);
                        if ((LA2_0 == AT_BOOL_OR))
                        {
                            alt2 = 1;
                        }
                        switch (alt2)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:259:20: AT_BOOL_OR ^ logical_and
                                    AT_BOOL_OR9 = (IToken)Match(input, AT_BOOL_OR, FOLLOW_AT_BOOL_OR_in_logical_or790);
                                    AT_BOOL_OR9_tree = (CommonTree)adaptor.Create(AT_BOOL_OR9);
                                    root_0 = (CommonTree)adaptor.BecomeRoot(AT_BOOL_OR9_tree, root_0);
                                    PushFollow(FOLLOW_logical_and_in_logical_or793);
                                    logical_and10 = Logical_and();
                                    state._fsp--;
                                    adaptor.AddChild(root_0, logical_and10.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop2_break;
                                    break;
                                }
                        }
                    loop2_continue: ;
                    }
                loop2_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root_0);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re);
            }
            // do for sure before leaving
            return retval;
        }



        // $ANTLR start "logical_and"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:262:1: logical_and : bitwise_or ( AT_BOOL_AND ^ bitwise_or )* ;

        public AstParserRuleReturnScope<ITree, IToken> Logical_and()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root;
            IToken AT_BOOL_AND12;
            AstParserRuleReturnScope<ITree, IToken> bitwise_or11;
            AstParserRuleReturnScope<ITree, IToken> bitwise_or13;
            CommonTree AT_BOOL_AND12_tree = null;
            try
            {
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:263:5: ( bitwise_or ( AT_BOOL_AND ^ bitwise_or )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:263:7: bitwise_or ( AT_BOOL_AND ^ bitwise_or )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_bitwise_or_in_logical_and812);
                    bitwise_or11 = Bitwise_or();
                    state._fsp--;
                    adaptor.AddChild(root, bitwise_or11.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:263:18: ( AT_BOOL_AND ^ bitwise_or )*
                    while (true)
                    {
                        int alt3 = 2;
                        int LA3_0 = input.LA(1);
                        if ((LA3_0 == AT_BOOL_AND))
                        {
                            alt3 = 1;
                        }
                        switch (alt3)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:263:19: AT_BOOL_AND ^ bitwise_or
                                    AT_BOOL_AND12 = (IToken)Match(input, AT_BOOL_AND, FOLLOW_AT_BOOL_AND_in_logical_and815);
                                    AT_BOOL_AND12_tree = (CommonTree)adaptor.Create(AT_BOOL_AND12);
                                    root = (CommonTree)adaptor.BecomeRoot(AT_BOOL_AND12_tree, root);
                                    PushFollow(FOLLOW_bitwise_or_in_logical_and818);
                                    bitwise_or13 = Bitwise_or();
                                    state._fsp--;
                                    adaptor.AddChild(root, bitwise_or13.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop3_break;
                                    break;
                                }
                        }
                    loop3_continue: ;
                    }
                loop3_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "bitwise_or"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:266:1: bitwise_or : bitwise_xor ( AT_BIT_OR ^ bitwise_xor )* ;

        public AstParserRuleReturnScope<ITree, IToken> Bitwise_or()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:267:5: ( bitwise_xor ( AT_BIT_OR ^ bitwise_xor )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:267:7: bitwise_xor ( AT_BIT_OR ^ bitwise_xor )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_bitwise_xor_in_bitwise_or837);
                    AstParserRuleReturnScope<ITree, IToken> bitwise_xor14 = Bitwise_xor();
                    state._fsp--;
                    adaptor.AddChild(root, bitwise_xor14.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:267:19: ( AT_BIT_OR ^ bitwise_xor )*
                    while (true)
                    {
                        int alt4 = 2;
                        int LA4_0 = input.LA(1);
                        if ((LA4_0 == AT_BIT_OR))
                        {
                            alt4 = 1;
                        }
                        switch (alt4)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:267:20: AT_BIT_OR ^ bitwise_xor
                                    IToken AT_BIT_OR15 = (IToken)Match(input, AT_BIT_OR, FOLLOW_AT_BIT_OR_in_bitwise_or840);
                                    CommonTree AT_BIT_OR15_tree = (CommonTree)adaptor.Create(AT_BIT_OR15);
                                    root = (CommonTree)adaptor.BecomeRoot(AT_BIT_OR15_tree, root);
                                    PushFollow(FOLLOW_bitwise_xor_in_bitwise_or843);
                                    AstParserRuleReturnScope<ITree, IToken> bitwise_xor16 = Bitwise_xor();
                                    state._fsp--;
                                    adaptor.AddChild(root, bitwise_xor16.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop4_break;
                                    break;
                                }
                        }
                    loop4_continue: ;
                    }
                loop4_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re);
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "bitwise_xor"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:270:1: bitwise_xor : bitwise_and ( AT_BIT_XOR ^ bitwise_and )* ;

        public AstParserRuleReturnScope<ITree, IToken> Bitwise_xor()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:271:5: ( bitwise_and ( AT_BIT_XOR ^ bitwise_and )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:271:7: bitwise_and ( AT_BIT_XOR ^ bitwise_and )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_bitwise_and_in_bitwise_xor862);
                    AstParserRuleReturnScope<ITree, IToken> bitwise_and17 = Bitwise_and();
                    state._fsp--;
                    adaptor.AddChild(root, bitwise_and17.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:271:19: ( AT_BIT_XOR ^ bitwise_and )*
                    while (true)
                    {
                        int alt5 = 2;
                        int LA5_0 = input.LA(1);
                        if ((LA5_0 == AT_BIT_XOR))
                        {
                            alt5 = 1;
                        }
                        switch (alt5)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:271:20: AT_BIT_XOR ^ bitwise_and
                                    IToken AT_BIT_XOR18 = (IToken)Match(input, AT_BIT_XOR, FOLLOW_AT_BIT_XOR_in_bitwise_xor865
                                        );
                                    CommonTree AT_BIT_XOR18_tree = (CommonTree)adaptor.Create(AT_BIT_XOR18);
                                    root = (CommonTree)adaptor.BecomeRoot(AT_BIT_XOR18_tree, root);
                                    PushFollow(FOLLOW_bitwise_and_in_bitwise_xor868);
                                    AstParserRuleReturnScope<ITree, IToken> bitwise_and19 = Bitwise_and();
                                    state._fsp--;
                                    adaptor.AddChild(root, bitwise_and19.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop5_break;
                                    break;
                                }
                        }
                    loop5_continue: ;
                    }
                loop5_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "bitwise_and"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:274:1: bitwise_and : equality ( AT_BIT_AND ^ equality )* ;

        public AstParserRuleReturnScope<ITree, IToken> Bitwise_and()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:275:5: ( equality ( AT_BIT_AND ^ equality )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:275:8: equality ( AT_BIT_AND ^ equality )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_equality_in_bitwise_and888);
                    AstParserRuleReturnScope<ITree, IToken> equality20 = Equality();
                    state._fsp--;
                    adaptor.AddChild(root, equality20.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:275:17: ( AT_BIT_AND ^ equality )*
                    while (true)
                    {
                        int alt6 = 2;
                        int LA6_0 = input.LA(1);
                        if ((LA6_0 == AT_BIT_AND))
                        {
                            alt6 = 1;
                        }
                        switch (alt6)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:275:18: AT_BIT_AND ^ equality
                                    IToken AT_BIT_AND21 = (IToken)Match(input, AT_BIT_AND, FOLLOW_AT_BIT_AND_in_bitwise_and891
                                        );
                                    CommonTree AT_BIT_AND21_tree = (CommonTree)adaptor.Create(AT_BIT_AND21);
                                    root = (CommonTree)adaptor.BecomeRoot(AT_BIT_AND21_tree, root);
                                    PushFollow(FOLLOW_equality_in_bitwise_and894);
                                    AstParserRuleReturnScope<ITree, IToken> equality22 = Equality();
                                    state._fsp--;
                                    adaptor.AddChild(root, equality22.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop6_break;
                                    break;
                                }
                        }
                    loop6_continue: ;
                    }
                loop6_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re);
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "equality"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:278:1: equality : relational ( ( AT_COMP_EQ | AT_COMP_NEQ ) ^ relational )* ;

        public AstParserRuleReturnScope<ITree, IToken> Equality()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree set24_tree = null;
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:279:5: ( relational ( ( AT_COMP_EQ | AT_COMP_NEQ ) ^ relational )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:279:7: relational ( ( AT_COMP_EQ | AT_COMP_NEQ ) ^ relational )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_relational_in_equality913);
                    AstParserRuleReturnScope<ITree, IToken> relational23 = Relational();
                    state._fsp--;
                    adaptor.AddChild(root, relational23.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:279:18: ( ( AT_COMP_EQ | AT_COMP_NEQ ) ^ relational )*
                    while (true)
                    {
                        int alt7 = 2;
                        int LA7_0 = input.LA(1);
                        if ((LA7_0 == AT_COMP_EQ || LA7_0 == AT_COMP_NEQ))
                        {
                            alt7 = 1;
                        }
                        switch (alt7)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:279:19: ( AT_COMP_EQ | AT_COMP_NEQ ) ^ relational
                                    IToken set24 = input.LT(1);
                                    set24 = input.LT(1);
                                    if (input.LA(1) == AT_COMP_EQ || input.LA(1) == AT_COMP_NEQ)
                                    {
                                        input.Consume();
                                        root = (CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(set24), root
                                            );
                                        state.errorRecovery = false;
                                    }
                                    else
                                    {
                                        MismatchedSetException mse = new MismatchedSetException(null, input);
                                        throw mse;
                                    }
                                    PushFollow(FOLLOW_relational_in_equality925);
                                    AstParserRuleReturnScope<ITree, IToken> relational25 = Relational();
                                    state._fsp--;
                                    adaptor.AddChild(root, relational25.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop7_break;
                                    break;
                                }
                        }
                    loop7_continue: ;
                    }
                loop7_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "relational"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:282:1: relational : shift ( ( AT_COMP_LT | AT_COMP_GT | AT_COMP_LTE | AT_COMP_GTE ) ^ shift )* ;

        public AstParserRuleReturnScope<ITree, IToken> Relational()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree set27_tree = null;
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:283:5: ( shift ( ( AT_COMP_LT | AT_COMP_GT | AT_COMP_LTE | AT_COMP_GTE ) ^ shift )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:283:7: shift ( ( AT_COMP_LT | AT_COMP_GT | AT_COMP_LTE | AT_COMP_GTE ) ^ shift )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_shift_in_relational944);
                    AstParserRuleReturnScope<ITree, IToken> shift26 = Shift();
                    state._fsp--;
                    adaptor.AddChild(root, shift26.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:283:13: ( ( AT_COMP_LT | AT_COMP_GT | AT_COMP_LTE | AT_COMP_GTE ) ^ shift )*
                    while (true)
                    {
                        int alt8 = 2;
                        int LA8_0 = input.LA(1);
                        if (((LA8_0 >= AT_COMP_GT && LA8_0 <= AT_COMP_LTE)))
                        {
                            alt8 = 1;
                        }
                        switch (alt8)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:283:14: ( AT_COMP_LT | AT_COMP_GT | AT_COMP_LTE | AT_COMP_GTE ) ^ shift
                                    IToken set27 = input.LT(1);
                                    set27 = input.LT(1);
                                    if ((input.LA(1) >= AT_COMP_GT && input.LA(1) <= AT_COMP_LTE))
                                    {
                                        input.Consume();
                                        root = (CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(set27), root
                                            );
                                        state.errorRecovery = false;
                                    }
                                    else
                                    {
                                        MismatchedSetException mse = new MismatchedSetException(null, input);
                                        throw mse;
                                    }
                                    PushFollow(FOLLOW_shift_in_relational964);
                                    AstParserRuleReturnScope<ITree, IToken> shift28 = Shift();
                                    state._fsp--;
                                    adaptor.AddChild(root, shift28.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop8_break;
                                    break;
                                }
                        }
                    loop8_continue: ;
                    }
                loop8_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "shift"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:286:1: shift : additive ( ( AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU ) ^ additive )* ;

        public AstParserRuleReturnScope<ITree, IToken> Shift()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree set30_tree = null;
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:287:5: ( additive ( ( AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU ) ^ additive )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:287:7: additive ( ( AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU ) ^ additive )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_additive_in_shift983);
                    AstParserRuleReturnScope<ITree, IToken> additive29 = Additive();
                    state._fsp--;
                    adaptor.AddChild(root, additive29.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:287:16: ( ( AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU ) ^ additive )*
                    while (true)
                    {
                        int alt9 = 2;
                        int LA9_0 = input.LA(1);
                        if (((LA9_0 >= AT_BIT_SHL && LA9_0 <= AT_BIT_SHU)))
                        {
                            alt9 = 1;
                        }
                        switch (alt9)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:287:17: ( AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU ) ^ additive
                                    IToken set30 = input.LT(1);
                                    set30 = input.LT(1);
                                    if ((input.LA(1) >= AT_BIT_SHL && input.LA(1) <= AT_BIT_SHU))
                                    {
                                        input.Consume();
                                        root = (CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(set30), root
                                            );
                                        state.errorRecovery = false;
                                    }
                                    else
                                    {
                                        MismatchedSetException mse = new MismatchedSetException(null, input);
                                        throw mse;
                                    }
                                    PushFollow(FOLLOW_additive_in_shift999);
                                    AstParserRuleReturnScope<ITree, IToken> additive31 = Additive();
                                    state._fsp--;
                                    adaptor.AddChild(root, additive31.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop9_break;
                                    break;
                                }
                        }
                    loop9_continue: ;
                    }
                loop9_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "additive"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:290:1: additive : multiplicative ( ( AT_ADD | AT_SUBTRACT ) ^ multiplicative )* ;

        public AstParserRuleReturnScope<ITree, IToken> Additive()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree set33_tree = null;
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:291:5: ( multiplicative ( ( AT_ADD | AT_SUBTRACT ) ^ multiplicative )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:291:7: multiplicative ( ( AT_ADD | AT_SUBTRACT ) ^ multiplicative )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_multiplicative_in_additive1018);
                    AstParserRuleReturnScope<ITree, IToken> multiplicative32 = Multiplicative();
                    state._fsp--;
                    adaptor.AddChild(root, multiplicative32.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:291:22: ( ( AT_ADD | AT_SUBTRACT ) ^ multiplicative )*
                    while (true)
                    {
                        int alt10 = 2;
                        int LA10_0 = input.LA(1);
                        if ((LA10_0 == AT_ADD || LA10_0 == AT_SUBTRACT))
                        {
                            alt10 = 1;
                        }
                        switch (alt10)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:291:23: ( AT_ADD | AT_SUBTRACT ) ^ multiplicative
                                    IToken set33 = input.LT(1);
                                    set33 = input.LT(1);
                                    if (input.LA(1) == AT_ADD || input.LA(1) == AT_SUBTRACT)
                                    {
                                        input.Consume();
                                        root = (CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(set33), root
                                            );
                                        state.errorRecovery = false;
                                    }
                                    else
                                    {
                                        MismatchedSetException mse = new MismatchedSetException(null, input);
                                        throw mse;
                                    }
                                    PushFollow(FOLLOW_multiplicative_in_additive1030);
                                    AstParserRuleReturnScope<ITree, IToken> multiplicative34 = Multiplicative();
                                    state._fsp--;
                                    adaptor.AddChild(root, multiplicative34.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop10_break;
                                    break;
                                }
                        }
                    loop10_continue: ;
                    }
                loop10_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "multiplicative"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:294:1: multiplicative : unary ( ( AT_MULTIPLY | AT_DIVIDE | AT_MODULO ) ^ unary )* ;

        public AstParserRuleReturnScope<ITree, IToken> Multiplicative()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree set36_tree = null;
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:295:5: ( unary ( ( AT_MULTIPLY | AT_DIVIDE | AT_MODULO ) ^ unary )* )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:295:7: unary ( ( AT_MULTIPLY | AT_DIVIDE | AT_MODULO ) ^ unary )*
                    root = (CommonTree)adaptor.Nil();
                    PushFollow(FOLLOW_unary_in_multiplicative1049);
                    AstParserRuleReturnScope<ITree, IToken> unary35 = Unary();
                    state._fsp--;
                    adaptor.AddChild(root, unary35.Tree);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:295:13: ( ( AT_MULTIPLY | AT_DIVIDE | AT_MODULO ) ^ unary )*
                    while (true)
                    {
                        int alt11 = 2;
                        int LA11_0 = input.LA(1);
                        if ((LA11_0 == AT_DIVIDE || (LA11_0 >= AT_MODULO && LA11_0 <= AT_MULTIPLY)))
                        {
                            alt11 = 1;
                        }
                        switch (alt11)
                        {
                            case 1:
                                {
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:295:14: ( AT_MULTIPLY | AT_DIVIDE | AT_MODULO ) ^ unary
                                    IToken set36 = input.LT(1);
                                    set36 = input.LT(1);
                                    if (input.LA(1) == AT_DIVIDE || (input.LA(1) >= AT_MODULO && input.LA(1) <= AT_MULTIPLY
                                        ))
                                    {
                                        input.Consume();
                                        root = (CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(set36), root
                                            );
                                        state.errorRecovery = false;
                                    }
                                    else
                                    {
                                        MismatchedSetException mse = new MismatchedSetException(null, input);
                                        throw mse;
                                    }
                                    PushFollow(FOLLOW_unary_in_multiplicative1065);
                                    AstParserRuleReturnScope<ITree, IToken> unary37 = Unary();
                                    state._fsp--;
                                    adaptor.AddChild(root, unary37.Tree);
                                    break;
                                }

                            default:
                                {
                                    goto loop11_break;
                                    break;
                                }
                        }
                    loop11_continue: ;
                    }
                loop11_break: ;
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "unary"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:298:1: unary : ( postfix | AT_ADD ! unary | unary_operator ^ unary );

        public AstParserRuleReturnScope<ITree, IToken> Unary()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root = null;
            CommonTree AT_ADD39_tree = null;
            try
            {
                // src/java/org/apache/lucene/expressions/js/Javascript.g:299:5: ( postfix | AT_ADD ! unary | unary_operator ^ unary )
                int alt12 = 3;
                switch (input.LA(1))
                {
                    case AT_LPAREN:
                    case DECIMAL:
                    case HEX:
                    case NAMESPACE_ID:
                    case OCTAL:
                        {
                            alt12 = 1;
                            break;
                        }

                    case AT_ADD:
                        {
                            alt12 = 2;
                            break;
                        }

                    case AT_BIT_NOT:
                    case AT_BOOL_NOT:
                    case AT_SUBTRACT:
                        {
                            alt12 = 3;
                            break;
                        }

                    default:
                        {
                            NoViableAltException nvae = new NoViableAltException(string.Empty, 12, 0, input);
                            throw nvae;
                        }
                }
                switch (alt12)
                {
                    case 1:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:299:7: postfix
                            root = (CommonTree)adaptor.Nil();
                            PushFollow(FOLLOW_postfix_in_unary1084);
                            AstParserRuleReturnScope<ITree, IToken> postfix38 = Postfix();
                            state._fsp--;
                            adaptor.AddChild(root, postfix38.Tree);
                            break;
                        }

                    case 2:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:300:7: AT_ADD ! unary
                            root = (CommonTree)adaptor.Nil();
                            IToken AT_ADD39 = (IToken)Match(input, AT_ADD, FOLLOW_AT_ADD_in_unary1092);
                            PushFollow(FOLLOW_unary_in_unary1095);
                            AstParserRuleReturnScope<ITree, IToken> unary40 = Unary();
                            state._fsp--;
                            adaptor.AddChild(root, unary40.Tree);
                            break;
                        }

                    case 3:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:301:7: unary_operator ^ unary
                            root = (CommonTree)adaptor.Nil();
                            PushFollow(FOLLOW_unary_operator_in_unary1103);
                            AstParserRuleReturnScope<ITree, IToken> unary_operator41 = Unary_operator();
                            state._fsp--;
                            root = (CommonTree)adaptor.BecomeRoot(unary_operator41.Tree, root);
                            PushFollow(FOLLOW_unary_in_unary1106);
                            AstParserRuleReturnScope<ITree, IToken> unary42 = Unary();
                            state._fsp--;
                            adaptor.AddChild(root, unary42.Tree);
                            break;
                        }
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "unary_operator"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:304:1: unary_operator : ( AT_SUBTRACT -> AT_NEGATE | AT_BIT_NOT | AT_BOOL_NOT );

        public AstParserRuleReturnScope<ITree, IToken> Unary_operator()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root = null;
            CommonTree AT_SUBTRACT43_tree = null;
            RewriteRuleTokenStream stream_AT_SUBTRACT = new RewriteRuleTokenStream(adaptor, "token AT_SUBTRACT"
                );
            try
            {
                // src/java/org/apache/lucene/expressions/js/Javascript.g:305:5: ( AT_SUBTRACT -> AT_NEGATE | AT_BIT_NOT | AT_BOOL_NOT )
                int alt13 = 3;
                switch (input.LA(1))
                {
                    case AT_SUBTRACT:
                        {
                            alt13 = 1;
                            break;
                        }

                    case AT_BIT_NOT:
                        {
                            alt13 = 2;
                            break;
                        }

                    case AT_BOOL_NOT:
                        {
                            alt13 = 3;
                            break;
                        }

                    default:
                        {
                            NoViableAltException nvae = new NoViableAltException(string.Empty, 13, 0, input);
                            throw nvae;
                        }
                }
                switch (alt13)
                {
                    case 1:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:305:7: AT_SUBTRACT
                            IToken AT_SUBTRACT43 = (IToken)Match(input, AT_SUBTRACT, FOLLOW_AT_SUBTRACT_in_unary_operator1123
                                );
                            stream_AT_SUBTRACT.Add(AT_SUBTRACT43);
                            // AST REWRITE
                            // elements: 
                            // token labels: 
                            // rule labels: retval
                            // token list labels: 
                            // rule list labels: 
                            // wildcard labels: 
                            retval.Tree = root;
                            RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval"
                                , retval != null ? ((CommonTree)retval.Tree) : null);
                            root = (CommonTree)adaptor.Nil();
                            {
                                // 305:19: -> AT_NEGATE
                                adaptor.AddChild(root, (CommonTree)adaptor.Create(AT_NEGATE, "AT_NEGATE"));
                            }
                            retval.Tree = root;
                            break;
                        }

                    case 2:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:306:7: AT_BIT_NOT
                            root = (CommonTree)adaptor.Nil();
                            IToken AT_BIT_NOT44 = (IToken)Match(input, AT_BIT_NOT, FOLLOW_AT_BIT_NOT_in_unary_operator1135
                                );
                            CommonTree AT_BIT_NOT44_tree = (CommonTree)adaptor.Create(AT_BIT_NOT44);
                            adaptor.AddChild(root, AT_BIT_NOT44_tree);
                            break;
                        }

                    case 3:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:307:7: AT_BOOL_NOT
                            root = (CommonTree)adaptor.Nil();
                            IToken AT_BOOL_NOT45 = (IToken)Match(input, AT_BOOL_NOT, FOLLOW_AT_BOOL_NOT_in_unary_operator1143
                                );
                            CommonTree AT_BOOL_NOT45_tree = (CommonTree)adaptor.Create(AT_BOOL_NOT45);
                            adaptor.AddChild(root, AT_BOOL_NOT45_tree);
                            break;
                        }
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "postfix"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:310:1: postfix : ( primary | NAMESPACE_ID arguments -> ^( AT_CALL NAMESPACE_ID ( arguments )? ) );

        public AstParserRuleReturnScope<ITree, IToken> Postfix()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root = null;
            CommonTree NAMESPACE_ID47_tree = null;
            var streamNamespaceId = new RewriteRuleTokenStream(adaptor, "token NAMESPACE_ID");
            var streamArguments = new RewriteRuleSubtreeStream(adaptor, "rule arguments");
            try
            {
                // src/java/org/apache/lucene/expressions/js/Javascript.g:311:5: ( primary | NAMESPACE_ID arguments -> ^( AT_CALL NAMESPACE_ID ( arguments )? ) )
                int alt14 = 2;
                int LA14_0 = input.LA(1);
                if ((LA14_0 == NAMESPACE_ID))
                {
                    int LA14_1 = input.LA(2);
                    if ((LA14_1 == EOF || (LA14_1 >= AT_ADD && LA14_1 <= AT_BIT_AND) || (LA14_1 >= AT_BIT_OR
                         && LA14_1 <= AT_BOOL_AND) || LA14_1 == AT_BOOL_OR || (LA14_1 >= AT_COLON && LA14_1
                         <= AT_DIVIDE) || (LA14_1 >= AT_MODULO && LA14_1 <= AT_MULTIPLY) || (LA14_1 >= AT_RPAREN
                         && LA14_1 <= AT_SUBTRACT)))
                    {
                        alt14 = 1;
                    }
                    else
                    {
                        if ((LA14_1 == AT_LPAREN))
                        {
                            alt14 = 2;
                        }
                        else
                        {
                            int nvaeMark = input.Mark();
                            try
                            {
                                input.Consume();
                                NoViableAltException nvae = new NoViableAltException(string.Empty, 14, 1, input);
                                throw nvae;
                            }
                            finally
                            {
                                input.Rewind(nvaeMark);
                            }
                        }
                    }
                }
                else
                {
                    if ((LA14_0 == AT_LPAREN || LA14_0 == DECIMAL || LA14_0 == HEX || LA14_0 == OCTAL
                        ))
                    {
                        alt14 = 1;
                    }
                    else
                    {
                        NoViableAltException nvae = new NoViableAltException(string.Empty, 14, 0, input);
                        throw nvae;
                    }
                }
                switch (alt14)
                {
                    case 1:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:311:7: primary
                            root = (CommonTree)adaptor.Nil();
                            PushFollow(FOLLOW_primary_in_postfix1160);
                            AstParserRuleReturnScope<ITree, IToken> primary46 = Primary();
                            state._fsp--;
                            adaptor.AddChild(root, primary46.Tree);
                            break;
                        }

                    case 2:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:312:7: NAMESPACE_ID arguments
                            IToken NAMESPACE_ID47 = (IToken)Match(input, NAMESPACE_ID, FOLLOW_NAMESPACE_ID_in_postfix1168
                                );
                            streamNamespaceId.Add(NAMESPACE_ID47);
                            PushFollow(FOLLOW_arguments_in_postfix1170);
                            AstParserRuleReturnScope<ITree, IToken> arguments48 = Arguments();
                            state._fsp--;
                            streamArguments.Add(arguments48.Tree);
                            // AST REWRITE
                            // elements: NAMESPACE_ID, arguments
                            // token labels: 
                            // rule labels: retval
                            // token list labels: 
                            // rule list labels: 
                            // wildcard labels: 
                            retval.Tree = root;
                            RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval"
                                , retval != null ? ((CommonTree)retval.Tree) : null);
                            root = (CommonTree)adaptor.Nil();
                            {
                                {
                                    // 312:30: -> ^( AT_CALL NAMESPACE_ID ( arguments )? )
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:312:33: ^( AT_CALL NAMESPACE_ID ( arguments )? )
                                    CommonTree root_1 = (CommonTree)adaptor.Nil();
                                    root_1 = (CommonTree)adaptor.BecomeRoot((CommonTree)adaptor.Create(AT_CALL, "AT_CALL"
                                        ), root_1);
                                    adaptor.AddChild(root_1, streamNamespaceId.NextNode());
                                    // src/java/org/apache/lucene/expressions/js/Javascript.g:312:56: ( arguments )?
                                    if (streamArguments.HasNext)
                                    {
                                        adaptor.AddChild(root_1, streamArguments.NextTree());
                                    }
                                    streamArguments.Reset();
                                    adaptor.AddChild(root, root_1);
                                }
                            }
                            retval.Tree = root;
                            break;
                        }
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "primary"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:315:1: primary : ( NAMESPACE_ID | numeric | AT_LPAREN ! conditional AT_RPAREN !);

        public AstParserRuleReturnScope<ITree, IToken> Primary()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root = null;
            IToken AT_LPAREN51 = null;
            IToken AT_RPAREN53 = null;
            CommonTree NAMESPACE_ID49_tree = null;
            CommonTree AT_LPAREN51_tree = null;
            CommonTree AT_RPAREN53_tree = null;
            try
            {
                // src/java/org/apache/lucene/expressions/js/Javascript.g:316:5: ( NAMESPACE_ID | numeric | AT_LPAREN ! conditional AT_RPAREN !)
                int alt15 = 3;
                switch (input.LA(1))
                {
                    case NAMESPACE_ID:
                        {
                            alt15 = 1;
                            break;
                        }

                    case DECIMAL:
                    case HEX:
                    case OCTAL:
                        {
                            alt15 = 2;
                            break;
                        }

                    case AT_LPAREN:
                        {
                            alt15 = 3;
                            break;
                        }

                    default:
                        {
                            NoViableAltException nvae = new NoViableAltException(string.Empty, 15, 0, input);
                            throw nvae;
                        }
                }
                switch (alt15)
                {
                    case 1:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:316:7: NAMESPACE_ID
                            root = (CommonTree)adaptor.Nil();
                            IToken NAMESPACE_ID49 = (IToken)Match(input, NAMESPACE_ID, FOLLOW_NAMESPACE_ID_in_primary1198
                                );
                            NAMESPACE_ID49_tree = (CommonTree)adaptor.Create(NAMESPACE_ID49);
                            adaptor.AddChild(root, NAMESPACE_ID49_tree);
                            break;
                        }

                    case 2:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:317:7: numeric
                            root = (CommonTree)adaptor.Nil();
                            PushFollow(FOLLOW_numeric_in_primary1206);
                            AstParserRuleReturnScope<ITree, IToken> numeric50 = Numeric();
                            state._fsp--;
                            adaptor.AddChild(root, numeric50.Tree);
                            break;
                        }

                    case 3:
                        {
                            // src/java/org/apache/lucene/expressions/js/Javascript.g:318:7: AT_LPAREN ! conditional AT_RPAREN !
                            root = (CommonTree)adaptor.Nil();
                            AT_LPAREN51 = (IToken)Match(input, AT_LPAREN, FOLLOW_AT_LPAREN_in_primary1214);
                            PushFollow(FOLLOW_conditional_in_primary1217);
                            AstParserRuleReturnScope<ITree, IToken> conditional52 = Conditional();
                            state._fsp--;
                            adaptor.AddChild(root, conditional52.Tree);
                            AT_RPAREN53 = (IToken)Match(input, AT_RPAREN, FOLLOW_AT_RPAREN_in_primary1219);
                            break;
                        }
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }

        // $ANTLR start "arguments"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:321:1: arguments : AT_LPAREN ! ( conditional ( AT_COMMA ! conditional )* )? AT_RPAREN !;

        public AstParserRuleReturnScope<ITree, IToken> Arguments()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree root = null;
            IToken AT_LPAREN54 = null;
            IToken AT_COMMA56 = null;
            IToken AT_RPAREN58 = null;
            CommonTree AT_LPAREN54_tree = null;
            CommonTree AT_COMMA56_tree = null;
            CommonTree AT_RPAREN58_tree = null;
            try
            {
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:322:5: ( AT_LPAREN ! ( conditional ( AT_COMMA ! conditional )* )? AT_RPAREN !)
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:322:7: AT_LPAREN ! ( conditional ( AT_COMMA ! conditional )* )? AT_RPAREN !
                    root = (CommonTree)adaptor.Nil();
                    AT_LPAREN54 = (IToken)Match(input, AT_LPAREN, FOLLOW_AT_LPAREN_in_arguments1237);
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:322:18: ( conditional ( AT_COMMA ! conditional )* )?
                    int alt17 = 2;
                    int LA17_0 = input.LA(1);
                    if ((LA17_0 == AT_ADD || LA17_0 == AT_BIT_NOT || LA17_0 == AT_BOOL_NOT || LA17_0
                        == AT_LPAREN || (LA17_0 >= AT_SUBTRACT && LA17_0 <= DECIMAL) || LA17_0 == HEX ||
                         (LA17_0 >= NAMESPACE_ID && LA17_0 <= OCTAL)))
                    {
                        alt17 = 1;
                    }
                    switch (alt17)
                    {
                        case 1:
                            {
                                // src/java/org/apache/lucene/expressions/js/Javascript.g:322:19: conditional ( AT_COMMA ! conditional )*
                                PushFollow(FOLLOW_conditional_in_arguments1241);
                                AstParserRuleReturnScope<ITree, IToken> conditional55 = Conditional();
                                state._fsp--;
                                adaptor.AddChild(root, conditional55.Tree);
                                // src/java/org/apache/lucene/expressions/js/Javascript.g:322:31: ( AT_COMMA ! conditional )*
                                while (true)
                                {
                                    int alt16 = 2;
                                    int LA16_0 = input.LA(1);
                                    if ((LA16_0 == AT_COMMA))
                                    {
                                        alt16 = 1;
                                    }
                                    switch (alt16)
                                    {
                                        case 1:
                                            {
                                                // src/java/org/apache/lucene/expressions/js/Javascript.g:322:32: AT_COMMA ! conditional
                                                AT_COMMA56 = (IToken)Match(input, AT_COMMA, FOLLOW_AT_COMMA_in_arguments1244);
                                                PushFollow(FOLLOW_conditional_in_arguments1247);
                                                AstParserRuleReturnScope<ITree, IToken> conditional57 = Conditional();
                                                state._fsp--;
                                                adaptor.AddChild(root, conditional57.Tree);
                                                break;
                                            }

                                        default:
                                            {
                                                goto loop16_break;
                                                break;
                                            }
                                    }
                                loop16_continue: ;
                                }
                            loop16_break: ;
                                break;
                            }
                    }
                    AT_RPAREN58 = (IToken)Match(input, AT_RPAREN, FOLLOW_AT_RPAREN_in_arguments1253);
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }


        // $ANTLR start "numeric"
        // src/java/org/apache/lucene/expressions/js/Javascript.g:325:1: numeric : ( HEX | OCTAL | DECIMAL );

        public AstParserRuleReturnScope<ITree, IToken> Numeric()
        {
            var retval = new AstParserRuleReturnScope<ITree, IToken> { Start = input.LT(1) };
            CommonTree set59_tree = null;
            try
            {
                CommonTree root = null;
                {
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:326:5: ( HEX | OCTAL | DECIMAL )
                    // src/java/org/apache/lucene/expressions/js/Javascript.g:
                    root = (CommonTree)adaptor.Nil();
                    IToken set59 = input.LT(1);
                    if (input.LA(1) == DECIMAL || input.LA(1) == HEX || input.LA(1) == OCTAL)
                    {
                        input.Consume();
                        adaptor.AddChild(root, (CommonTree)adaptor.Create(set59));
                        state.errorRecovery = false;
                    }
                    else
                    {
                        MismatchedSetException mse = new MismatchedSetException(null, input);
                        throw mse;
                    }
                }
                retval.Stop = input.LT(-1);
                retval.Tree = (CommonTree)adaptor.RulePostProcessing(root);
                adaptor.SetTokenBoundaries(retval.Tree, retval.Start, retval.Stop);
            }
            catch (RecognitionException re)
            {
                ReportError(re);
                Recover(input, re);
                retval.Tree = (CommonTree)adaptor.ErrorNode(input, retval.Start, input.LT(-1), re
                    );
            }
            // do for sure before leaving
            return retval;
        }

        public static readonly BitSet FOLLOW_conditional_in_expression737 = new BitSet(new[] { ((ulong)(0x0000000000000000L)) });

        public static readonly BitSet FOLLOW_EOF_in_expression739 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_logical_or_in_conditional757 = new BitSet(new[] { ((ulong)(0x0000000001000002L)) });

        public static readonly BitSet FOLLOW_AT_COND_QUE_in_conditional760 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_conditional_in_conditional763 = new BitSet(new[] { ((ulong)(0x0000000000010000L)) });

        public static readonly BitSet FOLLOW_AT_COLON_in_conditional765 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_conditional_in_conditional768 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_logical_and_in_logical_or787 = new BitSet(new[] { ((ulong)(0x0000000000004002L)) });

        public static readonly BitSet FOLLOW_AT_BOOL_OR_in_logical_or790 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_logical_and_in_logical_or793 = new BitSet(new[] { ((ulong)(0x0000000000004002L)) });

        public static readonly BitSet FOLLOW_bitwise_or_in_logical_and812 = new BitSet(new[] { ((ulong)(0x0000000000001002L)) });

        public static readonly BitSet FOLLOW_AT_BOOL_AND_in_logical_and815 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_bitwise_or_in_logical_and818 = new BitSet(new[] { ((ulong)(0x0000000000001002L)) });

        public static readonly BitSet FOLLOW_bitwise_xor_in_bitwise_or837 = new BitSet(new[] { ((ulong)(0x0000000000000082L)) });

        public static readonly BitSet FOLLOW_AT_BIT_OR_in_bitwise_or840 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_bitwise_xor_in_bitwise_or843 = new BitSet(new[] { ((ulong)(0x0000000000000082L)) });

        public static readonly BitSet FOLLOW_bitwise_and_in_bitwise_xor862 = new BitSet(new[] { ((ulong)(0x0000000000000802L)) });

        public static readonly BitSet FOLLOW_AT_BIT_XOR_in_bitwise_xor865 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_bitwise_and_in_bitwise_xor868 = new BitSet(new[] { ((ulong)(0x0000000000000802L)) });

        public static readonly BitSet FOLLOW_equality_in_bitwise_and888 = new BitSet(new[] { ((ulong)(0x0000000000000022L)) });

        public static readonly BitSet FOLLOW_AT_BIT_AND_in_bitwise_and891 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_equality_in_bitwise_and894 = new BitSet(new[] { ((ulong)(0x0000000000000022L)) });

        public static readonly BitSet FOLLOW_relational_in_equality913 = new BitSet(new[] { ((ulong)(0x0000000000840002L)) });

        public static readonly BitSet FOLLOW_set_in_equality916 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_relational_in_equality925 = new BitSet(new[] { ((ulong)(0x0000000000840002L)) });

        public static readonly BitSet FOLLOW_shift_in_relational944 = new BitSet(new[] { ((ulong)(0x0000000000780002L)) });

        public static readonly BitSet FOLLOW_set_in_relational947 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_shift_in_relational964 = new BitSet(new[] { ((ulong)(0x0000000000780002L)) });

        public static readonly BitSet FOLLOW_additive_in_shift983 = new BitSet(new[] { ((ulong)(0x0000000000000702L)) });

        public static readonly BitSet FOLLOW_set_in_shift986 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_additive_in_shift999 = new BitSet(new[] { ((ulong)(0x0000000000000702L)) });

        public static readonly BitSet FOLLOW_multiplicative_in_additive1018 = new BitSet(new[] { ((ulong)(0x0000000100000012L)) });

        public static readonly BitSet FOLLOW_set_in_additive1021 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_multiplicative_in_additive1030 = new BitSet(new[] { ((ulong)(0x0000000100000012L)) });

        public static readonly BitSet FOLLOW_unary_in_multiplicative1049 = new BitSet(new[] { ((ulong)(0x0000000032000002L)) });

        public static readonly BitSet FOLLOW_set_in_multiplicative1052 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_unary_in_multiplicative1065 = new BitSet(new[] { ((ulong)(0x0000000032000002L)) });

        public static readonly BitSet FOLLOW_postfix_in_unary1084 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_AT_ADD_in_unary1092 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_unary_in_unary1095 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_unary_operator_in_unary1103 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_unary_in_unary1106 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_AT_SUBTRACT_in_unary_operator1123 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_AT_BIT_NOT_in_unary_operator1135 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_AT_BOOL_NOT_in_unary_operator1143 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_primary_in_postfix1160 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_NAMESPACE_ID_in_postfix1168 = new BitSet(new[] { ((ulong)(0x0000000008000000L)) });

        public static readonly BitSet FOLLOW_arguments_in_postfix1170 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_NAMESPACE_ID_in_primary1198 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_numeric_in_primary1206 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_AT_LPAREN_in_primary1214 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_conditional_in_primary1217 = new BitSet(new[] { ((ulong)(0x0000000080000000L)) });

        public static readonly BitSet FOLLOW_AT_RPAREN_in_primary1219 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });

        public static readonly BitSet FOLLOW_AT_LPAREN_in_arguments1237 = new BitSet(new[] { ((ulong)(0x0000032388002050L)) });

        public static readonly BitSet FOLLOW_conditional_in_arguments1241 = new BitSet(new[] { ((ulong)(0x0000000080020000L)) });

        public static readonly BitSet FOLLOW_AT_COMMA_in_arguments1244 = new BitSet(new[] { ((ulong)(0x0000032308002050L)) });

        public static readonly BitSet FOLLOW_conditional_in_arguments1247 = new BitSet(new[] { ((ulong)(0x0000000080020000L)) });

        public static readonly BitSet FOLLOW_AT_RPAREN_in_arguments1253 = new BitSet(new[] { ((ulong)(0x0000000000000002L)) });
        // $ANTLR end "numeric"
        // Delegated rules
    }


}
