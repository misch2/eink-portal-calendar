Run the application

If it's the first time run on an older DB it will throw an exception saying:

> SQLite Error 1: 'table "cache" already exists'.

Run this SQL to fix it:
```sql
INSERT INTO __EFMigrationsHistory VALUES ('20260208135310_InitialSqlite', '8.0.0')
```

then run the app again.

It should run the remaining migrations and display something like this:
```text
[info] Applying migration '20260208140607_RemoveMojoMigrations'. [Microsoft.EntityFrameworkCore.Migrations[20402]]
[info] Applying migration '20260214150117_UpdateModelNamespace'. [Microsoft.EntityFrameworkCore.Migrations[20402]]
[info] Applying migration '20260214150850_AddThemesTable'. [Microsoft.EntityFrameworkCore.Migrations[20402]]
[info] Applying migration '20260214151933_AddThemesContent'. [Microsoft.EntityFrameworkCore.Migrations[20402]]
[info] Applying migration '20260214152116_UpdateConfigWithThemeIds'. [Microsoft.EntityFrameworkCore.Migrations[20402]]
[info] Applying migration '20260214205636_AddThemeIdToDisplay'. [Microsoft.EntityFrameworkCore.Migrations[20402]]
```

