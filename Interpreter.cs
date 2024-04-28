using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lox
{
    internal class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        private readonly Dictionary<Expr, int> _locals = new Dictionary<Expr, int>();
        internal readonly LoxEnvironment _globals = new LoxEnvironment();
        private LoxEnvironment _loxEnvironment;

        internal Interpreter()
        {
            _loxEnvironment = _globals;
            var clock = new LoxClockCallable();
            var readLine = new LoxReadLineCallable();
            _globals.define("clock", clock);
            _globals.define("readLine", readLine);
        }
        internal void interpret(List<Stmt> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    execute(statement);
                }
            }
            catch (RuntimeError error)
            {
                Lox.Program.runtimeError(error);
            }
        }

        //二項演算子を評価する
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            object left = evaluate(expr.left);
            object right = evaluate(expr.right);

            switch (expr.lox_operator.type)
            {
                case TokenType.BANG_EQUAL:
                    return !isEqual(left, right);

                case TokenType.EQUAL_EQUAL:
                    return isEqual(left, right);

                case TokenType.GREATER:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left > (double)right;

                case TokenType.GREATER_EQUAL:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left >= (double)right;

                case TokenType.LESS:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left < (double)right;

                case TokenType.LESS_EQUAL:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left <= (double)right;

                case TokenType.MINUS:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left - (double)right;

                case TokenType.PLUS:
                    if (left is double && right is double)
                    {
                        return (double)left + (double)right;
                    }

                    if (left is string && right is string)
                    {
                        return (string)left + (string)right;
                    }

                    throw new RuntimeError(expr.lox_operator, "Operands must be two number or two strings.");

                case TokenType.SLASH:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left / (double)right;

                case TokenType.STAR:
                    checkNumberOperands(expr.lox_operator, left, right);
                    return (double)left * (double)right;


            }

            return null;
        }

        //グルーピングのノードは、カッコ内におかれた式を表す内部ノードへの参照を持ちます。
        //グルーピング式そのものを評価するには、その部分式を再帰的に評価します
        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return evaluate(expr.expression);
        }

        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }

        //単項式を評価する
        public object VisitUnaryExpr(Expr.Unary expr)
        {
            object right = evaluate(expr.right);

            switch (expr.lox_operator.type)
            {
                case TokenType.BANG:
                    return !isTruthy(right);
                case TokenType.MINUS:
                    checkNumberOperand(expr.lox_operator, right);
                    return -(double)right;
            }

            return null;
        }

        private void checkNumberOperand(Token lox_operator, object operand)
        {
            if (operand is double)
            {
                return;
            }

            throw new RuntimeError(lox_operator, "Operand must be a number.");
        }

        private void checkNumberOperands(Token lox_operator, object left, object right)
        {
            if (left is double && right is double)
            {
                return;
            }

            throw new RuntimeError(lox_operator, "Operands must be a number.");
        }

        private object evaluate(Expr expr)
        {
            return expr.Accept(this);
        }

        private void execute(Stmt stmt)
        {
            stmt.Accept(this);
        }

        public void resolve(Expr expr, int depth)
        {
            _locals.Add(expr, depth);
        }

        public void executeBlock(List<Stmt> statements, LoxEnvironment environment)
        {
            LoxEnvironment previous = this._loxEnvironment;

            try
            {
                this._loxEnvironment = environment;
                foreach (var statement in statements)
                {
                    execute(statement);
                }
            }
            finally
            {
                this._loxEnvironment = previous;
            }
        }


        //真偽値を判定するためのヘルパー関数
        //nilとfalse以外の値は真として扱う
        private bool isTruthy(object lox_object)
        {
            if (lox_object == null)
            {
                return false;
            }

            if (lox_object is bool)
            {
                return (bool)lox_object;
            }

            return true;
        }

        private bool isEqual(object a, object b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        private string stringify(object lox_object)
        {
            if (lox_object == null)
            {
                return "nil";
            }

            if (lox_object is double)
            {
                string text = lox_object.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(0, text.Length - 2);
                }
                return text;
            }

            return lox_object.ToString();
        }

        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            evaluate(stmt.expression);
            return null;
        }

        public object VisitPrintStmt(Stmt.Print stmt)
        {
            object value = evaluate(stmt.expression);
            Console.WriteLine(stringify(value));
            return null;
        }

        public object VisitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            _loxEnvironment.define(stmt.name.lexeme, value);
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr)
        {
            return lookUpVariable(expr.name, expr);
        }

        //まず解決への距離をマップで探索します。ローカル変数だけを解決したことを思い出します。
        //グローバルは特別あつかいでこのマップには入りません。もし距離がマップになければグローバルに違いないのです。
        //その場合は動的な参照をグローバル環境で直接行い、もし未定義の変数ならランタイムエラーを送出します。
        private object lookUpVariable(Token name, Expr expr)
        {
            if (_locals.ContainsKey(expr))
            {
                int distance = _locals.GetValueOrDefault(expr);
                return _loxEnvironment.getAt(distance, name.lexeme);
            }
            else
            {
                return _globals.get(name);
            }
        }

        public object VisitAssignExpr(Expr.Assign expr)
        {
            object value = evaluate(expr.value);

            if (_locals.ContainsKey(expr))
            {
                int distance = _locals.GetValueOrDefault(expr);
                _loxEnvironment.AssignAt(distance, expr.name, value);
            }
            else
            {
                _globals.assign(expr.name, value);
            }

            return value;
        }
        public object VisitBlockStmt(Stmt.Block stmt)
        {
            executeBlock(stmt.statements, new LoxEnvironment(_loxEnvironment));
            return null;
        }

        //現在の環境でこのクラス名を宣言してから、クラスの構文ノードをクラスのランタイム表現であるLoxClassに変換する
        //そしてそのオブジェクトを、先に宣言した変数に保存します。
        //このように変数束縛のプロセスを二段階で行うことによって、クラスメソッドの内部からクラス自身を参照できるようにしています。
        public object VisitClassStmt(Stmt.Class stmt)
        {

            //このクラスにスーパークラスの式があれば、それを評価して、それがクラスであることを確認します。
            object superclass = null;
            if (stmt.superclass != null)
            {
                superclass = evaluate(stmt.superclass);
                if (!(superclass is LoxClass))
                {
                    throw new RuntimeError(stmt.superclass.name, "Superclass must be a class.");
                }
            }


            _loxEnvironment.define(stmt.name.lexeme, null);

            //サブクラスび定義を評価するときに新しい環境を作ります。
            if (stmt.superclass != null)
            {
                _loxEnvironment = new LoxEnvironment(_loxEnvironment);
                _loxEnvironment.define("super", superclass);
            }

            Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();

            //個々のmethod宣言をLoxFunctuionオブジェクトに展開する
            foreach (var method in stmt.methods)
            {
                LoxFunction function = new LoxFunction(method, _loxEnvironment, method.name.lexeme.Equals("init"));
                methods.Add(method.name.lexeme, function);
            }

            //これらすべてをメソッド名をキーとするMAPでつつみLOXCLASSに格納する
            LoxClass klass = new LoxClass(stmt.name.lexeme, (LoxClass)superclass, methods);


            //スーパークラスがあれば、それを環境から取り除きます。環境をホップする
            if (superclass != null)
            {
                _loxEnvironment = _loxEnvironment._enclosing;
            }

            _loxEnvironment.assign(stmt.name, klass);
            return null;
        }

        public object VisitIfStmt(Stmt.If stmt)
        {
            if (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.thenBranch);
            }
            else if (stmt.elseBranch != null)
            {
                execute(stmt.elseBranch);
            }

            return null;
        }

        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = evaluate(expr.left);

            if (expr.lox_operator.type == TokenType.OR)
            {
                if (isTruthy(left))
                {
                    return left;
                }
            }
            else
            {
                if (!isTruthy(left))
                {
                    return left;
                }
            }

            return evaluate(expr.right);
        }


        //プロパティをセットしようとしているオブジェクトを評価して、LOXINSTANCEかどうかチェックします。
        //そうでなければランタイムエラー。インスタンスならセットする値を評価して、そこに保存します。
        public object VisitSetExpr(Expr.Set expr)
        {
            object _object = evaluate(expr._object);

            if (!(_object is LoxInstance))
            {
                throw new RuntimeError(expr.name, "Only instance have fields.");
            }

            object value = evaluate(expr.value);
            ((LoxInstance)_object).set(expr.name, value);
            return value;
        }

        public object VisitSuperExpr(Expr.Super expr)
        {
            //super式を囲むクラスのスーパークラスを適切な距離にある環境でsuperを探索することによって探し出します。
            int distance = _locals.GetValueOrDefault(expr);
            LoxClass superclass = (LoxClass)_loxEnvironment.getAt(distance, "super");

            //super式における現在のオブジェクトは暗黙のうちに、いま実際使っている現在のオブジェクト　すなわちTHISと同じものになります。
            //例えばスーパークラスのメソッドを探索していても、そのインスタンスは歴然としてTHISなのです。
            //this が束縛される環境は常に我々がSUPERを格納する環境のすぐ内側にあります。
            //距離から１を引けば、その内側の環境でTHISを探索できます。
            LoxInstance loxInstance_object = (LoxInstance)_loxEnvironment.getAt(distance - 1, "this");

            //これでもうスーパークラスを始点としてメソッドを探索し、束縛できるようになります。
            //これはGET式のメソッドを探索するコードとほとんど同じです。ただし現在のオブジェクトのクラスではなくスーパークラスのFINDMETHODを呼び出すことだけが違います。
            LoxFunction method = superclass.findMethod(expr.method.lexeme);

            if (method == null)
            {
                throw new RuntimeError(expr.method, "Undefined proterty '" + expr.method.lexeme + "'.");
            }

            return method.bind(loxInstance_object);
        }
        
        public object VisitThisExpr(Expr.This expr)
        {
            return lookUpVariable(expr.keyword, expr);
        }

        public object VisitWhileStmt(Stmt.While stmt)
        {
            while (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.body);
            }

            return null;
        }

        //最初にコーリーの式を評価します、この式は典型的には呼び出したい関数を名前で探索するための識別子。
        //次に引数式を並び順に評価して結果をリストに格納する
        //コーリーをILoxCallableにキャストしてからコールメソッドを呼び出す
        //関数のように呼び出すLOXオブジェクトはどれもこのインターフェースを実装することになる
        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = evaluate(expr.callee);

            List<object> arguments = new List<object>();

            foreach (Expr argument in expr.arguments)
            {
                arguments.Add(evaluate(argument));
            }

            if (!(callee is ILoxCallable))
            {
                throw new RuntimeError(expr.paren, "Can only call functions and classes");
            }

            ILoxCallable function = (ILoxCallable)callee;

            if (arguments.Count() != function.arity())
            {
                throw new RuntimeError(expr.paren, "Expected " + function.arity() + " arguments but got " + arguments.Count() + ".");
            }

            return function.call(this, arguments);
        }

        //まずプロパティアクセスの対象となる式を評価する。LOXでプロパティを持つのは、クラスのインスタンスだけ。
        //数値など、その他の型のオブジェクトに対するゲッター呼び出しならばランタイムエラー
        //オブジェクトがLoxInstanceならば、プロパティの参照が必要
        public object VisitGetExpr(Expr.Get expr)
        {
            object _object = evaluate(expr._object);

            if (_object is LoxInstance)
            {
                return ((LoxInstance)_object).get(expr.name);
            }

            throw new RuntimeError(expr.name, "Only instance have properties.");
        }

        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            LoxFunction function = new LoxFunction(stmt, _loxEnvironment, false);//普通の関数宣言ならinitialierは常にfalse
            _loxEnvironment.define(stmt.name.lexeme, function);
            return null;
        }


        //まず戻り値があれば評価します。なければNILを使います
        //次にカスタム例外クラスでラップして送出します。
        //関数コールが開始された場所LoxFunction の　call()にいたる巻き戻しをおこなう
        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null)
            {
                value = evaluate(stmt.value);
            }

            throw new Return(value);
        }

    }
}
