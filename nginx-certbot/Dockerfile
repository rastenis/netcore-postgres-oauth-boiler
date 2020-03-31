FROM staticfloat/nginx-certbot
COPY *.conf /etc/nginx/conf.d/

ARG DOMAIN

RUN sed -i "s/DOMAIN/${DOMAIN}/g" /etc/nginx/conf.d/boiler.conf
RUN sed -i 's/acme-v01/acme-v02/g' /scripts/util.sh
