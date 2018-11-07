using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.Plugins
{
    /// <summary>
    /// 譜面データを扱うプラグインを表します。
    /// </summary>
    public interface IScorePlugin : IPlugin
    {
        void Run(Score score);
    }
}
