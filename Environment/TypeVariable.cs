using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Plover.Environment
{
    public class TypeVariable
    {
        private static ulong VariableIdCounter = 0;
        public ulong VariableId;
        public string Name;

        public TypeVariable(string name)
        {
            Name = name;
            VariableId = VariableIdCounter++;
        }

        public override string ToString() => $"{Name}({VariableId})";
    }
}
