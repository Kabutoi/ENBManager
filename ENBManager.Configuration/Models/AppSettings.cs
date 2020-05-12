﻿using ENBManager.Configuration.Models.Base;

namespace ENBManager.Configuration.Models
{
    public class AppSettings : BaseSettings
    {
        #region Private Members

        private const string FILE_NAME = "appsettings.json"; 

        #endregion

        #region Settings

        public bool Initialized { get; set; }

        #endregion

        #region Constructor

        public AppSettings()
            : base(FILE_NAME)
        {
            // Set factory values
            Initialized = false;
        }

        #endregion
    }
}
