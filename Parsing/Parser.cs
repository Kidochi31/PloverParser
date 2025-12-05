using Plover.Scanning;
using Plover.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static Plover.Scanning.TokenType;

namespace Plover.Parsing
{
    internal class ParseError(Token startToken, Token endToken, string message, List<ErrorSuggestion> suggestions)
    {
        public readonly Token StartToken = startToken;
        public readonly Token EndToken = endToken;
        public readonly string Message = message;
        public readonly List<ErrorSuggestion> Suggestions = suggestions;

        public string VisualMessage(List<string> source)
        {
            return Debug.CreateErrorMessage(source, [new ErrorMessage(StartToken.StartLine, StartToken.StartColumn, Message)], [],
                [new ErrorUnderline(StartToken.StartLine, StartToken.StartColumn, EndToken.EndLine, EndToken.EndColumn, '~')], [new ErrorPointer(StartToken.StartLine, StartToken.StartColumn, [Message])], new ErrorSettings(1, 1, 1, 1), Suggestions);
        }
    }

    internal partial class Parser
    {
        internal class ParseException() : Exception { }

        readonly Queue<Token> Tokens = new Queue<Token>();
        Token Previous = null;
        readonly Scanner Scanner;

        public List<ParseError> Errors = new();

        public Parser(Scanner scanner, List<Token> tokens)
        {
            Scanner = scanner;
            foreach (Token token in tokens)
            {
                Tokens.Enqueue(token);
            }
        }

        public Parser(Scanner scanner)
        {
            Scanner = scanner;
        }

        

        //Will ignore tokens until it finds: EOF
        void RecoverFromError()
        {
            Advance();
            while (!IsAtEnd())
            {
                Token t = Peek();
                switch (t.Type)
                {
                    case EOF:
                        return;
                }
                Advance();
            }
        }

        void EnsureTokens(int amount)
        {
            if (amount > Tokens.Count)
            {
                int difference = amount - Tokens.Count;
                for (int i = 0; i < difference; i++)
                {
                    Tokens.Enqueue(Scanner.ScanNextToken());
                }
            }
        }


        Token Advance()
        {
            Previous = Tokens.Dequeue();
            return Previous;
        }

        Token PeekNext()
        {
            EnsureTokens(2);
            return Tokens.ElementAt(1);
        }

        Token Peek()
        {
            EnsureTokens(1);
            return Tokens.Peek();
        }

        Token ConsumeAny(string message, ErrorSuggestion? suggestion = null, params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type))
                    return Advance();
            }
            Token nextToken = Peek();
            throw PanicError(nextToken, nextToken, message, suggestion);
        }

        Token Consume(TokenType type, string message, string? suggestionMessage=null, string? insertion=null, string? expressionSuggestionMessage=null, string? expressionReplacement=null)
        {
            if (Check(type)) return Advance();
            ErrorSuggestion? suggestion = null;
            Token nextToken = Peek();
            Expr? expression = ParseExpression();
            if(expression is not null && expressionReplacement is not null)
            {
                Token lastExprToken = expression.GetLastToken();
                int length = lastExprToken.EndColumn - nextToken.StartColumn + 1;
                if(lastExprToken.EndLine != Previous.EndLine)
                {
                    length = int.MaxValue / 2;
                }
                suggestion = new ErrorSuggestion([new ErrorReplaceSuggestion(nextToken.StartLine, nextToken.StartColumn, length, expressionReplacement)], expressionSuggestionMessage is null ? "" : expressionSuggestionMessage);
            }
            else if(insertion is not null)
            {
                suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(Previous.EndLine, Previous.EndColumn+1, insertion)], suggestionMessage is null ? "" : suggestionMessage);
            }
            throw PanicError(nextToken, nextToken, message, suggestion);
        }

        bool Match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }



        bool CheckAny(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type)) return true;
            }
            return false;
        }

        bool Check(TokenType type)
        {
            if (IsAtEnd() && type == EOF) return true;
            if (IsAtEnd() && type != EOF) return false;
            return Peek().Type == type;
        }

        bool IsAtEnd() => Peek().Type == EOF;


        void LogError(Token startToken, Token endToken, string message, ErrorSuggestion? suggestion = null)
        {
            Errors.Add(new ParseError(startToken, endToken, message, suggestion is null ? [] : [suggestion]));
        }

        ParseException PanicError(Token startToken, Token endToken, string message, ErrorSuggestion? suggestion = null)
        {
            Errors.Add(new ParseError(startToken, endToken, message, suggestion is null ? [] : [suggestion]));
            return new ParseException();
        }
    }
}
