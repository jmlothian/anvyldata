using Microsoft.Extensions.Configuration;
using System;

namespace Anvyl.Data.Validation.Abstract
{
    public interface IConfigurationValidator
    {
        string FQN { get; }
        //should we pass in a DataField?
        IValidationResponse IsValid(IConfiguration configuration, string Path);
    }
}
