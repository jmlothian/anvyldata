using Anvyl.Data;
using Anvyl.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Anvyl.Reflection
{
    public class AssemblyReflector
    {

        List<Assembly> Assemblies = new List<Assembly>();
        Dictionary<string, List<Type>> Types = new Dictionary<string, List<Type>>();
        Dictionary<string, DataField> Objects = new Dictionary<string, DataField>();
        Dictionary<Type, IDatatype> RegisteredConversions = new Dictionary<Type, IDatatype>();
        public List<DataField> FieldsForType = new List<DataField>();
        public void RegisterTypeToDatatype(Type t, IDatatype datatype)
        {
            RegisteredConversions[t] = datatype;
        }
        public AssemblyReflector()
        {

        }
        public void LoadAssembly(string assemblyPath)
        {
            var assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyPath);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            //var assembly = Assembly.Load(assemblyName);
            Types[assemblyPath] = new List<Type>(assembly.ExportedTypes);
            //assembly
            foreach (KeyValuePair<string, List<Type>> items in Types)
            {
                //types
                foreach (Type t in items.Value)
                {
                    Console.WriteLine("======== Type - " + t.Name + " =============");
                    //specifically do not use the singleton
                    ObjectType ot = new ObjectType();
                    ot.FQN = t.Namespace + "." + t.Name;
                    RegisterTypeToDatatype(t, ot);
                    //properties
                }
                foreach (Type t in items.Value)
                {
                    Console.WriteLine("======== Type - " + t.Name + " =============");
                    //properties
                    var info = t.GetProperties();
                    DataField df = new DataField(t.Name, t.Name, RegisteredConversions[t]);
                    foreach (PropertyInfo p in info)
                    {
                        Console.WriteLine(p.Name + " " + p.PropertyType.Name);
                        //df.Members.ad
                        var member = ParseType(p.PropertyType);

                        if(member != null)
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

        private DataField ParseType(Type t)
        {
            DataField field = null;
            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    //Console.WriteLine("\t\tNullable");
                    Console.WriteLine("\t" + Nullable.GetUnderlyingType(t));
                    if (RegisteredConversions.ContainsKey(t))
                    {
                        Console.WriteLine("\t\tRegistered Type Nullable<" + Nullable.GetUnderlyingType(t) + "> " + RegisteredConversions[t].FQN);
                    } else
                    {

                    }
                }
                else
                {
                    Console.WriteLine("\tGeneric");
                    //field = new DataField(t.Name, t.Name, ListType)
                    //Console.WriteLine("\t\tNot Nullable");

                    //gather all generic params
                    Type[] typeParameters = t.GetGenericArguments();
                    var gType = t.GetGenericTypeDefinition().Name;
                    var genericName = gType.Substring(0, gType.IndexOf('`'));
                    if (RegisteredConversions.ContainsKey(t.GetGenericTypeDefinition()))
                    {
                        field = new DataField(t.Name, t.Name, RegisteredConversions[t.GetGenericTypeDefinition()]);
                        List<DataField> datatypeParams = new List<DataField>();
                        foreach (Type tp in typeParameters)
                        {
                            var generic_fd = ParseType(tp);
                            datatypeParams.Add(generic_fd);
                            field.GenericParameters.Add(generic_fd);
                        }
                    }


                }

            }
            else
            {
                Console.WriteLine("..." + t.Name);
                Console.WriteLine("\tNot Generic / Not Nullable");
                if(RegisteredConversions.ContainsKey(t))
                {
                    field = new DataField(t.Name, t.Name, RegisteredConversions[t]);
                    Console.WriteLine("\t\tRegistered Type " + t.Name + " "  + RegisteredConversions[t].FQN);
                }
                else
                {
                }
            }
            return field;
        }
    }
}
