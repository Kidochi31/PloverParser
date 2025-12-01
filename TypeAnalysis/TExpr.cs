using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plover.Environment;
using Plover.Scanning;

namespace Plover.TypeAnalysis
{
    internal abstract record class TExpr(EnvironmentState Environment)
    {
        public override abstract string ToString();

        // Types of expressions:
        // Function call
        // Operator functions
        // Conditional
        // Constant
        // Variable Reading
        // Variable Writing


        public record class FunctionCall(EnvironmentState Environment, TExpr? Function, List<TExpr?> Arguments): TExpr(Environment)
        {
            public override string ToString() => $"call ({Function?.ToString() ?? "null"})({string.Join(", ", from arg in Arguments select arg?.ToString() ?? "null")})";
        }

        public record class OperatorFunction(EnvironmentState Environment, TokenType OperatorType, int Args) : TExpr(Environment)
        {
            public override string ToString() => $"function {OperatorType}:{Args}";
        }

        public record class Conditional(EnvironmentState Environment, TExpr? Condition, TExpr? IfTrue, TExpr? IfFalse): TExpr(Environment)
        {
            public override string ToString() => $"if ({Condition?.ToString() ?? "null"}) then ({IfTrue?.ToString() ?? "null"}) else ({IfFalse?.ToString() ?? "null"})";
        }

        public record class Constant(EnvironmentState Environment, object Value) : TExpr(Environment)
        {
            public override string ToString() => $"constant {Value}";
        }

        public record class VariableRead(EnvironmentState Environment, Variable Variable) : TExpr(Environment)
        {
            public override string ToString() => $"{Variable.Name} in {Environment.GetEnvironmentName()}";
        }

        public record class VariableWrite(EnvironmentState Environment, Variable Variable, TExpr? Value, TExpr? Evaluate) : TExpr(Environment)
        {
            public override string ToString() => $"({Variable.Name} in {Environment.GetEnvironmentName()} <- ({Value?.ToString() ?? "null"})) then return ({Evaluate?.ToString() ?? "null"})";
        }
    }
}
