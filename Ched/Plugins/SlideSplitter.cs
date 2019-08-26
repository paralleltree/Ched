using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Notes;
using Ched.Localization;

namespace Ched.Plugins
{
    public class SlideSplitter : IScorePlugin
    {
        public string DisplayName => PluginStrings.SlideSplitter;

        public void Run(IScorePluginArgs args)
        {
            var score = args.GetCurrentScore();
            var range = args.GetSelectedRange();
            bool modified = false;
            var targets = score.Notes.Slides.Where(p => p.StartTick < range.StartTick && p.StepNotes.OrderByDescending(q => q.TickOffset).First().Tick > range.StartTick);
            var endStepDic = score.Notes.Slides.ToDictionary(p => p, p => p.StepNotes.OrderByDescending(q => q.TickOffset).First());
            var airStepDic = score.Notes.Airs
                .Where(p => endStepDic.Values.Contains(p.ParentNote))
                .ToDictionary(p => p.ParentNote as Slide.StepTap, p => p);
            var airActionStepDic = score.Notes.AirActions
               .Where(p => endStepDic.Values.Contains(p.ParentNote))
               .ToDictionary(p => p.ParentNote as Slide.StepTap, p => p);

            foreach (var slide in targets.ToList())
            {
                // カーソル位置に中継点が存在しなければ処理しない
                int offset = range.StartTick - slide.StartTick;
                if (slide.StepNotes.All(p => p.TickOffset != offset)) continue;

                var first = new Slide() { StartTick = slide.StartTick };
                first.SetPosition(slide.StartLaneIndex, slide.StartWidth);
                first.StepNotes.AddRange(slide.StepNotes.OrderBy(p => p.TickOffset).TakeWhile(p => p.TickOffset <= offset).Select(p =>
                {
                    var step = new Slide.StepTap(first) { TickOffset = p.TickOffset, IsVisible = p.IsVisible };
                    step.SetPosition(p.LaneIndexOffset, p.WidthChange);
                    return step;
                }));
                first.StepNotes[first.StepNotes.Count - 1].IsVisible = true;

                var second = new Slide() { StartTick = range.StartTick };
                var trailing = slide.StepNotes.OrderBy(p => p.TickOffset).SkipWhile(p => p.TickOffset < offset).ToList();
                second.SetPosition(trailing[0].LaneIndex, trailing[0].Width);
                second.StepNotes.AddRange(trailing.Skip(1).Select(p =>
                {
                    var step = new Slide.StepTap(second) { TickOffset = p.TickOffset - offset, IsVisible = p.IsVisible };
                    step.SetPosition(p.LaneIndex - second.StartLaneIndex, p.Width - second.StartWidth);
                    return step;
                }));

                // 終点AIRをsecondに挿入
                if (airStepDic.ContainsKey(endStepDic[slide]))
                {
                    var origAir = airStepDic[endStepDic[slide]];
                    var air = new Air(second.StepNotes[second.StepNotes.Count - 1])
                    {
                        VerticalDirection = origAir.VerticalDirection,
                        HorizontalDirection = origAir.HorizontalDirection
                    };
                    score.Notes.Airs.Remove(origAir);
                    score.Notes.Airs.Add(air);
                }
                if (airActionStepDic.ContainsKey(endStepDic[slide]))
                {
                    var origAirAction = airActionStepDic[endStepDic[slide]];
                    var airAction = new AirAction(second.StepNotes[second.StepNotes.Count - 1]);
                    airAction.ActionNotes.AddRange(origAirAction.ActionNotes.Select(p => new AirAction.ActionNote(airAction) { Offset = p.Offset }));
                    score.Notes.AirActions.Remove(origAirAction);
                    score.Notes.AirActions.Add(airAction);
                }

                score.Notes.Slides.Add(first);
                score.Notes.Slides.Add(second);
                score.Notes.Slides.Remove(slide);
                modified = true;
            }
            if (modified) args.UpdateScore(score);
        }
    }
}
