﻿using ENBManager.Core.Helpers;
using ENBManager.Core.Interfaces;
using ENBManager.Core.Services;
using ENBManager.Core.Views;
using ENBManager.Infrastructure.BusinessEntities;
using NLog;
using Prism.Commands;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ENBManager.Core.ViewModels
{
    public class SideMenuViewModel : BindableBase
    {
        #region Private Members

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IConfigurationManager<AppSettings> _configurationManager;
        private readonly IDialogService _dialogService;
        private readonly IFileService _fileService;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly IModuleManager _moduleManager;

        private InstalledGame _selectedGame;

        #endregion

        #region Public Properties

        public ObservableCollection<InstalledGame> Games { get; set; }
        public InstalledGame SelectedGame
        {
            get { return _selectedGame; }
            set
            {
                _selectedGame = value;
                RaisePropertyChanged();

                if (value != null)
                    ActivateModule(value.Module);
            }
        }

        #endregion

        #region Helper Properties

        public bool DarkMode
        {
            get { return _configurationManager.Settings.DarkMode; }
            set
            {
                _configurationManager.Settings.DarkMode = value;
                _configurationManager.SaveSettings();

                ThemeHelper.UpdateTheme(value);
            }
        }
        public bool ShowDarkModeShortcut => _configurationManager.Settings.DarkModeShortcut;

        #endregion

        #region Commands

        public DelegateCommand GetDataCommand { get; set; }
        public DelegateCommand OpenSettingsCommand { get; set; }
        public DelegateCommand OpenDiscoverGamesCommand { get; set; }

        #endregion

        #region Constructor

        public SideMenuViewModel(
            IConfigurationManager<AppSettings> configurationManager,
            IDialogService dialogService,
            IFileService fileService, 
            IModuleCatalog moduleCatalog, 
            IModuleManager moduleManager)
        {
            _configurationManager = configurationManager;
            _dialogService = dialogService;
            _fileService = fileService;
            _moduleCatalog = moduleCatalog;
            _moduleManager = moduleManager;

            GetDataCommand = new DelegateCommand(OnGetDataCommand);
            OpenSettingsCommand = new DelegateCommand(OnOpenSettingsCommand);
            OpenDiscoverGamesCommand = new DelegateCommand(OnOpenDiscoverGamesCommand);

            _logger.Debug($"{nameof(SideMenuViewModel)} initialized");
        }

        #endregion

        #region Private Methods

        private void OnGetDataCommand()
        {
            _logger.Debug(nameof(OnGetDataCommand));

            GameSettings gameSettings;

            Games = new ObservableCollection<InstalledGame>();

            var directories = _fileService.GetGameDirectories();

            foreach (var directory in directories)
            {
                var moduleInfo = _moduleCatalog.Modules.SingleOrDefault(x => x.ModuleName == new DirectoryInfo(directory).Name);
                var game = (InstalledGame)InstanceFactory.CreateInstance(Type.GetType(moduleInfo.ModuleType));

                gameSettings = new GameSettings(game.Module);
                game.Settings = ConfigurationManager<GameSettings>.LoadSettings(gameSettings.GetFilePath());

                Games.Add(game);
            }

            if (_configurationManager.Settings.OpenLastActiveGame && !string.IsNullOrEmpty(_configurationManager.Settings.LastActiveGame))
                _selectedGame = Games.FirstOrDefault(x => x.Module == _configurationManager.Settings.LastActiveGame);

            RaisePropertyChanged(nameof(Games));
            RaisePropertyChanged(nameof(SelectedGame));

            _logger.Info("Game list initialized");
        }

        private void OnOpenSettingsCommand()
        {
            _logger.Debug(nameof(OnOpenSettingsCommand));

            _dialogService.ShowDialog(nameof(AppSettingsDialog), new DialogParameters(), (dr) =>
            {
                if (dr.Result == ButtonResult.OK)
                {
                    _configurationManager.LoadSettings();

                    RaisePropertyChanged(nameof(DarkMode));
                    RaisePropertyChanged(nameof(ShowDarkModeShortcut));
                }
            });
        }

        private void OnOpenDiscoverGamesCommand()
        {
            _logger.Debug(nameof(OnOpenDiscoverGamesCommand));

            DialogParameters dp = new DialogParameters();
            dp.Add("Games", Games);

            _dialogService.ShowDialog(nameof(DiscoverGamesDialog), dp, (dr) =>
            {
                OnGetDataCommand();
            });
        }

        private void ActivateModule(string name)
        {
            var module = _moduleCatalog.Modules.SingleOrDefault(x => x.ModuleName == name);
            _moduleManager.LoadModule(module.ModuleName);

            _configurationManager.Settings.LastActiveGame = name;
            _configurationManager.SaveSettings();
        }

        #endregion
    }
}
