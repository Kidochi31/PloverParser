using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Scanning
{
    internal class Token
    {
        internal readonly TokenType Type;
        internal readonly string Lexeme;
        internal readonly int StartLine;
        internal readonly int StartColumn;
        internal readonly int EndLine;
        internal readonly int EndColumn;

        public Token(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn)
        {
            Type = type;
            Lexeme = lexeme;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public override string ToString()
        {
            return $"{Type} '{Lexeme}' of type {Type}, found at [{StartLine}, {StartColumn}]";
        }
    }

    internal class IdentifierToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, string name)
        : Token(type, lexeme, startLine, startColumn, endLine, endColumn)
    {
        public readonly string IdentifierName = name;
    }

    internal abstract class LiteralToken<T>(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, T value)
        : Token(type, lexeme, startLine, startColumn, endLine, endColumn)
    {
        internal readonly T Value = value;
    }

    internal class BoolToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, bool value)
        : LiteralToken<bool>(type, lexeme, startLine, startColumn, endLine, endColumn, value)
    {
    }

    internal class StringToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, string value)
        : LiteralToken<string>(type, lexeme, startLine, startColumn, endLine, endColumn, value)
    {
    }

    internal class CharToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, char value)
        : LiteralToken<char>(type, lexeme, startLine, startColumn, endLine, endColumn, value)
    {
    }

    internal class IntegerToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, BigInteger value, string? prefix, string suffix)
        : LiteralToken<BigInteger>(type, lexeme, startLine, startColumn, endLine, endColumn, value)
    {
        internal readonly string Suffix = suffix;
        internal readonly string? Prefix = prefix;
    }

    internal class FloatToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, double value, string suffix)
        : LiteralToken<double>(type, lexeme, startLine, startColumn, endLine, endColumn, value)
    {
        internal readonly string Suffix = suffix;
    }

    internal class CustomLiteralToken(TokenType type, string lexeme, int startLine, int startColumn, int endLine, int endColumn, string value, string suffix)
        : LiteralToken<string>(type, lexeme, startLine, startColumn, endLine, endColumn, value)
    {
        internal readonly string Suffix = suffix;
    }
}
