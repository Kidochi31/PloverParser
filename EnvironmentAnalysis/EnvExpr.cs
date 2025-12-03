using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plover.Environment;
using Plover.Scanning;

namespace Plover.EnvironmentAnalysis
{
    internal abstract record class EnvExpr(EnvironmentState Environment)
    {
        public override abstract string ToString();

        // Types of expressions:
        // Function call
        // Operator functions
        // Conditional
        // Constant
        // Literal
        // Variable Reading
        // Variable Writing


        public record class FunctionCall(EnvironmentState Environment, EnvExpr? Function, List<EnvExpr?> Arguments): EnvExpr(Environment)
        {
            public override string ToString() => $"call ({Function?.ToString() ?? "null"})({string.Join(", ", from arg in Arguments select arg?.ToString() ?? "null")})";
        }

        public record class OperatorFunction(EnvironmentState Environment, TokenType OperatorType, int Args) : EnvExpr(Environment)
        {
            public override string ToString() => $"function {OperatorType}:{Args}";
        }

        public record class Conditional(EnvironmentState Environment, EnvExpr? Condition, EnvExpr? IfTrue, EnvExpr? IfFalse): EnvExpr(Environment)
        {
            public override string ToString() => $"if ({Condition?.ToString() ?? "null"}) then ({IfTrue?.ToString() ?? "null"}) else ({IfFalse?.ToString() ?? "null"})";
        }

        public record class Constant(EnvironmentState Environment, object Value) : EnvExpr(Environment)
        {
            public override string ToString() => $"constant {Value}";
        }

        public record class Literal(EnvironmentState Environment, LiteralToken token) : EnvExpr(Environment)
        {
            public override string ToString() => $"constant {token.Lexeme}";
        }

        public record class VariableRead(EnvironmentState Environment, Variable Variable) : EnvExpr(Environment)
        {
            public override string ToString() => $"{Variable.Name} in {Environment.GetEnvironmentName()}";
        }

        public record class VariableWrite(EnvironmentState Environment, Variable Variable, EnvExpr? Value, EnvExpr? Evaluate) : EnvExpr(Environment)
        {
            public override string ToString() => $"({Variable.Name} in {Environment.GetEnvironmentName()} <- ({Value?.ToString() ?? "null"})) then return ({Evaluate?.ToString() ?? "null"})";
        }
    }
}
