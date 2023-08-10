When adding new migrations, remember to specify the Database type in command line argument, as well as which context to migrate. These must match.
Here are some common examples:

```
Add-Migration Initial -Context SegnoSharpMysqlDbContext -OutputDir Migrations\MySQL -Args '--Database:Type mysql'
Add-Migration Initial -Context SegnoSharpSqliteDbContext -OutputDir Migrations\SQLite -Args '--Database:Type sqlite'

Remove-Migration -Context SegnoSharpMysqlDbContext -Args '--Database:Type mysql'
Remove-Migration -Context SegnoSharpSqliteDbContext -Args '--Database:Type sqlite'
```

The `-OutputDir` should only be used for the very first migration, as the subsequent migrations are siblings of the very first one, which EF Tools remembers.
https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers

To manually run the `Update-Database` command for SQLite, you also need to specify the path to the data folder, relative to the "SegnoSharp" project folder, otherwise
EF Core will try to create the database inside the "SegnoSharp" project folder, which is not wanted:
```
Update-Database -Context SegnoSharpSqliteDbContext -Args '--Database:Type sqlite --CommonConfig:DataPath ..\..\data'
```