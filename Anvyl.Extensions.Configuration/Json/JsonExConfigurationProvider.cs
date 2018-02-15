using Anvyl.Data;
using Anvyl.Data.csharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Anvyl.Extensions.Configuration.Json
{
    public class JsonExConfigurationProviderFactory : IExConfigurationProviderFactory
    {
        public IExConfigurationProvider Create(string connectionString)
        {
            //get all the datas
            //string data = (new FileStream(connectionString)).Read(data,0, length);
            string data = "";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data));
            var source = new JsonStreamConfigurationSource();
            source.stream = stream;
            return new JsonExConfigurationProvider(source);
        }
    }

    public class JsonExConfigurationProvider : JsonStreamConfigurationProvider, IExConfigurationProvider
    {
        public JsonExConfigurationProvider(JsonStreamConfigurationSource source) : base(source) { }

        StringBuilder sb = new StringBuilder();
        DataField RootDataField;
        public bool Load(IConfigurationBuilder configBuilder, string connectionString)
        {
            configBuilder.AddJsonFile(connectionString);
            return true;
        }
        public bool Save(IConfigurationRoot root, IConfiguration configuration, DataField rootDataField, string deletionValue= "**********[MARKED_FOR_DELETE]**********", string fileName = null)
        {
            sb.Clear();
            RootDataField = rootDataField;
            bool success = true;

            try
            {
                sb.AppendLine("{");
                foreach (IConfigurationSection sect in configuration.GetChildren())
                {
                    ParseSection(root, sect, deletionValue);
                }
                sb.AppendLine("}");
                File.WriteAllText(fileName, JObject.Parse(sb.ToString()).ToString());
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                success = false;
            }

            return success;
        }

        private void ParseSection(IConfigurationRoot root, IConfigurationSection section, string deletionValue)
        {
            Console.WriteLine("Parsing section: " + section.Key);
            IEnumerable<KeyValuePair<string, string>> props = section.AsEnumerable();
            bool writeOuter = section.GetChildren().Count() > 0;
            string LastList="";
            string LastDict="";
            if (section.Value != deletionValue)
            {


                if (writeOuter)
                {
                    //check if parent is list
                    if (section.Path.Contains(":") && RootDataField[section.Path.Substring(0, section.Path.LastIndexOf(':'))].Datatype is ListType)
                    {
                        sb.AppendLine("{");
                    }
                    else
                    {
                        sb.AppendLine("\"" + section.Key + "\" : {");
                    }
                }
                foreach (KeyValuePair<string, string> kvp in section.AsEnumerable())
                {
                    if (LastDict != "" && !kvp.Key.StartsWith(LastDict))
                    {
                        sb.AppendLine("},");
                        LastDict = "";
                    }
                    Console.WriteLine("...Enumerating: " + kvp.Key);
                    //we have a list we're working on
                    //does this item start with the list? item:list
                    //if we remove the list - item:list:0 -> 0 [false], item:list:0:subobj -> 0:subobj [true]
                    //  then don't process, already handled by recursion.
                    if (LastList != "" && kvp.Key.StartsWith(LastList) && kvp.Key.Substring(LastList.Length).TrimStart(':').Contains(":"))
                    {
                        Console.WriteLine("      Ignoring - " + kvp.Key);
                    }
                    else
                    {
                        //value is actually at this level, should have no extra :'s
                        //value == null could be either a section or a value that is truly null.  Oh Well.
                        if (RootDataField[kvp.Key] != null
                            && kvp.Value == deletionValue
                            )
                        {
                            //deleted
                        }
                        else
                        {
                            if (RootDataField[kvp.Key] != null //for datatypes that aren't implemented
                                && RootDataField[kvp.Key].Datatype.IsPOCO
                                && !kvp.Key.Replace(section.Path + ":", "").Contains(":"))
                            {
                                Console.WriteLine("......Member: " + kvp.Key + "=" + kvp.Value);
                                //remove the path from the key
                                if (kvp.Value == null)
                                {
                                    sb.AppendLine("\"" + kvp.Key.Replace(section.Path + ":", "") + "\" : null,");
                                }
                                else
                                {
                                    sb.AppendLine("\"" + kvp.Key.Replace(section.Path + ":", "") + "\" : \"" + kvp.Value + "\",");
                                }
                                //we rely on json.net to remove the trailing commas
                            }
                            else
                            {
                                if (kvp.Value == null)
                                {
                                    //check dict
                                    if (kvp.Key.Contains(':') && RootDataField.ContainsKey(kvp.Key) && RootDataField[kvp.Key].Datatype is DictionaryType)
                                    {
                                        Console.WriteLine("......Dictionary");
                                        if (kvp.Key.Replace(section.Path + ":", "").Contains(':'))
                                        {
                                            //todo: not sure why this happens yet
                                            Console.WriteLine(".................duplicate dict ignored: " + kvp.Key.Replace(section.Path + ":", ""));
                                        }
                                        else
                                        {
                                            LastDict = kvp.Key;
                                            sb.AppendLine("\"" + kvp.Key.Replace(section.Path + ":", "") + "\" : {");

                                            //poco values don't include objects/sub-sections, so the recursive ParseSection won't add them to the dictionary
                                            if (RootDataField[kvp.Key].GenericParameters[1].Datatype.IsPOCO)
                                            {
                                                //no objects, so lets just iterate through them?
                                                foreach (IConfigurationSection s in root.GetSection(kvp.Key).GetChildren())
                                                {
                                                    if (s.Path != kvp.Key)
                                                    {
                                                        if (s.Value == null)
                                                        {
                                                            sb.AppendLine("\"" + s.Key.Replace(section.Path + ":", "") + "\" : null,");
                                                        }
                                                        else
                                                        {
                                                            sb.AppendLine("\"" + s.Key + "\" : \"" + s.Value + "\",");
                                                        }
                                                    }
                                                }

                                                /*
                                                //iterate through each item...
                                                foreach (IConfigurationSection s in root.GetSection(kvp.Key).GetChildren())
                                                {
                                                    //this should only fail if a property does not have a supported datatype
                                                    //the values will be output anyways, since they are included as members with values and not just sections
                                                    //todo: warn if this happens
                                                    if (RootDataField[s.Path] != null)
                                                    {
                                                        if (!(RootDataField[s.Path].Datatype is ListType) && !(RootDataField[s.Path].Datatype is DictionaryType))
                                                            ParseSection(root, s);
                                                    }
                                                }
                                                //ParseSection(root, root.GetSection(kvp.Key));
                                                sb.AppendLine("},");
                                                */
                                            }
                                        }
                                    }
                                    else if (RootDataField[kvp.Key] != null
                                        //&& kvp.Key.Contains(':')
                                        //&& RootDataField[kvp.Key.Substring(0, kvp.Key.LastIndexOf(':'))] != null
                                        //must check parent to determine if this is a list
                                        //&& RootDataField[kvp.Key.Substring(0, kvp.Key.LastIndexOf(':'))].Datatype is ListType)
                                        && RootDataField[kvp.Key].Datatype is ListType) //check for list
                                    {
                                        Console.WriteLine("......List - " + kvp.Key);
                                        LastList = kvp.Key;
                                        if (kvp.Key.Replace(section.Path + ":", "").Contains(':'))
                                        {
                                            //todo: not sure why this happens yet
                                            Console.WriteLine(".................duplicate list ignored: " + kvp.Key.Replace(section.Path + ":", ""));
                                        }
                                        else
                                        {
                                            sb.AppendLine("\"" + kvp.Key.Replace(section.Path + ":", "") + "\" : [");
                                            //iterate through each list item, adding as you go.  If poco, its easy.  Otherwise, have to iterate sections
                                            //foreach (KeyValuePair<string, string> kvp in section.AsEnumerable())
                                            //{
                                            //value is actually at this level, should have no extra :'s
                                            //value == null could be either a section or a value that is truly null.  Oh Well.
                                            int atIndex = 0;
                                            //root.AsEnumerable().Contains(kvp.Key + ":" + atIndex.ToString()) ;
                                            Dictionary<string, string> rootDict = root.AsEnumerable()
                                                      .ToDictionary(x => x.Key, x => x.Value);
                                            bool containsIndex = rootDict.ContainsKey(kvp.Key + ":" + atIndex.ToString()); //root[kvp.Key + ":" + atIndex.ToString()] != null;
                                            while (containsIndex)
                                            {
                                                Console.WriteLine(".............." + atIndex.ToString());
                                                if (RootDataField[kvp.Key].GenericParameters[0].Datatype.IsPOCO)
                                                {
                                                    sb.AppendLine("\"" + root[kvp.Key + ":" + atIndex.ToString()] + "\",");
                                                    Console.WriteLine(".................Member: " + kvp.Key + "=" + root[kvp.Key + ":" + atIndex.ToString()]);
                                                }
                                                else
                                                {
                                                    //sb.AppendLine("\"" + kvp.Key + "\" : ");
                                                    ///sb.AppendLine("\"[OBJECT-" + kvp.Key + "]\",");
                                                    //add properties

                                                    //handle children
                                                    ParseSection(root, root.GetSection(kvp.Key + ":" + atIndex.ToString()), deletionValue);
                                                }
                                                atIndex++;
                                                containsIndex = rootDict.ContainsKey(kvp.Key + ":" + atIndex.ToString());
                                            }

                                            sb.AppendLine("],");
                                        }
                                    }
                                    else
                                    {
                                        //section?
                                        //dictionary?
                                        if (kvp.Key.Contains(":") && kvp.Key != section.Path)
                                        {
                                            if (RootDataField[kvp.Key.Substring(0, kvp.Key.LastIndexOf(':'))] != null
                                            //must check parent to determine if this is a dict
                                            && RootDataField[kvp.Key.Substring(0, kvp.Key.LastIndexOf(':'))].Datatype is DictionaryType)
                                            {
                                                ParseSection(root, root.GetSection(kvp.Key), deletionValue);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    Console.WriteLine(".......Unhandled value" + kvp.Key + "=" + kvp.Value);
                                }
                                //ParseChildren(root, section);
                            }
                        }// delete
                    }

                }
                ///trailing dictionary
                if (LastDict != "")
                {
                    sb.AppendLine("},");
                    LastDict = "";
                }
                //TODO:handle lists here, and still somehow allow objects in lists

                ParseChildren(root, section, deletionValue);
                if (writeOuter)
                    sb.AppendLine("},");
            }
        }

        private void ParseChildren(IConfigurationRoot root, IConfigurationSection section, string deletionValue)
        {
            foreach (IConfigurationSection sect in section.GetChildren())
            {
                //this should only fail if a property does not have a supported datatype
                //the values will be output anyways, since they are included as members with values and not just sections
                //todo: warn if this happens
                if (RootDataField[sect.Path] != null)
                {
                    if (!(RootDataField[sect.Path].Datatype is ListType) && !(RootDataField[sect.Path].Datatype is DictionaryType))
                        ParseSection(root, sect, deletionValue);
                }
            }
        }

        public bool Commit(IConfigurationRoot root, IConfiguration configuration, DataField rootDataField, string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
