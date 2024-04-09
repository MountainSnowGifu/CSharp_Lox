using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class RuntimeError : SystemException
    {
        internal Token Token { get; }

        internal RuntimeError(Token token, string message) : base(message)
        {
            this.Token = token;
        }
    }
}
