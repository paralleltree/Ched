using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Ched.Core
{
    public class Constants
    {
        public static int LanesCount = int.Parse(ConfigurationManager.AppSettings["LanesCount"]);
        public static int MLanesCount = int.Parse(ConfigurationManager.AppSettings["MinusLanesCount"]);//必ずマイナス(か0)

        /// <summary>
        /// レーンの数を設定します。
        /// </summary>
        public int LaneCount
        {
            get { return LanesCount; }
            set
            {
                LanesCount = value;
            }
        }

        /// <summary>
        /// マイナス方向のレーンの数を設定します。
        /// </summary>
        public int MinusLaneCount
        {
            get { return MLanesCount; }
            set
            {
                MLanesCount = value;
            }
        }

    }
}
