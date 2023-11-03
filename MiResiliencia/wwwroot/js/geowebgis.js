(function (window) {
    'use strict';

    function define_GeoWebGIS() {
        var GeoWebGIS = {
            Options: {
                Bounds: [609681, 267328, 610178, 267775],
                Extent: [420000, 30000, 900000, 350000],
                Resolutions: [4000, 3750, 3500, 3250, 3000, 2750, 2500, 2250, 2000, 1750, 1500, 1250, 1000, 750, 650, 500, 250, 100, 50, 20, 10, 5, 2.5, 2, 1.5, 1, 0.5, 0.25],
                Center: [600000, 200000],
                isInternational: false,
            },
            drawlayer: null,
            geojsonlayerCluster: null,
            projection: null,
            map: null,
            matrixIds: [],
            attributions: null,
            applicationdefinition: null,
            backgroudmap: null,
            satmap: null,
            backgroudimagery: null,
            backgroudosm: null,
            backgroundesri: null,
            backgroundesriImage: null,
            bingmap: null,
            geojsonlayer: null,
            geologielayer: null,
            customLayers: [],
            loadstate: 1,
            draw_interaction: null,
            clusterselect: null,
            showLabels: false,
            wfslayer: null,
            wfssource: null,
            projectslayer: null,
            projectssource: null,
            drawsource: null,
            geolocation: null,
            positionFeature: null,
            positionLayer: null,
            workingNamespace: '',
            interactionSelect: null,
            lastInsertedFeatureId: null,
            clickedcoord: null,
            measureTooltipElement: null,
            measureTooltip: null,
            sketch: null,
            listener: null,
            popup: null,
            progress: null,
            translator: null,
            legendLayers: [],
            isDrawing: false
        };


        GeoWebGIS.initialize = function (options) {

            proj4.defs("EPSG:21781", "+proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +k_0=1 +x_0=600000 +y_0=200000 +ellps=bessel +towgs84=674.4,15.1,405.3,0,0,0,0 +units=m +no_defs");

            options = options || {};

            for (var i = 0; i < GeoWebGIS.Options.Resolutions.length; i++) {
                this.matrixIds.push(i);
            }

            //GeoWebGIS.translator = new GeoWebGISLanguage("en");
            GeoWebGIS.translator = new GeoWebGISLanguage("en");
        }




        /**
         * Function: createMap
         *
         * Function that creates the map with two layers: basemap (swisstopo wmts) and the mapguide layer
         *
         * Return: 
         * nothing
         */

        GeoWebGIS.createMap = function (islogin) {
            var self = this;

            this.progress = new Progress(document.getElementById('progress'));


            var urlAddon = '';
            this.projection = ol.proj.get('EPSG:3857');

            this.backgroudosm = new ol.layer.Tile({
                source: new ol.source.OSM({
                }),
                opacity: 0.6
            });

            /*this.backgroudimagery = new ol.layer.Tile({
                source: new ol.source.XYZ({
                    url: 'https://atlas.microsoft.com/map/tile?subscription-key=z-ehgM0XS8-dfBEeY1guLd4YiO02xNyZ6n7Ni5I5FNo&api-version=2.0&tilesetId=microsoft.imagery&zoom={z}&x={x}&y={y}&tileSize=256&language=en-US',
                    attributions: ''
                }),
                opacity: 0.8
            });*/

            this.backgroudimagery = new ol.layer.Tile({
                source: new ol.source.BingMaps({
                    key: 'AimcXg9FM3tvlLlm3DJlO7kw_8QRFCJI5BkRA0IWJQP-Y5wtZJGJw81C-YuTcMMF',
                    imagerySet: 'AerialWithLabels',
                    maxZoom: 19
                }),
                opacity: 0.8
            }); 

            this.backgroundesri = new ol.layer.Tile({
                source: new ol.source.XYZ({
                    attributions:
                        'Tiles © <a href="https://services.arcgisonline.com/ArcGIS/' +
                        'rest/services/World_Topo_Map/MapServer">ArcGIS</a>',
                    url:
                        'https://server.arcgisonline.com/ArcGIS/rest/services/' +
                        'World_Topo_Map/MapServer/tile/{z}/{y}/{x}',
                }),
                opacity: 0.8
            }); 

            this.backgroundesriImage = new ol.layer.Tile({
                source: new ol.source.XYZ({
                    attributions:
                        'Tiles © <a href="https://services.arcgisonline.com/ArcGIS/' +
                        'rest/services/World_Imagery/MapServer">ArcGIS</a>',
                    url:
                        'https://server.arcgisonline.com/ArcGIS/rest/services/' +
                        'World_Imagery/MapServer/tile/{z}/{y}/{x}',
                }),
                opacity: 0.8
            }); 
            

            // create a vector layer used for editing
            this.drawlayer = new ol.layer.Vector({
                name: 'drawlayer',
                source: new ol.source.Vector(),
                style: new ol.style.Style({
                    fill: new ol.style.Fill({
                        color: 'rgba(255, 255, 255, 0)'
                    }),
                    stroke: new ol.style.Stroke({
                        color: '#ffcc33',
                        width: 2
                    }),
                    image: new ol.style.Circle({
                        radius: 7,
                        fill: new ol.style.Fill({
                            color: '#ffcc33'
                        })
                    })
                })
            });

            var getText = function (feature) {
                var text = feature.get('name');
                return text;
            };

            var createTextStyle = function (feature) {
                if (!GeoWebGIS.showLabels) return null;

                var offsetX = 0;
                var offsetY = 0;

                if (feature.getGeometry().getType() == 'Point') {
                    offsetX = 12;
                    offsetY = -12;
                }
                var normalfont = '12px Calibri,sans-serif';

                return new ol.style.Text({
                    font: normalfont,
                    align: 'Start',
                    offsetX: offsetX,
                    offsetY: offsetY,
                    text: getText(feature),
                    fill: new ol.style.Fill({
                        color: '#000'
                    }),
                    stroke: new ol.style.Stroke({
                        color: '#fff',
                        width: 3
                    })
                });
            };


            function getColor(feature) {
                // Do coloring effects like this
                if (feature.get('zustand') == 3) return 'red';
                else return 'blue';
            }

            function polygonStyleFunction(feature) {
                return new ol.style.Style({
                    stroke: new ol.style.Stroke({
                        color: getColor(feature),
                        width: 5
                    }),
                    fill: new ol.style.Fill({
                        color: 'rgba(0,0,255, 0.1)'
                    }),
                    text: createTextStyle(feature)
                });
            }

            function lineStyleFunction(feature) {
                return new ol.style.Style({
                    stroke: new ol.style.Stroke({
                        color: getColor(feature),
                        width: 3
                    }),
                    fill: new ol.style.Fill({
                        color: 'rgba(0,0,255, 0.1)'
                    }),
                    text: createTextStyle(feature)
                });
            }

            function pointStyleFunction(feature) {
                return new ol.style.Style({
                    image: new ol.style.Circle({
                        radius: 5,
                        fill: new ol.style.Fill({
                            color: 'rgba(0,0,255, 0.1)'
                        }),
                        stroke: new ol.style.Stroke({ color: getColor(feature), width: 3 })

                    }),
                    text: createTextStyle(feature)
                });
            }



            var styleFunctionFeldApp = function (feature) {
                if (feature.getGeometry().getType() == 'Point') return pointStyleFunction(feature);
                if (feature.getGeometry().getType() == 'LineString') return lineStyleFunction(feature);
                if (feature.getGeometry().getType() == 'Polygon') return polygonStyleFunction(feature);

            };

            var styleFunctionGeneral = styleFunctionFeldApp;

            // measure
            /**
       * Format length output.
       * @param {ol.geom.LineString} line The line.
       * @return {string} The formatted length.
       */

            var formatLength = function (line) {
                var length = ol.sphere.getLength(line);
                var output;
                if (length > 100) {
                    output = (Math.round(length / 1000 * 100) / 100) +
                        ' ' + 'km';
                } else {
                    output = (Math.round(length * 100) / 100) +
                        ' ' + 'm';
                }
                return output;
            };
            /**
       * Format area output.
       * @param {ol.geom.Polygon} polygon The polygon.
       * @return {string} Formatted area.
       */

            var formatArea = function (polygon) {
                var area = ol.sphere.getArea(polygon);
                var output;
                if (area > 10000) {
                    output = (Math.round(area / 1000000 * 100) / 100) +
                        ' ' + 'km<sup>2</sup>';
                } else {
                    output = (Math.round(area * 100) / 100) +
                        ' ' + 'm<sup>2</sup>';
                }
                return output;
            };


            /**
             * Creates a new measure tooltip
             */
            function createMeasureTooltip() {
                if (GeoWebGIS.measureTooltipElement) {
                    GeoWebGIS.measureTooltipElement.parentNode.removeChild(GeoWebGIS.measureTooltipElement);
                }
                GeoWebGIS.measureTooltipElement = document.createElement('div');
                GeoWebGIS.measureTooltipElement.className = 'ol-tooltip ol-tooltip-measure';
                GeoWebGIS.measureTooltip = new ol.Overlay({
                    element: GeoWebGIS.measureTooltipElement,
                    offset: [0, -15],
                    positioning: 'bottom-center'
                });
                GeoWebGIS.map.addOverlay(GeoWebGIS.measureTooltip);
            }


            // GPS
            this.geolocation = new ol.Geolocation({
                projection: this.projection
            });

            this.positionFeature = new ol.Feature();
            this.positionFeature.setStyle(new ol.style.Style({
                image: new ol.style.Circle({
                    radius: 6,
                    fill: new ol.style.Fill({
                        color: '#3399CC'
                    }),
                    stroke: new ol.style.Stroke({
                        color: '#fff',
                        width: 2
                    })
                })
            }));
            this.geolocation.on('change:position', function () {
                var self = this;
                var coordinates = GeoWebGIS.geolocation.getPosition();
                GeoWebGIS.positionFeature.setGeometry(coordinates ?
                    new ol.geom.Point(coordinates) : null);
                GeoWebGIS.map.getView().setCenter(coordinates);
            });
            this.positionLayer = new ol.layer.Vector({
                source: new ol.source.Vector({
                    features: [this.positionFeature]
                })
            });


            var gj;

            var wfsurl = "";
            var formatWFS = new ol.format.WFS();


            this.wfssource = new ol.source.Vector({
                format: new ol.format.GeoJSON(),
                url: function (extent) {
                    if (!islogin) {
                        wfsurl = '/proxy/miresiliencia/ows?service=WFS&version=2.0.0&request=GetFeature&typeName=miresiliencia:EditableProjects&cql_filter=CompanyID={CompanyID}&outputFormat=application/json';
                    }
                    return wfsurl;
                }
            });

            this.wfssource.on('tileloadstart', function () {
                progress.addLoading();
            });

            this.wfssource.on('tileloadend', function () {
                progress.addLoaded();
            });
            this.wfssource.on('tileloaderror', function () {
                progress.addLoaded();
            });


            this.wfslayer = new ol.layer.Vector({
                source: this.wfssource,
                id: 'wfslayer',
                style: new ol.style.Style({
                    stroke: new ol.style.Stroke({
                        color: 'rgba(0, 0, 255, 1.0)',
                        width: 2
                    }),
                    image: new ol.style.Circle({
                        radius: 5,
                        fill: new ol.style.Fill({
                            color: 'rgba(0,0,255, 0.0)'
                        }),
                        stroke: new ol.style.Stroke({ color: 'rgba(0, 0, 255, 0.5)', width: 1 })

                    })
                }),

            });

            this.popup = new ol.Overlay({
                element: document.getElementById('popup')
            });


            if (!islogin) {
                var listenerKey = this.wfssource.on('change', function (e) {
                    var self = this;
                    if (GeoWebGIS.wfssource.getState() == 'ready') {

                        var features = this.getFeatures();



                        if (features.length > 0) GeoWebGIS.fit(GeoWebGIS.wfslayer.getSource().getExtent());
                        ol.Observable.unByKey(listenerKey);
                    }
                });

                var myView;
                myView = new ol.View({
                    center: [-7584999.190795, -1861394.512801],
                    zoom: 13
                })
                var scaleLineControl = new ol.control.ScaleLine();

                this.map = new ol.Map({
                    layers: [this.backgroudimagery, this.backgroudosm, this.backgroundesri, this.backgroundesriImage, this.wfslayer, this.positionLayer, this.drawlayer],
                    target: 'map',

                    view: myView,
                    controls: ol.control.defaults({
                        attributionOptions: ({
                            collapsible: false
                        })
                    }).extend([
                        new ol.control.ScaleLine()
                    ]),
                    logo: false
                });

                this.map.addOverlay(this.popup);
                //this.map.addControl(new ol.control.LoadingPanel());

            }
            // Not logged in, show standard map
            else {
                this.map = new ol.Map({
                    layers: [this.backgroudosm],
                    target: 'map',

                    view: myView,
                    controls: ol.control.defaults({
                        attributionOptions: ({
                            collapsible: false
                        })
                    }),
                    logo: false
                });

                var polyCoords = [];
                var coords = "-66.18500141,-17.32590673 -66.186213,-17.32279273 -66.1872382,-17.31878894 -66.18798379,-17.31514096 -66.18817019,-17.31336144 -66.18855464,-17.30960213 -66.18706345,-17.30426334 -66.18366167,-17.29069322 -66.18235687,-17.27805653 -66.17434172,-17.26773296 -66.17322332,-17.25616275 -66.16390338,-17.25278055 -66.15104185,-17.25171248 -66.14358589,-17.2600789 -66.14060351,-17.26630897 -66.13482514,-17.27538669 -66.13202916,-17.28037035 -66.14489068,-17.29229499 -66.15551542,-17.29727819 -66.17070694,-17.30786704 -66.17247773,-17.31649784 -66.18279957,-17.32495029 -66.18401116,-17.32628486 -66.18500141,-17.32590673".split(' ');
                for (var i in coords) {
                    var c = coords[i].split(',');
                    polyCoords.push(ol.proj.transform([parseFloat(c[0]), parseFloat(c[1])], "WGS84", "EPSG:900913"));
                }
                var extent = new ol.geom.Polygon([polyCoords]);

                GeoWebGIS.map.getView().fit(extent.getExtent(), GeoWebGIS.map.getSize());
            }

            this.clusterselect = new ol.interaction.Select({
                condition: ol.events.condition.never,
                layers: [this.wfslayer],
                multi: true
            });

            // GeoWebGIS.clusterselect.getFeatures().push(feature);

            this.map.on('singleclick', function (e) {
                if (GeoWebGIS.workingNamespace == 'MappedObject') {
                    if (e.originalEvent.altKey != true) {
                        GeoWebGIS.clusterselect.getFeatures().clear();
                    }
                }
                else {
                    GeoWebGIS.clusterselect.getFeatures().clear();
                }
                GeoWebGIS.map.forEachFeatureAtPixel(e.pixel, function (f) {

                    GeoWebGIS.clusterselect.getFeatures().push(f);
                    GeoWebGIS.clusterselect.dispatchEvent({
                        type: 'select',
                        selected: GeoWebGIS.clusterselect.getFeatures(),
                        deselected: []
                    });
                });
                if (GeoWebGIS.clusterselect.getFeatures().getArray().length==0) {
                    var element = GeoWebGIS.popup.getElement();
                    $(element).popover('dispose');
                }
            });

            this.map.addInteraction(this.clusterselect);

            this.clusterselect.on('select', function (e) {
                console.log("Selected");
                var isLoadingTable = false;
                if (e.target.getFeatures().getLength() > 0) {
                    var firstfeature = e.target.getFeatures().getArray();

                    if (firstfeature.length == 1) {
                        var feature = firstfeature[0];
                        var layer = feature.getLayer(self.map);

                        var currentwfsurl = null;
                        if (layer != null) currentwfsurl = layer.getSource().getUrl();
                        if (currentwfsurl == null) currentwfsurl = "";
                        if (GeoWebGIS.wfsurl == null) GeoWebGIS.wfsurl = "";
                        // show popup?
                        if ((currentwfsurl.toString().indexOf("miresiliencia:ErrorView") >= 0) || (GeoWebGIS.wfsurl.indexOf("miresiliencia:ErrorView") >= 0)) {
                            var element = GeoWebGIS.popup.getElement();
                            $(element).popover('dispose');
                            var coordinate = firstfeature[0].getGeometry().getFirstCoordinate();

                            var extent = firstfeature[0].getGeometry().getExtent();
                            var middlepint = ol.extent.getCenter(extent);
                            var coord = ol.proj.transform(middlepint, 'EPSG:900913', 'EPSG:4326');

                            GeoWebGIS.popup.setPosition(coordinate);
                            // the keys are quoted to prevent renaming in ADVANCED mode.
                            $(element).popover({
                                'placement': 'top',
                                'animation': false,
                                'html': true,
                                'content': '<p>' + GeoWebGIS.translator.get("errorwas") + ':</p><b>' + firstfeature[0].getProperties()["Log"] + '</b></p>'
                                    //+ '<a href="#" onclick="reloadErrorViewMappedObject(' + firstfeature[0].getProperties()["ID"] + ', ' + coord[0] + ', ' + coord[1] + '); ">' + GeoWebGIS.translator.get("change") + '</a><div id="mappedObjectDivErrorView"></div>'
                            });
                            $(element).popover('show');
                        }
                        else {
                            var element = GeoWebGIS.popup.getElement();
                            $(element).popover('dispose');
                        }


                        // clicked on a project perimeter
                        if ((currentwfsurl.toString().indexOf("miresiliencia:EditableProjects") >= 0) || (GeoWebGIS.wfsurl.indexOf("miresiliencia:EditableProjects") >= 0)) {
                            // not editable
                            if (isLoadingTable == false) {
                                var idlist = "";
                                e.target.getFeatures().forEach(function (entry) {
                                    idlist += entry.getProperties()["ProjectID"] + ",";
                                });
                                $('#pthide').html('<div style="text-align:center; width:100%;">' + GeoWebGIS.translator.get("Wait") + ' <i class="fa fa-spinner fa-spin" style="font-size:25px;text-align:center;margin-top:50px;"></i></div >');
                                $('#pthide').show();
                                $('#projectTableDiv').load("/" + urlAddon + "Project/ProjectTable?idlist=" + idlist);
                                isLoadingTable = true;
                            }

                            //$('#pthide').show();
                        }

                        else if (GeoWebGIS.workingNamespace == 'MappedObject') {
                            var extent = firstfeature[0].getGeometry().getExtent();
                            var middlepint = ol.extent.getCenter(extent);
                            var coord = ol.proj.transform(middlepint, 'EPSG:900913', 'EPSG:4326');
                            if (firstfeature[0].getProperties()["ID"] !== undefined)
                                reloadMappedObject(firstfeature[0].getProperties()["ID"], coord[0], coord[1]);

                        }
                        else if (GeoWebGIS.workingNamespace == 'Resilience') {
                            reloadRMid(firstfeature[0].getProperties()["mappedObjectID"]);

                        }
                        else if (GeoWebGIS.workingNamespace == 'ResilienceCopy') {
                            copyResilience(firstfeature[0].getProperties()["mappedObjectID"]);
                        }
                        else if (GeoWebGIS.workingNamespace == 'Intensity') {
                            if (!GeoWebGIS.isDrawing) {
                                if (firstfeature[0].getProperties()["BeforeAction"] == true) {
                                    $("#NatHazardIKBefore").val(firstfeature[0].getProperties()["NatHazardID"]);
                                    $("#IKClassesBefore").val(firstfeature[0].getProperties()["IKClassesID"]);
                                    $("#IntensityDegreeBefore").val(firstfeature[0].getProperties()["IntensityDegree"]);
                                    changeIKDrawSettings();
                                }
                                else {

                                    $("#NatHazardIKAfter").val(firstfeature[0].getProperties()["NatHazardID"]);
                                    $("#IKClassesAfter").val(firstfeature[0].getProperties()["IKClassesID"]);
                                    $("#IntensityDegreeAfter").val(firstfeature[0].getProperties()["IntensityDegree"]);
                                    changeIKDrawSettingsAfter();
                                }
                            }

                        }
                    }
                    // Multiple Select
                    else {
                        if (GeoWebGIS.workingNamespace == 'MappedObject') {
                            var extent = firstfeature[0].getGeometry().getExtent();
                            var middlepint = ol.extent.getCenter(extent);
                            var coord = ol.proj.transform(middlepint, 'EPSG:900913', 'EPSG:4326');

                            var ids = firstfeature.map(id => id.getProperties()["ID"]);
                            console.log(ids);

                            reloadMultipleMappedObject(ids);

                        }
                    }


                    e.selected.forEach(function (feature) {




                    });

                }
                else {

                    var element = GeoWebGIS.popup.getElement();
                    $(element).popover('dispose');
                    var currentwfsurl = GeoWebGIS.wfslayer.getSource().getUrl();
                    // we deselected everything
                    if ((currentwfsurl.toString().indexOf("miresiliencia:EditableProjects") >= 0) || (GeoWebGIS.wfsurl.indexOf("miresiliencia:EditableProjects") >= 0)) {
                        $('#projectTableDiv').load("/" + urlAddon + "Project/ProjectTable/");

                    }
                }
            });

            function getLayer(id) {
                for (var i = 0, numLayers = GeoWebGIS.customLayers.length; i < numLayers; i++) {
                    if (GeoWebGIS.customLayers[i].id == id) {
                        return GeoWebGIS.customLayers[i].layer;
                    }
                }
            }

            GeoWebGIS.getLayer = function (id) {
                for (var i = 0, numLayers = GeoWebGIS.customLayers.length; i < numLayers; i++) {
                    if (GeoWebGIS.customLayers[i].id == id) {
                        return GeoWebGIS.customLayers[i].layer;
                    }
                }
            }

            GeoWebGIS.fit = function (extent) {
                var mySize = GeoWebGIS.map.getSize();

                GeoWebGIS.map.getView().fit(extent, {
                    size: mySize,
                    passing: [0, 0, 0, $('.olsidebar-content').width()]
                });
                GeoWebGIS.map.getView().centerOn(GeoWebGIS.map.getView().getCenter(), mySize, [20 + (mySize[0] / 2) + $('.olsidebar-content').width() / 2, mySize[1] / 2]);

            }


            GeoWebGIS.addDrawInteraction = function (geomType, mode, featureId, natgef, ikclass, isbefore, degree, projectid) {
                //remove other interactions
                var dirty = {};
                var self = this;
                this.isDrawing = true;
                // clear the map
                this.drawlayer = null;
                this.drawlayer = new ol.layer.Vector({
                    name: 'drawlayer',
                    source: this.drawsource,
                    style: new ol.style.Style({
                        stroke: new ol.style.Stroke({
                            color: 'rgba(100,100,100, 0.01)',
                            width: 5
                        }),
                        image: new ol.style.Circle({
                            radius: 5,
                            fill: new ol.style.Fill({
                                color: 'rgba(255,0,0, 0.01)'
                            }),
                            stroke: new ol.style.Stroke({ color: 'rgba(255,0,0, 0.01)', width: 5 })

                        }),
                        fill: new ol.style.Fill({
                            color: 'rgba(255, 255, 255, 0.01)'
                        })
                    })
                });

                GeoWebGIS.map.removeLayer(self.drawlayer);
                GeoWebGIS.map.addLayer(self.drawlayer);



                if (this.clusterselect) {
                    this.clusterselect.getFeatures().clear();
                }

                this.map.removeInteraction(this.clusterselect);

                if (mode == "Draw") {

                    // create the interaction
                    this.draw_interaction = null;
                    this.draw_interaction = new ol.interaction.Draw({
                        source: this.drawlayer.getSource(),
                        type: /** @type {ol.geom.GeometryType} */ geomType
                    });


                    this.draw_interaction.on('drawstart',
                        function (evt) {
                            createMeasureTooltip();
                            // set sketch
                            GeoWebGIS.sketch = evt.feature;

                            /** @type {ol.Coordinate|undefined} */
                            var tooltipCoord = evt.coordinate;

                            GeoWebGIS.listener = GeoWebGIS.sketch.getGeometry().on('change', function (evt) {
                                var geom = evt.target;
                                var output;
                                if (geom instanceof ol.geom.Polygon) {
                                    output = formatArea(geom);
                                    tooltipCoord = geom.getInteriorPoint().getCoordinates();
                                } else if (geom instanceof ol.geom.LineString) {
                                    output = formatLength(geom);
                                    tooltipCoord = geom.getLastCoordinate();
                                }
                                GeoWebGIS.measureTooltipElement.innerHTML = output;
                                GeoWebGIS.measureTooltip.setPosition(tooltipCoord);
                            });
                        }, this);


                    // add it to the map
                    this.map.addInteraction(this.draw_interaction);
                    this.draw_interaction.on('drawend', function (e) {

                        GeoWebGIS.map.removeOverlay(GeoWebGIS.measureTooltip);
                        ol.Observable.unByKey(GeoWebGIS.listener);

                        if (featureId > 0) {
                            var ffeatures = self.drawlayer.getSource().getFeatures();
                            // we have multipolygon

                            if ((ffeatures.length > 0) && (ffeatures[0].getGeometry() != null) && (ffeatures[0].getGeometry().getType() == "MultiPolygon")) {

                                ffeatures[0].getGeometry().appendPolygon(e.feature.getGeometry().getPolygons()[0]);


                                var parser = new jsts.io.OL3Parser();

                                // convert the OpenLayers geometry to a JSTS geometry
                                var jstsGeom = parser.read(e.feature.getGeometry().getPolygons()[0]);

                                if (jstsGeom.isValid()) {
                                    self.saveData('update', ffeatures[0]);
                                }
                                else {
                                    BootstrapDialog.show({
                                        type: BootstrapDialog.TYPE_DANGER,
                                        title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                        message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/content/themes/invalid_polygon.png" width="560" />',
                                        buttons: [{
                                            label: 'OK',
                                            action: function (dialogItself) {
                                                dialogItself.close();
                                            }
                                        }]
                                    });
                                }


                                GeoWebGIS.endDrawInteraction

                            }
                            else {
                                e.feature.setId(featureId);
                                var parser = new jsts.io.OL3Parser();
                                // convert the OpenLayers geometry to a JSTS geometry
                                var jstsGeom = parser.read(e.feature.getGeometry());

                                if (jstsGeom.isValid()) {
                                    self.saveData('update', e.feature);
                                }
                                else {
                                    BootstrapDialog.show({
                                        type: BootstrapDialog.TYPE_DANGER,
                                        title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                        message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/content/themes/invalid_polygon.png" width="560" />',
                                        buttons: [{
                                            label: 'OK',
                                            action: function (dialogItself) {
                                                dialogItself.close();
                                            }
                                        }]
                                    });
                                }

                            }
                        }
                        else {
                            if (projectid != 0) {
                                if (GeoWebGIS.workingNamespace == 'Intensity') {
                                    e.feature.setProperties({ 'IKClassesID': ikclass, 'NatHazardID': natgef, 'BeforeAction': isbefore, 'ProjectId': projectid, 'IntensityDegree': degree });

                                    var parser = new jsts.io.OL3Parser();

                                    // convert the OpenLayers geometry to a JSTS geometry
                                    var jstsGeom = parser.read(e.feature.getGeometry());

                                    if (jstsGeom.isValid()) {
                                        self.saveData('insert', e.feature);
                                    }
                                    else {
                                        BootstrapDialog.show({
                                            type: BootstrapDialog.TYPE_DANGER,
                                            title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                            message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/content/themes/invalid_polygon.png" width="560" />',
                                            buttons: [{
                                                label: 'OK',
                                                action: function (dialogItself) {
                                                    dialogItself.close();
                                                }
                                            }]
                                        });

                                    }

                                    //self.map.removeInteraction(self.draw_interaction);
                                }
                                else if (GeoWebGIS.workingNamespace == 'ProtectionMeasure') {
                                    e.feature.setProperties({
                                        'Costs': 0, 'LifeSpan': 0, 'OperatingCosts': 0, 'MaintenanceCosts': 0, 'RateOfReturn': 0, 'ProjectID': projectid
                                    });

                                    var parser = new jsts.io.OL3Parser();

                                    // convert the OpenLayers geometry to a JSTS geometry
                                    var jstsGeom = parser.read(e.feature.getGeometry());

                                    if (jstsGeom.isValid()) {
                                        self.saveData('insert', e.feature);
                                    }
                                    else {
                                        BootstrapDialog.show({
                                            type: BootstrapDialog.TYPE_DANGER,
                                            title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                            message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/content/themes/invalid_polygon.png" width="560" />',
                                            buttons: [{
                                                label: 'OK',
                                                action: function (dialogItself) {
                                                    dialogItself.close();
                                                }
                                            }]
                                        });

                                    }

                                }


                                else if (GeoWebGIS.workingNamespace == 'MappedObject') {
                                    e.feature.setProperties({
                                        'ObjectparameterID': $('#landuseselector').find(":selected").val(), 'ProjectId': projectid
                                    });
                                    var parser = new jsts.io.OL3Parser();
                                    // convert the OpenLayers geometry to a JSTS geometry
                                    var jstsGeom = parser.read(e.feature.getGeometry());

                                    if (jstsGeom.isValid()) {
                                        self.saveData('insert', e.feature);
                                    }
                                    else {
                                        BootstrapDialog.show({
                                            type: BootstrapDialog.TYPE_DANGER,
                                            title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                            message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/content/themes/invalid_polygon.png" width="560" />',
                                            buttons: [{
                                                label: 'OK', action: function (dialogItself) {
                                                    dialogItself.close();
                                                }
                                            }]
                                        });

                                    }


                                }
                            }
                        }
                    });

                    var snap = new ol.interaction.Snap({
                        source: this.wfssource
                    });
                    this.map.addInteraction(snap);
                }
                else if (mode == "Modify") {

                    var interactionSelectPointerMove = new ol.interaction.Select({

                    });

                    var modifystyles = [
                        /* We are using two different styles for the polygons:
                         *  - The first style is for the polygons themselves.
                         *  - The second style is to draw the vertices of the polygons.
                         *    In a custom `geometry` function the vertices of a polygon are
                         *    returned as `MultiPoint` geometry, which will be used to render
                         *    the style.
                         */
                        new ol.style.Style({
                            stroke: new ol.style.Stroke({
                                color: 'rgba(255,0,0, 1)',
                                width: 5
                            }),
                            image: new ol.style.Circle({
                                radius: 5,
                                fill: new ol.style.Fill({
                                    color: 'rgba(255,0,0, 1)'
                                }),

                            }),
                            fill: new ol.style.Fill({
                                color: 'rgba(255, 255, 255, 0.5)'
                            })
                        }),
                        new ol.style.Style({
                            image: new ol.style.Circle({
                                radius: 7,
                                fill: new ol.style.Fill({
                                    color: 'red',
                                }),
                            }),
                            geometry: function (feature) {

                                console.log(feature.getGeometry().getCoordinates());
                                // return the coordinates of the first ring of the polygon
                                //const coordinates = sqlcolumn != 'polygon' ? feature.getGeometry().getCoordinates() : feature.getGeometry().getCoordinates()[0];


                                const coordinates = feature.getGeometry().getCoordinates();
                                if (Array.isArray(coordinates[0])) return new ol.geom.MultiPoint(coordinates[0]);
                                return new ol.geom.MultiPoint(coordinates);
                            },
                        })];

                    this.interactionSelect = new ol.interaction.Select({
                        condition: ol.events.condition.click,
                        layers: [this.drawlayer],
                        style: function () {
                            return modifystyles;

                        }
                    });
                    this.interactionSelect.getFeatures().clear();
                    this.map.removeInteraction(this.interactionSelect);
                    //this.map.addInteraction(interactionSelectPointerMove);
                    this.map.addInteraction(this.interactionSelect);

                    // create the interaction
                    this.draw_interaction = null;

                    // if there is only one feature, modify this
                    this.draw_interaction = new ol.interaction.Modify({
                        features: this.interactionSelect.getFeatures()
                    });
                    var self = this;
                    this.drawsource.once('change', function (e) {
                        var selectedfeatures;
                        if (GeoWebGIS.drawsource.getFeatures().length == 1) {
                            selectedfeatures = GeoWebGIS.drawsource.getFeatures()[0];
                            var features = self.interactionSelect.getFeatures();
                            // now you have an ol.Collection of features that you can add features to
                            features.push(selectedfeatures);
                        }
                    });

                    // add it to the map
                    this.map.addInteraction(this.draw_interaction);

                    this.interactionSelect.getFeatures().on('add', function (e) {
                        e.element.on('change', function (e) {
                            dirty[e.target.getId()] = true;
                        });
                    });
                    this.interactionSelect.getFeatures().on('remove', function (e) {
                        var f = e.element;
                        if (dirty[f.getId()]) {
                            var parser = new jsts.io.OL3Parser();

                            var jstsGeom = parser.read(f.getGeometry());

                            if (jstsGeom.isValid()) {

                                delete dirty[f.getId()];
                                var featureProperties = f.getProperties();
                                delete featureProperties.boundedBy;
                                var clone = new ol.Feature(featureProperties);
                                clone.setId(f.getId());

                                self.saveData('update', clone);
                            }
                            else {
                                BootstrapDialog.show({
                                    type: BootstrapDialog.TYPE_DANGER,
                                    title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                    message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/content/themes/invalid_polygon.png" width="560" />',
                                    buttons: [{
                                        label: 'OK',
                                        action: function (dialogItself) {
                                            dialogItself.close();
                                        }
                                    }]
                                });

                            }



                        }
                    });
                    var snap = new ol.interaction.Snap({
                        source: this.wfssource
                    });
                    this.map.addInteraction(snap);
                }
                else if (mode == "Delete") {
                    var interactionSelectPointerMove = new ol.interaction.Select({

                    });

                    this.interactionSelect = new ol.interaction.Select({
                        condition: ol.events.condition.click,
                        layers: [this.drawlayer],
                        style: new ol.style.Style({
                            stroke: new ol.style.Stroke({
                                color: '#FF2828'
                            }),
                            fill: new ol.style.Fill({
                                color: [0, 0, 255, 0.5]
                            })
                        })
                    });

                    this.interactionSelect.on('select', function (evt) {
                        GeoWebGIS.clickedcoord = evt.mapBrowserEvent.coordinate;
                        var myf = evt.selected;
                        if (myf.length > 0) {

                            // Handle Multipolygon
                            if (myf[0].getGeometry().getType() == "MultiPolygon") {
                                var polygons = myf[0].getGeometry().getPolygons();

                                var newpolys = [];
                                polygons.forEach(function (mypoly) {
                                    if (mypoly.intersectsCoordinate(GeoWebGIS.clickedcoord)) {
                                    }
                                    else {
                                        newpolys.push(mypoly.getCoordinates());
                                    }

                                });
                                // no polygons anymore, delete the entry
                                if (newpolys.length == 0) {
                                    self.saveData('delete', evt.selected[0]);
                                    self.interactionSelect.getFeatures().clear();
                                }
                                else {
                                    myf[0].getGeometry().setCoordinates(newpolys);
                                    self.saveData('update', evt.selected[0]);
                                    self.interactionSelect.getFeatures().clear();
                                }
                            }
                            else {
                                self.saveData('delete', evt.selected[0]);
                                self.interactionSelect.getFeatures().clear();
                            }

                        }




                    });


                    this.interactionSelect.getFeatures().clear();
                    this.map.removeInteraction(this.interactionSelect);
                    this.map.addInteraction(this.interactionSelect);

                    // create the interaction
                    this.draw_interaction = null;

                    // if there is only one feature, modify this
                    this.draw_interaction = new ol.interaction.Modify({
                        features: this.interactionSelect.getFeatures()
                    });

                    // add it to the map
                    this.map.addInteraction(this.draw_interaction);
                    var self = this;


                }

            }

            GeoWebGIS.endDrawInteraction = function () {
                this.isDrawing = false;
                if (this.interactionSelect) this.interactionSelect.getFeatures().clear();
                this.drawlayer.getSource().clear();
                this.map.removeLayer(this.drawlayer);
                this.map.removeInteraction(this.draw_interaction);
                this.map.addInteraction(self.clusterselect);
                GeoWebGIS.showWFSLayer(GeoWebGIS.wfsurl);
            }

            GeoWebGIS.saveData = function (mode, newFeature) {
                var self = this;

                var formatWFS = new ol.format.WFS();

                var formatGML;

                formatGML = new ol.format.GML({
                    featureNS: 'miresiliencia',
                    featureType: GeoWebGIS.workingNamespace,
                    srsName: 'EPSG:3857'
                });

                var xs = new XMLSerializer();
                //var newFeature = this.drawlayer.getSource().getFeatures()[0];
                var param = "";
                // geoserver Arbeitsbereich
                var workbench = "miresiliencia";

                var node;
                switch (mode) {
                    case 'insert':
                        node = formatWFS.writeTransaction([newFeature], null, null, formatGML);
                        break;
                    case 'update':
                        node = formatWFS.writeTransaction(null, [newFeature], null, formatGML);
                        break;
                    case 'delete':
                        node = formatWFS.writeTransaction(null, null, [newFeature], formatGML);
                        break;
                }

                var payload = xs.serializeToString(node);
                $.ajax('/MapImageProxy/GetGeoServer?param=' + param + '&workbench=' + workbench, {
                    service: 'WFS',
                    type: 'POST',
                    dataType: 'json',
                    processData: false,
                    contentType: 'json',
                    data: payload,
                    success: function (data) {
                        console.log("Datatata");
                        console.log(data);
                        // parse data
                        if (data.length > 0) {
                            if (data[0].indexOf("Intensity.") == 0) {

                                GeoWebGIS.lastInsertedFeatureId = data[0].replace("Intensity.", "");
                                $('#stopIKDrawAfter').click();
                                $('#stopIKDrawBefore').click();
                            }

                        }


                        self.wfslayer.getSource().clear();
                        try {
                            GeoWebGIS.map.removeLayer(self.wfslayer);
                        }
                        catch (exp) {

                        }
                        self.wfslayer.setSource(new ol.source.Vector());
                        self.wfssource.refresh();
                        self.wfslayer.setSource(self.wfssource);

                        GeoWebGIS.map.addLayer(self.wfslayer);


                    }
                });

            }


            GeoWebGIS.checkProjectState = function (successhandler) {
                $.ajax('/Project/GetProjectState', {
                    dataType: 'json',
                    async: false,
                    success: function (data) {
                        var dataString = data + "";
                        if (dataString.indexOf("ERROR") >= 0) {
                            BootstrapDialog.show({
                                type: BootstrapDialog.TYPE_DANGER,
                                title: 'Error',
                                message: data,
                                buttons: [{
                                    label: 'OK',
                                    action: function (dialogItself) {
                                        dialogItself.close();
                                    }
                                }]
                            });
                            return false;
                        }

                        if (data.StateID == "1") successhandler();
                        else if (data.StateID == "2") {
                            BootstrapDialog.show({
                                type: BootstrapDialog.TYPE_WARNING,
                                title: GeoWebGIS.translator.get("calcStateTitle"),
                                message: GeoWebGIS.translator.get("calcStateText"),
                                buttons: [{
                                    label: 'Cerrar',
                                    action: function (dialogItself) {
                                        dialogItself.close();
                                    }
                                }, {
                                    label: GeoWebGIS.translator.get("deleteCalc"),
                                    action: function (dialogItself) {
                                        var dialog = dialogItself;
                                        $.get('/api/Result/Delete/' + data.ProjectID, function (data) {
                                            dialog.close();

                                        })
                                    }
                                }]
                            });
                            return false;
                        }
                        else {
                            BootstrapDialog.show({
                                type: BootstrapDialog.TYPE_DANGER,
                                title: GeoWebGIS.translator.get("closedTitle"),
                                message: GeoWebGIS.translator.get("closedText"),
                                buttons: [{
                                    label: 'OK',
                                    action: function (dialogItself) {
                                        dialogItself.close();
                                    }
                                }]
                            });
                            return false;
                        }
                    }

                });




            }

            GeoWebGIS.showWFSLayer = function (wfsurl) {
                GeoWebGIS.wfsurl = wfsurl;
                var self = this;
                GeoWebGIS.clusterselect.getFeatures().clear();
                this.wfssource = new ol.source.Vector({
                    format: new ol.format.GeoJSON(),
                    loader: function (extent, resolution, projection) {
                        this.resolution = resolution;
                        self.wfssource.set('loadstart', Math.random());
                        self.wfssource.set('myownurl', GeoWebGIS.wfsurl);
                        var proj = projection.getCode();
                        var url = GeoWebGIS.wfsurl;
                        url = url.replace("cql_filter=", "cql_filter=BBOX%28%5Bgeometry%5D," + extent.join(',') + "%29%20AND%20");
                        //if (GeoWebGIS.wfsurl.indexOf("featureID") < 0) url = GeoWebGIS.wfsurl + '&bbox=' + extent.join(',');


                        var xhr = new XMLHttpRequest();
                        xhr.open('GET', url);
                        var onError = function () {
                            vectorSource.removeLoadedExtent(extent);
                        }
                        xhr.onerror = onError;
                        xhr.onload = function () {
                            if (xhr.status == 200) {
                                self.wfssource.addFeatures(
                                    self.wfssource.getFormat().readFeatures(xhr.responseText));
                                self.wfssource.set('loadend', Math.random());
                            } else {
                                onError();
                            }
                        }
                        xhr.send();
                    },

                    //strategy: ol.loadingstrategy.bbox
                    strategy: function (extent, resolution) {
                        if (this.resolution && this.resolution != resolution) {
                            this.loadedExtentsRtree_.clear();
                        }
                        return [extent];
                    }
                });

                this.wfssource.set('loadstart', '');
                this.wfssource.set('loadend', '');

                this.wfssource.on('change:loadstart', function () {
                    GeoWebGIS.progress.addLoading();
                });

                this.wfssource.on('change:loadend', function () {
                    GeoWebGIS.progress.addLoaded();
                });

                this.map.removeLayer(this.wfslayer);
                this.wfslayer.getSource().clear();
                this.wfslayer.setSource(this.wfssource);
                this.map.addLayer(this.wfslayer);
            }

            /*
            GeoWebGIS.showWFSLayer = function (wfsurl2) {
                console.log("ShowWFSLayer: "+wfsurl2);
                GeoWebGIS.wfsurl = wfsurl2;


                GeoWebGIS.clusterselect.getFeatures().clear();
                this.map.removeInteraction(this.clusterselect);

                //this.wfslayer.getSource().clear();

                this.wfslayer.getSource().getFeatures().forEach(function (feature) {
                    GeoWebGIS.wfslayer.getSource().removeFeature(feature);
                });

                this.map.removeLayer(this.wfslayer);
                this.wfssource = null;
                this.wfssource = new ol.source.Vector({
                    format: new ol.format.GeoJSON(),
                    url: GeoWebGIS.wfsurl + '&t=' + new Date().getTime(),
                    strategy: ol.loadingstrategy.bbox
                });
                

                this.wfslayer.setSource(null);
                this.wfslayer.setSource(this.wfssource);
                this.map.addLayer(this.wfslayer);

                this.map.addInteraction(this.clusterselect);
            }
            */

            GeoWebGIS.showLayer = function (id) {
                var self = this;
                var la = getLayer(id);
                try {
                    la.getSource().updateParams({ "time": Date.now() });
                }
                catch (err) { }
                GeoWebGIS.map.removeLayer(la);
                GeoWebGIS.map.addLayer(la);
            }

            GeoWebGIS.hideLayer = function (id) {
                var self = this;
                var la = getLayer(id);
                GeoWebGIS.map.removeLayer(getLayer(id));
            }

            GeoWebGIS.generateLegend = function (legendDiv) {
                GeoWebGIS.legendLayers = [];
                GeoWebGIS.map.getLayers().forEach(function (layer) {
                    if (layer instanceof ol.layer.Vector) {
                        var url = "";
                        var gl = "";
                        try {
                            url = layer.getSource().getUrl().toString();
                            var layerGeoserver = url.match(/&typeName=[a-zA-Z]*:[a-zA-Z]*&/);
                            if (layerGeoserver.length > 0) gl = layerGeoserver[0].replace("&", "").replace("typeName=", "").replace("&", "");
                        }
                        catch (e) {

                        }
                        try {
                            url = layer.getSource().get('myownurl');
                            var layerGeoserver = url.match(/&typeName=[a-zA-Z]*:[a-zA-Z]*&/);
                            if (layerGeoserver.length > 0) gl = layerGeoserver[0].replace("&", "").replace("typeName=", "").replace("&", "");
                        }
                        catch (e) {

                        }
                        if (gl != "") GeoWebGIS.legendLayers.push(gl);

                    }




                });
            }



        };


        /**
         * Function: parseQueryString
         *
         * An initialization time function to parse the application URL parameters
         * and stores them in an array.  They can be retrieved using 
         * {<Fusion.getQueryParam>}
         *
         * Return: 
         * {Array} an array of the query params from when the page was loaded
         */
        GeoWebGIS.parseQueryString = function () {
            this.queryParams = [];
            var s = window.location.search;
            if (s != '') {
                s = s.substring(1);
                var p = s.split('&');
                for (var i = 0; i < p.length; i++) {
                    var q = p[i].split('=');
                    this.queryParams[q[0].toLowerCase()] = decodeURIComponent(q[1]);
                }
            }
            return this.queryParams;
        };

        /**
         * Function: getQueryParam
         *
         * Returns the query parameter value for a given parameter name
         *
         * Parameters: 
         * p - {String} the parameter to lookup
         *
         * Return: 
         * parameter value or the empty string '' if not found
         */
        GeoWebGIS.getQueryParam = function (p) {
            if (!this.queryParams) {
                this.parseQueryString();
            }
            p = p.toLowerCase();
            if (this.queryParams[p] && typeof this.queryParams[p] == 'string') {
                return this.queryParams[p];
            } else {
                return '';
            }
        };





        function Progress(el) {
            this.el = el;
            this.loading = 0;
            this.loaded = 0;
        }


        /**
         * Increment the count of loading tiles.
         */
        Progress.prototype.addLoading = function () {
            if (this.loading === 0) {
                this.show();
            }
            ++this.loading;
            this.update();
        };


        /**
         * Increment the count of loaded tiles.
         */
        Progress.prototype.addLoaded = function () {
            var this_ = this;
            setTimeout(function () {
                ++this_.loaded;
                this_.update();
            }, 100);
        };


        /**
         * Update the progress bar.
         */
        Progress.prototype.update = function () {
            var width = (this.loaded / this.loading * 100).toFixed(1) + '%';
            this.el.style.width = width;
            if (this.loading <= this.loaded) {
                this.loading = 0;
                this.loaded = 0;
                waitingDialog.hide();
                var this_ = this;
                setTimeout(function () {
                    this_.hide();
                }, 500);
            }
        };


        /**
         * Show the progress bar.
         */
        Progress.prototype.show = function () {
            this.el.style.visibility = 'visible';
        };


        /**
         * Hide the progress bar.
         */
        Progress.prototype.hide = function () {
            if (this.loading === this.loaded) {
                this.el.style.visibility = 'hidden';
                this.el.style.width = 0;
            }
        };





        return GeoWebGIS;
    }
    //define globally if it doesn't already exist
    if (typeof (GeoWebGIS) === 'undefined') {
        window.GeoWebGIS = define_GeoWebGIS();
        window.GeoWebGIS.initialize();
    }
    else {
        console.log("GeoWebGIS already defined.");
    }




})(window);


var exportPNGElement = document.getElementById('export-png');
var loading = 0;
var loaded = 0;


var legend_loaded = 0;
var legend_loading = 0;




exportPNGElement.addEventListener('click', function () {
    console.log("Bin in ExportPNGElement");


    document.body.style.cursor = 'progress';

    var dpi = 150;

    var dim = [297, 210];
    console.log(GeoWebGIS.map.getSize());
    var width = Math.round(dim[0] * dpi / 25.4);
    var height = Math.round(dim[1] * dpi / 25.4);
    var size = /** @type {ol.Size} */ (GeoWebGIS.map.getSize());
    var extent = GeoWebGIS.map.getView().calculateExtent(size);


    var olscale = $('.ol-scale-line-inner');
    //Scaleline thicknes
    var line1 = 6;
    //Offset from the left
    var x_offset = 10;
    //offset from the bottom
    var y_offset = 30;
    var fontsize1 = 15;
    var font1 = fontsize1 + 'px Arial';
    // how big should the scale be (original css-width multiplied)
    var multiplier = 1;

    function WriteLegendtoCanvas() {
        var canvas = $('canvas').get(0);
        var ctx = canvas.getContext("2d");
        //var ctx = e;
        ctx.fillStyle = "#ffffff";
        console.log(250 / 150 * dpi);
        ctx.fillRect(10, 10, 250 / 150 * dpi, 80 / 150 * dpi);
        //ctx.strokeStile = "#000000";
        //ctx.strokeRect(10, 10, 250, 400);

        ctx.fillStyle = "#000000";
        ctx.font = "25px Arial";
        ctx.fillText("Leyenda", 15, 40 / 150 * dpi);



        //var legends = $('.legendImage');
        //for (i = 0; i < legends.length; i++) {

        var legendy = 60 / 150 * dpi;
        var self = this;

        var na = new Image();
        na.src = "/content/themes/northarrow.png";
        self.legend_loading++;

        ctx.drawImage(na, 10, height - 200 / 150 * dpi, 62 / 150 * dpi, 100 / 150 * dpi);
        na.onload = function () {
            ctx.drawImage(na, 10, height - 200 / 150 * dpi, 62 / 150 * dpi, 100 / 150 * dpi);
            self.legend_loaded++;
            if (self.legend_loading == self.legend_loaded) {
                $.event.trigger({
                    type: "legendLoaded"
                });
            }
        }

        GeoWebGIS.legendLayers.forEach(function (Layer) {
            var img = new Image();
            img.src = '/proxy/wms?REQUEST=GetLegendGraphic&VERSION=1.0.0&FORMAT=image/png&WIDTH=' + 60 / 150 * dpi + '&Legend_options=forceLabels:on&LAYER=' + Layer;

            console.log(img.src);
            self.legend_loading++;
            img.onload = function () {


                ctx.fillStyle = "#ffffff";
                ctx.fillRect(10, legendy, 250 / 150 * dpi, (img.height / 150 * dpi) + (10 / 150 * dpi));

                ctx.drawImage(img, 10, legendy);



                legendy = legendy + (img.height / 150) * dpi + (10 / 150 * dpi);
                self.legend_loaded++;
                if (self.legend_loading == self.legend_loaded) {
                    $.event.trigger({
                        type: "legendLoaded"
                    });
                }
            }
            img.onerror = function () {
                self.legend_loaded++;
                if (self.legend_loading == self.legend_loaded) {
                    $.event.trigger({
                        type: "legendLoaded"
                    });
                }
            }
            ctx.drawImage(img, 10, legendy);
        });

    }


    /* go for it */
    function WriteScaletoCanvas() {
        var canvas = $('canvas').get(0);
        var ctx = canvas.getContext("2d");

        var scalewidth = parseInt(olscale.css('width'), 10) * multiplier;
        var scale = olscale.text();
        var scalenumber = parseInt(scale, 10) * multiplier;
        var scaleunit = scale.match(/[Aa-zZ]{1,}/g);

        //Scale Text
        ctx.beginPath();
        ctx.textAlign = "left";
        ctx.strokeStyle = "#ffffff";
        ctx.fillStyle = "#000000";
        ctx.lineWidth = 5;
        ctx.font = font1;
        ctx.strokeText([scalenumber + ' ' + scaleunit], x_offset + fontsize1 / 2, canvas.height - y_offset - fontsize1 / 2);
        ctx.fillText([scalenumber + ' ' + scaleunit], x_offset + fontsize1 / 2, canvas.height - y_offset - fontsize1 / 2);

        //Scale Dimensions
        var xzero = scalewidth + x_offset;
        var yzero = canvas.height - y_offset + 10;
        var xfirst = x_offset + scalewidth * 1 / 4;
        var xsecond = xfirst + scalewidth * 1 / 4;
        var xthird = xsecond + scalewidth * 1 / 4;
        var xfourth = xthird + scalewidth * 1 / 4;

        // Stroke
        ctx.beginPath();
        ctx.lineWidth = line1 + 2;
        ctx.strokeStyle = "#000000";
        ctx.fillStyle = "#ffffff";
        ctx.moveTo(x_offset, yzero);
        ctx.lineTo(xzero + 1, yzero);
        ctx.stroke();

        //sections black/white
        ctx.beginPath();
        ctx.lineWidth = line1;
        ctx.strokeStyle = "#000000";
        ctx.moveTo(x_offset, yzero);
        ctx.lineTo(xfirst, yzero);
        ctx.stroke();

        ctx.beginPath();
        ctx.lineWidth = line1;
        ctx.strokeStyle = "#FFFFFF";
        ctx.moveTo(xfirst, yzero);
        ctx.lineTo(xsecond, yzero);
        ctx.stroke();

        ctx.beginPath();
        ctx.lineWidth = line1;
        ctx.strokeStyle = "#000000";
        ctx.moveTo(xsecond, yzero);
        ctx.lineTo(xthird, yzero);
        ctx.stroke();

        ctx.beginPath();
        ctx.lineWidth = line1;
        ctx.strokeStyle = "#FFFFFF";
        ctx.moveTo(xthird, yzero);
        ctx.lineTo(xfourth, yzero);
        ctx.stroke();



        // Draw logo

        var x = 50, y = 50;
        var arr = $('div#promiLogo img');

        for (i = 0; i < arr.length; i++) {

            var img = new Image();
            img.src = $(arr[i]).attr('src');
            img.onload = function () { ctx.drawImage(img, 0, 0, img.width, img.height, width - 80, height - 80, 70, 70); }
            ctx.drawImage(img, 0, 0, img.width, img.height, width - 80, height - 80, 70, 70);

        }




    }
    var source = GeoWebGIS.backgroudosm.getSource();

    var tileLoadStart = function () {
        //console.log("Loading");

        isReloading = true;
        ++loading;
    };




    var tileLoadEnd = function () {
        ++loaded;
        window.setTimeout(function () {
            if (loading === loaded) {



                //loading = 0;
                //loaded = -1;
                console.log("WriteScaleAndLegfen");

                $.event.trigger({
                    type: "legendLoaded"
                });
                WriteScaletoCanvas();
                WriteLegendtoCanvas();

            }
        }, 1000);
    };
    $(document).on("legendLoaded", createThePDF);

    function createThePDF() {


        





        window.setTimeout(function () {

            const exportOptions = {
                useCORS: false,
                allowTaint: true,
                ignoreElements: function (element) {
                    const className = element.className || '';
                    return (
                        className.includes('ol-control') &&
                        !className.includes('ol-scale') &&
                        (!className.includes('ol-attribution') ||
                            !className.includes('ol-uncollapsible'))
                    );
                },
            };
            exportOptions.width = width;
            exportOptions.height = height;
            console.log($('canvas').get(0));

            //var canvas = $('canvas').get(0);
            //if (legend_loaded >= legend_loading) {

            html2canvas(GeoWebGIS.map.getViewport(), exportOptions).then(function (canvas) {
                var data = canvas.toDataURL('image/jpeg');
                var pdf = new jsPDF({
                    orientation: 'landscape', unit: 'mm',
                    format: [297, 210]
                });
                pdf.addImage(data, 'JPEG', 5, 20, dim[0] - 10, dim[1] - 30);
                pdf.rect(5, 20, dim[0] - 10, dim[1] - 30);
                pdf.text($('#printtitle').val(), 8, 13);
                pdf.setFontSize(6);
                pdf.text("(C) MiResiliencia (https://www.mi-resiliencia.org) ", 10, dim[1] - 5);
                pdf.save('map.pdf');

                source.un('tileloadstart', tileLoadStart);
                source.un('tileloadend', tileLoadEnd, canvas);
                source.un('tileloaderror', tileLoadEnd, canvas);
                GeoWebGIS.map.getTargetElement().style.width = '';
                GeoWebGIS.map.getTargetElement().style.height = '';
                GeoWebGIS.map.updateSize();
                document.body.style.cursor = 'auto';
            });
            //};
        }, 5000);
    }

    var isReloading = false;

    GeoWebGIS.map.once('postcompose', function (event) {
        GeoWebGIS.generateLegend();
        source.on('tileloadstart', tileLoadStart);
        source.on('tileloadend', tileLoadEnd);
        source.on('tileloaderror', tileLoadEnd);
    });


    GeoWebGIS.map.getTargetElement().style.width = width + 'px';
    GeoWebGIS.map.getTargetElement().style.height = height + 'px';
    GeoWebGIS.map.updateSize();

    //GeoWebGIS.map.setSize([width, height]);
    //GeoWebGIS.map.getView().fit(extent, { size: /** @type {ol.Size} */([width, height]), constrainResolution: false });
    //GeoWebGIS.map.renderSync();

    // check if it is reloading. If not, start pdf creation anyway
    window.setTimeout(function () {
        if (!isReloading) {
            GeoWebGIS.generateLegend();
            WriteScaletoCanvas();
            WriteLegendtoCanvas();
        }
    }, 1000);



}, false);


/**
 * This is a workaround.
 * Returns the associated layer.
 * @param {ol.Map} map.
 * @return {ol.layer.Vector} Layer.
 */
ol.Feature.prototype.getLayer = function (map) {
    var this_ = this, layer_, layersToLookFor = [];
    /**
     * Populates array layersToLookFor with only
     * layers that have features
     */
    var check = function (layer) {
        var source = layer.getSource();
        if (source instanceof ol.source.Vector) {
            var features = source.getFeatures();
            if (features.length > 0) {
                layersToLookFor.push({
                    layer: layer,
                    features: features
                });
            }
        }
    };
    //loop through map layers
    map.getLayers().forEach(function (layer) {
        if (layer instanceof ol.layer.Group) {
            layer.getLayers().forEach(check);
        } else {
            check(layer);
        }
    });
    layersToLookFor.forEach(function (obj) {
        var found = obj.features.some(function (feature) {
            return this_ === feature;
        });
        if (found) {
            //this is the layer we want
            layer_ = obj.layer;
        }
    });
    return layer_;
};


var utils = {
    refreshGeoJson: function (url, source) {
        this.getJson(url).when({
            ready: function (response) {
                var format = new ol.format.GeoJSON();
                var features = format.readFeatures(response, {
                    featureProjection: 'EPSG:3857'
                });
                source.addFeatures(features);
                source.refresh();
            }
        });
    },
    getJson: function (url) {
        var xhr = new XMLHttpRequest(),
            when = {},
            onload = function () {
                if (xhr.status === 200) {
                    when.ready.call(undefined, JSON.parse(xhr.response));
                }
            },
            onerror = function () {
                console.info('Cannot XHR ' + JSON.stringify(url));
            };
        xhr.open('GET', url, true);
        xhr.setRequestHeader('Accept', 'application/json');
        xhr.onload = onload;
        xhr.onerror = onerror;
        xhr.send(null);

        return {
            when: function (obj) { when.ready = obj.ready; }
        };
    }
};
