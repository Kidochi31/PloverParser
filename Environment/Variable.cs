using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Environment
{
    internal class Variable
    {
        public string Name;
        public Token? DeclarationToken;

        public Variable(string name, Token? declarationToken)
        {
            Name = name;
            DeclarationToken = declarationToken;
        }
    }
}
