using Compiler.IO;
using Compiler.Nodes;
using Compiler.Tokenization;
using System;
using System.Collections.Generic;
using static Compiler.Tokenization.TokenType;

namespace Compiler.SyntacticAnalysis
{
    /// <summary>
    /// A recursive descent parser
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// The error reporter
        /// </summary>
        public ErrorReporter Reporter { get; }

        /// <summary>
        /// The tokens to be parsed
        /// </summary>
        private List<Token> tokens;

        /// <summary>
        /// The index of the current token in tokens
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// The current token
        /// </summary>
        private Token CurrentToken { get { return tokens[currentIndex]; } }

        /// <summary>
        /// Advances the current token to the next one to be parsed
        /// </summary>
        private void MoveNext()
        {
            if (currentIndex < tokens.Count - 1)
                currentIndex += 1;
        }

        /// <summary>
        /// Creates a new parser
        /// </summary>
        /// <param name="reporter">The error reporter to use</param>
        public Parser(ErrorReporter reporter)
        {
            Reporter = reporter;
        }


        /// <summary>
        /// Checks the current token is the expected kind and moves to the next token
        /// </summary>
        /// <param name="expectedType">The expected token type</param>
        private void Accept(TokenType expectedType)
        {
            if (CurrentToken.Type == expectedType)
            {
                Debugger.Write($"Accepted {CurrentToken}");
                MoveNext();
            }
        }


        /// <summary>
        /// Parses a program
        /// </summary>
        /// <param name="tokens">The tokens to parse</param>
        /// <returns>The abstract syntax tree resulting from the parse</returns>
        public ProgramNode Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            ProgramNode program = ParseProgram();
            return program;
        }


        /// <summary>
        /// Parses a program
        /// </summary>
        /// <returns>An abstract syntax tree representing the program</returns>
        private ProgramNode ParseProgram()
        {
            Debugger.Write("Parsing program");
            ICommandNode command = ParseSingleCommand();
            ProgramNode program = new ProgramNode(command);
            return program;
        }


        /// <summary>
        /// Parses a command
        /// </summary>
        /// <returns>An abstract syntax tree representing the command</returns>
        private ICommandNode ParseCommand()
        {
            Debugger.Write("Parsing command");
            List<ICommandNode> commands = new List<ICommandNode>();
            commands.Add(ParseSingleCommand());
            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                commands.Add(ParseSingleCommand());
            }
            if (commands.Count == 1)
                return commands[0];
            else
                return new SequentialCommandNode(commands);
        }


        /// <summary>
        /// Parses a single command
        /// </summary>
        /// <returns>An abstract syntax tree representing the single command</returns>
        private ICommandNode ParseSingleCommand()
        {
            Debugger.Write("Parsing Single Command");
            switch (CurrentToken.Type)
            {
                case Identifier:
                    return ParseAssignmentOrCallCommand();
                case QuestionMark:
                    return ParseQuickIfCommand();
                case If:
                    return ParseIfCommand();
                case While:
                    return ParseWhileCommand();
                case Loop:
                    return ParseLoopCommand();
                case Let:
                    return ParseLetCommand();
                case Begin:
                    return ParseBeginCommand();
                default:
                    return ParseSkipCommand();
            }
        }


        /// <summary>
        /// Parses an assignment or call command
        /// </summary>
        /// <returns>An abstract syntax tree representing the command</returns>
        private ICommandNode ParseAssignmentOrCallCommand()
        {
            Debugger.Write("Parsing Assignment Command or Call Command");
            Position startPosition = CurrentToken.Position;
            IdentifierNode identifier = ParseIdentifier();
            if (CurrentToken.Type == LeftBracket)
            {
                Debugger.Write("Parsing Call Command");
                Accept(LeftBracket);
                IParameterNode parameter = ParseParameter();
                Accept(RightBracket);
                return new CallCommandNode(identifier, parameter);
            }
            else if (CurrentToken.Type == Is)
            {
                Debugger.Write("Parsing Assignment Command");
                Accept(Is);
                IExpressionNode expression = ParseExpression();
                return new AssignCommandNode(identifier, expression);
            }
            else
            {
                WriteError($"identifier can only be followed by a left bracket symbol '(' for a call command or an is symbol '~' for an assingment command not '{CurrentToken.Spelling}'.");
                return new ErrorNode(startPosition);
            }
        }


        /// <summary>
        /// Parses a quick if command
        /// </summary>
        /// <returns>An abstact syntax tree representing the quick if command</returns>
        private ICommandNode ParseQuickIfCommand() 
        {
            Debugger.Write("Parsing Quick If Command");
            Position startPosition = CurrentToken.Position;
            Accept(QuestionMark);
            IExpressionNode expression = ParseExpression();
            if (CurrentToken.Type != QuickIf) WriteError($"expression in a quick if statemanet can only be followed by a '=>' symbol not '{CurrentToken.Spelling}'.");
            Accept(QuickIf);
            ICommandNode command = ParseSingleCommand();
            return new QuickIfCommandNode(expression, command, startPosition);
        }


        /// <summary>
        /// Parses a skip command
        /// </summary>
        /// <returns>An abstract syntax tree representing the skip command</returns>
        private ICommandNode ParseSkipCommand()
        {
            Debugger.Write("Parsing Skip Command");
            Position startPosition = CurrentToken.Position;
            return new BlankCommandNode(startPosition);
        }


        /// <summary>
        /// Parses a while command
        /// </summary>
        /// <returns>An abstract syntax tree representing the while command</returns>
        private ICommandNode ParseWhileCommand()
        {
            Debugger.Write("Parsing While Command");
            Position startPosition = CurrentToken.Position;
            Accept(While);
            if (CurrentToken.Type != LeftBracket) WriteError($"while keyword can only be followed by a '(' symbol not '{CurrentToken.Spelling}'.");
            Accept(LeftBracket);
            IExpressionNode expression = ParseExpression();
            if (CurrentToken.Type != RightBracket) WriteError($"while statement's expression can only be followed by a ')' symbol not '{CurrentToken.Spelling}'.");
            Accept(RightBracket);
            ICommandNode command = ParseSingleCommand();
            if (CurrentToken.Type != Wend) WriteError($"while statement's command can only be followed by a 'wend' keyword not '{CurrentToken.Spelling}'.");
            Accept(Wend);
            return new WhileCommandNode(expression, command, startPosition);
        }


        /// <summary>
        /// Parses a loop command
        /// </summary>
        /// <returns>An absract syntax tree representing the loop command</returns>
        private ICommandNode ParseLoopCommand()
        {
            Debugger.Write("Parsing Loop Command");
            Position startPosition = CurrentToken.Position;
            Accept(Loop);
            ICommandNode loopCommand = ParseSingleCommand();
            if (CurrentToken.Type != While) WriteError($"loop statement's command can only be followed by a 'while' keyword not '{CurrentToken.Spelling}'.");
            Accept(While);
            if (CurrentToken.Type != LeftBracket) WriteError($"loop statement's while keyword can only be followed by a '(' symbol not '{CurrentToken.Spelling}'.");
            Accept(LeftBracket);
            IExpressionNode expression = ParseExpression();
            if (CurrentToken.Type != RightBracket) WriteError($"loop statement's expression can only be followed by a ')' symbol not '{CurrentToken.Spelling}'.");
            Accept(RightBracket);
            ICommandNode whileCommand = ParseSingleCommand();
            if (CurrentToken.Type != Repeat) WriteError($"loop statement's command can only be followed by a 'repeat' keyword not '{CurrentToken.Spelling}'.");
            Accept(Repeat);
            return new LoopCommandNode(loopCommand, expression, whileCommand, startPosition);
            
        }

        /// <summary>
        /// Parses an if command
        /// </summary>
        /// <returns>An abstract syntax tree representing the if command</returns>
        private ICommandNode ParseIfCommand()
        {
            Debugger.Write("Parsing If Command");
            Position startPosition = CurrentToken.Position;
            Accept(If);
            if (CurrentToken.Type != LeftBracket) WriteError($"if statement's if keyword can only be followed by a '(' symbol not '{CurrentToken.Spelling}'.");
            Accept(LeftBracket);
            IExpressionNode expression = ParseExpression();
            if (CurrentToken.Type != RightBracket) WriteError($"if statement's expression can only be followed by a ')' symbol not '{CurrentToken.Spelling}'.");
            Accept(RightBracket);
            if (CurrentToken.Type != Then) WriteError($"if statement's expression can only be followed by a 'then' keyword not '{CurrentToken.Spelling}'.");
            Accept(Then);
            ICommandNode thenCommand = ParseSingleCommand();
            if (CurrentToken.Type != Else) WriteError($"if statement's command can only be followed by a 'else' keyword not '{CurrentToken.Spelling}'.");
            Accept(Else);
            ICommandNode elseCommand = ParseSingleCommand();
            return new IfCommandNode(expression, thenCommand, elseCommand, startPosition);
        }

        /// <summary>
        /// Parses a let command
        /// </summary>
        /// <returns>An abstract syntax tree representing the let command</returns>
        private ICommandNode ParseLetCommand()
        {
            Debugger.Write("Parsing Let Command");
            Position startPosition = CurrentToken.Position;
            Accept(Let);
            IDeclarationNode declaration = ParseDeclaration();
            if (CurrentToken.Type != In) WriteError($"let statement's declarations can only be followed by an 'in' keyword not '{CurrentToken.Spelling}'.");
            Accept(In);
            ICommandNode command = ParseSingleCommand();
            return new LetCommandNode(declaration, command, startPosition);
        }

        /// <summary>
        /// Parses a begin command
        /// </summary>
        /// <returns>An abstract syntax tree representing the begin command</returns>
        private ICommandNode ParseBeginCommand()
        {
            Debugger.Write("Parsing Begin Command");
            Accept(Begin);
            ICommandNode command = ParseCommand();
            if (CurrentToken.Type != End) WriteError($"begin statement's command can only be followed by an 'end' keyword not '{CurrentToken.Spelling}'.");
            Accept(End);
            return command;
        }

        /// <summary>
        /// Parses a declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the declaration</returns>
        private IDeclarationNode ParseDeclaration()
        {
            Debugger.Write("Parsing Declaration");
            List<IDeclarationNode> declarations = new List<IDeclarationNode>();
            declarations.Add(ParseSingleDeclaration());
            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                declarations.Add(ParseSingleDeclaration());
            }
            if (declarations.Count == 1)
                return declarations[0];
            else
                return new SequentialDeclarationNode(declarations);
        }

        /// <summary>
        /// Parses a single declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the single declaration</returns>
        private IDeclarationNode ParseSingleDeclaration()
        {
            Debugger.Write("Parsing Single Declaration");
            switch (CurrentToken.Type)
            {
                case Const:
                    return ParseConstDeclaration();
                case Var:
                    return ParseVarDeclaration();
                default:
                    WriteError($"single decloration can only be either a 'const' or 'var' not '{CurrentToken.Spelling}'.");
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses a constant declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the constant declaration</returns>
        private IDeclarationNode ParseConstDeclaration()
        {
            Debugger.Write("Parsing Constant Declaration");
            Position StartPosition = CurrentToken.Position;
            Accept(Const);
            IdentifierNode identifier = ParseIdentifier();
            if (CurrentToken.Type != Is) WriteError($"const decloration's indentifier statement can only be followed by a 'is' keyword not '{CurrentToken.Spelling}'.");
            Accept(Is);
            IExpressionNode expression = ParseExpression();
            return new ConstDeclarationNode(identifier, expression, StartPosition);
        }

        /// <summary>
        /// Parses a variable declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the variable declaration</returns>
        private IDeclarationNode ParseVarDeclaration()
        {
            Debugger.Write("Parsing Variable Declaration");
            Position StartPosition = CurrentToken.Position;
            Accept(Var);
            IdentifierNode identifier = ParseIdentifier();
            if (CurrentToken.Type != Is) WriteError($"var decloration's indentifier statement can only be followed by a 'is' keyword not '{CurrentToken.Spelling}'.");
            Accept(Is);
            TypeDenoterNode typeDenoter = ParseTypeDenoter();
            return new VarDeclarationNode(identifier, typeDenoter, StartPosition);
        }

        /// <summary>
        /// Parses a type denoter
        /// </summary>
        /// <returns>An abstract syntax tree representing the type denoter</returns>
        private TypeDenoterNode ParseTypeDenoter()
        {
            Debugger.Write("Parsing Type Denoter");
            IdentifierNode identifier = ParseIdentifier();
            return new TypeDenoterNode(identifier);
        }

        /// <summary>
        /// Parses an expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression</returns>
        private IExpressionNode ParseExpression()
        {
            Debugger.Write("Parsing Expression");
            IExpressionNode leftExpression = ParsePrimaryExpression();
            while (CurrentToken.Type == Operator)
            {
                OperatorNode operation = ParseOperator();
                IExpressionNode rightExpression = ParsePrimaryExpression();
                leftExpression = new BinaryExpressionNode(leftExpression, operation, rightExpression);
            }
            return leftExpression;
        }

        /// <summary>
        /// Parses a primary expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the primary expression</returns>
        private IExpressionNode ParsePrimaryExpression()
        {
            Debugger.Write("Parsing Primary Expression");
            switch (CurrentToken.Type)
            {
                case IntLiteral:
                    return ParseIntExpression();
                case CharLiteral:
                    return ParseCharExpression();
                case Identifier:
                    return ParseIdOrCallExpression();
                case Operator:
                    return ParseUnaryExpression();
                case LeftBracket:
                    return ParseBracketExpression();
                default:
                    WriteError($"invalid expression: '{CurrentToken.Spelling}'.");
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an int expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the int expression</returns>
        private IExpressionNode ParseIntExpression()
        {
            Debugger.Write("Parsing Int Expression");
            IntegerLiteralNode intLit = ParseIntegerLiteral();
            return new IntegerExpressionNode(intLit);
        }

        /// <summary>
        /// Parses a char expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the char expression</returns>
        private IExpressionNode ParseCharExpression()
        {
            Debugger.Write("Parsing Char Expression");
            CharacterLiteralNode charLit = ParseCharacterLiteral();
            return new CharacterExpressionNode(charLit);
        }

        private IExpressionNode ParseIdOrCallExpression()
        {
            Debugger.Write("Parsing an Identifier Expression or a Call Expression");
            IdentifierNode identifier = ParseIdentifier();
            if (CurrentToken.Type == LeftBracket)
            {
                Debugger.Write("Parsing a Call Expression");
                Accept(LeftBracket);
                IParameterNode parameter = ParseParameter();
                if (CurrentToken.Type != RightBracket) WriteError($"call expression's parameter can only be followed by a ')' symbol not '{CurrentToken.Spelling}'." );
                Accept(RightBracket);
                return new CallExpressionNode(identifier, parameter);
            } 
            else
            {
                Debugger.Write("Parsing a Identifier Expression");
                return new IdExpressionNode(identifier);
            }
        }

        /// <summary>
        /// Parses a unary expresion
        /// </summary>
        /// <returns>An abstract syntax tree representing the unary expression</returns>
        private IExpressionNode ParseUnaryExpression()
        {
            Debugger.Write("Parsing Unary Expression");
            OperatorNode operation = ParseOperator();
            IExpressionNode expression = ParsePrimaryExpression();
            return new UnaryExpressionNode(operation, expression);
        }

        /// <summary>
        /// Parses a bracket expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the bracket expression</returns>
        private IExpressionNode ParseBracketExpression()
        {
            Debugger.Write("Parsing Bracket Expression");
            Accept(LeftBracket);
            IExpressionNode expression = ParseExpression();
            if (CurrentToken.Type != RightBracket) WriteError($"bracket expression can only be closed using a ')' symbol not '{CurrentToken.Spelling}'.");
            Accept(RightBracket);
            return expression;
        }

        /// <summary>
        /// Parses a parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the parameter</returns>
        private IParameterNode ParseParameter()
        {
            Debugger.Write("Parsing Parameter");
            switch (CurrentToken.Type)
            {
                case Identifier:
                case IntLiteral:
                case CharLiteral:
                case Operator:
                case LeftBracket:
                    return ParseExpressionParameter();
                case Var:
                    return ParseVarParameter();
                case RightBracket:
                    return new BlankParameterNode(CurrentToken.Position);
                default:
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an expression parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression parameter</returns>
        private IParameterNode ParseExpressionParameter()
        {
            Debugger.Write("Parsing Value Parameter");
            IExpressionNode expression = ParseExpression();
            return new ExpressionParameterNode(expression);
        }

        /// <summary>
        /// Parses a variable parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the variable parameter</returns>
        private IParameterNode ParseVarParameter()
        {
            Debugger.Write("Parsing Variable Parameter");
            Position startPosition = CurrentToken.Position;
            Accept(Var);
            IdentifierNode identifier = ParseIdentifier();
            return new VarParameterNode(identifier, startPosition);
        }

        /// <summary>
        /// Parses an integer literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the integer literal</returns>
        private IntegerLiteralNode ParseIntegerLiteral()
        {
            Debugger.Write("Parsing integer literal");
            Token integerLiteralToken = CurrentToken;
            Accept(IntLiteral);
            return new IntegerLiteralNode(integerLiteralToken);
        }

        /// <summary>
        /// Parses a character literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the character literal</returns>
        private CharacterLiteralNode ParseCharacterLiteral()
        {
            Debugger.Write("Parsing character literal");
            Token CharacterLiteralToken = CurrentToken;
            Accept(CharLiteral);
            return new CharacterLiteralNode(CharacterLiteralToken);
        }

        /// <summary>
        /// Parses an identifier
        /// </summary>
        /// <returns>An abstract syntax tree representing the identifier</returns>
        private IdentifierNode ParseIdentifier()
        {
            Debugger.Write("Parsing identifier");
            Token IdentifierToken = CurrentToken;
            Accept(Identifier);
            return new IdentifierNode(IdentifierToken);
        }

        /// <summary>
        /// Parses an operator
        /// </summary>
        /// <returns>An abstract syntax tree representing the operator</returns>
        private OperatorNode ParseOperator()
        {
            Debugger.Write("Parsing operator");
            Token OperatorToken = CurrentToken;
            Accept(Operator);
            return new OperatorNode(OperatorToken);
        }

        /// <summary>
        /// Write error message from the parser
        /// </summary>
        /// <param name="errorMessage">message to be put into error message</param>
        private void WriteError(String errorMessage)
        {   
            Console.WriteLine("Error at " + CurrentToken.Position + ", " + errorMessage);
            Reporter.HasErrors = true;
        }
    }
}