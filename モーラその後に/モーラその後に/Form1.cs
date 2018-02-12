using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Gageas.モーラその後に
{
    public partial class Form1 : Form
    {
        private Queue<string> queue = new Queue<string>();
        private static readonly byte[] M4A_FTYP_BRANDS;
        private System.Threading.Thread th;
        private readonly Object insleepLock = new Object(); 

        static Form1()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(Encoding.ASCII.GetBytes("M4A "), 0, 4);
                ms.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
                ms.Write(Encoding.ASCII.GetBytes("M4A "), 0, 4);
                ms.Write(Encoding.ASCII.GetBytes("mp42"), 0, 4);
                ms.Write(Encoding.ASCII.GetBytes("isom"), 0, 4);
                M4A_FTYP_BRANDS = ms.ToArray();
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void AppendLog(string str){
            this.Invoke((MethodInvoker)(() => {
                textBox1.AppendText(str + System.Environment.NewLine);
            }));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (System.Environment.GetCommandLineArgs().Length > 1)
            {
                StartWork(System.Environment.GetCommandLineArgs().Skip(1).ToArray());
            }

            th = new System.Threading.Thread(() => {
                while (true)
                {
                    string filename = null;
                    if (queue.Count > 0)
                    {
                        lock (queue)
                        {
                            filename = queue.Dequeue();
                        }
                    }
                    else
                    {
                        try
                        {
                            lock (insleepLock)
                            {
                                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                            }
                        }
                        catch (System.Threading.ThreadInterruptedException) { }
                    }
                    if (filename == null) continue;
                    try
                    {
                        if (Directory.Exists(filename))
                        {
                            var mp4files = Directory.GetFiles(filename, "*.mp4", SearchOption.AllDirectories);
                            lock (queue)
                            {
                                foreach(var f in mp4files){
                                    queue.Enqueue(f);
                                }
                            }
                            continue;
                        }
                        AppendLog(filename);
                        bool ret = FixMoraMP4(filename);
                        AppendLog((ret ? "[OK]" : "[NG]") + System.Environment.NewLine);
                        this.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        AppendLog(ex.Message);
                    }
                }
            });
            th.IsBackground = true;
            th.Start();
        }

        private static string getTagText<T>(MP4.ATOM root) where T : MP4.ATOM
        {
            var node = root.GetChildNode<T>();
            if (node == null) return null;
            var data = (MP4.ATOM_data)node.First();
            if (data == null) return null;
            return data.getTextData();
        }

        private void CopyID32DataToMP4(ID3V2Tag.id3tag id3tag, MP4.ATOM mp4)
        {
            if (id3tag == null) return;
            var geob = id3tag.frame.Find(_ => _.id == "GEOB" && _.geob_mime.StartsWith("image/"));
            if ((geob == null) || (geob.geob_binarydata == null)) return;
            
            var covrdata = new MP4.ATOM_raw();
            covrdata.AtomCode = Encoding.ASCII.GetBytes("data");
            covrdata.data = new byte[geob.geob_binarydata.Length + 8];
            covrdata.data[0] = 0x00;
            covrdata.data[1] = 0x00;
            covrdata.data[2] = 0x00;
            covrdata.data[3] = 0x0D;
            covrdata.data[4] = 0x00;
            covrdata.data[5] = 0x00;
            covrdata.data[6] = 0x00;
            covrdata.data[7] = 0x00;
            Buffer.BlockCopy(geob.geob_binarydata, 0, covrdata.data, 8, geob.geob_binarydata.Length);

            var covratom = new MP4.ATOM_covr();
            covratom.AtomCode = Encoding.ASCII.GetBytes("covr");

            var covr = mp4.GetChildNode<MP4.ATOM_covr>();
            if (covr == null)
            {
                covr = new MP4.ATOM_covr();
                covr.AtomCode = Encoding.ASCII.GetBytes("covr");
                var ilst = mp4.GetChildNode<MP4.ATOM_ilst>();
                if (ilst == null) return;
                ilst.AddChild(covr);
            }
            covr.AddChild(covrdata);

            AppendLog("  歌詞画像をコピーしました");
        }

        private bool FixMoraMP4(string filepath)
        {
            if (!File.Exists(filepath)) return false;
            var destfilepath = (System.IO.Path.GetExtension(filepath).ToLower() == ".m4a")
                ? filepath + ".m4a"
                : System.IO.Path.ChangeExtension(filepath, ".m4a");

            using (var fs = System.IO.File.OpenRead(filepath))
            {
                try
                {
                    // 元のファイルサイズを保持
                    var originalsize = fs.Length;

                    // mp4の読み出し
                    var mp4 = MP4.Read(fs, true);

                    // トラック名を表示
                    var title = getTagText<MP4.ATOM__nam>(mp4);
                    var artist = getTagText<MP4.ATOM__ART>(mp4);
                    AppendLog(String.Format("  {0} - {1}", title ?? "?", artist ?? "?"));

                    // brandを修正
                    mp4.GetChildNode<MP4.ATOM_ftyp>().brands = M4A_FTYP_BRANDS;

                    // id32を探す
                    var id32atom = mp4.GetChildNode<MP4.ATOM_ID32>();
                    if (id32atom != null)
                    {
                        var id32body = new byte[id32atom.data.Length - 6];
                        System.Buffer.BlockCopy(id32atom.data, 6, id32body, 0, id32atom.data.Length - 6);
                        ID3V2Tag.id3tag id3tag = null;
                        try
                        {
                            id3tag = ID3V2Tag.read_id3tag(new MemoryStream(id32body), false);
                        }
                        catch {
                            AppendLog("ID32領域の内容が読めませんでした。が処理を続行します。");
                        }

                        // ID3から使えるデータを引っこ抜く
                        if (id3tag != null)
                        {
                            CopyID32DataToMP4(id3tag, mp4);
                        }
                        
                        // ID32の親から、hdlrとID32を削除する
                        var parent = id32atom.ParentNode;
                        parent.RemoveChild(parent.GetChildNode<MP4.ATOM_hdlr>());
                        parent.RemoveChild(id32atom);

                        // ID32の入っていたmetaが空になっているなら、それも削除
                        if (parent.GetChildNodes<MP4.ATOM>().Count() == 0)
                        {
                            parent.ParentNode.RemoveChild(parent);
                        }
                    }

                    using (var fsw = System.IO.File.OpenWrite(destfilepath))
                    {
                        var buf = new byte[fs.Length + M4A_FTYP_BRANDS.Length];
                        // 一旦編集後のmp4をバッファに書き出してみる
                        var len = mp4.WriteToByteArray(buf, 0);

                        // stco atomのシークテーブルを補正
                        var stcos = mp4.GetChildNodes<MP4.ATOM_stco>();
                        foreach (var stco in stcos)
                        {
                            stco.Adjust((int)(len - originalsize));
                        }

                        // mp4を生成しなおしてファイルに書く
                        len = mp4.WriteToByteArray(buf, 0);
                        fsw.Write(buf, 0, len);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog(ex.StackTrace);
                    return false;
                }
            }
            return true;
        }

        private void StartWork(String[] filenames)
        {
            lock (queue)
            {
                foreach (var filename in filenames)
                {
                    queue.Enqueue(filename);
                }
            }
            if (System.Threading.Monitor.TryEnter(insleepLock))
            {
                System.Threading.Monitor.Exit(insleepLock);
            }
            else
            {
                th.Interrupt();
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var contents = e.Data.GetData(DataFormats.FileDrop);
            if(contents == null)return;
            var filenames = (String[])contents;
            this.Invoke((MethodInvoker)(() => { StartWork(filenames); }));
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/gageas");
        }
    }
}
