﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18010
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.AspNet.SignalR.SqlServer {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.AspNet.SignalR.SqlServer.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR SQL scale out schema is newer than the currently executing version..
        /// </summary>
        internal static string Error_SignalRSQLScaleOutNewerThanCurrentVersion {
            get {
                return ResourceManager.GetString("Error_SignalRSQLScaleOutNewerThanCurrentVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The streamIndex value {0} is higher than the maximum configured index {1}.
        /// </summary>
        internal static string Error_StreamIndexOutOfRange {
            get {
                return ResourceManager.GetString("Error_StreamIndexOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected SqlNotificationType was received. Details: Type={0}, Source={1}, Info={2}.
        /// </summary>
        internal static string Error_UnexpectedSqlNotificationType {
            get {
                return ResourceManager.GetString("Error_UnexpectedSqlNotificationType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SQL Server edition of the target server is unsupported, e.g. SQL Azure..
        /// </summary>
        internal static string Error_UnsupportedSqlEdition {
            get {
                return ResourceManager.GetString("Error_UnsupportedSqlEdition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported value for parameter &apos;{0}&apos;. The value must be greater than 1..
        /// </summary>
        internal static string Error_ValueMustBeGreaterThan1 {
            get {
                return ResourceManager.GetString("Error_ValueMustBeGreaterThan1", resourceCulture);
            }
        }
    }
}
