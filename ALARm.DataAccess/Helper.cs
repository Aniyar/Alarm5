﻿using System.Configuration;
using Microsoft.Extensions.Configuration;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

namespace ALARm.DataAccess
{
    public static class Helper
    {

        public static string ConnectionString()
        {
            try
            {
                var configBuilder = new ConfigurationBuilder();
                var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                configBuilder.AddJsonFile(path, false);
                var root = configBuilder.Build();
                var appSetting = root.GetSection("ConnectionStrings:DefaultConnection");
                return appSetting.Value;
            }
            catch
            {
                return ConfigurationManager.ConnectionStrings["cn"].ConnectionString;
            }
        }

       
    }
}
