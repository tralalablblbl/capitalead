# Capitalead

## Launch the project


1. You have to go into `src/main/resources/application.yml` file and fill all required fields

```
    lobstr-auth-token:
    nocrm-auth-token:
    nocrm-user-email:
```
```
    datasource:
        url:
        username:
        password:
```

2. To start the project, go to `com/zakupki/capitalead/CapitaleadApplication.java` and launch `main()` function.

## More details

The project contains a single script that runs itself once a day at 9:00 GMT+2 (it can be configured in `src/main/resources/application.yml`). You can run it manually via a GET
request `/api/v1/run`.

When project runs:
1. Script will check if processing list for each cluster exists. If such clusters with uncreated list exist, script will create processing lists for them in CRM.
2. When all processing lists are present, it starts loading data into each of them and cloud database.
3. Each processing list has tags: the name of the cluster, its id (required for the script to work), the source from which the cluster takes information.

Lobstr API docs: https://lobstrio.docs.apiary.io/# (open with VPN)
NoCRM API docs: https://www.nocrm.io/api/

## Add migration

to create new migrations:

in project folder execute:

    export ConnectionString__Default='Host=localhost;Database=capitalead;Port=5432;User Id=postgres;Password=postgres;Pooling=true;Timeout=30;Command Timeout=0;Internal Command Timeout=-1;Minimum Pool Size=1;Maximum Pool Size=50;Connection Idle Lifetime=300;Connection Pruning Interval=10;'
    dotnet ef migrations add {MigrationName}
