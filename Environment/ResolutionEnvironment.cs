using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Environment
{
    internal class ResolutionEnvironment
    {
        private static int EnvironmentCounter = 0;
        private static int AnonymousVarCounter = 0;
        private ResolutionEnvironment? ParentEnvironment;
        private Dictionary<string, Variable> Variables = new();
        private Dictionary<string, TypeVariable> TypeVariables = new();
        private int EnvironmentNumber = EnvironmentCounter++;

        public static ResolutionEnvironment CreateParentEnvironment()
        {
            return new ResolutionEnvironment();
        }

        private ResolutionEnvironment()
        {
            ParentEnvironment = null;
            // declare default operator functions
            // +, -, *, /, not, xor, ==, !=, <, <=, >, >=, &, |, ^, <<, >>, ~
            List<TokenType> prefixOperators = new List<TokenType> {TokenType.PLUS, TokenType.MINUS, TokenType.TILDE, TokenType.NOT };
            List<TokenType> binaryOperators = new List<TokenType> {TokenType.PLUS, TokenType.MINUS, TokenType.STAR, TokenType.FORWARD_SLASH,
                                                                   TokenType.XOR, TokenType.EQUAL_EQUAL, TokenType.BANG_EQUAL,
                                                                   TokenType.LT, TokenType.LT_EQUAL, TokenType.GT, TokenType.GT_EQUAL,
                                                                   TokenType.AMPERSAND, TokenType.BAR, TokenType.CARET, TokenType.LT_LT,
                                                                   TokenType.GT_GT};
            foreach(TokenType op in prefixOperators)
            {
                string varName = GetOperatorFunctionName(op, prefix: true);
                Variable opVar = new Variable(varName, null);
                Variables[varName] = opVar;
            }
            foreach (TokenType op in binaryOperators)
            {
                string varName = GetOperatorFunctionName(op, binary: true);
                Variable opVar = new Variable(varName, null);
                Variables[varName] = opVar;
            }

            // declare default types
            List<string> predeclaredTypes = new List<string> {"int", "bool", "char", "string"};
            foreach(string predeclaredType in predeclaredTypes)
            {
                TypeVariables[predeclaredType] = new TypeVariable(predeclaredType);
            }
        }

        public ResolutionEnvironment(ResolutionEnvironment environment)
        {
            ParentEnvironment = environment;
        }

        public string GetEnvironmentName() => $"env{EnvironmentNumber}";

        public static string GetOperatorFunctionName(TokenType op, bool prefix = false, bool postfix = false, bool binary = false)
        {
            if (prefix)
            {
                return "prefix@" + op.ToString();
            }
            if (postfix)
            {
                return "postfix@" + op.ToString();
            }
            if (binary)
            {
                return "binary@" + op.ToString();
            }
            throw new Exception("Operator function must be prefix, postfix or binary!");
        }

        public Variable AddAnonymousVariable()
        {
            string name = $"@non{AnonymousVarCounter++}";
            Variable newVariable = new Variable(name, null);
            Variables[name] = newVariable;
            return newVariable;

        }

        public Variable? DeclareVariable(string name, Token? declarationToken)
        {
            // only allow shadowing of variables outside this scope
            if(Variables.ContainsKey(name))
            {
                // not allowed
                return null;
            }
            Variable newVariable = new Variable(name, declarationToken);
            Variables[name] = newVariable;
            return newVariable;
        }

        public Variable ForceDeclareVariable(string name, Token? declarationToken)
        {
            Variable newVariable = new Variable(name, declarationToken);
            Variables[name] = newVariable;
            return newVariable;
        }

        public Variable? GetVariable(string name)
        {
            if (Variables.ContainsKey(name))
            {
                return Variables[name];
            }
            if (ParentEnvironment == null)
            {
                return null;
            }
            return ParentEnvironment.GetVariable(name);
        }

        public TypeVariable? GetTypeVariable(string name)
        {
            if (TypeVariables.ContainsKey(name))
            {
                return TypeVariables[name];
            }
            if (ParentEnvironment == null)
            {
                return null;
            }
            return ParentEnvironment.GetTypeVariable(name);
        }

        public TypeVariable ForceDeclareTypeVariable(string name)
        {
            TypeVariable newVariable = new TypeVariable(name);
            TypeVariables[name] = newVariable;
            return newVariable;
        }
    }
}
