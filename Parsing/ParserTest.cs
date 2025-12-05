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
        public static (Expr?, Scanner) ParseExpressionAndPrintOnError(string text)
        {
            (List<Token>? tokens, Scanner scanner) = ScannerTest.ScanAndPrintOnError(text);
            if (tokens is null)
            {
                Console.WriteLine("\nCannot parse text.");
                Console.WriteLine("");
                return (null, scanner);
            }

            Parser parser = new Parser(scanner, tokens);
            Expr? expression = parser.ParseExpression();
            if (expression is null || parser.Errors.Count > 0)
            {
                Console.WriteLine("\nParsing errors:");
                foreach (ParseError error in parser.Errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (expression, scanner);
        }


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
                (Expr? expression, _) = ParseExpressionAndPrintOnError(text);
                if(expression is null)
                {
                    continue;
                }
                Console.WriteLine("Expression:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static (TypeExpr?, Scanner) ParseTypeExpressionAndPrintOnError(string text)
        {
            (List<Token>? tokens, Scanner scanner) = ScannerTest.ScanAndPrintOnError(text);
            if (tokens is null)
            {
                Console.WriteLine("\nCannot parse text.");
                Console.WriteLine("");
                return (null, scanner);
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
                return (null, scanner);
            }

            return (expression, scanner);
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
                (TypeExpr? expression, _) = ParseTypeExpressionAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }
                Console.WriteLine("Type Expression:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static (Stmt?, Scanner) ParseStatementAndPrintOnError(string text)
        {
            (List<Token>? tokens, Scanner scanner) = ScannerTest.ScanAndPrintOnError(text);
            if (tokens is null)
            {
                Console.WriteLine("\nCannot parse text.");
                Console.WriteLine("");
                return (null, scanner);
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
                return (null, scanner);
            }

            return (expression, scanner);
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
                (Stmt? expression, _) = ParseStatementAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }
                Console.WriteLine("Statement:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static (List<Decl>?, Scanner) ParseDeclarationsAndPrintOnError(string text)
        {
            (List<Token>? tokens, Scanner scanner) = ScannerTest.ScanAndPrintOnError(text);
            if (tokens is null)
            {
                Console.WriteLine("\nCannot parse text.");
                Console.WriteLine("");
                return (null, scanner);
            }

            Parser parser = new Parser(scanner, tokens);
            List<Decl>? expression = parser.ParseDeclarations();
            if (expression is null || parser.Errors.Count > 0)
            {
                Console.WriteLine("\nParsing errors:");
                foreach (ParseError error in parser.Errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (expression, scanner);
        }

        public static void DeclReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the declaration parser repl.");
            Console.WriteLine("Enter in the text of one or more declarations to see parse results.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                (List<Decl>? expression, _) = ParseDeclarationsAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }

                Console.WriteLine("Declarations:");
                Console.WriteLine(string.Join("\n\n", (from decl in expression select decl.ToString())));
                Console.WriteLine("");
            }
        }
    }
}
