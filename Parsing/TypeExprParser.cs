using Plover.Scanning;
using Plover.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Plover.Scanning.TokenType;

namespace Plover.Parsing
{
    internal partial class Parser
    {
        public TypeExpr? ParseTypeExpression()
        {
            try
            {
                return TypeExpression();
            }
            catch (ParseException) { return null; }
        }


        // Type -> RefinedType ( '|' RefinedType )*
        //       | RefinedType( '&' RefinedType )* ;
        TypeExpr TypeExpression()
        {
            TypeExpr left = RefinedType();
            if (CheckAny(AMPERSAND, BAR))
            {
                TokenType type = Peek().Type;
                //match & and |
                while (Match(AMPERSAND, BAR))
                {
                    Token previous = Previous;
                    TypeExpr right = RefinedType();

                    if (previous.Type != type)
                    {
                        ErrorSuggestion suggestion = new ErrorSuggestion([new ErrorInsertSuggestion(left.GetFirstToken().StartLine, left.GetFirstToken().StartColumn, "("), new ErrorInsertSuggestion(left.GetLastToken().EndLine, left.GetLastToken().EndColumn + 1, ")")], "Add brackets around the expression.");
                        LogError(previous, previous, "Type operators & and | can only chain with themselves.", suggestion);
                    }

                    left = type == AMPERSAND ? new TypeExpr.And(left, right) : new TypeExpr.Or(left, right);
                }
            }
            return left;
        }

        // RefinedType -> BaseType ( '[' Expression ']' )* ;
        TypeExpr RefinedType()
        {
            TypeExpr type = BaseType();
            while (Match(LEFT_SQUARE))
            {
                Expr refinement = Expression();
                Consume(RIGHT_SQUARE, "Expected ']' after refinement expression", "Add ']' after refinement expression", "]");
                Token rightBracket = Previous;
                type = new TypeExpr.Refinement(type, refinement, rightBracket);
            }
            return type;
        }

        // BaseType -> NamedType | '(' TypeExpression ')' ;
        TypeExpr BaseType()
        {
            if (Match(LEFT_PAREN))
            {
                Token paren1 = Previous;
                TypeExpr type = TypeExpression();
                Consume(RIGHT_PAREN, "Expected ')' after type expression.", "Add ')' after type expression", ")");
                Token paren2 = Previous;
                return new TypeExpr.Grouping(paren1, type, paren2);
            }
            return NamedType();
        }

        // NamedType -> IDENTIFIER ;

        TypeExpr NamedType()
        {
            IdentifierToken identifier = (IdentifierToken)Consume(IDENTIFIER, "Expected a type expression.");
            return new TypeExpr.NamedType(identifier);
        }
    }
}
