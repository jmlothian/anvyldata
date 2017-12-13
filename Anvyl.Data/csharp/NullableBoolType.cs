using Anvyl.Data.Abstract;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.csharp
{
    public class BoolValue : IDatatypeValue<bool>
    {
        //wire up to the singleton
        public IDatatype Datatype => BoolType._dataType;

        public bool Value { get; set; }
    }

    public class BoolType : IDatatype
    {
        public string FQN => "csharp.bool";
        public List<IDatatype> GenericTypes { get; } = new List<IDatatype>();
        public bool IsNullable => false;

        public bool IsValid { get
            {
                return true;
            }
        }
        public string Inherits => null;

        //provided as a static singleton - can't have statics in an interface, so use this convention
        internal static IDatatype _dataType = new BoolType();
        //bind to the static version
        public IDatatype Datatype => _dataType;

        public BoolValue GetValue<BoolValue>()
        {
            throw new NotImplementedException();
        }

        /*
        public bool FromConfig(IConfiguration configuration, string key)
        {
            throw new NotImplementedException();
        }

        public bool FromObject<T>(T obj)
        {
            
        }
        */
        public bool Validate<T>(T data)
        {
            return true;
        }
    }
}
