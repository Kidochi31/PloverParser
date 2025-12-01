using Plover.Debugging;
using Plover.Environment;
using Plover.Parsing;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Plover.TypeAnalysis
{
    internal class AnalyserTest
    {
        public static void ExprAnalyserReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the expression analyser repl.");
            Console.WriteLine("Enter in the text of an expression to see analysis result.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                Console.Write(">>> ");
                string? text = Console.ReadLine();
                if (text is null || text == "")
                {
                    continue;
                }
                if (text == "quit")
                {
                    System.Environment.Exit(0);
                }
                if (text == "menu")
                {
                    return;
                }
                text = Repl.GetEscapedReplText(text);
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
                Expr? expression = parser.ParseExpression();
                if (expression is null || parser.Errors.Count > 0)
                {
                    Console.WriteLine("\nParsing errors:");
                    foreach (ParseError error in parser.Errors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("\nCannot analyse text.");
                    Console.WriteLine("");
                    continue;
                }

                TypeAnalyser analyser = new TypeAnalyser();
                TExpr? analysisResults = analyser.AnalyseExpressionWithoutEnvironment(expression);
                Console.WriteLine("Expression analysis:");
                Console.WriteLine(analysisResults);
                Console.WriteLine("");

                if(analyser.Errors.Count > 0)
                {
                    Console.WriteLine("Analysis errors:");
                    foreach (AnalysisError error in analyser.Errors)
                    {
                        Console.WriteLine(error.VisualMessage(scanner.Lines));
                    }
                    Console.WriteLine("");
                    continue;
                }

            }
        }


    }
}
