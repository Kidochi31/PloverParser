using Plover.Debugging;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Scanning
{
    internal class ScannerTest
    {
        public static (List<Token>?, Scanner) ScanAndPrintOnError(string text)
        {
            Scanner scanner = new Scanner(text);
            List<Token> tokens = scanner.ScanTokens();
            List<ScanError> errors = scanner.Errors;

            if (errors.Count > 0)
            {
                Console.WriteLine("\nScanning errors:");
                foreach (ScanError error in errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (tokens, scanner);
        }

        public static void ReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the scanner repl.");
            Console.WriteLine("Enter in text to see the tokens/errors.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true){
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                (List<Token>? tokens, _) = ScanAndPrintOnError(text);
                if(tokens is null)
                {
                    continue;
                }
                Console.WriteLine("\nNo scanning errors.");
                foreach (Token token in tokens)
                {
                    Console.WriteLine(token);
                }
            }
        }
    }
}
