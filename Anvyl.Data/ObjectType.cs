using Anvyl.Data.Abstract;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data
{
    public class ObjectValue : IDatatypeValue<Dictionary<string, IDatatypeValue<IDatatype>>>
    {
        //wire up to the singleton
        public IDatatype Datatype => ObjectType._dataType;

        public Dictionary<string, IDatatypeValue<IDatatype>> Value { get; set; }
    }

    public class ObjectType : IDatatype
    {
        public string FQN { get; set; } = "object";
        public List<IDatatype> GenericTypes { get; } = new List<IDatatype>();
        public List<IDatatype> Fields = new List<IDatatype>();
        public bool IsNullable => true;

        public bool IsValid
        {
            get
            {
                return true;
            }
        }
        public string Inherits => null;

        //provided as a static singleton - can't have statics in an interface, so use this convention
        internal static IDatatype _dataType = new ObjectType();
        //DO NOT bind to the static version
        public IDatatype Datatype => this;

        public ObjectValue GetValue<ObjectValue>()
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
