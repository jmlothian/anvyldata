using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Anvyl.Data.Abstract
{
    public interface IDatatype
    {
        IDatatype Datatype { get; }
        string FQN { get; }
        /*
        bool FromObject(object obj);
        bool FromConfig(IConfiguration configuration, string key);
        */
        bool Validate<T>(T data);
        bool IsNullable { get; }
        bool IsValid { get; }
        string Inherits { get; }
        List<IDatatype> GenericTypes { get; }
    }
}
