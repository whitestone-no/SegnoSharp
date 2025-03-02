When adding new migrations, remember to specify the Database type in command line argument, as well as which context to migrate. These must match.
Here are some common examples:

```
Add-Migration Initial -Project MySQL -Args '--Database:Type mysql'
Add-Migration Initial -Project PostgreSQL -Args '--Database:Type postgresql'
Add-Migration Initial -Project MSSQL -Args '--Database:Type mssql'
Add-Migration Initial -Project SQLite -Args '--Database:Type sqlite'

Remove-Migration -Project MySQL -Args '--Database:Type mysql'
Remove-Migration -Project PostgreSQL -Args '--Database:Type postgresql'
Remove-Migration -Project MSSQL -Args '--Database:Type mssql'
Remove-Migration -Project SQLite -Args '--Database:Type sqlite'
```

To manually run the `Update-Database` command for SQLite, you also need to specify the path to the data folder, relative to the "SegnoSharp" project folder, otherwise
EF Core will try to create the database inside the "SegnoSharp" project folder, which is not wanted:
```
Update-Database -Project MySQL -Args '--Database:Type mysql'
Update-Database -Project PostgreSQL -Args '--Database:Type postgresql'
Update-Database -Project MSSQL -Args '--Database:Type mssql'
Update-Database -Project SQLite -Args '--Database:Type sqlite --CommonConfig:DataPath ..\..\data'
```

MySQL tables are created with the MyISAM engine by default.
The InnoDB engine is required, but Pomelo.EntityFrameworkCore.MySql does not support setting the default engine,
so you need to manually set the engine in every generated migration file at the start of the `Up()` method:

```
migrationBuilder.Sql("SET default_storage_engine=INNODB");
```