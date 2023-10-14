namespace Compiler.Nodes
{
    /// <summary>
    /// A node corresponding to a quick if command
    /// </summary>
    public class QuickIfCommandNode : ICommandNode
    {
        /// <summary>
        /// The condition expression
        /// </summary>
        public IExpressionNode Expression { get; }

        /// <summary>
        /// The then branch command
        /// </summary>
        public ICommandNode Command { get; }

        /// <summary>
        /// The position in the code where the content associated with the node begins
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Creates a new if command node
        /// </summary>
        /// <param name="expression">The condition expression</param>
        /// <param name="command">The command</param>
        /// <param name="position">The position in the code where the content associated with the node begins</param>
        public QuickIfCommandNode(IExpressionNode expression, ICommandNode command, Position position)
        {
            Expression = expression;
            Command = command;
            Position = position;
        }
    }
}