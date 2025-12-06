using Plover.Environment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Plover.EnvironmentAnalysis
{
    internal abstract record class EnvTypeExpr
    {

        public abstract override string ToString();

        public record class And(EnvTypeExpr Left, EnvTypeExpr Right) : EnvTypeExpr
        {

            public override string ToString() => $"({Left}) & ({Right})";

            
        }
        public record class Or(EnvTypeExpr Left, EnvTypeExpr Right) : EnvTypeExpr
        {
            public override string ToString() => $"({Left}) | ({Right})";
        }

        public record class Refinement(EnvTypeExpr BaseType, EnvExpr RefinementExpr) : EnvTypeExpr
        {
            public override string ToString() => $"({BaseType})[{RefinementExpr}]";
        }

        public record class NamedType(TypeVariable Variable) : EnvTypeExpr
        {
            public override string ToString() => $"{Variable}";
        }

        public record class TypeOf(EnvExpr Expression) : EnvTypeExpr
        {
            public override string ToString() => $"typeof({Expression})";
        }

        public record class Function(List<EnvTypeExpr> Input, EnvTypeExpr? Output) : EnvTypeExpr
        {
            public override string ToString() => $"fn({string.Join(", ", from t in Input select t.ToString())}) -> {Output?.ToString() ?? "unit"}";
        }
    }
}
