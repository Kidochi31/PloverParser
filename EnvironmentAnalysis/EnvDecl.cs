using Plover.EnvironmentAnalysis;
using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Environment
{
    internal abstract record class EnvDecl()
    {

        public override abstract string ToString();

        public record class FnParam(Variable Variable, EnvTypeExpr Type)
        {
            public override string ToString() => $"{Variable}: ({Type})";
        }


        public record class Function(Variable Name, List<FnParam> Parameters, EnvTypeExpr? ReturnType, EnvStmt Body) : EnvDecl()
        {
            public override string ToString() => $"fn {Name}({string.Join(", ",from param in Parameters select param.ToString())}){(ReturnType is null ? "" : $" -> ({ReturnType})")} {Body}";
        }
    }

    
}
