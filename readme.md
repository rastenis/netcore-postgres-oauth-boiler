# netcore-postgres-oauth-boiler

A basic .NET Core website boilerplate using PostgreSQL for storage, adminer for db management, and Nginx for SSL certificates & routing.

<!-- [Demo website.](https://netcore.demos.mtr.lt) -->

## Features

- TLS/HTTPS:
  - Automatic certificate generation powered by Let's Encrypt
  - Hosting modes:
    - Self hosted mode (443/80 port access required)
    - Simple mode (just the Dockerfile, http only), for use with reverse proxy configurations

## Running on Windows

Windows has an open issue with local volume mapping permissions, which results in PostgreSQL not being able to write to a (relative) local directory. Thus, when running on Windows (via run.sh), an external volume is created, which is managed by Docker.

## Installation

[WIP]
