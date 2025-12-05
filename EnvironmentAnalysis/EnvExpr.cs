using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plover.Environment;
using Plover.Scanning;

namespace Plover.EnvironmentAnalysis
{
    internal abstract record class EnvExpr()
    {
        public override abstract string ToString();

        // Types of expressions:
        // Function call
        // Conditional
        // Constant
        // Literal
        // Variable Reading
        // Using Bindings


        public record class FunctionCall(EnvExpr Function, List<EnvExpr> Arguments): EnvExpr()
        {
            public override string ToString() => $"call ({Function})({string.Join(", ", from arg in Arguments select arg.ToString())})";
        }

        public record class Conditional(EnvExpr Condition, EnvExpr IfTrue, EnvExpr IfFalse): EnvExpr()
        {
            public override string ToString() => $"if ({Condition}) then ({IfTrue}) else ({IfFalse})";
        }

        public record class Constant(object Value) : EnvExpr()
        {
            public override string ToString() => $"constant {Value}";
        }

        public record class Literal(LiteralToken token) : EnvExpr()
        {
            public override string ToString() => $"literal {token.Lexeme}";
        }

        public record class VariableRead(Variable Variable) : EnvExpr()
        {
            public override string ToString() => Variable.ToString();
        }

        public record class UsingBindings(List<(Variable Variable, EnvExpr Value)> Bindings, EnvExpr Evaluate) : EnvExpr()
        {
            public override string ToString() => $"using ({string.Join(", ", from binding in Bindings select $"{binding.Variable} <- ({binding.Value})")}) evaluate ({Evaluate})";
        }
    }
}
