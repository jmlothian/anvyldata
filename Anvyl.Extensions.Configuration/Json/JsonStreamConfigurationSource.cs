//Modifed from: https://raw.githubusercontent.com/aspnet/Configuration/dev/src/Config.Json/JsonConfigurationSource.cs
//

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Anvyl.Extensions.Configuration.Json
{

    /// <summary>
    /// Represents a JSON file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class JsonStreamConfigurationSource : IConfigurationSource
    {
        public Stream stream { get; set; }
        /// <summary>
        /// Builds the <see cref="JsonStreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="JsonStreamConfigurationProvider"/></returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new JsonStreamConfigurationProvider(this);
        }
    }

}
