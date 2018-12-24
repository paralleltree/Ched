using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Ched.Configuration
{
    internal sealed class ApplicationSettings : SettingsBase
    {
        private static ApplicationSettings defaultInstance = (ApplicationSettings)Synchronized(new ApplicationSettings());

        private ApplicationSettings()
        {
        }

        public static ApplicationSettings Default => defaultInstance;

        [UserScopedSetting]
        [DefaultSettingValue("120")]
        public int UnitBeatHeight
        {
            get { return ((int)(this["UnitBeatHeight"])); }
            set { this["UnitBeatHeight"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool IsPreviewAbortAtLastNote
        {
            get { return ((bool)(this["IsPreviewAbortAtLastNote"])); }
            set { this["IsPreviewAbortAtLastNote"] = value; }
        }
    }
}
