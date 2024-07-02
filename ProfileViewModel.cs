using Core.Client.GameEventSystem;
using Core.Client.UI;
using Core.Client.Unity.UI;
using Core.Client.Unity.UI.Binding;
using Core.Client.Unity.UI.Extensions;
using Game.Client.App;
using Game.Client.Model.Generated.AnalyticsEvent;
using Game.Client.Helpers;
using Game.Client.UI.Models.Profile;
using Game.Client.Unity.Obsolete;
using Game.Client.Unity.ResourcesSystem;
using Game.Client.Unity.UI.Helpers;
using Game.Client.Unity.UI.ViewModels.Competition.Championships;
using Game.Client.Unity.UI.ViewModels.Promocode;
using Game.Client.Unity.UI.ViewModels.Registration;
using Game.Client.Unity.UI.ViewModels.UserBadges;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Game.Client.Unity.UI.ViewModels.Profile
{
    [Binding]
    public class ProfileViewModel : ViewModelWithSubViewModelBase<ProfileModel, ProfileViewModel>, IBackButton
    {
        private readonly IUIManager _uiManager;
        private readonly ITopControlsController _topControlsController;
        private readonly IUnityAppAdapter _unityAppAdapter;
        private readonly ILocalizer _localizer;
        private readonly IResourcesLocator _resources;
        private readonly IAtlasProvider _atlasProvider;
        private readonly ILocalPlayerPrefs _localPlayerPrefs;
        private readonly INativeButtonsHandler _nativeButtonsHandler;

        #region Common

        private UserBadgesSubViewModel _userBadges;

        [Binding]
        public string NameLabel => Model.Name;

        [Binding]
        public string ProfileIdLabel => Model.UserCode;

        [Binding]
        public int NameThemeId => Model.NameStyleId;

        [Binding]
        public bool IsNewAvatarsAvailable => Model.IsNewAvatarsAvailable;


        [Binding]
        public Sprite PlayerAvatarSprite => ProfileIconsHelper.GetPlayerAvatarSprite(Model.AvatarId, _resources, _atlasProvider);

        [Binding]
        public Sprite CountryFlagSprite => ProfileIconsHelper.GetCountryFlagSprite(Model.CountryCode, _resources, _atlasProvider);

        [Binding]
        public UserBadgesSubViewModel UserBadges
        {
            get => _userBadges;
            private set => SetField(ref _userBadges, value);
        }

        #endregion


        #region Statistics

        [Binding]
        public bool IsChampionshipUnlocked => Model.IsChampionshipUnlocked;

        [Binding]
        public string ChampionshipToUnlockedValue => Model.ChampionshipToUnlock.ToString();

        [Binding]
        public string CharactersCountLabel => $"{Model.UnlockedCharacters}/{Model.TotalCharactersCount}";

        [Binding]
        public string BattlesCountLabel => Model.BattlesCount.ToString();

        [Binding]
        public Sprite LeagueSprite => _resources.GetLeagueIcon(Model.MaxLeague, _atlasProvider);

        [Binding]
        public string LeagueLabel => _localizer.Localize($"LEAGUE_{Model.MaxLeague}_HEADER");

        [Binding]
        public string VictoriesCountLabel
        {
            get
            {
                if(Model.BattlesCount == 0 || Model.VictoriesCount == 0)
                {
                    return "0%";
                }

                var percent = (float)Model.VictoriesCount / Model.BattlesCount * 100;
                return $"{percent:F1}%";
            }
        }

        [Binding]
        public string TrophiesCountLabel => Model.TrophiesCount.ToString();

        #endregion


        #region Promocode

        private bool _isPromocodePopupWasShown;

        [Binding]
        public bool IsPromocodePopupWasShown
        {
            get => _isPromocodePopupWasShown;
            private set => SetField(ref _isPromocodePopupWasShown, value);
        }

        [Binding]
        public bool IsShareAvailable => Model.IsShareAvailable;

        #endregion


        public ProfileViewModel
        (
            ProfileModel model,
            IUIManager uiManager,
            ITopControlsController topControlsController,
            IUnityAppAdapter unityAppAdapter,
            ILocalizer localizer,
            IResourcesLocator resources,
            IAtlasProvider atlasProvider,
            ILocalPlayerPrefs localPlayerPrefs,
            INativeButtonsHandler nativeButtonsHandler,
            ISubViewModelFactory subViewModelFactory,
            ILogger log
        )
            : base(model, subViewModelFactory, log)
        {
            _uiManager = uiManager;
            _topControlsController = topControlsController;
            _unityAppAdapter = unityAppAdapter;
            _localizer = localizer;
            _resources = resources;
            _atlasProvider = atlasProvider;
            _localPlayerPrefs = localPlayerPrefs;
            _nativeButtonsHandler = nativeButtonsHandler;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // ======================== Common ======================== //
            BindSubViewModel(m => m.UserBadges, vm => vm.UserBadges);

            WatchModelProperty(m => m.Name, vm => vm.NameLabel);
            WatchModelProperty(m => m.UserCode, vm => vm.ProfileIdLabel);
            WatchModelProperty(m => m.NameStyleId, vm => vm.NameThemeId);
            WatchModelProperty(m => m.AvatarId, vm => vm.PlayerAvatarSprite);
            WatchModelProperty(m => m.CountryCode, vm => vm.CountryFlagSprite);
            WatchModelProperty(m => m.IsChampionshipUnlocked, vm => vm.IsChampionshipUnlocked);
            WatchModelProperty(m => m.ChampionshipToUnlock, vm => vm.ChampionshipToUnlockedValue);

            // ======================== Statistics ======================== //
            WatchModelProperty(m => m.UnlockedCharacters, vm => vm.CharactersCountLabel);
            WatchModelProperty(m => m.TotalCharactersCount, vm => vm.CharactersCountLabel);
            WatchModelProperty(m => m.BattlesCount, vm => vm.BattlesCountLabel);
            WatchModelProperty(m => m.MaxLeague, vm => vm.LeagueLabel);
            WatchModelProperty(m => m.MaxLeague, vm => vm.LeagueSprite);
            WatchModelProperty(m => m.BattlesCount, vm => vm.VictoriesCountLabel);
            WatchModelProperty(m => m.VictoriesCount, vm => vm.VictoriesCountLabel);
            WatchModelProperty(m => m.TrophiesCount, vm => vm.TrophiesCountLabel);

            WatchModelProperty(m => m.IsNewAvatarsAvailable, vm => vm.IsNewAvatarsAvailable);

            // ======================== Promocode ======================== //
            WatchModelProperty(m => m.IsShareAvailable, vm => vm.IsShareAvailable);
            IsPromocodePopupWasShown = _localPlayerPrefs.GetBool(LocalPlayerPrefs.ReferralPromocodeShown);

            new GameViewEventProfileScreenAppear(Model.ProfileId).Publish();

            this.SubscribeNativeBackButton(_nativeButtonsHandler);
        }

        protected override void OnFocusedChanged(bool isFocused)
        {
            base.OnFocusedChanged(isFocused);

            if(isFocused)
            {
                _topControlsController.SetState(TopControlsState.None);
            }
        }

        [Binding]
        public void CloseClickHandler()
        {
            Model.Dispose();
        }

        [Binding]
        public void CopyProfileIdClickHandler()
        {
            _unityAppAdapter.SystemCopyBuffer = Model.UserCode;

            _uiManager.InfoMessage("MESSAGE_PROFILE_COPIED");
        }

        [Binding]
        public void ChangeAvatarClickHandler()
        {
            _uiManager.ShowView<ChangeAvatarViewModel>();
        }

        [Binding]
        public void ChangeNameClickHandler()
        {
            _uiManager.ShowView<RegistrationViewModel>();
        }

        [Binding]
        public void OpenPromoCodeClickHandler()
        {
            if(IsPromocodePopupWasShown == false)
            {
                IsPromocodePopupWasShown = true;
                _localPlayerPrefs.SetBool(LocalPlayerPrefs.ReferralPromocodeShown, true);
            }

            _uiManager.ShowView<PromocodeViewModel>();
        }
    }
}
