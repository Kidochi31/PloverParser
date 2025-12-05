using Plover.Debugging;
using Plover.Environment;
using Plover.Parsing;
using Plover.Scanning;
using Plover.TypeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Plover.EnvironmentAnalysis
{
    internal class EnvironmentAnalyserTest
    {
        public static (EnvExpr?, Scanner) AnalyseEnvironmentExpressionAndPrintOnError(string text)
        {
            (Expr? expression, Scanner scanner) = ParserTest.ParseExpressionAndPrintOnError(text);
            if (expression is null)
            {
                Console.WriteLine("\nCannot analyse environment of text.");
                Console.WriteLine("");
                return (null, scanner);
            }

            EnvironmentAnalyser analyser = new EnvironmentAnalyser();
            EnvExpr? analysedExpression = analyser.AnalyseExpressionWithoutEnvironment(expression);
            if (analysedExpression is null || analyser.Errors.Count > 0)
            {
                Console.WriteLine("Analysing errors:");
                foreach (EnvironmentAnalysisError error in analyser.Errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (analysedExpression, scanner);
        }

        public static void ExprAnalyserReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the expression environment analyser repl.");
            Console.WriteLine("Enter in the text of an expression to see environment analysis result.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if(text is null)
                {
                    return;
                }
                (EnvExpr? expression, _) = AnalyseEnvironmentExpressionAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }
                Console.WriteLine("Expression analysis:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }



        public static (EnvStmt?, Scanner) AnalyseEnvironmentStatementAndPrintOnError(string text)
        {
            (Stmt? expression, Scanner scanner) = ParserTest.ParseStatementAndPrintOnError(text);
            if (expression is null)
            {
                Console.WriteLine("\nCannot analyse environment of text.");
                Console.WriteLine("");
                return (null, scanner);
            }

            EnvironmentAnalyser analyser = new EnvironmentAnalyser();
            EnvStmt? analysedExpression = analyser.AnalyseStatementWithoutEnvironment(expression);
            if (analysedExpression is null || analyser.Errors.Count > 0)
            {
                Console.WriteLine("Analysing errors:");
                foreach (EnvironmentAnalysisError error in analyser.Errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (analysedExpression, scanner);
        }

        public static void StmtAnalyserReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the statement environment analyser repl.");
            Console.WriteLine("Enter in the text of a statement to see environment analysis result.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                (EnvStmt? expression, _) = AnalyseEnvironmentStatementAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }
                Console.WriteLine("Statement analysis:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static (EnvTypeExpr?, Scanner) AnalyseEnvironmentTypeExpressionAndPrintOnError(string text)
        {
            (TypeExpr? expression, Scanner scanner) = ParserTest.ParseTypeExpressionAndPrintOnError(text);
            if (expression is null)
            {
                Console.WriteLine("\nCannot analyse environment of text.");
                Console.WriteLine("");
                return (null, scanner);
            }

            EnvironmentAnalyser analyser = new EnvironmentAnalyser();
            EnvTypeExpr? analysedExpression = analyser.AnalyseTypeExpressionWithoutEnvironment(expression);
            if (analysedExpression is null || analyser.Errors.Count > 0)
            {
                Console.WriteLine("Analysing errors:");
                foreach (EnvironmentAnalysisError error in analyser.Errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (analysedExpression, scanner);
        }

        public static void TypeExprAnalyserReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the type expression environment analyser repl.");
            Console.WriteLine("Enter in the text of a type expression to see environment analysis result.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                (EnvTypeExpr? expression, _) = AnalyseEnvironmentTypeExpressionAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }
                Console.WriteLine("Type expression analysis:");
                Console.WriteLine(expression.ToString());
                Console.WriteLine("");
            }
        }

        public static (List<EnvDecl>?, Scanner) AnalyseEnvironmentDeclarationsAndPrintOnError(string text)
        {
            (List<Decl>? expression, Scanner scanner) = ParserTest.ParseDeclarationsAndPrintOnError(text);
            if (expression is null)
            {
                Console.WriteLine("\nCannot analyse environment of text.");
                Console.WriteLine("");
                return (null, scanner);
            }

            EnvironmentAnalyser analyser = new EnvironmentAnalyser();
            List<EnvDecl>? analysedExpression = (from decl in expression select analyser.AnalyseDeclarationWithoutEnvironment(decl)).ToList();
            if (analysedExpression is null || analyser.Errors.Count > 0)
            {
                Console.WriteLine("Analysing errors:");
                foreach (EnvironmentAnalysisError error in analyser.Errors)
                {
                    Console.WriteLine(error.VisualMessage(scanner.Lines));
                }
                Console.WriteLine("");
                return (null, scanner);
            }

            return (analysedExpression, scanner);
        }

        public static void DeclAnalyserReplTest()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the declaration environment analyser repl.");
            Console.WriteLine("Enter in the text of a declaration to see environment analysis result.");
            Console.WriteLine("Enter \\n to create a new line, and \\\\n to enter '\\n'.");
            Console.WriteLine("Enter 'quit' to quit.");
            Console.WriteLine("Enter 'menu' to return to the menu.");

            while (true)
            {
                string? text = Repl.GetUserInput();
                if (text is null)
                {
                    return;
                }
                (List<EnvDecl>? expression, _) = AnalyseEnvironmentDeclarationsAndPrintOnError(text);
                if (expression is null)
                {
                    continue;
                }
                Console.WriteLine("Declaration analysis:");
                Console.WriteLine(string.Join("\n",expression).ToString());
                Console.WriteLine("");
            }
        }

    }
}
