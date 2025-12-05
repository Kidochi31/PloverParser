using Plover.Debugging;
using Plover.Environment;
using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Plover.EnvironmentAnalysis
{
    internal partial class EnvironmentAnalyser
    {
        public EnvExpr? AnalyseExpressionWithoutEnvironment(Expr expression)
        {
            ResolutionEnvironment environment = ResolutionEnvironment.CreateParentEnvironment();
            try
            {
                return AnalyseExpression(environment, expression);
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalysisExpressionError: {e}");
                return null;
            }
        }

        private EnvExpr AnalyseExpression(ResolutionEnvironment environment, Expr expression)
        {
            switch (expression)
            {
                case Expr.Literal literalExpression:
                    return AnalyseLiteralExpression(literalExpression);
                case Expr.Identifier identifierExpression:
                    {
                        Variable variable = GetVariableForceDeclareOnError(environment, identifierExpression.Token.IdentifierName, identifierExpression.Token);
                        return new EnvExpr.VariableRead(variable);
                    }
                case Expr.Prefix prefixExpression: // Function call of Op(Arg)
                    return new EnvExpr.FunctionCall(new EnvExpr.VariableRead(GetOperatorFunction(environment, prefixExpression.Operator.Type, prefix:true)),
                                                             [AnalyseExpression(environment, prefixExpression.Right)]);
                case Expr.Binary binaryExpression: // Function call of Op(Arg1, Arg2)
                    return AnalyseBinaryExpression(environment, binaryExpression);
                case Expr.UnlessTernary unlessExpression: // Conditional of If Not(Arg1) Then Arg2 Else Arg3
                    return new EnvExpr.Conditional(new EnvExpr.FunctionCall(new EnvExpr.VariableRead(GetOperatorFunction(environment, TokenType.NOT, prefix:true)), [AnalyseExpression(environment, unlessExpression.Condition)]),
                                                              AnalyseExpression(environment, unlessExpression.IfTrue),
                                                              AnalyseExpression(environment, unlessExpression.IfFalse)
                                                              );
                case Expr.Ternary ifExpression: // Conditional of If Arg1 Then Arg2 Else Arg3
                    return new EnvExpr.Conditional(AnalyseExpression(environment, ifExpression.Condition),
                                                              AnalyseExpression(environment, ifExpression.IfTrue),
                                                              AnalyseExpression(environment, ifExpression.IfFalse)
                                                              );
                case Expr.NAry naryExpression: // Converted into variable declarations, assignments, and conditionals
                    {
                        // don't need separate environments for each declaration
                        // just create one environment to keep it clean
                        ResolutionEnvironment naryEnvironment = new ResolutionEnvironment(environment);
                        // can declare all of the anonymous variables first
                        // and can analyse the bindings as well first (we aren't analysing types here)
                        // then go in reverse to create the ands
                        Stack<TokenType> operators = new();
                        Stack<(Variable variable, EnvExpr binding)> bindings = new();
                        bindings.Push((naryEnvironment.AddAnonymousVariable(), AnalyseExpression(naryEnvironment, naryExpression.Start)));
                        foreach ((Token op, Expr expr) in naryExpression.Operations)
                        {
                            bindings.Push((naryEnvironment.AddAnonymousVariable(), AnalyseExpression(naryEnvironment, expr)));
                            operators.Push(op.Type);
                        }

                        // go in reverse and create the bindings + ands
                        EnvExpr currentExpr = new EnvExpr.Constant(true);
                        var (va, bi) = bindings.Pop();
                        foreach ((Variable v, EnvExpr b) in bindings)
                        {
                            // using va as bi in (v op va) and (currentExpr)
                            var comparisonExpression = new EnvExpr.FunctionCall(new EnvExpr.VariableRead(GetOperatorFunction(environment, operators.Pop(), binary:true)), [new EnvExpr.VariableRead(v), new EnvExpr.VariableRead(va)]);
                            currentExpr = CreateShortCircuitAnd(naryEnvironment, comparisonExpression, currentExpr);
                            currentExpr = new EnvExpr.UsingBindings([(va, bi)], currentExpr);
                            (va, bi) = (v, b);
                        }
                        // there is still a va, bi left to bind
                        currentExpr = new EnvExpr.UsingBindings([(va, bi)], currentExpr);
                        return currentExpr;
                    }
                case Expr.Grouping groupExpression: // Returns the underlying expression
                    return AnalyseExpression(environment, groupExpression.Expr);
                case Expr.Using usingExpression:// Converted to variable declaration and assignment
                    {
                        // Create a new environment for all of the variables
                        ResolutionEnvironment usingEnvironment = new ResolutionEnvironment(environment);
                        // go through each binding
                        // declare the variable
                        // then create an assignment expression
                        List<(Variable Variable, EnvExpr Value)> bindings = new();
                        foreach ((Expr.Identifier identifier, Expr expr) in usingExpression.Bindings)
                        {
                            Variable variable = DeclareVariableForceDeclareOnError(usingEnvironment, identifier.Token.IdentifierName, identifier.Token);
                            EnvExpr value = AnalyseExpression(usingEnvironment, expr);
                            bindings.Add((variable, value));
                        }
                        // all bindings added, now evaluate the expression
                        EnvExpr evaluate = AnalyseExpression(usingEnvironment, usingExpression.Expr);
                        return new EnvExpr.UsingBindings(bindings, evaluate);
                    }
                case Expr.FunctionCall functionCall: // Returns the function call
                    return new EnvExpr.FunctionCall(AnalyseExpression(environment, functionCall.Function), (from e in functionCall.Arguments select AnalyseExpression(environment, e)).ToList());
                default:
                    throw new Exception("Expression type not supported.");
            }
        }

        EnvExpr.Literal AnalyseLiteralExpression(Expr.Literal expression)
        {
            return new EnvExpr.Literal(expression.Token);
        }

        EnvExpr AnalyseBinaryExpression(ResolutionEnvironment environment, Expr.Binary expression)
        {
            TokenType operatorType = expression.Operator.Type;
            switch (operatorType)
            {
                case TokenType.AND: // for (A or B) it is equivalent to if (not A) then false else B
                    return CreateShortCircuitAnd(environment, AnalyseExpression(environment, expression.Left), AnalyseExpression(environment, expression.Right));
                case TokenType.OR: // for (A or B) it is equivalent to if A then true else B
                    return CreateShortCircuitOr(environment, AnalyseExpression(environment, expression.Left), AnalyseExpression(environment, expression.Right));
            }

            // for all others, return a normal function call
            return new EnvExpr.FunctionCall(new EnvExpr.VariableRead(GetOperatorFunction(environment, expression.Operator.Type, binary: true))
                                                             , [AnalyseExpression(environment, expression.Left),
                                                                AnalyseExpression(environment, expression.Right)]);
        }

        EnvExpr CreateShortCircuitAnd(ResolutionEnvironment environment, EnvExpr left, EnvExpr right)
        {
            return new EnvExpr.Conditional(new EnvExpr.FunctionCall(new EnvExpr.VariableRead(GetOperatorFunction(environment, TokenType.NOT, prefix: true)),[left]), new EnvExpr.Constant(false),right);
        }

        EnvExpr CreateShortCircuitOr(ResolutionEnvironment environment, EnvExpr left, EnvExpr right)
        {
            return new EnvExpr.Conditional(left, new EnvExpr.Constant(true), right);
        }
    }
}
