using Microsoft.Z3;
using Plover.Environment;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Typing
{
    internal class TypedEnvironment
    {
        private static int EnvironmentCounter = 0;
        private Dictionary<Variable, TypedVariable> Variables = new();
        private Dictionary<string, NamedType> NamedTypes = new();
        private int EnvironmentNumber = EnvironmentCounter++;
        public Context Context;
        private List<BoolExpr> EnvironmentRefinements = new();

        public TypedEnvironment(Context context)
        {
            Context = context;
            AddNamedType(new NamedType("bool", context));
            AddNamedType(new NamedType("int", context));
            AddNamedType(new NamedType("float", context));
            AddNamedType(new NamedType("char", context));
        }

        public TypedEnvironment(TypedEnvironment environment)
        {
            Variables = new Dictionary<Variable, TypedVariable>(environment.Variables);
            EnvironmentRefinements = new List<BoolExpr>(EnvironmentRefinements);
            NamedTypes = new Dictionary<string, NamedType>(environment.NamedTypes);
            Context = environment.Context;
        }



        public string GetEnvironmentName() => $"env{EnvironmentNumber}";

        public TypedVariable AddVariable(Variable variable, ExprType type)
        {
            TypedVariable newVariable = new TypedVariable(variable, type);
            Variables[variable] = newVariable;
            return newVariable;
        }

        public void AddNamedType(NamedType type)
        {
            NamedTypes[type.Name] = type;
        }

        public NamedType GetNamedType(string name)
        {
            return NamedTypes[name];
        }

        public TypedVariable GetVariable(Variable variable)
        {
            if (Variables.ContainsKey(variable))
            {
                return Variables[variable];
            }
            throw new Exception($"Variable {variable.Name} not yet written in type environment!");
        }

        public List<TypedVariable> GetVariables()
        {
            return Variables.Values.ToList();
        }

        public void AddEnvironmentRefinement(BoolExpr refinement)
        {
            EnvironmentRefinements.Add(refinement);
        }

        public List<BoolExpr> GetEnvironmentRefinements()
        {
            return EnvironmentRefinements.ToList();
        }
    }
}
