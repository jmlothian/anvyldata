using Anvyl.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Extensions.Configuration
{
    public interface IExConfigurationProviderFactory
    {
        IExConfigurationProvider Create(string connectionString);
    }
    public interface IExConfigurationProvider : IConfigurationProvider
    {
        //null filename might assume one already exists (ie, file provider)
        bool Save(IConfigurationRoot root, IConfiguration configuration, DataField rootDataField, string deletionString= "**********[MARKED_FOR_DELETE]**********", string connectionString= null);

        //we don't actually know what extension method on configBuilder to call at runtime, so we pass it in
        bool Load(IConfigurationBuilder configBuilder, string connectionString);
        //If this config system has something beyond simple saving/load, such as versioning, that can be done here
        bool Commit(IConfigurationRoot root, IConfiguration configuration, DataField rootDataField, string connectionString);
    }
}
