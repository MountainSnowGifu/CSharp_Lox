using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    //LoxClassのインスタンスを実行時に表現するもの。
    internal class LoxInstance
    {
        private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();
        private LoxClass klass;
        public LoxInstance(LoxClass klass)
        {
            this.klass = klass;
        }

        //このインスタンスに与えられた名前のフィールドが存在するか確認し、確かにあればそれを返す
        //名前がなければエラー
        //fieldsはインスタンスに直接格納される名前付き状態
        //プロパティはGET式が返すことができる、名前付きのものたち（OBJECT）
        //どのフィールドもプロパティですが、すべてのプロパティがフィールドとは限らない。
        public object get(Token name)
        {
            if (_fields.ContainsKey(name.lexeme))
            {
                return _fields[name.lexeme];
            }

            //インスタンスには状態を持たせ、クラスには振る舞いを持たせます。
            //LoxInstanceにフィールドのマップがあるように、LoxClassにはメソッドのMAPを持たせます。
            //したがってメソッドの所有者はクラスですが、それでもメソッドはそのクラスのインスタンスを通じてアクセスされます。

            //インスタンスでプロパティを探索してもマッチするフィールドが見つからないときは、その名前を持つメソッドを、その
            //インスタンスのクラスで探します。そして見つかったものを返します。
            //プロパティのアクセスには、フィールドを取得する場合も（つまりインスタンスに格納された状態データを読みだすときも）
            //そのインスタンスのクラスで定義されたメソッドを使う場合もあるのです。
            LoxFunction method = klass.findMethod(name.lexeme);

            //実行時にはインスタンスでメソッドが見つかるたびに、その環境を作成する
            if (method != null)
            {
                return method.bind(this);
            }

            throw new RuntimeError(name, "Undefined property '" + name.lexeme + "' .");
        }

        //ただ値を表現するMAPに入れるだけです。LOXではインスタンスの新しいフィールドを自由に作れるので、
        //そのキーがすでに存在するかチェックする必要はありません。
        public void set(Token name, object value)
        {
            _fields[name.lexeme] = value;
        }

        public override string ToString()
        {
            return klass._name + " instance";
        }
    }
}
