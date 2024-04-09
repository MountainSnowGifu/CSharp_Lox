using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lox
{
    internal class Return : SystemException
    {
        internal readonly object value;

        internal Return(object value) : base()
        {
            this.value = value;
        }
    }
}
