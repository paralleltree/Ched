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
        public static SoundSettings Default { get; } = (SoundSettings)Synchronized(new SoundSettings());

        private SoundSettings()
        {
        }

        // ref: https://stackoverflow.com/a/12807699
        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        [DefaultSettingValue("")] // empty dictionary
        public Dictionary<string, SoundSource> ScoreSound
        {
            get { return (Dictionary<string, SoundSource>)this["ScoreSound"]; }
            set { this["ScoreSound"] = value; }
        }

        [UserScopedSetting]
        public SoundSource GuideSound
        {
            get => (SoundSource)this["GuideSound"] ?? new SoundSource("guide.mp3", 0.036);
            set => this["GuideSound"] = value;
        }
    }
}
