using Plover.Debugging;
using Plover.Environment;
using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static Plover.Parsing.Expr;

namespace Plover.EnvironmentAnalysis
{
    internal partial class EnvironmentAnalyser
    {
        public EnvTypeExpr? AnalyseTypeExpressionWithoutEnvironment(TypeExpr expression)
        {
            ResolutionEnvironment environment = ResolutionEnvironment.CreateParentEnvironment();
            try
            {
                return AnalyseTypeExpression(environment, expression);
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalysisExpressionError: {e}");
                return null;
            }
        }
        private EnvTypeExpr AnalyseTypeExpression(ResolutionEnvironment environment, TypeExpr expression)
        {
            switch (expression)
            {
                case TypeExpr.And andExpr:
                    return new EnvTypeExpr.And(AnalyseTypeExpression(environment, andExpr.Left), AnalyseTypeExpression(environment, andExpr.Right));
                case TypeExpr.Or orExpr:
                    return new EnvTypeExpr.Or(AnalyseTypeExpression(environment, orExpr.Left), AnalyseTypeExpression(environment, orExpr.Right));
                case TypeExpr.Grouping groupExpr:
                    return AnalyseTypeExpression(environment, groupExpr.Type);
                case TypeExpr.NamedType namedType:
                    {
                        TypeVariable? typeVariable = GetTypeVariableForceDeclareOnError(environment, namedType.Token.IdentifierName, namedType.Token);
                        return new EnvTypeExpr.NamedType(typeVariable);
                    }
                case TypeExpr.Refinement refinementType:
                    {
                        EnvTypeExpr baseType = AnalyseTypeExpression(environment, refinementType.BaseType);
                        EnvExpr refinementExpression = AnalyseExpression(environment, refinementType.RefinementExpr);
                        return new EnvTypeExpr.Refinement(baseType, refinementExpression);
                    }
                default:
                    throw new Exception($"Cannot analyse type expression: {expression.GetType()}");
            }
        }
    }
}
