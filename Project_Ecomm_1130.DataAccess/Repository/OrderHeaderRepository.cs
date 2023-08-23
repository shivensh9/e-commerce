﻿using Microsoft.AspNetCore.Builder.Internal;
using Project_Ecomm_1130.DataAccess.Data;
using Project_Ecomm_1130.DataAccess.Repository.IRepository;
using Project_Ecomm_1130.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Ecomm_1130.DataAccess.Repository
{
    public class OrderHeaderRepository:Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderHeaderRepository(ApplicationDbContext context)
            :base(context) 
        {
            _context = context; 
        }
    }
}
