using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AvCoverDownloader
{
    class Collector
    {
        public EventHandler<String> CollectorLog;
        public EventHandler<String> ErrorLog;
        SynchronizationContext _syncContext;

        private int errName = 0;

        private string _path;
        
        public Collector(SynchronizationContext formContext, string path)
        {
            _path = path;
            _syncContext = formContext;
        }

        public void Start()
        {
            //client = new HttpClient();
            new Thread(Collect).Start();
        }

        public void Collect()
        {
            _syncContext.Post(OutLog, "开始");
            Director(_path);
            _syncContext.Post(OutLog, "结束");
            _syncContext.Post(OutLog, "修改数量:" + errName);
        }//method 


        void Director(string dir)
        {
            DirectoryInfo fdir = new DirectoryInfo(dir);
            FileSystemInfo[] fsinfos = null;
            try
            {
                fsinfos = fdir.GetFileSystemInfos();
            }
            catch 
            {   
                try
                {
                    fdir = new DirectoryInfo("\\\\?\\" + dir);
                    fsinfos = fdir.GetFileSystemInfos();
                }
                catch {
                    _syncContext.Post(OutLog, "Error(" + dir + ")");
                    return;
                }
            }
            


            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if ((fsinfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden && (fsinfo.Attributes & FileAttributes.System) == FileAttributes.System)
                {
                    //_syncContext.Post(OutLog, "跳过" + fsinfo.FullName);
                    continue;
                }

                if (fsinfo is DirectoryInfo)     //判断是否为文件夹
                    Director(fsinfo.FullName);   //递归调用
                
                rename(fsinfo);
            }//foreach


        }//method



        public void rename(FileSystemInfo fsinfo) {
            string oldName = fsinfo is DirectoryInfo ? fsinfo.Name : Path.GetFileNameWithoutExtension(fsinfo.FullName);

            //去除不可见字符
            string newName = Regex.Replace(oldName, "\\p{C}", "");
            //去除所有 Emoji
            newName = Regex.Replace(newName, @"(\ud83c[\udf00-\udfff])|(\ud83d[\udc00-\ude4f\ude80-\udeff])|[\u2600-\u2B55]", "");
            newName = newName.Replace("\\", "＼");
            newName = newName.Replace("/", "／");
            newName = newName.Replace(":", "：");
            newName = newName.Replace("*", "＊");
            newName = newName.Replace("!", "！");
            newName = newName.Replace("?", "？");
            newName = newName.Replace("<", "〈");
            newName = newName.Replace(">", "〉");
            newName = newName.Replace("+", "＋");
            newName = newName.Replace("-", "－");
            newName = newName.Replace("#", "＃");
            newName = newName.Replace("$", "￥");
            newName = newName.Replace("%", "％");
            newName = newName.Replace("&", "＆");
            newName = newName.Replace("\"", "＂");
            newName = newName.Replace("'", "＇");
            newName = newName.Replace("(", "（");
            newName = newName.Replace(")", "）");
            newName = newName.Replace(".", "．");
            newName = Regex.Replace(newName, @"\s", " ");
            //去除名称开头结尾的空格
            //结尾有空格会触发 文件系统 BUG
            newName = newName.Trim();

            if (!newName.Equals(oldName))
            {

                try
                {
                    if (fsinfo is DirectoryInfo)
                        Directory.Move(fsinfo.FullName, Path.GetDirectoryName(fsinfo.FullName) + "\\" + newName);
                    else
                        File.Move(fsinfo.FullName, Path.GetDirectoryName(fsinfo.FullName) + "\\" + newName + Path.GetExtension(fsinfo.FullName));
                    _syncContext.Post(OutError, "(" + oldName + ") -> (" + newName + ")");
                }
                catch {
                    try
                    {
                        if (fsinfo is DirectoryInfo)
                            Directory.Move("\\\\?\\" + fsinfo.FullName, "\\\\?\\" + Path.GetDirectoryName(fsinfo.FullName) + "\\" + newName);
                        else
                            File.Move("\\\\?\\" + fsinfo.FullName, "\\\\?\\" + Path.GetDirectoryName(fsinfo.FullName) + "\\" + newName + Path.GetExtension(fsinfo.FullName));
                    }
                    catch 
                    {
                        _syncContext.Post(OutLog, fsinfo.FullName + " ERROR(" + oldName + ") -> (" + newName + ")");
                    }
                }
                
                errName++;
            }
            
            
        }// method


        private void Output(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                Console.WriteLine(output.Data);
            }
        }

        private void OutLog(object state)
        {
            CollectorLog?.Invoke(this, state.ToString());
        }

        private void OutError(object state)
        {
            ErrorLog?.Invoke(this, state.ToString());
        }

    }//class
}
