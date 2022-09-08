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
using System.IO;
using Newtonsoft.Json;
using System.IO.Compression;
/*
1) Install-Package Google.Apis.Storage.v1 -Version 1.57.0.2647
2) Added nuget Google.Cloud.Storage.v1  version 2.40
using Object = Google.Apis.Storage.v1.Data.Object;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
*/
namespace DynamicCsvGeneration
{
    public class FileHelper
    {
        public FileHelper()
        {

        }
        public string[]  GetFilesFromDir(string directoryPath)
        {
            string[] filePaths = Directory.GetFiles(directoryPath);
            return filePaths;
        }
        public static void CompressFile(string originalFileName, string compressedFileName = "")
        {
            compressedFileName = (string.IsNullOrEmpty(compressedFileName) ) ? originalFileName + ".zip" : compressedFileName;
            using (FileStream zipToOpen = new FileStream(compressedFileName, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    archive.CreateEntryFromFile( originalFileName, Path.GetFileName(originalFileName));
                }
            }
        }
        /* need to add Added nuget Google.Cloud.Storage.v1 2.400
        public static void UploadFile(string bucketName = "your-unique-bucket-name",
        string localPath = "my-local-path/my-file-name",
        string objectName = "my-file-name")
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"C:\Project\dev-data-platform-342813-d99ede8d31df_averianov.json");
            //var credential = GoogleCredential.GetApplicationDefault();
            //$env: GOOGLE_APPLICATION_CREDENTIALS = "C:\Users\username\Downloads\service-account-file.json"
            //var storage = StorageClient.Create();
            
            var storage = StorageClient.Create();
            //var bkt = storage.GetBucket("dbprj-landing-dev");
            using (var fileStream = File.OpenRead(localPath))
            {
                storage.UploadObject("dbprj-landing-dev", "rescentral-csv/" + objectName, null, fileStream);
                Console.WriteLine($"Uploaded {objectName}.");
            }
        }
        */
        public static void CompressGzipFile(string originalFileName, string compressedFileName = "")
        {
            compressedFileName = (string.IsNullOrEmpty(compressedFileName)) ? originalFileName + ".zip" : compressedFileName;
            FileStream originalFileStream = File.Open(originalFileName, FileMode.Open);
            FileStream compressedFileStream = File.Create(compressedFileName);
            var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
            originalFileStream.CopyTo(compressor);
            originalFileStream.Close();
            compressedFileStream.Close();
        }
        public static void DecompressGzipFile(string compressedFileName)
        {
            string decompressedFileName = compressedFileName.Replace(".zip", "");
            FileStream compressedFileStream = File.Open(compressedFileName, FileMode.Open);
            FileStream outputFileStream = File.Create(decompressedFileName);
            var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            decompressor.CopyTo(outputFileStream);
        }

        public static void SaveText(string script, string filename, string path)
        {
            filename = path + filename;
            File.WriteAllText(filename, script);
        }
        public static void SaveJson(string ljson, string filename, string path)
        {
            filename = path + filename;
            string str = Directory.GetCurrentDirectory();
            bool b = File.Exists(filename);
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializerf = new JsonSerializer();
                serializerf.Serialize(file, ljson);
            }
        }
        public static void CopyUTF16ToUTF8(string sourcefilename, string targetfilename)
        {
            string sLine = null;
            bool b = File.Exists(sourcefilename);
            using (StreamReader sfile = File.OpenText(sourcefilename))
            {
                using (StreamWriter tfile = File.CreateText(targetfilename))
                {
                    while (!string.IsNullOrEmpty(sLine = sfile.ReadLine()))
                    {
                        tfile.WriteLine(Utf16ToUtf8(sLine));
                    }
                }
            }
        }
        //Example:
        //processCMD("bcp", String.Format("\"select 'COUNTRY', 'MONTH' union all SELECT COUNTRY, cast(MONTH as varchar(2)) FROM DBName.dbo.TableName\" queryout {0}FILE_NAME.csv -c -t; -T -{1}", ExportDirectory, SQLServer));
        static void ProcessCMD(string fileName, string arguments)
        {

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.Arguments = arguments;
            proc.Start();
            proc.WaitForExit();

        }
        public static string Utf16ToUtf8(string utf16String)
        {
            /**************************************************************
             * Every .NET string will store text with the UTF16 encoding, *
             * known as Encoding.Unicode. Other encodings may exist as    *
             * Byte-Array or incorrectly stored with the UTF16 encoding.  *
             *                                                            *
             * UTF8 = 1 bytes per char                                    *
             *    ["100" for the ansi 'd']                                *
             *    ["206" and "186" for the russian 'κ']                   *
             *                                                            *
             * UTF16 = 2 bytes per char                                   *
             *    ["100, 0" for the ansi 'd']                             *
             *    ["186, 3" for the russian 'κ']                          *
             *                                                            *
             * UTF8 inside UTF16                                          *
             *    ["100, 0" for the ansi 'd']                             *
             *    ["206, 0" and "186, 0" for the russian 'κ']             *
             *                                                            *
             * We can use the convert encoding function to convert an     *
             * UTF16 Byte-Array to an UTF8 Byte-Array. When we use UTF8   *
             * encoding to string method now, we will get a UTF16 string. *
             *                                                            *
             * So we imitate UTF16 by filling the second byte of a char   *
             * with a 0 byte (binary 0) while creating the string.        *
             **************************************************************/

            // Storage for the UTF8 string
            string utf8String = String.Empty;

            // Get UTF16 bytes and convert UTF16 bytes to UTF8 bytes
            byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf16String);
            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);

            // Fill UTF8 bytes inside UTF8 string
            for (int i = 0; i < utf8Bytes.Length; i++)
            {
                // Because char always saves 2 bytes, fill char with 0
                byte[] utf8Container = new byte[2] { utf8Bytes[i], 0 };
                utf8String += BitConverter.ToChar(utf8Container, 0);
            }

            return utf8String;
        }

    }
}
