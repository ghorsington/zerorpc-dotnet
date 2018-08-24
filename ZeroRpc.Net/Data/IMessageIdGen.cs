using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroRpc.Net.Data
{
    /// <summary>
    /// A message ID generator used to assign messages to correct channels
    /// </summary>
    public interface IMessageIdGen
    {
        /// <summary>
        /// Generate a new ID for the next message.
        /// </summary>
        /// <returns>An object that represents a message id.</returns>
        object Next();
    }
}
