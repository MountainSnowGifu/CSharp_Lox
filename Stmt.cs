using System.Collections.Generic;

namespace Lox
{
    internal abstract class Stmt
    {
        internal interface IVisitor<T>{
            T VisitBlockStmt(Block stmt);
            T VisitClassStmt(Class stmt);
            T VisitExpressionStmt(Expression stmt);
            T VisitFunctionStmt(Function stmt);
            T VisitIfStmt(If stmt);
            T VisitPrintStmt(Print stmt);
            T VisitReturnStmt(Return stmt);
            T VisitWhileStmt(While stmt);
            T VisitVarStmt(Var stmt);
   }
        internal class Block : Stmt
        {
            internal List<Stmt> statements { get; }

            internal Block(List<Stmt> statements)
            {
                this.statements = statements;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitBlockStmt(this);
            }
}
        internal class Class : Stmt
        {
            internal Token name { get; }
            internal List<Stmt.Function> methods { get; }

            internal Class(Token name, List<Stmt.Function> methods)
            {
                this.name = name;
                this.methods = methods;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitClassStmt(this);
            }
}
        internal class Expression : Stmt
        {
            internal Expr expression { get; }

            internal Expression(Expr expression)
            {
                this.expression = expression;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitExpressionStmt(this);
            }
}
        internal class Function : Stmt
        {
            internal Token name { get; }
            internal List<Token> fun_params { get; }
            internal List<Stmt> body { get; }

            internal Function(Token name, List<Token> fun_params, List<Stmt> body)
            {
                this.name = name;
                this.fun_params = fun_params;
                this.body = body;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitFunctionStmt(this);
            }
}
        internal class If : Stmt
        {
            internal Expr condition { get; }
            internal Stmt thenBranch { get; }
            internal Stmt elseBranch { get; }

            internal If(Expr condition, Stmt thenBranch, Stmt elseBranch)
            {
                this.condition = condition;
                this.thenBranch = thenBranch;
                this.elseBranch = elseBranch;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitIfStmt(this);
            }
}
        internal class Print : Stmt
        {
            internal Expr expression { get; }

            internal Print(Expr expression)
            {
                this.expression = expression;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitPrintStmt(this);
            }
}
        internal class Return : Stmt
        {
            internal Token keyword { get; }
            internal Expr value { get; }

            internal Return(Token keyword, Expr value)
            {
                this.keyword = keyword;
                this.value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitReturnStmt(this);
            }
}
        internal class While : Stmt
        {
            internal Expr condition { get; }
            internal Stmt body { get; }

            internal While(Expr condition, Stmt body)
            {
                this.condition = condition;
                this.body = body;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitWhileStmt(this);
            }
}
        internal class Var : Stmt
        {
            internal Token name { get; }
            internal Expr initializer { get; }

            internal Var(Token name, Expr initializer)
            {
                this.name = name;
                this.initializer = initializer;
            }

            internal override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitVarStmt(this);
            }
}

        internal abstract T Accept<T>(IVisitor<T> visitor);
    }
}
