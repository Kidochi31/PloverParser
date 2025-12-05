using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Parsing
{
    internal abstract record class Decl
    {
        public abstract Token GetFirstToken();
        public abstract Token GetLastToken();

        public override abstract string ToString();

        internal record class FnParam(IdentifierToken Identifier, TypeExpr Type)
        {
            public override string ToString() => $"{Identifier.IdentifierName} : ({Type})";
        }

        public record class Function(Token FnToken, IdentifierToken Name, List<FnParam> Parameters, TypeExpr? ReturnType, Stmt Body) : Decl
        {
            public override Token GetFirstToken() => FnToken;
            public override Token GetLastToken() => Body.GetLastToken();
            public override string ToString() => $"fn {Name.IdentifierName}({string.Join(", ",from param in Parameters select param.ToString())}){(ReturnType is null ? "" : $" -> ({ReturnType})")} {Body}";
        }
    }

    
}
