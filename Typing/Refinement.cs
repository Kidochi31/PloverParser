using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;
using Plover.TypeAnalysis;


namespace Plover.Typing
{
    internal abstract record class Refinement
    {
        public abstract override string ToString();
        public abstract BoolExpr Z3Expression(Context context, ulong expressionIdentifier);


        public record class Not(Refinement Right) : Refinement
        {
            public override string ToString() => $"not ({Right})";

            public override BoolExpr Z3Expression(Context context, ulong expressionIdentifier) => context.MkNot(Right.Z3Expression(context, expressionIdentifier));
        }

        public record class And(Refinement Left, Refinement Right) : Refinement
        {
            public override string ToString() => $"({Left}) and ({Right})";

            public override BoolExpr Z3Expression(Context context, ulong expressionIdentifier) => context.MkAnd(Left.Z3Expression(context, expressionIdentifier), Right.Z3Expression(context, expressionIdentifier));
        }

        public record class Or(Refinement Left, Refinement Right) : Refinement
        {
            public override string ToString() => $"({Left}) or ({Right})";

            public override BoolExpr Z3Expression(Context context, ulong expressionIdentifier) => context.MkOr(Left.Z3Expression(context, expressionIdentifier), Right.Z3Expression(context, expressionIdentifier));
        }

        public record class Subtype(ExprType Type) : Refinement
        {
            public override string ToString() => $"subtype of ({Type})";

            public override BoolExpr Z3Expression(Context context, ulong expressionIdentifier) => Type.Z3Expression(context, expressionIdentifier);
        }

        public record class NamedSubtype(NamedType Type) : Refinement
        {
            // Named types are implemented as uninterpreted functions over variables (or more specifically, variable states)
            // Each time a variable is modified, a new variable state is made -> important
            public override string ToString() => $":{Type}";
            public override BoolExpr Z3Expression(Context context, ulong expressionIdentifier) => (BoolExpr)context.MkApp(Type.Z3SubtypeFunction, context.MkInt(expressionIdentifier));
        }

        public record class Expression(TExpr Expr) : Refinement
        {
            public override string ToString() => $"{Expr}";
            public override BoolExpr Z3Expression(Context context, ulong expressionIdentifier)
            {
                throw new NotImplementedException();
            }
        }
    }
}
