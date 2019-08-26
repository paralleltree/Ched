using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using Ched.UI;

namespace Ched.Configuration
{
    internal sealed class SoundSettings : SettingsBase
    {
        private static SoundSettings defaultInstance = (SoundSettings)Synchronized(new SoundSettings());

        private SoundSettings()
        {
        }

        public static SoundSettings Default => defaultInstance;

        // ref: https://stackoverflow.com/a/12807699
        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")] // empty dictionary
        public Dictionary<string, SoundSource> ScoreSound
        {
            get { return (Dictionary<string, SoundSource>)this["ScoreSound"]; }
            set { this["ScoreSound"] = value; }
        }
    }
}
