using System;
using System.Collections.Generic;
using System.Text;

namespace OnAuth.LDAPUserStore
{
    public class LDAPUserStoreOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ServerAddress { get; set; }
    }
}
