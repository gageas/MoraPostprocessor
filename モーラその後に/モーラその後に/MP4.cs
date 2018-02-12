using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Gageas.モーラその後に
{
    class MP4
    {
        private static UInt32 BEToUInt32(byte[] buf, int offset)
        {
            return ((UInt32)buf[0 + offset] << 24) + ((UInt32)buf[1 + offset] << 16) + ((UInt32)buf[2 + offset] << 8) + buf[3 + offset];
        }

        private static void UIn32ToBE(UInt32 num, byte[] buf, int offset)
        {
            buf[offset] = (byte)(num >> 24);
            buf[offset + 1] = (byte)(num >> 16);
            buf[offset + 2] = (byte)(num >> 8);
            buf[offset + 3] = (byte)(num);
        }

        public static ATOM Read(Stream strm, bool createImageObject = true)
        {
            List<KeyValuePair<string, object>> tag = new List<KeyValuePair<string, object>>();
            ATOM_nodeList rootAtom = new ATOM_nodeList();
            try
            {
                rootAtom.BuildFromStream(strm, strm.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
            return rootAtom;
        }

        public abstract class ATOM : IEnumerable<ATOM>
        {
            // nullのとき、親ノードの値を継承する
            protected List<ATOM> childNodes;
            public ATOM ParentNode = null;

            public abstract void BuildFromStream(Stream strm, long length);
            public abstract int WriteBodyToByteArray(Byte[] dest, int offset);
            public int WriteToByteArray(Byte[] dest, int offset)
            {
                bool isRoot = this.AtomCode == null; // このnodeListがルートかどうか
                int bodyLen = WriteBodyToByteArray(dest, offset + (isRoot ? 0 : 8));
                if (!isRoot)
                {
                    MP4.UIn32ToBE((UInt32)(bodyLen + 8), dest, offset);
                    dest[offset + 4] = AtomCode[0];
                    dest[offset + 5] = AtomCode[1];
                    dest[offset + 6] = AtomCode[2];
                    dest[offset + 7] = AtomCode[3];
                }
                return bodyLen + (isRoot ? 0 : 8);
            }

            public byte[] AtomCode = null;
            
            public ATOM()
            {
                this.childNodes = new List<ATOM>();
            }

            public T GetChildNode<T>() where T : ATOM
            {
                if (childNodes == null) return null;
                foreach (ATOM atom in childNodes)
                {
                    if (atom is T) return (T)atom;
                    T sub = atom.GetChildNode<T>();
                    if (sub != null) return sub;
                }
                return null;
            }

            public IEnumerable<T> GetChildNodes<T>() where T : ATOM
            {
                if (childNodes == null) return null;
                List<T> list = new List<T>();
                foreach (ATOM atom in childNodes)
                {
                    if (atom is T) list.Add((T)atom);
                    var sub = atom.GetChildNodes<T>();
                    list.AddRange(sub);
                }
                return list;
            }

            public void AddChild(ATOM node)
            {
                node.ParentNode = this;
                childNodes.Add(node);
            }

            public void RemoveChild(ATOM node)
            {
                if (node != null && childNodes.Contains(node))
                {
                    childNodes.Remove(node);
                }
            }

            IEnumerator<ATOM> IEnumerable<ATOM>.GetEnumerator()
            {
                return childNodes.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return childNodes.GetEnumerator();
            }
        }

        public class ATOM_nodeList : ATOM
        {
            private const int NODE_LIST_HEADER_SIZE = 8;
            private static readonly Regex linehead = new Regex(@"^", RegexOptions.Multiline);

            public ATOM_nodeList()
                : base()
            {
            }
            public override string ToString()
            {
                List<string> buf = new List<string>();
                foreach (var node in childNodes)
                {
                    buf.Add(linehead.Replace(node.GetType().Name + " : " + node.ToString(), "    "));
                }
                return this.GetType().Name + Environment.NewLine + String.Join(Environment.NewLine, buf.ToArray());
            }
            public override void BuildFromStream(Stream strm, long length)
            {
                long p = 0;

                while (p < length)
                {
                    byte[] header = new byte[NODE_LIST_HEADER_SIZE];
                    strm.Read(header, 0, NODE_LIST_HEADER_SIZE);
                    uint atom_size = BEToUInt32(header, 0) - NODE_LIST_HEADER_SIZE;
                    string atom_name = Encoding.ASCII.GetString(header, 4, 4);
                    p += atom_size + NODE_LIST_HEADER_SIZE;

                    long initial_pos = strm.Position;
                    ATOM atom = null;
                    switch (atom_name)
                    {
                        case "ftyp":
                            atom = new ATOM_ftyp();
                            break;
                        case "moov":
                            atom = new ATOM_moov();
                            break;
                        case "trak":
                            atom = new ATOM_moov();
                            break;
                        case "mdia":
                            atom = new ATOM_mdia();
                            break;
                        case "minf":
                            atom = new ATOM_minf();
                            break;
                        case "stbl":
                            atom = new ATOM_stbl();
                            break;
                        case "stco":
                            atom = new ATOM_stco();
                            break;
                        case "udta":
                            atom = new ATOM_udta();
                            break;
                        case "meta":
                            atom = new ATOM_meta();
                            break;
                        case "ilst":
                            atom = new ATOM_ilst();
                            break;
                        case "data":
                            atom = new ATOM_data();
                            break;
                        case "trkn":
                            atom = new ATOM_trkn();
                            break;
                        case "disk":
                            atom = new ATOM_disk();
                            break;
                        case "covr":
                            atom = new ATOM_covr();
                            break;
                        case "----":
                            atom = new ATOM_____();
                            break;
                        case "ID32":
                            atom = new ATOM_ID32();
                            break;
                        case "hdlr":
                            atom = new ATOM_hdlr();
                            break;
                        //                        case "name":
//                            atom = new ATOM_name();
//                            break;
//                        case "mean":
//                            atom = new ATOM_mean();
//                            break;
                        default:
                            switch (BitConverter.ToUInt32(header, 4)) // NOTICE: for Little Endian
                            {
                                case 0x545241A9: // .ART Artist
                                    atom = new ATOM__ART();
                                    break;
                                case 0x6D616EA9: // .nam Track
                                    atom = new ATOM__nam();
                                    break;
                                case 0x626C61A9: // .alb Album
                                    atom = new ATOM__alb();
                                    break;
                                case 0x6E6567A9: // .gen Genre
                                    atom = new ATOM__gen();
                                    break;
                                case 0x796164A9: // .dat Date
                                    atom = new ATOM__dat();
                                    break;
                                default:
                                    atom = new ATOM_raw();
//                                    Logger.Debug("There's no rule to read " + atom_name);
                                    break;
                            }
                            break;
                    }
                    try
                    {
                        if (atom != null)
                        {
                            atom.AtomCode = new byte[] { header[4], header[5], header[6], header[7]};
                            this.AddChild(atom);
                            atom.BuildFromStream(strm, atom_size);
                        }
                    }
                    finally
                    {
                        strm.Seek(initial_pos += atom_size, SeekOrigin.Begin);
                    }
                }
            }
            public override int WriteBodyToByteArray(byte[] dest, int offset)
            {
                int childLengthTotal = 0;
                foreach(var node in childNodes){
                    var len = node.WriteToByteArray(dest, offset + childLengthTotal);
                    childLengthTotal += len;
                }
                return childLengthTotal;
            }
        }

        private class ATOM_moov : ATOM_nodeList { }

        private class ATOM_trak : ATOM_nodeList { }

        private class ATOM_mdia : ATOM_nodeList { }

        private class ATOM_minf : ATOM_nodeList { }

        private class ATOM_stbl : ATOM_nodeList { }

        private class ATOM_udta : ATOM_nodeList { }

        private class ATOM_meta : ATOM_nodeList {
            public override void BuildFromStream(Stream strm, long length)
            {
                strm.Seek(4, SeekOrigin.Current);
                base.BuildFromStream(strm, (int)length - 4);
            }

            public override int WriteBodyToByteArray(byte[] dest, int offset)
            {
                dest[offset] = 0x00;
                dest[offset + 1] = 0x00;
                dest[offset + 2] = 0x00;
                dest[offset + 3] = 0x00;
                int childLengthTotal = 4;
                foreach (var node in childNodes)
                {
                    var len = node.WriteToByteArray(dest, offset + childLengthTotal);
                    childLengthTotal += len;
                }
                return childLengthTotal;
            }
        }

        public class ATOM_ilst : ATOM_nodeList { }

        public class ATOM_trkn : ATOM_nodeList { }

        public class ATOM_disk : ATOM_nodeList { }

        public class ATOM__ART : ATOM_nodeList { }

        public class ATOM__nam : ATOM_nodeList { }

        public class ATOM__alb : ATOM_nodeList { }

        public class ATOM__gen : ATOM_nodeList { }

        public class ATOM__dat : ATOM_nodeList { }

        public class ATOM_covr : ATOM_nodeList { }

        public class ATOM_____ : ATOM_nodeList { } // ハイフン4つ

        public class ATOM_raw : ATOM
        {
            public Byte[] data;
            public override void BuildFromStream(Stream strm, long length)
            {
                data = new byte[(int)length];
                strm.Read(data, 0, (int)length);
            }

            public override int WriteBodyToByteArray(byte[] dest, int offset)
            {
                System.Buffer.BlockCopy(data, 0, dest, offset, data.Length);
                return data.Length;
            }
        }

        public class ATOM_ftyp : ATOM_raw
        {
            public byte[] brands
            {
                get
                {
                    return data;
                }
                set
                {
                    if (value.Length < 8) return;
                    if (value.Length % 4 != 0) return;
                    this.data = value;
                }
            }
        }

        public class ATOM_data : ATOM_raw
        {
            public enum TYPE { TEXT, TRACKNUM, PICTURE, UNKNOWN };

            public TYPE getDataType()
            {
                switch (BitConverter.ToUInt64(this.data, 0))
                {
                    case 0: // trknとかdiskの場合。nn/mm形式の数値
                        return TYPE.TRACKNUM;
                    case 0x0000000001000000: // Text
                        return TYPE.TEXT;
                    case 0x000000000D000000: // Image
                    case 0x000000000E000000: // Image
                    case 0x0D0000000D000000: // Image moraで見つけた
                        return TYPE.PICTURE;
                    default:
                        return TYPE.UNKNOWN;
                }
            }

            public string getTextData()
            {
                var type = getDataType();
                if (type == TYPE.TEXT)
                {
                    return Encoding.UTF8.GetString(data, 8, data.Length - 8);
                }
                else if(type == TYPE.TRACKNUM)
                {
                    return ((data[10] << 8) + data[11]) + "/" + ((data[12] << 8) + data[13]);
                }
                return null;
            }

            public System.Drawing.Image getPictureData()
            {
                try
                {
                    return System.Drawing.Image.FromStream(new MemoryStream(data, 8, data.Length - 8));
                }
                catch
                {
                }
                return null;
            }
        }

        public class ATOM_ID32 : ATOM_raw { }

        public class ATOM_hdlr : ATOM_raw { }

        public class ATOM_stco : ATOM_raw {
            public void Adjust(int adjust)
            {
                if (this.data == null) return;
                int count = this.data.Length / 4 - 2;
                for (int i = 0; i < count; i++)
                {
                    var tmp = BEToUInt32(this.data, 8 + i * 4);
                    tmp = (UInt32)(tmp + adjust); // adjustはマイナスもあるので += (UInt32)adjustはだめ
                    UIn32ToBE(tmp, this.data, 8 + i * 4);                    
                }
            }
        }
    }
}
