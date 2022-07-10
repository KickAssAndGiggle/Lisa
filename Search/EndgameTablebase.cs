using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
namespace Lisa
{
    public sealed class EndgameTablebase
    {

        public struct TablebasePosition
        {
            public bool checkmate;
            public bool stalemate;
            public bool variant_win;
            public bool variant_loss;
            public bool insufficient_material;
            public int? wdl;
            public int? dtz;
            public int? dtm;
            public List<TablebaseMove> moves;
        }

        public struct TablebaseMove
        {
            public string uci;
            public string san;
            public bool zeroing;
            public bool checkmate;
            public bool stalemate;
            public bool variant_win;
            public bool variant_loss;
            public bool insufficient_material;
            public int? wdl;
            public int? dtz;
            public int? dtm;
        }

        public string FindBestMoveFrom7ManTablebase(string fen)
        {

            fen = fen.Replace(" ", "_");
            try
            {
                using (HttpClient cli = new())
                {
                    Task<string> resp = cli.GetStringAsync("http://tablebase.lichess.ovh/standard?fen=" + fen);
                    resp.Wait();
                    if (resp.IsCompletedSuccessfully && resp.Result != null && resp.Result != "")
                    {
                        TablebasePosition? TBP = (TablebasePosition?)JsonConvert.DeserializeObject(resp.Result, typeof(TablebasePosition));
                        if (TBP != null)
                        {
                            return TBP.Value.moves[0].uci;
                        }
                        else
                        {
                            return "";
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            catch
            {
                System.IO.StreamWriter sw = System.IO.File.AppendText("C:\\PSOutput\\Fen_TB_Misses.txt");
                sw.WriteLine("http://tablebase.lichess.ovh/standard?fen=" + fen);
                sw.Close();
                return "";
            }

        }


    }
}
