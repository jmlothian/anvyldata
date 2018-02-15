using Anvyl.Data.Abstract;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.csharp
{
    public class Int32Value : IDatatypeValue<Int32>
    {
        //wire up to the singleton
        public IDatatype Datatype => Int32Type._dataType;

        public Int32 Value { get; set; }
    }

    public class Int32Type : IDatatype
    {
        public string FQN => "csharp.int32";
        public List<IDatatype> GenericTypes { get; } = new List<IDatatype>();
        public bool IsNullable => false;
        public bool IsPOCO => true;
        public bool IsValid { get
            {
                return true;
            }
        }
        public string Inherits => null;
        public Type GetCodeType()
        {
            return typeof(Int32);
        }
        //provided as a static singleton - can't have statics in an interface, so use this convention
        internal static IDatatype _dataType = new Int32Type();
        //bind to the static version
        public IDatatype Datatype => _dataType;

        public Int32Value GetValue<Int32Value>()
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
