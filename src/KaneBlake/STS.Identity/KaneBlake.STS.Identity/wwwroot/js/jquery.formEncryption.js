// Unobtrusive formEncryptionSubmit support library for jQuery
(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
    } else if (typeof module === 'object' && module.exports) {
        // CommonJS-like environments that support module.exports     
    } else {
        // Browser global
        jQuery.validator.unobtrusive.EncryptedSubmit = factory(jQuery, JSEncrypt, MessagePack, axios, Qs);
        debugger
    }
}(function (_$, _JSEncrypt, _MessagePack, _axios, _Qs) {
    const $jQval = _$.validator;
    // https://jqueryvalidation.org/validate/
    $jQval.setDefaults({
        onkeyup: false
    });

    const encryptedFormAttr = "data-encrypted-form=true";
    const data_formEncryptionSubmit = "formEncryptionSubmit";
    const storage_CachePublickey = 'PublicKey'

    const formSubmitApi = _axios.create({
        baseURL: '/',
        timeout: 30000
    })
    formSubmitApi.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest'; // tag XHR request
    formSubmitApi.defaults.headers.post['Content-Type'] = 'application/x-msgpack'
    formSubmitApi.defaults.headers.post['form-data-format'] = 'EncryptionForm'; 

    formSubmitApi.interceptors.request.use(
        config => {
            return config
        },
        error => {
            console.log('request err:' + error)
            return Promise.reject(error)
        }
    )
    formSubmitApi.interceptors.response.use(
        response => {
            if (response.status === 200 && Object.keys(response.headers).includes('transparent-redirect')) {
                window.location.replace(response.headers['transparent-redirect']);
                return;
            }
            return response.data // 等同于 return Promise.resolve(response.data) 
        },
        error => {
            console.log('response err:' + error)
            return Promise.reject(error) // catch中处理,如果直接返回错误,在then中处理
        }
    )

    const GetPublicKey = function () {
        const publicKey = localStorage.getItem(storage_CachePublickey)
        if (publicKey) {
            return Promise.resolve(publicKey)
        }
        return formSubmitApi({
            url: '/Account/GetPublicKey',
            method: 'get'
        })
            .then((data) => {
                debugger
                localStorage.setItem(storage_CachePublickey, data)
                return Promise.resolve(data) // 返回结果 then 处理
            })
            .catch((err) => {
                debugger
                return Promise.reject(err)// 抛出错误 catch 处理
            })
    }

    const hexToBytes = function (hexStr) {
        // Shortcut
        var hexStrLength = hexStr.length;
        // Convert
        var words = [];
        for (var i = 0; i < hexStrLength; i += 2) {
            words.push(parseInt(hexStr.substr(i, 2), 16));
        }
        return words;
    }

    const escapeCssMeta = function (a) {
        return a.replace(/([\\!"#$%&'()*+,.\/:;<=>?@\[\]^`{|}~])/g, "\\$1")
    }

    const onSubmitCustomErrors = function (errorMap) {  // 'this' is the form element
        debugger
        console.log("onSubmitCustomErrors")
        var container = _$(this).find("[data-valmsg-custom=true]"),
            list = container.find("ul");

        if (list && list.length && errorMap) {
            list.empty();
            //container.addClass("validation-summary-errors").removeClass("validation-summary-valid");

            for (let [key, value] of Object.entries(errorMap)) {
                _$("<li />").html(value).appendTo(list);
            }
        }
    }

    const attachEncryptedForm = function (form) {
        const $form = _$(form);
        const _encrypt = new _JSEncrypt({
            default_key_size: 4096
        });
        const formEncryptionSubmit = function () {
            if (!$form.data().validator.form()) {
                return false;
            }

            let formDataObj = _Qs.parse($form.serialize());
            if (!Object.keys(formDataObj).includes(document.activeElement.name)) {
                formDataObj[document.activeElement.name] = document.activeElement.value
            }
            let postUrl = $form.attr('action') ?? document.baseURI
            debugger
            GetPublicKey().then((data) => {

                _encrypt.setPublicKey('MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA83r9FJsrALyxfL4G9aSJOqwwpnkEVv1fheGuqpE2wE8t9FRtsg+EDqX6DKPLcAEGq9pv5EYmiYwuy/Swt1PmIQkgu0HzcMdC7V4agtLL+grBlMP58VZsb+cg6pxOcpvLS+EZ9L22lXLnfXsricPRjkdjoZ+7X+mdE9Zxln6j9s+JD6Hdw+GUxO2X3ukCoK7FSC+Hk+P9ETpy0yPxnwIN8cnqQLCYt0yLf144niex2G3nECw7Ky8nznjd5EWfqAXpKvIZEsvjofW4qSGDoUNXWpMJcDDL28KjO5dgDzkIJQjhix3UwSSwneyZuiq8QW9wMuQUcZuB1TAmTMbnCh+Pfav0Hij7WuxsT92nasrxdHABL6SmJInhxQ3/rWk4sem20cZohaarMGoiseCJNs9lA0SYUMvWyaY+A9QsmY+xI0eJj1YRKHtG4C8CYrZbmK80yXINOa7S32TfURXKocl1O6n6PLfusrCts5w45jjCDNLvva5WtOwky604YKGvMrlM1yS2PnMwkX1NBxrJ/50E81xnG1VSMgS07OoKJOowmnbY9f1O3myHlOh9BFvwfo9ciWraxPiVaddAesHVlXyfYa6E50HIEJ9aqZeyLMMbxzZB2Nrv8uV2E0Me2gyVzntgSu5ssAaRJCJVWdHVNwqeK11BaxGBWk6EtNJcRajOITMCAwEAAQ==');

                let msgSize = ((_encrypt.getKey().n.bitLength() + 7) >> 3) - 11;
                for (let [key, value] of Object.entries(formDataObj)) {
                    if (key == '__RequestVerificationToken') {
                        delete formDataObj[key]
                    }
                }

                //let Plaintext = JSON.stringify(formDataObj)  // json
                let Plaintext = _Qs.stringify(formDataObj)      // application/x-www-form-urlencoded

                var re = new RegExp(`(.{1,${msgSize}})`, "g");
                let PlaintextArray = Plaintext.match(re);

                // byte[][]
                let CipherBufferArray = PlaintextArray.map((el) => Uint8Array.from(hexToBytes(_encrypt.getKey().encrypt(el))));
                let bodyData = _MessagePack.encode(CipherBufferArray);

                
                let _RequestVerificationToken = $form.find('input[name="__RequestVerificationToken"]').val();
                debugger

                // 局部刷新问题:1、重定向跳转 2、表单校验信息显示

                formSubmitApi({
                    url: postUrl,
                    method: 'post',
                    data: bodyData,
                    transformRequest: [function (data, headers) {
                        // Do whatever you want to transform the data
                        headers['RequestVerificationToken'] = _RequestVerificationToken;
                        return data;
                    }],
                })
                    .then((data) => {
                        debugger
                    })
                    .catch((error) => {
                        debugger
                        if (error.response.status === 400) {
                            let customErrorMap = {};
                            for (let [key, value] of Object.entries(error.response.data)) {
                                let element = $form.find("[name='" + escapeCssMeta(key) + "']")[0]

                                if (element == undefined) {
                                    delete error.response.data[key]
                                    customErrorMap[key] = value
                                    console.log(value)
                                }
                            }
                            let formData = $form.data();
                            debugger
                            formData.validator.showErrors(error.response.data);
                            onSubmitCustomErrors.apply($form, [customErrorMap]);
                            return;
                        }
                    })
            })

        }
        _$(form).data(data_formEncryptionSubmit, formEncryptionSubmit);
    }

    $jQval.unobtrusive.EncryptedSubmit = {
        parse: function (selector) {
            var $selector = _$(selector),
                $forms = $selector.parents()
                    .addBack()
                    .filter(`form[${encryptedFormAttr}]`)
                    .add($selector.find(`form[${encryptedFormAttr}]`))
                    .has("[data-val=true]");

            $forms.each(function () {
                attachEncryptedForm(this);
            });
            $forms.submit(function (e) {
                event.preventDefault(); //阻止默认提交

                let form = $(this);
                form.data()[data_formEncryptionSubmit]();
            });
        }
    }

    $(function () {
        debugger
        $jQval.unobtrusive.EncryptedSubmit.parse(document);
    });

    debugger
    return $jQval.unobtrusive.EncryptedSubmit;
}));