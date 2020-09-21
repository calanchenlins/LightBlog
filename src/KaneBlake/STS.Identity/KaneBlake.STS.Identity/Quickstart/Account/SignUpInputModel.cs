using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity
{
    /// <summary>
    /// <see href="https://github.com/Jimmey-Jiang/Common.Utility/blob/master/src/Utility%E5%9F%BA%E7%A1%80%E7%B1%BB%E5%A4%A7%E5%85%A8_CN/%E5%90%84%E7%A7%8D%E9%AA%8C%E8%AF%81%E5%B8%AE%E5%8A%A9%E7%B1%BB/ValidatorHelper.cs"/>
    /// <see href="https://github.com/ldqk/Masuit.Tools/tree/master/Masuit.Tools.Abstractions/Validator"/>
    /// </summary>
    public class SignUpInputModel: IValidatableObject
    {
        [Required]
        [Remote(action: "VerifyUserName", controller: "Account")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "'{0}' and '{1}' do not match.")]
        public string ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string pwd = Password;
            var regex = new Regex(@"(?=.*[0-9])                             #必须包含数字
                                            (?=.*[a-z])                     #必须包含小写字母
                                            (?=.*[A-Z])                     #必须包含大写字母
                                            (?=([\x21-\x7e]+)[^a-zA-Z0-9])  #必须包含特殊符号
                                            .{12,30}                        #至少6个字符，最多30个字符
                                            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            if (!regex.Match(pwd).Success)
            {
                yield return new ValidationResult(
                    $"密码强度值不够，密码必须包含数字，必须包含小写和大写字母，必须包含至少一个特殊符号，至少12个字符，最多30个字符！",
                    new[] { nameof(Password) });
            }
        }
    }
}
