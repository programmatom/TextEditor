/*
 *  Copyright © 1992-2002, 2015 Thomas R. Lawrence
 * 
 *  GNU General Public License
 * 
 *  This file is part of "Text Editor"
 * 
 *  "Text Editor" is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class StochasticTest : Form
    {
        private readonly TextEditControl textEditControl;
        private readonly DateTime started;
        private readonly Queue<Action> queue = new Queue<Action>();
        private readonly int randomSeed;
        private readonly Random random;

        private int operationCount;
        private string state = String.Empty;

        public StochasticTest(TextEditControl textEditControl)
            : this(textEditControl, Environment.TickCount)
        {
        }

        public StochasticTest(TextEditControl textEditControl, int randomSeed)
        {
            this.textEditControl = textEditControl;
            this.randomSeed = randomSeed;
            this.random = new Random(randomSeed);
            Debugger.Log(0, "TextEditorApp.StochasticTest", String.Format("StochasticTest: random seed = {0}" + Environment.NewLine, randomSeed));

            InitializeComponent();
            this.Icon = TextEditorApp.Properties.Resources.Icon2;

            this.started = DateTime.Now;
            UpdateStatus();

            state = textEditControl.Text;

            Disposed += StochasticTest_Disposed;
        }

        private void StochasticTest_Disposed(object sender, EventArgs e)
        {
            timerTask.Stop();
        }

        private void UpdateStatus()
        {
            TimeSpan elapsed = DateTime.Now - started + new TimeSpan(0, 0, 0, 0, 500);
            elapsed = new TimeSpan(elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds, 0);
            this.labelElapsedTime.Text = elapsed.ToString("g");
            this.labelOperationCount.Text = operationCount.ToString("N0");
            this.labelLines.Text = textEditControl.Count.ToString("N0");
            this.labelCharacters.Text = state.Length.ToString("N0");
            this.labelMode.Text = tuning.Label;
            this.labelRandomSeed.Text = randomSeed.ToString();
        }

        private void timerUpdateStatus_Tick(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private const int TimeSliceMsec = 1000;
        private void timerTask_Elapsed(object sender, ElapsedEventArgs e)
        {
            Debug.Assert(!IsDisposed);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < TimeSliceMsec)
            {
                DispatchTask();
                Application.DoEvents();
            }

            timerTask.Enabled = true;
        }

        private void DispatchTask()
        {
            operationCount++;

            if (queue.Count != 0)
            {
                Action action = queue.Dequeue();
                try
                {
                    action.Do();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.ToString());
                    Debugger.Break();
                }
            }
            else
            {
                GenerateTask();
            }
        }

        private class Tuning
        {
            public static readonly Tuning Grow = new Tuning(
                Label: "Grow",
                WordsPerLineLimit: 10,
                WordLengthLimit: 10,
                LineLimit: 10000,
                Exponent: 1.1,
                ReplaceRangeAffinity: 1000);

            public static readonly Tuning Shrink = new Tuning(
                Label: "Shrink",
                WordsPerLineLimit: 10,
                WordLengthLimit: 10,
                LineLimit: 10000,
                Exponent: .6,
                ReplaceRangeAffinity: 500);

            public readonly string Label;
            public readonly int WordsPerLineLimit;
            public readonly int WordLengthLimit;
            public readonly int LineLimit;
            public readonly double Exponent;
            public readonly int ReplaceRangeAffinity;

            public Tuning(
                string Label,
                int WordsPerLineLimit,
                int WordLengthLimit,
                int LineLimit,
                double Exponent,
                int ReplaceRangeAffinity)
            {
                this.Label = Label;
                this.WordsPerLineLimit = WordsPerLineLimit;
                this.WordLengthLimit = WordLengthLimit;
                this.LineLimit = LineLimit;
                this.Exponent = Exponent;
                this.ReplaceRangeAffinity = ReplaceRangeAffinity;
            }
        }

        private Tuning tuning = Tuning.Grow;

        private int lastOffset;
        private void GenerateTask()
        {
            bool oversized = false;

            if (textEditControl.Count > 10000)
            {
                tuning = Tuning.Shrink;
            }
            else if (textEditControl.Count < 500)
            {
                tuning = Tuning.Grow;
            }

        Retry:
            lastOffset = Math.Min(Math.Max(lastOffset, 0), state.Length); // ensure valid given deletion may have occurred
            const int DeclusteringLikelihood = 10;
            const int ClusteringAffinity = 25;
            const int UndoRedoLikelihood = 10;
            const int UndoRedoMaxDepth = 20;
            const double UndoRedoBias = 2;
            switch (random.Next(4))
            {
                default:
                    Debug.Assert(false);
                    break;

                case 0: // insert
                    {
                        Position insertionPoint = random.Next(DeclusteringLikelihood) != 0
                            ? GetNearbyPosition(OffsetToValidPosition(lastOffset), ClusteringAffinity)
                            : GetRandomValidPosition();
                        queue.Enqueue(new ActionSelect(this, insertionPoint, insertionPoint));
                        queue.Enqueue(new ActionReplace(this, insertionPoint, insertionPoint));
                        lastOffset = insertionPoint.offset;
                    }
                    break;

                case 1: // replace
                    {
                        Position start = random.Next(DeclusteringLikelihood) != 0
                            ? GetNearbyPosition(OffsetToValidPosition(lastOffset), ClusteringAffinity)
                            : GetRandomValidPosition();
                        Position end = !oversized ? GetNearbyPosition(start, tuning.ReplaceRangeAffinity) : GetRandomValidPosition();
                        EnsurePositionOrdering(ref start, ref end);
                        queue.Enqueue(new ActionSelect(this, start, end));
                        queue.Enqueue(new ActionReplace(this, start, end));
                        lastOffset = (start.offset + end.offset) / 2;
                    }
                    break;

                case 2: // undo/redo
                    if (random.Next(UndoRedoLikelihood) != 0)
                    {
                        goto Retry;
                    }
                    {
                        int depth = TruncatedHyperbolicDistribution(UndoRedoMaxDepth, UndoRedoBias);
                        for (int i = 0; i < depth; i++)
                        {
                            queue.Enqueue(new ActionUndo(this));
                        }
                        for (int i = 0; i < depth; i++)
                        {
                            queue.Enqueue(new ActionRedo(this));
                        }
                    }
                    break;

                case 3: // periodically clear undo/redo to reign in memory usage
                    if (random.Next(1000) != 0)
                    {
                        goto Retry;
                    }
                    textEditControl.ClearUndoRedo();
                    break;
            }

            queue.Enqueue(new ActionValidate(this));
        }

        private double TruncatedHyperbolicDistribution(double limit, double exponent)
        {
            double r = Math.Pow(random.NextDouble(), exponent);
            double b = 1d / limit;
            double i = 1 / (r * (1 - b) + b);
            if (Double.IsNaN(i) || Double.IsInfinity(i))
            {
                i = Double.MaxValue;
            }
            return Math.Max(Math.Min(i, limit - 1), 1);
        }

        private int TruncatedHyperbolicDistribution(int limit, double exponent)
        {
            double r = Math.Pow(random.NextDouble(), exponent);
            double b = 1d / limit;
            int i = (int)(1 / (r * (1 - b) + b));
            return Math.Max(Math.Min(i, limit - 1), 1);
        }

        private void EnsurePositionOrdering(ref Position start, ref Position end)
        {
            if (start.offset > end.offset)
            {
                Position temp = start;
                start = end;
                end = temp;
            }
        }

        private Position GetRandomValidPosition()
        {
            return OffsetToValidPosition(random.Next(state.Length));
        }

        private Position GetNearbyPosition(Position pivot, int range)
        {
            double frac = (TruncatedHyperbolicDistribution((double)range + 1.0, 1) - 1) / range;
            int adjust = ((int)(frac * state.Length) + 1) * (random.Next(2) > 0 ? 1 : -1);
            if ((pivot.offset + adjust < 0) || (pivot.offset + adjust > state.Length))
            {
                adjust = -adjust;
            }
            int offset = pivot.offset + adjust;
            offset = Math.Min(Math.Max(offset, 0), state.Length);
            return OffsetToValidPosition(offset);
        }

        private static readonly char NewLineFirstChar = Environment.NewLine[0];
        private Position OffsetToValidPosition(int offset)
        {
            Debug.Assert((offset >= 0) && (offset <= state.Length));

            if (offset < state.Length)
            {
                int nl = Environment.NewLine.IndexOf(state[offset]);
                if (nl > 0)
                {
                    offset = offset + (random.Next(2) > 0 ? Environment.NewLine.Length - nl : -nl);
                }
            }
            Debug.Assert((offset == state.Length) || (Environment.NewLine.IndexOf(state[offset]) <= 0));

            int l = 0;
            int c = 0;
            for (int i = 0; i < offset; i++)
            {
                if (state[i] == NewLineFirstChar)
                {
                    c = i + Environment.NewLine.Length;
                    l++;
                }
            }
            c = offset - c;

            int count = 0;
            for (int i = 0; i < l; i++)
            {
                count += textEditControl[i].Length + Environment.NewLine.Length;
            }
            count += c;
            Debug.Assert(count == offset);

            Debug.Assert((l >= 0) && (c >= 0));
            Debug.Assert(l < textEditControl.Count);
            Debug.Assert(c <= textEditControl[l].Length);

            return new Position(offset, l, c);
        }

        private struct Position
        {
            public int offset;
            public int l;
            public int c;

            public Position(int offset, int l, int c)
            {
                Debug.Assert((offset >= 0) && (l >= 0) && (c >= 0));
                this.offset = offset;
                this.l = l;
                this.c = c;
            }

            public override string ToString()
            {
                return String.Format("[{0}: {1}.{2}]", offset, l, c);
            }
        }

        private abstract class Action
        {
            protected readonly StochasticTest context;

            protected Action(StochasticTest context)
            {
                this.context = context;
            }

            public abstract void Do();
        }

        private class ActionValidate : Action
        {
            public ActionValidate(StochasticTest context)
                : base(context)
            {
            }

            public override void Do()
            {
                string actual = context.textEditControl.Text;
                if (!String.Equals(context.state, actual, StringComparison.Ordinal))
                {
                    throw new ValidateException(
                        "Reference text state does not match control's text state",
                        context.state,
                        actual);
                }
            }

            private class ValidateException : ApplicationException
            {
                private readonly string reference;
                private readonly string actual;

                public ValidateException(string message, string reference, string actual)
                    : base(message)
                {
                    this.reference = reference;
                    this.actual = actual;
                }

                public string Reference { get { return reference; } }
                public string Actual { get { return actual; } }
            }
        }

        private class ActionSelect : Action
        {
            private readonly Position start;
            private readonly Position end;

            public ActionSelect(StochasticTest context, Position start, Position end)
                : base(context)
            {
                Debug.Assert(start.offset <= end.offset);
                this.start = start;
                this.end = end;
            }

            public override void Do()
            {
                context.textEditControl.SetSelection(start.l, start.c, end.l, end.c);
                context.textEditControl.ScrollToSelection();
            }
        }

        private class ActionReplace : Action
        {
            private readonly Position start;
            private readonly Position end;

            public ActionReplace(StochasticTest context, Position start, Position end)
                : base(context)
            {
                this.start = start;
                this.end = end;
            }

            private const string Domain = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            public override void Do()
            {
                StringBuilder sb = new StringBuilder();
                int lines = context.TruncatedHyperbolicDistribution(context.tuning.LineLimit, context.tuning.Exponent) - 1;
                for (int i = 0; i <= lines; i++)
                {
                    for (int j = context.random.Next(context.tuning.WordsPerLineLimit); j >= 0; j--)
                    {
                        for (int k = context.random.Next(context.tuning.WordLengthLimit); k >= 0; k--)
                        {
                            sb.Append(Domain[context.random.Next(Domain.Length)]);
                        }
                        if (j > 0)
                        {
                            sb.Append(" ");
                        }
                    }
                    if (i < lines)
                    {
                        sb.Append(Environment.NewLine);
                    }
                }
                string s = sb.ToString();

                Debug.Assert((context.textEditControl.SelectionStartLine == start.l)
                    || (context.textEditControl.SelectionEndLine == end.l)
                    || (context.textEditControl.SelectionStartChar == start.c)
                    || (context.textEditControl.SelectionEndCharPlusOne == end.c));

                context.state = String.Concat(
                    context.state.Substring(0, start.offset),
                    s,
                    context.state.Substring(end.offset, context.state.Length - end.offset));
                context.textEditControl.SelectedText = s;
            }
        }

        private class ActionUndo : Action
        {
            public ActionUndo(StochasticTest context)
                : base(context)
            {
            }

            public override void Do()
            {
                context.textEditControl.Undo();
            }
        }

        private class ActionRedo : Action
        {
            public ActionRedo(StochasticTest context)
                : base(context)
            {
            }

            public override void Do()
            {
                context.textEditControl.Redo();
            }
        }
    }
}
