using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using Ched.UI;

namespace Ched.Properties
{
    internal sealed class SoundConfiguration : ApplicationSettingsBase
    {
        private static SoundConfiguration defaultInstance = (SoundConfiguration)Synchronized(new SoundConfiguration());

        private SoundConfiguration()
        {
        }

        public static SoundConfiguration Default { get { return defaultInstance; } }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool HasUpgraded
        {
            get { return (bool)this["HasUpgraded"]; }
            set { this["HasUpgraded"] = value; }
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
    }
}
