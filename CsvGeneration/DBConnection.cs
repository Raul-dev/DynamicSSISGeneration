/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration
{
    public class DBConnection
    {
        public string Server;
        string DataBase;
        string User;
        string Password;
        string AppType;
        bool IntegratedSecurity;

        public string SSISPackageName { get; set; }
        
        public DBConnection(string dataBase, string server = "localhost", string appType = "Landing", string user = "", string password="", bool integratedSecurity=true) {
            Server = server;
            DataBase = dataBase;
            User = user;
            Password = password;
            IntegratedSecurity = integratedSecurity;
            AppType = appType;

        }
        public void Copy(DBConnection obj)
        {
            User = obj.User;
            Password = obj.Password;
            IntegratedSecurity = obj.IntegratedSecurity;
            if(obj.SSISPackageName != null)
                SSISPackageName = obj.SSISPackageName;
            
        }

        public string GetName()
        {
            return GetConnectionName(DataBase, Server,  AppType);
        }
        static public string GetConnectionName(string dataBase, string server = "localhost", string appType = "Landing")
        {   
            return server + "_" + dataBase + "_" + appType;
        }
        public string GetADONetConnectionstring()
        {
            string con;
            if (IntegratedSecurity)
                con = string.Format("Data Source = {0}; Initial Catalog = {1}; Max Pool Size = 800; Connect Timeout = 300; Integrated Security = True;",Server,DataBase);
            else
                con = string.Format("Data Source = {0}; Initial Catalog = {1}; Max Pool Size = 800; Connect Timeout = 300; User ID = {2}; Integrated Security = False; Password={3}",Server,DataBase,User,Password);
            
            return con;
        }
        public string GetSqlNetConnectionstring()
        {
            string con;
            if (IntegratedSecurity)
                con = string.Format("Server ={0}; Database={1}; Trusted_Connection = True;",Server,DataBase);
            else
                con = string.Format("Server ={0}; Database={1}; Trusted_Connection = False; User ID = {2}; Password={3};",Server,DataBase,User,Password);
            return con;
        }

        public string GetOleDBConnectionstring()
        {
            string con;
            if (IntegratedSecurity)
                con = string.Format("Provider = SQLNCLI11.1; Auto Translate = False;ServerName = {0}; InitialCatalog = {1};",Server,DataBase);
            else
                con = string.Format("Provider = SQLNCLI11.1; Auto Translate = False; ServerName = {0}; InitialCatalog = {1}; UserName = {2}; Password = {3}",Server,DataBase,User,Password);
            return con;
        }
    }
}
