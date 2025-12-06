using Plover.EnvironmentAnalysis;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Plover.Environment
{
    internal class Variable
    {
        private static ulong VariableIdCounter = 0;
        public ulong VariableId;
        public string Name;
        public Token? DeclarationToken;
        public EnvTypeExpr? DeclarationType = null;

        /* Need to organise how to deal with the declaration type of variables
         * I.e. how to get the declaration type of variables through the environment analyser and into the type analyser
         * Cannot add them when declaring variable (because declaration type may reference variable)
         * So need to add after somehow...
         * But also need to make sure that every variable is declared with a type?
         * ....
         * Have a think about it
        */

        public Variable(string name, Token? declarationToken)
        {
            Name = name;
            DeclarationToken = declarationToken;
            VariableId = VariableIdCounter++;
        }

        public override string ToString() => $"{Name}({VariableId})";
    }
}
