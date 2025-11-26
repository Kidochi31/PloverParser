using Plover.Scanning;
using PloverParser.Scanning;
using System;

namespace Plover
{
    internal class Plover
    {
        static Dictionary<string, (string, Action)> Commands = new Dictionary<string, (string, Action)> 
        {
            { "scanner-repl", ("Test the scanner via repl", ScannerTest.ReplTest) },
            { "quit", ("Quit the program", () => {Environment.Exit(0); }) },
            { "help", ("See a list of comands", Help)}
        };

        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Plover Compiler command line tester!");
            Console.WriteLine("© 2025 Kidochi");
            while (true)
            {
                Console.WriteLine($"");
                Console.Write("Enter a command to test: ");
                string? command = Console.ReadLine();
                if (Commands.ContainsKey(command))
                {
                    Commands[command].Item2();
                    continue;
                }
                if (command is null || command == "")
                {
                    continue;
                }
                Console.WriteLine("Invalid command. Enter 'help' for help or 'quit' to quit.");
            }
        }

        public static void Help()
        {
            
            Console.WriteLine($"COMMANDS");
            Console.WriteLine($"~~~~~~~~");
            foreach ((string command, (string description, _)) in Commands)
            {
                Console.WriteLine($"{command}: {description}");
            }
            Console.WriteLine($"");
        }
    }
}