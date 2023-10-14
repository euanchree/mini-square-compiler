namespace Compiler.Nodes
{
    /// <summary>
    /// A node corresponding to a loop command
    /// </summary>
    public class LoopCommandNode : ICommandNode
    {
        /// <summary>
        /// The command inside the loop
        /// </summary>
        public ICommandNode LoopCommand { get; }

        /// <summary>
        /// The condition associated with the loop
        /// </summary>
        public IExpressionNode Expression { get; }

        /// <summary>
        /// The command inside the while
        /// </summary>
        public ICommandNode WhileCommand { get; }

        /// <summary>
        /// The position in the code where the content associated with the node begins
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Creates a new while node
        /// </summary>
        /// <param name="loopCommand">The command initial command</param>
        /// <param name="expression">The condition associated with the loop</param>
        /// <param name="whileCommand">The command inside the loop</param>
        /// <param name="position">The position in the code where the content associated with the node begins</param>
        public LoopCommandNode(ICommandNode loopCommand, IExpressionNode expression, ICommandNode whileCommand, Position position)
        {
            LoopCommand = loopCommand;
            Expression = expression;
            WhileCommand = whileCommand;
            Position = position;
        }
    }
}