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

        public string FindBestMoveFrom7ManTablebase(string Fen)
        {

            Fen = Fen.Replace(" ", "_");
            try
            {
                using (WebClient Cli = new())
                {
                    string JSON = Cli.DownloadString("http://tablebase.lichess.ovh/standard?fen=" + Fen);
                    TablebasePosition TBP = (TablebasePosition)JsonConvert.DeserializeObject(JSON, typeof(TablebasePosition));
                    return TBP.moves[0].uci;
                }
            }
            catch
            {
                System.IO.StreamWriter SW = System.IO.File.AppendText("C:\\PSOutput\\Fen_TB_Misses.txt");
                SW.WriteLine("http://tablebase.lichess.ovh/standard?fen=" + Fen);
                SW.Close();
                return "";
            }

        }


    }
}
