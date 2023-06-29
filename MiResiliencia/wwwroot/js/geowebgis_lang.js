var en = {
    wait: "Please wait",
    errorwas: "The error was",
    change: "Change",
    wrongPolyTitle: "Could not generate the polygon",
    wrongPolyText: "The polygon is not valid. These polygons are invalid",
    calcStateTitle: "Status calculated",
    calcStateText: "Could not do any changes in the project, because the state is calculated. Please click on  \"Delete the calculation\"",
    closedTitle: "Project is closed",
    closedText: "Could not edit the project, because the project state is closed or marked for closed. Please inform the super company",
    deleteCalc: "Delete the calculation",
    table: {

    }

}

var es = {
    wait: "Procesando",
    errorwas: "El error es",
    change: "Modificar",
    wrongPolyTitle: "Error en la generación de polígono",
    wrongPolyText: "El polígono digitalizado no es correcto. Las siguientes formas son inválidas.",
    calcStateTitle: "Estatus calculado",
    calcStateText: "No se puede editar el proyecto antes de presionar el boton \"cancelar resultados\"",
    closedTitle: "Project is closed",
    closedText: "Could not edit the project, because the project state is closed or marked for closed. Please inform the super company",
    deleteCalc: "Cancelar resultados",
    table: {
        "sProcessing": "Procesando...",
        "sLengthMenu": "Mostrar _MENU_ proyectos",
        "sZeroRecords": "No se encontraron resultados",
        "sEmptyTable": "Ningún dato disponible en esta tabla",
        "sInfo": "Mostrando registros del _START_ al _END_ de un total de _TOTAL_ proycetos",
        "sInfoEmpty": "Mostrando registros del 0 al 0 de un total de 0 proyectos",
        "sInfoFiltered": "(filtrado de un total de _MAX_ proyectos)",
        "sInfoPostFix": "",
        "sSearch": "Buscar:",
        "sUrl": "",
        "sInfoThousands": ",",
        "sLoadingRecords": "Cargando...",
        "oPaginate": {
            "sFirst": "Primero",
            "sLast": "Último",
            "sNext": "Siguiente",
            "sPrevious": "Anterior"
        },
        "oAria": {
            "sSortAscending": ": Activar para ordenar la columna de manera ascendente",
            "sSortDescending": ": Activar para ordenar la columna de manera descendente"
        }
    }
}




function GeoWebGISLanguage(lang) {
    var __construct = function () {
        if (eval('typeof ' + lang) == 'undefined') {
            lang = "en";
        }
        return;
    }()

    this.get = function (str, defaultStr) {
        var retStr = eval('eval(lang).' + str);
        if (typeof retStr != 'undefined') {
            return retStr;
        } else {
            if (typeof defaultStr != 'undefined') {
                return defaultStr;
            } else {
                return eval('en.' + str);
            }
        }
    }
}