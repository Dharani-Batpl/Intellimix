using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IntelliMix_Core.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }

}