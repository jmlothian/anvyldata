using Anvyl.Data.Abstract;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.csharp
{
    public class StringValue : IDatatypeValue<string>
    {
        //wire up to the singleton
        public IDatatype Datatype => StringType._dataType;

        public string Value { get; set; }
    }

    public class StringType : IDatatype
    {
        public string FQN => "csharp.string";
        public List<IDatatype> GenericTypes { get; } = new List<IDatatype>();
        public bool IsNullable => true;

        public bool IsValid { get
            {
                return true;
            }
        }
        public string Inherits => null;

        //provided as a static singleton - can't have statics in an interface, so use this convention
        internal static IDatatype _dataType = new StringType();
        //bind to the static version
        public IDatatype Datatype => _dataType;

        public StringValue GetValue<StringValue>()
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
