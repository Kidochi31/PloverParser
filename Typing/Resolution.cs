using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;

// .NET Z3 documentation: https://z3prover.github.io/api/html/namespace_microsoft_1_1_z3.html

// .NET Z3 source: https://github.com/Z3Prover/z3/blob/master/src/api/dotnet

namespace Plover.Typing
{
    internal abstract record class ImpositionResult
    {
        public record class Success : ImpositionResult { }
        public record class Counterexample(Model model) : ImpositionResult { }
    }

    internal class Resolution
    {
        
        public ImpositionResult AImposesOnB(Context context, TypedEnvironment environment, uint expressionId, ExprType a, ExprType b)
        {
            List<TypedVariable> Variables = environment.GetVariables();
            List<BoolExpr> VariableExpressions = (from var in Variables select var.Type.Z3Expression(context, var.VariableId)).ToList();

            // a -> b requires that a and not b is unsatisfiable
            BoolExpr aRefinements = a.Z3Expression(context, expressionId);
            BoolExpr bRefinements = context.MkNot(b.Z3Expression(context, expressionId));
            VariableExpressions.Add(aRefinements);
            VariableExpressions.Add(bRefinements);

            Solver solver = context.MkSolver();
            solver.Add(VariableExpressions);
            var status = solver.Check();
            switch (status)
            {
                case Status.UNSATISFIABLE:
                    return new ImpositionResult.Success();
                case Status.SATISFIABLE:
                    // satisfiable -> find a counterexample
                    var model = solver.Model;
                    return new ImpositionResult.Counterexample(model);
                default:
                    // could not resolve -> throw error
                    throw new Exception("Could not resolve imposition");
            }
        }
    }
}
