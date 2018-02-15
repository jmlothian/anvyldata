using Anvyl.Data;
using Anvyl.Data.Abstract;
using Anvyl.Data.Validation.Abstract;
using Anvyl.Extensions.Configuration;
using Anvyl.Reflection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Anvyl.Reflection
{
    public class AssemblyReflector
    {

        List<Assembly> Assemblies = new List<Assembly>();
        Dictionary<string, List<Type>> SchemaObjectTypes = new Dictionary<string, List<Type>>();
        Dictionary<string, List<Type>> RegisteredObjectTypes = new Dictionary<string, List<Type>>();
        Dictionary<string, DataField> Objects = new Dictionary<string, DataField>();
        Dictionary<Type, IDatatype> RegisteredConversions = new Dictionary<Type, IDatatype>();
        //datatype, validator assigned.  Only one validator per type, if there are multiple validtions than a composite pattern can be used.
        public Dictionary<string, IConfigurationValidator> Validators = new Dictionary<string, IConfigurationValidator>();

        Dictionary<DataField, string> TypeNames = new Dictionary<DataField, string>();

        public List<DataField> FieldsForType = new List<DataField>();
        public DataField rootDataField = new DataField();
        public void RegisterTypeToDatatype(Type t, IDatatype datatype)
        {
            RegisteredConversions[t] = datatype;
        }
        public AssemblyReflector()
        {

        }
        public IExConfigurationProvider LoadProvider(string assemblyPath, string connectionString)
        {
            IExConfigurationProvider provider = null;
            var assembly = AssemblyLoader.Load(assemblyPath);
            //var assembly = Assembly.Load(assemblyName);
            var providerObjects = new List<Type>(assembly.ExportedTypes);
            foreach (Type t in providerObjects)
            {
                if (t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IExConfigurationProviderFactory)))
                {
                    provider = ((IExConfigurationProviderFactory)assembly.CreateInstance(t.FullName)).Create(connectionString);
                }
            }
            return provider;
        }
        public void LoadSchemaAssembly(string assemblyPath)
        {
            rootDataField.Datatype = new ObjectType();
            rootDataField.FieldName = "[root]";
            rootDataField.FieldLabel = "[root]";
            //var assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyPath);
            //var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            var assembly = AssemblyLoader.Load(assemblyPath);
            //var assembly = Assembly.Load(assemblyName);
            SchemaObjectTypes[assemblyPath] = new List<Type>(assembly.ExportedTypes);
            //assembly
            foreach (KeyValuePair<string, List<Type>> items in SchemaObjectTypes)
            {
                //types
                foreach (Type t in items.Value)
                {
                    //Console.WriteLine("======== Type - " + t.Name + " =============");
                    //specifically do not use the singleton
                    if (!(t is IConfigurationValidator))
                    {
                        ObjectType ot = new ObjectType();
                        ot.FQN = t.Namespace + "." + t.Name;
                        RegisterTypeToDatatype(t, ot);
                    }
                }
                foreach (Type t in items.Value)
                {
                    if (!(t is IConfigurationValidator))
                    {
                        //Console.WriteLine("======== Type - " + t.Name + " =============");
                        //properties
                        var info = t.GetProperties();
                        string typeName = GetTypeName(t);

                        DataField df = new DataField(typeName, t.Name, RegisteredConversions[t], rootDataField, rootDataField);
                        TypeNames[df] = typeName;
                        rootDataField.Members.Add(df.FieldName, df);
                        foreach (PropertyInfo p in info)
                        {
                            //Console.WriteLine(p.Name + " " + p.PropertyType.Name);
                            //df.Members.ad
                            var member = ParseType(p.PropertyType, p.Name, df);

                            if (member != null)
                            {
                                member.FieldName = p.Name;
                                member.FieldLabel = p.Name;
                                df.Members[p.Name] = member;
                            }
                        }
                        FieldsForType.Add(df);
                    }
                }
            }
        }
        public void LoadRegisteredTypes(string assemblyPath)
        {
            var assembly = AssemblyLoader.Load(assemblyPath);
            RegisteredObjectTypes[assemblyPath] = new List<Type>(assembly.ExportedTypes);
            foreach (KeyValuePair<string, List<Type>> items in RegisteredObjectTypes)
            {
                foreach (Type t in items.Value)
                {
                    //.ImplementedInterfaces.Contains(typeof(yourInterface)
                    if (t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDatatype)))
                    {
                        var instance = (IDatatype)assembly.CreateInstance(t.FullName);
                        RegisterTypeToDatatype(instance.GetCodeType(), instance.Datatype);
                    }
                    // ... it is /technically/ possible for an object to be both a datatype and it's own validator...
                    if (t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IConfigurationValidator)))
                    {
                        var instance = (IConfigurationValidator)assembly.CreateInstance(t.FullName);
                        Validators[instance.FQN] = instance;
                    }
                }
            }
        }

        private string GetTypeName(Type t)
        {
            string typeName = t.Name;
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(t);
            foreach (System.Attribute attr in attrs)
            {
                if (attr is ConfigurationName)
                {
                    typeName = ((ConfigurationName)attr).Name;
                }
            }
            return typeName;
        }

        private DataField ParseType(Type t, string Name, DataField Parent)
        {
            DataField field = null;
            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    //Console.WriteLine("\t\tNullable");
                    //Console.WriteLine("\t" + Nullable.GetUnderlyingType(t));
                    if (RegisteredConversions.ContainsKey(t))
                    {
                        //Console.WriteLine("\t\tRegistered Type Nullable<" + Nullable.GetUnderlyingType(t) + "> " + RegisteredConversions[t].FQN);
                        field = new DataField(t.Name, t.Name, RegisteredConversions[t], Parent, Parent.Root);
                    }
                    else
                    {

                    }
                }
                else
                {
                    //Console.WriteLine("\tGeneric");
                    //field = new DataField(t.Name, t.Name, ListType)
                    //Console.WriteLine("\t\tNot Nullable");

                    //gather all generic params
                    Type[] typeParameters = t.GetGenericArguments();
                    var gType = t.GetGenericTypeDefinition().Name;
                    var genericName = gType.Substring(0, gType.IndexOf('`'));
                    if (RegisteredConversions.ContainsKey(t.GetGenericTypeDefinition()))
                    {
                        field = new DataField(t.Name, t.Name, RegisteredConversions[t.GetGenericTypeDefinition()], Parent, Parent.Root);
                        List<DataField> datatypeParams = new List<DataField>();
                        foreach (Type tp in typeParameters)
                        {
                            var generic_fd = ParseType(tp, tp.Name, field);
                            datatypeParams.Add(generic_fd);
                            field.GenericParameters.Add(generic_fd);
                        }
                    }


                }

            }
            else
            {
                //Console.WriteLine("..." + t.Name);
                //Console.WriteLine("\tNot Generic / Not Nullable");
                if(RegisteredConversions.ContainsKey(t))
                {
                    field = new DataField(t.Name, t.Name, RegisteredConversions[t], Parent, Parent.Root);
                    //Console.WriteLine("\t\tRegistered Type " + t.Name + " "  + RegisteredConversions[t].FQN);
                }
                else
                {
                }
            }
            return field;
        }
    }
}
