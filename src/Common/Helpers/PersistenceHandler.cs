using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models.PersistenceManager;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Common.Helpers
{
    public class PersistenceHandler(
        IDbContextFactory<SegnoSharpDbContext> dbContextFactory,
        ILogger<PersistenceHandler> logger) : BackgroundService, IPersistenceManager
    {
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

            foreach (PersistenceEntry key in configKeys)
            {
                await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

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
                               ((objValue != null && objValue.ToString()?.Replace(',', '.') != e.Value?.Replace(',', '.')) ||
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
                            entry.Value = entry.PropertyInfo.GetValue(persistence)?.ToString();
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (persistenceEntries.Any())
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
            if (key.PropertyInfo.PropertyType == typeof(string))
            {
                key.PropertyInfo.SetValue(configuration, value);
            }
            else if (key.PropertyInfo.PropertyType == typeof(bool))
            {
                key.PropertyInfo.SetValue(configuration, bool.Parse(value));
            }
            else if (key.PropertyInfo.PropertyType == typeof(int))
            {
                key.PropertyInfo.SetValue(configuration, int.Parse(value));
            }
            else if (key.PropertyInfo.PropertyType == typeof(float))
            {
                key.PropertyInfo.SetValue(configuration, float.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (key.PropertyInfo.PropertyType == typeof(double))
            {
                key.PropertyInfo.SetValue(configuration, double.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (key.PropertyInfo.PropertyType == typeof(decimal))
            {
                key.PropertyInfo.SetValue(configuration, decimal.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (key.PropertyInfo.PropertyType == typeof(ushort))
            {
                key.PropertyInfo.SetValue(configuration, ushort.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (key.PropertyInfo.PropertyType == typeof(DateTime))
            {
                key.PropertyInfo.SetValue(configuration,
                    DateTime.ParseExact(value, "s", CultureInfo.InvariantCulture));
            }
            else throw new ArgumentException("Unsupported configuration parameter type");
        }
    }
}
