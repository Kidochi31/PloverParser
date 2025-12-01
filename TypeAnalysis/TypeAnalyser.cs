using Plover.Debugging;
using Plover.Environment;
using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Plover.TypeAnalysis
{
    internal class AnalysisError(Expr expression, string message)
    {
        public readonly Expr Expression = expression;
        public readonly string Message = message;

        public string VisualMessage(List<string> source)
        {
            int startLine = Expression.GetFirstToken().StartLine;
            int startColumn = Expression.GetFirstToken().StartColumn;
            int endLine = Expression.GetLastToken().EndLine;
            int endColumn = Expression.GetLastToken().EndColumn;
            return Debug.CreateErrorMessage(source, [new ErrorMessage(startLine, startColumn, Message)], [],
                [new ErrorUnderline(startLine, startColumn, endLine, endColumn, '~')], [new ErrorPointer(startLine, startColumn, [Message])], new ErrorSettings(1, 1, 1, 1), []);
        }
    }

    internal class TypeAnalyser
    {

        public List<AnalysisError> Errors = new();

        public TExpr? AnalyseExpressionWithoutEnvironment(Expr expression)
        {
            EnvironmentState environment = EnvironmentState.CreateParentEnvironment();
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

        private TExpr? AnalyseExpression(EnvironmentState environment, Expr expression)
        {
            switch (expression)
            {
                case Expr.Literal literalExpression:
                    return AnalyseLiteralExpression(environment, literalExpression);
                case Expr.Identifier identifierExpression:
                    Variable? variable = environment.GetVariable(identifierExpression.Token.IdentifierName);
                    if(variable is null)
                    {
                        LogError(identifierExpression, $"Variable {identifierExpression.Token.IdentifierName} is not declared.");
                        return null;
                    }
                    return new TExpr.VariableRead(environment, variable);
                case Expr.Unary unaryExpression: // Function call of Op(Arg)
                    return new TExpr.FunctionCall(environment, new TExpr.OperatorFunction(environment, unaryExpression.Operator.Type, 1)
                                                             , [AnalyseExpression(environment, unaryExpression.Right)]); 
                case Expr.Binary binaryExpression: // Function call of Op(Arg1, Arg2)
                    return AnalyseBinaryExpression(environment, binaryExpression);
                case Expr.UnlessTernary unlessExpression: // Conditional of If Not(Arg1) Then Arg2 Else Arg3
                    return new TExpr.Conditional(environment, new TExpr.FunctionCall(environment, new TExpr.OperatorFunction(environment, TokenType.NOT, 1), [AnalyseExpression(environment, unlessExpression.Condition)]),
                                                              AnalyseExpression(environment, unlessExpression.IfTrue),
                                                              AnalyseExpression(environment, unlessExpression.IfFalse)
                                                              );
                case Expr.Ternary ifExpression: // Conditional of If Arg1 Then Arg2 Else Arg3
                    return new TExpr.Conditional(environment, AnalyseExpression(environment, ifExpression.Condition),
                                                              AnalyseExpression(environment, ifExpression.IfTrue),
                                                              AnalyseExpression(environment, ifExpression.IfFalse)
                                                              ); 
                case Expr.NAry naryExpression: // Converted into variable declarations, assignments, and conditionals
                    EnvironmentState thisEnvironment = environment;
                    Stack<(EnvironmentState environment, Variable variable, Expr binding)> varBindings = new();
                    Stack<TokenType> operators = new();
                    thisEnvironment = new EnvironmentState(thisEnvironment);
                    Variable firstVar = thisEnvironment.AddAnonymousVariable();
                    varBindings.Push((thisEnvironment, firstVar, naryExpression.Start));
                    foreach((Token op, Expr expr) in naryExpression.Operations)
                    {
                        thisEnvironment = new EnvironmentState(thisEnvironment);
                        Variable var = thisEnvironment.AddAnonymousVariable();
                        varBindings.Push((thisEnvironment, var, expr));
                        operators.Push(op.Type);
                    }

                    // all the variables have been declared -> go the other way down the stack and create the TExpr
                    var (en1, va1, bi1) = varBindings.Pop();
                    var (cen, cva, cbi) = varBindings.Pop();

                    // produce the last comparison expression
                    TExpr? comparisonExpression = new TExpr.FunctionCall(en1, new TExpr.OperatorFunction(en1, operators.Pop(), 2), [new TExpr.VariableRead(en1, cva), new TExpr.VariableRead(en1, va1)]);
                    TExpr? writeExpression = new TExpr.VariableWrite(en1, va1, AnalyseExpression(en1, bi1), comparisonExpression);
                    foreach ((EnvironmentState e, Variable v, Expr b) in varBindings)
                    {
                        // a == b == c is (a == b) and (b == c)
                        // which is if not (a == b) then false else (b == c)
                        // writeExpression is currently (b == c)
                        // cen, cva, cbi is currently b (unwritten) 
                        (en1, va1, bi1) = (cen, cva, cbi); // b
                        // a (unwritten)
                        (cen, cva, cbi) = (e, v, b);
                        comparisonExpression = new TExpr.FunctionCall(en1, new TExpr.OperatorFunction(en1, operators.Pop(), 2), [new TExpr.VariableRead(en1, cva), new TExpr.VariableRead(en1, va1)]);
                        TExpr? ifExpression = new TExpr.Conditional(en1, new TExpr.FunctionCall(environment,
                                    new TExpr.OperatorFunction(environment, TokenType.NOT, 1), [comparisonExpression]),
                                    new TExpr.Constant(environment, false),
                                    writeExpression);
                        writeExpression = new TExpr.VariableWrite(en1, va1, AnalyseExpression(en1, bi1), ifExpression);
                    }
                    // now apply the first write expression
                    writeExpression = new TExpr.VariableWrite(cen, cva, AnalyseExpression(cen, cbi), writeExpression);
                    return writeExpression; 
                case Expr.Grouping groupExpression: // Returns the underlying expression
                    return AnalyseExpression(environment, groupExpression.Expr); 
                case Expr.Using usingExpression:// Converted to variable declaration and assignment
                    // create a new environment to declare the variables in
                    EnvironmentState currentEnvironment = environment;
                    Stack<(EnvironmentState environment, Variable variable, Expr binding)> bindings = new();
                    foreach((Expr.Identifier identifier, Expr expr) in usingExpression.Bindings)
                    {
                        currentEnvironment = new EnvironmentState(currentEnvironment);
                        Variable? newVariable = currentEnvironment.AddVariable(identifier.Token.IdentifierName);
                        if(newVariable is null)
                        {
                            // error - could not create variable name
                        }
                        else
                        {
                            bindings.Push((currentEnvironment, newVariable, expr));
                        }
                    }
                    // all the variables have been declared -> go the other way down the stack and create the TExpr
                    TExpr? currentExpression = AnalyseExpression(currentEnvironment, usingExpression.Expr);
                    foreach((EnvironmentState e, Variable v, Expr b) in bindings)
                    {
                        currentExpression = new TExpr.VariableWrite(e, v, AnalyseExpression(e, b), currentExpression);
                    }
                    return currentExpression; 
                case Expr.FunctionCall functionCall: // Returns the function call
                    return new TExpr.FunctionCall(environment, AnalyseExpression(environment, functionCall.Function),(from e in functionCall.Arguments select AnalyseExpression(environment, e)).ToList()); 
                default:
                    throw new Exception("Expression type not supported.");
            }
        }

        TExpr.Constant AnalyseLiteralExpression(EnvironmentState environment, Expr.Literal expression)
        {
            switch (expression.Token)
            {
                case BoolToken boolToken:
                    return new TExpr.Constant(environment, boolToken.Value);
                case StringToken stringToken:
                    return new TExpr.Constant(environment, stringToken.Value);
                case CharToken charToken:
                    return new TExpr.Constant(environment, charToken.Value);
                case IntegerToken integerToken:
                    return new TExpr.Constant(environment, integerToken.Value);
                case FloatToken floatToken:
                    return new TExpr.Constant(environment, floatToken.Value);
                case CustomLiteralToken:
                default:
                    throw new Exception($"Literal expression type not supported: {expression.Token.GetType()}");
            }
        }

        TExpr? AnalyseBinaryExpression(EnvironmentState environment, Expr.Binary expression)
        {
            TokenType operatorType = expression.Operator.Type;
            switch (operatorType)
            {
                case TokenType.AND: // for (A or B) it is equivalent to if (not A) then false else B
                    return new TExpr.Conditional(environment, new TExpr.FunctionCall(environment, new TExpr.OperatorFunction(environment, TokenType.NOT, 1),
                                                                    [AnalyseExpression(environment, expression.Left)]),
                                                              new TExpr.Constant(environment, false),
                                                              AnalyseExpression(environment, expression.Right));
                case TokenType.OR: // for (A or B) it is equivalent to if A then true else B
                    return new TExpr.Conditional(environment, AnalyseExpression(environment, expression.Left),
                                                              new TExpr.Constant(environment, true),
                                                              AnalyseExpression(environment, expression.Right));
            }

            // for all others, return a normal function call
            return new TExpr.FunctionCall(environment, new TExpr.OperatorFunction(environment, expression.Operator.Type, 2)
                                                             , [AnalyseExpression(environment, expression.Left),
                                                                AnalyseExpression(environment, expression.Right)]);
        }

        void LogError(Expr expression, string message)
        {
            Errors.Add(new AnalysisError(expression, message));
        }
    }
}
