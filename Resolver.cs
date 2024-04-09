using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lox
{
    //解決
    //リゾルバは、構文木のすべてのノードを訪問する必要があるので、既存のIVisitor抽象を実装する
    //ブロック文では、それに含まれる文のために、新しいスコープを導入する
    //関数宣言では、本文のために新しいスコープを導入して、パラメータをそのスコープで束縛する
    //変数宣言では、現在のスコープに新しい変数を加える
    //変数式と代入式では、変数の解決が必要
    internal class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        private readonly Interpreter _interpreter;

        //このスコープフィールドで現在のスコープに対するスタックを追跡します。
        //このスコープスタックは、ローカルなブロックスコープだけを対象とします。
        //グローバルスコープのトップレベルで宣言された変数は、リゾルバで追跡しません。
        //変数を解決しようとして、ローカルスコープスタックで見つからなければ、グローバルに違いないと考えます。
        private readonly Stack<Dictionary<string, bool>> _scopes = new Stack<Dictionary<string, bool>>();

        private FunctonType _currentFunction = FunctonType.NONE;

        public Resolver(Interpreter interpreter)
        {
            this._interpreter = interpreter;
        }

        private enum FunctonType
        {
            NONE,
            FUNCTION,
            INITIALIZER,
            METHOD,
        }

        private enum ClassType
        {
            NONE,
            CLASS,
        }

        //構文木を辿っているときにいまクラス宣言の中なのかを教えてくれる
        private ClassType _currentClass = ClassType.NONE;

        //文のリストを辿って、それぞれの文を解決する
        public void resolve(List<Stmt> statements)
        {
            foreach (var statement in statements)
            {
                resolve(statement);
            }
        }


        //新しいスコープを開始して、そのブロック内の文STMTを辿って（たどって）解決したあと、そのスコープを棄てる。
        public object VisitBlockStmt(Stmt.Block stmt)
        {
            beginScope();
            resolve(stmt.statements);
            endScope();
            return null;
        }


        public object VisitClassStmt(Stmt.Class stmt)
        {
            //構文木を辿っているときにいまクラス宣言の中なのかを教えてくれる
            //クラス宣言の解決を始めるときに、その値を変更する
            //それまでフィールドにあった値をローカル変数に保存　JVMへのピギーバック管理
            //クラスがほかのクラスの内側にネストしても、まえの値を失わないようになる。
            ClassType enclosingClass = _currentClass;
            _currentClass = ClassType.CLASS;

            declare(stmt.name);
            define(stmt.name);

            //thisという名前を使うローカル変数とまったく同じように解決される。
            //メソッド本文に踏み込んで解決するまえに、それを囲むスコープを設定し、その中にthisを変数のように定義しておく
            //そして解決を終えたらメソッド本文を囲んでいたスコープを破棄する。
            //こうすれば少なくともメソッドないで遭遇するthis式は常に、そのメソッド本文のブロックを囲む暗黙のスコープで定義されたローカル変数をして解決される
            beginScope();
            _scopes.Peek().Add("this", true);

            //クラス本文にあるメソッドを巡回して、それぞれをresolveFunctionへ渡します。
            foreach (var method in stmt.methods)
            {
                FunctonType declaration = FunctonType.METHOD;

                //いま解決いているメソッドが初期化子かどうかは訪問しているメソッド名を使って判定する
                if (method.name.lexeme.Equals("init"))
                {
                    declaration = FunctonType.INITIALIZER;
                }

                resolveFunction(method, declaration);
            }

            endScope();

            _currentClass = enclosingClass;

            return null;
        }

        //変数宣言の解決
        //現在最も内側にあるスコープのマップに新しいエントリを加えます。
        //式を訪問しているとき、いま何らかの変数を初期化中なのか、そうでないかを把握するために、束縛を二段階にわけた
        public object VisitVarStmt(Stmt.Var stmt)
        {
            //宣言
            declare(stmt.name);


            if (stmt.initializer != null)
            {
                //初期化式の解決
                resolve(stmt.initializer);
            }

            //定義
            define(stmt.name);
            return null;
        }

        //変数式（関数宣言も）スコープマップに書き込みます。変数式を解決するときは、それらのマップを読みだします。
        //まずその変数が自身の初期化子のなかでアクセスされるかチェックします。もし変数が現在のスコープに存在するのに、マップの値がFALSEならば
        //宣言したけどまだ定義されていないという意味なのでエラーとして報告します。
        //チェックを終えたら、resolveLocalで解決します。
        public object VisitVariableExpr(Expr.Variable expr)
        {
            if (_scopes.Count != 0)
            {
                if (_scopes.Peek().ContainsKey(expr.name.lexeme))
                {
                    if (_scopes.Peek().GetValueOrDefault(expr.name.lexeme) == false)
                    {
                        Lox.Program.error(expr.name, "Can't read local variable in its own initializer.");
                    }
                }
            }

            resolveLocal(expr, expr.name);
            return null;
        }

        //代入式
        //代入値にほかの変数への参照が含まれる場合に備えて、その値の式を解決します。
        //それからresolveLocalで代入を受ける側の変数を解決します。
        public object VisitAssignExpr(Expr.Assign expr)
        {
            resolve(expr.value);
            resolveLocal(expr, expr.name);
            return null;
        }

        //関数宣言の解決
        //関数は名前を束縛するだけでなく、スコープの導入も行います。関数そのものの名前は、その関数宣言を囲むスコープに束縛されます。
        //そして関数本文に踏み込んだら、その内側の関数スコープにパラメーター群が束縛されます。
        //関数名を現在のスコープで宣言して定義します。しかし変数と違って、関数名の定義はその関数本文の解決前に行います。こうすれば
        //関数は、本文から自分自身を再帰的に参照できるのです
        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            declare(stmt.name);
            define(stmt.name);
            resolveFunction(stmt, FunctonType.FUNCTION);
            return null;
        }

        //式文には辿るべき式が１つしか含まれない
        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            resolve(stmt.expression);
            return null;
        }

        //IF文の解決に制御フローはありません、条件と両方の分岐とをひたすら解決します。
        public object VisitIfStmt(Stmt.If stmt)
        {
            resolve(stmt.condition);
            resolve(stmt.thenBranch);
            if (stmt.elseBranch != null)
            {
                resolve(stmt.elseBranch);
            }
            return null;
        }

        //式文と同じく、PRINT文も一個の部分式を含みます
        public object VisitPrintStmt(Stmt.Print stmt)
        {
            resolve(stmt.expression);
            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt)
        {
            if (_currentFunction == FunctonType.NONE)
            {
                Lox.Program.error(stmt.keyword, "Can't return from top-leevel code.");
            }


            if (stmt.value != null)
            {
                //リターン文でinitメソッドから値を返すのをエラーとする
                if (_currentFunction == FunctonType.INITIALIZER)
                {
                    Lox.Program.error(stmt.keyword, "Can't return a value from an initializer.");
                }

                resolve(stmt.value);
            }
            return null;
        }

        //IF文同様にWHILE文も条件を解決しますが、ループ本文の解決は一回きりです。
        public object VisitWhileStmt(Stmt.While stmt)
        {
            resolve(stmt.condition);
            resolve(stmt.body);
            return null;
        }

        //二項式では、両方のオペランドを辿って解決します
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            resolve(expr.left);
            resolve(expr.right);
            return null;
        }

        //コール式も同様　引数リストを辿って、それらをすべて解決します。コーリーも式なので、（通常は変数式）それも解決します。
        public object VisitCallExpr(Expr.Call expr)
        {
            resolve(expr.callee);

            foreach (var arg in expr.arguments)
            {
                resolve(arg);
            }

            return null;
        }

        //Loxのプロパティが動的にディスパッチされることは、静的解決のパスでプロパティ名を処理しないという事実から見て取れる
        public object VisitGetExpr(Expr.Get expr)
        {
            resolve(expr._object);
            return null;
        }

        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            resolve(expr.expression);
            return null;
        }

        //リテラル式はどんな変数も言及せず、どんな部分式も含まないので、解決すべきものがない
        //
        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return null;
        }

        //
        //論理式は普通の二項式と同じ
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            resolve(expr.left);
            resolve(expr.right);
            return null;
        }

        //Expr.Getと同じく、プロパティそのものは動的に評価されるので、解決すべきものはありません。
        //必要なのはExpr.Setの二つの部分式（プロパティをセットしたいオブジェクトと、セットしたい値）の再帰的な解決だけです。
        public object VisitSetExpr(Expr.Set expr)
        {
            resolve(expr.value);
            resolve(expr._object);
            return null;
        }

        public object VisitThisExpr(Expr.This expr)
        {
            if (_currentClass == ClassType.NONE)
            {
                Lox.Program.error(expr.keyword, "Can't use 'this' outside of a class.");
                return null;
            }

            resolveLocal(expr, expr.keyword);
            return null;
        }

        //単項式ではその唯一のオペランドを解決する
        public object VisitUnaryExpr(Expr.Unary expr)
        {
            resolve(expr.right);
            return null;
        }

        private void resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        private void resolve(Expr expr)
        {
            expr.Accept(this);
        }

        private void beginScope()
        {
            _scopes.Push(new Dictionary<string, bool>());
        }

        //スコープをおわらせる
        private void endScope()
        {
            _scopes.Pop();
        }


        //宣言
        //変数を最も内側のスコープに追加する。これによってその変数の存在をしり
        //もし外側に同じ変数があれば、それをシャドーイングすることになる。
        //その名前をスコープマップにFALSEの値で束縛すれば、その変数に準備中のマークがつきます。
        //キーに割り当てられる値はその変数を初期化しおえたかどうかを表す。
        private void declare(Token name)
        {
            if (_scopes.Count == 0)
            {
                return;
            }

            var scope = _scopes.Peek();

            if (scope.ContainsKey(name.lexeme))
            {
                Lox.Program.error(name, "Alredy a variable with this name in this scope.");
            }

            //scope.Add(name.lexeme, false);//初期化未：準備中
            scope[name.lexeme] = false;
        }

        //その変数のスコープマップにおける値をTRUEにセットすると完全に初期化され、利用できるようになった。というマークがつく。変数が誕生する
        private void define(Token name)
        {
            if (_scopes.Count == 0)
            {
                return;
            }

            //scopes.Peek().Add(name.lexeme, true);
            _scopes.Peek()[name.lexeme] = true;
        }

        //もっとも内側のスコープから、外に向かいながら、マッチする名前を、それぞれのマップでさがします。
        //もし変数を見つけたら、解決resolveを呼び出して、最も内側のスコープから、その変数が見つかったスコープまでの距離を表す、スコープ数を渡します。
        //もし現在のスコープで見つかったなら渡す値は０です。すぐ外側を囲むスコープにあった場合は１です。
        //private void resolveLocal(Expr expr, Token name)
        //{
        //    for (int i = _scopes.Count - 1; i >= 0; i--)
        //    {
        //        //JAVAのstackの添え字アクセスとC#のstackの添え字アクセスは逆
        //        var array = _scopes.Reverse().ToArray();
        //        if (array.ElementAt(i).ContainsKey(name.lexeme))
        //        {
        //            //リゾルバの仕事は、変数を訪問するたびに、現在のスコープと変数が定義されたスコープとの間にあるスコープ数をインタプリタに知らせること。
        //            //そのスコープ数はインタプリタ実行時の現在の環境と、変数の値を参照できる環境との間にある環境数に、正確に対応します。
        //            _interpreter.resolve(expr, _scopes.Count - 1 - i);
        //            return;
        //        }
        //    }
        //}

        //上のを改良
        private void resolveLocal(Expr expr, Token name)
        {
            for (int i = 0; i < _scopes.Count; i++)
            {
                if (_scopes.ElementAt(i).ContainsKey(name.lexeme))
                {
                    //リゾルバの仕事は、変数を訪問するたびに、現在のスコープと変数が定義されたスコープとの間にあるスコープ数をインタプリタに知らせること。
                    //そのスコープ数はインタプリタ実行時の現在の環境と、変数の値を参照できる環境との間にある環境数に、正確に対応します。
                    _interpreter.resolve(expr, i);
                    return;
                }
            }
        }

        //新しいスコープを関数本文ように作ってから、関数パラメーターそれぞれに、変数の束縛を行います。
        //こうして準備がととのったらそのスコープの中で関数本文を解決します。
        //静的解析においてはそのときそこに存在する本文を即座に辿るのです。
        private void resolveFunction(Stmt.Function function, FunctonType type)
        {
            FunctonType enclosingFunction = _currentFunction;
            _currentFunction = type;

            beginScope();
            foreach (var param in function.fun_params)
            {
                declare(param);
                define(param);
            }

            resolve(function.body);
            endScope();

            _currentFunction = enclosingFunction;
        }

    }
}
