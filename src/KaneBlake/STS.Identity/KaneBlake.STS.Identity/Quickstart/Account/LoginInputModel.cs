// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.ComponentModel.DataAnnotations;

namespace KaneBlake.STS.Identity
{
    public class LoginInputModel
    {
        [Required]
        [Display(Name = "UserName")]
        public string UserName { get; set; }
        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }
        [Display(Name = "Remember My Login")]
        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
    }
}