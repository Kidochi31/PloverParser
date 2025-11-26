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

        // Punctuator tokens
        EQUAL_EQUAL, BANG_EQUAL, LT, LT_EQUAL, GT, GT_EQUAL,
        AMPERSAND, BAR, CARET, TILDE,
        COMMA,
        LEFT_PAREN, RIGHT_PAREN,
        PLUS, MINUS, STAR, FORWARD_SLASH, PERCENT, LT_LT, GT_GT,
        UNDERSCORE,

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
