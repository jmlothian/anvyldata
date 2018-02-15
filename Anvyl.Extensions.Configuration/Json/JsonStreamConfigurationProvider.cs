// Modified from: https://raw.githubusercontent.com/aspnet/Configuration/dev/src/Config.Json/JsonConfigurationProvider.cs
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Linq;

namespace Anvyl.Extensions.Configuration.Json
{


    /// <summary>
    /// A JSON file based <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public class JsonStreamConfigurationProvider : ConfigurationProvider
    {
        public JsonStreamConfigurationSource Source { get; }
        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public JsonStreamConfigurationProvider(JsonStreamConfigurationSource source)
        //: base(source)
        {
            Source = source;
        }


        /// <summary>
        /// Loads the JSON data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public void Load(Stream stream)
        {
            try
            {
                Data = JsonConfigurationStreamParser.Parse(stream);
            }
            catch (JsonReaderException e)
            {
                string errorLine = string.Empty;
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    IEnumerable<string> streamContent;
                    using (var streamReader = new StreamReader(stream))
                    {
                        streamContent = ReadLines(streamReader);
                        errorLine = RetrieveErrorContext(e, streamContent);
                    }
                }
                throw new Exception("json parse exception");
            }
        }

        private static string RetrieveErrorContext(JsonReaderException e, IEnumerable<string> streamContent)
        {
            string errorLine = null;
            if (e.LineNumber >= 2)
            {
                var errorContext = streamContent.Skip(e.LineNumber - 2).Take(2).ToList();
                // Handle situations when the line number reported is out of bounds
                if (errorContext.Count() >= 2)
                {
                    errorLine = errorContext[0].Trim() + Environment.NewLine + errorContext[1].Trim();
                }
            }
            if (string.IsNullOrEmpty(errorLine))
            {
                var possibleLineContent = streamContent.Skip(e.LineNumber - 1).FirstOrDefault();
                errorLine = possibleLineContent ?? string.Empty;
            }
            return errorLine;
        }

        private static IEnumerable<string> ReadLines(StreamReader streamReader)
        {
            string line;
            do
            {
                line = streamReader.ReadLine();
                yield return line;
            } while (line != null);
        }







        public override void Load()
        {
            try
            {
                var stream = Source.stream;

                // stream could be null if the request to remote config failed
                // if so, parser will throw an exception... which is what we want/need
                Data = JsonConfigurationStreamParser.Parse(stream);

                // because we're not using a 'using' - do we need to manually dispose?
                stream.Dispose();
            }
            catch (Exception ex)
            {
                // [otherwise] swallow the exception and just default
                Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            throw new NotImplementedException();
        }
    }

}
