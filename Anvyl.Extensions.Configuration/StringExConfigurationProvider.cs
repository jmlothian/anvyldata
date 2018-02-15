using Anvyl.Data;
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
    public class StringExConfigurationProvider : JsonConfigurationProvider, IExConfigurationProvider
    {
        public StringExConfigurationProvider(JsonConfigurationSource source) : base(source) { }

        StringBuilder sb = new StringBuilder();

        public override string ToString()
        {
            return sb.ToString();
        }
        public bool Save(IConfigurationRoot root, IConfiguration configuration, DataField rootDataField,string deletionValue="**********[MARKED_FOR_DELETE]**********", string fileName = null)
        {
            bool success = true;
            if (fileName == null)
                fileName = Source.Path;
            try
            {
                sb.AppendLine("{");
                foreach (IConfigurationSection sect in configuration.GetChildren())
                {
                    ParseSection(sect);
                }
                sb.AppendLine("}");
                //File.WriteAllText(fileName, JObject.Parse(sb.ToString()).ToString());
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                success = false;
            }

            return success;
        }
        public void ParseSection(IConfigurationSection section)
        {
            IEnumerable<KeyValuePair<string, string>> props = section.AsEnumerable();
            bool writeOuter = section.GetChildren().Count() > 0;

            if (writeOuter)
                sb.AppendLine("\"" + section.Key + "\" : {");
            foreach (KeyValuePair<string, string> kvp in section.AsEnumerable())
            {
                //value is actually at this level, should have no extra :'s
                //value == null could be either a section or a value that is truly null.  Oh Well.
                if (kvp.Value != null && !kvp.Key.Replace(section.Path + ":", "").Contains(":"))
                {
                    //remove the path from the key
                    sb.AppendLine("\"" + kvp.Key.Replace(section.Path + ":", "") + "\" : \"" + kvp.Value + "\",");
                    //we rely on json.net to remove the trailing commas
                }
            }
            foreach(IConfigurationSection sect in section.GetChildren())
            {
                ParseSection(sect);
            }
            if(writeOuter)
                sb.AppendLine("},");
        }

        public bool Load(IConfigurationBuilder configBuilder, string connectionString)
        {
            throw new NotImplementedException();
        }

        public bool Commit(IConfigurationRoot root, IConfiguration configuration, DataField rootDataField, string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
