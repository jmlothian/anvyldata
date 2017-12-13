using Anvyl.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.csharp
{
    public class ListValue : IDatatypeValue<List<IDatatypeValue<IDatatype>>>
    {
        //wire up to the singleton
        public IDatatype Datatype => NullableBoolType._dataType;

        public List<IDatatypeValue<IDatatype>> Value { get; set; }
    }

    public class DictionaryValue : IDatatypeValue<Dictionary<IDatatypeValue<IDatatype>, IDatatypeValue<IDatatype>>>
    {
        //wire up to the singleton
        public IDatatype Datatype => NullableBoolType._dataType;

        public Dictionary<IDatatypeValue<IDatatype>, IDatatypeValue<IDatatype>> Value { get; set; }
    }


    public class ListType : IDatatype
    {
        public string FQN => "csharp.string";

        public bool IsNullable => true;

        public bool IsValid
        {
            get
            {
                return true;
            }
        }
        public string Inherits => null;

        public List<IDatatype> GenericTypes { get; } = new List<IDatatype>();
        //provided as a static singleton - can't have statics in an interface, so use this convention
        internal static IDatatype _dataType = new ListType();
        //bind to the static version
        public IDatatype Datatype => _dataType;

        public ListValue GetValue<ListValue>()
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
