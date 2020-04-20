using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity
{
    public class SignUpViewModel: SignUpInputModel
    {
        public string InvalidInfo { get; set; } = "";
    }
}
