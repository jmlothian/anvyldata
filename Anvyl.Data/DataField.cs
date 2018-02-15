using Anvyl.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data
{
    public class DataField
    {
        public DataField() { }
        public DataField(string fieldName, string fieldLabel, IDatatype datatype, DataField parent, DataField root)
        { 
            Id = Guid.NewGuid();
            Datatype = datatype;
            FieldName = fieldName;
            FieldLabel = fieldLabel;
            Root = root;
            Parent = parent;
        }
        public DataField(string fieldName, string fieldLabel, IDatatype datatype, Guid guid, DataField parent, DataField root)
        {
            Id = guid;
            Datatype = datatype;
            FieldName = fieldName;
            FieldLabel = fieldLabel;
            Root = root;
            Parent = parent;
        }
        public Guid Id { get; set; }
        public IDatatype Datatype { get; set; }
        public IDatatype DatatypeDictValue { get; set; }
        public string FieldName { get; set; }
        public string FieldLabel { get; set; }
        public DataField Root { get; set; }
        public DataField Parent { get; set; }
        //public IEnumerable<DataField> Children()
        //{
        //    return null;
        //}
        public Dictionary<string, DataField> Members { get; set; } = new Dictionary<string, DataField>();
        public bool ContainsKey(string key)
        {
            /*
            if (key.Contains(":"))
            {
                //we are not the target, but see if it matches a child
                string parentKey = key.Substring(0, key.IndexOf(':')).Trim(':');
                string childPath = key.Substring(key.IndexOf(':')).Trim(':');
                if (Members.ContainsKey(parentKey))
                {
                    return Members[parentKey].ContainsKey(childPath);
                } else
                {
                    return false;
                }
            }
            else
            {
                return Members.ContainsKey(key) ? true : false;
            }
            */
            //cheat for now, just see if we can get there...
            return GetValue(key) != null ? true : false;
        }
        public DataField this[string key]
        {
            get
            {
                return GetValue(key);
            }
        }
        private DataField GetValue(string key, int level = 0)
        {
            if (key.Contains(":"))
            {
                //we are not the target, but see if it matches a child
                string parentKey = key.Substring(0, key.IndexOf(':')).Trim(':');
                string childPath = key.Substring(key.IndexOf(':')).Trim(':');
                //Console.WriteLine("Searching for ..." + parentKey);
                if (Members.ContainsKey(parentKey))
                {
                    //check if parent is object/list/dict
                    switch(Members[parentKey].Datatype.FQN)
                    {
                        case "object":
                            break;
                        case "csharp.list<$1>":
                            if(childPath.Contains(":"))
                            {
                                //"0:member"
                                return Root[Members[parentKey].GenericParameters[0].FieldName].GetValue(childPath.Substring(childPath.IndexOf(":")).Trim(':'), level + 1);
                            }
                            else
                            {
                                //"0"
                                if (Members[parentKey].GenericParameters[0].Datatype.IsPOCO)
                                {
                                    return Members[parentKey].GenericParameters[0];
                                } else
                                {
                                    return Root[Members[parentKey].GenericParameters[0].FieldName];
                                }
                            }
                        case "csharp.dictionary<$1,$2>":
                            if (childPath.Contains(":"))
                            {
                                //"0:member"
                                return Root[Members[parentKey].GenericParameters[1].FieldName].GetValue(childPath.Substring(childPath.IndexOf(":")).Trim(':'), level + 1);
                            }
                            else
                            {
                                //"0"
                                if (Members[parentKey].GenericParameters[1].Datatype.IsPOCO)
                                {
                                    return Members[parentKey].GenericParameters[1];
                                }
                                else
                                {
                                    return Root[Members[parentKey].GenericParameters[1].FieldName];
                                }
                            }
                        default:
                            break;
                    }
                    if (Root != null && Members[parentKey].Datatype is ObjectType)
                    {//members in root, match FQN
                        foreach (KeyValuePair<string, DataField> dr in Root.Members)
                        {
                            if(dr.Value.Datatype == Members[parentKey].Datatype)
                            {
                                return dr.Value[childPath];
                            }
                        }
                        return Root[parentKey][childPath];
                    }
                    else
                    {
                        return Members[parentKey].GetValue(childPath, level + 1);
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (Root != null && Members.ContainsKey(key) && Members[key].Datatype is ObjectType)
                {//members in root, match FQN
                    foreach (KeyValuePair<string, DataField> dr in Root.Members)
                    {
                        if (dr.Value.Datatype == Members[key].Datatype)
                        {
                            return dr.Value;
                        }
                    }
                    return Root[key];
                }
                else
                {
                    if(Members.ContainsKey(key))
                        return Members[key];
                    else
                        return null;
                }
                //return Members.ContainsKey(key) ? Members[key] : null;
            }
        }
        //Dictionary<IDatatype, IDatatype> DictionaryValues = new Dictionary<IDatatype, IDatatype>();
        public List<DataField> GenericParameters = new List<DataField>();
        //List<IDatatype> ListValues = new List<IDatatype>();
        public string ToJson(int indent = 0)
        {
            string ind = new string(' ', indent * 4);
            ++indent;

            StringBuilder sb = new StringBuilder(ind + "\""+FieldName+"\"" + " : {\r\n");
            sb.AppendLine(ind + "  \"Id\" : \"" + Id.ToString()+"\",");

            string fqn = Datatype.FQN;
            /*
            for (int i = 0; i < GenericParameters.Count; i++)
            {
                fqn = fqn.Replace("$" + (i + 1).ToString(), GenericParameters[i].Datatype.FQN);
            }
            */

            sb.AppendLine(ind + "  \"Datatype\" : \"" + fqn + "\",");
            sb.AppendLine(ind + "  \"FieldLabel\" : \"" + FieldLabel+"\",");

            sb.AppendLine(ind + "  \"Members\" : {");
            StringBuilder members = new StringBuilder();
            foreach (KeyValuePair<string, DataField> kvp in Members)
            {
                members.AppendLine(kvp.Value.ToJson(indent).TrimEnd() + ",");
            }
            sb.AppendLine(members.ToString().TrimEnd().TrimEnd(','));
            sb.AppendLine(ind + "  },"); //members

            sb.AppendLine(ind + "  \"GenericParameters\" : {");
            StringBuilder genericParameters = new StringBuilder();
            foreach (DataField df in GenericParameters)
            {
                genericParameters.AppendLine(df.ToJson(indent).TrimEnd() + ",");
            }
            sb.AppendLine(genericParameters.ToString().TrimEnd().TrimEnd(','));
            sb.AppendLine(ind + "  }"); //GenericParameters



            sb.AppendLine(ind + "}"); //object
            return sb.ToString();
        }
    }
}
