#!/bin/sh

if [ $# -eq 0 ]
  then
    echo "No Let's Encrypt email supplied. Aborting..."
    exit 1
fi

CERTBOT_EMAIL=$1 docker-compose up -d
