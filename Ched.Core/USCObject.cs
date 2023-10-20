using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core
{
    public class USCObject
    {

        

    }

    public class BaseUSCObject : USCObject
    {

        public double beat { get; set; }
        public int timeScaleGroup { get; set; }


        public BaseUSCObject(int vbeat, int vtimeScaleGroup)
        {
            beat = vbeat;
            timeScaleGroup = vtimeScaleGroup;
        }
    }

    public class BaseUSCNote : USCObject
    {

        public double beat { get; set; }
        public int timeScaleGroup { get; set; }
        public float lane { get; set; }
        public float size { get; set; }



        public BaseUSCNote(double beat, int timeScaleGroup, float lane, float size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.lane = lane;
            this.size = size;
        }
    }

    
    public class USCSingleNote : BaseUSCNote
    {
        public string type = "single";
        public bool critical { get; set; }
        public bool trace { get; set; }

        public USCSingleNote(double beat, int timeScaleGroup, float lane, float size, bool critical, bool trace) : base(beat, timeScaleGroup, lane, size)
        {
            this.critical = critical;
            this.timeScaleGroup = timeScaleGroup;
            this.trace = trace;
        }
    }


    public enum directionTypes { up, left, right, none }

    public class USCAirNote : BaseUSCNote
    {
        public string type = "single";
        public bool critical { get; set; }
        public bool trace { get; set; }
        public string direction { get; set; }
        public USCAirNote(double beat, int timeScaleGroup, float lane, float size, bool critical, bool trace, int direction = 3) : base(beat, timeScaleGroup, lane, size)
        {
            this.critical = critical;
            this.timeScaleGroup = timeScaleGroup;
            this.trace = trace;
            this.direction = Enum.GetName(typeof(directionTypes), direction);
        }
    }

    public class USCDamageNote : BaseUSCNote
    {
        public string type = "damage";

        public USCDamageNote(double beat, int timeScaleGroup, float lane, float size) : base(beat, timeScaleGroup, lane, size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.lane = lane;
            this.size = size;
        }
    }


    public class USCSlideNote : USCObject
    {
        public string type = "slide";
        public bool critical { get; set; }
        public List<USCObject> connections { get; set;}

        public USCSlideNote(bool critical, USCConnectionStartNote start, USCConnectionVisibleTickNote[] vitick, USCConnectionTickNote[] tick, USCConnectionAttachNote[] attach, USCConnectionEndNote end) 
        {
            this.critical = critical;
            connections = new List<USCObject>();
            connections.Add(start);
            if(vitick != null)
            foreach(var note in vitick)
            {
                connections.Add(note);
            }
            if (tick != null)
                foreach (var note in tick)
            {
                connections.Add(note);
            }
            if (attach != null)
                foreach (var note in attach)
            {
                connections.Add(note);
            }
            connections.Add(end);
        }

        public USCSlideNote(bool critical, USCConnectionStartNote start, USCConnectionVisibleTickNote[] vitick, USCConnectionTickNote[] tick, USCConnectionAttachNote[] attach, USCConnectionAirEndNote end)
        {
            this.critical = critical;
            connections = new List<USCObject>();
            
            connections.Add(start);
            if (vitick != null)
                foreach (var note in vitick)
                {
                    connections.Add(note);
                }
            if (tick != null)
                foreach (var note in tick)
                {
                    connections.Add(note);
                }
            if (attach != null)
                foreach (var note in attach)
                {
                    connections.Add(note);
                }
            connections.Add(end);
            

        }



    }

    public enum easeTypes { @out, linear, @in }
    public enum judgeTypes { normal, trace, none }

    public class USCConnectionStartNote : BaseUSCNote
    {
        public string type = "start";
        public bool critical { get; set; }
        public string ease { get; set; }
        public string judgeType { get; set; }


        public USCConnectionStartNote(double beat, int timeScaleGroup, float lane, float size, bool critical, string ease, string judge) : base(beat, timeScaleGroup, lane, size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.critical = critical;
            this.ease = ease;
            this.judgeType = judge;
            this.lane = lane;
            this.size = size;

        }
    }


    public class USCConnectionVisibleTickNote : BaseUSCNote
    {
        public string type = "tick";
        public bool critical { get; set; }
        public string ease { get; set; }


        public USCConnectionVisibleTickNote(double beat, int timeScaleGroup, float lane, float size, bool critical, string ease) : base(beat, timeScaleGroup, lane, size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.critical = critical;
            this.ease = ease;
            this.lane = lane;
            this.size = size;
        }
    }


    public class USCConnectionTickNote : BaseUSCNote
    {
        public string type = "tick";
        public string ease { get; set; }


        public USCConnectionTickNote(double beat, int timeScaleGroup, float lane, float size, string ease) : base(beat, timeScaleGroup, lane, size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.ease = ease;
            this.lane = lane;
            this.size = size;
        }
    }


    public class USCConnectionAttachNote : BaseUSCNote
    {
        public string type = "attach";
        public bool critical { get; set; }

        public USCConnectionAttachNote(double beat, int timeScaleGroup, float lane, float size, bool critical) : base(beat, timeScaleGroup, lane, size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.critical = critical;
            this.lane = lane;
            this.size = size;
        }
    }


    public class USCConnectionEndNote : BaseUSCNote
    {
        public string type = "end";
        public bool critical { get; set; }
        public string judgeType { get; set; }


        public USCConnectionEndNote(double beat, int timeScaleGroup, float lane, float size, bool critical, string judge) : base(beat, timeScaleGroup, lane, size)
        {
            this.timeScaleGroup = timeScaleGroup;
            this.critical = critical;
            this.judgeType = judge;
            this.lane = lane;
            this.size = size;
        }
    }
    public class USCConnectionAirEndNote : BaseUSCNote
    {
        public string type = "end";
        public bool critical { get; set; }
        public string judgeType { get; set; }
        public string direction;


        public USCConnectionAirEndNote(double beat, int timeScaleGroup, float lane, float size, bool critical, string judge, int direction = 3) : base(beat, timeScaleGroup, lane, size)
        {
            this.critical = critical;
            this.judgeType = judge;
            this.timeScaleGroup = timeScaleGroup;
            this.direction = Enum.GetName(typeof(directionTypes), direction);
            this.lane = lane;
            this.size = size;
        }
    }


    public class USCGuideMidpointNote : BaseUSCNote
    {
        public string ease { get; set; }
        public USCGuideMidpointNote(double beat, int timeScaleGroup, float lane, float size, string ease) : base(beat, timeScaleGroup, lane, size)
        {
            this.beat = beat;
            this.timeScaleGroup = timeScaleGroup;
            this.lane = lane;
            this.size = size;
            this.ease = ease;
        }
    }

    public class USCGuideNote : USCObject
    {
        public string type = "guide";
        public string color { get; set; }
        public string fade { get; set; }
        public List<USCGuideMidpointNote> midpoints { get; set; }
        public USCGuideNote(string color, string fade, USCGuideMidpointNote[] midpointlist )
        {
            this.color = color;
            this.fade = fade;

            midpoints = new List<USCGuideMidpointNote>();

            if (midpointlist != null)
                foreach (var midpoint in midpointlist.OrderBy(p => p.beat))
                {
                    midpoints.Add(midpoint);
                }


        }
    }







    public class USCBpmChange : USCObject
    {
        public double beat { get; set; }
        public string type = "bpm";
        public double bpm {get; set; }


        public USCBpmChange(int beat, double bpm)
        {
            this.beat = beat;
            this.bpm = bpm;
        }
    }

    public class USCTimeScaleChange : USCObject
    {

        public string type = "timeScaleGroup";
        public List<USCObject> changes = new List<USCObject>();

        public USCTimeScaleChange(List<USCTimeScale> change)
        {
            changes = new List<USCObject>();
            foreach(var chg  in change)
            {
                changes.Add(chg);
            }

        }
    }

    public class USCTimeScale : USCObject
    {

        public double beat { get; set; }
        public decimal timeScale { get; set; }

        public USCTimeScale(double beat, decimal timeScale)
        {
            this.beat = beat;
            this.timeScale = timeScale;
        }
    }
}
