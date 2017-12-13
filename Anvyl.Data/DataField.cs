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
        Dictionary<IDatatype, IDatatype> DictionaryValues = new Dictionary<IDatatype, IDatatype>();
        List<IDatatype> ListValues = new List<IDatatype>();

    }
}
