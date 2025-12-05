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
    internal abstract record class Stmt
    {
        public abstract Token GetFirstToken();
        public abstract Token GetLastToken();

        public override abstract string ToString();

        public record class Print(Token PrintToken, Expr Expression, Token Semicolon) : Stmt
        {
            public override Token GetFirstToken() => PrintToken;
            public override Token GetLastToken() => Semicolon;

            public override string ToString() => $"print ({Expression}) ;";
        };

        public record class Return(Token ReturnToken, Expr? Expression, Token Semicolon) : Stmt
        {
            public override Token GetFirstToken() => ReturnToken;
            public override Token GetLastToken() => Semicolon;

            public override string ToString() => $"return ({Expression}) ;";
        };

        public record class LocVarDec(Token LetToken, IdentifierToken Identifier, TypeExpr Type, Expr? Value, Token Semicolon) : Stmt
        {
            public override Token GetFirstToken() => LetToken;
            public override Token GetLastToken() => Semicolon;

            public override string ToString() => $"let {Identifier.IdentifierName} : ({Type}) {(Value is null ? "" : $"<- ({Value})")} ;";
        };

        public record class Assignment(Expr Target, Expr Value, Token Semicolon) : Stmt
        {
            public override Token GetFirstToken() => Target.GetFirstToken();
            public override Token GetLastToken() => Semicolon;

            public override string ToString() => $"{Target} <- ({Value}) ;";
        };

        public record class ExprStmt(Expr Expression, Token Semicolon) : Stmt
        {
            public override Token GetFirstToken() => Expression.GetFirstToken();
            public override Token GetLastToken() => Semicolon;

            public override string ToString() => $"{Expression} ;";
        };

        public record class If(Token IfToken, Expr Condition, Stmt ifTrue, Stmt? ifFalse) : Stmt
        {
            public override Token GetFirstToken() => IfToken;
            public override Token GetLastToken() => ifFalse is null ? ifTrue.GetLastToken() : ifFalse.GetLastToken();

            public override string ToString() => $"if ({Condition}) then\n{{{ifTrue}}}\nelse\n{{{ifFalse}}}";
        };

        public record class Block(Token OpenBrace, List<Stmt> Body, Token CloseBrace) : Stmt
        {
            public override Token GetFirstToken() => OpenBrace;
            public override Token GetLastToken() => CloseBrace;

            public override string ToString() => $"{{\n{string.Join("\n", from stmt in Body select stmt.ToString())}\n}}";
        }
    }
}
