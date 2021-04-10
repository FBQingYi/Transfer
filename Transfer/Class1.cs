using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSR;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Collections;
using Codeplex.Data;

namespace Transfer
{
    public class Program
    {
        static MCCSAPI mapi;
        static Dictionary<string, string> nameuuids = new Dictionary<string, string>();
        //格式化json
        public static string ConvertStringToJson(string response)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(response);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return response;
            }
        }

        //创建文件夹
        public static string aicQ()
        {
            string path = @"plugins\Transfer";
            string fileName = "transfer.json";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!File.Exists(path + "\\" + fileName))
            {
                string data = "{\"content\":\"服务器列表\",\"buttons\":[{\"image\":{\"type\":\"path\",\"data\":\"textures/items/book_writable.png\"},\"text\":\"XXX服务器\"},{\"image\":{\"type\":\"path\",\"data\":\"textures/items/iron_sword.png\"},\"text\":\"ZZZ服务器\"},{\"text\": \"§c关闭\"}],\"type\":\"form\",\"title\":\"跨服传送\"}";
                File.WriteAllText(@"plugins\Transfer\transfer.json", ConvertStringToJson(data));
                string data1 = "[{\"method\":\"cmd\",\"data\":\"第一个服务器ip\",\"cata\":\"端口\"},{\"method\":\"cmd\",\"data\":\"第二个服务器ip\",\"cata\":\"端口\"},{\"method\":\"custom_cmd\",\"data\":\".\",\"cata\":\".\"}]";
                File.WriteAllText(@"plugins\Transfer\transfer_cmd.json", ConvertStringToJson(data1));

            }
            return "ok";
        }

        // 获取uuid
        public static string getUUUD(string name)
        {
            string uuid;
            if (nameuuids.TryGetValue(name, out uuid))
            {
                return uuid;
            }
            nameuuids.Clear();
            string ols = mapi.getOnLinePlayers();
            if (!string.IsNullOrEmpty(ols))
            {
                var ser = new JavaScriptSerializer();
                ArrayList ol = ser.Deserialize<ArrayList>(ols);
                foreach (Dictionary<string, object> d in ol)
                {
                    object tname, tuuid;
                    if (d.TryGetValue("playername", out tname))
                    {
                        if (d.TryGetValue("uuid", out tuuid))
                        {
                            nameuuids[tname.ToString()] = tuuid.ToString();
                            if (tname.ToString() == name)
                            {
                                uuid = tuuid.ToString();
                            }
                        }
                    }
                }
            }
            return uuid;
        }

        public bool cshook(int rva, IntPtr hook, out IntPtr org)
        {
            IntPtr sorg = IntPtr.Zero;
            var ret = ccshook != null && ccshook(rva, hook, out sorg);
            org = sorg;
            return ret;
        }

        //主入口
        public static void init(MCCSAPI api)
        {
            mapi = api;
            api.setCommandDescribeEx("tr", "跨服面板", MCCSAPI.CommandPermissionLevel.GameMasters, (byte)MCCSAPI.CommandCheatFlag.NotCheat, 0);
            api.addBeforeActListener(EventKey.onInputCommand, x =>
            {
                var e = BaseEvent.getFrom(x) as InputCommandEvent;
                if (e.cmd.Trim() == "/tr")
                {
                    string s = File.ReadAllText(@"plugins\Transfer\transfer.json");
                    var uuid = getUUUD(e.playername);
                    var formid = api.sendCustomForm(uuid, s);
                    mapi.addBeforeActListener(EventKey.onFormSelect, x1 =>
                    {
                        var je = BaseEvent.getFrom(x1) as FormSelectEvent;
                        if (je.formid == formid)
                        {
                            mapi.removeBeforeActListener(EventKey.onFormSelect, c => { return true; });
                            if (je.selected != "[\"\",\"\"]" && je.selected != null)
                            {
                                if (je.selected != "null")
                                {
                                    string dt = File.ReadAllText(@"plugins\Transfer\transfer_cmd.json");
                                    var json = DynamicJson.Parse(dt);
                                    int age = Convert.ToInt32(je.selected);
                                    if (json[age]["method"] == "cmd")
                                    {
                                        if (json[age]["cata"] != "端口" && json[age]["cata"] != "" && json[age]["cata"] != " " && json[age]["cata"] != null)
                                        {
                                            string ip = json[age]["data"];
                                            int dk = Convert.ToInt32(json[age]["cata"]);
                                            api.transferserver(uuid, ip, dk);
                                        }
                                        else {
                                            api.sendText(uuid, "§l§4<----[Transfer]---->\n§l§4请修改配置文件！");
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    });
                    return false;
                }
                return true;
            });
            //聊天监听，处理命令方块；
            api.addAfterActListener(EventKey.onChat, e =>
            {
                var je = BaseEvent.getFrom(e) as ChatEvent;
                if (je.chatstyle == "title")
                {
                    if (je.msg != "" && je.msg != " ")
                    {
                        var uuid = getUUUD(je.playername);
                        string[] ss = je.msg.Split(' ');
                        if (ss[0] == "tr")
                        {
                            string ip = ss[1];
                            int dk = Convert.ToInt32(ss[2]);
                            api.transferserver(uuid, ip, dk);
                        }
                    }
                }
                return true;
            });

            aicQ();
        }
    }
}

namespace CSR
{
    partial class Plugin
    {
        private static MCCSAPI mapi = null;
        public static MCCSAPI api { get { return mapi; } }
        public static int onServerStart(string pathandversion)
        {
            string path = null, version = null;
            bool commercial = false;
            string[] pav = pathandversion.Split(',');
            if (pav.Length > 1)
            {
                path = pav[0];
                version = pav[1];
                commercial = (pav[pav.Length - 1] == "1");
                mapi = new MCCSAPI(path, version, commercial);
                if (mapi != null)
                {
                    onStart(mapi);
                    GC.KeepAlive(mapi);
                    return 0;
                }
            }
            Console.WriteLine("插件开始载入。。");
            return -1;
        }
        public static void onStart(MCCSAPI api)
        {
            Transfer.Program.init(api);
            Console.WriteLine("[CSR] [清漪花开]Transfer-->载入成功。版本：0.0.1");
        }
    }
}