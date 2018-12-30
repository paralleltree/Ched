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
        void Run(IScorePluginArgs args);
    }

    /// <summary>
    /// <see cref="IScorePlugin"/>の実行時に渡される情報を表します。
    /// </summary>
    public interface IScorePluginArgs
    {
        Score GetCurrentScore();
        void UpdateScore(Score score);
    }
}
