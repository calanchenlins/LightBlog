const ApiService = axios.create({
    baseURL: '/',
    timeout: 30000
})
ApiService.defaults.headers.post['Content-Type'] = 'application/x-msgpack'
ApiService.interceptors.request.use(
    config => {
        debugger
        //config.transformRequest = [function (data, headers) {
        //    // 对 data 进行任意转换处理
        //    let buffer = msgpack.encode(data);
        //    return buffer;
        //}];
        config.headers['form-encrypt'] = 'formrsaencrypt'
        config.headers['RequestVerificationToken'] = $('input[name="__RequestVerificationToken"]').val()
        return config
    },
    error => {
        debugger
        console.log('request err:' + error)
        return Promise.reject(error)
    }
)
ApiService.interceptors.response.use(
    response => {
        debugger
        return response.data // 等同于return Promise.resolve(response.data) 
    },
    error => { // 请求异常处理
        debugger
        console.log('response err:' + error)
        return Promise.reject(error) // catch中处理,如果直接返回错误,在then中处理
    }
)

const storage_CachePublickey = 'PublicKey'
const encrypt = new JSEncrypt({
    default_key_size: 4096
});

/**
 * 获取JWKS公匙
 */
const GetPublicKey = function () {
    const publicKey = localStorage.getItem(storage_CachePublickey)
    if (publicKey) {
        return Promise.resolve(publicKey)
    }
    return ApiService({
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
        return Promise.reject(err)// 抛出错误 catch处理
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


$("form[data-form-axios-encrypt=true]").submit(function (e) {
    event.preventDefault(); //阻止默认提交
    let form = $(this);
    let formDataObj = Qs.parse(form.serialize());
    if (!Object.keys(formDataObj).includes(document.activeElement.name)) {
        formDataObj[document.activeElement.name] = document.activeElement.value
    }
    let postUrl = form.attr('action') ?? document.baseURI
    debugger
    GetPublicKey().then((data) => {
        encrypt.setPublicKey('MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA83r9FJsrALyxfL4G9aSJOqwwpnkEVv1fheGuqpE2wE8t9FRtsg+EDqX6DKPLcAEGq9pv5EYmiYwuy/Swt1PmIQkgu0HzcMdC7V4agtLL+grBlMP58VZsb+cg6pxOcpvLS+EZ9L22lXLnfXsricPRjkdjoZ+7X+mdE9Zxln6j9s+JD6Hdw+GUxO2X3ukCoK7FSC+Hk+P9ETpy0yPxnwIN8cnqQLCYt0yLf144niex2G3nECw7Ky8nznjd5EWfqAXpKvIZEsvjofW4qSGDoUNXWpMJcDDL28KjO5dgDzkIJQjhix3UwSSwneyZuiq8QW9wMuQUcZuB1TAmTMbnCh+Pfav0Hij7WuxsT92nasrxdHABL6SmJInhxQ3/rWk4sem20cZohaarMGoiseCJNs9lA0SYUMvWyaY+A9QsmY+xI0eJj1YRKHtG4C8CYrZbmK80yXINOa7S32TfURXKocl1O6n6PLfusrCts5w45jjCDNLvva5WtOwky604YKGvMrlM1yS2PnMwkX1NBxrJ/50E81xnG1VSMgS07OoKJOowmnbY9f1O3myHlOh9BFvwfo9ciWraxPiVaddAesHVlXyfYa6E50HIEJ9aqZeyLMMbxzZB2Nrv8uV2E0Me2gyVzntgSu5ssAaRJCJVWdHVNwqeK11BaxGBWk6EtNJcRajOITMCAwEAAQ==');
        let msgSize = ((encrypt.getKey().n.bitLength() + 7) >> 3)-11;
        for (let [key, value] of Object.entries(formDataObj)) {
            if (key == '__RequestVerificationToken') {
                delete formDataObj[key]
            }
        }

        //let Plaintext = JSON.stringify(formDataObj)  // json
        let Plaintext = Qs.stringify(formDataObj)      // application/x-www-form-urlencoded

        var re = new RegExp(`(.{1,${msgSize}})`, "g");
        let PlaintextArray = Plaintext.match(re);

        // byte[][]
        let CipherBufferArray = PlaintextArray.map((el) => Uint8Array.from(hexToBytes(encrypt.getKey().encrypt(el))));
        let bodyData = MessagePack.encode(CipherBufferArray);

        debugger
        let _RequestVerificationToken = $('input[name="__RequestVerificationToken"]').val();


        // 局部刷新问题:1、重定向跳转 2、表单校验信息显示

        ApiService({
            url: postUrl,
            method: 'post',
            data: bodyData
        })
        .then((data) => {
            debugger
        })
        .catch((err) => {
            debugger
        })

        //fetch(postUrl,{
        //    method: 'POST',
        //    headers: {
        //        'Content-Type': 'application/x-msgpack',
        //        'form-encrypt': 'formrsaencrypt',
        //        'RequestVerificationToken': _RequestVerificationToken
        //    },
        //    body: bodyData
        //})
        //.then(data => console.log(data))
        //.catch(e => console.error(e));
    })
    return false;//阻止默认提交
});