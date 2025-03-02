using System.Runtime.CompilerServices;
using System;

namespace Whitestone.SegnoSharp.Database
{
    public static class DbContextInitializaer
    {
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            // DateTime is not properly supported in PostgreSQL, so we need to enable legacy timestamp behavior
            // This shouldn't have any impact on other databases
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
