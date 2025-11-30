using PloverParser.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Plover.Scanning.TokenType;

namespace Plover.Scanning
{
    internal class ScanError(int startLine, int startColumn, int endLine, int endColumn, string message, List<ErrorSuggestion> suggestions)
    {
        public readonly int StartLine = startLine;
        public readonly int StartColumn = startColumn;
        public readonly int EndLine = endLine;
        public readonly int EndColumn = endColumn;
        public readonly string Message = message;
        public readonly List<ErrorSuggestion> Suggestions = suggestions;

        public string VisualMessage(List<string> source)
        {
            return Debugging.CreateErrorMessage(source, [new ErrorMessage(StartLine, StartColumn, Message)], [],
                [new ErrorUnderline(StartLine, StartColumn, EndLine, EndColumn, '~')], [new ErrorPointer(StartLine, StartColumn, [Message])], new ErrorSettings(1, 1, 1, 1), Suggestions);
        }
    }

    internal class Scanner
    {
        internal readonly string Source;
        internal readonly List<string> Lines = new();
        internal readonly List<Token> Tokens = new List<Token>();

        int Start = 0;
        int Current = 0;

        int StartLine = 1;
        int StartColumn = 1;

        int Line = 1;
        int Column = 1;

        public List<ScanError> Errors = new(); 

        public Scanner(string source)
        {
            Source = source;
        }

        public List<Token> ScanTokens()
        {
            List<Token> tokens = new List<Token>();
            while (!IsAtEnd())
            {
                Token token = ScanNextToken();
                tokens.Add(token);
            }
            if (tokens[^1].Type != EOF)
                tokens.Add(ScanNextToken()); //EOF
            return tokens;
        }

        public Token ScanNextToken()
        {
            while (!IsAtEnd())
            {
                Start = Current;
                StartLine = Line;
                StartColumn = Column;

                Token? possibleToken = ScanToken();
                if (possibleToken is Token)
                {
                    return possibleToken;
                }
            }
            if(Column != 1 && Line > Lines.Count)
            {
                Lines.Add(Source[(Current - Column + 1)..(Current)]);
            }
            
            return new Token(EOF, "", Line, Column, Line, Column);
        }

        Token? ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(': return CreateToken(LEFT_PAREN);
                case ')': return CreateToken(RIGHT_PAREN);
                case ',': return CreateToken(COMMA);
                case '~': return CreateToken(TILDE);

                case '-': return CreateToken(MINUS);
                case '+': return CreateToken(PLUS);
                case '*': return CreateToken(STAR);
                case '%': return CreateToken(PERCENT);
                case '^': return CreateToken(CARET);
                case '!':
                    if(Consume('=', "Expected '=' after '!'.")) return CreateToken(BANG_EQUAL);
                    return null;
                case '=':
                    if (Consume('=', "Expected '=' after '='.")) return CreateToken(EQUAL_EQUAL);
                    return null;
                case '<':
                    return CreateToken(Match('=') ? LT_EQUAL :
                                       Match('<') ? LT_LT :
                                       LT);
                case '>':
                    return CreateToken(Match('=') ? GT_EQUAL :
                                       Match('>') ? GT_GT :
                                       GT);
                case '&': return CreateToken(AMPERSAND);
                case '|': return CreateToken(BAR);
                case '/':
                    if (Match('/'))
                    {
                        // a comment goes until the end of line
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                        return null;
                    }
                    else if (Match('*'))
                    {
                        //delimited comment goes until */
                        while (!IsAtEnd())
                        {
                            char current = Advance();
                            if (current == '*' && Match('/')) // */
                            {
                                break; //end commend
                            }
                        }
                        return null;
                    }
                    return CreateToken(FORWARD_SLASH);
                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    //Ignore whitespace
                    return null;
                case '@':
                    if (Match('"')) return RawString();
                    return EscapedIdentifier();
                case '"':
                    return SimpleString();
                case '`':
                    return CustomLiteral();
                case '\'':
                    return CharLiteral();

                default:
                    if (IsDigit(c)) return Number();
                    else if (IsAlpha(c)) return SimpleIdentifier();

                    ReportError(StartLine, StartColumn, StartLine, StartColumn, $"Unexpected character '{c}'.",
                        new ErrorSuggestion([new ErrorDeleteSuggestion(StartLine, StartColumn, 1)], $"Remove '{c}' at {StartLine}:{StartColumn}."));
                    return null;
            }
        }

        Token? CharLiteral()
        {
            bool terminated = false;
            int length = 0;
            char value = '\0';
            while (!IsAtEnd())
            {
                char current = Advance();
                if (current == '\n') //\n not allowed in simple strings
                    ReportError(Line, Column, "Char literal cannot contain new line.", new ErrorSuggestion([new ErrorCombineLinesSuggestion(Line-1), new ErrorInsertSuggestion(Line-1, Lines[Line-2].Length+1, "\\n")], "Remove the new line and use \'\\n\' instead."));
                if (current == '\'') //end on '
                {
                    terminated = true;
                    break;
                }
                length++;
                if (current == '\\')
                {
                    if (Match('\\')) { value = '\\'; continue; }
                    if (Match('\'')) { value = '\''; continue; }
                    if (Match('n')) { value = '\n'; continue; }
                    if (Match('0')) { value = '\0'; continue; }
                    if (Match('"')) { value = '"'; continue; }
                    if (Match('`')) { value = '`'; continue; }
                    ReportError(Line, Column-1, "Unescaped '\\'.", new ErrorSuggestion([new ErrorInsertSuggestion(Line, Column, "\\")], "Add another '\\'."));
                }
                value = current;
            }

            if (!terminated)
            {
                ReportError(StartLine, StartColumn, StartLine, StartColumn, "Unterminated char literal.");
                return null;
            }

            if (length != 1)
            {
                ReportError(StartLine, StartColumn, Line, Column - 1, "Char literal can only be one character.");
                return null;
            }

            return CreateCharToken(CHAR, value);
        }

        Token? CustomLiteral()
        {
            bool terminated = false;
            StringBuilder builder = new StringBuilder();
            while (!IsAtEnd())
            {
                char current = Advance();
                if (current == '\n') //\n not allowed in custom literals
                    ReportError(Line, Column, "Custom literals cannot contain new line.", new ErrorSuggestion([new ErrorCombineLinesSuggestion(Line - 1), new ErrorInsertSuggestion(Line - 1, Lines[Line - 2].Length + 1, "\\n")], "Remove the new line and use \'\\n\' instead."));
                if (current == '`' && !(Peek() == '`')) //end on non-double `
                {
                    terminated = true;
                    break;
                }
                if (current == '`' && Match('`')) //`` becomes `
                {
                    builder.Append('`');
                    continue;
                }
                if (current == '\\')
                {
                    if (Match('\\')) { builder.Append('\\'); continue; }
                    if (Match('\'')) { builder.Append('\''); continue; }
                    if (Match('n')) { builder.Append('\n'); continue; }
                    if (Match('0')) { builder.Append('\0'); continue; }
                    if (Match('"')) { builder.Append('"'); continue; }
                    if (Match('`')) { builder.Append('`'); continue; }
                    ReportError(Line, Column-1, "Unescaped '\\'.", new ErrorSuggestion([new ErrorInsertSuggestion(Line, Column, "\\")], "Add another '\\'."));
                }
                builder.Append(current);
            }

            if (!terminated)
            {
                ReportError(StartLine, StartColumn, StartLine, StartColumn, "Unterminated custom literal.");
                return null;
            }

            int literalEndIndex = Current; //position of final `

            //ensure that there is a suffix
            if (!IsAlpha(Peek()))
            {
                ReportError("Custom literal requires suffix");
                return null;
            }


            //find any literal suffix characters -> alphanumeric
            while (IsAlphaNumeric(Peek())) Advance();

            //Trim `...`
            string value = builder.ToString();
            string suffix = Source[(literalEndIndex + 1)..Current];
            return CreateCustomLiteralToken(CUSTOM, value, suffix);
        }

        Token EscapedIdentifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            //Trim @...
            string text = Source[(Start + 1)..Current];
            return CreateIdentifierToken(IDENTIFIER, text);
        }

        Token SimpleIdentifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            string text = Source[Start..Current];
            TokenType type = Keywords.ContainsKey(text) ? Keywords[text] : IDENTIFIER;

            if (type == TRUE) return CreateBoolToken(type, true);
            if (type == FALSE) return CreateBoolToken(type, false);

            return type == IDENTIFIER ? CreateIdentifierToken(type, text) : CreateToken(type);
        }

        Token Number()
        {
            //Find the rest of the digits, allowing _
            while (IsMiddleDigit(Peek())) Advance();

            //find decimal point, with a digit after it
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                //It is a floating point number
                //consume '.'
                Advance();

                while (IsMiddleDigit(Peek())) Advance();//find the rest of the digits

                int finalDigitIndex = Current;

                //Find any number suffix characters
                if (IsAlpha(Peek())) //Must start with letter
                {
                    while (IsAlphaNumeric(Peek())) Advance(); //find all alphanumeric
                }
                string numberText = Source[Start..finalDigitIndex];
                string suffixText = finalDigitIndex + 1 <= Current ? Source[(finalDigitIndex + 1)..Current] : "";
                return CreateFloatToken(FLOAT, double.Parse(numberText), suffixText);
            }
            else //It is an integer number
            {
                int finalDigitIndex = Current;

                //Find any number suffix characters
                if (IsAlpha(Peek())) //Must start with letter
                {
                    while (IsAlphaNumeric(Peek())) Advance(); //find all alphanumeric
                }
                //string numberText = Source[Start..Current];
                string numberText = Source[Start..finalDigitIndex];
                string suffixText = finalDigitIndex + 1 <= Current ? Source[(finalDigitIndex + 1)..Current] : "";
                return CreateIntegerToken(INTEGER, BigInteger.Parse(numberText), null, suffixText);
            }
        }

        Token? RawString()
        {
            bool terminated = false;
            StringBuilder builder = new StringBuilder();
            while (!IsAtEnd())
            {
                char current = Advance();
                if (current == '"' && !(Peek() == '"')) //end on non-double "
                {
                    terminated = true;
                    break;
                }
                if (current == '"' && Match('"')) //"" becomes "
                {
                    builder.Append('"');
                    continue;
                }
                builder.Append(current);
            }

            if (!terminated)
            {
                ReportError(StartLine, StartColumn, StartLine, StartColumn, "Unterminated string.");
                return null;
            }

            //Trim @"..."
            return CreateStringToken(STRING, builder.ToString());
        }

        Token? SimpleString()
        {
            bool terminated = false;
            StringBuilder builder = new StringBuilder();
            while (!IsAtEnd())
            {
                char current = Advance();
                if (current == '\n') //\n not allowed in simple strings
                    ReportError(Line, Column, "Simple string cannot contain a new line.", new ErrorSuggestion([new ErrorCombineLinesSuggestion(Line - 1), new ErrorInsertSuggestion(Line - 1, Lines[Line - 2].Length + 1, "\\n")], "Remove the new line and use \'\\n\' instead."));
                if (current == '"' && !(Peek() == '"')) //end on non-double "
                {
                    terminated = true;
                    break;
                }
                if (current == '"' && Match('"')) //"" becomes "
                {
                    builder.Append('"');
                    continue;
                }
                if (current == '\\')
                {
                    if (Match('\\')) { builder.Append('\\'); continue; }
                    if (Match('\'')) { builder.Append('\''); continue; }
                    if (Match('n')) { builder.Append('\n'); continue; }
                    if (Match('0')) { builder.Append('\0'); continue; }
                    if (Match('"')) { builder.Append('"'); continue; }
                    if (Match('`')) { builder.Append('`'); continue; }
                    ReportError(Line, Column-1, "Unescaped '\\'.", new ErrorSuggestion([new ErrorInsertSuggestion(Line, Column, "\\")], "Add another '\\'."));
                }
                builder.Append(current);
            }

            if (!terminated)
            {
                ReportError(StartLine, StartColumn, StartLine, StartColumn, "Unterminated string.");
                return null;
            }

            //Trim "..."
            return CreateStringToken(STRING, builder.ToString());
        }

        char PeekNext()
        {
            if (Current + 1 >= Source.Length) return '\0';
            return Source[Current + 1];
        }

        //Lookahead at the current character
        char Peek()
        {
            if (IsAtEnd()) return '\0';
            return Source[Current];
        }

        //Will check the next char and conditionally eat it if expected
        bool Match(char expected)
        {
            if (Peek() != expected) return false;
            Advance();
            return true;
        }

        bool Consume(char expected, string message)
        {
            if (Match(expected))
            {
                return true;
            }

            ReportError(message, new ErrorSuggestion([new ErrorInsertSuggestion(Line, Column, $"{expected}")], $"Add in '{expected}'."));
            return false;
        }

        // returns current char and moves to next
        //Also increases line count if necessary
        char Advance()
        {
            char current = Source[Current++];
            Column++;
            if (current == '\n')
            {
                // add current line to lines
                Lines.Add(Source[(Current-Column+1)..(Current-1)]);
                // increment line and reset column
                Line++;
                Column = 1;
            }
            return current;
        }

        IdentifierToken CreateIdentifierToken(TokenType type, string value)
        {
            string text = Source[Start..Current];
            IdentifierToken newToken = new IdentifierToken(type, text, StartLine, StartColumn, Line, Column-1, value);
            Tokens.Add(newToken);
            return newToken;
        }

        BoolToken CreateBoolToken(TokenType type, bool value)
        {
            string text = Source[Start..Current];
            BoolToken newToken = new BoolToken(type, text, StartLine, StartColumn, Line, Column-1, value);
            Tokens.Add(newToken);
            return newToken;
        }

        CustomLiteralToken CreateCustomLiteralToken(TokenType type, string value, string suffix)
        {
            string text = Source[Start..Current];
            CustomLiteralToken newToken = new CustomLiteralToken(type, text, StartLine, StartColumn, Line, Column-1, value, suffix);
            Tokens.Add(newToken);
            return newToken;
        }

        FloatToken CreateFloatToken(TokenType type, double value, string suffix)
        {
            string text = Source[Start..Current];
            FloatToken newToken = new FloatToken(type, text, StartLine, StartColumn, Line, Column-1, value, suffix);
            Tokens.Add(newToken);
            return newToken;
        }

        IntegerToken CreateIntegerToken(TokenType type, BigInteger value, string? prefix, string suffix)
        {
            string text = Source[Start..Current];
            IntegerToken newToken = new IntegerToken(type, text, StartLine, StartColumn, Line, Column-1, value, prefix, suffix);
            Tokens.Add(newToken);
            return newToken;
        }


        CharToken CreateCharToken(TokenType type, char value)
        {
            string text = Source[Start..Current];
            CharToken newToken = new CharToken(type, text, StartLine, StartColumn, Line, Column-1, value);
            Tokens.Add(newToken);
            return newToken;
        }

        StringToken CreateStringToken(TokenType type, string value)
        {
            string text = Source[Start..Current];
            StringToken newToken = new StringToken(type, text, StartLine, StartColumn, Line, Column-1, value);
            Tokens.Add(newToken);
            return newToken;
        }

        Token CreateToken(TokenType type)
        {
            string text = Source[Start..Current];
            Token newToken = new Token(type, text, StartLine, StartColumn, Line, Column-1);
            Tokens.Add(newToken);
            return newToken;
        }

        void ReportError(string message, ErrorSuggestion? suggestion=null)
        {
            Errors.Add(new ScanError(StartLine, StartColumn, Line, Column, message, suggestion is null ? [] : [suggestion]));
        }

        void ReportError(int startLine, int startColumn, string message, ErrorSuggestion? suggestion = null)
        {
            Errors.Add(new ScanError(startLine, startColumn, Line, Column, message, suggestion is null ? [] : [suggestion]));
        }
            void ReportError(int startLine, int startColumn, int endLine, int endColumn, string message, ErrorSuggestion? suggestion = null)
            {
                Errors.Add(new ScanError(startLine, startColumn, endLine, endColumn, message, suggestion is null ? [] : [suggestion]));
            }


            bool IsAtEnd() => Current >= Source.Length;

        bool IsDigit(char c) => char.IsDigit(c);
        bool IsMiddleDigit(char c) => char.IsDigit(c) || c == '_';
        bool IsAlpha(char c) => char.IsAsciiLetter(c) || c == '_';
        bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

        static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            {"using", USING}, {"as", AS }, {"in", IN},
            {"and", AND}, {"or", OR}, {"not", NOT}, {"xor", XOR},
            {"true", TRUE}, {"false", FALSE},
            {"if", IF}, {"then", THEN}, {"else", ELSE}, {"unless", UNLESS},
        };
    }
}
