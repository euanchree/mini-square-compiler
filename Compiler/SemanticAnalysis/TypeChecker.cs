using Compiler.IO;
using Compiler.Nodes;
using System;
using System.Reflection;
using static System.Reflection.BindingFlags;

namespace Compiler.SemanticAnalysis
{
    /// <summary>
    /// A type checker
    /// </summary>
    public class TypeChecker
    {
        /// <summary>
        /// The error reporter
        /// </summary>
        public ErrorReporter Reporter { get; }

        /// <summary>
        /// Creates a new type checker
        /// </summary>
        /// <param name="reporter">The error reporter to use</param>
        public TypeChecker(ErrorReporter reporter)
        {
            Reporter = reporter;
        }

        /// <summary>
        /// Carries out type checking on a program
        /// </summary>
        /// <param name="tree">The program to check</param>
        public void PerformTypeChecking(ProgramNode tree)
        {
            PerformTypeCheckingOnProgram(tree);
        }

        /// <summary>
        /// Carries out type checking on a node
        /// </summary>
        /// <param name="node">The node to perform type checking on</param>
        private void PerformTypeChecking(IAbstractSyntaxTreeNode node)
        {
            if (node is null)
                // Shouldn't have null nodes - there is a problem with your parsing
                Debugger.Write("Tried to perform type checking on a null tree node");
            else if (node is ErrorNode)
                // Shouldn't have error nodes - there is a problem with your parsing
                Debugger.Write("Tried to perform type checking on an error tree node");
            else
            {
                string functionName = "PerformTypeCheckingOn" + node.GetType().Name.Remove(node.GetType().Name.Length - 4);
                MethodInfo function = this.GetType().GetMethod(functionName, NonPublic | Public | Instance | Static);
                if (function == null)
                    // There is not a correctly named function below
                    Debugger.Write($"Couldn't find the function {functionName} when type checking");
                else
                    function.Invoke(this, new[] { node });
            }
        }



        /// <summary>
        /// Carries out type checking on a program node
        /// </summary>
        /// <param name="programNode">The node to perform type checking on</param>
        private void PerformTypeCheckingOnProgram(ProgramNode programNode)
        {
            PerformTypeChecking(programNode.Command);
        }



        /// <summary>
        /// Carries out type checking on an assign command node
        /// </summary>
        /// <param name="assignCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnAssignCommand(AssignCommandNode assignCommand)
        {
            PerformTypeChecking(assignCommand.Identifier);
            PerformTypeChecking(assignCommand.Expression);
            if (!(assignCommand.Identifier.Declaration is IVariableDeclarationNode varDeclaration))
            {
                // Error - identifier is not a variable
                WriteError($"identifier is not a declared variable, '{assignCommand.Identifier.IdentifierToken.Spelling}' does not exist.", assignCommand);
            }
            else if (varDeclaration.EntityType != assignCommand.Expression.Type)
            {
                // Error - expression is wrong type for the variable
                WriteError($"expression is wrong type for the variable, {assignCommand.Identifier.IdentifierToken.Spelling} is not of type {assignCommand.Expression.Type.ToString()}.", assignCommand);
            }
        }

        /// <summary>
        /// Carries out type checking on a blank command node
        /// </summary>
        /// <param name="blankCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnBlankCommand(BlankCommandNode blankCommand)
        {
        }

        /// <summary>
        /// Carries out type checking on a call command node
        /// </summary>
        /// <param name="callCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnCallCommand(CallCommandNode callCommand)
        {
            PerformTypeChecking(callCommand.Identifier);
            PerformTypeChecking(callCommand.Parameter);

            if (!(callCommand.Identifier.Declaration is FunctionDeclarationNode functionDeclaration))
            {
                // Error: Identifier is not a function
                WriteError($"indentifier '{callCommand.Identifier.IdentifierToken.Spelling}' does not represent a function.", callCommand);
            }
            else if (GetNumberOfArguments(functionDeclaration.Type) == 0)
            {
                if (!(callCommand.Parameter is BlankParameterNode))
                {
                    // Error: function takes no arguments but is called with one
                    WriteError($"function's takes no arguments but has been called with one.", callCommand);
                }
            }
            else
            {
                if (callCommand.Parameter is BlankParameterNode)
                {
                    // Error: function takes an argument but is called without one
                    WriteError($"function's requires an argument but has not been given one.", callCommand);
                }
                else
                {
                    if (GetArgumentType(functionDeclaration.Type, 0) != callCommand.Parameter.Type)
                    {
                        // Error: Function called with parameter of the wrong type
                        //WriteError($"call command's function's passed parameter is of the wrong type, requires {GetArgumentType(functionDeclaration.Type, 0).ToString()}.", callCommand); //Works without using a function as parameter however type checker has issue analysis return type of call expression
                    }
                    if (ArgumentPassedByReference(functionDeclaration.Type, 0))
                    {
                        if (!(callCommand.Parameter is VarParameterNode))
                        {
                            // Error: Function requires a var parameter but has been given an expression parameter
                            WriteError($"function requires a var parameter but has been given an expression parameter.", callCommand);
                        }
                    }
                    else
                    {
                        if (!(callCommand.Parameter is ExpressionParameterNode))
                        {
                            // Error: Function requires an expression parameter but has been given a var parameter
                            WriteError($"function requires an expression parameter but has been given a var parameter.", callCommand);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carries out type checking on a quick if command node
        /// </summary>
        /// <param name="quickIfCommand">The node perform type checking on</param>
        private void PerformTypeCheckingOnQuickIfCommand(QuickIfCommandNode quickIfCommand)
        {
            PerformTypeChecking(quickIfCommand.Expression);
            PerformTypeChecking(quickIfCommand.Command);
            if (quickIfCommand.Expression.Type != StandardEnvironment.BooleanType)
            {
                // Error: expression needs to be a boolean
                WriteError($"quick if command's expression needs to be a boolean.", quickIfCommand);
            }
        }

        /// <summary>
        /// Carries out type checking on an if command node
        /// </summary>
        /// <param name="ifCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIfCommand(IfCommandNode ifCommand)
        {
            PerformTypeChecking(ifCommand.Expression);
            PerformTypeChecking(ifCommand.ThenCommand);
            PerformTypeChecking(ifCommand.ElseCommand);
            if (ifCommand.Expression.Type != StandardEnvironment.BooleanType)
            {
                // Error: expression needs to be a boolean
                WriteError($"if command's expression needs to be a boolean not {ifCommand.Expression.Type.ToString()}.", ifCommand);
            }
        }

        /// <summary>
        /// Carries out type checking on a let command node
        /// </summary>
        /// <param name="letCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnLetCommand(LetCommandNode letCommand)
        {
            PerformTypeChecking(letCommand.Declaration);
            PerformTypeChecking(letCommand.Command);
        }

        /// <summary>
        /// Carries out type checking on a sequential command node
        /// </summary>
        /// <param name="sequentialCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnSequentialCommand(SequentialCommandNode sequentialCommand)
        {
            foreach (ICommandNode command in sequentialCommand.Commands)
                PerformTypeChecking(command);
        }

        /// <summary>
        /// Carries out type checking on a while command node
        /// </summary>
        /// <param name="whileCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnWhileCommand(WhileCommandNode whileCommand)
        {
            PerformTypeChecking(whileCommand.Expression);
            PerformTypeChecking(whileCommand.Command);
            if (whileCommand.Expression.Type != StandardEnvironment.BooleanType)
            {
                // Error: expression needs to be a boolean
                WriteError($"while command's expression needs to be a boolean not {whileCommand.Expression.Type.ToString()}.", whileCommand);
            }
        }

        /// <summary>
        /// Carries out type checking on a loop command node
        /// </summary>
        /// <param name="loopCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnLoopCommand(LoopCommandNode loopCommand)
        {
            PerformTypeChecking(loopCommand.LoopCommand);
            PerformTypeChecking(loopCommand.Expression);
            PerformTypeChecking(loopCommand.WhileCommand);
            if (loopCommand.Expression.Type != StandardEnvironment.BooleanType)
            {
                // Error: expression needs to be a boolean
                WriteError($"loop command's expression needs to be a boolean not {loopCommand.Expression.Type.ToString()}.", loopCommand);
            }
        }

        /// <summary>
        /// Carries out type checking on a const declaration node
        /// </summary>
        /// <param name="constDeclaration"The node to perform type checking on></param>
        private void PerformTypeCheckingOnConstDeclaration(ConstDeclarationNode constDeclaration)
        {
            PerformTypeChecking(constDeclaration.Identifier);
            PerformTypeChecking(constDeclaration.Expression);
        }

        /// <summary>
        /// Carries out type checking on a sequential declaration node
        /// </summary>
        /// <param name="sequentialDeclaration">The node to perform type checking on</param>
        private void PerformTypeCheckingOnSequentialDeclaration(SequentialDeclarationNode sequentialDeclaration)
        {
            foreach (IDeclarationNode declaration in sequentialDeclaration.Declarations)
                PerformTypeChecking(declaration);
        }

        /// <summary>
        /// Carries out type checking on a var declaration node
        /// </summary>
        /// <param name="varDeclaration">The node to perform type checking on</param>
        private void PerformTypeCheckingOnVarDeclaration(VarDeclarationNode varDeclaration)
        {
            PerformTypeChecking(varDeclaration.TypeDenoter);
            PerformTypeChecking(varDeclaration.Identifier);
        }



        /// <summary>
        /// Carries out type checking on a binary expression node
        /// </summary>
        /// <param name="binaryExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnBinaryExpression(BinaryExpressionNode binaryExpression)
        {
            PerformTypeChecking(binaryExpression.Op);
            PerformTypeChecking(binaryExpression.LeftExpression);
            PerformTypeChecking(binaryExpression.RightExpression);
            if (!(binaryExpression.Op.Declaration is BinaryOperationDeclarationNode opDeclaration))
            {
                // Error: operator is not a binary operator
                WriteError("binary expression's operator is not a binary operator.", binaryExpression);
            }
            else
            {
                if (GetArgumentType(opDeclaration.Type, 0) == StandardEnvironment.AnyType)
                {
                    if (binaryExpression.LeftExpression.Type != binaryExpression.RightExpression.Type)
                    {
                        // Error: left and right hand side arguments not the same type
                        WriteError($"binary expression's arguments have to be the same type.", binaryExpression);
                    }
                }
                else
                {
                    if (GetArgumentType(opDeclaration.Type, 0) != binaryExpression.LeftExpression.Type)
                    {
                        // Error: Left hand expression is wrong type
                        WriteError($"left hand expression is the wrong type.", binaryExpression);
                    }
                    if (GetArgumentType(opDeclaration.Type, 1) != binaryExpression.RightExpression.Type)
                    {
                        // Error: Right hand expression is wrong type
                        WriteError($"right hand expression is the wrong type.", binaryExpression);
                    }
                }
                binaryExpression.Type = GetReturnType(opDeclaration.Type);
            }
        }

        /// <summary>
        /// Carries out type checking on a character expression node
        /// </summary>
        /// <param name="characterExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnCharacterExpression(CharacterExpressionNode characterExpression)
        {
            PerformTypeChecking(characterExpression.CharLit);
            characterExpression.Type = StandardEnvironment.CharType;
        }

        /// <summary>
        /// Carries out type checking on an ID expression node
        /// </summary>
        /// <param name="idExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIdExpression(IdExpressionNode idExpression)
        {
            PerformTypeChecking(idExpression.Identifier);
            if (!(idExpression.Identifier.Declaration is IEntityDeclarationNode declaration))
            {
                // Error: identifier is not a variable or constant
                WriteError($"Identifier '{idExpression.Identifier.IdentifierToken.Spelling}' isn't a variable or a constant.", idExpression);
            }
            else
                idExpression.Type = declaration.EntityType;
        }

        /// <summary>
        /// Carries out type checking on a call expression node
        /// </summary>
        /// <param name="callExpression"></param>
        private void PerformTypeCheckingOnCallExpression(CallExpressionNode callExpression)
        {
            PerformTypeChecking(callExpression.Identifier);
            PerformTypeChecking(callExpression.Parameter);

            if (!(callExpression.Identifier.Declaration is FunctionDeclarationNode functionDeclaration))
            {
                // Error: Identifier is not a function
                WriteError($"indentifier '{callExpression.Identifier.IdentifierToken.Spelling}' does not represent a function.", callExpression);
            }
            else if (GetNumberOfArguments(functionDeclaration.Type) == 0)
            {
                if (!(callExpression.Parameter is BlankParameterNode))
                {
                    // Error: function takes no arguments but is called with one
                    WriteError($"call expression's function requires an argument but has not been given one.", callExpression);
                }
            }
            else if (IsProcedure(functionDeclaration))
            {
                // Error: Function is a procedure
                WriteError("call expression's function has to be a procedure.", callExpression);
            }
            else
            {
                if (callExpression.Parameter is BlankParameterNode)
                {
                    // Error: function takes an argument but is called without one
                    WriteError($"call expression's function requires an argument but has not been given one.", callExpression);
                }
                else
                {
                    if (GetArgumentType(functionDeclaration.Type, 0) != callExpression.Parameter.Type)
                    {
                        // Error: Function called with parameter of the wrong type
                        WriteError($"call expression's function's passed parameter is of the wrong type, requires {GetArgumentType(functionDeclaration.Type, 0).ToString()}.", callExpression);

                    }
                    if (ArgumentPassedByReference(functionDeclaration.Type, 0))
                    {
                        if (!(callExpression.Parameter is VarParameterNode))
                        {
                            // Error: Function requires a var parameter but has been given an expression parameter
                            WriteError($"call expression's function requires a var parameter but has been given an expression parameter.", functionDeclaration);
                        }
                    }
                    else
                    {
                        if (!(callExpression.Parameter is ExpressionParameterNode))
                        {
                            // Error: Function requires an expression parameter but has been given a var parameter
                            WriteError($"call expression's function requires an expression parameter but has been given a var parameter.", functionDeclaration);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carries out type checking on a  node
        /// </summary>
        /// <param name="integerExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIntegerExpression(IntegerExpressionNode integerExpression)
        {
            PerformTypeChecking(integerExpression.IntLit);
            integerExpression.Type = StandardEnvironment.IntegerType;
        }

        /// <summary>
        /// Carries out type checking on a unary expression node
        /// </summary>
        /// <param name="unaryExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnUnaryExpression(UnaryExpressionNode unaryExpression)
        {
            PerformTypeChecking(unaryExpression.Op);
            PerformTypeChecking(unaryExpression.Expression);
            if (!(unaryExpression.Op.Declaration is UnaryOperationDeclarationNode opDeclaration))
            {
                // Error: operator is not a unary operator
                WriteError("operator is not a unary operator.", unaryExpression);
            }
            else
            {
                if (GetArgumentType(opDeclaration.Type, 0) != unaryExpression.Expression.Type)
                {
                    // Error: expression is the wrong type
                    WriteError("unary expression is of the wrong type.", unaryExpression);
                }
                unaryExpression.Type = GetReturnType(opDeclaration.Type);
            }
        }



        /// <summary>
        /// Carries out type checking on a blank parameter
        /// </summary>
        /// <param name="blankParameter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnBlankParameter(BlankParameterNode blankParameter)
        {
        }

        /// <summary>
        /// Carries out type checking on an expression parameter node
        /// </summary>
        /// <param name="expressionParameter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnExpressionParameter(ExpressionParameterNode expressionParameter)
        {
            PerformTypeChecking(expressionParameter.Expression);
            expressionParameter.Type = expressionParameter.Expression.Type;
        }

        /// <summary>
        /// Carries out type checking on a var parameter node
        /// </summary>
        /// <param name="varParameter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnVarParameter(VarParameterNode varParameter)
        {
            PerformTypeChecking(varParameter.Identifier);
            if (!(varParameter.Identifier.Declaration is IVariableDeclarationNode varDeclaration))
            {
                // Error: identifier is not a variable
                WriteError($"indentifier '{varParameter.Identifier.IdentifierToken.Spelling.ToString()}' is not a vairable.", varParameter);
            }
            else
                varParameter.Type = varDeclaration.EntityType;
        }



        /// <summary>
        /// Carries out type checking on a type denoter node
        /// </summary>
        /// <param name="typeDenoter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnTypeDenoter(TypeDenoterNode typeDenoter)
        {
            PerformTypeChecking(typeDenoter.Identifier);
            if (!(typeDenoter.Identifier.Declaration is SimpleTypeDeclarationNode declaration))
            {
                // Error: identifier is not a type
                WriteError($"indentifier {typeDenoter.Identifier.IdentifierToken.Spelling.ToString()} is not a type.", typeDenoter);
            }
            else
                typeDenoter.Type = declaration;
        }



        /// <summary>
        /// Carries out type checking on a character literal node
        /// </summary>
        /// <param name="characterLiteral">The node to perform type checking on</param>
        private void PerformTypeCheckingOnCharacterLiteral(CharacterLiteralNode characterLiteral)
        {
            if (characterLiteral.Value < short.MinValue || characterLiteral.Value > short.MaxValue)
            {
                // Error - value too big
                WriteError("Value is too big.", characterLiteral);
            }
        }

        /// <summary>
        /// Carries out type checking on an identifier node
        /// </summary>
        /// <param name="identifier">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIdentifier(IdentifierNode identifier)
        {
        }

        /// <summary>
        /// Carries out type checking on an integer literal node
        /// </summary>
        /// <param name="integerLiteral">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIntegerLiteral(IntegerLiteralNode integerLiteral)
        {
            if (integerLiteral.Value < short.MinValue || integerLiteral.Value > short.MaxValue)
            {
                // Error - value too big
                WriteError($"value {integerLiteral.IntegerLiteralToken.Spelling.ToString()} is too big, max is {short.MaxValue}.", integerLiteral);
            }
        }

        /// <summary>
        /// Carries out type checking on an operation node
        /// </summary>
        /// <param name="operation">The node to perform type checking on</param>
        private void PerformTypeCheckingOnOperator(OperatorNode operation)
        {
        }


        /// <summary>
        /// Gets the number of arguments that a function takes
        /// </summary>
        /// <param name="node">The function</param>
        /// <returns>The number of arguments taken by the function</returns>
        private static int GetNumberOfArguments(FunctionTypeDeclarationNode node)
        {
            return node.Parameters.Length;
        }

        /// <summary>
        /// Gets the type of a function's argument
        /// </summary>
        /// <param name="node">The function</param>
        /// <param name="argument">The index of the argument</param>
        /// <returns>The type of the given argument to the function</returns>
        private static SimpleTypeDeclarationNode GetArgumentType(FunctionTypeDeclarationNode node, int argument)
        {
            return node.Parameters[argument].type;
        }

        /// <summary>
        /// Gets the whether an argument to a function is passed by reference
        /// </summary>
        /// <param name="node">The function</param>
        /// <param name="argument">The index of the argument</param>
        /// <returns>True if and only if the argument is passed by reference</returns>
        private static bool ArgumentPassedByReference(FunctionTypeDeclarationNode node, int argument)
        {
            return node.Parameters[argument].byRef;
        }

        /// <summary>
        /// Gets the return type of a function
        /// </summary>
        /// <param name="node">The function</param>
        /// <returns>The return type of the function</returns>
        private static SimpleTypeDeclarationNode GetReturnType(FunctionTypeDeclarationNode node)
        {
            return node.ReturnType;
        }

        /// <summary>
        /// Write error message from the type checker
        /// </summary>
        /// <param name="errorMessage">message to be put into the error message</param>
        private void WriteError(String errorMessage, IAbstractSyntaxTreeNode node)
        {
            Console.WriteLine("Error at " + node.Position.ToString() + ", " + errorMessage);
            Reporter.HasErrors = true;
        }

        /// <summary>
        /// Check wether a indentifier is a function or a procedure
        /// </summary>
        /// <returns>The indentifier to check</returns>
        private static Boolean IsProcedure(FunctionDeclarationNode functionDeclaration)
        {   
            switch (functionDeclaration.Name)
            {
                case "chr":
                case "ord":
                case "eof":
                case "eol":
                    return false;
                default:
                    return true;
            }
        }
    }
}
