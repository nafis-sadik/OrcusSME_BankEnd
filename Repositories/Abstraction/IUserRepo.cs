﻿using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Abstraction
{
    public interface IUserRepo : IRepositoryBase<User>
    {
        public User FindUser(string userName, string pass);
    }
}