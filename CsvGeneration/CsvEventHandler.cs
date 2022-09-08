/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/

using System;
using Microsoft.SqlServer.Dts.Runtime;
using Serilog;
namespace DynamicCsvGeneration
{
    public class CsvEventHandler : DefaultEvents
    {
        public override bool OnError(DtsObject source, int errorCode, string subComponent, string description, string helpFile, int helpContext, string idofInterfaceWithError)
        {
            Log.Error(subComponent + ": " + description.Substring(0, description.Length - 2));
            return true;
        }

        public override void OnInformation(DtsObject source, int informationCode, string subComponent, string description, string helpFile, int helpContext, string idofInterfaceWithError, ref bool fireAgain)
        {
            Log.Information(subComponent +": "+ description.Substring(0, description.Length -2));
        }
    }
}
