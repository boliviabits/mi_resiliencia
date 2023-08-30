# MiResiliencia

MiResiliencia is a web application for calculating the cost-benefit factor of protective structures in natural hazard areas. It was developed in 2018 for COSUDE Bolivia by [GEOTEST AG](https://www.geotest.ch) and adapted in 2023. It runs on Linux and Windows as an ASP.NET 6.0 web application.

MiResiliencia is developed under the Apache licence.


## Setup

MiResiliencia consists of three parts:
- WebApplication
- Spatial database (PostgreSQL / PostGIS)
- Geoserver

In order to install the application, the database and the geoserver must be installed first.

### Installation database

Download and install PostgreSQL from: https://www.postgresql.org/download
- Run the standard installation, including PostGIS package (https://postgis.net/documentation/getting_started/)
- Login to the database using psql in the CLI
	```
 	sudo -i
 	sudo su postgres
 	psql
 	```
- Create the user 'miresiliencia' (Please change and remember the password! The password is used hereafter.)
	```
	CREATE USER miresiliencia WITH ENCRYPTED PASSWORD 'yourpassword';
	```
- Create the database 'miresilienciadb'
	```
	CREATE DATABASE miresilienciadb OWNER miresiliencia;
	```
- Create a database schema
	```
	\c miresilienciadb
	CREATE SCHEMA public;
	```
- Grant privileges to user 'miresiliencia'
	```
	GRANT ALL ON SCHEMA public TO miresiliencia;

	GRANT ALL ON DATABASE miresilienciadb to miresiliencia;

	GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO miresiliencia;
	ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON tables TO miresiliencia;
	``` 
- Add PostGIS Extension to schema: Run "CREATE EXTENSION postgis;"
	```
 	SET SCHEMA 'public';
	CREATE EXTENSION postgis;
 	```
- Execute the SQL miresiliencia_initial.sql inside the miresiliencia schema [MiResiliencia-initial.sql](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/Setup/miresiliencia-initial.sql)
	```
 	-- in the linux command line
 
 	cd /tmp
	wget https://raw.githubusercontent.com/GEOTEST-AG/MiResiliencia/master/Setup/miresiliencia-initial.sql

 	-- back in psql
 
 	\c miresilienciadb
 	SET SCHEMA 'public';
 	\i /tmp/miresiliencia-initial.sql
	
 	```

### Geo-servidor (servidor cartográfico)

Download GeoServer version 2.23 from http://geoserver.org/download/
- Do the standard installation on port 8080
- Overwrite the data_dir directory with the provided and extracted [data.zip](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/Setup/data.zip)
- Login to Geoserver (http://localhost:8080) with standard Geoserver access (admin/geoserver). Edit Datastore -> miresilienciadb and fill in the db, server and password to the database
- In Security > Data, set the write permissions (*.w) to "Enable for each function".

### Use MiResiliencia Docker file to run MiResiliencia

1. copy [MiResiliencia/docker-compose.yml](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/MiResiliencia/docker-compose.yml) to server (e.g. user dir  $HOME) and change to this directory (e.g. cd )
2. Edit the DB (DBHost, DB, DBUser, DBPassword) and Geoserver host inside docker-compose.yml

3. start container(s) with
		```docker compose up -d ```

4. stop container(s) with 
		```docker compose down```

5. update container image(s)
		```
		docker compose down
		docker compose pull
		docker compose up -d
		```

Login to http://localhost:80. The default Login-Credentials are: ```admin / MiResiliencia23!```
