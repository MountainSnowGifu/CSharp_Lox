using System.Collections.Generic;

namespace Lox
{
    internal abstract class Expr
    {
        internal interface IVisitor<T>{
            T VisitAssignExpr(Assign expr);
            T VisitBinaryExpr(Binary expr);
            T VisitCallExpr(Call expr);
            T VisitGetExpr(Get expr);
            T VisitGroupingExpr(Grouping expr);
            T VisitLiteralExpr(Literal expr);
            T VisitLogicalExpr(Logical expr);
            T VisitSetExpr(Set expr);
            T VisitThisExpr(This expr);
            T VisitUnaryExpr(Unary expr);
            T VisitVariableExpr(Variable expr);
   }
        internal class Assign : Expr
        {
            internal Token name { get; }
            internal Expr value { get; }

            internal Assign(Token name, Expr value)
            {
                this.name = name;
                this.value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitAssignExpr(this);
            }
}
        internal class Binary : Expr
        {
            internal Expr left { get; }
            internal Token lox_operator { get; }
            internal Expr right { get; }

            internal Binary(Expr left, Token lox_operator, Expr right)
            {
                this.left = left;
                this.lox_operator = lox_operator;
                this.right = right;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitBinaryExpr(this);
            }
}
        internal class Call : Expr
        {
            internal Expr callee { get; }
            internal Token paren { get; }
            internal List<Expr> arguments { get; }

            internal Call(Expr callee, Token paren, List<Expr> arguments)
            {
                this.callee = callee;
                this.paren = paren;
                this.arguments = arguments;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitCallExpr(this);
            }
}
        internal class Get : Expr
        {
            internal Expr _object { get; }
            internal Token name { get; }

            internal Get(Expr _object, Token name)
            {
                this._object = _object;
                this.name = name;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitGetExpr(this);
            }
}
        internal class Grouping : Expr
        {
            internal Expr expression { get; }

            internal Grouping(Expr expression)
            {
                this.expression = expression;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitGroupingExpr(this);
            }
}
        internal class Literal : Expr
        {
            internal object value { get; }

            internal Literal(object value)
            {
                this.value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitLiteralExpr(this);
            }
}
        internal class Logical : Expr
        {
            internal Expr left { get; }
            internal Token lox_operator { get; }
            internal Expr right { get; }

            internal Logical(Expr left, Token lox_operator, Expr right)
            {
                this.left = left;
                this.lox_operator = lox_operator;
                this.right = right;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitLogicalExpr(this);
            }
}
        internal class Set : Expr
        {
            internal Expr _object { get; }
            internal Token name { get; }
            internal Expr value { get; }

            internal Set(Expr _object, Token name, Expr value)
            {
                this._object = _object;
                this.name = name;
                this.value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitSetExpr(this);
            }
}
        internal class This : Expr
        {
            internal Token keyword { get; }

            internal This(Token keyword)
            {
                this.keyword = keyword;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitThisExpr(this);
            }
}
        internal class Unary : Expr
        {
            internal Token lox_operator { get; }
            internal Expr right { get; }

            internal Unary(Token lox_operator, Expr right)
            {
                this.lox_operator = lox_operator;
                this.right = right;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitUnaryExpr(this);
            }
}
        internal class Variable : Expr
        {
            internal Token name { get; }

            internal Variable(Token name)
            {
                this.name = name;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitVariableExpr(this);
            }
}

        internal abstract T Accept<T>(IVisitor<T> visitor);
    }
}
