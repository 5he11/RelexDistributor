using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CryptoDistributor
{
    public class ConfigurationProvider
    {
        private static Lazy<IConfiguration> config = new Lazy<IConfiguration>(
            () => new ConfigurationBuilder()
#if DEBUG
                .SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
#endif
                .AddEnvironmentVariables()
                .Build(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static string GetSetting(string key)
        {
#if DEBUG
            var v = config.Value[$"Values:{key}"];
            return v ?? config.Value[key];
#else
            return config.Value[key];
#endif
        }
    }
}
