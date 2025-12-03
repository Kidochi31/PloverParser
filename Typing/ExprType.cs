using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Typing
{
    internal class ExprType
    {
        public Refinement? Refinement;

        public ExprType(Refinement? refinement)
        {
            Refinement = refinement;
        }

        // The any type is the type with no refinements (i.e. it can hold any value)
        // The any type can only fit into another Any type
        // Any other type can fit into the Any type
        public static ExprType Any = new ExprType(null);
        // The never type is the type with all refinements (i.e. it cannot hold any value)
        // The never type can fit into any other type, including another Never type
        // No other type can fit into the Never type (except for another Never type)
        public static ExprType Never = new ExprType(new Refinement.Not(new Refinement.Subtype(Any)));

        public override string ToString() => Refinement is null ? "Any" : $"Any[{Refinement}]";

        public BoolExpr Z3Expression(Context context, ulong expressionIdentifier) => Refinement?.Z3Expression(context, expressionIdentifier) ?? context.MkTrue();
    }
}
