using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                       System.AttributeTargets.Struct)]
    public class ConfigurationName : System.Attribute
    {
        public string Name;
        public ConfigurationName(string name)
        {
            Name = name;
        }
    }
}
