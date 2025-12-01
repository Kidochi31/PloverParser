using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Environment
{
    internal class EnvironmentState
    {
        private static int EnvironmentCounter = 0;
        private static int AnonymousVarCounter = 0;
        private EnvironmentState? ParentEnvironment;
        private Dictionary<string, Variable> Variables = new();
        private int EnvironmentNumber = EnvironmentCounter++;

        public static EnvironmentState CreateParentEnvironment()
        {
            return new EnvironmentState();
        }

        private EnvironmentState()
        {
            ParentEnvironment = null;
        }

        public EnvironmentState(EnvironmentState environment)
        {
            ParentEnvironment = environment;
        }

        public string GetEnvironmentName() => $"env{EnvironmentNumber}";

        public Variable AddAnonymousVariable()
        {
            string name = $"@non{AnonymousVarCounter++}";
            Variable newVariable = new Variable(name);
            Variables[name] = newVariable;
            return newVariable;

        }

        public Variable? AddVariable(string name)
        {
            if(GetVariable(name) is not null)
            {
                // no shadowing allowed
                return null;
            }
            Variable newVariable = new Variable(name);
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
    }
}
