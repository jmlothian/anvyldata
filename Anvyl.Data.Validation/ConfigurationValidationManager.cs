using Anvyl.Data.Validation.Abstract;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Anvyl.Data.Validation
{
    public class ValidationResponse : IValidationResponse
    {
        public bool Passed { get; set; } = true;

        public string ErrorMessage { get; set; } = "";

        public string ErrorCode { get; set; } = "";

        public string FQN { get; set; } = "null";
        public List<IValidationResponse> ChildResponses { get; set; } = new List<IValidationResponse>();

        public string Path { get; set; } = "";
    }
    public class ConfigurationValidationManager
    {
        private Dictionary<string, IConfigurationValidator> Validators = new Dictionary<string, IConfigurationValidator>();
        public void SetValidators(Dictionary<string, IConfigurationValidator> validators)
        {
            Validators = validators;
        }
        public void RegisterValidator(IConfigurationValidator validator)
        {
            Validators[validator.FQN] = validator;
        }

        //run validation on a top level object, recurse sections, following in a Datafield to pull fqn
        public IValidationResponse Validate(IConfigurationRoot configuration, DataField rootDataField, bool root = false)
        {
            IValidationResponse response = new ValidationResponse();
            response.Passed = true;
            response.FQN = rootDataField.Datatype.FQN;

            foreach (IConfigurationSection sect in configuration.GetChildren())
            {
                //match root datafields against config root children
                foreach (KeyValuePair<string, DataField> df in rootDataField.Members)
                {
                    if (df.Key == sect.Key)
                    {
                        var childresponse = Validate(sect, df.Value, root);
                        response.Passed = childresponse.Passed == false ? false : response.Passed;
                        response.ChildResponses.Add(childresponse);
                        break;
                    }
                }
            }
            return response;
        }
        public IValidationResponse Validate(IConfigurationSection configuration, DataField dataField, bool root = false)
        {
            IValidationResponse response = null;
            //validate
            foreach(KeyValuePair<string, IConfigurationValidator> validator in Validators)
            {
                if(validator.Key == dataField.Datatype.FQN)
                {
                    response = validator.Value.IsValid(configuration, configuration.Path);
                    response.Path = configuration.Path;
                }
            }
            if (response == null)
            {
                response = new ValidationResponse();
                response.Passed = true;
                response.FQN = dataField.Datatype.FQN;
            }
            foreach (IConfigurationSection config in configuration.GetChildren())
            {
                //only validate members that have a datafield
                if (dataField.Members.ContainsKey(config.Key))
                {
                    var childresponse = Validate(config, dataField.Members[config.Key]);
                    //chain up success state, failure overrides success
                    response.Passed = childresponse.Passed == false ? false : response.Passed;
                    response.ChildResponses.Add(childresponse);
                }
            }
            return response;
        }
    }
}
