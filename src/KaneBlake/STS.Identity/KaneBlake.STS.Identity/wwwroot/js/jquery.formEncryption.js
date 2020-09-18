// Unobtrusive formEncryptionSubmit support library for jQuery
(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
    } else if (typeof module === 'object' && module.exports) {
        // CommonJS-like environments that support module.exports     
    } else {
        // Browser global
        jQuery.validator.unobtrusive.EncryptedSubmit = factory(jQuery, JSEncrypt, MessagePack, axios, Qs);
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
        console.log("onSubmitCustomErrors")
        var container = _$(this).find("[data-valmsg-custom=true]"),
            list = container.find("ul");

        if (list && list.length && errorMap) {
            list.empty();

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
        const GetPublicKeyAsync = async function (refresh) {
            if (refresh) {
                localStorage.clear();
            }
            let publicKey = localStorage.getItem(storage_CachePublickey)
            if (publicKey) {
                return publicKey;
            }
            let response = await axios.get('/Account/GetPublicKey');
            //localStorage.setItem(storage_CachePublickey, response.data)
            return response.data;
        }

        const formSubmitApi = _axios.create({
            baseURL: '/',
            timeout: 30000
        })
        formSubmitApi.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest'; // tag XHR request
        formSubmitApi.defaults.headers.post['Content-Type'] = 'application/x-www-form-urlencoded' //application/x-msgpack
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
                if (response.status === 200) {
                    // redirect
                    if (response.data.statusCode === 3002) {
                        window.location.replace(response.data.message);
                        return;
                    }
                    // model validate failed
                    if (response.data.statusCode === 4000) {
                        let medelState = response.data.result;
                        let customErrorMap = {};
                        for (let [key, value] of Object.entries(medelState)) {
                            let element = $form.find("[name='" + escapeCssMeta(key) + "']")[0]

                            if (element == undefined) {
                                delete medelState[key]
                                customErrorMap[key] = value
                                console.log(value)
                            }
                        }
                        let formData = $form.data();
                        formData.validator.showErrors(medelState);
                        onSubmitCustomErrors.apply($form, [customErrorMap]);
                        return;
                    }
                    if (response.data.statusCode === 5000) {
                        return;
                    }
                }

                return response.data // 等同于 return Promise.resolve(response.data) 
            },
            error => {
                console.log('response err:' + error)
                return Promise.reject(error) // catch中处理,如果直接返回错误,在then中处理
            }
        )

        const formEncryptionSubmit = async function () {
            if (!$form.data().validator.form()) {
                return false;
            }

            let formDataObj = _Qs.parse($form.serialize());// exclude <button/></button> <input type='submit'/>
            if (!Object.keys(formDataObj).includes(document.activeElement.name)) {
                formDataObj[document.activeElement.name] = document.activeElement.value
            }
            let postUrl = $form.attr('action') ?? document.baseURI
            

            let publicKey = await GetPublicKeyAsync();
            debugger
            // public key format:  PKCS#8 
            // private key format: PKCS#1
            _encrypt.setPublicKey(publicKey);

            let msgSize = ((_encrypt.getKey().n.bitLength() + 7) >> 3) - 11;
            for (let [key, value] of Object.entries(formDataObj)) {
                if (key == '__RequestVerificationToken') {
                    delete formDataObj[key]
                }
            }

            // 表单序列化结果的值全为字符串,对于bool值,后端反序列化时报错
            // 1、后端自定义反序列化
            // 2、前端使用对象绑定表单元素,直接序列化对象
            //let Plaintext = JSON.stringify(formDataObj)  // application/json 
            let Plaintext = _Qs.stringify(formDataObj)     // application/x-www-form-urlencoded

            var re = new RegExp(`(.{1,${msgSize}})`, "g");
            let PlaintextArray = Plaintext.match(re);

            // byte[][]
            // 文本加密得到16进制字符串 长度1024
            //// 转base64字符串 长度 684
            //// 转byte[]，长度512
            let CipherBufferArray = PlaintextArray.map((el) => Uint8Array.from(hexToBytes(_encrypt.getKey().encrypt(el))));
            let bodyData = _MessagePack.encode(CipherBufferArray);

            let _RequestVerificationToken = $form.find('input[name="__RequestVerificationToken"]').val();

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
                    // clear localStorage when decryption failed on server side
                    localStorage.clear();
                    debugger
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
        
        $jQval.unobtrusive.EncryptedSubmit.parse(document);
    });

    
    return $jQval.unobtrusive.EncryptedSubmit;
}));