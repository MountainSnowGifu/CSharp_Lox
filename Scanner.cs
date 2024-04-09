using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int start = 0;
        private int current = 0;
        private int line = 1;
        private bool isDebug = false;

        private readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
        {
            {"and", TokenType.AND},
            {"class", TokenType.CLASS},
            {"else", TokenType.ELES},
            {"false", TokenType.FALSE},
            {"for", TokenType.FOR},
            {"fun", TokenType.FUN},
            {"if", TokenType.IF},
            {"nil", TokenType.NIL},
            {"or", TokenType.OR},
            {"print", TokenType.PRINT},
            {"return", TokenType.RETURN},
            {"super", TokenType.SUPER},
            {"this", TokenType.THIS},
            {"true", TokenType.TRUE},
            {"var", TokenType.VAR},
            {"while", TokenType.WHILE},
        };


        internal Scanner(string source)
        {
            this.source = source;
        }

        internal List<Token> scanTokens()
        {
            DebugHelper.ConsoleOutScannerDebugInfo(isDebug,start, current, line, source, tokens);
            while (!isAtEnd())
            {
                start = current;
                scanToken();
                DebugHelper.ConsoleOutScannerDebugInfo(isDebug, start, current, line, source, tokens);
            }

            tokens.Add(new Token(TokenType.EOF, "", null, line));
            DebugHelper.ConsoleOutScannerDebugInfo(isDebug, start, current, line, source, tokens);
            return tokens;
        }

        //ループを繰り返すたびに一個のトークンをスキャンする
        private void scanToken()
        {
            char c = Advance();

            switch (c)
            {
                //一文字のトークン
                case '(': AddToken(TokenType.LEFT_PAREN); break;
                case ')': AddToken(TokenType.RIGHT_PAREN); break;
                case '{': AddToken(TokenType.LEFT_BRACE); break;
                case '}': AddToken(TokenType.RIGHT_BRACE); break;

                case ',': AddToken(TokenType.COMMA); break;
                case '.': AddToken(TokenType.DOT); break;
                case '-': AddToken(TokenType.MINUS); break;

                case '+': AddToken(TokenType.PLUS); break;
                case ';': AddToken(TokenType.SEMICOLON); break;
                case '*': AddToken(TokenType.STAR); break;

                case '!': AddToken(match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                case '=': AddToken(match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL); break;
                case '<': AddToken(match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                case '>': AddToken(match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;

                case '/':
                    if (match('/')) //一致すれば消費する
                    {
                        //行末に到達するまで残りの文字を消費する
                        while (peek() != '\n' && !isAtEnd())
                        {
                            Advance();
                        }
                    }
                    else
                    {
                        AddToken(TokenType.SLASH);
                    }
                    break;

                case ' ':
                case '\r':
                case '\t':
                    break;//空白文字は無視する
                case '\n':
                    line++;
                    break;

                case '"': lox_string(); break;
                default:
                    //数字かどうかを判定する、数字から始まるなら数値として扱う
                    if (isDigit(c))
                    {
                        number();
                    }
                    else if (isAlpha(c))//アルファベットか_から始まるならすぐに識別子としてみなす
                    {
                        identifier();
                    }
                    else
                    {
                        //エラーを起こす文字も事前に呼び出したadvanceで消費している。
                        //無限ループを防ぐため
                        //想定外の文字
                        Lox.Program.error(line, "Unexpected character.");
                    }
                    break;
            }
        }

        private void identifier()
        {
            //アルファベットか数字が続く限り消費する
            while (isAlphaNumeric(peek()))
            {
                Advance();
            }

            //識別子の文字列を取得する
            string text = source.Substring(start, current - start);

            //キーワードが見つからなければ、default valueはIDENTIFIER
            TokenType type = keywords.GetValueOrDefault(text, TokenType.IDENTIFIER);
            AddToken(type);
        }
        private void number()
        {
            //数字が続く限り消費する
            while (isDigit(peek()))
            {
                Advance();
            }

            //小数部を探す
            if (peek() == '.' && isDigit(peekNext()))
            {
                Advance();//小数点を消費する

                //小数部が続く限り消費する
                while (isDigit(peek()))
                {
                    Advance();
                }
            }

            //文字列を数値に変換してトークンに追加する
            var value = source.Substring(start, current - start);
            AddToken(TokenType.NUMBER, double.Parse(value));
        }

        private void lox_string()
        {
            //先読みが"かつ最後になるまで文字を消費する
            while (peek() != '"' && !isAtEnd())
            {
                if (peek() == '\n')
                {
                    line++;
                }
                Advance();
            }

            if (isAtEnd())
            {
                Lox.Program.error(line, "Unterminated string.");//文字列が閉じられていない
                return;
            }

            Advance();//右側の"を消費する

            string value = source.Substring(start + 1, current - start - 2);//左右の"を切り捨てる
            AddToken(TokenType.STRING, value);
        }

        //条件付きadvanceのような関数
        //次の文字がexpectedと一致するかどうか
        //一致した場合は、currentをインクリメント(消費)してtrueを返す
        private bool match(char expected)
        {
            if (isAtEnd())
            {
                return false;
            }

            if (source[current] != expected)
            {
                return false;
            }

            current++;
            return true;
        }

        //文字を消費せずに次の文字を返す
        //文字を消費しない先読み
        private char peek()
        {
            if (isAtEnd())
            {
                return '\0';//null文字
            }
            return source[current];
        }

        //文字を消費せずに次の次文字を返す
        //文字を消費しない先読み 小数点の後に数字が一個みつかるまで、現在の.を消費したくない
        private char peekNext()
        {
            if (current + 1 >= source.Length)
            {
                return '\0';
            }
            return source[current + 1];
        }

        private bool isAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                    c == '_';
        }

        private bool isAlphaNumeric(char c)
        {
            return isAlpha(c) || isDigit(c);
        }

        private bool isDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        //すべての文字を消費しつくしたかどうか
        private bool isAtEnd()
        {
            return current >= source.Length;
        }

        //現在の文字を返し、currentをインクリメントする
        //次の文字を消費して、それを返す
        private char Advance()
        {
            return source[current++];
        }

        //トークンを追加する
        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        //リテラルを持つトークンを追加する
        private void AddToken(TokenType type, object literal)
        {
            var text = source.Substring(start, current - start);
            tokens.Add(new Token(type, text, literal, line));
        }
    }
}
