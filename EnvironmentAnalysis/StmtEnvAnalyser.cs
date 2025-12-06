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
        public EnvStmt? AnalyseStatementWithoutEnvironment(Stmt statement)
        {
            ResolutionEnvironment environment = ResolutionEnvironment.CreateParentEnvironment();
            try
            {
                var expr = AnalyseStatement(environment, statement);
                CloseEnvironment(environment);
                return expr;
            }
            catch (Exception e)
            {
                CloseEnvironment(environment);
                Console.WriteLine($"AnalyseStatementError: {e}");
                return null;
            }
        }

        private EnvStmt AnalyseStatement(ResolutionEnvironment environment, Stmt statement)
        {
            switch (statement)
            {
                case Stmt.Print printStmt:
                    return new EnvStmt.Print(AnalyseExpression(environment, printStmt.Expression));
                case Stmt.Return returnStmt:
                    return new EnvStmt.Return(returnStmt.Expression is null ? null : AnalyseExpression(environment, returnStmt.Expression));
                case Stmt.ExprStmt exprStmt:
                    return new EnvStmt.ExprStmt(AnalyseExpression(environment, exprStmt.Expression));
                case Stmt.Assignment assignmentStmt:
                    {
                        if(assignmentStmt.Target is Expr.Identifier identifier)
                        {
                            Variable variable = GetVariableForceDeclareOnError(environment, identifier.Token.IdentifierName, identifier.Token);
                            // don't mark variable as read
                            EnvExpr value = AnalyseExpression(environment, assignmentStmt.Value);
                            return new EnvStmt.Assignment(variable, value);
                        }
                        else
                        {
                            throw new Exception("Cannot analyse assignment statement of anything but an identifier!");
                        } 
                    }
                case Stmt.If ifStmt:
                    {
                        // analyse the condition first
                        EnvExpr condition = AnalyseExpression(environment, ifStmt.Condition);
                        // then analyse the ifTrue
                        EnvStmt ifTrue = AnalyseStatement(environment, ifStmt.ifTrue);
                        // then analyse the ifFalse (if there is one)
                        EnvStmt? ifFalse = ifStmt.ifFalse is null ? null : AnalyseStatement(environment, ifStmt.ifFalse);
                        return new EnvStmt.If(condition, ifTrue, ifFalse);
                    }
                case Stmt.Block blockStmt:
                    {
                        // create a new environment
                        ResolutionEnvironment bodyEnvironment = new ResolutionEnvironment(environment);
                        // then analyse all statements in order
                        List<EnvStmt> statements = (from Stmt in blockStmt.Body select AnalyseStatement(bodyEnvironment, Stmt)).ToList();
                        // close environment
                        CloseEnvironment(bodyEnvironment);
                        return new EnvStmt.Block(statements);
                    }
                case Stmt.LocVarDec locVarDec:
                    {
                        // two phases:
                        // 1. variable is declared (in environment)
                        // 2. variable is assigned (via statement) if necessary
                        Variable? varName = DeclareVariableForceDeclareOnError(environment, locVarDec.Identifier.IdentifierName, locVarDec.Identifier);
                        varName.DeclarationType = AnalyseTypeExpression(environment, locVarDec.Type);
                        // declaration complete
                        // if there is no assignment -> return an empty block
                        if(locVarDec.Value is null)
                        {
                            return new EnvStmt.Block([]);
                        }
                        else
                        {
                            // otherwise, return assignment statement
                            return new EnvStmt.Assignment(varName, AnalyseExpression(environment, locVarDec.Value));
                        }

                    }
                default:
                    throw new Exception($"Cannot analyse statement: {statement.GetType()}");
            }
        }
    }
}
