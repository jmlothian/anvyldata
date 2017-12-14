using Anvyl.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data
{
    public class DataField
    {
        public DataField() { }
        public DataField(string fieldName, string fieldLabel, IDatatype datatype)
        { 
            Id = Guid.NewGuid();
            Datatype = datatype;
            FieldName = fieldName;
            FieldLabel = fieldLabel;
        }
        public DataField(string fieldName, string fieldLabel, IDatatype datatype, Guid guid)
        {
            Id = guid;
            Datatype = datatype;
            FieldName = fieldName;
            FieldLabel = fieldLabel;
        }
        public Guid Id { get; set; }
        public IDatatype Datatype { get; set; }
        public IDatatype DatatypeDictValue { get; set; }
        public string FieldName { get; set; }
        public string FieldLabel { get; set; }
        //public IEnumerable<DataField> Children()
        //{
        //    return null;
        //}
        public Dictionary<string, DataField> Members { get; set; } = new Dictionary<string, DataField>();
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
            for (int i = 0; i < GenericParameters.Count; i++)
            {
                fqn = fqn.Replace("$" + (i + 1).ToString(), GenericParameters[i].Datatype.FQN);
            }


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
