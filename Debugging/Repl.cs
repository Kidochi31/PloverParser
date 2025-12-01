using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Debugging
{
    internal static class Repl
    {
        public static string GetEscapedReplText(string text)
        {
            // replace \\ with \ and \n with newline.
            StringBuilder newText = new StringBuilder();
            bool escaped = false;
            foreach (char c in text)
            {
                if (escaped)
                {
                    if (c == 'n') newText.Append('\n');
                    else newText.Append(c);
                    escaped = false;
                    continue;
                }
                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }
                newText.Append(c);
            }
            return newText.ToString();
        }
    }
}
