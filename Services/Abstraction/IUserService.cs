﻿using Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Abstraction
{
    public interface IUserService
    {
        public bool? SignUp(UserModel user, out string token);
        public bool? LogIn(string userName, string pass, out string token);
        public bool ArchiveAccount(string userId);
        public bool DeleteAccount(string userId);
        public bool ResetPassword(string userId);
    }
}
