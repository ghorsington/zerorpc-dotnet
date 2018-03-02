namespace ZeroRpc.Net.Data
{
    /// <summary>
    ///     A class that represents information about an error.
    /// </summary>
    public class ErrorInformation
    {
        /// <summary>
        ///     Initializes the error information.
        /// </summary>
        /// <param name="name">Name of the error.</param>
        /// <param name="message">A message that briefly describes the error.</param>
        /// <param name="stack">An optional stack trace.</param>
        public ErrorInformation(string name, string message, string stack = "")
        {
            Name = name;
            Message = message;
            StackTrace = stack;
        }

        /// <summary>
        ///     Brief description of the error.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Name of the error.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     An optional stack trace releated to the error.
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        ///     Converts the error information into an array that can be serialized through Msgpack.
        /// </summary>
        /// <returns>A string array representation of the error information.</returns>
        public object[] ToArray()
        {
            return new object[] {Name, Message, StackTrace};
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name}; {Message}.{(string.IsNullOrEmpty(StackTrace) ? string.Empty : $" Stack trace: {StackTrace}")}";
        }
    }
}