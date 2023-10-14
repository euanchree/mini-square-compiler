using Compiler.IO;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Console;

namespace Compiler.Tokenization
{
    /// <summary>
    /// A tokenizer for the reader language
    /// </summary>
    public class Tokenizer
    {
        /// <summary>
        /// The error reporter
        /// </summary>
        public ErrorReporter Reporter { get; }

        /// <summary>
        /// The reader getting the characters from the file
        /// </summary>
        private IFileReader Reader { get; }

        /// <summary>
        /// The characters currently in the token
        /// </summary>
        private StringBuilder TokenSpelling { get; } = new StringBuilder();

        /// <summary>
        /// Createa a new tokenizer
        /// </summary>
        /// <param name="reader">The reader to get characters from the file</param>
        /// <param name="reporter">The error reporter to use</param>
        public Tokenizer(IFileReader reader, ErrorReporter reporter)
        {
            Reader = reader;
            Reporter = reporter;
        }

        /// <summary>
        /// Gets all the tokens from the file
        /// </summary>
        /// <returns>A list of all the tokens in the file in the order they appear</returns>
        public List<Token> GetAllTokens()
        {
            List<Token> tokens = new List<Token>();
            Token token = GetNextToken();
            while (token.Type != TokenType.EndOfText)
            {
                tokens.Add(token);
                token = GetNextToken();
            }
            tokens.Add(token);
            Reader.Close();
            return tokens;
        }

        /// <summary>
        /// Scan the next token
        /// </summary>
        /// <returns>True if and only if there is another token in the file</returns>
        private Token GetNextToken()
        {
            // Skip forward over any white spcae and comments
            SkipSeparators();

            // Remember the starting position of the token
            Position tokenStartPosition = Reader.CurrentPosition;

            // Scan the token and work out its type
            TokenType tokenType = ScanToken();

            // Create the token
            Token token = new Token(tokenType, TokenSpelling.ToString(), tokenStartPosition);
            Debugger.Write($"Scanned {token}");

            // Report an error if necessary
            if (tokenType == TokenType.Error)
            {
                // Report the error here
                Reporter.HasErrors = true;
               
            }

            return token;
        }

        /// <summary>
        /// Skip forward until the next character is not whitespace or a comment ("&" character)
        /// </summary>
        private void SkipSeparators()
        {
            while (Reader.Current == '&' || IsWhiteSpace(Reader.Current))
            {
                if (Reader.Current == '&')
                    // Skip rest of line
                    Reader.SkipRestOfLine();
                else
                    // Skip only the white space
                    Reader.MoveNext();
            }
        }

        /// <summary>
        /// Find the next token
        /// </summary>
        /// <returns>The type of the next token</returns>
        /// <remarks>Sets tokenSpelling to be the characters in the token</remarks>
        private TokenType ScanToken()
        {
            TokenSpelling.Clear();

            if (char.IsLetterOrDigit(Reader.Current))
            {
                // Reading an keywork, identifier or an int literal
                TakeIt();
                while (char.IsLetterOrDigit(Reader.Current))
                    TakeIt();
                if (TokenTypes.IsKeyword(TokenSpelling))
                {
                    // Returning a keyword token
                    return TokenTypes.GetTokenForKeyword(TokenSpelling);
                }
                else
                {
                    if (IsAllDigits(TokenSpelling.ToString()))
                        // Returning an int literal
                        return TokenType.IntLiteral;
                    else
                        // Returning an indentifier
                        return TokenType.Identifier;
                }
            }
            else if (IsOperator(Reader.Current))
            {
                // Reading an operator or quick if
                TakeIt();
                if (Reader.Current == '>')
                {
                    // Return quick if token
                    TakeIt();
                    return TokenType.QuickIf;
                } 
                else
                {   
                    // Return an operator
                    return TokenType.Operator;
                }
            }
            else if (Reader.Current == '(')
            {
                // Read a (
                TakeIt();
                return TokenType.LeftBracket;
            }
            else if (Reader.Current == ')')
            {
                // Read a )
                TakeIt();
                return TokenType.RightBracket;
            }
            else if (Reader.Current == ';')
            {
                // Read a ;
                TakeIt();
                return TokenType.Semicolon;
            }
            else if (Reader.Current == '?')
            {
                // Read a ?
                TakeIt();
                return TokenType.QuestionMark;
            }
            else if (Reader.Current == '~')
            {
                // Read a ~
                TakeIt();
                return TokenType.Is;
            } else if (Reader.Current == '{')
            {
                // Reading a graphic
                TakeIt();
                if (char.IsLetterOrDigit(Reader.Current) | Reader.Current == ' ' | Reader.Current == '?')
                {
                    TakeIt();

                    if (Reader.Current == '}')
                    {   
                        // Return a char literal
                        TakeIt();
                        return TokenType.CharLiteral;
                    }
                    else
                    {   
                        // Report error
                        WriteError("character literal must be closed with a '}'.");
                        return TokenType.Error;
                    }
                }
                else
                {   
                    // Report error
                    WriteError(("graphic can only be a letter, digit, space or ?. Not '" + Reader.Current + "'."));
                    return TokenType.Error;
                }
            }
            else if (Reader.Current == default(char))
            {
                // Read the end of the file
                TakeIt();
                return TokenType.EndOfText;
            }
            else
            {
                // Encountered a character we weren't expecting
                TakeIt();
                WriteError(("'" + TokenSpelling.ToString() + "' is invalid and wasn't expected."));
                return TokenType.Error;
            }
        }

        /// <summary>
        /// Appends the current character to the current token then moves to the next character
        /// </summary>
        private void TakeIt()
        {
            TokenSpelling.Append(Reader.Current);
            Reader.MoveNext();
        }

        /// <summary>
        /// Checks if a current token is all digits
        /// </summary>
        /// <param name="tokenSpelling"></param>
        /// <returns></returns>
        private Boolean IsAllDigits(String TokenSpelling)
        {   
            Boolean result = true;

            foreach (char currentChar in TokenSpelling)
            {
                if (!char.IsDigit(currentChar))
                        result = false;
            }

            return result;
        }

        /// <summary>
        /// Write error message
        /// </summary>
        /// <param name="errorMessage"></param>
        private void WriteError(String errorMessage)
        {
            WriteLine("Error at " + Reader.CurrentPosition + ", " + errorMessage);
        }

        /// <summary>
        /// Checks whether a character is white space
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if and only if c is a whitespace character</returns>
        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }

        /// <summary>
        /// Checks whether a character is an operator
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if and only if the character is an operator in the language</returns>
        private static bool IsOperator(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '<':
                case '>':
                case '=':
                case '!':
                    return true;
                default:
                    return false;
            }
        }
    }
}
