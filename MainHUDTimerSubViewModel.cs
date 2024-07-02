using System;
using Core.Client.GameEventSystem;
using Core.Client.Unity.UI;
using Core.Client.Unity.UI.Binding;
using Core.Rx;
using Game.Client.Fight.UI.MainHUD.SubModels.Timers;
using Game.Client.Unity.GameViewEvents;
using Game.Client.Unity.Tween;
using Game.Client.Unity.Tween.Extensions;
using Game.Client.Unity.UI.Extensions;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Game.Client.Unity.UI.ViewModels.MainHUD.SubViewModels.Timers
{
    [Binding]
    public class MainHUDTimerSubViewModel : SubViewModelBase<MainHUDTimerSubModel, MainHUDTimerSubViewModel>
    {
        private readonly ITweenManager _tweenManager;
        private string _timeValue = 0.SecondsToHumanFormat();
        private float _timerAlpha = 1;
        private ITween _blinkTween;
        private Color _textColor;
        private float _textSize;
        private Vector2 _textBackSize;
        private Vector3 _timerTextScale;
        private bool _isFinalCountdown;


        [Binding]
        public string TimeValue
        {
            get => _timeValue;
            private set => SetField(ref _timeValue, value);
        }

        [Binding]
        public bool IsLeader => Model.IsLeader;

        [Binding]
        public float TimerAlpha
        {
            get => _timerAlpha;
            private set => SetField(ref _timerAlpha, value);
        }

        [Binding]
        public Vector3 TimerTextScale
        {
            get => _timerTextScale;
            private set => SetField(ref _timerTextScale, value);
        }

        [Binding]
        public Color TextColor
        {
            get => _textColor;
            private set => SetField(ref _textColor, value);
        }

        [Binding]
        public Single TextSize
        {
            get => _textSize;
            private set => SetField(ref _textSize, value);
        }

        [Binding]
        public Vector2 TextBackSize
        {
            get => _textBackSize;
            private set => SetField(ref _textBackSize, value);
        }

        [Binding]
        public bool IsFinalCountdown
        {
            get => _isFinalCountdown;
            private set => SetField(ref _isFinalCountdown, value);
        }

        public MainHUDTimerSubViewModel(ITweenManager tweenManager, MainHUDTimerSubModel model, ILogger log) :
            base(model, log)
        {
            _tweenManager = tweenManager;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            WatchModelProperty(m => m.IsLeader, vm => vm.IsLeader);

            Model.WhenAny(m => m.IsFinalTimerStarted,
                          m => m.IsLeader,
                          m => m.Time,
                          (isFinalTimerStarted, isLeader, time) => (isFinalTimerStarted, isLeader, time))
                 .StartWith((Model.IsFinalTimerStarted, Model.IsLeader, Model.Time))
                 .Subscribe(UpdateTimerTween)
                 .AddTo(Model.CompositeDisposable);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _blinkTween?.Kill();
            _blinkTween = null;
        }

        private void UpdateTimerTween((bool isFinalTimerStarted, bool isLeader, int time) data)
        {
            var (isFinalTimerStarted, isLeader, time) = data;

            var newTimeValue = time.SecondsToHumanFormat();
            if(TimeValue != newTimeValue)
            {
                if(time <= TweenStartTime)
                {
                    new GameViewEventFinalTimerTick(time).Publish();
                }
                TimeValue = time.SecondsToHumanFormat();
            }

            if(!isFinalTimerStarted)
            {
                SetTimerColorAndSize(
                    time > TweenStartTime
                        ? DefaultTimerColor
                        : AttentionTimerColor,
                    DefaultTimerTextSize,
                    DefaultTimerTextBackSize);
                TimerAlpha = 1;
                TimerTextScale = Vector3.one;
                return;
            }

            var timeSpan = TimeSpan.FromSeconds(time);
            if(timeSpan.TotalSeconds > TweenStartTime && _blinkTween != null)
            {
                _blinkTween?.Kill();
                _blinkTween = null;
            }
            else if(timeSpan.TotalSeconds > TweenStartTime && _blinkTween == null)
            {
                SetTimerColorAndSize(AttentionTimerColor, FinalTimerTextSize, FinalTimerTextBackSize);
                TimerAlpha = 1;
                TimerTextScale = Vector3.one;
            }
            else if(timeSpan.TotalSeconds <= TweenStartTime)
            {
                IsFinalCountdown = true;
               
                var color = isLeader
                    ? FinalTimerForLeaderColor
                    : FinalTimerForLoserColor;
                SetTimerColorAndSize(color, FinalTimerTextSize, FinalTimerTextBackSize);

                var fadeInTween = _tweenManager.BuildTweenChain()
                                               .Float(0, 1, 0.3f, alpha => TimerAlpha = alpha, EasingFunction.Ease.EaseOutExpo)
                                               .Vector3(Vector3.one * 3, Vector3.one, 0.3f, scale => TimerTextScale = scale, EasingFunction.Ease.EaseOutExpo)
                                               .Parallel();

                var waitTween = _tweenManager.BuildTween()
                                             .Wait(0.7f);

                _blinkTween = _tweenManager.BuildTweenChain()
                                           .Other(fadeInTween)
                                           .Other(waitTween)
                                           .Sequence();
                _blinkTween.Play();
            }
        }

        private void SetTimerColorAndSize(Color textColor, float textSize, Vector2 backSize)
        {
            TextColor = textColor;
            TextSize = textSize;
            TextBackSize = backSize;
        }

        private const float TweenStartTime = 30.0f; // when to start tween

        private static readonly Color DefaultTimerColor = new Color(1, 1, 1, 0.9f);
        private static readonly Color AttentionTimerColor = new Color(1, 1, 1);
        private static readonly Color FinalTimerForLeaderColor = new Color(134 / 255f, 255 / 255f, 39 / 255f);
        private static readonly Color FinalTimerForLoserColor = new Color(255 / 255f, 62 / 255f, 62 / 255f);

        private const float DefaultTimerTextSize = 55;
        private const float FinalTimerTextSize = 60;

        private static readonly Vector2 DefaultTimerTextBackSize = new Vector2(276f, 99.6f);
        private static readonly Vector2 FinalTimerTextBackSize = new Vector2(276 * 1.2f, 99.6f);
    }
}