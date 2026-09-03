ARG POSTGRES_IMAGE=postgres:18.6-alpine3.24
FROM ${POSTGRES_IMAGE}

COPY init/10-create-managed-client-roles.sh /docker-entrypoint-initdb.d/10-create-managed-client-roles.sh

RUN sed -i 's/\r$//' /docker-entrypoint-initdb.d/10-create-managed-client-roles.sh \
    && chmod 0555 /docker-entrypoint-initdb.d/10-create-managed-client-roles.sh