(function (hangfire) {

    hangfire.CustomPage = (function () {
        function CustomPage() {
            this._title = 'title';
            this._message = 'message';
            this._initialize();
        }

        function loadScriptAsync(url) {
            return new Promise((resolve, reject) => {
                // let head = document.head;
                let body = document.body;
                let script = document.createElement('script');
                script.type = 'text/javascript';
                script.src = url;
                script.async = false;

                // Then bind the event to the callback function.
                // There are several events for cross browser compatibility.
                //if (script.readyState) {
                //    script.onreadystatechange = function () {
                //        if (script.readyState == "loaded" || script.readyState == "complete") {
                //            script.onreadystatechange = null;
                //            callback();
                //        }
                //    };
                //}
                //else {
                //    script.onload = callback;
                //}

                script.onload = script.onreadystatechange = function () {
                    if (!this.readyState || this.readyState === 'loaded' || this.readyState === 'complete') {
                        script.onload = script.onreadystatechange = null;
                        resolve();
                    }
                };

                // Fire the loading
                body.appendChild(script);
            });
        };

        function loadStylesheetAsync(url) {
            return new Promise((resolve, reject) => {
                var head = document.head;
                var link = document.createElement('link');
                link.type = 'text/css';
                link.rel = 'stylesheet';
                link.href = url;
                link.onload = () => resolve();
                head.appendChild(link);
            });
        }

        CustomPage.prototype._initialize = async function () {
            let div = document.createElement('div');
            div.id = 'App';
            document.body.appendChild(div);
            await loadStylesheetAsync('/lib/element-ui/lib/theme-chalk/index.css')
            await loadScriptAsync('/lib/axios.min.js')
            await loadScriptAsync('/lib/element-ui/vue.js')
            await loadScriptAsync('/lib/element-ui/lib/index.js')
            let apptemplate = (await axios.get('/js/hangfire.custom.apptemplate.html', { responseType: 'html' })).data;
            let recurringJobs = (await axios.get('/hangfireapi/Manage/JobEntries')).data;
            debugger
            let options = [...new Set(recurringJobs.map(job => job.typeName))].map(function (el) {
                return { value: el, label: el };
            });
            options.forEach((e) => {
                e.children = recurringJobs.filter(el => el.typeName === e.value)
                    .map(function (ele) {
                        return { value: ele.methodName, label: ele.methodName, parameters: ele.parameters};
                    });
            })
            debugger
            if (window.location.href.includes('/recurring')) {
                let RecurringJobAddOrUpdateButton = `
                    <button class="js-jobs-list-command btn btn-sm btn-primary js-job-custom-command recurring-job-update" disabled>
	                    <span class="glyphicon glyphicon-edit"></span>
	                    编辑定时任务
                    </button>
                    <button class="btn btn-sm btn-primary custom-job-add">
	                    <span class="glyphicon glyphicon-plus-sign"></span>
	                    创建任务
                    </button>`;
                $('.btn-toolbar-top').append(RecurringJobAddOrUpdateButton);
                $('.custom-job-add').click(function () {
                    mainApp.customJobAdd();
                });
                debugger
                $._data($('.js-jobs-list').get(0), 'events').click.find(el => el.selector == ".js-jobs-list-command").selector = '.js-jobs-list-command:not(".js-job-custom-command")'
                $('.recurring-job-update').click(function () {
                    var jobs = $(".js-jobs-list input[name='jobs[]']:checked").map(function () {
                        return $(this).val();
                    }).get();
                    if (jobs.length == 0) {
                        mainApp.$message({
                            message: '请选择需要修改的作业!',
                            type: 'warning'
                        });
                    }
                    if (jobs.length > 1)
                    {
                        mainApp.$message({
                            message: '请选择需要修改的作业!',
                            type: 'warning'
                        });
                    }
                    mainApp.recurringJobUpdate(jobs[0]);
                });
                const fixedparameter = {
                    Key: 'cronExpression',
                    value: '0 0 * * *'
                }
                let Main = {
                    template: apptemplate,
                    data() {
                        return {
                            dialogFormVisible: false,
                            jobOptions: options,
                            jobType:'recurringJob',
                            targetJob: [],
                            dynamicForm: [],
                            fixedparameters:[],
                            dynamicFormColNum: 2
                        }
                    },
                    watch: {},
                    mounted() {
                        this.$nextTick(function () { });
                    },
                    computed: {
                        dynamicFormView: function () {
                            let _dynamicForm = this.fixedparameters.concat(this.dynamicForm);
                            let rowNum = Math.ceil(_dynamicForm.length / this.dynamicFormColNum);
                            let FormView = new Array(rowNum);
                            let index = 0;
                            do {
                                let temp = _dynamicForm.slice(index, index + this.dynamicFormColNum)
                                FormView.push(temp)
                                index = index + this.dynamicFormColNum;
                            } while (index < _dynamicForm.length);
                            debugger
                            return FormView;
                        },
                        jobTypeLable: function ()
                        {
                            return this.jobType === 'recurringJob' ? '定时作业' : '后台作业';
                        }
                    },
                    methods: {
                        genNonDuplicateID() {
                            return Date.now().toString(36) + Math.random().toString(36).substr(3);
                        },
                        buildFormObject() {
                            let _dynamicForm = this.fixedparameters.concat(this.dynamicForm);
                            let formDataObj = new Object();
                            _dynamicForm.forEach(el => {
                                if (el.value.trim().length > 0) {
                                    formDataObj[el.Key] = el.value;
                                }
                            });
                            formDataObj.TypeName = this.targetJob[0];
                            formDataObj.MethodName = this.targetJob[1];
                            return formDataObj;
                        },
                        handleChange(arr) {
                            if (arr.length < 2)
                            {
                                this.dynamicForm = [];
                                return
                            }
                            let job = this.jobOptions.find((el) => el.value === arr[0]).children.find((el) => el.value === arr[1]);
                            console.log(job.parameters);
                            let newDynamicForm = job.parameters.map(function (el) {
                                return { Key: el.Key, value: el.value };
                            });
                            this.dynamicForm = newDynamicForm;
                        },
                        onJobTypeChanged(val) {
                            if (val === 'recurringJob') {
                                this.fixedparameters = [
                                    { Key: 'Cron', value: '0 0 * * *' },
                                    { Key: 'TimeZoneId', value: '' },
                                    { Key: 'Queue', value: 'default' }
                                ];
                            }
                            else {
                                this.fixedparameters = [
                                    { Key: 'EnqueueAt', value: '', type:'date' },
                                    { Key: 'Queue', value: 'default' }
                                ];
                            }
                        },
                        handleClose(done) {
                            this.onFormClosed();
                            done();
                        },
                        onCanceled() {
                            this.onFormClosed();
                        },
                        onFormClosed() {
                            this.targetJob = [];
                            this.dynamicForm = [];
                            this.fixedparameters = [];
                            this.dialogFormVisible = false;
                        },
                        customJobAdd() {
                            this.onJobTypeChanged('recurringJob');
                            this.dialogFormVisible = true;
                        },
                        recurringJobUpdate(id) {

                        },
                        async onSubmit() {
                            let that = this;
                            let url = '/hangfireapi/Manage/RecurringJob/add';
                            if (that.jobType === 'backgroundJob') {
                                url = '/hangfireapi/Manage/BackgroundJob/add';
                            }
                            let recurringJobs = (await axios.post(url, this.buildFormObject())).data;
                            debugger
                            return
                            this.$refs['dynamicFormRef'].validate((valid) => {
                                if (!valid) {
                                    console.log('error submit!');
                                    return false;
                                }
                                if (this.newRowForm.changeType !== 'ChangeProcN') {
                                    let items = this.newRowTableData.map((el) => el.item_no.trim());
                                    ServiceApi('IsActionValid', { changeType: this.newRowForm.changeType, sys_process_no: this.newRowForm.sys_process_no.split('|')[0], items },
                                        (data) => {
                                            if (data.Code === 2000) {
                                                addNewRow(this.newRowForm, this.newRowTableData)
                                                this.onCanceled();
                                                this.$message({
                                                    message: '新增记录成功!',
                                                    type: 'success'
                                                });
                                            }
                                            else {
                                                this.$message({
                                                    showClose: true,
                                                    message: data.Message,
                                                    duration: 5000,
                                                    type: 'error'
                                                })
                                            }
                                        }
                                    );
                                }
                                else {
                                    addNewRow(this.newRowForm, this.newRowTableData)
                                    this.onCanceled();
                                    this.$message({
                                        message: '新增记录成功!',
                                        type: 'success'
                                    });
                                }
                            });
                        },
                    }
                };
                let Ctor = Vue.extend(Main)
                let mainApp = new Ctor();
                window.mainApp = mainApp;
                mainApp.$mount('#App')

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
