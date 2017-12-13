using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.Abstract
{
    public interface IDatatypeValue<T>
    {
        IDatatype Datatype { get; }
        T Value { get; set; }
    }
}
