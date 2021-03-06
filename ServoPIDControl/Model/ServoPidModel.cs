﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ServoPIDControl.Model
{
    public class ServoPidModel : INotifyPropertyChanged
    {
        public const float TimeSeriesLengthSec = 10.0f;

        private float _p;
        private float _i;
        private float _d;
        private float _dLambda = 1;
        private float _setPoint;
        private float _InputMin = 1;
        private float _InputMax;
        private float _input;
        private float _output;
        private float _integrator;
        private float _dFiltered;

        private readonly object _timeSeriesLock = new object();

        public ServoPidModel(int id)
        {
            Id = id;

#if DEBUG && false
            lock (_timeSeriesLock)
            {
                const int ts = 500;

                Times = Enumerable.Range(0, ts)
                    .Select(i => i / 100.0f).ToList();
                SetPoints = Enumerable.Range(id * 100, ts)
                    .Select(i => i / 100 % 2 == 0 ? 80.0f : 100.0f).ToList();
                Inputs = Enumerable.Range(id * 100, ts)
                    .Select(i => 90 + (float) Math.Cos(i / 20.0f) * 9).ToList();
                Outputs = Enumerable.Range(id * 100, ts)
                    .Select(i => 90 + (float) Math.Sin(i / 20.0f) * 5).ToList();
            }
#endif
        }

        public int Id { get; }

        public float P
        {
            get => _p;
            set
            {
                if (value.Equals(_p)) return;
                _p = value;
                OnPropertyChanged();
            }
        }

        public float I
        {
            get => _i;
            set
            {
                if (value.Equals(_i)) return;
                _i = value;
                OnPropertyChanged();
            }
        }

        public float D
        {
            get => _d;
            set
            {
                if (value.Equals(_d)) return;
                _d = value;
                OnPropertyChanged();
            }
        }

        public float DLambda
        {
            get => _dLambda;
            set
            {
                if (value.Equals(_dLambda)) return;
                _dLambda = value;
                OnPropertyChanged();
            }
        }

        public float SetPoint
        {
            get => _setPoint;
            set
            {
                if (value.Equals(_setPoint)) return;
                _setPoint = value;
                OnPropertyChanged();
            }
        }

        public float InputMin
        {
            get => _InputMin;
            set
            {
                if (value.Equals(_InputMin)) return;
                _InputMin = value;
                OnPropertyChanged();
            }
        }

        public float InputMax
        {
            get => _InputMax;
            set
            {
                if (value.Equals(_InputMax)) return;
                _InputMax = value;
                OnPropertyChanged();
            }
        }

        public float Input
        {
            get => _input;
            internal set
            {
                if (value.Equals(_input)) return;
                _input = value;
                OnPropertyChanged();
            }
        }

        public float Output
        {
            get => _output;
            internal set
            {
                if (value.Equals(_output)) return;
                _output = value;
                OnPropertyChanged();
            }
        }

        public float Integrator
        {
            get => _integrator;
            internal set
            {
                if (value.Equals(_integrator)) return;
                _integrator = value;
                OnPropertyChanged();
            }
        }

        public float DFiltered
        {
            get => _dFiltered;
            internal set
            {
                if (value.Equals(_dFiltered)) return;
                _dFiltered = value;
                OnPropertyChanged();
            }
        }

        // ReSharper disable MemberInitializerValueIgnored
        public List<float> Times { get; } = new List<float>();
        public List<float> SetPoints { get; } = new List<float>();
        public List<float> Inputs { get; } = new List<float>();
        public List<float> Outputs { get; } = new List<float>();
        // ReSharper restore MemberInitializerValueIgnored

        internal class TimeSeries
        {
            public float[] X { get; set; }
            public float[] Y { get; set; }
            public string Name { get; set; }
        }

        internal IEnumerable<TimeSeries> AllTimeSeries
        {
            get
            {
                lock (_timeSeriesLock)
                {
                    if (!Times.Any())
                        yield break;

                    var timeArray = Times.ToArray();

                    yield return new TimeSeries {X = timeArray, Y = SetPoints.ToArray(), Name = "SetPoint"};
                    yield return new TimeSeries {X = timeArray, Y = Inputs.ToArray(), Name = "Input"};
                    yield return new TimeSeries {X = timeArray, Y = Outputs.ToArray(), Name = "Output"};
                }
            }
        }

        public event EventHandler TimePointRecorded;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RecordTimePoint(float elapsedSeconds)
        {
            lock (_timeSeriesLock)
            {
                Times.Add(elapsedSeconds);
                SetPoints.Add(SetPoint);
                Inputs.Add(Input);
                Outputs.Add(Output);

                var lastTime = Times.Last();
                var removeCount = Times.TakeWhile(t => lastTime - t > TimeSeriesLengthSec).Count();

                Times.RemoveRange(0, removeCount);
                SetPoints.RemoveRange(0, removeCount);
                Inputs.RemoveRange(0, removeCount);
                Outputs.RemoveRange(0, removeCount);
            }

            TimePointRecorded?.Invoke(this, EventArgs.Empty);
        }
    }
}