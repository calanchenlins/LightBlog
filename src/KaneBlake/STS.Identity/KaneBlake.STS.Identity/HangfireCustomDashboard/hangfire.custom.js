(function (hangfire) {

    hangfire.CustomPage = (function () {
        function CustomPage() {
            this._title = 'title';
            this._message = 'message';
            this._initialize();
        }

        CustomPage.prototype._initialize = function () {

        }

        CustomPage.prototype.show = function () {
            this._errorAlertTitle.html(this._title);
            this._errorAlertMessage.html(this._message);
            $('#errorAlert').slideDown('fast');
        };

        return CustomPage;
    })();

})(window.Hangfire = window.Hangfire || {});

$(function () {

    function loadCustomPageModule() {
        Hangfire.customPage = new Hangfire.CustomPage();
    }

    if (window.attachEvent) {
        window.attachEvent('onload', loadCustomPageModule);
    } else {
        if (window.onload) {
            var curronload = window.onload;
            var newonload = function (evt) {
                curronload(evt);
                loadCustomPageModule(evt);
            };
            window.onload = newonload;
        } else {
            window.onload = loadCustomPageModule;
        }
    }

    console.log('CustomPage Initialize!');
});
