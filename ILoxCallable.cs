using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lox
{
    internal interface ILoxCallable
    {
        int arity();
        object call(Interpreter interpreter, List<object> arguments);
    }

    internal class LoxReadLineCallable : ILoxCallable
    {
        public int arity()
        {
            return 0;
        }

        public object call(Interpreter interpreter, List<object> arguments)
        {
            string? name = Console.ReadLine();
            if (name == null)
            {
                return string.Empty;
            }

            return name;
        }

        public override string ToString()
        {
            return "<native fn>";
        }
    }

    internal class LoxClockCallable : ILoxCallable
    {
        public int arity()
        {
            return 0;
        }

        public object call(Interpreter interpreter, List<object> arguments)
        {
            return (double)Environment.TickCount;//java system.currentTimeMillis と挙動が違うよ
        }

        public override string ToString()
        {
            return "<native fn>";
        }
    }
}
