using Plover.Environment;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Typing
{
    internal class TypedVariable
    {
        private static ulong VariableIdCounter = 10;


        public Variable UnderlyingVariable;
        public ExprType Type;
        public ulong VariableId;

        public TypedVariable(Variable underlyingVariable, ExprType type)
        {
            UnderlyingVariable = underlyingVariable;
            Type = type;
            VariableId = VariableIdCounter++;
        }
    }
}
