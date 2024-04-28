using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class LoxClass : ILoxCallable
    {
        public readonly string _name;
        public readonly LoxClass _superclass;
        private readonly Dictionary<string, LoxFunction> _methods;

        public LoxClass(string name, LoxClass _superclass, Dictionary<string, LoxFunction> methods)
        {
            this._name = name;
            this._superclass = _superclass;
            this._methods = methods;
        }

        public LoxFunction findMethod(string name)
        {
            if (this._methods.ContainsKey(name))
            {
                return this._methods[name];
            }


            if (_superclass != null)
            {
                return _superclass.findMethod(name);
            }

            return null;
        }


        //もし初期化しがあれば、そのメソッドのアリティによってクラス自身を呼び出すときに渡すべき引数の数が決まります。
        //ただしクラスに初期化子の定義を要求するものではありません。初期化子はオプションなので、もしなければアリティは０のままです。
        public int arity()
        {
            LoxFunction initializer = findMethod("init");
            if (initializer == null)
            {
                return 0;
            }

            return initializer.arity();
        }

        //クラスの呼び出しは、呼び出されたクラスの新しいLoxInstanceを実体化して、それを返します。
        //クラスが呼び出されたときはLoxInstance作成後に　initメソッドをさがします。
        //もしみつかたら即座にそれを束縛して、通常のメソッドコールのように呼び出します。
        //その時　引数リストをそのまま渡します。
        public object call(Interpreter interpreter, List<object> arguments)
        {
            LoxInstance instance = new LoxInstance(this);
            LoxFunction initializer = findMethod("init");

            if (initializer != null)
            {
                initializer.bind(instance).call(interpreter, arguments);
            }

            return instance;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
