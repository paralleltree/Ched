using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Notes;
using Ched.Localization;

namespace Ched.Plugins
{
    public class SlideReverser : IScorePlugin
    {
        public string DisplayName => PluginStrings.SlideReverser;

        public void Run(IScorePluginArgs args)
        {
            var score = args.GetCurrentScore();
            var range = args.GetSelectedRange();
            int startTick = range.Duration < 0 ? range.StartTick + range.Duration : range.StartTick;
            int endTick = range.Duration < 0 ? range.StartTick : range.StartTick + range.Duration;
            var endStepDic = score.Notes.Slides.ToDictionary(p => p, p => p.StepNotes.OrderByDescending(q => q.TickOffset).First());
            var airStepDic = score.Notes.Airs
                .Where(p => endStepDic.Values.Contains(p.ParentNote))
                .ToDictionary(p => p.ParentNote as Slide.StepTap, p => p);
            var airActionStepDic = score.Notes.AirActions
                .Where(p => endStepDic.Values.Contains(p.ParentNote))
                .ToDictionary(p => p.ParentNote as Slide.StepTap, p => p);

            var targets = score.Notes.Slides
                .Where(p => p.StartTick >= startTick && p.StartTick + p.GetDuration() <= endTick)
                .Where(p => p.StartLaneIndex >= range.StartLaneIndex && p.StartLaneIndex + p.StartWidth <= range.StartLaneIndex + range.SelectedLanesCount)
                .Where(p => p.StepNotes.All(q => q.LaneIndex >= range.StartLaneIndex && q.LaneIndex + q.Width <= range.StartLaneIndex + range.SelectedLanesCount))
                .Where(p => !airStepDic.ContainsKey(endStepDic[p]) && !airActionStepDic.ContainsKey(endStepDic[p]))
                .ToList();
            if (targets.Count == 0) return;
            var results = targets.Select(p =>
            {
                var ordered = p.StepNotes.OrderByDescending(q => q.TickOffset).ToList();
                var res = new Slide() { StartTick = startTick + (endTick - ordered[0].Tick) };
                res.SetPosition(ordered[0].LaneIndex, ordered[0].Width);
                var trailing = new Slide.StepTap(res) { IsVisible = true, TickOffset = startTick + (endTick - p.StartTick) - res.StartTick };
                trailing.SetPosition(p.StartLaneIndex - res.StartLaneIndex, p.StartWidth - res.StartWidth);
                var steps = ordered.Skip(1).Select(q =>
                {
                    var step = new Slide.StepTap(res) { IsVisible = q.IsVisible, TickOffset = startTick + (endTick - q.Tick) - res.StartTick };
                    step.SetPosition(q.LaneIndex - res.StartLaneIndex, q.Width - res.StartWidth);
                    return step;
                })
                .Concat(new[] { trailing });
                res.StepNotes.AddRange(steps);
                return res;
            });

            foreach (var slide in targets) score.Notes.Slides.Remove(slide);
            score.Notes.Slides.AddRange(results);
            args.UpdateScore(score);
        }
    }
}
