using System.Text;

namespace Lox
{
    internal class Program
    {
        private static readonly Interpreter _interpreter = new Interpreter();
        static bool hadError = false;
        static bool hadRuntimeError = false;
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: C#lox [script]");
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                runFile(args[0]);
            }
            else
            {
                runPrompt();
            }

        }

        private static void runFile(string path)
        {
            try
            {
                //byte[] fileBytes = File.ReadAllBytes(path);
                //string fileContent = Encoding.UTF8.GetString(fileBytes);
                string fileContent =File.ReadAllText(path);
                //Console.WriteLine($"{path} {fileContent}");
                run(fileContent);

                if (hadError)
                {
                    Environment.Exit(64);
                }

                if (hadRuntimeError)
                {
                    Environment.Exit(70);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("An IO exception has been thrown!");
                Console.WriteLine(e.ToString());
            }
        }

        private static void runPrompt()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            for (; ; )
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == null)
                {
                    break;
                }
                run(line);
                hadError = false;
            }
        }

        private static void run(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.scanTokens();
            var parser = new Parser(tokens);
            List<Stmt> statements = parser.parse();

            if (hadError)
            {
                return;
            }

            Resolver resolver = new Resolver(_interpreter);
            resolver.resolve(statements);

            if (hadError)
            {
                return;
            }

            _interpreter.interpret(statements);

            //Console.WriteLine(new AstPrinter().print(expression));
        }

        internal static void error(int line, string message)
        {
            report(line, "", message);
        }

        internal static void report(int line, string where, string message) { 
            Console.Error.WriteLine(" [line " + line + "] Error " + where + ": " + message );
            hadError = true;
        }

        internal static void error(Token token,string message)
        {
            if (token.type == TokenType.EOF)
            {
                report(token.line, " at end", message);
            }
            else
            {
                report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        internal static void runtimeError(RuntimeError error)
        {
            Console.Error.WriteLine(error.Message + Environment.NewLine + " [line " + error.Token.line + "]");
            hadRuntimeError = true;
        }
    }
}
