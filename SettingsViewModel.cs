using System;
using System.Linq;
using Core.Client.UI;
using Core.Client.Unity.UI;
using Core.Client.Unity.SoundSystem;
using Core.Client.Unity.UI.Binding;
using Core.Client.Unity.UI.Extensions;
using Core.Rx;
using Game.Client.App;
using Game.Client.Auth;
using Game.Client.Helpers;
using Game.Client.UI.Models.Settings;
using Game.Common.Entities.Profile;
using Game.Common.Protocol;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Game.Client.Unity.UI.ViewModels.Settings
{
    [Binding]
    public class SettingsViewModel : ViewModelBase<SettingsModel, SettingsViewModel>, IBackButton
    {
        private readonly ISoundManager _soundManager;
        private readonly IUIManager _uiManager;
        private readonly ILocalizer _localizer;
        private readonly IClientIdentity _clientIdentity;
        private readonly INativeButtonsHandler _nativeButtonsHandler;

        private bool _isSoundOn;
        private bool _isLanguagesGroupOpen;
        private string _currentTab;

        [Binding]
        public string PlayerIdLabel => $"Id: {Model.ProfileId}";

        [Binding]
        public string PlatformLabel
        {
            get
            {
                switch(_clientIdentity.Platform)
                {
                    case PlayerMessage.PlatformCode.MobileAndroid:
                        return _localizer.Localize("SETTINGS_GOOGLE_HEADER");
                    case PlayerMessage.PlatformCode.MobileApple:
                        return _localizer.Localize("SETTINGS_APPLE_HEADER");
                    default:
                        return "";
                }
            }
        }

        [Binding]
        public string BuildLabel => $"Build version: {Model.BuildVersion}";

        [Binding]
        public string UserName => Model.UserName;

        [Binding]
        public bool IsSoundOn
        {
            get => _isSoundOn;
            private set => SetField(ref _isSoundOn, value);
        }

        [Binding]
        public bool IsMoveJoyFollowToFinger => Model.IsMoveJoyFollowToFinger;

        [Binding]
        public bool AccountLinkPending => Model.IsPlatformAccountLinkPending;

        [Binding]
        public string CurrentTab
        {
            get => _currentTab;
            private set => SetField(ref _currentTab, value);
        }

        [Binding]
        public bool IsLanguagesGroupOpen
        {
            get => _isLanguagesGroupOpen;
            private set => SetField(ref _isLanguagesGroupOpen, value);
        }

        [Binding]
        public string SelectedLanguageState => Model.SelectedLanguage;

        [Binding]
        public bool IsControlTouchscreenSelected => Model.InputScheme == InputScheme.Touchscreen;

        [Binding]
        public bool IsControlKeyboardMouseSelected => Model.InputScheme == InputScheme.MouseAndKeyboard;

        [Binding]
        public bool IsControlControllerSelected => Model.InputScheme == InputScheme.Controller;

        [Binding]
        public bool IsControlKeyboardMouseDisabled => !Model.AvailableInputSchemes.Contains(InputScheme.MouseAndKeyboard);

        [Binding]
        public bool IsControlTouchscreenDisabled => !Model.AvailableInputSchemes.Contains(InputScheme.Touchscreen);

        [Binding]
        public bool IsControlControllerDisabled => !Model.AvailableInputSchemes.Contains(InputScheme.Controller);

        [Binding]
        public bool IsPlatformAccountLinked => Model.IsPlatformAccountLinked;

        [Binding]
        public bool IsFacebookConnected => Model.IsFacebookConnected;

        public SettingsViewModel
        (
            SettingsModel model,
            ISoundManager soundManager,
            IUIManager uiManager,
            ILocalizer localizer,
            IClientIdentity clientIdentity,
            INativeButtonsHandler nativeButtonsHandler,
            ILogger log
        )
            : base(model, log)
        {
            _soundManager = soundManager;
            _uiManager = uiManager;
            _localizer = localizer;
            _clientIdentity = clientIdentity;
            _nativeButtonsHandler = nativeButtonsHandler;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            WatchModelProperty(m => m.UserName, vm => vm.UserName);
            WatchModelProperty(m => m.ProfileId, vm => vm.PlayerIdLabel);
            WatchModelProperty(m => m.BuildVersion, vm => vm.BuildLabel);

            WatchModelProperty(m => m.IsPlatformAccountLinkPending, vm => vm.AccountLinkPending);
            WatchModelProperty(m => m.IsPlatformAccountLinked, vm => vm.IsPlatformAccountLinked);
            WatchModelProperty(m => m.IsFacebookConnected, vm => vm.IsFacebookConnected);

            WatchModelProperty(m => m.SelectedLanguage, vm => vm.SelectedLanguageState);

            WatchModelProperty(m => m.IsMoveJoyFollowToFinger, vm => vm.IsMoveJoyFollowToFinger);
            WatchModelProperty(m => m.InputScheme, vm => vm.IsControlTouchscreenSelected);
            WatchModelProperty(m => m.InputScheme, vm => vm.IsControlKeyboardMouseSelected);
            WatchModelProperty(m => m.InputScheme, vm => vm.IsControlControllerSelected);
            WatchModelProperty(m => m.AvailableInputSchemes, vm => vm.IsControlKeyboardMouseDisabled);
            WatchModelProperty(m => m.AvailableInputSchemes, vm => vm.IsControlTouchscreenDisabled);
            WatchModelProperty(m => m.AvailableInputSchemes, vm => vm.IsControlControllerDisabled);

            Model.DeleteAccountFailure
                 .Subscribe(OnDeleteAccountFailure)
                 .AddTo(Model.CompositeDisposable);

            this.SubscribeNativeBackButton(_nativeButtonsHandler);

            IsSoundOn = !_soundManager.Mapper.IsSoundMute;
            CurrentTab = SettingsTab.Settings.ToString();
        }

        #region Bindings for Buttons

        public void ChangeLanguage(string language)
        {
            if(language == Model.SelectedLanguage)
            {
                return;
            }

            _uiManager
                .SystemMessage("SETTINGS_CHANGE_LANGUAGE_TITLE", "SETTINGS_CHANGE_LANGUAGE_MESSAGE")
                .WithButton("SETTINGS_CHANGE_LANGUAGE_YES", () => Model.ChangeLanguage(language))
                .WithButton("SETTINGS_CHANGE_LANGUAGE_NO", null, true);
        }

        [Binding]
        public void Close()
        {
            CloseClickHandler();
        }

        [Binding]
        public void OpenSettingsPage()
        {
            CurrentTab = SettingsTab.Settings.ToString();
        }

        [Binding]
        public void OpenSupportPage()
        {
            CurrentTab = SettingsTab.Support.ToString();
        }

        [Binding]
        public void OpenAboutPage()
        {
            CurrentTab = SettingsTab.About.ToString();
        }

        [Binding]
        public void OpenControlsPage()
        {
            CurrentTab = SettingsTab.Controls.ToString();
        }

        [Binding]
        public void LanguagesClickHandler()
        {
            IsLanguagesGroupOpen = !IsLanguagesGroupOpen;
        }

        [Binding]
        public void SoundEnableClickHandler()
        {
            _soundManager.Mapper.Mute(IsSoundOn);
            IsSoundOn = !_soundManager.Mapper.IsSoundMute;
        }

        [Binding]
        public void MoveJoyFollowToFingerChangeClickHandler()
        {
            Model.ChangeFollowToFingerMode();
        }

        [Binding]
        public void InputSchemeControllerChangeClickHandler()
        {
            Model.ChangeInputScheme(InputScheme.Controller);
        }

        [Binding]
        public void InputSchemeTouchscreenChangeClickHandler()
        {
            Model.ChangeInputScheme(InputScheme.Touchscreen);
        }

        [Binding]
        public void InputSchemeMouseAndKeyboardChangeClickHandler()
        {
            Model.ChangeInputScheme(InputScheme.MouseAndKeyboard);
        }

        [Binding]
        public void LinkAccountClickHandler()
        {
            Model.LinkAccount(FnChangeUser);
        }

        [Binding]
        public void FeedbackClickHandler()
        {
            Model.ShareFeedback(FeedbackLincCode.EmailBug);
        }

        [Binding]
        public void FAQClickHandler()
        {
            Model.ShareFeedback(FeedbackLincCode.Faq);
        }

        [Binding]
        public void GoDiscord()
        {
            Model.ShareFeedback(FeedbackLincCode.Discord);
        }

        [Binding]
        public void GoFacebook()
        {
            Model.ShareFeedback(FeedbackLincCode.Facebook);
        }

        [Binding]
        public void GoYoutube()
        {
            Model.ShareFeedback(FeedbackLincCode.Youtube);
        }

        [Binding]
        public void GoVkontakte()
        {
            Model.ShareFeedback(FeedbackLincCode.Vkontakte);
        }

        [Binding]
        public void GoInstagram()
        {
            Model.ShareFeedback(FeedbackLincCode.Instagram);
        }

        [Binding]
        public void GoTwitter()
        {
            Model.ShareFeedback(FeedbackLincCode.Twitter);
        }

        [Binding]
        public void GoReddit()
        {
            Model.ShareFeedback(FeedbackLincCode.Reddit);
        }

        [Binding]
        public void GoTiktok()
        {
            Model.ShareFeedback(FeedbackLincCode.Tiktok);
        }

        [Binding]
        public void PrivacyPolicyClickHandler()
        {
            Model.ShareFeedback(FeedbackLincCode.PrivacyPolicy);
        }

        [Binding]
        public void TermsOfUseClickHandler()
        {
            Model.ShareFeedback(FeedbackLincCode.TermsOfUse);
        }

        [Binding]
        public void EmailClickHandler()
        {
            Model.ShareFeedback(FeedbackLincCode.Email);
        }

        [Binding]
        public void RequestToDeleteAccount()
        {
            _uiManager.SystemMessage("DELETE_ACCOUNT_TITLE", "DELETE_ACCOUNT_TEXT")
                      .WithCloseButton(() => { })
                      .WithNativeClose(_nativeButtonsHandler, () => { })
                      .WithButton("BUTTON_OKAY_TEXT", Model.DeleteAccount)
                      .WithButton("CANCEL", null, true);
        }

        #endregion

        private IObservable<bool> FnChangeUser(UserProfileStatsEntry profile, bool currentLinked)
        {
            var subject = RxExtensions.CreateSubject<bool>();

            void OnYes()
            {
                subject.OnNext(true);
                subject.OnCompleted();
            }

            void OnNo()
            {
                subject.OnNext(false);
                subject.OnCompleted();
            }

            var message = currentLinked
                ? "CHANGE_USER_ACCOUNT_MESSAGE_RECONNECT"
                : "CHANGE_USER_ACCOUNT_MESSAGE_LOSE";

            message = string.Format(
                _localizer.Localize(message),
                profile?.Name ?? "",
                profile?.BattleCount ?? 0
            );

            _uiManager
                .SystemMessage("CHANGE_USER_ACCOUNT_TITLE", message)
                .WithButton("CHANGE_USER_ACCOUNT_BTN_OK", OnYes)
                .WithButton("CHANGE_USER_ACCOUNT_BTN_CANCEL", OnNo, true);

            return subject;
        }

        private void OnDeleteAccountFailure(bool status)
        {
            _uiManager.SystemMessage("DELETE_ACCOUNT_FAILURE_TITLE", "DELETE_ACCOUNT_FAILURE_TEXT")
                      .WithCloseButton(() => { })
                      .WithNativeClose(_nativeButtonsHandler, () => { })
                      .WithButton("BUTTON_OKAY_TEXT", null, true);
        }

        public enum SettingsTab
        {
            Settings,
            Controls,
            Support,
            About
        }

        public void CloseClickHandler()
        {
            Model.Dispose();
        }
    }
}
