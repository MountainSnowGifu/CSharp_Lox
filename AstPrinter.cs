using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class AstPrinter : Expr.IVisitor<string>
    {
        internal string print(Expr expr)
        {
            return expr.Accept(this);
        }
        string Expr.IVisitor<string>.VisitBinaryExpr(Expr.Binary expr)
        {
            return parenthesize(expr.lox_operator.lexeme, expr.left, expr.right);
        }

        string Expr.IVisitor<string>.VisitGroupingExpr(Expr.Grouping expr)
        {
            return parenthesize("group", expr.expression);
        }

        string Expr.IVisitor<string>.VisitLiteralExpr(Expr.Literal expr)
        {
            if (expr.value == null) return "nil";
            return expr.value.ToString();
        }

        string Expr.IVisitor<string>.VisitUnaryExpr(Expr.Unary expr)
        {
            return parenthesize(expr.lox_operator.lexeme, expr.right);
        }

        private string parenthesize(string name, params Expr[] exprs)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("(").Append(name);
            foreach (var expression in exprs)
            {
                builder.Append(" ");
                builder.Append(expression.Accept(this));
            }
            builder.Append(")");
            return builder.ToString();
        }

        internal static void AstTest()
        {
            var unary = new Expr.Unary(new Token(TokenType.MINUS, "-", null, 1), new Expr.Literal(123));
            var token = new Token(TokenType.STAR, "*", null, 1);
            var expression = new Expr.Grouping(new Expr.Literal(45.67));

            var binary = new Expr.Binary(unary, token, expression);

            Console.WriteLine(new AstPrinter().print(binary));
        }

        public string VisitVariableExpr(Expr.Variable expr)
        {
            throw new NotImplementedException();
        }

        public string VisitAssignExpr(Expr.Assign expr)
        {
            throw new NotImplementedException();
        }

        public string VisitLogicalExpr(Expr.Logical expr)
        {
            throw new NotImplementedException();
        }

        public string VisitCallExpr(Expr.Call expr)
        {
            throw new NotImplementedException();
        }

        public string VisitGetExpr(Expr.Get expr)
        {
            throw new NotImplementedException();
        }

        public string VisitSetExpr(Expr.Set expr)
        {
            throw new NotImplementedException();
        }

        public string VisitThisExpr(Expr.This expr)
        {
            throw new NotImplementedException();
        }
    }
}
