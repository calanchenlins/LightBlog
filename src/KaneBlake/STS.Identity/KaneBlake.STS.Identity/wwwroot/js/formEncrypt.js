const ApiService = axios.create({
    baseURL: '/',
    timeout: 30000
})
ApiService.interceptors.request.use(
    config => {
        debugger
        // axio对于data复杂对象默认使用Content-Type: application/json;
        config.data = Qs.stringify(config.data)
        config.headers['Content-type'] = 'application/x-www-form-urlencoded'
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

$("#loginbtn").click(function () {
    let formDataObj = Qs.parse($('#form1').serialize());
    debugger
    GetPublicKey().then((data) => {
        encrypt.setPublicKey('MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA83r9FJsrALyxfL4G9aSJOqwwpnkEVv1fheGuqpE2wE8t9FRtsg+EDqX6DKPLcAEGq9pv5EYmiYwuy/Swt1PmIQkgu0HzcMdC7V4agtLL+grBlMP58VZsb+cg6pxOcpvLS+EZ9L22lXLnfXsricPRjkdjoZ+7X+mdE9Zxln6j9s+JD6Hdw+GUxO2X3ukCoK7FSC+Hk+P9ETpy0yPxnwIN8cnqQLCYt0yLf144niex2G3nECw7Ky8nznjd5EWfqAXpKvIZEsvjofW4qSGDoUNXWpMJcDDL28KjO5dgDzkIJQjhix3UwSSwneyZuiq8QW9wMuQUcZuB1TAmTMbnCh+Pfav0Hij7WuxsT92nasrxdHABL6SmJInhxQ3/rWk4sem20cZohaarMGoiseCJNs9lA0SYUMvWyaY+A9QsmY+xI0eJj1YRKHtG4C8CYrZbmK80yXINOa7S32TfURXKocl1O6n6PLfusrCts5w45jjCDNLvva5WtOwky604YKGvMrlM1yS2PnMwkX1NBxrJ/50E81xnG1VSMgS07OoKJOowmnbY9f1O3myHlOh9BFvwfo9ciWraxPiVaddAesHVlXyfYa6E50HIEJ9aqZeyLMMbxzZB2Nrv8uV2E0Me2gyVzntgSu5ssAaRJCJVWdHVNwqeK11BaxGBWk6EtNJcRajOITMCAwEAAQ==');
        for (let [key, value] of Object.entries(formDataObj)) {
            if (key == '__RequestVerificationToken') {
                delete formDataObj[key]
            }
            else
            {
                formDataObj[key] = encrypt.encrypt(value)
            }
        }
        debugger
        ApiService({
            url: '/Account/Login',
            method: 'post',
            data: formDataObj
        })
            .then((data) => {
                debugger
            })
            .catch((err) => {
                debugger
            })
    })
    debugger
})