using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class LoxEnvironment
    {
        internal LoxEnvironment _enclosing { get; }
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        internal LoxEnvironment()
        {
            this._enclosing = null;
        }

        internal LoxEnvironment(LoxEnvironment enclosing)
        {
            this._enclosing= enclosing;
        }
        internal void define(string name, object value)
        {
            _values.Add(name, value);
        }

        //ancestorで得た環境のマップにあるその変数の値を返す。
        internal object getAt(int distance,string name)
        {
            var tmp = ancestor(distance);
            return tmp._values[name];
            //return ancestor(distance)._values[name];
        }

        //このメソッドはチェーンの親に向かって固定数のホップを行い、そこにある環境を返します。
        //ここでは変数が存在することさえチェックしません。そこにあることは、すでにリゾルバによって確認済み
        internal LoxEnvironment ancestor(int distance)
        {
            LoxEnvironment environment = this;
            for (int i = 0; i < distance; i++)
            {
                environment = environment._enclosing;
            }

            return environment;
        }

        //固定数の環境を辿り、そのマップに新しい値を記入します。
        internal void AssignAt(int distance, Token name, object value)
        {
            //ancestor(distance)._values.Add(name.lexeme, value);
            ancestor(distance)._values[name.lexeme]=value;
        }

        internal object get(Token name)
        {
            if (_values.ContainsKey(name.lexeme))
            {
                return _values[name.lexeme];
            }

            if (this._enclosing != null)
            {
                return this._enclosing.get(name);
            }

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        internal void assign(Token name, object value)
        {
            if (_values.ContainsKey(name.lexeme))
            {
                _values[name.lexeme] = value;
                return;
            }

            if (this._enclosing != null)
            {
                this._enclosing.assign(name, value);
                return;
            }

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }
    }
}
