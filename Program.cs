using System;
using System.IO;
using System.Linq;
using System.Net;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace orthancReplicator
{
    class Program
    {
        private static string basedir = "Y:\\";
        private static string orthancip = "192.168.101.46";
        private static int orthancport = 4242;
        private static string logfilename;
        private static bool IsDICOMfile(string FileName)
        {
            String fame = FileName.Substring(FileName.LastIndexOf('\\') + 1);
            if (FileName.LastIndexOf("DICOMDIR", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            FileStream fs = new FileStream(FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
            byte[] dba = new byte[4];
            fs.Seek(128, SeekOrigin.Begin);
            //Read the following 4 dba
            fs.Read(dba, 0, 4);
            fs.Close();
            //Compare to 'DICM'
            if (dba.SequenceEqual(new byte[4] { 68, 73, 67, 77 }))
            {
                return true;
            }
            return false;
        }
        
        private static async Task<string> uploadDirmulti(string dirname)
        {
            if (Directory.Exists(dirname))
            {
                var client = DicomClientFactory.Create(orthancip, orthancport, false, "RADIANT", "ORTHANC");
                client.NegotiateAsyncOps();
                var txtFiles = Directory.EnumerateFiles(dirname, "*", SearchOption.AllDirectories);
                String errorMessage = "";
                int faulty = 0;
                List<DicomCStoreRequest> dicomfiles = new List<DicomCStoreRequest>();
                foreach (string currentFile in txtFiles)
                {
                    if (IsDICOMfile(currentFile))
                    {
                        dicomfiles.Add(new DicomCStoreRequest(currentFile));
                    }
                    else { faulty++; }
                }
                try
                {
                    await client.AddRequestsAsync(dicomfiles);
                    await client.SendAsync();
                }
                catch (Exception sa)
                {
                    errorMessage = sa.Message;
                }
                if (errorMessage != "")
                    return errorMessage;
                else
                    return dirname + "," + txtFiles.Count().ToString() + " OK," + faulty.ToString() + " F";
            }
            else
            {
                return dirname + " EMPTY";
            }
        }
        private static void httpUploadfile(string filename)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.ContentType,"application/dicom");
                wc.Headers.Add(HttpRequestHeader.Authorization, "Basic "+Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("ibs:ibs")));
                wc.UploadFile("http://192.168.101.46:443/instances","POST",filename);
            }
        }
        private static void uploadDirHttp(string dirname)
        {
            if (Directory.Exists(dirname))
            {
                var txtFiles = Directory.EnumerateFiles(dirname, "*", SearchOption.AllDirectories);
                foreach (string currentFile in txtFiles)
                {
                    httpUploadfile(currentFile);
                }
            }
        }
        static void Main(string[] args)
        {
            logfilename = "log_" + DateTime.Now.ToString("ddMMyyyy_hhmm");
            Console.WriteLine("Logfile will be: " + logfilename);
            Console.WriteLine("Copy started..");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Task<String> k = uploadDirmulti("di");
            //uploadDirHttp("Y:\\3C\\06");
            k.Wait();
            Console.WriteLine(k.Result);
            stopwatch.Stop();
            Console.WriteLine("Copy took " + stopwatch.Elapsed.ToString() + " ms..");
        }
    }
}
//dotnet publish -c release -r linux-x64 /p:PublishSingleFile=true