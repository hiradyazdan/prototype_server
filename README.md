# prototype_server

![Coverage](./Docs/badges/badge_combined.svg)

## Database

#### Create/Add Migration

```shell
dotnet ef migrations add [migration name]
```

#### Update Database (Commit)

```shell
dotnet ef database update
```


## Tests

#### Run

```shell
dotnet test /p:AltCover=true /p:AltCoverXmlreport="bin/Debug/netcoreapp2.2/Reports/coverage.xml" /p:AltCoverAssemblyFilter="NUnit|Specs"
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