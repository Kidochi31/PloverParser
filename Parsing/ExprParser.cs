using Plover.Scanning;
using Plover.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Plover.Scanning.TokenType;

namespace Plover.Parsing
{

    internal partial class Parser
    {
        public Expr? ParseExpression()
        {
            try
            {
                return Expression();
            }
            catch (ParseException) { return null; }
        }


        //Expression -> Using
        Expr Expression()
        {
            return Using();
        }

        //Using -> ('using' (IDENTIFIER 'as' Expression )+ 'in' )? Conditional
        Expr Using()
        {
            if (!Match(USING)) return Conditional();
            Token useToken = Previous;

            List<(Expr.Identifier, Expr)> bindings = new();

            Consume(IDENTIFIER, "Expected at least one binding in using clause", "Insert an identifier and a binding expression.", " <IDENTIFIER> as <EXPRESSION> ", "Replace expression with an identifier.", " <IDENTIFIER> as <EXPRESSION> ");
            Expr.Identifier identifier0 = new Expr.Identifier((IdentifierToken)Previous);
            Consume(AS, "Expected 'as' after identifier in using clause", "Insert an as expression.", " as <EXPRESSION> ");
            Expr expr0 = Expression();
            bindings.Add((identifier0, expr0));

            while (Match(COMMA))
            {
                Consume(IDENTIFIER, "Expected identifier after ',' in using clause", "Insert an identifier and a binding expression.", " <IDENTIFIER> as <EXPRESSION> ", "Replace expression with an identifier.", " <IDENTIFIER> as <EXPRESSION> ");
                Expr.Identifier identifier = new Expr.Identifier((IdentifierToken)Previous);
                Consume(AS, "Expected 'as' after identifier in using clause", "Insert an as expression.", " as <EXPRESSION> ");
                Expr expr = Expression();
                bindings.Add((identifier, expr));
            }

            Consume(IN, "Expected 'in' at the end of a using clause", "Insert an in expression.", " in <EXPRESSION> ");
            Expr expression = Expression();

            return new Expr.Using(useToken, bindings, expression);

        }

        //Conditional -> 'if' Conditional 'then' Conditional 'else' Conditional
        //             | 'unless' Conditional 'then' Conditional 'else' Conditional
        //             | LogicalBinary
        Expr Conditional()
        {
            //If no if, move on
            if (!Match(IF, UNLESS)) return LogicalBinary();
            Token ifToken = Previous;
            bool isUnless = ifToken.Type == UNLESS;
            //Else create a conditional expression
            Expr condition = Conditional();
            Consume(THEN, "Expected 'then' in conditional expression.", "Insert a then expression.", " then <EXPRESSION> ");
            Expr ifTrue = Conditional();
            Consume(ELSE, "Expected 'else' in conditional expression.", "Insert an else expression.", " else <EXPRESSION> ");
            Expr ifFalse = Conditional();
            if (isUnless)
                return new Expr.UnlessTernary(ifToken, condition, ifTrue, ifFalse);
            else
                return new Expr.Ternary(ifToken, condition, ifTrue, ifFalse);
        }

        //LogicalBinary -> LogicalUnary ( 'and' LogicalUnary ) *
        //               | LogicalUnary ( 'or' LogicalUnary ) *
        //               | LogicalUnary ( 'xor' LogicalUnary ) *
        Expr LogicalBinary()
        {
            Expr left = LogicalUnary();
            //and, or, xor can only chain with themselves
            if (CheckAny(AND, OR, XOR))
            {
                TokenType type = Peek().Type;
                //match and, or, and xor
                while (Match(AND, OR, XOR))
                {
                    Token previous = Previous;
                    Expr right = LogicalUnary();

                    if (previous.Type != type)
                    {
                        ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(left.GetFirstToken().StartLine, left.GetFirstToken().StartColumn, "("), new ErrorInsertSuggestion(left.GetLastToken().EndLine, left.GetLastToken().EndColumn + 1, ")")], "Add brackets around the expression.");
                        LogError(previous, previous, "Logical operators can only chain with themselves.", suggestion);
                    }

                    left = new Expr.Binary(previous, left, right);
                }
            }

            return left;
        }

        //LogicalUnary -> LogicalNot | Comparison
        //LogicalNot -> 'not' LogicalNot | Prefix
        // For error checking, it is parsed as if it was LogicalNot -> 'not' LogicalBinary | Prefix ;
        Expr LogicalUnary()
        {
            if (Match(NOT))
            {
                Token token = Previous;

                //
                Expr right = LogicalBinary();
                //if not is not followed by a prefix expression (or another not)
                //then this is invalid
                // i.e. cannot be followed by a using expression, if/unless expression, or any binary/N-ary expression
                // but using, if, and unless will automatically result in an error anyway
                if (right is Expr.Binary || right is Expr.NAry)
                {
                    ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(right.GetFirstToken().StartLine, right.GetFirstToken().StartColumn, "("), new ErrorInsertSuggestion(right.GetLastToken().EndLine, right.GetLastToken().EndColumn + 1, ")")], "Add brackets around the expression.");
                    LogError(token, token, "not must be followed by a not or prefix expression.", suggestion);
                }

                return new Expr.Unary(token, right);
            }

            return Comparison();
        }

        //Comparison -> Numeric ( '==' Numeric ) *
        //            | Numeric ( '!=' Numeric )
        //            | Numeric ( ( '<' | '<=' ) Numeric ) *
        //            | Numeric ( ( '>' | '>=' ) Numeric ) *
        Expr Comparison()
        {
            Expr left = Numeric();
            //!= can only chain with themselves
            if (Match(BANG_EQUAL))
            {
                Token previous = Previous;
                Expr right = Numeric();
                left = new Expr.NAry(previous, left, right);

                //Match all further ==, !=, <, <=, >, >=, but error
                while (Match(EQUAL_EQUAL, BANG_EQUAL, LT, LT_EQUAL, GT, GT_EQUAL))
                {
                    previous = Previous;
                    right = Numeric();
                    left = new Expr.NAry(previous, left, right);

                    LogError(previous, previous, "Cannot chain != with other comparisons.");


                }
            }
            //==  can only chain with itself
            if (CheckAny(EQUAL_EQUAL))
            {
                //Match all further ==, !=, <, <=, >, >=
                while (Match(EQUAL_EQUAL, BANG_EQUAL, LT, LT_EQUAL, GT, GT_EQUAL))
                {
                    Token previous = Previous;
                    Expr right = Numeric();
                    left = new Expr.NAry(previous, left, right);
                    if (previous.Type != EQUAL_EQUAL)
                    {
                        LogError(previous, previous, "Can only chain == with itself.");
                    }
                }
            }
            //< and <= can chain together
            else if (CheckAny(LT, LT_EQUAL))
            {
                //Match all further =, !=, <, <=, >, >=
                while (Match(EQUAL_EQUAL, BANG_EQUAL, LT, LT_EQUAL, GT, GT_EQUAL))
                {
                    Token previous = Previous;
                    Expr right = Numeric();
                    left = new Expr.NAry(previous, left, right);

                    if (previous.Type != LT && previous.Type != LT_EQUAL)
                    {
                        LogError(previous, previous, "< and <= can only chain with each other.");
                    }
                }
            }
            //> and >= can chain together
            else if (CheckAny(GT, GT_EQUAL))
            {
                //Match all further =, !=, <, <=, >, >=
                while (Match(EQUAL_EQUAL, BANG_EQUAL, LT, LT_EQUAL, GT, GT_EQUAL))
                {
                    Token previous = Previous;
                    Expr right = Numeric();
                    left = new Expr.NAry(previous, left, right);

                    if (previous.Type != GT && previous.Type != GT_EQUAL)
                    {
                        LogError(previous, previous, "> and >= can only chain with each other.");
                    }
                }
            }
            return left;
        }

        //Numeric -> Bitwise | Term
        //This is parsed as if Numeric -> Bitwise
        Expr Numeric()
        {
            return Bitwise();
        }

        // Bitwise -> Prefix ( '&' Prefix ) *
        //          | Prefix ( '|' Prefix ) *
        //          | Prefix ( '^' Prefix ) *
        //          | Prefix ( ( '<<' | '>>' Prefix ) ?

        //This is parsed as if Prefix is Term, but
        //non-trivial Term and Factor productions result
        //in a parser error
        Expr Bitwise()
        {
            bool termOrFactorFound = false;
            bool bitwiseFound = false;
            Expr left = Term(ref termOrFactorFound);
            Token previous = null;
            //&, |, ^ can be repeated with themselves
            if (CheckAny(AMPERSAND, BAR, CARET))
            {
                bitwiseFound = true;
                TokenType type = Peek().Type;
                //match all further &, |, ^, <<, >>
                while (Match(AMPERSAND, BAR, CARET, LT_LT, GT_GT))
                {
                    previous = Previous;
                    Expr right = Term(ref termOrFactorFound);


                    if (previous.Type != type)
                    {
                        ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(left.GetFirstToken().StartLine, left.GetFirstToken().StartColumn, "("), new ErrorInsertSuggestion(left.GetLastToken().EndLine, left.GetLastToken().EndColumn + 1, ")")], "Add brackets around the expression.");
                        LogError(previous, previous, "Cannot mix bitwise operators &, |, ^, <<, and >> without parentheses.", suggestion);
                    }
                    left = new Expr.Binary(previous, left, right);
                }
            }
            else if (Match(LT_LT, GT_GT))
            {
                bitwiseFound = true;
                previous = Previous;
                Expr right = Term(ref termOrFactorFound);
                left = new Expr.Binary(previous, left, right);

                //match all further &, |, ^, <<, >>
                while (Match(AMPERSAND, BAR, CARET, LT_LT, GT_GT))
                {
                    previous = Previous;
                    right = Term(ref termOrFactorFound);


                    ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(left.GetFirstToken().StartLine, left.GetFirstToken().StartColumn, "("), new ErrorInsertSuggestion(left.GetLastToken().EndLine, left.GetLastToken().EndColumn + 1, ")")], "Add brackets around the expression.");
                    LogError(previous, previous, "Cannot chain '>>' or '<<' with any bitwise operator.", suggestion);

                    left = new Expr.Binary(previous, left, right);
                }
            }

            if (termOrFactorFound && bitwiseFound)
            {
                LogError(previous, previous, "Cannot mix bitwise operators with arithmetic operators. Use brackets to separate the expressions.");
            }
            return left;
        }

        //Term -> Factor ( ( '+' | '-' ) Factor )*
        Expr Term(ref bool termOrFactorFound)
        {
            Expr left = Factor(ref termOrFactorFound);
            while (Match(PLUS, MINUS))
            {
                termOrFactorFound = true;
                Token previous = Previous;
                Expr right = Factor(ref termOrFactorFound);
                left = new Expr.Binary(previous, left, right);
            }

            return left;
        }

        //Factor -> Prefix ( ( '/' | '*' ) Prefix )*
        Expr Factor(ref bool termOrFactorFound)
        {
            Expr left = Prefix();
            while (Match(FORWARD_SLASH, STAR))
            {
                termOrFactorFound = true;
                Token previous = Previous;
                Expr right = Prefix();
                left = new Expr.Binary(previous, left, right);
            }

            return left;
        }

        //Prefix -> ( '+' | '-' | '~' ) Prefix | Postfix
        Expr Prefix()
        {
            if (Match(PLUS, MINUS, TILDE))
            {
                Token token = Previous;
                Expr right = Prefix();
                return new Expr.Unary(token, right);
            }

            return Postfix();
        }

        //Postfix -> Primary ( '(' FunctionArguments ')' )*
        Expr Postfix()
        {
            Expr expr = Primary();

            while (Match(LEFT_PAREN))
            {
                //Function call
                List<Expr> args = FunctionArguments();
                Consume(RIGHT_PAREN, "Expected ')' after function arguments", "Add ')' after function arguments", ")");
                Token rightParen = Previous;
                expr = new Expr.FunctionCall(expr, args, rightParen);
            }

            return expr;
        }

        //FunctionArguments -> ( Expression ( ','  Expression )* )?
        List<Expr> FunctionArguments()
        {
            List<Expr> args = new List<Expr>();
            if (Check(RIGHT_PAREN)) return args;
            args.Add(Expression());

            while (Match(COMMA))
            {
                args.Add(Expression());
            }
            return args;
        }

        //Primary -> 'true' | 'false' | IDENTIFIER
        //         | NUMBER_LITERAL | STRING_LITERAL
        //         | CHAR_LITERAL
        Expr Primary()
        {
            if (Match(TRUE)) return new Expr.Literal((LiteralToken)Previous);
            if (Match(FALSE)) return new Expr.Literal((LiteralToken)Previous);
            if (Match(IDENTIFIER)) return new Expr.Identifier((IdentifierToken)Previous);
            if (Match(INTEGER, FLOAT)) return new Expr.Literal((LiteralToken)Previous);
            if (Match(STRING)) return new Expr.Literal((LiteralToken)Previous);
            if (Match(CHAR)) return new Expr.Literal((LiteralToken)Previous);
            if (Match(CUSTOM)) return new Expr.Literal((LiteralToken)Previous);

            // parse using, if, and unless statements here, but log an error as this is not allowed
            Token next = Peek();
            if (next.Type == USING)
            {
                Expr conditional = Using();
                Token firstToken = conditional.GetFirstToken();
                Token lastToken = conditional.GetLastToken();
                ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(firstToken.StartLine, firstToken.StartColumn, "("), new ErrorInsertSuggestion(lastToken.EndLine, lastToken.EndColumn + 1, ")")], "Add brackets around the using expression.");
                LogError(firstToken, lastToken, "using expression must be used at the top level, or with brackets", suggestion);
                return conditional;
            }
            if (next.Type == IF)
            {
                Expr conditional = Conditional();
                Token firstToken = conditional.GetFirstToken();
                Token lastToken = conditional.GetLastToken();
                ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(firstToken.StartLine, firstToken.StartColumn, "("), new ErrorInsertSuggestion(lastToken.EndLine, lastToken.EndColumn + 1, ")")], "Add brackets around the if expression.");
                LogError(firstToken, lastToken, "if expression must be used at the top level, or with brackets", suggestion);
                return conditional;
            }
            if (next.Type == UNLESS)
            {
                Expr conditional = Conditional();
                Token firstToken = conditional.GetFirstToken();
                Token lastToken = conditional.GetLastToken();
                Console.WriteLine(lastToken.EndColumn);
                ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(firstToken.StartLine, firstToken.StartColumn, "("), new ErrorInsertSuggestion(lastToken.EndLine, lastToken.EndColumn + 1, ")")], "Add brackets around the unless expression.");
                LogError(firstToken, lastToken, "unless expression must be used at the top level, or with brackets", suggestion);
                return conditional;
            }

            //if (Match(UNDERSCORE)) return new Expr.Underscore(Previous);

            if (Match(LEFT_PAREN))
            {
                Token paren1 = Previous;
                Expr expr = Expression();
                Consume(RIGHT_PAREN, "Expected ')' after expression.", "Add ')' after expression", ")");
                Token paren2 = Previous;
                return new Expr.Grouping(paren1, paren2, expr);
            }

            // error -> no expression
            Token nextToken = Peek();
            throw PanicError(nextToken, nextToken, "Expected expression.");
        }
    }
}
