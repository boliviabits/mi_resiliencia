function loadHeaderURL(url) {
    $('#HeaderContent').html('');
    goToInsideUrl = url;
    $hidden = $('#HeaderUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#HeaderContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#header-loading-spinner");
    $hidden.attr("ic-on-complete", "loadedHeader()");
    $hidden.trigger("click");
}

function loadedHeader() {
}

$('.interLink').each(
    function () {
        var $this = $(this);
        if ($this.attr("ic-get-from") == null) {
            $this.attr("ic-get-from", $this.attr("href"));
            $this.attr("ic-target", "#HeaderContent");
            $this.attr("ic-push-url", "false");
            $this.attr("ic-indicator", "#header-loading-spinner");
            $this.removeAttr("href");
        }

    }
)


function loadSubHeaderURL(url) {
    $('#SubHeaderContent').html('');
    goToInsideUrl = url;
    $hidden = $('#SubHeaderUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#SubHeaderContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#SubHeader-loading-spinner");
    $hidden.trigger("click");
}

function loadInfoURL(url) {
    $('#InfoContent').html('');
    goToInsideUrl = url;
    $hidden = $('#InfoUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#InfoContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#Info-loading-spinner");
    $hidden.trigger("click");
}

function loadSearchURL(url) {
    $('#SearchContent').html('');
    goToInsideUrl = url;
    $hidden = $('#SearchUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#SearchContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#Search-loading-spinner");
    $hidden.trigger("click");
}

function loadPermitURL(url) {
    $('#PermitContent').html('');
    goToInsideUrl = url;
    $hidden = $('#PermitUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#PermitContent");
    $hidden.attr("ic-indicator", "#Permit-loading-spinner");
    $hidden.attr("ic-push-url", "false");
    $hidden.trigger("click");
}


