#https://hub.docker.com/_/microsoft-mssql-server
FROM mcr.microsoft.com/mssql/server:2019-latest

USER root

RUN apt-get update

RUN mkdir /var/opt/mssql/backup
WORKDIR /var/opt/mssql/backup

RUN wget https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2019.bak -O AdventureWorks2019.bak -q
COPY install.sql .
COPY install.sh .

# Set environment variables
ENV MSSQL_SA_PASSWORD=Password#123
ENV ACCEPT_EULA=Y

# Wait for Sql Server to start and then run install.sql
RUN ( /opt/mssql/bin/sqlservr & ) \
    | grep -q "Service Broker manager has started" \
    && bash install.sh \
    && pkill sqlservr
