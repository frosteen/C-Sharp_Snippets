using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//SELF-MADE CLASS

namespace Helper
{
    public class Firebase
    {
        string Link;

        //Firebase Constructor
        public Firebase(string firebaseURL)
        {
            Link = firebaseURL;
        }

        //Insert data in firebase
        public async Task Insert(string jsonName, dynamic jsonValue, string directory)
        {
            await Task.Run(()=> {
                if (Check.InternetConnection() != false)
                {
                    string json = "{\"" + jsonName.ToString() + "\":\"" + jsonValue.ToString() + "\"}";
                    WebRequest request = WebRequest.Create(Link + directory + "/.json");
                    request.ContentType = "application/json";
                    request.Method = "PATCH";
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    request.ContentLength = buffer.Length;
                    request.GetRequestStream().Write(buffer, 0, buffer.Length);
                    try
                    {
                        WebResponse response = request.GetResponse();
                        json = (new StreamReader(response.GetResponseStream())).ReadToEnd();
                        response.Close();
                    }
                    catch (System.Net.WebException error)
                    {
                        Message.Information("WebException Error: " + error.Message, "Information");
                        Application.Exit();
                    }
                }
                else
                {
                    Message.Error("No internet connection.", "Internet");
                    Application.Exit();
                }
            });
        }

        //Receive data in firebase
        public async Task<dynamic> Receive(string directory)
        {
            object values = null;
            await Task.Run(() => {
                if (Check.InternetConnection() != false)
                {
                    string firebaseURL = Link + directory + "/.json";
                    HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(firebaseURL);
                    request1.ContentType = "application/json: charset=utf-8";
                    HttpWebResponse response1 = request1.GetResponse() as HttpWebResponse;
                    using (Stream responsestream = response1.GetResponseStream())
                    {
                        StreamReader Read = new StreamReader(responsestream, Encoding.UTF8);
                        values = JsonConvert.DeserializeObject(Read.ReadToEnd());
                    }
                }
                else
                {
                    Message.Error("No internet connection.", "Internet");
                    Application.Exit();
                }
            });
            return values;
        }

        //Delete data in firebase
        public async Task Delete(string directory)
        {
            await Task.Run(()=> {
                if (Check.InternetConnection() != false)
                {
                    WebRequest request = WebRequest.Create(Link + directory + "/.json");
                    request.ContentType = "application/json";
                    request.Method = "DELETE";
                    WebResponse response = request.GetResponse();
                    response.Close();
                }
                else
                {
                    Message.Error("No internet connection.", "Internet");
                    Application.Exit();
                }
            });
        }
    }

    public class Check
    {
        //Check internet connection
        public static bool InternetConnection()
        {
            bool connection = NetworkInterface.GetIsNetworkAvailable();
            if (connection == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class String
    {
        //String replace
        public static string Replace(string text, string whatToReplace, string theReplace)
        {
            string modString = text.ToString();
            modString = modString.Replace(whatToReplace, theReplace);
            return modString;
        }

        //String split
        public static string[] Split(string stringToBeSplit, string[] splits)
        {
            string[] splitThisBes = stringToBeSplit.ToString().Split(splits, StringSplitOptions.RemoveEmptyEntries);
            return splitThisBes;
        }
    }

    public class Message
    {
        //Pops a default message
        public static void Default(string message, string caption)
        {
            MessageBox.Show(message, caption);
        }

        //Pops an information message
        public static void Information(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //Pops an error message
        public static void Error(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        //Pops a question message
        public static DialogResult Question(string message, string caption)
        {
            return MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.None);
        }

        //Pops a warning message
        public static void Warning(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public class EmbeddedAssembly
    {
        static Dictionary<string, Assembly> dic = null;
        public static void Load(string fileName)
        {
            if (dic == null)
                dic = new Dictionary<string, Assembly>();

            byte[] ba = null;
            Assembly asm = null;
            Assembly curAsm = Assembly.GetExecutingAssembly();

            using (Stream stm = curAsm.GetManifestResourceStream(System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "." + fileName))
            {
                if (stm == null)
                    throw new Exception(System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "." + fileName + " is not found in Embedded Resources.");

                ba = new byte[(int)stm.Length];
                stm.Read(ba, 0, (int)stm.Length);
                try
                {
                    asm = Assembly.Load(ba);

                    dic.Add(asm.FullName, asm);
                    return;
                }
                catch
                {
                }
            }

            bool fileOk = false;
            string tempFile = "";

            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                string fileHash = BitConverter.ToString(sha1.ComputeHash(ba)).Replace("-", string.Empty);

                tempFile = Path.GetTempPath() + fileName;

                if (File.Exists(tempFile))
                {
                    byte[] bb = File.ReadAllBytes(tempFile);
                    string fileHash2 = BitConverter.ToString(sha1.ComputeHash(bb)).Replace("-", string.Empty);

                    if (fileHash == fileHash2)
                    {
                        fileOk = true;
                    }
                    else
                    {
                        fileOk = false;
                    }
                }
                else
                {
                    fileOk = false;
                }
            }

            if (!fileOk)
            {
                System.IO.File.WriteAllBytes(tempFile, ba);
            }

            asm = Assembly.LoadFile(tempFile);

            dic.Add(asm.FullName, asm);
        }

        public static Assembly Get(string assemblyFullName)
        {
            if (dic == null || dic.Count == 0)
                return null;

            if (dic.ContainsKey(assemblyFullName))
                return dic[assemblyFullName];

            return null;
        }

        public static void Dll(string fileName)
        {
            Load(fileName);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Get(args.Name);
        }
    }

    public class Thread
    {
        public static void AddRow(Form f, DataGridView ctrl, string[] text)
        {
            if (ctrl.InvokeRequired)
            {
                AddRowCallBack method = new AddRowCallBack(AddRow);
                f.Invoke(method, new object[] { f, ctrl, text });
            }
            else
            {
                ctrl.Rows.Add(text);
            }
        }

        public static void AddValueToRow(Form f, DataGridView ctrl, int count, int cell, string value)
        {
            if (ctrl.InvokeRequired)
            {
                AddValueToRowCallBack method = new AddValueToRowCallBack(AddValueToRow);
                f.Invoke(method, new object[] { f, ctrl, count, cell, value });
            }
            else
            {
                ctrl.Rows[count].Cells[cell].Value = value;
            }
        }

        public static void AddItemsList(Form f, dynamic ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                AddItemsListCallBack method = new AddItemsListCallBack(AddItemsList);
                f.Invoke(method, new object[] { f, ctrl, text });
            }
            else
            {
                ctrl.Items.Add(text);
            }
        }

        public static void ClearItemsList(Form form, dynamic ctrl)
        {
            if (ctrl.InvokeRequired)
            {
                ClearItemsListCallBack method = new ClearItemsListCallBack(ClearItemsList);
                form.Invoke(method, new object[] { form, ctrl });
            }
            else
            {
                ctrl.Items.Clear();
            }
        }

        public static void ClearRow(Form f, DataGridView ctrl)
        {
            if (ctrl.InvokeRequired)
            {
                ClearRowCallBack method = new ClearRowCallBack(ClearRow);
                f.Invoke(method, new object[] { f, ctrl });
            }
            else
            {
                ctrl.Rows.Clear();
                ctrl.Refresh();
            }
        }

        //Read property in multithreading
        public static dynamic ReadProperty(Form f, dynamic varControl, string property)
        {
            if (varControl.InvokeRequired)
            {
                dynamic res = "";
                Action<dynamic> action = new Action<dynamic>(c =>
                res = varControl.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase).GetValue(varControl, null));
                f.Invoke(action, varControl);
                return res.ToString();
            }
            dynamic varText = varControl.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase).GetValue(varControl, null);
            return varText.ToString();
        }

        //Set property in multithreading
        public static void SetProperty(Form f, dynamic ctrl, string property, dynamic text)
        {
            if (ctrl.InvokeRequired)
            {
                SetPropertyCallBack method = new SetPropertyCallBack(SetProperty);
                f.Invoke(method, new object[] { f, ctrl, property, text });
            }
            else
            {
                ctrl.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase).SetValue(ctrl, text, null);

            }
        }

        private delegate void SetPropertyCallBack(Form f, dynamic ctrl, string property, dynamic text);

        private delegate void AddRowCallBack(Form f, DataGridView ctrl, string[] text);

        private delegate void AddItemsListCallBack(Form f, dynamic ctrl, string text);

        private delegate void ClearItemsListCallBack(Form f, dynamic ctrl);

        private delegate void AddValueToRowCallBack(Form f, DataGridView ctrl, int count, int cell, string value);

        private delegate void ClearRowCallBack(Form f, DataGridView ctrl);
    }
}
