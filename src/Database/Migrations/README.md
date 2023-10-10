When adding new migrations, remember to specify the Database type in command line argument, as well as which context to migrate. These must match.
Here are some common examples:

```
Add-Migration Initial -Project MySQL -Args '--Database:Type mysql'
Add-Migration Initial -Project SQLite -Args '--Database:Type sqlite'

Remove-Migration -Project MySQL -Args '--Database:Type mysql'
Remove-Migration -Project SQLite -Args '--Database:Type sqlite'
```

To manually run the `Update-Database` command for SQLite, you also need to specify the path to the data folder, relative to the "SegnoSharp" project folder, otherwise
EF Core will try to create the database inside the "SegnoSharp" project folder, which is not wanted:
```
Update-Database -Project MySQL -Args '--Database:Type mysql'
Update-Database -Project SQLite -Args '--Database:Type sqlite --CommonConfig:DataPath ..\..\data'
```