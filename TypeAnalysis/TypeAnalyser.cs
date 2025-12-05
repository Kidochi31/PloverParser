using Microsoft.Z3;
using Plover.Debugging;
using Plover.Environment;
using Plover.EnvironmentAnalysis;
using Plover.Parsing;
using Plover.Scanning;
using Plover.Typing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Plover.TypeAnalysis
{
    internal class TypeAnalysisError(EnvExpr expression, string message, List<ErrorPointer>? otherPointers = null)
    {
        public readonly EnvExpr Expression = expression;
        public readonly string Message = message;
        public readonly List<ErrorPointer>? OtherPointers = otherPointers;

        public string VisualMessage(List<string> source)
        {
            return $"Error ({Message})\n at: {Expression}";
            //int startLine = EnvExpr.GetFirstToken().StartLine;
            //int startColumn = Expression.GetFirstToken().StartColumn;
            //int endLine = Expression.GetLastToken().EndLine;
            //int endColumn = Expression.GetLastToken().EndColumn;
            //return Debug.CreateErrorMessage(source, [new ErrorMessage(startLine, startColumn, Message)], [],
            //    [new ErrorUnderline(startLine, startColumn, endLine, endColumn, '~')], [new ErrorPointer(startLine, startColumn, [Message]), ..(OtherPointers ?? [])], new ErrorSettings(1, 1, 1, 1), []);
        }
    }

    internal class TypeAnalyser
    {

        public List<TypeAnalysisError> Errors = new();

        public TExpr? AnalyseExpressionTypeWithoutEnvironment(EnvExpr expression, Context context)
        {
            TypedEnvironment environment = new TypedEnvironment(context);
            try
            {
                return AnalyseExpressionType(environment, expression);
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalyseExpressionTypeError: {e}");
                return null;
            }
        }

        private TExpr? AnalyseExpressionType(TypedEnvironment environment, EnvExpr? expression)
        {
            switch (expression)
            {
                case EnvExpr.Constant constantExpr:
                    return AnalyseConstantExpression(environment, constantExpr);
                case EnvExpr.Literal literalExpr:
                    return AnalyseLiteralExpression(environment, literalExpr);
                case EnvExpr.VariableRead readExpr:
                    {
                        // the type of the expression is the type of the variable -> it must have been written to for the variable to have a type
                        TypedVariable typedVar = environment.GetVariable(readExpr.Variable);
                        return new TExpr.VariableRead(environment, typedVar, typedVar.Type);
                    }
                //case EnvExpr.VariableWrite writeExpression:
                //    // the type of the variable is the type of the expression
                //    // i.e. this is not about how the variable is stored but simply its most refined type
                //    //  the write occurs in a new environment
                //    {
                //        TypedEnvironment newEnvironment = new TypedEnvironment(environment);
                //        TExpr? variableValue = AnalyseExpressionType(newEnvironment, writeExpression.Value);
                //        if (variableValue is null)
                //            return null;
                //        TypedVariable typedVar = newEnvironment.AddVariable(writeExpression.Variable, variableValue.Type);
                //        // now the type of the variable value is known -> we can now evaluate the evaluate expression
                //        TExpr? evaluateExpression = AnalyseExpressionType(newEnvironment, writeExpression.Evaluate);
                //        return new TExpr.VariableWrite(newEnvironment, typedVar, variableValue, evaluateExpression, evaluateExpression.Type);
                //    }
                case EnvExpr.Conditional conditionalExpression:
                    // for each conditional branch, the environment gains a refinement (in a new environment)
                    {
                        TExpr? condition = AnalyseExpressionType(environment, conditionalExpression.Condition);
                        if (condition is null)
                            return null;
                        // evaluate ifTrue branch
                        TypedEnvironment trueEnvironment = new TypedEnvironment(environment);
                        Refinement trueEnvironmentRefinement = new Refinement.Expression(condition);
                        trueEnvironment.AddEnvironmentRefinement(trueEnvironmentRefinement.Z3Expression(trueEnvironment.Context, 0));
                        TExpr? ifTrue = AnalyseExpressionType(trueEnvironment, conditionalExpression.IfTrue);

                        // evaluate ifFalse branch
                        TypedEnvironment falseEnvironment = new TypedEnvironment(environment);
                        Refinement falseEnvironmentRefinement = new Refinement.Not(trueEnvironmentRefinement);
                        falseEnvironment.AddEnvironmentRefinement(falseEnvironmentRefinement.Z3Expression(falseEnvironment.Context, 0));
                        TExpr? ifFalse = AnalyseExpressionType(falseEnvironment, conditionalExpression.IfFalse);

                        // the return type is of the form (if condition then ifTrue.Type else ifFalse.Type)
                        // this is written as (condition and ifTrue.Type) or (not condition and ifFalse.Type)
                        ExprType type = new ExprType(new Refinement.Or(
                                                        new Refinement.And(trueEnvironmentRefinement, new Refinement.Subtype(ifTrue.Type)),
                                                        new Refinement.And(falseEnvironmentRefinement, new Refinement.Subtype(ifFalse.Type))));
                        return new TExpr.Conditional(environment, condition, ifTrue, ifFalse, type);
                    }
                case EnvExpr.FunctionCall callExpression:
                default:
                    throw new Exception("Expression type not supported.");
            }
        }

        private TExpr? AnalyseLiteralExpression(TypedEnvironment environment, EnvExpr.Literal expression)
        {
            switch (expression.token)
            {
                case BoolToken boolToken:
                    
                case IntegerToken:
                case FloatToken:
                
                case CharToken:
                case StringToken:
                case CustomLiteralToken:
                default:
                    return null;
            }
        }

        private TExpr? AnalyseConstantExpression(TypedEnvironment environment, EnvExpr.Constant expression)
        {
            switch (expression.Value)
            {
                case bool:
                case double:
                case BigInteger:
                default:
                    return null;
            }
        }

        void LogError(EnvExpr expression, string message, List<ErrorPointer>? otherPointers = null)
        {
            Errors.Add(new TypeAnalysisError(expression, message, otherPointers));
        }
    }
}
