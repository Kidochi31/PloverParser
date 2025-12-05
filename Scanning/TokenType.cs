using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plover.Scanning
{
    internal enum TokenType
    {
        // Keyword tokens
        USING, AS, IN,
        AND, OR, NOT, XOR,
        TRUE, FALSE,
        IF, THEN, ELSE, UNLESS,
        PRINT, LET, RETURN,
        FN,

        // Punctuator tokens
        EQUAL_EQUAL, BANG_EQUAL, LT, LT_EQUAL, GT, GT_EQUAL,
        AMPERSAND, BAR, CARET, TILDE,
        COMMA, SEMICOLON, 
        LEFT_PAREN, RIGHT_PAREN, LEFT_SQUARE, RIGHT_SQUARE, LEFT_BRACE, RIGHT_BRACE,
        PLUS, MINUS, STAR, FORWARD_SLASH, PERCENT, LT_LT, GT_GT,
        UNDERSCORE,
        LT_MINUS,
        COLON, MINUS_GT,

        // Literal tokens
        IDENTIFIER,
        INTEGER,
        FLOAT,
        STRING,
        CHAR,
        CUSTOM,

        // End of file
        EOF
    }
}
