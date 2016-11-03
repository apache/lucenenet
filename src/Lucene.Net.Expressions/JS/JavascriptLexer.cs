using System;
using Antlr.Runtime;

namespace Lucene.Net.Expressions.JS
{
	internal class JavascriptLexer : Lexer
	{
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
		public override void DisplayRecognitionError(string[] tokenNames, RecognitionException re)
		{
			string message = " unexpected character '" + (char)re.Character + "' at position (" + re.CharPositionInLine + ").";
			ParseException parseException = new ParseException(message, re.CharPositionInLine);
			
			throw new InvalidOperationException(parseException.Message, parseException);
		}

		// delegates
		// delegators
		public virtual Lexer[] GetDelegates()
		{
			return new Lexer[] {  };
		}

		public JavascriptLexer()
		{
			dfa9 = new JavascriptLexer.DFA9(this, this);
		}

		public JavascriptLexer(ICharStream input) : this(input, new RecognizerSharedState(
			))
		{
			dfa9 = new JavascriptLexer.DFA9(this, this);
		}

		public JavascriptLexer(ICharStream input, RecognizerSharedState state) : base(input
			, state)
		{
			dfa9 = new JavascriptLexer.DFA9(this, this);
		}

		public override string GrammarFileName
		{
		    get { return ""; }
		}

		// $ANTLR start "AT_ADD"
		
		public void MAT_ADD()
		{
			try
			{
				int _type = AT_ADD;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:25:8: ( '+' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:25:10: '+'
					Match('+');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_ADD"
		// $ANTLR start "AT_BIT_AND"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_AND()
		{
		    int _type = AT_BIT_AND;
		    int _channel = TokenChannels.Default;
		    {
		        // src/java/org/apache/lucene/expressions/js/Javascript.g:26:12: ( '&' )
		        // src/java/org/apache/lucene/expressions/js/Javascript.g:26:14: '&'
		        Match('&');
		    }
		    state.type = _type;
		    state.channel = _channel;
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_AND"
		// $ANTLR start "AT_BIT_NOT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_NOT()
		{
			try
			{
				int _type = AT_BIT_NOT;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:27:12: ( '~' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:27:14: '~'
					Match('~');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_NOT"
		// $ANTLR start "AT_BIT_OR"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_OR()
		{
			try
			{
				int _type = AT_BIT_OR;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:28:11: ( '|' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:28:13: '|'
					Match('|');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_OR"
		// $ANTLR start "AT_BIT_SHL"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_SHL()
		{
			try
			{
				int _type = AT_BIT_SHL;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:29:12: ( '<<' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:29:14: '<<'
					Match("<<");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_SHL"
		// $ANTLR start "AT_BIT_SHR"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_SHR()
		{
			try
			{
				int _type = AT_BIT_SHR;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:30:12: ( '>>' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:30:14: '>>'
					Match(">>");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_SHR"
		// $ANTLR start "AT_BIT_SHU"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_SHU()
		{
			try
			{
				int _type = AT_BIT_SHU;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:31:12: ( '>>>' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:31:14: '>>>'
					Match(">>>");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_SHU"
		// $ANTLR start "AT_BIT_XOR"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BIT_XOR()
		{
			try
			{
				int _type = AT_BIT_XOR;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:32:12: ( '^' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:32:14: '^'
					Match('^');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BIT_XOR"
		// $ANTLR start "AT_BOOL_AND"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BOOL_AND()
		{
			try
			{
				int _type = AT_BOOL_AND;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:33:13: ( '&&' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:33:15: '&&'
					Match("&&");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BOOL_AND"
		// $ANTLR start "AT_BOOL_NOT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BOOL_NOT()
		{
			try
			{
				int _type = AT_BOOL_NOT;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:34:13: ( '!' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:34:15: '!'
					Match('!');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BOOL_NOT"
		// $ANTLR start "AT_BOOL_OR"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_BOOL_OR()
		{
			try
			{
				int _type = AT_BOOL_OR;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:35:12: ( '||' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:35:14: '||'
					Match("||");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_BOOL_OR"
		// $ANTLR start "AT_COLON"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COLON()
		{
			try
			{
				int _type = AT_COLON;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:36:10: ( ':' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:36:12: ':'
					Match(':');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COLON"
		// $ANTLR start "AT_COMMA"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMMA()
		{
			try
			{
				int _type = AT_COMMA;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:37:10: ( ',' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:37:12: ','
					Match(',');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMMA"
		// $ANTLR start "AT_COMP_EQ"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMP_EQ()
		{
			try
			{
				int _type = AT_COMP_EQ;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:38:12: ( '==' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:38:14: '=='
					Match("==");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMP_EQ"
		// $ANTLR start "AT_COMP_GT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMP_GT()
		{
			try
			{
				int _type = AT_COMP_GT;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:39:12: ( '>' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:39:14: '>'
					Match('>');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMP_GT"
		// $ANTLR start "AT_COMP_GTE"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMP_GTE()
		{
			try
			{
				int _type = AT_COMP_GTE;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:40:13: ( '>=' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:40:15: '>='
					Match(">=");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMP_GTE"
		// $ANTLR start "AT_COMP_LT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMP_LT()
		{
			try
			{
				int _type = AT_COMP_LT;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:41:12: ( '<' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:41:14: '<'
					Match('<');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMP_LT"
		// $ANTLR start "AT_COMP_LTE"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMP_LTE()
		{
			try
			{
				int _type = AT_COMP_LTE;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:42:13: ( '<=' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:42:15: '<='
					Match("<=");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMP_LTE"
		// $ANTLR start "AT_COMP_NEQ"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COMP_NEQ()
		{
			try
			{
				int _type = AT_COMP_NEQ;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:43:13: ( '!=' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:43:15: '!='
					Match("!=");
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COMP_NEQ"
		// $ANTLR start "AT_COND_QUE"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_COND_QUE()
		{
			try
			{
				int _type = AT_COND_QUE;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:44:13: ( '?' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:44:15: '?'
					Match('?');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_COND_QUE"
		// $ANTLR start "AT_DIVIDE"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_DIVIDE()
		{
			try
			{
				int _type = AT_DIVIDE;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:45:11: ( '/' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:45:13: '/'
					Match('/');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_DIVIDE"
		// $ANTLR start "AT_DOT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_DOT()
		{
			try
			{
				int _type = AT_DOT;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:46:8: ( '.' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:46:10: '.'
					Match('.');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_DOT"
		// $ANTLR start "AT_LPAREN"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_LPAREN()
		{
			try
			{
				int _type = AT_LPAREN;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:47:11: ( '(' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:47:13: '('
					Match('(');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_LPAREN"
		// $ANTLR start "AT_MODULO"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_MODULO()
		{
			try
			{
				int _type = AT_MODULO;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:48:11: ( '%' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:48:13: '%'
					Match('%');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_MODULO"
		// $ANTLR start "AT_MULTIPLY"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_MULTIPLY()
		{
			try
			{
				int _type = AT_MULTIPLY;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:49:13: ( '*' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:49:15: '*'
					Match('*');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_MULTIPLY"
		// $ANTLR start "AT_RPAREN"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_RPAREN()
		{
			try
			{
				int _type = AT_RPAREN;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:50:11: ( ')' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:50:13: ')'
					Match(')');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_RPAREN"
		// $ANTLR start "AT_SUBTRACT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MAT_SUBTRACT()
		{
			try
			{
				int _type = AT_SUBTRACT;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:51:13: ( '-' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:51:15: '-'
					Match('-');
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "AT_SUBTRACT"
		// $ANTLR start "NAMESPACE_ID"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MNAMESPACE_ID()
		{
			try
			{
				int _type = NAMESPACE_ID;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:334:5: ( ID ( AT_DOT ID )* )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:334:7: ID ( AT_DOT ID )*
					MID();
					// src/java/org/apache/lucene/expressions/js/Javascript.g:334:10: ( AT_DOT ID )*
					while (true)
					{
						int alt1 = 2;
						int LA1_0 = input.LA(1);
						if ((LA1_0 == '.'))
						{
							alt1 = 1;
						}
						switch (alt1)
						{
							case 1:
							{
								// src/java/org/apache/lucene/expressions/js/Javascript.g:334:11: AT_DOT ID
								MAT_DOT();
								MID();
								break;
							}

							default:
							{
								goto loop1_break;
								break;
							}
						}
loop1_continue: ;
					}
loop1_break: ;
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "NAMESPACE_ID"
		// $ANTLR start "ID"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MID()
		{
			try
			{
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:340:5: ( ( 'a' .. 'z' | 'A' .. 'Z' | '_' ) ( 'a' .. 'z' | 'A' .. 'Z' | '0' .. '9' | '_' )* )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:340:7: ( 'a' .. 'z' | 'A' .. 'Z' | '_' ) ( 'a' .. 'z' | 'A' .. 'Z' | '0' .. '9' | '_' )*
					if ((input.LA(1) >= 'A' && input.LA(1) <= 'Z') || input.LA(1) == '_' || (input.LA
						(1) >= 'a' && input.LA(1) <= 'z'))
					{
						input.Consume();
					}
					else
					{
						MismatchedSetException mse = new MismatchedSetException(null, input);
						Recover(mse);
						throw mse;
					}
					// src/java/org/apache/lucene/expressions/js/Javascript.g:340:31: ( 'a' .. 'z' | 'A' .. 'Z' | '0' .. '9' | '_' )*
					while (true)
					{
						int alt2 = 2;
						int LA2_0 = input.LA(1);
						if (((LA2_0 >= '0' && LA2_0 <= '9') || (LA2_0 >= 'A' && LA2_0 <= 'Z') || LA2_0 ==
							 '_' || (LA2_0 >= 'a' && LA2_0 <= 'z')))
						{
							alt2 = 1;
						}
						switch (alt2)
						{
							case 1:
							{
								// src/java/org/apache/lucene/expressions/js/Javascript.g:
								if ((input.LA(1) >= '0' && input.LA(1) <= '9') || (input.LA(1) >= 'A' && input.LA
									(1) <= 'Z') || input.LA(1) == '_' || (input.LA(1) >= 'a' && input.LA(1) <= 'z'))
								{
									input.Consume();
								}
								else
								{
									MismatchedSetException mse = new MismatchedSetException(null, input);
									Recover(mse);
									throw mse;
								}
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
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "ID"
		// $ANTLR start "WS"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MWS()
		{
			try
			{
				int _type = WS;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:343:5: ( ( ' ' | '\\t' | '\\n' | '\\r' )+ )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:343:7: ( ' ' | '\\t' | '\\n' | '\\r' )+
					// src/java/org/apache/lucene/expressions/js/Javascript.g:343:7: ( ' ' | '\\t' | '\\n' | '\\r' )+
					int cnt3 = 0;
					while (true)
					{
						int alt3 = 2;
						int LA3_0 = input.LA(1);
						if (((LA3_0 >= '\t' && LA3_0 <= '\n') || LA3_0 == '\r' || LA3_0 == ' '))
						{
							alt3 = 1;
						}
						switch (alt3)
						{
							case 1:
							{
								// src/java/org/apache/lucene/expressions/js/Javascript.g:
								if ((input.LA(1) >= '\t' && input.LA(1) <= '\n') || input.LA(1) == '\r' || input.
									LA(1) == ' ')
								{
									input.Consume();
								}
								else
								{
									MismatchedSetException mse = new MismatchedSetException(null, input);
									Recover(mse);
									throw mse;
								}
								break;
							}

							default:
							{
								if (cnt3 >= 1)
								{
									goto loop3_break;
								}
								EarlyExitException eee = new EarlyExitException(3, input);
								throw eee;
							}
						}
						cnt3++;
loop3_continue: ;
					}
loop3_break: ;
					Skip();
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "WS"
		// $ANTLR start "DECIMAL"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MDECIMAL()
		{
		    try
		    {
		        int type = DECIMAL;
		        int channel = TokenChannels.Default;
		        // src/java/org/apache/lucene/expressions/js/Javascript.g:347:5: ( DECIMALINTEGER AT_DOT ( DECIMALDIGIT )* ( EXPONENT )? | AT_DOT ( DECIMALDIGIT )+ ( EXPONENT )? | DECIMALINTEGER ( EXPONENT )? )
		        int alt9 = 3;
		        alt9 = dfa9.Predict(input);
		        switch (alt9)
		        {
		            case 1:
		            {
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:347:7: DECIMALINTEGER AT_DOT ( DECIMALDIGIT )* ( EXPONENT )?
		                MDECIMALINTEGER();
		                MAT_DOT();
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:347:29: ( DECIMALDIGIT )*
		                while (true)
		                {
		                    int alt4 = 2;
		                    int LA4_0 = input.LA(1);
		                    if (((LA4_0 >= '0' && LA4_0 <= '9')))
		                    {
		                        alt4 = 1;
		                    }
		                    switch (alt4)
		                    {
		                        case 1:
		                        {
		                            // src/java/org/apache/lucene/expressions/js/Javascript.g:
		                            if ((input.LA(1) >= '0' && input.LA(1) <= '9'))
		                            {
		                                input.Consume();
		                            }
		                            else
		                            {
		                                MismatchedSetException mse = new MismatchedSetException(null, input);
		                                Recover(mse);
		                                throw mse;
		                            }
		                            break;
		                        }

		                        default:
		                        {
		                            goto loop4_break;
		                            break;
		                        }
		                    }
		                    loop4_continue:
		                    ;
		                }
		                loop4_break:
		                ;
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:347:43: ( EXPONENT )?
		                int alt5 = 2;
		                int LA5_0 = input.LA(1);
		                if ((LA5_0 == 'E' || LA5_0 == 'e'))
		                {
		                    alt5 = 1;
		                }
		                switch (alt5)
		                {
		                    case 1:
		                    {
		                        // src/java/org/apache/lucene/expressions/js/Javascript.g:347:43: EXPONENT
		                        MEXPONENT();
		                        break;
		                    }
		                }
		                break;
		            }

		            case 2:
		            {
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:348:7: AT_DOT ( DECIMALDIGIT )+ ( EXPONENT )?
		                MAT_DOT();
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:348:14: ( DECIMALDIGIT )+
		                int cnt6 = 0;
		                while (true)
		                {
		                    int alt6 = 2;
		                    int LA6_0 = input.LA(1);
		                    if (((LA6_0 >= '0' && LA6_0 <= '9')))
		                    {
		                        alt6 = 1;
		                    }
		                    switch (alt6)
		                    {
		                        case 1:
		                        {
		                            // src/java/org/apache/lucene/expressions/js/Javascript.g:
		                            if ((input.LA(1) >= '0' && input.LA(1) <= '9'))
		                            {
		                                input.Consume();
		                            }
		                            else
		                            {
		                                MismatchedSetException mse = new MismatchedSetException(null, input);
		                                Recover(mse);
		                                throw mse;
		                            }
		                            break;
		                        }

		                        default:
		                        {
		                            if (cnt6 >= 1)
		                            {
		                                goto loop6_break;
		                            }
		                            EarlyExitException eee = new EarlyExitException(6, input);
		                            throw eee;
		                        }
		                    }
		                    cnt6++;
		                    loop6_continue:
		                    ;
		                }
		                loop6_break:
		                ;
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:348:28: ( EXPONENT )?
		                int alt7 = 2;
		                int LA7_0 = input.LA(1);
		                if ((LA7_0 == 'E' || LA7_0 == 'e'))
		                {
		                    alt7 = 1;
		                }
		                switch (alt7)
		                {
		                    case 1:
		                    {
		                        // src/java/org/apache/lucene/expressions/js/Javascript.g:348:28: EXPONENT
		                        MEXPONENT();
		                        break;
		                    }
		                }
		                break;
		            }

		            case 3:
		            {
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:349:7: DECIMALINTEGER ( EXPONENT )?
		                MDECIMALINTEGER();
		                // src/java/org/apache/lucene/expressions/js/Javascript.g:349:22: ( EXPONENT )?
		                int alt8 = 2;
		                int LA8_0 = input.LA(1);
		                if ((LA8_0 == 'E' || LA8_0 == 'e'))
		                {
		                    alt8 = 1;
		                }
		                switch (alt8)
		                {
		                    case 1:
		                    {
		                        // src/java/org/apache/lucene/expressions/js/Javascript.g:349:22: EXPONENT
		                        MEXPONENT();
		                        break;
		                    }
		                }
		                break;
		            }
		        }
		        state.type = type;
		        state.channel = channel;
		    }
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "DECIMAL"
		// $ANTLR start "OCTAL"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MOCTAL()
		{
			try
			{
				int _type = OCTAL;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:353:5: ( '0' ( OCTALDIGIT )+ )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:353:7: '0' ( OCTALDIGIT )+
					Match('0');
					// src/java/org/apache/lucene/expressions/js/Javascript.g:353:11: ( OCTALDIGIT )+
					int cnt10 = 0;
					while (true)
					{
						int alt10 = 2;
						int LA10_0 = input.LA(1);
						if (((LA10_0 >= '0' && LA10_0 <= '7')))
						{
							alt10 = 1;
						}
						switch (alt10)
						{
							case 1:
							{
								// src/java/org/apache/lucene/expressions/js/Javascript.g:
								if ((input.LA(1) >= '0' && input.LA(1) <= '7'))
								{
									input.Consume();
								}
								else
								{
									MismatchedSetException mse = new MismatchedSetException(null, input);
									Recover(mse);
									throw mse;
								}
								break;
							}

							default:
							{
								if (cnt10 >= 1)
								{
									goto loop10_break;
								}
								EarlyExitException eee = new EarlyExitException(10, input);
								throw eee;
							}
						}
						cnt10++;
loop10_continue: ;
					}
loop10_break: ;
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "OCTAL"
		// $ANTLR start "HEX"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MHEX()
		{
			try
			{
				int _type = HEX;
				int _channel = TokenChannels.Default;
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:357:5: ( ( '0x' | '0X' ) ( HEXDIGIT )+ )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:357:7: ( '0x' | '0X' ) ( HEXDIGIT )+
					// src/java/org/apache/lucene/expressions/js/Javascript.g:357:7: ( '0x' | '0X' )
					int alt11 = 2;
					int LA11_0 = input.LA(1);
					if ((LA11_0 == '0'))
					{
						int LA11_1 = input.LA(2);
						if ((LA11_1 == 'x'))
						{
							alt11 = 1;
						}
						else
						{
							if ((LA11_1 == 'X'))
							{
								alt11 = 2;
							}
							else
							{
								int nvaeMark = input.Mark();
								try
								{
									input.Consume();
									NoViableAltException nvae = new NoViableAltException(string.Empty, 11, 1, input);
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
						NoViableAltException nvae = new NoViableAltException(string.Empty, 11, 0, input);
						throw nvae;
					}
					switch (alt11)
					{
						case 1:
						{
							// src/java/org/apache/lucene/expressions/js/Javascript.g:357:8: '0x'
							Match("0x");
							break;
						}

						case 2:
						{
							// src/java/org/apache/lucene/expressions/js/Javascript.g:357:13: '0X'
							Match("0X");
							break;
						}
					}
					// src/java/org/apache/lucene/expressions/js/Javascript.g:357:19: ( HEXDIGIT )+
					int cnt12 = 0;
					while (true)
					{
						int alt12 = 2;
						int LA12_0 = input.LA(1);
						if (((LA12_0 >= '0' && LA12_0 <= '9') || (LA12_0 >= 'A' && LA12_0 <= 'F') || (LA12_0
							 >= 'a' && LA12_0 <= 'f')))
						{
							alt12 = 1;
						}
						switch (alt12)
						{
							case 1:
							{
								// src/java/org/apache/lucene/expressions/js/Javascript.g:
								if ((input.LA(1) >= '0' && input.LA(1) <= '9') || (input.LA(1) >= 'A' && input.LA
									(1) <= 'F') || (input.LA(1) >= 'a' && input.LA(1) <= 'f'))
								{
									input.Consume();
								}
								else
								{
									MismatchedSetException mse = new MismatchedSetException(null, input);
									Recover(mse);
									throw mse;
								}
								break;
							}

							default:
							{
								if (cnt12 >= 1)
								{
									goto loop12_break;
								}
								EarlyExitException eee = new EarlyExitException(12, input);
								throw eee;
							}
						}
						cnt12++;
loop12_continue: ;
					}
loop12_break: ;
				}
				state.type = _type;
				state.channel = _channel;
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "HEX"
		// $ANTLR start "DECIMALINTEGER"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MDECIMALINTEGER()
		{
			try
			{
				// src/java/org/apache/lucene/expressions/js/Javascript.g:363:5: ( '0' | '1' .. '9' ( DECIMALDIGIT )* )
				int alt14 = 2;
				int LA14_0 = input.LA(1);
				if ((LA14_0 == '0'))
				{
					alt14 = 1;
				}
				else
				{
					if (((LA14_0 >= '1' && LA14_0 <= '9')))
					{
						alt14 = 2;
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
						// src/java/org/apache/lucene/expressions/js/Javascript.g:363:7: '0'
						Match('0');
						break;
					}

					case 2:
					{
						// src/java/org/apache/lucene/expressions/js/Javascript.g:364:7: '1' .. '9' ( DECIMALDIGIT )*
						MatchRange('1', '9');
						// src/java/org/apache/lucene/expressions/js/Javascript.g:364:16: ( DECIMALDIGIT )*
						while (true)
						{
							int alt13 = 2;
							int LA13_0 = input.LA(1);
							if (((LA13_0 >= '0' && LA13_0 <= '9')))
							{
								alt13 = 1;
							}
							switch (alt13)
							{
								case 1:
								{
									// src/java/org/apache/lucene/expressions/js/Javascript.g:
									if ((input.LA(1) >= '0' && input.LA(1) <= '9'))
									{
										input.Consume();
									}
									else
									{
										MismatchedSetException mse = new MismatchedSetException(null, input);
										Recover(mse);
										throw mse;
									}
									break;
								}

								default:
								{
									goto loop13_break;
									break;
								}
							}
loop13_continue: ;
						}
loop13_break: ;
						break;
					}
				}
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "DECIMALINTEGER"
		// $ANTLR start "EXPONENT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MEXPONENT()
		{
			try
			{
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:369:5: ( ( 'e' | 'E' ) ( '+' | '-' )? ( DECIMALDIGIT )+ )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:369:7: ( 'e' | 'E' ) ( '+' | '-' )? ( DECIMALDIGIT )+
					if (input.LA(1) == 'E' || input.LA(1) == 'e')
					{
						input.Consume();
					}
					else
					{
						MismatchedSetException mse = new MismatchedSetException(null, input);
						Recover(mse);
						throw mse;
					}
					// src/java/org/apache/lucene/expressions/js/Javascript.g:369:17: ( '+' | '-' )?
					int alt15 = 2;
					int LA15_0 = input.LA(1);
					if ((LA15_0 == '+' || LA15_0 == '-'))
					{
						alt15 = 1;
					}
					switch (alt15)
					{
						case 1:
						{
							// src/java/org/apache/lucene/expressions/js/Javascript.g:
							if (input.LA(1) == '+' || input.LA(1) == '-')
							{
								input.Consume();
							}
							else
							{
								MismatchedSetException mse = new MismatchedSetException(null, input);
								Recover(mse);
								throw mse;
							}
							break;
						}
					}
					// src/java/org/apache/lucene/expressions/js/Javascript.g:369:28: ( DECIMALDIGIT )+
					int cnt16 = 0;
					while (true)
					{
						int alt16 = 2;
						int LA16_0 = input.LA(1);
						if (((LA16_0 >= '0' && LA16_0 <= '9')))
						{
							alt16 = 1;
						}
						switch (alt16)
						{
							case 1:
							{
								// src/java/org/apache/lucene/expressions/js/Javascript.g:
								if ((input.LA(1) >= '0' && input.LA(1) <= '9'))
								{
									input.Consume();
								}
								else
								{
									MismatchedSetException mse = new MismatchedSetException(null, input);
									Recover(mse);
									throw mse;
								}
								break;
							}

							default:
							{
								if (cnt16 >= 1)
								{
									goto loop16_break;
								}
								EarlyExitException eee = new EarlyExitException(16, input);
								throw eee;
							}
						}
						cnt16++;
loop16_continue: ;
					}
loop16_break: ;
				}
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "EXPONENT"
		// $ANTLR start "DECIMALDIGIT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MDECIMALDIGIT()
		{
			try
			{
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:374:5: ( '0' .. '9' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:
					if ((input.LA(1) >= '0' && input.LA(1) <= '9'))
					{
						input.Consume();
					}
					else
					{
						MismatchedSetException mse = new MismatchedSetException(null, input);
						Recover(mse);
						throw mse;
					}
				}
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "DECIMALDIGIT"
		// $ANTLR start "HEXDIGIT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MHEXDIGIT()
		{
			try
			{
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:379:5: ( DECIMALDIGIT | 'a' .. 'f' | 'A' .. 'F' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:
					if ((input.LA(1) >= '0' && input.LA(1) <= '9') || (input.LA(1) >= 'A' && input.LA
						(1) <= 'F') || (input.LA(1) >= 'a' && input.LA(1) <= 'f'))
					{
						input.Consume();
					}
					else
					{
						MismatchedSetException mse = new MismatchedSetException(null, input);
						Recover(mse);
						throw mse;
					}
				}
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "HEXDIGIT"
		// $ANTLR start "OCTALDIGIT"
		/// <exception cref="Org.Antlr.Runtime.RecognitionException"></exception>
		public void MOCTALDIGIT()
		{
			try
			{
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:386:5: ( '0' .. '7' )
					// src/java/org/apache/lucene/expressions/js/Javascript.g:
					if ((input.LA(1) >= '0' && input.LA(1) <= '7'))
					{
						input.Consume();
					}
					else
					{
						MismatchedSetException mse = new MismatchedSetException(null, input);
						Recover(mse);
						throw mse;
					}
				}
			}
			finally
			{
			}
		}

		// do for sure before leaving
		// $ANTLR end "OCTALDIGIT"
		
		public override void mTokens()
		{
			// src/java/org/apache/lucene/expressions/js/Javascript.g:1:8: ( AT_ADD | AT_BIT_AND | AT_BIT_NOT | AT_BIT_OR | AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU | AT_BIT_XOR | AT_BOOL_AND | AT_BOOL_NOT | AT_BOOL_OR | AT_COLON | AT_COMMA | AT_COMP_EQ | AT_COMP_GT | AT_COMP_GTE | AT_COMP_LT | AT_COMP_LTE | AT_COMP_NEQ | AT_COND_QUE | AT_DIVIDE | AT_DOT | AT_LPAREN | AT_MODULO | AT_MULTIPLY | AT_RPAREN | AT_SUBTRACT | NAMESPACE_ID | WS | DECIMAL | OCTAL | HEX )
			int alt17 = 32;
			switch (input.LA(1))
			{
				case '+':
				{
					alt17 = 1;
					break;
				}

				case '&':
				{
					int LA17_2 = input.LA(2);
					if ((LA17_2 == '&'))
					{
						alt17 = 9;
					}
					else
					{
						alt17 = 2;
					}
					break;
				}

				case '~':
				{
					alt17 = 3;
					break;
				}

				case '|':
				{
					int LA17_4 = input.LA(2);
					if ((LA17_4 == '|'))
					{
						alt17 = 11;
					}
					else
					{
						alt17 = 4;
					}
					break;
				}

				case '<':
				{
					switch (input.LA(2))
					{
						case '<':
						{
							alt17 = 5;
							break;
						}

						case '=':
						{
							alt17 = 18;
							break;
						}

						default:
						{
							alt17 = 17;
							break;
						}
					}
					break;
				}

				case '>':
				{
					switch (input.LA(2))
					{
						case '>':
						{
							int LA17_31 = input.LA(3);
							if ((LA17_31 == '>'))
							{
								alt17 = 7;
							}
							else
							{
								alt17 = 6;
							}
							break;
						}

						case '=':
						{
							alt17 = 16;
							break;
						}

						default:
						{
							alt17 = 15;
							break;
						}
					}
					break;
				}

				case '^':
				{
					alt17 = 8;
					break;
				}

				case '!':
				{
					int LA17_8 = input.LA(2);
					if ((LA17_8 == '='))
					{
						alt17 = 19;
					}
					else
					{
						alt17 = 10;
					}
					break;
				}

				case ':':
				{
					alt17 = 12;
					break;
				}

				case ',':
				{
					alt17 = 13;
					break;
				}

				case '=':
				{
					alt17 = 14;
					break;
				}

				case '?':
				{
					alt17 = 20;
					break;
				}

				case '/':
				{
					alt17 = 21;
					break;
				}

				case '.':
				{
					int LA17_14 = input.LA(2);
					if (((LA17_14 >= '0' && LA17_14 <= '9')))
					{
						alt17 = 30;
					}
					else
					{
						alt17 = 22;
					}
					break;
				}

				case '(':
				{
					alt17 = 23;
					break;
				}

				case '%':
				{
					alt17 = 24;
					break;
				}

				case '*':
				{
					alt17 = 25;
					break;
				}

				case ')':
				{
					alt17 = 26;
					break;
				}

				case '-':
				{
					alt17 = 27;
					break;
				}

				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'G':
				case 'H':
				case 'I':
				case 'J':
				case 'K':
				case 'L':
				case 'M':
				case 'N':
				case 'O':
				case 'P':
				case 'Q':
				case 'R':
				case 'S':
				case 'T':
				case 'U':
				case 'V':
				case 'W':
				case 'X':
				case 'Y':
				case 'Z':
				case '_':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
				case 'g':
				case 'h':
				case 'i':
				case 'j':
				case 'k':
				case 'l':
				case 'm':
				case 'n':
				case 'o':
				case 'p':
				case 'q':
				case 'r':
				case 's':
				case 't':
				case 'u':
				case 'v':
				case 'w':
				case 'x':
				case 'y':
				case 'z':
				{
					alt17 = 28;
					break;
				}

				case '\t':
				case '\n':
				case '\r':
				case ' ':
				{
					alt17 = 29;
					break;
				}

				case '0':
				{
					switch (input.LA(2))
					{
						case 'X':
						case 'x':
						{
							alt17 = 32;
							break;
						}

						case '0':
						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
						{
							alt17 = 31;
							break;
						}

						default:
						{
							alt17 = 30;
							break;
						}
					}
					break;
				}

				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					alt17 = 30;
					break;
				}

				default:
				{
					NoViableAltException nvae = new NoViableAltException(string.Empty, 17, 0, input);
					throw nvae;
				}
			}
			switch (alt17)
			{
				case 1:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:10: AT_ADD
					MAT_ADD();
					break;
				}

				case 2:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:17: AT_BIT_AND
					MAT_BIT_AND();
					break;
				}

				case 3:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:28: AT_BIT_NOT
					MAT_BIT_NOT();
					break;
				}

				case 4:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:39: AT_BIT_OR
					MAT_BIT_OR();
					break;
				}

				case 5:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:49: AT_BIT_SHL
					MAT_BIT_SHL();
					break;
				}

				case 6:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:60: AT_BIT_SHR
					MAT_BIT_SHR();
					break;
				}

				case 7:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:71: AT_BIT_SHU
					MAT_BIT_SHU();
					break;
				}

				case 8:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:82: AT_BIT_XOR
					MAT_BIT_XOR();
					break;
				}

				case 9:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:93: AT_BOOL_AND
					MAT_BOOL_AND();
					break;
				}

				case 10:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:105: AT_BOOL_NOT
					MAT_BOOL_NOT();
					break;
				}

				case 11:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:117: AT_BOOL_OR
					MAT_BOOL_OR();
					break;
				}

				case 12:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:128: AT_COLON
					MAT_COLON();
					break;
				}

				case 13:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:137: AT_COMMA
					MAT_COMMA();
					break;
				}

				case 14:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:146: AT_COMP_EQ
					MAT_COMP_EQ();
					break;
				}

				case 15:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:157: AT_COMP_GT
					MAT_COMP_GT();
					break;
				}

				case 16:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:168: AT_COMP_GTE
					MAT_COMP_GTE();
					break;
				}

				case 17:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:180: AT_COMP_LT
					MAT_COMP_LT();
					break;
				}

				case 18:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:191: AT_COMP_LTE
					MAT_COMP_LTE();
					break;
				}

				case 19:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:203: AT_COMP_NEQ
					MAT_COMP_NEQ();
					break;
				}

				case 20:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:215: AT_COND_QUE
					MAT_COND_QUE();
					break;
				}

				case 21:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:227: AT_DIVIDE
					MAT_DIVIDE();
					break;
				}

				case 22:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:237: AT_DOT
					MAT_DOT();
					break;
				}

				case 23:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:244: AT_LPAREN
					MAT_LPAREN();
					break;
				}

				case 24:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:254: AT_MODULO
					MAT_MODULO();
					break;
				}

				case 25:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:264: AT_MULTIPLY
					MAT_MULTIPLY();
					break;
				}

				case 26:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:276: AT_RPAREN
					MAT_RPAREN();
					break;
				}

				case 27:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:286: AT_SUBTRACT
					MAT_SUBTRACT();
					break;
				}

				case 28:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:298: NAMESPACE_ID
					MNAMESPACE_ID();
					break;
				}

				case 29:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:311: WS
					MWS();
					break;
				}

				case 30:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:314: DECIMAL
					MDECIMAL();
					break;
				}

				case 31:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:322: OCTAL
					MOCTAL();
					break;
				}

				case 32:
				{
					// src/java/org/apache/lucene/expressions/js/Javascript.g:1:328: HEX
					MHEX();
					break;
				}
			}
		}

		protected internal JavascriptLexer.DFA9 dfa9;

		internal static readonly string DFA9_eotS = "\x1\uffff\x2\x4\x3\uffff\x1\x4";

		internal static readonly string DFA9_eofS = "\x7\uffff";

		internal static readonly string DFA9_minS = "\x3\x30\x3\uffff\x1\x30";

		internal static readonly string DFA9_maxS = "\x1\x49\x1\x30\x1\x49\x3\uffff\x1\x49";

		internal static readonly string DFA9_acceptS = "\x3\uffff\x1\x2\x1\x3\x1\x1\x1\uffff";

		internal static readonly string DFA9_specialS = "\x7\uffff}>";

		internal static readonly string[] DFA9_transitionS = new string[] { "\x1\x3\x1\uffff\x1\x1\xb\x2"
			, "\x1\x5", "\x1\x5\x1\uffff\xc\x6", string.Empty, string.Empty, string.Empty, "\x1\x5\x1\uffff\xc\x6"
			 };

		internal static readonly short[] DFA9_eot = DFA.UnpackEncodedString(DFA9_eotS);

		internal static readonly short[] DFA9_eof = DFA.UnpackEncodedString(DFA9_eofS);

	    internal static readonly char[] DFA9_min = {'.','.','.','?','?','?','.'}; //DFA.UnpackEncodedStringToUnsignedChars(DFA9_minS);

	    internal static readonly char[] DFA9_max = {'9', '.', '9', '?', '?', '?', '9'}; //DFA.UnpackEncodedStringToUnsignedChars(DFA9_maxS);

		internal static readonly short[] DFA9_accept = DFA.UnpackEncodedString(DFA9_acceptS
			);

		internal static readonly short[] DFA9_special = DFA.UnpackEncodedString(DFA9_specialS
			);

		internal static readonly short[][] DFA9_transition;

		static JavascriptLexer()
		{
			
			int numStates = DFA9_transitionS.Length;
			DFA9_transition = new short[numStates][];
			for (int i = 0; i < numStates; i++)
			{
				DFA9_transition[i] = DFA.UnpackEncodedString(DFA9_transitionS[i]);
			}
		}

		protected internal class DFA9 : DFA
		{
			public DFA9(JavascriptLexer _enclosing, BaseRecognizer recognizer)
			{
				this._enclosing = _enclosing;
				this.recognizer = recognizer;
				this.decisionNumber = 9;
				this.eot = DFA9_eot;
				this.eof = DFA9_eof;
				this.min = DFA9_min;
				this.max = DFA9_max;
				this.accept = DFA9_accept;
				this.special = DFA9_special;
				this.transition = DFA9_transition;
			}

			public override string Description
			{
			    get
			    {
			        return
			            "346:1: DECIMAL : ( DECIMALINTEGER AT_DOT ( DECIMALDIGIT )* ( EXPONENT )? | AT_DOT ( DECIMALDIGIT )+ ( EXPONENT )? | DECIMALINTEGER ( EXPONENT )? );";
			    }
			}

			private readonly JavascriptLexer _enclosing;
		}
	}

    public class ParseException:Exception
    {
        public ParseException(string message, int charPositionInLine)
        {
            
        }
    }
}
