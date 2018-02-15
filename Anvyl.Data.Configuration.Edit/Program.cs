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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Anvyl.Data.Configuration.Edit
{
    public class Program
    {
        //lazy right now
        public static AssemblyReflector reflector = new AssemblyReflector();
        public static DataField rootDataField => reflector.rootDataField;

        public static IConfigurationRoot dotFileConfigurationRoot;
        public static IConfigurationBuilder dotFileConfigBuilder;

        public static IConfigurationRoot configuration;
        public static IConfigurationBuilder configurationBuilder;
        public static ConfigurationManager configManager = null;
        public static string HomeAppDataFolder = Environment.GetEnvironmentVariable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "LocalAppData" : "Home" );
        public static string dotFile => HomeAppDataFolder + @"\adce.ini";
        /*
        public static bool Validate()
        {
            //pass in with module
            var Validation = new ConfigurationValidationManager();
            Validation.RegisterValidator(new TestValidatorNullableInt32());
            IValidationResponse resp = Validation.Validate(configuration, reflector.rootDataField);
            return resp.Passed;
        }*/
        static void Main(string[] args)
        {
            if (!File.Exists(dotFile))
            {
                CreateDotFile();
            }

            //load adce config
            dotFileConfigBuilder = new ConfigurationBuilder().SetBasePath(HomeAppDataFolder);
            dotFileConfigBuilder.SetBasePath(HomeAppDataFolder);
            dotFileConfigBuilder.AddIniFile(dotFile);
            dotFileConfigurationRoot = dotFileConfigBuilder.Build();

            //load registered data types
            foreach (string s in dotFileConfigurationRoot["RegisteredTypesLibraries"].Split(','))
            {
                reflector.LoadRegisteredTypes(s);
            }

            /*
            reflector.RegisterTypeToDatatype(typeof(bool), new BoolType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(bool?), new NullableBoolType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(Int32?), new NullableInt32Type().Datatype);
            reflector.RegisterTypeToDatatype(typeof(Int32), new Int32Type().Datatype);
            reflector.RegisterTypeToDatatype(typeof(String), new StringType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(List<>), new ListType().Datatype);
            reflector.RegisterTypeToDatatype(typeof(Dictionary<,>), new DictionaryType().Datatype);
            */


            //load schema
            string [] asms = dotFileConfigurationRoot["SchemaLocation"].Split(";");
            foreach(string schemaFile in asms)
            {
                reflector.LoadSchemaAssembly(schemaFile);
            }

            //reflector.LoadAssembly(@"C:\Users\jeremy.lothian\OneDrive\Projects\Anvyl\Anvyl.Experimental.Library\bin\Debug\netcoreapp2.0\Anvyl.Experimental.Library.dll");
            if (string.IsNullOrEmpty(dotFileConfigurationRoot["ProviderLocation"]))
            {
                Console.Error.WriteLine("No provider specified.  Make sure to run Connect first and specify a provider location");
                return;
            }
            //load provider
            IExConfigurationProvider provider = reflector.LoadProvider(dotFileConfigurationRoot["ProviderLocation"], dotFileConfigurationRoot["ConnectionString"]);
            if (provider == null)
            {
                Console.Error.WriteLine("A provider could not be loaded.  Make sure to run Connect first and specify a provider location");
                return;
            }

            //set connection string
            configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
            if (dotFileConfigurationRoot["ConnectionString"] != "")
            {
                //load with provider
                provider.Load(configurationBuilder, dotFileConfigurationRoot["ConnectionString"]);
                //configurationBuilder.AddJsonFile(dotFileConfigurationRoot["ConnectionString"]);
            } else
            {
                Console.Error.WriteLine("No connection string specified.  Make sure to run Connect first and specify a connection string");
                return;
            }
            configuration = configurationBuilder.Build();

            //provider exists, load it!
            configManager = new ConfigurationManager(provider);

            //setup validation
            configManager.Validation.SetValidators(reflector.Validators);


            CommandLineApplication app = new CommandLineApplication();
            app.Name = "AnvylEdit";
            app.HelpOption("-?|-h|--help");
            app.Command("set", command =>
            {
                var validateOption = command.Option("-v|--validate",
                               "validate schema after setting",
                               CommandOptionType.NoValue);

                command.Description = "Set a configuration value for a key";
                command.HelpOption("-?|-h|--help");
                var keyArg = command.Argument("[key]", "Configuration key to set");
                var ValueArg = command.Argument("[value]", "Configuration value to set");

                command.OnExecute(() =>
                {
                    var key = keyArg.Value;
                    var value = ValueArg.Value;
                    if (string.IsNullOrEmpty(key)) { Console.Error.WriteLine("You must provide a key"); return; }
                    if (string.IsNullOrEmpty(value)) { Console.Error.WriteLine("You must provide a value"); return; }
                    if (configuration[key] == null)
                    {
                        Console.Error.WriteLine("Key does not exist in this configuration.");
                        return;
                    }
                    configuration[key] = value;
                    //validate
                    configManager.ValidateAndSave(validateOption, key, value, dotFileConfigurationRoot["ConnectionString"]);
                });
            });

            app.Command("show", command =>
            {
                command.Description = "Show configuration tree from key";
                command.HelpOption("-?|-h|--help");
                var keyArg = command.Argument("[key]", "Configuration key to show from");
                
                command.OnExecute(() =>
                {
                    var key = keyArg.Value;

                    if (string.IsNullOrEmpty(key)) { Console.WriteLine("You must provide a key"); return; }
                    if (configuration.GetSection(key) == null)
                    {
                        Console.Error.WriteLine("Key does not exist in this configuration.");
                        return;
                    }
                    StringExConfigurationProvider secp = new StringExConfigurationProvider(new JsonConfigurationSource());

                    secp.ParseSection(configuration.GetSection(key));
                    Console.WriteLine(JObject.Parse("{" + secp.ToString() + "}").ToString() );
                });
            });
            //we may just validate on edit...
            //app.Command("Validate", command =>
            //{

            //});
            app.Command("Commit", command =>
            {
                command.Description = "Commit changes to configuration";
                command.HelpOption("-?|-h|--help");
            });
            app.Command("Add", command =>
            {
                command.Description = "Add a new item to a list or dictionary";
                command.HelpOption("-?|-h|--help");
                var keyArg = command.Argument("[key]", "Configuration key for the list or dictionary with new key appended");
                //var dictKeyArg = command.Argument("[dictionary key]", "dictionary key to add");
                var ValueArg = command.Argument("[value]", "Configuration value to set - omit for creating objects");
                var validateOption = command.Option("-v|--validate",
                   "validate schema after setting",
                   CommandOptionType.NoValue);


                command.OnExecute(() =>
                {
                    var key = keyArg.Value;
                    var value = ValueArg.Value;
                    var dictKey = "";
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    if (string.IsNullOrEmpty(key)) { Console.Error.WriteLine("You must provide a key"); return; }
                    //if (string.IsNullOrEmpty(value)) { Console.Error.WriteLine("You must provide a value"); return; }
                    if (value == null) value = "";


                    /*
                    if (configuration[key] == null)
                    {
                        //determine if this is a dictionary
                        if (key.Contains(':'))
                        {
                            dictKey = key.Substring(key.LastIndexOf(':')).Trim(':');
                            var newkey = key.Substring(0, key.LastIndexOf(':')).Trim(':');
                            if(configuration[newkey] == null)
                            {
                                //determine if this exists but is empty
                                //list
                                if(!rootDataField.ContainsKey(key))
                                {
                                    Console.Error.WriteLine("Key not found -  ");
                                    //Console.Error.WriteLine(rootDataField.ToJson());
                                    //Console.Error.WriteLine(configuration.ToString());
                                }
                                if(rootDataField.ContainsKey(key) && rootDataField[key].Datatype is ListType)
                                {
                                    configManager.AddListItem(key, value, dict);
                                }
                                else if(rootDataField[newkey] != null && rootDataField[newkey].Datatype is DictionaryType) //dict
                                {
                                    configManager.AddDictionaryItem(dictKey, key, value, validateOption);
                                } else
                                {
                                    Console.Error.WriteLine("Key does not exist in this configuration.");
                                    //JsonExConfigurationProvider secp = new JsonExConfigurationProvider(new JsonConfigurationSource());
                                    //bool succ = secp.Save(configuration, configuration, rootDataField, "xpconfig-err.json");
                                    return;
                                }
                            }
                            else
                            {
                                if (rootDataField[newkey].Datatype.FQN != "csharp.dictionary<$1,$2>")
                                {
                                    Console.Error.WriteLine("Key does not point to a Dictionary object");
                                    return;
                                }
                                key = newkey;
                                //else, flow through
                            }

                        }
                        else
                        {
                            Console.Error.WriteLine("Key does not exist in this configuration.");
                            return;
                        }
                    }
                    */
                    //dict key is somewhat optional
                    if (rootDataField[key] == null)
                    {
                        Console.Error.WriteLine("The given key is not found in this configuration"); return;
                    }
                    else
                    {
                        if (rootDataField[key].Datatype.FQN == "csharp.dictionary<$1,$2>")
                        {
                            configManager.AddDictionaryItem(value, key, "", validateOption, dict);
                        }
                        else if (rootDataField[key].Datatype.FQN == "csharp.list<$1>")
                        {
                            configManager.AddListItem(key, value, dict);
                        }
                        else
                        {
                            Console.Error.WriteLine("The given key is not a list or dictionary"); return;
                        }
                        if (dict.Count() > 0)
                        {
                            configurationBuilder.AddInMemoryCollection(dict);
                            configuration = configurationBuilder.Build();
                            configManager.ValidateAndSave(validateOption, key, value, dotFileConfigurationRoot["ConnectionString"]);
                        }
                    }

                    if (!rootDataField.ContainsKey(key))
                    {
                        Console.Error.WriteLine("Key exists in this configuration but does not exist in class hierarchy");
                        return;
                    }


                });

            });

            //initialize settings, connect to config file/location
            app.Command("Connect", command =>
            {


                command.Description = "Add a new item to a list or dictionary";
                command.HelpOption("-?|-h|--help");
                var connectionOption = command.Option("-c|--connectionstr",
                   "Connection string passed to a config provider for loading/saving",
                    CommandOptionType.SingleValue);
                var providerOption = command.Option("-p|--provider",
                   "path to provider library for saving/loading configuration",
                    CommandOptionType.SingleValue);
                var schemaOption = command.Option("-s|--schema",
                   "comma seperated paths to class libraries for generating schema",
                    CommandOptionType.SingleValue);
                var registeredTypesOption = command.Option("-r|--register",
                   "comma seperated paths to registered types libraries for generating and validating schema",
                    CommandOptionType.SingleValue);
                var deletionOption = command.Option("-d|--delstr",
                   "deletion magic string used to indicate an object will be removed on save",
                    CommandOptionType.SingleValue);
                //load provider
                //connection string
                //set deletion string
                command.OnExecute(() =>
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    if (connectionOption.HasValue())
                    {
                        dict["ConnectionString"] = connectionOption.Value();
                    }
                    if (providerOption.HasValue())
                    {
                        dict["ProviderLocation"] = providerOption.Value();
                    }
                    if (schemaOption.HasValue())
                    {
                        dict["SchemaLocation"] = schemaOption.Value();
                    }
                    if (registeredTypesOption.HasValue())
                    {
                        dict["RegisteredTypesLibraries"] = registeredTypesOption.Value();
                    }
                    if (deletionOption.HasValue())
                    {
                        dict["DeletionString"] = deletionOption.Value();
                    }
                    dotFileConfigBuilder.AddInMemoryCollection(dict);
                    dotFileConfigurationRoot = dotFileConfigBuilder.Build();
                    SaveDotFile();

                    //fw.WriteLine("ProviderLocation=");
                    ////connection string to the current configuration, the provider is responsible for interpreting this
                    //fw.WriteLine("ConnectionString=");
                    ////schema from file or library
                    //fw.WriteLine("SchemaType=Library");
                    ////schema library, file or dll
                    ////should we allow multiple DLLs/files?  Not for now, keep it simple
                    //fw.WriteLine("SchemaLocation=");
                    ////root config key (if any)?
                    //fw.WriteLine("DeletionString=");
                    ////comma seperated list of additional existing types
                    //fw.WriteLine("RegisteredTypesLibraries=Anvyl.Data.dll");
                });
            });

            app.Command("Delete", command =>
            {
                command.Description = "Delete an item from a list or dictionary";
                command.HelpOption("-?|-h|--help");
                var keyArg = command.Argument("[key]", "Configuration key for the list or dictionary");
                var validateOption = command.Option("-v|--validate",
                    "validate schema after setting",
                    CommandOptionType.NoValue);
                var deletionOption = command.Option("-d|--delstring",
                    "deletion magic string used to indicate an object will be removed on save",
                    CommandOptionType.NoValue);
                command.OnExecute(() =>
                {
                    var key = keyArg.Value;
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    var delete = dotFileConfigurationRoot["ConnectionString"];
                    if (deletionOption.HasValue())
                        delete = deletionOption.Value();
                    configManager.DeleteItem(key, configuration, dict, delete);

                    if (dict.Count() > 0)
                    {
                        configurationBuilder.AddInMemoryCollection(dict);
                        configuration = configurationBuilder.Build();
                        configManager.ValidateAndSave(validateOption, key, null, dotFileConfigurationRoot["ConnectionString"]);
                    }
                });
            });

            app.ThrowOnUnexpectedArgument = false;
            try
            {
                app.Execute(args);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //foreach (string arg in args)
            //{
            //    Console.WriteLine(arg);
            //}
            /*
            Console.WriteLine("Anvyl.Data.Configuration.Edit v0.1");
            bool quit = false;
            while(!quit)
            {
                Console.Write("")
            }
            */
        }

        private static void CreateDotFile()
        {
            Console.WriteLine("Creating .adce ... " + dotFile);
            var fs = File.Create(dotFile);
            var fw = new StreamWriter(fs);
            //the current provider thats loaded
            fw.WriteLine("ProviderLocation=");
            //connection string to the current configuration, the provider is responsible for interpreting this
            fw.WriteLine("ConnectionString=");
            //schema from file or library
            fw.WriteLine("SchemaType=Library");
            //schema library, file or dll
            //should we allow multiple DLLs/files?  Not for now, keep it simple
            fw.WriteLine("SchemaLocation=");
            //root config key (if any)?
            fw.WriteLine("DeletionString=");
            //comma seperated list of additional existing types
            fw.WriteLine("RegisteredTypesLibraries=Anvyl.Data.dll");
            fw.Close();
            //output connection string?
        }
        private static void SaveDotFile()
        {
            var fs = File.Create(dotFile);
            var fw = new StreamWriter(fs);
            //the current provider thats loaded
            fw.WriteLine("ProviderLocation=" + dotFileConfigurationRoot["ProviderLocation"]);
            //connection string to the current configuration, the provider is responsible for interpreting this
            fw.WriteLine("ConnectionString=" + dotFileConfigurationRoot["ConnectionString"]);
            //schema from file or library
            fw.WriteLine("SchemaType=" + dotFileConfigurationRoot["SchemaType"]);
            //schema library, file or dll
            //should we allow multiple DLLs/files?  Not for now, keep it simple
            fw.WriteLine("SchemaLocation=" + dotFileConfigurationRoot["SchemaLocation"]);
            //root config key (if any)?
            fw.WriteLine("DeletionString=" + dotFileConfigurationRoot["DeletionString"]);
            //comma seperated list of additional existing types
            fw.WriteLine("RegisteredTypesLibraries=" + dotFileConfigurationRoot["RegisteredTypesLibraries"]);
            fw.Close();
            //output connection string?
        }

    }
}
