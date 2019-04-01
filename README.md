# prototype_server

![Coverage](./Docs/badges/badge_combined.svg)

## Database

#### Migrations

##### Add

```shell
dotnet ef migrations add [migration name]
```

##### Remove

```shell
dotnet ef migrations remove
```

##### Apply (Commit)

```shell
dotnet ef database update
```

##### Revert

```shell
dotnet ef database update [migration name]
```

## Tests

#### Run

```shell
dotnet test /p:AltCover=true /p:AltCoverXmlreport="bin/Debug/netcoreapp2.2/Reports/coverage.xml" /p:AltCoverAssemblyExcludeFilter="NUnit|Specs"  /p:AltCoverPathFilter=Libs|Migrations
```


#### Generate Coverage

```shell
dotnet reportgenerator -reports:bin/Debug/netcoreapp2.2/Reports/coverage.xml -targetdir:bin/Debug/netcoreapp2.2/Reports "-reporttypes:Html;Badges"
```


###### HTML Spec Report Path

```xpath
bin/Debug/netcoreapp2.2/Reports/specs-report.html
```


###### HTML Coverage Report Path

```xpath
bin/Debug/netcoreapp2.2/Reports/index.htm
```