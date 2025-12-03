using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Parsing
{
    internal abstract record class TypeExpr
    {
        public abstract Token GetFirstToken();
        public abstract Token GetLastToken();

        public abstract override string ToString();

        public record class And(TypeExpr Left, TypeExpr Right) : TypeExpr
        {
            public override Token GetFirstToken() => Left.GetFirstToken();

            public override Token GetLastToken() => Right.GetLastToken();

            public override string ToString() => $"({Left}) & ({Right})";

            
        }

        public record class Or(TypeExpr Left, TypeExpr Right) : TypeExpr
        {
            public override Token GetFirstToken() => Left.GetFirstToken();

            public override Token GetLastToken() => Right.GetLastToken();
            public override string ToString() => $"({Left}) | ({Right})";
        }

        public record class Refinement(TypeExpr Type, Expr RefinementExpr, Token lastBracket) : TypeExpr
        {
            public override Token GetFirstToken() => Type.GetFirstToken();

            public override Token GetLastToken() => lastBracket;
            public override string ToString() => $"({Type})[{RefinementExpr}]";
        }

        public record class NamedType(IdentifierToken Token) : TypeExpr
        {
            public override Token GetFirstToken() => Token;

            public override Token GetLastToken() => Token;
            public override string ToString() => $"{Token.IdentifierName}";
        }

        public record class Grouping(Token LeftParen, TypeExpr Type, Token RightParen) : TypeExpr
        {
            public override Token GetFirstToken() => LeftParen;

            public override Token GetLastToken() => RightParen;
            public override string ToString() => $"({Type})";
        }
    }
}
