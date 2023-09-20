# Geoserver setup in existing geoserver installation

1. Define a new workspace ``miresiliencia``

    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/60b84504-cc20-4aae-8669-2b48ec7a6e8e)

2. Then make sure that the newly created workspace is enabled and WFS is selected

    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/f30232ab-f508-4b5b-a148-175ab82f01be)

3. Create a new store (PostGIS Database)
    - workspace: ``miresiliencia``
    - data source name: ``miresilienciadb``
    - host: ``[your database host]``
    - database: ``miresilienciadb``
    - user: ``miresiliencia``
    - passwd: ``[your password]``

    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/d6f75f3e-45f0-4cbf-afed-7ffaad5fdf78)

4. Set rights for workspace miresiliencia for all roles to *.w (under Security -> Data)
    - workspace: ``miresiliencia``
    - Layer and groups: ``*``
    - Access mode: ``Write``
    - Selected Roles: ``[select all]``

    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/4dea89e0-9970-4330-9bd3-e815bf08903c)
    
    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/65a7aee5-81ce-4abc-8e11-c28f883e57e2)

6. Create a new layer, select miresilienciadb as source and select the view ``EditableProjects``
    - Native and Declared SRS:  ``EPSG:3857``

    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/12aa3ac5-a9dc-4128-89a7-116a56b9b337)

    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/93350513-07b9-4803-ae3b-49773983e19f)

7. Repeat the last step for the following views
    - (EditableProjects)
    - ErrorView 			
    - Intensity 			
    - MappedObject 				
    - MappedObjectWithResilience 	
    - MappedObjectsView 			
    - Project 					
    - ProtectionMeasure 
  
   
    ![grafik](https://github.com/GEOTEST-AG/MiResiliencia/assets/68429061/2640c5a5-0afb-415a-a54d-39248b6c4dae)
