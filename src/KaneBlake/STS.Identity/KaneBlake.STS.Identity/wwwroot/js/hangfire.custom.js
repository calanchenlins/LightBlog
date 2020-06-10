(function (hangfire) {

    hangfire.CustomPage = (function () {
        function CustomPage() {
            this._title = 'title';
            this._message = 'message';
            this._initialize();
        }

        function loadScript(url, callback) {
            // Adding the script tag to the head as suggested before
            var head = document.head;
            var script = document.createElement('script');
            script.type = 'text/javascript';
            script.src = url;

            // Then bind the event to the callback function.
            // There are several events for cross browser compatibility.
            script.onreadystatechange = callback;
            script.onload = callback;

            // Fire the loading
            head.appendChild(script);
        }

        CustomPage.prototype._initialize = function () {
            loadScript('/lib/layer/layer.js', function () {
                console.log('load layer.js');
            });
            if (window.location.href.includes('/recurring'))
            {
                // js-jobs-list  btn-toolbar-top
                //jQuery,
                let RecurringJobAddOrUpdateButton = `
                    <button class="btn btn-sm btn-primary recurring-job-add-or-update">
	                    <span class="glyphicon glyphicon-play-circle"></span>
	                    新增任务
                    </button>`;
                $('.btn-toolbar-top').append(RecurringJobAddOrUpdateButton);
                $('.recurring-job-add-or-update').click(function () {
                    layer.open({
                        type: 2,
                        content: '/Hangfier/Index' //这里content是一个URL，如果你不想让iframe出现滚动条，你还可以content: ['http://sentsin.com', 'no']
                    }); 
                });
            }
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
