using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plover.Environment;
using Plover.Scanning;
using Plover.Typing;

namespace Plover.TypeAnalysis
{
    internal abstract record class TExpr(TypedEnvironment Environment, ExprType Type)
    {
        public override abstract string ToString();

        // Types of expressions:
        // Function call
        // Operator functions
        // Conditional
        // Constant
        // Variable Reading
        // Variable Writing


        public record class FunctionCall(TypedEnvironment Environment, TExpr? Function, List<TExpr?> Arguments, ExprType Type): TExpr(Environment, Type)
        {
            public override string ToString() => $"call ({Function?.ToString() ?? "null"})({string.Join(", ", from arg in Arguments select arg?.ToString() ?? "null")})";
        }

        public record class OperatorFunction(TypedEnvironment Environment, TokenType OperatorType, int Args, ExprType Type) : TExpr(Environment, Type)
        {
            public override string ToString() => $"function {OperatorType}:{Args}";
        }

        public record class Conditional(TypedEnvironment Environment, TExpr? Condition, TExpr? IfTrue, TExpr? IfFalse, ExprType Type) : TExpr(Environment, Type)
        {
            public override string ToString() => $"if ({Condition?.ToString() ?? "null"}) then ({IfTrue?.ToString() ?? "null"}) else ({IfFalse?.ToString() ?? "null"})";
        }

        public record class Constant(TypedEnvironment Environment, object Value, ExprType Type) : TExpr(Environment, Type)
        {
            public override string ToString() => $"constant {Value}";
        }

        public record class VariableRead(TypedEnvironment Environment, TypedVariable Variable, ExprType Type) : TExpr(Environment, Type)
        {
            public override string ToString() => $"{Variable.UnderlyingVariable.Name} in {Environment.GetEnvironmentName()}";
        }

        public record class VariableWrite(TypedEnvironment Environment, TypedVariable Variable, TExpr? Value, TExpr? Evaluate, ExprType Type) : TExpr(Environment, Type)
        {
            public override string ToString() => $"({Variable.UnderlyingVariable.Name} in {Environment.GetEnvironmentName()} <- ({Value?.ToString() ?? "null"})) then return ({Evaluate?.ToString() ?? "null"})";
        }
    }
}
