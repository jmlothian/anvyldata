using AccuWeather.Configuration.Models.Accufiguration;
using Anvyl.Data;
using Anvyl.Data.Abstract;
using Anvyl.Data.csharp;
using Anvyl.Data.Validation;
using Anvyl.Data.Validation.Abstract;
using Anvyl.Extensions.Configuration.Json;
using Anvyl.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

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
    public class TestValidatorNullableInt32 : IConfigurationValidator
    {
        public string FQN => "csharp.int32.nullable";

        public IValidationResponse IsValid(IConfiguration configuration, string Path)
        {
            ValidationResponse resp = new ValidationResponse();
            resp.FQN = FQN;
            resp.Path = Path;
            var val = configuration.Get<Int32?>();
            if(val != null && val < 0)
            {
                resp.ErrorCode = "Int32?.1001";
                resp.ErrorMessage = "Value less than zero";
                resp.Passed = false;
            }
            return resp;
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
            reflector.RegisterTypeToDatatype(typeof(Int32), new Int32Type().Datatype);
            reflector.RegisterTypeToDatatype(typeof(String), new StringType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(List<>), new ListType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(Dictionary<,>), new DictionaryType().Datatype);
            
            reflector.LoadSchemaAssembly(@"C:\Users\jeremy.lothian\OneDrive\Projects\Anvyl\Anvyl.Experimental.Library\bin\Debug\netcoreapp2.0\Anvyl.Experimental.Library.dll");


            var settings = new JsonSerializerSettings
            {
                ContractResolver = ShouldSerializeContractResolver.Instance,
                Formatting = Formatting.Indented
            };
            StringBuilder sb = new StringBuilder();
            foreach(DataField df in reflector.FieldsForType)
            {
                Console.WriteLine(df.ToJson());
                sb.AppendLine(df.ToJson().TrimEnd() + ",");
            }
            File.WriteAllText("output.json", "{" + sb.ToString().TrimEnd().TrimEnd(',') + "}");
            //string serialized = JsonConvert.SerializeObject(reflector.FieldsForType, settings);
            //Console.WriteLine(serialized);
            //List<DataField> df = JsonConvert.DeserializeObject<List<DataField>>(serialized);
            //string serialized2 = JsonConvert.SerializeObject(df, settings);
            //if(serialized == serialized2)
            {
                //    Console.WriteLine("Sussesses!");
            }
             
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
            configurationBuilder.AddJsonFile("xpconfig.json");

            var configuration = configurationBuilder.Build();
            var asettings = configuration.GetSection("Accufiguration").Get<AccufigurationSettings>();


            //validation
            var Validation = new ConfigurationValidationManager();
            Validation.RegisterValidator(new TestValidatorNullableInt32());

            var isValid = Validation.Validate(configuration.GetSection("Accufiguration"), reflector.FieldsForType[0]);

            //JsonExConfigurationProvider jsonProv = new JsonExConfigurationProvider(new JsonConfigurationSource());
            //jsonProv.Save(configuration, configuration, reflector.rootDataField, "**********[MARKED_FOR_DELETE]**********", "outNew.txt");

        }
    }
}
