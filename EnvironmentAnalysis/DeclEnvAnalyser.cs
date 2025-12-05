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
        public EnvDecl? AnalyseDeclarationWithoutEnvironment(Decl declaration)
        {
            ResolutionEnvironment environment = ResolutionEnvironment.CreateParentEnvironment();
            try
            {
                return AnalyseDeclaration(environment, declaration);
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalysisExpressionError: {e}");
                return null;
            }
        }
        private EnvDecl? AnalyseDeclaration(ResolutionEnvironment environment, Decl declaration)
        {
            switch (declaration)
            {
                case Decl.Function functionDeclaration:
                    {
                        Variable? functionName = DeclareVariableForceDeclareOnError(environment, functionDeclaration.Name.IdentifierName, functionDeclaration.Name);
                        // create a new environment to put function parameters and body in
                        // this is a function environment
                        ResolutionEnvironment bodyEnvironment = new ResolutionEnvironment(environment);

                        // declare all the function parameters before analysing the types
                        List<(Variable, TypeExpr)> parameters = new();
                        foreach(Decl.FnParam parameter in functionDeclaration.Parameters)
                        {
                            parameters.Add((DeclareFunctionParameter(bodyEnvironment, parameter), parameter.Type));
                        }

                        // now analyse the types
                        List<EnvDecl.FnParam> parametersWithTypes = new();
                        foreach((Variable v, TypeExpr t) in parameters)
                        {
                            parametersWithTypes.Add(new EnvDecl.FnParam(v, AnalyseTypeExpression(bodyEnvironment, t)));
                        }

                        // analyse the return type if there is one
                        EnvTypeExpr? returnType = functionDeclaration.ReturnType is null ? null : AnalyseTypeExpression(bodyEnvironment, functionDeclaration.ReturnType);

                        // analyse the statements as well
                        EnvStmt body = AnalyseStatement(bodyEnvironment, functionDeclaration.Body);

                        return new EnvDecl.Function(functionName, parametersWithTypes, returnType, body);
                    }
                default:
                    throw new Exception($"Cannot analyse declaration: {declaration.GetType()}");
            }
        }

        private Variable DeclareFunctionParameter(ResolutionEnvironment environment, Decl.FnParam parameter)
        {
            return DeclareVariableForceDeclareOnError(environment, parameter.Identifier.IdentifierName, parameter.Identifier);
        }
    }
}
