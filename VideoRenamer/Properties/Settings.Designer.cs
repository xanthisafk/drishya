﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Drishya.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.12.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultVideoFolder {
            get {
                return ((string)(this["DefaultVideoFolder"]));
            }
            set {
                this["DefaultVideoFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultScreenshotFolder {
            get {
                return ((string)(this["DefaultScreenshotFolder"]));
            }
            set {
                this["DefaultScreenshotFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double DefaultScreenshotInterval {
            get {
                return ((double)(this["DefaultScreenshotInterval"]));
            }
            set {
                this["DefaultScreenshotInterval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int DefaultVolume {
            get {
                return ((int)(this["DefaultVolume"]));
            }
            set {
                this["DefaultVolume"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ShouldAutoLoop {
            get {
                return ((bool)(this["ShouldAutoLoop"]));
            }
            set {
                this["ShouldAutoLoop"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IncludeAlreadySorted {
            get {
                return ((bool)(this["IncludeAlreadySorted"]));
            }
            set {
                this["IncludeAlreadySorted"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Drishya")]
        public string SortingPostfix {
            get {
                return ((string)(this["SortingPostfix"]));
            }
            set {
                this["SortingPostfix"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ShouldMoveOnSort {
            get {
                return ((bool)(this["ShouldMoveOnSort"]));
            }
            set {
                this["ShouldMoveOnSort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultDestinationFolder {
            get {
                return ((string)(this["DefaultDestinationFolder"]));
            }
            set {
                this["DefaultDestinationFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ShouldCreateScreenshotSubfolder {
            get {
                return ((bool)(this["ShouldCreateScreenshotSubfolder"]));
            }
            set {
                this["ShouldCreateScreenshotSubfolder"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source={databasePath};Version=3;")]
        public string DatabaseConnectionString {
            get {
                return ((string)(this["DatabaseConnectionString"]));
            }
        }
    }
}
