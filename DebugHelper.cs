using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal static class DebugHelper
    {
        internal static void ConsoleOutScannerDebugInfo(bool isDebug,int start ,int current, int line,string source, List<Token> tokens)
        {
            if (!isDebug)
            {
                return;
            }

            Console.WriteLine("+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-");
            Console.WriteLine("start={0}", start);
            Console.WriteLine("current={0}", current);
            Console.WriteLine("line={0}", line);
            Console.WriteLine("source={0}", source);
            Console.WriteLine(" ");

            var count = 0;
            foreach (char c in source)
            {
                if (count == current)
                {
                    Console.Write(" |");
                }
                Console.Write(c); // 一文字ずつ出力
                if (count == current)
                {
                    Console.Write("| ");
                }
                count++;
            }

            Console.WriteLine(" ");
            Console.WriteLine(" ");
            Console.WriteLine("~Tokens~");

            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }
        }
        internal static void ConsoleOutParserDebugInfo(bool isDebug,int current, List<Token> tokens, List<Stmt> stmts)
        {
            if (!isDebug)
            {
                return;
            }

            Console.WriteLine("+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-");
            Console.WriteLine("current: " + current);
            Console.WriteLine("tokens: " + tokens.Count);
            Console.WriteLine(" ");

            var count = 0;
            foreach (var token in tokens)
            {
                if (count == current)
                {
                    Console.Write(" *");
                }
                Console.Write(token.lexeme);
                Console.Write(" _ ");
                count++;
            }

            Console.WriteLine(" ");
            Console.WriteLine(" ");
            Console.WriteLine("+-curret-+");
            Console.WriteLine(" ");
            Console.WriteLine("tokens[current].type: " + tokens[current].type);
            Console.WriteLine("tokens[current].lexeme: " + tokens[current].lexeme);
            Console.WriteLine("tokens[current].literal: " + tokens[current].literal);
            Console.WriteLine("tokens[current].line: " + tokens[current].line);
            Console.WriteLine(" ");
            Console.WriteLine("stmt");

            foreach (var stmt in stmts)
            {
                Console.WriteLine(stmt);
            }
        }
    }
}

