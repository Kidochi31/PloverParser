using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PloverParser.Scanning
{
    internal class ScannerTest
    {
        public static void ReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the scanner repl.");
            Console.WriteLine("Enter in text to see the tokens/errors.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true){
                Console.Write(">>> ");
                string? text = Console.ReadLine();
                if(text is null || text == "")
                {
                    continue;
                }
                if (text == "quit")
                {
                    Environment.Exit(0);
                }
                if (text == "menu")
                {
                    return;
                }
                text = Debugging.Repl.GetEscapedReplText(text);
                Scanner scanner = new Scanner(text);
                List<Token> tokens = scanner.ScanTokens();
                List<ScanError> errors = scanner.Errors;

                foreach (Token token in tokens)
                {
                    Console.WriteLine(token);
                }
                if (errors.Count > 0) Console.WriteLine("\nScanning errors:");
                else Console.WriteLine("\nNo scanning errors.");
                foreach (ScanError error in errors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                Console.WriteLine("");
            }
        }
    }
}
