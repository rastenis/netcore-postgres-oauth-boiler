# netcore-postgres-oauth-boiler

A basic .NET Core website boilerplate using PostgreSQL for storage, adminer for db management, and Nginx for SSL certificates & routing.

<!-- [Demo website.](https://netcore.demos.mtr.lt) -->

## Features

-   TLS/HTTPS:
    -   Automatic certificate generation powered by Let's Encrypt
    -   Hosting modes:
        -   Self hosted mode (443/80 port access required)
        -   Simple mode (just the Dockerfile, http only), for use with reverse proxy configurations

## Configuration

The file `appsettingsExample.json` needs to be renamed to `appsettings.json` with replaced OAuth keys.

-   The process for obtaining a Google key is described [here](https://developers.google.com/identity/protocols/OAuth2).
-   The process for obtaining a Twitter key is described [here](https://developer.twitter.com/en/docs/basics/authentication/guides/access-tokens.html).

## Running the boilerplate

1. Standalone:

    Run `docker-compose -f docker-compose-windows.yml up` if you are on Windows, and `docker-compose -f docker-compose-linux.yml up` if you are on Linux. For an explanation of this separation, take a look at [Running on Windows](#running-on-windows).

2. Through Visual Studio:

    Run the docker-compose configuration.

### Running on Windows

Windows has an open issue with local volume mapping permissions, which results in PostgreSQL not being able to write to a (relative) local directory. Thus, when running on Windows (via run.sh), an external volume is created, which is managed by Docker.

### Information & Sources

Documentation for ASP.NET Core can be found here: [.NET Core docs.](https://docs.microsoft.com/aspnet/core)
Read about PostgreSQL here: [PostgreSQL.](https://www.postgresql.org/docs/12/tutorial-start.html)
Guide for Materialize UI: [Getting started with Materialize.](https://materializecss.com/getting-started.html)
C# Reference: [C# docs.](https://docs.microsoft.com/en-us/dotnet/csharp/)

### Contribution & Support

Submit bugs and requests through the project's issue tracker:

[![Issues](http://img.shields.io/github/issues/Scharkee/netcore-postgres-oauth-boiler.svg)](https://github.com/Scharkee/netcore-postgres-oauth-boiler/issues)

### License

This project is licensed under the terms of the MIT license.
