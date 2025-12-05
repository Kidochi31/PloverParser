using Plover.Debugging;
using Plover.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Plover.Scanning.TokenType;

namespace Plover.Parsing
{
    internal partial class Parser
    {
        public List<Decl>? ParseDeclarations()
        {
            try
            {
                return Declarations();
            }
            catch (ParseException) { return null; }
        }


        List<Decl> Declarations()
        {
            List<Decl> declarations = new List<Decl>();
            while (true)
            {
                Decl? nextDecl = Declaration();
                if (nextDecl is not null)
                {
                    declarations.Add(nextDecl);
                }
                else
                {
                    return declarations;
                }
            }
        }

        // Declaration -> FunctionDeclaration ;
        Decl? Declaration()
        {
            // certain keywords to look for:
            // fn -> function
            if (Check(FN))
            {
                return FunctionDeclaration();
            }
            // otherwise, there is no declaration
            if (!Check(EOF))
            {
                LogError(Peek(), Peek(), $"Declaration expected, instead got {Peek()}.");
            }
            return null;
        }

        // FunDeclaration -> 'fn' IDENTIFIER '(' Params ')' ('->' Type)? '{' FunctionBody '}' ;
        Decl FunctionDeclaration()
        {
            Token fnToken = Consume(FN, "Expected function declaration.");
            IdentifierToken identifier = (IdentifierToken)Consume(IDENTIFIER, "Expected identifier after 'fn'.");
            Token openParen = Consume(LEFT_PAREN, "Expected '(' at start of function parameters.");
            List<Decl.FnParam> parameters = FunctionDeclarationParameters();
            Consume(RIGHT_PAREN, "Expected ')' after function parameters", "Add ')' after function parameters", ")");
            TypeExpr? returnType = null;
            if (Match(MINUS_GT))
            {
                returnType = TypeExpression();
            }
            Stmt body = BracedBlock();

            return new Decl.Function(fnToken, identifier, parameters, returnType, body);
        }

        //Params -> Param ( ',' Param )* ;
        List<Decl.FnParam> FunctionDeclarationParameters()
        {
            List<Decl.FnParam> parameters = new List<Decl.FnParam>();
            if (Check(RIGHT_PAREN)) return parameters;
            parameters.Add(FunctionDeclarationParameter());

            while (Match(COMMA))
            {
                parameters.Add(FunctionDeclarationParameter());
            }
            return parameters;
        }

        //Param -> IDENTIFIER ':' Type ;
        Decl.FnParam FunctionDeclarationParameter()
        {
            IdentifierToken identifier = (IdentifierToken)Consume(IDENTIFIER, "Expected identifier for function parameter.");
            Token openParen = Consume(COLON, "Expected ':' after function parameter.");
            TypeExpr type = TypeExpression();
            return new(identifier, type);
        }
    }
}
