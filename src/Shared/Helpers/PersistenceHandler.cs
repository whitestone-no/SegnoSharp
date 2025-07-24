using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.PersistenceManager;

namespace Whitestone.SegnoSharp.Shared.Helpers
{
    public class PersistenceHandler(
        IDbContextFactory<SegnoSharpDbContext> dbContextFactory,
        ILogger<PersistenceHandler> logger) : BackgroundService, IPersistenceManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly List<PersistenceEntry> _entries = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    IEnumerable<object> objects;
                    lock (_entries)
                    {
                        objects = _entries.Select(e => e.Owner).Distinct();
                    }

                    foreach (object @object in objects)
                    {
                        await WriteAsync(@object);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Task is cancelled. Ignore this and just pass it on to TPL.
                throw;
            }
            catch (Exception e)
            {
                // We don't need an additional message as the exception message will also be logged.
                // Use "" instead of string.Empty as the latter is not a compile time constant
                logger.LogError(e, "");
            }
        }

        public void Register(object persistence)
        {
            AsyncHelper.RunSync(async () => await RegisterAsync(persistence));
        }

        public async Task RegisterAsync(object persistence)
        {
            List<PersistenceEntry> entries = [];

            lock (persistence)
            {
                Type type = persistence.GetType();

                // Find all persistence properties and their persist attributes
                foreach (PropertyInfo property in type.GetProperties())
                {
                    var persistEntryFound = false;
                    DefaultValueAttribute defaultValue = null;

                    foreach (object attribute in property.GetCustomAttributes(true))
                    {
                        if (attribute is DefaultValueAttribute defaultValueAttribute)
                        {
                            defaultValue = defaultValueAttribute;
                        }

                        if (attribute is PersistAttribute)
                        {
                            persistEntryFound = true;
                        }
                    }

                    // Check if all necessary attributes were found for the property to be a valid persistence entry
                    if (!persistEntryFound)
                    {
                        continue;
                    }

                    // If no [DefaultValue] attribute was found, set the actual value of the persistence entry as default value
                    defaultValue ??= new DefaultValueAttribute
                    {
                        DefaultValue = property.GetValue(persistence)
                    };

                    // Add the valid persistence entry to the list of entries
                    entries.Add(new PersistenceEntry
                    {
                        DefaultValue = defaultValue,
                        Key = type.FullName + "." + property.Name,
                        Owner = persistence,
                        PropertyInfo = property,
                        PropertyType = property.PropertyType
                    });
                }

                // Copy the contents of the temporary local persistence entries to the global list of entries
                entries.ForEach(entry => _entries.Add(entry));
            }

            if (entries.Count > 0)
            {
                await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

                foreach (PersistenceEntry entry in entries)
                {
                    // Save to DB
                    PersistenceManagerEntry dbEntry = await dbContext.PersistenceManagerEntries.FindAsync(entry.Key);

                    if (dbEntry != null)
                    {
                        // Only update the database entry if the underlying object type is changed
                        if (dbEntry.DataType == entry.PropertyType.ToString())
                        {
                            continue;
                        }

                        dbEntry.DataType = entry.PropertyType.ToString();
                        dbEntry.Value = entry.DefaultValueString;
                    }
                    else
                    {
                        // This entry is not in the database, so add it
                        dbEntry = new PersistenceManagerEntry
                        {
                            Key = entry.Key,
                            DataType = entry.PropertyType.ToString(),
                            Value = entry.DefaultValueString
                        };

                        await dbContext.PersistenceManagerEntries.AddAsync(dbEntry);
                    }
                }

                await dbContext.SaveChangesAsync();
            }

            // Reread the actual values from the DB and put into the persistence entry
            await ReadAsync(persistence);
        }

        private async Task ReadAsync(object persistence)
        {
            List<PersistenceEntry> configKeys;

            lock (persistence)
            {
                configKeys = _entries.Where(e => e.Owner == persistence).ToList();
            }

            await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

            foreach (PersistenceEntry key in configKeys)
            {
                PersistenceManagerEntry dbEntry = await dbContext.PersistenceManagerEntries.FindAsync(key.Key);

                if (dbEntry != null && key.PropertyInfo != null && key.PropertyInfo.CanWrite)
                {
                    lock (persistence)
                    {
                        SetValue(key, dbEntry.Value, persistence);
                    }

                    key.Value = dbEntry.Value;
                }
            }
        }

        private async Task WriteAsync(object persistence)
        {
            PersistenceEntry[] persistenceEntries;

            lock (persistence)
            {
                // Get changed configuration entries
                persistenceEntries = _entries
                    .Where(e =>
                    {
                        if (e.Owner != persistence)
                        {
                            return false;
                        }

                        object objValue = e.PropertyInfo.GetValue(persistence);
                        return e.Owner == persistence &&
                               ((objValue != null && JsonSerializer.Serialize(objValue, JsonOptions) != e.Value) ||
                                (objValue == null && e.Value != null));
                    })
                    .ToArray();

                if (persistenceEntries.Length == 0) return;

                try
                {
                    // Map data from object to our configuration
                    foreach (PersistenceEntry entry in persistenceEntries)
                    {
                        lock (entry.Owner)
                        {
                            // Update the value to be stored in the database
                            entry.Value = JsonSerializer.Serialize(entry.PropertyInfo.GetValue(persistence), JsonOptions);
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (persistenceEntries.Length > 0)
            {
                await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

                foreach (PersistenceEntry entry in persistenceEntries)
                {
                    PersistenceManagerEntry dbEntry = await dbContext.PersistenceManagerEntries.FindAsync(entry.Key);
                    if (dbEntry != null)
                    {
                        dbEntry.Value = entry.Value;
                    }
                    else
                    {
                        await dbContext.PersistenceManagerEntries.AddAsync(new PersistenceManagerEntry
                        {
                            Key = entry.Key,
                            DataType = entry.PropertyType.ToString(),
                            Value = entry.Value
                        });
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private static void SetValue(PersistenceEntry key, string value, object configuration)
        {
            // Fix to handle old PersistenceHandler entries that were stored as `.ToString()`
            // These values were not valid JSON as strings were stored without quotes,
            // and booleans to stored as `True` or `False`.
            // Remove this fix in a future version when probability of all installations have
            // been updated to the new format.
            if (key.PropertyInfo.PropertyType == typeof(string) ||
                key.PropertyInfo.PropertyType.IsEnum)
            {
                if (string.IsNullOrEmpty(value))
                {
                    value = "\"\"";
                }
                else
                {
                    if (!value.StartsWith('"'))
                    {
                        value = "\"" + value;
                    }

                    if (!value.EndsWith('"'))
                    {
                        value = value + "\"";
                    }
                }
            }

            if (key.PropertyInfo.PropertyType == typeof(bool))
            {
                value = value.ToLowerInvariant();
            }
            // End of fix

            object obj = JsonSerializer.Deserialize(value, key.PropertyInfo.PropertyType, JsonOptions);
            if (obj != null)
            {
                key.PropertyInfo.SetValue(configuration, obj);
            }
        }
    }
}
