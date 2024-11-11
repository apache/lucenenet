/*
    Javascript.g
    An expression syntax based on ECMAScript/Javascript.

    This file was adapted from a general ECMAScript language definition at http://research.xebic.com/es3.
    The major changes are the following:
        * Stripped grammar of all parts not relevant for expression syntax.
        * Stripped grammar of unicode character support.
        * Added override function for customized error handling.
        * Renaming of many grammar rules.
        * Removal of annotations no longer relevant for stripped pieces.

    The Original Copyright Notice is the following:

        Copyrights 2008-2009 Xebic Reasearch BV. All rights reserved..
        Original work by Patrick Hulsmeijer.

        This ANTLR 3 LL(*) grammar is based on Ecma-262 3rd edition (JavaScript 1.5, JScript 5.5).
        The annotations refer to the "A Grammar Summary" section (e.g. A.1 Lexical Grammar)
        and the numbers in parenthesis to the paragraph numbers (e.g. (7.8) ).
        This document is best viewed with ANTLRWorks (www.antlr.org).

        Software License Agreement (BSD License)

        Copyright (c) 2008-2010, Xebic Research B.V.
        All rights reserved.

        Redistribution and use of this software in source and binary forms, with or without modification, are
        permitted provided that the following conditions are met:

            * Redistributions of source code must retain the above
              copyright notice, this list of conditions and the
              following disclaimer.

            * Redistributions in binary form must reproduce the above
              copyright notice, this list of conditions and the
              following disclaimer in the documentation and/or other
              materials provided with the distribution.

            * Neither the name of Xebic Research B.V. nor the names of its
              contributors may be used to endorse or promote products
              derived from this software without specific prior
              written permission of Xebic Research B.V.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
        WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
        PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
        ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
        LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
        INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
        TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
        ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

// ***********************************************************************
// * ANTLRv4 grammar for Lucene expression language.
//
// LUCENENET-specific: ported from ANTLRv3 grammar. Inline C# code is no
// longer necessary for error handling.
// ***********************************************************************

grammar Javascript;

options {
    language = CSharp;
}

// ***********************************************************************
// * Parser Rules
// ***********************************************************************

expression
    : conditional EOF
    ;

conditional
    : logical_or (AT_COND_QUE conditional AT_COLON conditional)?
    ;

logical_or
    : logical_and (AT_BOOL_OR logical_and)*
    ;

logical_and
    : bitwise_or (AT_BOOL_AND bitwise_or)*
    ;

bitwise_or
    : bitwise_xor (AT_BIT_OR bitwise_xor)*
    ;

bitwise_xor
    : bitwise_and (AT_BIT_XOR bitwise_and)*
    ;

bitwise_and
    :  equality (AT_BIT_AND equality)*
    ;

equality
    : relational ((AT_COMP_EQ | AT_COMP_NEQ) relational)*
    ;

relational
    : shift ((AT_COMP_LT | AT_COMP_GT | AT_COMP_LTE | AT_COMP_GTE) shift)*
    ;

shift
    : additive ((AT_BIT_SHL | AT_BIT_SHR | AT_BIT_SHU) additive)*
    ;

additive
    : multiplicative ((AT_ADD | AT_SUBTRACT) multiplicative)*
    ;

multiplicative
    : unary ((AT_MULTIPLY | AT_DIVIDE | AT_MODULO) unary)*
    ;

unary
    : postfix
    | AT_ADD unary
    | unary_operator unary
    ;

unary_operator
    : AT_SUBTRACT
    | AT_BIT_NOT
    | AT_BOOL_NOT
    ;

postfix
    : primary
    | call      // LUCENENET-specific: see note for call rule below (was AT_CALL)
    ;

// LUCENENET-specific: previously used blank AT_CALL token to detect function calls,
// but this is not necessary in ANTLRv4. Instead, we can just make it a new rule
call
    : NAMESPACE_ID arguments?
    ;

primary
    : NAMESPACE_ID
    | numeric
    | AT_LPAREN conditional AT_RPAREN
    ;

arguments
    : AT_LPAREN (conditional (AT_COMMA conditional)*)? AT_RPAREN
    ;

numeric
    : HEX | OCTAL | DECIMAL
    ;

// ***********************************************************************
// * Lexer Rules
// ***********************************************************************

AT_LPAREN          : '('  ;
AT_RPAREN          : ')'  ;
AT_DOT             : '.'  ;
AT_COMMA           : ','  ;
AT_COLON           : ':'  ;

AT_COMP_LT         : '<'  ;
AT_COMP_LTE        : '<=' ;
AT_COMP_EQ         : '==' ;
AT_COMP_NEQ        : '!=' ;
AT_COMP_GTE        : '>=' ;
AT_COMP_GT         : '>'  ;

AT_BOOL_NOT        : '!'  ;
AT_BOOL_AND        : '&&' ;
AT_BOOL_OR         : '||' ;
AT_COND_QUE        : '?'  ;

// AT_NEGATE                 ;
AT_ADD             : '+'  ;
AT_SUBTRACT        : '-'  ;
AT_MULTIPLY        : '*'  ;
AT_DIVIDE          : '/'  ;
AT_MODULO          : '%'  ;

AT_BIT_SHL         : '<<' ;
AT_BIT_SHR         : '>>' ;
AT_BIT_SHU         : '>>>';
AT_BIT_AND         : '&'  ;
AT_BIT_OR          : '|'  ;
AT_BIT_XOR         : '^'  ;
AT_BIT_NOT         : '~'  ;

// AT_CALL                   ;

NAMESPACE_ID
    : ID (AT_DOT ID)*
    ;

fragment
ID
    : ('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')*
    ;

WS
    : (' '|'\t'|'\n'|'\r')+ -> skip
    ;

DECIMAL
    : DECIMALINTEGER AT_DOT DECIMALDIGIT* EXPONENT?
    | AT_DOT DECIMALDIGIT+ EXPONENT?
    | DECIMALINTEGER EXPONENT?
    ;

OCTAL
    : '0' OCTALDIGIT+
    ;

HEX
    : ('0x'|'0X') HEXDIGIT+
    ;

fragment
DECIMALINTEGER
    : '0'
    | '1'..'9' DECIMALDIGIT*
    ;

fragment
EXPONENT
    : ('e'|'E') ('+'|'-')? DECIMALDIGIT+
    ;

fragment
DECIMALDIGIT
    : '0'..'9'
    ;

fragment
HEXDIGIT
    : DECIMALDIGIT
    | 'a'..'f'
    | 'A'..'F'
    ;

fragment
OCTALDIGIT
    : '0'..'7'
    ;
