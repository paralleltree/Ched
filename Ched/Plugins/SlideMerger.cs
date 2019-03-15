using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Notes;

namespace Ched.Plugins
{
    public class SlideMerger : IScorePlugin
    {
        public string DisplayName => "スライド結合";

        public void Run(IScorePluginArgs args)
        {
            var score = args.GetCurrentScore();
            var range = args.GetSelectedRange();
            bool modified = false;
            int startTick = range.Duration < 0 ? range.StartTick + range.Duration : range.StartTick;
            int endTick = range.Duration < 0 ? range.StartTick : range.StartTick + range.Duration;
            var endStepDic = score.Notes.Slides.ToDictionary(p => p, p => p.StepNotes.OrderByDescending(q => q.TickOffset).First());
            var startDic = score.Notes.Slides.Where(p => p.StartTick >= startTick && p.StartTick <= endTick)
                .Select(p => new { Position = Tuple.Create(p.StartTick, p.StartLaneIndex, p.StartWidth), Note = p })
                .GroupBy(p => p.Position)
                .ToDictionary(p => p.Key, p => p.Select(q => q.Note).ToList());
            var endDic = score.Notes.Slides.Where(p => endStepDic[p].Tick >= startTick && endStepDic[p].Tick <= endTick)
                .Select(p => new { Position = Tuple.Create(endStepDic[p].Tick, endStepDic[p].LaneIndex, endStepDic[p].Width), Note = p })
                .GroupBy(p => p.Position)
                .ToDictionary(p => p.Key, p => p.Select(q => q.Note).ToList());

            while (endDic.Count > 0)
            {
                var pos = endDic.First().Key;
                if (endDic[pos].Count == 0 || !startDic.ContainsKey(pos) || startDic[pos].Count == 0)
                {
                    endDic.Remove(pos);
                    continue;
                }

                if (startDic[pos].Count > 0)
                {
                    var heading = endDic[pos][0];
                    var trailing = startDic[pos][0];
                    var trailingOldSteps = trailing.StepNotes.OrderBy(p => p.TickOffset).ToList();
                    heading.StepNotes.AddRange(trailingOldSteps.Select(p =>
                    {
                        var step = new Slide.StepTap(heading) { TickOffset = p.Tick - heading.StartTick, IsVisible = p.IsVisible };
                        step.SetPosition(p.LaneIndex - heading.StartLaneIndex, p.Width - heading.StartWidth);
                        return step;
                    }));
                    score.Notes.Slides.Remove(trailing);
                    startDic[pos].Remove(trailing);
                    var trailingEndPos = Tuple.Create(endStepDic[trailing].Tick, endStepDic[trailing].LaneIndex, endStepDic[trailing].Width);
                    if (endDic.ContainsKey(trailingEndPos)) endDic[trailingEndPos].Remove(trailing);
                    endDic[pos].Remove(heading);
                    if (!endDic.ContainsKey(trailingEndPos)) endDic.Add(trailingEndPos, new[] { heading }.ToList());
                    else endDic[trailingEndPos].Add(heading);
                    modified = true;
                }
            }
            if (modified) args.UpdateScore(score);
        }
    }
}
