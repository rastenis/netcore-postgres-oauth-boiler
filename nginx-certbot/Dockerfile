FROM staticfloat/nginx-certbot
COPY *.conf /etc/nginx/conf.d/

RUN sed -i 's/acme-v01/acme-v02/g' /scripts/util.sh
