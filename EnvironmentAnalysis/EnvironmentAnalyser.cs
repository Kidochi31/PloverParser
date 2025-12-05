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

namespace Plover.EnvironmentAnalysis
{
    internal class EnvironmentAnalysisError(Token StartToken, Token EndToken, string message, List<ErrorPointer>? otherPointers = null)
    {
        public readonly Token StartToken = StartToken;
        public readonly Token EndToken = EndToken;
        public readonly string Message = message;
        public readonly List<ErrorPointer>? OtherPointers = otherPointers;

        public string VisualMessage(List<string> source)
        {
            int startLine = StartToken.StartLine;
            int startColumn = StartToken.StartColumn;
            int endLine = EndToken.EndLine;
            int endColumn = EndToken.EndColumn;
            return Debug.CreateErrorMessage(source, [new ErrorMessage(startLine, startColumn, Message)], [],
                [new ErrorUnderline(startLine, startColumn, endLine, endColumn, '~')], [new ErrorPointer(startLine, startColumn, [Message]), ..(OtherPointers ?? [])], new ErrorSettings(1, 1, 1, 1), []);
        }
    }

    internal partial class EnvironmentAnalyser
    {

        public List<EnvironmentAnalysisError> Errors = new();

        Variable DeclareVariableForceDeclareOnError(ResolutionEnvironment environment, string variableName, Token token)
        {
            Variable? functionName = environment.DeclareVariable(variableName, token);
            if (functionName is null)
            {
                // function already declared in scope
                Token? previousDeclaration = environment.GetVariable(variableName)?.DeclarationToken;
                LogError(token, $"The name {variableName} is already declared in scope.",
                                    previousDeclaration is null ? null : [new ErrorPointer(previousDeclaration.StartLine, previousDeclaration.StartColumn, [$"{variableName} is declared here."])]);
                // continue, even though it is an error (force declare new variable)
                functionName = environment.ForceDeclareVariable(variableName, token);
            }
            return functionName;
        }


        Variable GetVariableForceDeclareOnError(ResolutionEnvironment environment, string variableName, Token token)
        {
            Variable? variable = environment.GetVariable(variableName);
            if (variable is null)
            {
                LogError(token, $"Variable {variableName} is not declared.");
                variable = environment.ForceDeclareVariable(variableName, token);
            }
            return variable;
        }

        TypeVariable GetTypeVariableForceDeclareOnError(ResolutionEnvironment environment, string variableName, Token token)
        {
            TypeVariable? variable = environment.GetTypeVariable(variableName);
            if (variable is null)
            {
                LogError(token, $"Type {variableName} is not declared.");
                variable = environment.ForceDeclareTypeVariable(variableName);
            }
            return variable;
        }

        Variable GetOperatorFunction(ResolutionEnvironment environment, TokenType op, bool prefix = false, bool postfix = false, bool binary = false)
        {
            return GetVariableOrError(environment, ResolutionEnvironment.GetOperatorFunctionName(op, prefix, postfix, binary));
        }

        Variable GetVariableOrError(ResolutionEnvironment environment, string variableName)
        {
            Variable? variable = environment.GetVariable(variableName);
            if (variable is null)
            {
                throw new Exception($"Expected variable {variableName} to be declared!");
            }
            return variable;
        }


        void LogError(Expr expression, string message, List<ErrorPointer>? otherPointers = null)
        {
            Errors.Add(new EnvironmentAnalysisError(expression.GetFirstToken(), expression.GetLastToken(), message, otherPointers));
        }

        void LogError(Token token, string message, List<ErrorPointer>? otherPointers = null)
        {
            Errors.Add(new EnvironmentAnalysisError(token, token, message, otherPointers));
        }
    }
}
