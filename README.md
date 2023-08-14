# MiResiliencia

MiResiliencia is a web application for calculating the cost-benefit factor of protective structures in natural hazard areas. It was developed in 2018 for COSUDE Bolivia by [GEOTEST AG](https://www.geotest.ch) and adapted in 2023. It runs on Linux and Windows as an APS.NET Core 6 web application.

MiResiliencia is developed under the Apache licence


## Setup

MiResiliencia consists of three parts:
- WebApplication
- Spatial database (PostgreSQL / PostGIS)
- Geoserver

In order to install the application, the database and the geoserver must be installed first.

### Installation database

Download version 12 from; https://www.postgresql.org/download
- Run the standard installation, including PostGIS package
- Login to the database using pqAdmin
- Create the user miresiliencia (the password is used hereafter)
- Create a database schema
- Add PostGIS Extension to schema: Run "CREATE EXTENSION postgis;"
- Execute the SQL miresiliencia_initial.sql inside the miresiliencia schema [MiResiliencia-initial.sql](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/Setup/miresiliencia-initial.sql)

### Geo-servidor (servidor cartográfico)

Download GeoServer version 2.23 from http://geoserver.org/download/
- Do the standard installation on port 8080
- Overwrite the data_dir directory with the provided and extracted [data.zip](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/Setup/data.zip)
- Login to Geoserver (http://localhost:8080) with standard Geoserver access (admin/geoserver). Edit Datastore -> mireslienciadb and fill in the db, server and password to the database
- In Security > Data, set the write permissions (*.w) to "Enable for each function".

### Use MiResiliencia Docker file to run MiResiliencia

1. copy [MiResiliencia/docker-compose.yml](https://github.com/GEOTEST-AG/MiResiliencia/docker-compose.yml) to server (e.g. user dir  $HOME) and change to this directory (e.g. cd )
2. Edit the DB and Geoserver host inside docker-compose.yml

3. start container(s) with
		docker compose up -d 

4. stop container(s) with 
		docker compose down

5. update container image(s)
		docker compose pull

Login to http://localhost:80. The default Login-Credentials are: admin / MiResiliencia23!
