# netcore-postgres-oauth-boiler

A basic .NET Core website boilerplate using PostgreSQL for storage, adminer for db management, Let's Encrypt for SSL certificates and Nginx for routing.

[Demo website.](https://netcore.demos.matasr.com)

## Features

-   Vanilla .NET Core Server Setup:
    -   Razor pages, upholstered with the Materialize css toolkit
    -   Server -> client data rendering demo
    -   Native Entity Framework database interface
    -   Asynchronous design
    -   Auth gated route examples
-   User authentication via:
    -   Regular email/password
    -   Google
    -   Github
    -   Reddit
-   Auth method merging, linking and unlinking of social auth accounts
-   TLS/HTTPS:
    -   Automatic certificate generation powered by Let's Encrypt
    -   Hosting modes:
        -   Self hosted mode (443/80 port access required)
        -   Simple mode (just the Dockerfile, http only), for use with reverse proxy configurations

## Configuration

1.  Open the docker-compose file you're going to use (depends on your platform) and set `CERTBOT_EMAIL` to your email for Let's Encrypt certificate generation. Moreover, set `DOMAIN` to your domain name. The domain should point to the IP of the machine you're running this project on.
2.  The file `appsettingsExample.json` needs to be renamed to `appsettings.json` with your own OAuth keys:

-   The process for obtaining a Google key is described [here](https://developers.google.com/identity/protocols/OAuth2).
-   The method to create a Github app in order to get an API key is described [here](https://developer.github.com/apps/building-oauth-apps/creating-an-oauth-app/).
-   The process for creating a Reddit app in order to get an API key is described [here](https://github.com/reddit-archive/reddit/wiki/OAuth2#getting-started).

## Running the boilerplate

-   Standalone:

```bash
# clone the repo
$ git clone https://github.com/Scharkee/netcore-postgres-oauth-boiler.git
$ cd netcore-postgres-oauth-boiler
# perform configuration...

# generate TLS certificates and run on ports 80/443
# choose between docker-compose-linux.yml and docker-compose-windows.yml
$ docker-compose -f docker-compose-linux.yml up
```

For an explanation of the docker-compose file separation, take a look at [Running on Windows](#running-on-windows).

-   Through Visual Studio:

1. Launch Visual Studio
2. Right-click on the `docker-compose` section in the Solution Explorer, and click `Set as Startup Project`
3. Select either Debug or Release at the top and click the `Docker Compose` button to run.

### Running the boilerplate independently

If you're behind Nginx or a similar reverse proxy setup, you can either:

1. Adjust the compose file so it no longer contains the Nginx container
2. Run only the boilerplate (you will have to run PostgreSQL separately):
    - Adjust the `DefaultConnection` in appsettings.json in accordance with your database
    - run `docker build . --tag boiler`
    - run `docker run boiler -p 3000:80 --name boiler`

### Running on Windows

Docker on Windows has an open issue with local volume mapping permissions, which results in PostgreSQL not being able to write to a (relative) local directory. Thus, when running on Windows (via run.sh), an external volume is created, which is managed by Docker.

Moreover, if you encounter a PR_END_OF_FILE_ERROR when trying to load the website, try executing:

```
$ docker exec nginx bash -c "mv /etc/nginx/conf.d/boiler.conf{.nokey,} ; nginx -s reload"
```

This seems to be a symlink issue with Windows Docker containers as well. After renaming the file once, it does not need to be touched anymore (unless you purge the nginx container).

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
