using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lox
{
    //再帰降下パーサー　recursive descent
    internal class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;
        private class ParseError : Exception;
        private bool isDebug = false;

        internal Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        internal List<Stmt> parse()
        {

            var statements = new List<Stmt>();
            DebugHelper.ConsoleOutParserDebugInfo(isDebug, current, tokens, statements);

            while (!isAtEnd())
            {
                statements.Add(declaration());
                DebugHelper.ConsoleOutParserDebugInfo(isDebug, current, tokens, statements);
            }

            DebugHelper.ConsoleOutParserDebugInfo(isDebug, current, tokens, statements);
            return statements;
        }

        private Stmt declaration()
        {
            try
            {
                //クラスは先頭おキーワードで識別されるので、名前付きの宣言が許される場所ならどこにでもおける。
                if (match(TokenType.CLASS))
                {
                    return classDeclaration();
                }

                if (match(TokenType.FUN))
                {
                    return function("function");
                }

                if (match(TokenType.VAR))
                {
                    return varDeclaration();
                }

                return statement();
            }
            catch (ParseError error)
            {
                synchronize();
                return null;
            }
        }


        //すでにマッチでクラスキーワードを消費しているので、それに続く、クラス名と波カッコを期待する。
        //本文に入ったら、メソッド宣言の解析を閉じ波カッコがくるまで繰り返す
        //メソッド宣言はfunctionを呼び出して解析する。
        private Stmt classDeclaration()
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect class name.");
            consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");

            List<Stmt.Function> methods = new List<Stmt.Function>();
            while (!check(TokenType.RIGHT_BRACE) && !isAtEnd())
            {
                methods.Add(function("method"));
            }

            consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");

            return new Stmt.Class(name, methods);
        }

        private Stmt.Function function(string kind)
        {
            //識別トークンを消費
            Token name = consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");



            //パラメータリストとそれを囲む丸カッコのペアを解析する
            //区切りのカンマがあるかぎりパラメータ群を解析していく。
            consume(TokenType.LEFT_PAREN, "Expect '(' after " + kind + " name.");
            List<Token> parameters = new List<Token>();
            if (!check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count() >= 255)
                    {
                        error(peek(), "Can't have more then 255 parameters.");
                    }

                    parameters.Add(consume(TokenType.IDENTIFIER, "Expect parameter name."));
                } while (match(TokenType.COMMA));
            }

            consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");

            //block()を呼び出す前にここで本文の先頭にある｛を消費しているのは、すでに開き波カッコのトークンとの
            //マッチが成立していることがblock()呼び出しの前提になるからです。
            //ここなら関数宣言の文脈であることが明らかなので消費するはずの｛が見つからないときは詳細なエラーを報告できる
            consume(TokenType.LEFT_BRACE, "Expect '{' before " + kind + " body.");

            List<Stmt> body = block();
            return new Stmt.Function(name, parameters, body);
        }

        //varトークンはすでにマッチしている
        //識別子トークンを要求し、それを消費するconsume
        //もし＝を見つけたらそれを初期化式として解析する
        //Stmt.Varでラップして返却する
        private Stmt varDeclaration()
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect variable name.");

            Expr initializer = null;
            if (match(TokenType.EQUAL))
            {
                initializer = expression();
            }

            consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            return new Stmt.Var(name, initializer);
        }


        private Stmt statement()
        {
            if (match(TokenType.FOR))
            {
                return forStatement();
            }

            if (match(TokenType.IF))
            {
                return ifStatement();
            }

            if (match(TokenType.PRINT))
            {
                return printStatement();
            }

            if (match(TokenType.RETURN))
            {
                return returnStatement();
            }

            if (match(TokenType.WHILE))
            {
                return whileStatement();
            }

            if (match(TokenType.LEFT_BRACE))
            {
                return new Stmt.Block(block());
            }

            return expressionStatement();
        }

        private Stmt forStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (match(TokenType.SEMICOLON))
            {
                initializer = null;
            }
            else if (match(TokenType.VAR))
            {
                initializer = varDeclaration();
            }
            else
            {
                initializer = expressionStatement();
            }

            Expr condition = null;
            if (!check(TokenType.SEMICOLON))
            {
                condition = expression();
            }

            consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!check(TokenType.RIGHT_PAREN))
            {
                increment = expression();
            }

            consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

            Stmt body = statement();

            if (increment != null)
            {
                body = new Stmt.Block(
                    new List<Stmt> { body, new Stmt.Expression(increment) });
            }

            if (condition == null)
            {
                condition = new Expr.Literal(true);
            }

            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                body = new Stmt.Block(new List<Stmt> { initializer, body });
            }

            return body;
        }


        //elseが最も近い先行したifに結びつく。
        private Stmt ifStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt thenBranch = statement();
            Stmt elseBranch = null;

            if (match(TokenType.ELES))
            {
                elseBranch = statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        //printトークンは消費済み
        //続く部分式を解析して最後のセミコロンを消費して構文木を出力する
        private Stmt printStatement()
        {
            Expr value = expression();
            consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Print(value);
        }

        //消費済みのRETURNキーワードを取り出してから、戻り値の式を探す
        //戻り値の不在をチェックするセミコロンで式を始めることはできないので、もし次のトークンがセミコロンならそこに値がないことがわかる。
        private Stmt returnStatement()
        {
            Token keyword = previous();
            Expr value = null;
            if (!check(TokenType.SEMICOLON))
            {
                value = expression();
            }

            consume(TokenType.SEMICOLON, "Expect ';' after retun value.");
            return new Stmt.Return(keyword, value);
        }

        private Stmt whileStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = expression();
            consume(TokenType.RIGHT_PAREN, "Expect ') after condition.'");

            Stmt body = statement();

            return new Stmt.While(condition, body);
        }

        private List<Stmt> block()
        {
            List<Stmt> statements = new List<Stmt>();
            while (!check(TokenType.RIGHT_BRACE) && !isAtEnd())
            {
                statements.Add(declaration());
            }

            consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }


        //セミコロンで終わる一個の式を解析して、その式を式文に適した型Stmtの文にラップして返す。
        private Stmt expressionStatement()
        {
            Expr expr = expression();
            consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        private Expr expression()
        {
            return assignment();
        }

        //もし＝を見つけたら、右辺を解析して、そのすべてを代入式の木ノードでラップします。
        //代入は右結合なので、assignmentの再帰呼び出しで右辺を解析します。
        private Expr assignment()
        {
            Expr expr = or();

            if (match(TokenType.EQUAL))
            {
                Token equals = previous();
                Expr value = assignment();

                if (expr is Expr.Variable)
                {
                    Token name = ((Expr.Variable)expr).name;
                    return new Expr.Assign(name, value);
                }else if (expr is Expr.Get)
                {
                    //左辺のExpr.Get式をExpr.Setに変換　これでSetによる代入が解析される
                    Expr.Get get = (Expr.Get)expr;
                    return new Expr.Set(get._object, get.name, value);
                }

                error(equals, "Invaild assignment target.");
            }

            return expr;
        }

        private Expr or()
        {
            Expr expr = and();

            while (match(TokenType.OR))
            {
                Token lox_operator = previous();
                Expr right = and();
                expr = new Expr.Logical(expr, lox_operator, right);
            }

            return expr;
        }

        private Expr and()
        {
            Expr expr = equality();

            while (match(TokenType.AND))
            {
                Token lox_operator = previous();
                Expr right = equality();
                expr = new Expr.Logical(expr, lox_operator, right);
            }

            return expr;
        }


        //等価
        private Expr equality()
        {
            Expr expr = comparison();
            while (match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                var lox_operator = previous();
                var right = comparison();
                expr = new Expr.Binary(expr, lox_operator, right);
            }

            return expr;
        }

        //コンパリソン 比較
        private Expr comparison()
        {
            Expr expr = term();
            while (match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                var lox_operator = previous();
                var right = term();
                expr = new Expr.Binary(expr, lox_operator, right);
            }

            return expr;
        }

        //ターム 項
        private Expr term()
        {
            Expr expr = factor();
            while (match(TokenType.MINUS, TokenType.PLUS))
            {
                var lox_operator = previous();
                var right = factor();
                expr = new Expr.Binary(expr, lox_operator, right);
            }

            return expr;
        }

        private Expr factor()
        {
            Expr expr = unary();
            while (match(TokenType.SLASH, TokenType.STAR))
            {
                var lox_operator = previous();
                var right = unary();
                expr = new Expr.Binary(expr, lox_operator, right);
            }

            return expr;
        }

        //単項式
        private Expr unary()
        {
            if (match(TokenType.BANG, TokenType.MINUS))
            {
                var lox_operator = previous();
                var right = unary();
                return new Expr.Unary(lox_operator, right);
            }

            return call();
        }

        //まずprimaryでコール演算の左オペランドに相当する式EXPRを解析する
        //ループで（　に遭遇するたびにfinishCallを呼び出す
        //ドットがあればP246のような連鎖を構築する
        private Expr call()
        {
            Expr expr = primary();

            while (true)
            {
                if (match(TokenType.LEFT_PAREN))
                {
                    expr = finishCall(expr);
                }
                else if (match(TokenType.DOT))
                {
                    Token name = consume(TokenType.IDENTIFIER, "Expect property name after '.' .");
                    expr = new Expr.Get(expr,name);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        //最初に次のトークンが）かどうか調べ、もしそうなら引数の解析は試みない
        //そうでなければexpression()で式の解析をおこなって、まだ引数があるかをカンマの有無で調べる。これを繰り返す
        //引数リストの処理が終わったら）を消費して、コーリーと引数を関数コールのＡＳＴノードにまとめてラップする。
        private Expr finishCall(Expr callee)
        {
            List<Expr> arguments = new List<Expr>();

            if (!check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (arguments.Count() >= 255)
                    {
                        error(peek(), "Can't have more then 255 arguments.");
                    }

                    arguments.Add(expression());
                } while (match(TokenType.COMMA));
            }

            Token paren = consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
        }

        //一次式
        private Expr primary()
        {
            if (match(TokenType.FALSE))
            {
                return new Expr.Literal(false);
            }

            if (match(TokenType.TRUE))
            {
                return new Expr.Literal(true);
            }

            if (match(TokenType.NIL))
            {
                return new Expr.Literal(null);
            }

            if (match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expr.Literal(previous().literal);
            }

            if (match(TokenType.THIS))
            {
                return new Expr.This(previous());
            }

            if (match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(previous());
            }

            if (match(TokenType.LEFT_PAREN))
            {
                Expr expr = expression();
                consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw error(peek(), "Expect expression.");
        }

        //現在のトークンが期待した型かチェックして期待した型ならトークンを消費してtrueを返す
        //そうでなければトークンを消費せずにfalseを返す
        private bool match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (check(type))
                {
                    advance();
                    return true;
                }
            }

            return false;
        }

        //次のトークンが期待した型かチェックして期待した型ならトークンを消費して返す
        private Token consume(TokenType type, string message)
        {
            if (check(type))
            {
                return advance();
            }

            throw error(peek(), message);
        }

        //現在のトークンが与えられたtypeと一致するかどうか
        private bool check(TokenType type)
        {
            if (isAtEnd())
            {
                return false;
            }
            return peek().type == type;
        }

        //現在のトークンを消費してそれを返す
        private Token advance()
        {
            if (!isAtEnd())
            {
                current++;
            }
            return previous();
        }

        //トークンリストを最後まで解析したか返す
        private bool isAtEnd()
        {
            return peek().type == TokenType.EOF;
        }

        //まだ消費していない現在のトークンを返す
        private Token peek()
        {
            return tokens[current];
        }

        //最後に消費したトークンを返す
        private Token previous()
        {
            return tokens[current - 1];
        }

        private ParseError error(Token token, string message)
        {
            Lox.Program.error(token, message);
            return new ParseError();
        }


        //　構文解析中にエラーが発生した場合、次の文に進む
        // 文の境界に達したと思うまでトークンを棄てていく
        private void synchronize()
        {
            advance();

            while (!isAtEnd())
            {
                if (previous().type == TokenType.SEMICOLON)
                {
                    return;
                }

                switch (peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }

                advance();
            }
        }
    }
}
