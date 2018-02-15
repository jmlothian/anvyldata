using Anvyl.Data.Abstract;
using Anvyl.Data.csharp;
using Anvyl.Data.Validation;
using Anvyl.Data.Validation.Abstract;
using Anvyl.Experimental;
using Anvyl.Extensions.Configuration;
using Anvyl.Extensions.Configuration.Json;
using Anvyl.Reflection;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anvyl.Data.Configuration.Edit
{
    public class ConfigurationManager
    {
        AssemblyReflector reflector => Program.reflector;
        DataField rootDataField => Program.rootDataField;
        IConfigurationRoot configuration { get { return Program.configuration; } set { Program.configuration = value; } }
        IConfigurationBuilder configurationBuilder => Program.configurationBuilder;
        public ConfigurationValidationManager Validation = new ConfigurationValidationManager();
        private IExConfigurationProvider ConfigProvider = null;
        private string DeletionString = "**********[MARKED_FOR_DELETE]**********";
        public ConfigurationManager(IExConfigurationProvider provider, string deletionString = null)
        {
            ConfigProvider = provider;
            if (deletionString != null)
                DeletionString = deletionString;
        }
        public bool Validate()
        {
            //pass in with module
            //Validation.RegisterValidator(new TestValidatorNullableInt32());
            IValidationResponse resp = Validation.Validate(configuration, reflector.rootDataField);
            return resp.Passed;
        }
        private void createConfigObject(Dictionary<string, string> dict, DataField df, string path)
        {
            //members
            path = path + ":" + df.FieldLabel;
            if (df.Datatype is ObjectType)
            {
                foreach (KeyValuePair<string, DataField> childDf in df.Members)
                {
                    if (childDf.Value.Datatype.IsPOCO)
                    {
                        //initialize to empty string, validation will catch stuff later
                        dict[path + ":" + childDf.Value.FieldName] = "";
                    }
                    else
                    {
                        createConfigObject(dict, childDf.Value, path);
                    }
                }
            }
        }

        public void ValidateAndSave(CommandOption validateOption, string key, string value, string connectionString)
        {
            var res = true;
            if (validateOption.HasValue())
                res = Validate();
            if (res == false)
                Console.Error.WriteLine(value + " is an invalid value for key " + key);
            else
            {
                Console.WriteLine("Ok");
                //JsonExConfigurationProvider secp = new JsonExConfigurationProvider(new JsonConfigurationSource());
                ConfigProvider.Save(configuration, configuration, rootDataField, DeletionString, connectionString);
            }
        }
        public void AddDictionaryItem(string dictKey, string key, string value, CommandOption validateOption, Dictionary<string, string> toAdd)
        {
            //dictionary, need a key
            if (string.IsNullOrEmpty(dictKey)) { Console.Error.WriteLine("You must provide a key for this dictionary"); return; }

            //objects can't simply be "set" in a dictionary, they have to be created and iterated through
            if (!rootDataField[key].GenericParameters[1].Datatype.IsPOCO)
            {
                AddObject(key + ":" + dictKey, rootDataField[rootDataField[key].GenericParameters[1].FieldName], toAdd);
                /*
                //get the object type in this dictionary
                IDatatype dt = rootDataField[key].GenericParameters[1].Datatype;

                //get the field of this type
                foreach (DataField df in reflector.FieldsForType)
                {
                    if (df.FieldName == rootDataField[key].FieldName)
                    {
                        //dictionary to store keys
                        createConfigObject(toAdd, df, key);
                        break;
                    }
                }
                */
            }
            else
            {
                if (string.IsNullOrEmpty(value)) { Console.Error.WriteLine("You must provide a value for a POCO dictionary entry"); return; }
                //POCOs are nice.
                toAdd[key + ":" + dictKey] = value;
            }
        }
        public void AddListItem(string key, string value, Dictionary<string, string> toAdd)
        {
            if (rootDataField[key].GenericParameters[0].Datatype.IsPOCO)
            {
                if (string.IsNullOrEmpty(value)) { Console.Error.WriteLine("You must provide a value for a POCO list entry"); return; }
                //POCOs are nice.
                //get last index
                var configSect = configuration.GetSection(key);
                var cnt = configSect.GetChildren().Count();
                var child = configSect.GetChildren();
                toAdd[key + ":" + cnt.ToString()] = value;
                /*
                var dict = new Dictionary<string, string>   
                            {
                                {key + ":" + cnt.ToString(), value}
                            };
                configurationBuilder.AddInMemoryCollection(dict);
                configuration = configurationBuilder.Build();
                ValidateAndSave(validateOption, key, value);
                */
            }
            else
            {
                //var dict = new Dictionary<string, string>();
                //get type from root
                DataField df = rootDataField[rootDataField[key].GenericParameters[0].FieldLabel];
                //get index from data
                int newIndex = 0;
                var sect = configuration.GetSection(key + ":0");
                while(sect.Exists())
                {
                    newIndex++;
                    sect = configuration.GetSection(key + ":" + newIndex.ToString());
                }
                AddObject(key + ":" + newIndex.ToString(), df, toAdd);
                //AddObject(key)
                //iterate over Datafield, adding to dict as we go, recursively
            }
        }
        public void DeleteItem(string key, IConfigurationRoot root, Dictionary<string, string> toDelete, string deletionString)
        {
            if (deletionString == null)
                deletionString = DeletionString;
            if (root.GetSection(key) != null)
            {
                DeleteSection(root.GetSection(key), toDelete, deletionString);
            }
        }
        public void DeleteSection(IConfigurationSection section, Dictionary<string, string> toDelete, string deletionString)
        {
            //there isn't a way to actually delete a section in netcore configuration system, so set everything to a magic string
            if (deletionString == null)
                deletionString = DeletionString;
            toDelete[section.Path] = deletionString;
            foreach(IConfigurationSection sect in section.GetChildren())
            {
                DeleteSection(sect, toDelete, deletionString);
            }
        }
        public void AddObject(string key, DataField df, Dictionary<string, string> toAdd)
        {
            //add this key
            toAdd[key] = null;
            //iterate objects
            foreach(KeyValuePair<string, DataField> child in df.Members)
            {
                if(child.Value.Datatype.IsPOCO)
                {
                    //easy peasy, add it blank
                    toAdd[key + ":" + child.Value.FieldName] = null;
                } else if (child.Value.Datatype is ObjectType)
                {
                    //lookup FQN
                    foreach(KeyValuePair<string, DataField> objs in child.Value.Root.Members)
                    {
                        if(objs.Value.Datatype.FQN == child.Value.Datatype.FQN)
                        {
                            AddObject(key + ":" + child.Value.FieldName, objs.Value, toAdd);
                        }
                    }
                } else if (child.Value.Datatype is ListType)
                {
                    //don't add an item to an empty list!
                    //AddListItem(key + ":" + child.Value.FieldName, "", toAdd);
                    //set list up though
                    toAdd[key + ":" + child.Value.FieldName] = null;
                }
            }
        }
    }
}
