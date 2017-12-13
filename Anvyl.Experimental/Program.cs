using Anvyl.Data;
using Anvyl.Data.Abstract;
using Anvyl.Data.csharp;
using Anvyl.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Anvyl.Experimental
{
    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public new static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();


        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.DeclaringType.GetInterfaces().Contains(typeof(IDatatype)) && property.PropertyName == "Datatype")
            {
                property.Ignored = true;
            }
            return property;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            AssemblyReflector reflector = new AssemblyReflector();
            reflector.RegisterTypeToDatatype(typeof(bool), new BoolType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(bool?), new NullableBoolType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(Int32?), new NullableInt32Type().Datatype);
            reflector.RegisterTypeToDatatype(typeof(String), new StringType().Datatype);
            reflector.LoadAssembly(@"C:\Users\jeremy.lothian\OneDrive\Projects\Anvyl\Anvyl.Experimental.Library\bin\Debug\netcoreapp2.0\Anvyl.Experimental.Library.dll");


            var settings = new JsonSerializerSettings
            {
                ContractResolver = ShouldSerializeContractResolver.Instance,
                Formatting = Formatting.Indented
            };
            string serialized = JsonConvert.SerializeObject(reflector.FieldsForType, settings);
            Console.WriteLine(serialized);
            List<DataField> df = JsonConvert.DeserializeObject<List<DataField>>(serialized);
            string serialized2 = JsonConvert.SerializeObject(df, settings);
            if(serialized == serialized2)
            {
                Console.WriteLine("Sussesses!");
            }

        }
    }
}
