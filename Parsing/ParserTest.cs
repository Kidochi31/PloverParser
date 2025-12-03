using Plover.Debugging;
using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Plover.Parsing
{
    internal class ParserTest
    {
        public static void ExprReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the expression parser repl.");
            Console.WriteLine("Enter in the text of an expression to see parse result.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                Scanner scanner = new Scanner(text);
                List<Token> tokens = scanner.ScanTokens();
                List<ScanError> scanErrors = scanner.Errors;

                if(scanErrors.Count > 0)
                {
                    Console.WriteLine("\nScanning errors:");
                    foreach (ScanError error in scanErrors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("\nCannot parse text.");
                    Console.WriteLine("");
                    continue;
                }

                Parser parser = new Parser(scanner, tokens);
                Expr? expression = parser.ParseExpression();
                if(expression is null || parser.Errors.Count > 0)
                {
                    Console.WriteLine("\nParsing errors:");
                    foreach (ParseError error in parser.Errors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("");
                    continue;
                }

                Console.WriteLine("Expression:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static void TypeExprReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the type expression parser repl.");
            Console.WriteLine("Enter in the text of a type expression to see parse result.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                // nexttext is null or nexttext is ""
                Scanner scanner = new Scanner(text);
                List<Token> tokens = scanner.ScanTokens();
                List<ScanError> scanErrors = scanner.Errors;

                if (scanErrors.Count > 0)
                {
                    Console.WriteLine("\nScanning errors:");
                    foreach (ScanError error in scanErrors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("\nCannot parse text.");
                    Console.WriteLine("");
                    continue;
                }

                Parser parser = new Parser(scanner, tokens);
                TypeExpr? expression = parser.ParseTypeExpression();
                if (expression is null || parser.Errors.Count > 0)
                {
                    Console.WriteLine("\nParsing errors:");
                    foreach (ParseError error in parser.Errors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("");
                    continue;
                }

                Console.WriteLine("Type expression:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static void StmtReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the statement parser repl.");
            Console.WriteLine("Enter in the text of a statement to see parse result.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                // nexttext is null or nexttext is ""
                Scanner scanner = new Scanner(text);
                List<Token> tokens = scanner.ScanTokens();
                List<ScanError> scanErrors = scanner.Errors;

                if (scanErrors.Count > 0)
                {
                    Console.WriteLine("\nScanning errors:");
                    foreach (ScanError error in scanErrors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("\nCannot parse text.");
                    Console.WriteLine("");
                    continue;
                }

                Parser parser = new Parser(scanner, tokens);
                Stmt? expression = parser.ParseStatement();
                if (expression is null || parser.Errors.Count > 0)
                {
                    Console.WriteLine("\nParsing errors:");
                    foreach (ParseError error in parser.Errors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("");
                    continue;
                }

                Console.WriteLine("Statement:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }
    }
}
