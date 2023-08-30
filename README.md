# MiResiliencia

MiResiliencia is a web application for calculating the cost-benefit factor of protective structures in natural hazard areas. It was developed in 2018 for COSUDE Bolivia by [GEOTEST AG](https://www.geotest.ch) and adapted in 2023. It runs on Linux and Windows as an ASP.NET 6.0 web application.

MiResiliencia is developed under the Apache licence.


## Setup

MiResiliencia consists of three parts:
- WebApplication
- Spatial database (PostgreSQL / PostGIS)
- Geoserver

In order to install the application, the database and the geoserver must be installed first.

### Spatial database
In the best case you use your own PostgreSQL installation and add a new database. 
Otherwise, install PostgreSQL on a server.
- Download and install PostgreSQL from: https://www.postgresql.org/download
- Run the standard installation, including PostGIS package
 
#### Setup database ####
- Login to postgresql using psql in the CLI
	```
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
- Add PostGIS Extension to schema: 
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

Download GeoServer stable version (e.g. 2.23.2 for the moment) from http://geoserver.org/download/
- Do the standard installation on port 8080
- Overwrite the data_dir directory with the provided and extracted [data.zip](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/Setup/data.zip)
- Login to Geoserver (http://localhost:8080) with standard Geoserver access (admin/geoserver). Edit Datastore -> miresilienciadb and fill in the db, server and password to the database
- In Security > Data, set the write permissions (*.w) to "Enable for each function".

### Use MiResiliencia Docker file to run MiResiliencia

1. Install docker https://docs.docker.com/engine/install/

2. copy [MiResiliencia/docker-compose.yml](https://github.com/GEOTEST-AG/MiResiliencia/blob/master/MiResiliencia/docker-compose.yml) to server (e.g. user dir  $HOME) and change to this directory (e.g. cd )
	```
 	cd 
	wget https://github.com/GEOTEST-AG/MiResiliencia/blob/master/MiResiliencia/docker-compose.yml
	```
3. Edit the DB (DBHost = `your.postgresdb.url`, DB = `miresilienciadb`, DBUser = `miresiliencia`, DBPassword = `yourpassword`) and Geoserver host inside `docker-compose.yml`

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

Login to http://localhost:8080. The default Login-Credentials are: ```admin / MiResiliencia23!```
