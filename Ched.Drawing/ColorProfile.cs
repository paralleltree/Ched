using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Drawing
{
    public class GradientColor
    {
        public Color DarkColor { get; set; }
        public Color LightColor { get; set; }

        public GradientColor(Color darkColor, Color lightColor)
        {
            DarkColor = darkColor;
            LightColor = lightColor;
        }
    }

    public class ColorProfile
    {
        public GradientColor BorderColor { get; set; }
        public GradientColor TapColor { get; set; }
        public GradientColor ExTapColor { get; set; }

        /// <summary>
        /// フリックを描画する際の<see cref="GradientColor"/>を指定します.
        /// Item1には背景色, Item2には前景色を指定します.
        /// </summary>
        public Tuple<GradientColor, GradientColor> FlickColor { get; set; }
        public GradientColor DamageColor { get; set; }
        public GradientColor HoldBackgroundColor { get; set; }
        public GradientColor HoldColor { get; set; }
        public GradientColor SlideBackgroundColor { get; set; }
        public GradientColor SlideColor { get; set; }
        public Color SlideLineColor { get; set; }
        public GradientColor AirActionColor { get; set; }
        public Color AirUpColor { get; set; }
        public Color AirDownColor { get; set; }
        public Color AirHoldLineColor { get; set; }
        public GradientColor AirStepColor { get; set; }

        public GradientColor GuideBackgroundColor { get; set; }
        public GradientColor GuideColor { get; set; }


        //ch別用
        public GradientColor InvBorderColor { get; set; }
        public GradientColor InvTapColor { get; set; }
        public GradientColor InvExTapColor { get; set; }
        public Tuple<GradientColor, GradientColor> InvFlickColor { get; set; }
        public GradientColor InvDamageColor { get; set; }
        public GradientColor InvHoldBackgroundColor { get; set; }
        public GradientColor InvHoldColor { get; set; }
        public GradientColor InvSlideBackgroundColor { get; set; }
        public GradientColor InvSlideColor { get; set; }
        public Color InvSlideLineColor { get; set; }
        public GradientColor InvAirActionColor { get; set; }
        public Color InvAirUpColor { get; set; }
        public Color InvAirDownColor { get; set; }
        public Color InvAirHoldLineColor { get; set; }
        public GradientColor InvAirStepColor { get; set; }
        public GradientColor InvGuideBackgroundColor { get; set; }
        public GradientColor InvGuideColor { get; set; }

    }
}
