using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FLib
{
    /// <summary>
    /// 自作時間計測クラス
    /// </summary>
    public static class FTimer
    {
        static Dictionary<string, Stopwatch> timerHist = new Dictionary<string,Stopwatch>();

        public static void Start(string id)
        {
            if (!timerHist.ContainsKey(id))
                timerHist[id] = new Stopwatch();
            timerHist[id].Restart();
        }

        public static void Pause(string id)
        {
            if (!timerHist.ContainsKey(id))
                timerHist[id] = new Stopwatch();
            timerHist[id].Stop();
        }

        public static void Resume(string id)
        {
            if (!timerHist.ContainsKey(id))
                timerHist[id] = new Stopwatch();
            timerHist[id].Start();
        }

        public static long ElapsedMilliseconds(string id)
        {
            if (!timerHist.ContainsKey(id))
                return -1;
            return timerHist[id].ElapsedMilliseconds;
        }
    }
}
