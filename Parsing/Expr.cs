using Plover.Scanning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Parsing
{
    internal abstract class Expr
    {
        public abstract Token GetFirstToken();
        public abstract Token GetLastToken();

        public override abstract string ToString();

        public class Literal(LiteralToken token) : Expr
        {
            public LiteralToken Token = token;

            public override Token GetFirstToken() => Token;
            public override Token GetLastToken() => Token;

            public override string ToString() => $"{Token.Lexeme}:{Token.GetType().Name}";
        }

        public class Identifier(IdentifierToken token) : Expr
        {
            public IdentifierToken Token = token;
            public override Token GetFirstToken() => Token;
            public override Token GetLastToken() => Token;
            public override string ToString() => $"{Token.IdentifierName}";
        }

        public class Unary(Token @operator, Expr right) : Expr
        {
            public Token Operator = @operator;
            public Expr Right = right;
            public override Token GetFirstToken() => Operator;
            public override Token GetLastToken() => Right.GetLastToken();
            public override string ToString() => $"{Operator.Lexeme} ({Right})";
        }

        public class Binary(Token @operator, Expr left, Expr right) : Expr
        {
            public readonly Token Operator = @operator;
            public readonly Expr Right = right;
            public readonly Expr Left = left;
            public override Token GetFirstToken() => Left.GetFirstToken();
            public override Token GetLastToken() => Right.GetLastToken();
            public override string ToString() => $"({Left}) {Operator.Lexeme} ({Right})";
        }

        public class UnlessTernary(Token unlessToken, Expr condition, Expr ifTrue, Expr ifFalse) : Expr
        {
            public readonly Token UnlessToken = unlessToken;
            public readonly Expr Condition = condition;
            public readonly Expr IfTrue = ifTrue;
            public readonly Expr IfFalse = ifFalse;

            public override Token GetFirstToken() => UnlessToken;
            public override Token GetLastToken() => IfFalse.GetLastToken();

            public override string ToString() => $"unless ({Condition}) then ({IfTrue}) else ({IfFalse})";
        }

        public class Ternary(Token ifToken, Expr condition, Expr ifTrue, Expr ifFalse) : Expr
        {
            public readonly Token IfToken = ifToken;
            public readonly Expr Condition = condition;
            public readonly Expr IfTrue = ifTrue;
            public readonly Expr IfFalse = ifFalse;

            public override Token GetFirstToken() => IfToken;
            public override Token GetLastToken() => IfFalse.GetLastToken();

            public override string ToString() => $"if ({Condition}) then ({IfTrue}) else ({IfFalse})";
        }

        public class NAry : Expr
        {
            public readonly Expr Start;
            public readonly List<(Token Operator, Expr Expr)> Operations;
            public NAry(Token token, Expr left, Expr right)
            {
                //If left is NAry -> merge lists
                if (left is NAry n)
                {
                    Start = n.Start;
                    Operations = [.. n.Operations];
                    Operations.Add((token, right));
                }
                else //If left is not NAry -> left is start
                {
                    Start = left;
                    Operations = [(token, right)];
                }
            }

            public override Token GetFirstToken() => Start.GetFirstToken();
            public override Token GetLastToken() => Operations.Last().Expr.GetLastToken();

            public override string ToString() => $"({Start.ToString()}) " + string.Join(" ", from op in Operations select $"{op.Operator.Lexeme} ({op.Expr})");
        }

        public class Grouping(Token paren1, Token paren2, Expr expr) : Expr
        {
            public readonly Expr Expr = expr;

            public override Token GetFirstToken() => paren1;
            public override Token GetLastToken() => paren2;

            public override string ToString() => $"({Expr})";
        }

        public class Using(Token use, List<(Expr.Identifier identifier, Expr expr)> bindings, Expr expression) : Expr
        {
            public readonly Token Use = use;
            public readonly Expr Expr = expression;
            public readonly List<(Expr.Identifier identifier, Expr expr)> Bindings = bindings;

            public override Token GetFirstToken() => Use;
            public override Token GetLastToken() => Expr.GetLastToken();

            public override string ToString() => "using " + string.Join(", ", from b in Bindings select $"{b.identifier} as ({b.expr})") + $" in ({Expr})";
        }

        public class FunctionCall(Expr function, List<Expr> arguments, Token finalParen) : Expr
        {
            public readonly Expr Function = function;
            public readonly List<Expr> Arguments = arguments;
            public readonly Token FinalParen = finalParen;

            public override Token GetFirstToken() => Function.GetFirstToken();
            public override Token GetLastToken() => FinalParen;

            public override string ToString() => $"call ({Function})(" +  string.Join(", ", from a in Arguments select $"({a})") + ")";
        }
    }
}
