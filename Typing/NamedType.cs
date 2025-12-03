using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;

namespace Plover.Typing
{
    internal class NamedType
    {
        public string Name;
        public Context Z3Context;
        public FuncDecl Z3SubtypeFunction;
        public NamedType(string name, Context z3Context)
        {
            Name = name;
            Z3Context = z3Context;
            // Whether a variable/expression is a subtype of this function
            Z3SubtypeFunction = Z3Context.MkFuncDecl($"t_in_{name}", Z3Context.MkIntSort(), Z3Context.MkBoolSort());

        }

        public override string ToString() => Name;
    }
}
