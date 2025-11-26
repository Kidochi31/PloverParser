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
    internal class ScanError(int startLine, int startColumn, int endLine, int endColumn, string message)
    {
        public readonly int StartLine = startLine;
        public readonly int StartColumn = startColumn;
        public readonly int EndLine = endLine;
        public readonly int EndColumn = endColumn;
        public readonly string Message = message;

        public string VisualMessage(List<string> source)
        {
            // first get the lines between the start - 1 and end + 1
            int initialLine = Math.Max(1, (StartLine - 1));
            int lastLine = Math.Min(EndLine + 1, source.Count);
            List<string> errorLines = source[(initialLine - 1)..(lastLine)];
            // add line numbers to all of the lines
            int maxLineNumberLength = lastLine.ToString().Length;
            string separator = "| ";
            for (int i = 0; i < errorLines.Count; i++)
            {
                int lineNumber = initialLine + i;
                string lineNumberStr = lineNumber.ToString();
                lineNumberStr = lineNumberStr.PadLeft(maxLineNumberLength);
                errorLines[i] = lineNumberStr + separator + errorLines[i];
            }

            // next, add an underline for the selected part of the lines
            List<char[]> underlineLines = new List<char[]>();
            int numLines = EndLine - StartLine + 1;
            int leftPad = maxLineNumberLength + separator.Length;
            // make all of these lines have ~~~~~ under them, with the length of the respective line
            for (int i = 0; i < numLines; i++)
            {
                underlineLines.Add(("".PadLeft(maxLineNumberLength, ' ') + separator + "".PadLeft(source[StartLine - 1 + i].Length + 1, '~')).ToCharArray());
            }
            // for the first line, replace '~' with ' ' up to startColumn
            for (int i = 1; i < StartColumn; i++)
            {
                underlineLines[0][leftPad+i - 1] = ' ';
            }
            // for the last line, replace '~' with ' ' from after endColumn
            for (int i = EndColumn + 1; i < source[EndLine - 1].Length + 2; i++)
            {
                underlineLines[^1][leftPad+i - 1] = ' ';
            }
            // additionally, add an error message line below the first error line
            string error_message = "".PadLeft(maxLineNumberLength, ' ') + separator + "".PadLeft(StartColumn - 1, ' ')+ "^ " + Message;
            int additionalLines = StartLine - initialLine;
            // now insert the underlines after their respective lines
            for (int i = numLines - 1; i >= 0; i--)
            {
                int index = additionalLines + i;
                if(i == 0)
                {
                    errorLines.Insert(index+1, error_message);
                }
                errorLines.Insert(index+1, new string(underlineLines[i]));
            }

            return string.Join('\n', errorLines);
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
            if(Column != 1)
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
                    Consume('=', "Expected '=' after '!'.");
                    return CreateToken(BANG_EQUAL);
                case '=':
                    Consume('=', "Expected '=' after '='.");
                    return CreateToken(EQUAL_EQUAL);
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

                    ReportError(StartLine, StartColumn, StartLine, StartColumn, $"Unexpected character '{c}'.");
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
                    ReportError(Line, Column, "Char literal cannot contain new line.");
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
                    ReportError(Line, Column-1, "Unescaped '\\'.");
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
                    ReportError(Line, Column, "Custom literals cannot contain new line.");
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
                    ReportError(Line, Column-1, "Unescaped '\\'.");
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
                    ReportError(Line, Column, "Simple string cannot contain new line.");
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
                    ReportError(Line, Column-1, "Unescaped '\\'.");
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

        void Consume(char expected, string message)
        {
            if (Match(expected))
            {
                return;
            }

            ReportError(message);
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
            IdentifierToken newToken = new IdentifierToken(type, text, StartLine, StartColumn, Line, Column, value);
            Tokens.Add(newToken);
            return newToken;
        }

        BoolToken CreateBoolToken(TokenType type, bool value)
        {
            string text = Source[Start..Current];
            BoolToken newToken = new BoolToken(type, text, StartLine, StartColumn, Line, Column, value);
            Tokens.Add(newToken);
            return newToken;
        }

        CustomLiteralToken CreateCustomLiteralToken(TokenType type, string value, string suffix)
        {
            string text = Source[Start..Current];
            CustomLiteralToken newToken = new CustomLiteralToken(type, text, StartLine, StartColumn, Line, Column, value, suffix);
            Tokens.Add(newToken);
            return newToken;
        }

        FloatToken CreateFloatToken(TokenType type, double value, string suffix)
        {
            string text = Source[Start..Current];
            FloatToken newToken = new FloatToken(type, text, StartLine, StartColumn, Line, Column, value, suffix);
            Tokens.Add(newToken);
            return newToken;
        }

        IntegerToken CreateIntegerToken(TokenType type, BigInteger value, string? prefix, string suffix)
        {
            string text = Source[Start..Current];
            IntegerToken newToken = new IntegerToken(type, text, StartLine, StartColumn, Line, Column, value, prefix, suffix);
            Tokens.Add(newToken);
            return newToken;
        }


        CharToken CreateCharToken(TokenType type, char value)
        {
            string text = Source[Start..Current];
            CharToken newToken = new CharToken(type, text, StartLine, StartColumn, Line, Column, value);
            Tokens.Add(newToken);
            return newToken;
        }

        StringToken CreateStringToken(TokenType type, string value)
        {
            string text = Source[Start..Current];
            StringToken newToken = new StringToken(type, text, StartLine, StartColumn, Line, Column, value);
            Tokens.Add(newToken);
            return newToken;
        }

        Token CreateToken(TokenType type)
        {
            string text = Source[Start..Current];
            Token newToken = new Token(type, text, StartLine, StartColumn, Line, Column);
            Tokens.Add(newToken);
            return newToken;
        }

        void ReportError(string message)
        {
            Errors.Add(new ScanError(StartLine, StartColumn, Line, Column, message));
        }

        void ReportError(int startLine, int startColumn, string message)
        {
            Errors.Add(new ScanError(startLine, startColumn, Line, Column, message));
        }
            void ReportError(int startLine, int startColumn, int endLine, int endColumn, string message)
            {
                Errors.Add(new ScanError(startLine, startColumn, endLine, endColumn, message));
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
