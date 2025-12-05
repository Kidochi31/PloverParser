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
        public Stmt? ParseStatement()
        {
            try
            {
                return Statement();
            }
            catch (ParseException) { return null; }
        }


        // Statement -> Print | LocVarDec | ExpressionStatement
        //            | Assignment | BracedIf | BracedBlock ;;
        Stmt Statement()
        {
            // certain keywords to look for:
            // print -> Print
            // return -> Return
            // let -> LocVarDec
            // if -> IfStatement
            // { -> BracedBlock
            if (Check(PRINT))
            {
                return Print();
            }
            if (Check(RETURN))
            {
                return Return();
            }
            if (Check(LET))
            {
                return LocVarDec();
            }
            if (Check(IF))
            {
                return IfStatement();
            }
            if (Check(LEFT_BRACE))
            {
                return BracedBlock();
            }
            // otherwise, the statement will begin with an expression
            return StatementWithExpression();
        }

        // Print -> 'print' Expression ';' ;
        Stmt Print()
        {
            Token printToken = Consume(PRINT, "Expected print statement.");
            Expr printExpr = Expression();
            Token semicolonToken = Consume(SEMICOLON, "Expected semicolon after statement.", "Insert semicolon after statement.", ";");
            return new Stmt.Print(printToken, printExpr, semicolonToken);
        }

        // Return -> 'return' (Expression)? ';' ;
        Stmt Return()
        {
            Token returnToken = Consume(RETURN, "Expected return statement.");
            Expr? returnExpr = null;
            if (!Check(SEMICOLON))
            {
                returnExpr = Expression();
            }
            Token semicolonToken = Consume(SEMICOLON, "Expected semicolon after statement.", "Insert semicolon after statement.", ";");
            return new Stmt.Return(returnToken, returnExpr, semicolonToken);
        }

        // LocVarDec -> 'let' IDENTIFIER ':' Type ( '<-' Expression )? ;
        Stmt LocVarDec()
        {
            Token letToken = Consume(LET, "Expected local var declaration statement.");
            IdentifierToken identifier = (IdentifierToken)Consume(IDENTIFIER, "Expected identifier after 'let'.");
            Token colonToken = Consume(COLON, "Expected ':' after identifier.");
            TypeExpr type = TypeExpression();
            Expr? assignmentExpr = null;
            if (Match(LT_MINUS))
            {
                // there is an assignment
                assignmentExpr = Expression();
            }
            Token semicolonToken = Consume(SEMICOLON, "Expected semicolon after statement.", "Insert semicolon after statement.", ";");
            return new Stmt.LocVarDec(letToken, identifier, type, assignmentExpr, semicolonToken);
        }

        // BracedIf -> 'if' Expression 'then' BracedBlock ('else' BracedElseBlock )? ;
        Stmt IfStatement()
        {
            Token ifToken = Consume(IF, "Expected if statement.");
            Expr condition = Expression();
            Token thenToken = Consume(THEN, "Expected 'then' after statement.", "Insert 'then'.", "then");
            Stmt ifTrue = BracedBlock();
            Stmt? ifFalse = null;
            if (Match(ELSE))
            {
                ifFalse = BracedElseBlock();
            }
            return new Stmt.If(ifToken, condition, ifTrue, ifFalse);
        }

        // BracedElseBody -> BracedBlock | BracedIf ;
        Stmt BracedElseBlock()
        {
            if (Check(LEFT_BRACE))
            {
                return BracedBlock();
            }
            if (Check(IF))
            {
                return IfStatement();
            }
            throw PanicError(Previous, Previous, "Expected 'if' or braced block after else.");
        }


        Stmt BracedBlock()
        {
            Token openBrace = Consume(LEFT_BRACE, "Expected '{' at start of braced block.");
            List<Stmt> body = new();
            while (!Check(RIGHT_BRACE))
            {
                body.Add(Statement());
            }
            Token closeBrace = Consume(RIGHT_BRACE, "Expected '}' at end of braced block.");
            return new Stmt.Block(openBrace, body, closeBrace);
        }

        // Could be expression statement or assignment
        Stmt StatementWithExpression()
        {
            Expr expr = Expression();
            if (Check(LT_MINUS))
            {
                return Assignment(expr);
            }
            // otherwise, it is just an expression statement
            Token semicolonToken = Consume(SEMICOLON, "Expected semicolon after statement.", "Insert semicolon after statement.", ";");
            return new Stmt.ExprStmt(expr, semicolonToken);
        }

        // Assignment -> Expression '<-' Expression ';' 
        Stmt Assignment(Expr target)
        {
            Token lt_minus = Consume(LT_MINUS, "Expected '<-' in assignment statement.");
            Expr value = Expression();
            Token semicolonToken = Consume(SEMICOLON, "Expected semicolon after statement.", "Insert semicolon after statement.", ";");
            return new Stmt.Assignment(target, value, semicolonToken);
        }
    }
}
