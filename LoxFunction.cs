using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class LoxFunction : ILoxCallable
    {
        private readonly Stmt.Function _declaration;
        private readonly LoxEnvironment _closure;
        private readonly bool _isInitializer;
        internal LoxFunction(Stmt.Function declaration, LoxEnvironment closure,bool isInitializer)
        {
            this._declaration = declaration;
            this._closure = closure;
            this._isInitializer = isInitializer;
        }


        //これはメソッドに元からあったクロージャーの内側にネストした新しい環境をつくるので、いわばクロージャの中のクロージャ
        //を作るようなものです。メソッドが呼び出されるとき、それが本文の環境の親になります。
        //thisをその環境ないの変数として宣言して、それを所与のインスタンスに束縛します。それはいまメソッドがアクセスされているインスタンスです。
        //するとこれによって返されるLoxFunctionはthisがOBJECTに束縛された状態をいわば　小さな永続する世界　として維持することになるのです
        public LoxFunction bind(LoxInstance instance) { 
            LoxEnvironment environment = new LoxEnvironment(_closure);
            environment.define("this", instance);
            return new LoxFunction(_declaration, environment, _isInitializer);
        }

        public int arity()
        {
            return _declaration.fun_params.Count;
        }

        //名前環境の管理は、言語実装のコアにあたる
        //それと密接に結びついているものが関数
        //パラメータが関数によってカプセル化されることが重要、関数の外にあるコードからパラメータを見ることはできない。
        //どの関数も独自の環境をもちそこにパラメータの変数を保存する
        //P198
        public object call(Interpreter interpreter, List<object> arguments)
        {
            LoxEnvironment environment = new LoxEnvironment(_closure);

            for (int i = 0; i < arguments.Count; i++)
            {
                environment.define(_declaration.fun_params[i].lexeme, arguments[i]);
            }

            //RETURN例外をキャッチしたら、その値をコールからの戻り値とする
            //例外をキャッチしなかったら関数を最後まで実行したが、リターン文に遭遇しなかったという意味なので暗黙のNILを変えす
            try
            {
                interpreter.executeBlock(_declaration.body, environment);
            }
            catch (Return returnValue)
            {
                //もし初期化しの中でRETURN文を実行しようとしたら、値（NIL）を返す代わりにTHISを返します。
                if (_isInitializer)
                {
                    return _closure.getAt(0, "this");
                }

                return returnValue.value;
            }

            if (_isInitializer)
            {
                return _closure.getAt(0, "this");
            }

            return null;
        }

        public override string ToString()
        {
            return "<fn " + _declaration.name.lexeme + ">";
        }
    }
}
