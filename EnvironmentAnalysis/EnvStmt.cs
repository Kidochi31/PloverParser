using Plover.Environment;
using Plover.Scanning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Plover.EnvironmentAnalysis
{
    internal abstract record class EnvStmt
    {

        public override abstract string ToString();

        public record class Print(EnvExpr Expression) : EnvStmt
        {

            public override string ToString() => $"print ({Expression}) ;";
        };

        public record class Return(EnvExpr? Expression) : EnvStmt
        {

            public override string ToString() => $"return ({Expression}) ;";
        };

        public record class Assignment(Variable Target, EnvExpr Value) : EnvStmt
        {
            public override string ToString() => $"{Target} <- ({Value}) ;";
        };

        public record class ExprStmt(EnvExpr Expression) : EnvStmt
        {
            public override string ToString() => $"{Expression} ;";
        };

        public record class If(EnvExpr Condition, EnvStmt ifTrue, EnvStmt? ifFalse) : EnvStmt
        {
            public override string ToString() => $"if ({Condition}) then\n{{{ifTrue}}}\nelse\n{{{ifFalse}}}";
        };

        public record class Block(List<EnvStmt> Body) : EnvStmt
        {
            public override string ToString() => $"{{\n{string.Join("\n", from stmt in Body select stmt.ToString())}\n}}";
        }
    }
}
