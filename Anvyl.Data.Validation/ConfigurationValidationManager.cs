using Anvyl.Data.Validation.Abstract;
using System;
using System.Collections.Generic;

namespace Anvyl.Data.Validation
{
    public class ConfigurationValidationManager
    {
        private Dictionary<string, IConfigurationValidator> Validators = new Dictionary<string, IConfigurationValidator>();
        public void RegisterValidator(IConfigurationValidator validator)
        {
            Validators[validator.FQN] = validator;
        }

        //run validation on a top level object, recurse sections, following in a Datafield to pull fqn
    }
}
