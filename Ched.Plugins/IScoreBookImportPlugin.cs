using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.Plugins
{
    /// <summary>
    /// 譜面データのインポートを行うプラグインを表します。
    /// </summary>
    public interface IScoreBookImportPlugin : IPlugin
    {
        string FileFilter { get; }
        ScoreBook Import(TextReader reader);
    }
}
