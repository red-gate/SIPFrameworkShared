using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RedGate.SIPFrameworkShared.Connections;
using RedGate.SIPFrameworkShared.ObjectExplorer;

namespace RedGate.SIPFrameworkShared
{
    /// <summary>
    /// To support backwards compatibility, once an interface is published it must not be changed. When we want to add 
    /// new features to an interface, we must create a new interface that inherits form the one we want to change. It
    /// is then the responsibility of the addin to cast the interface to the revision is requires.
    /// 
    /// In this way, the oldest addins always receive ISsmsFunctionality provider as their entry point to the addin 
    /// framework. Newer addins will also receive an ISsmsFunctionality provider but will cast it to the version they
    /// need. For example ISsmsFunctionalityProvider3.
    /// </summary>
    public interface ISsmsFunctionalityProvider
    {
        /// <summary>
        /// Adds a menu item to the ObjectExplorer's context menu under a shared "Red Gate" top level context menu.
        /// </summary>
        /// <param name="menuItem">The menu item to add.</param>
        void AddMenuItem(SimpleOeMenuItemBase menuItem);

        /// <summary>
        /// Adds a menu item to the ObjectExplorer's context menu.
        /// </summary>
        /// <param name="menuItem"></param>
        void AddTopLevelMenuItem(SimpleOeMenuItemBase menuItem);
    }

    /// <summary>
    /// This should be called ISsmsFunctionalityProvider2.
    /// 
    /// This interface does not follow the naming convention, but cannot be renamed because we must maintain backwards compatibility.
    /// </summary>
    public interface ISsmsExtendedFunctionalityProvider : ISsmsFunctionalityProvider
    {
        /// <summary>
        /// Create and opens a docked tool window in SSMS that hosts a user-defined .Net control.
        /// 
        /// The control type is specified, the control is loaded by reflection from SSMS itself with the default constructor. You will 
        /// need to use the returned ISsmsTabPage to access the instance of you control and pass in any initialization data.
        /// </summary>
        /// <param name="type">This must be a .Net control and must be ComVisible.</param>
        /// <param name="title">The title of the window</param>
        /// <param name="guid">A GUID for the window</param>
        /// <returns>An ISsmsTabPage that can be used as a handle to interact with the new window.</returns>
        ISsmsTabPage CreateTabPage(Type type, string title, string guid);

        /// <summary>
        /// This can be used to explore the server connections open in SSMS.
        /// </summary>
        /// <returns>The dictionary keys describe a server connection. The values are lists of the databases on those servers.</returns>
        Dictionary<DatabaseConnectionDescriptor, List<string>> GetConnectionsAndDatabases();

        /// <summary>
        /// Provides access to Red Gate's feature usage reporting service. If the user has opted into feature usage reporting messages 
        /// can be sent back to Red Gate about the feature that have been used.
        /// </summary>
        /// <param name="message">The feature message</param>
        void ReportUsage(string message);

        /// <summary>
        /// Allows you to open a connection to a server from within SSMS. SSMS will display the standard connection dialog, the function 
        /// will return the connection once the dialog is closed. If the connection already exists it's details will be returned.
        /// 
        /// Really this should be replaced by function which does not need the IWin32Window reference, since the framework can figure that out.
        /// </summary>
        /// <param name="parent">The handle for the main SSMS window</param>
        /// <param name="server">The server you want a connection to</param>
        /// <param name="database">The default database for the connection</param>
        /// <returns>Information about the connection</returns>
        DatabaseConnectionDescriptor GetConnection(IWin32Window parent, String server, String database = null);

        /// <summary>
        /// Tests if a particular server connection exists within SSMS.
        /// 
        /// The database parameter doesn't seem to be necessary.
        /// </summary>
        /// <param name="server">The server you want to test is connectioned</param>
        /// <param name="database"></param>
        /// <param name="connectionDescriptor"></param>
        /// <returns></returns>
        bool ConnectionExists(String server, String database, out DatabaseConnectionDescriptor connectionDescriptor);
    }

    public interface ISsmsFunctionalityProvider3 : ISsmsExtendedFunctionalityProvider
    {
        /// <summary>
        /// Returns an instance of ISsmsQueryWindowManager. You should probably use IQueryWindowServices available
        /// in ISsmsFunctionalityProvider4 unless you want to augment a query window by docking a custom control
        /// to it.
        /// 
        /// Query windows are the text windows that you can type SQL into execute.
        /// </summary>
        /// <returns></returns>
        ISsmsQueryWindowManager GetQueryWindowManager();

        /// <summary>
        /// Adds a menu to the Red Gate toolbar
        /// </summary>
        /// <param name="command">The command to add to the Red Gate toolbar.</param>
        void AddToolbarItem(ISharedCommand command);

        /// <summary>
        /// Adds a menu to the main Tools menu
        /// </summary>
        /// <param name="command">The commenu to add to the tools menu</param>
        void AddToolsMenuItem(ISharedCommand command);
    }

    public interface ISsmsFunctionalityProvider4 : ISsmsFunctionalityProvider3
    {
        /// <summary>
        /// Provides a way of registering a command that takes an argument. This has been used allow addins to pass
        /// information between them. 
        /// </summary>
        /// <param name="command"></param>
        void AddGlobalCommand(ISharedCommandWithExecuteParameter command);

        /// <summary>
        /// The SSMS Dte2 object.
        /// </summary>
        object SsmsDte2 { get; }

        /// <summary>
        /// The origin AddinInstance.
        /// </summary>
        object SsmsAddinInstance { get; }

        /// <summary>
        /// IQueryWindowServices will provide a collection of utilties for interacting with query windows. You should 
        /// check if you can cast it to a later version of the interface.
        /// 
        /// Query windows are the text windows that you can type SQL into execute.
        /// </summary>
        IQueryWindowServices QueryWindow { get; }

        /// <summary>
        /// Provides a fluent API for building main menus.
        /// </summary>

        IMainSsmsWindowProvider MainWindows { get; }
    }

    public interface ISsmsFunctionalityProvider5 : ISsmsFunctionalityProvider4
    {
        /// <summary>
        /// Tool windows are dockable controls with SSMS
        /// </summary>
        IToolWindowService ToolWindow { get; }

        /// <summary>
        /// Provides useful information about the SSMS and the environment
        /// </summary>
        ISsmsInfo SsmsInfo { get; }

        /// <summary>
        /// Events raised adding and removing server connections
        /// </summary>
        IConnectionManager ConnectionManager { get; }

        /// <summary>
        /// Provides a handle to the main SSMS window. Useful for modal windows.
        /// </summary>
        ISsmsMainWindow MainWindow { get; }
    }

    public interface ISsmsFunctionalityProvider6 : ISsmsFunctionalityProvider5
    {
        /// <summary>
        /// This can be used to explore the server connections open in SSMS.
        /// </summary>
        /// <returns>The dictionary keys describe a server connection. The values are lists of the databases on those servers.</returns>
        new Dictionary<IConnectionInfo2, IEnumerable<string>> GetConnectionsAndDatabases();

        /// <summary>
        /// Provides additional functions to control command registered with SSMS
        /// </summary>
        ICommandManager CommandManager { get; }

        /// <summary>
        /// Provides events and state of the object explorer
        /// </summary>
        IObjectExplorerWatcher ObjectExplorerWatcher { get; }

        /// <summary>
        /// A wrapper around SMO functions.
        /// 
        /// Every version of SSMS loads a different version of SMO. If you can't provide your own SMO implementation this provides a common interface across the one SSMS has loaded.
        /// </summary>
        IServerManagementObjectsAdapter ServerManagementObjects { get; }

        /// <summary>
        /// IQueryWindowServices will provide a collection of utilties for interacting with query windows. You should 
        /// check if you can cast it to a later version of the interface.
        /// 
        /// Query windows are the text windows that you can type SQL into execute.
        /// </summary>
        new IQueryWindowServices2 QueryWindow { get; }
    }

    public interface ISsmsFunctionalityProvider7 : ISsmsFunctionalityProvider6
    {
        /// <summary>
        /// Provides events and state of the object explorer
        /// </summary>
        new IObjectExplorerWatcher2 ObjectExplorerWatcher { get; }
    }

    public interface ISsmsFunctionalityProvider8 : ISsmsFunctionalityProvider7
    {
        /// <summary>
        /// A wrapper around SMO functions.
        /// 
        /// Every version of SSMS loads a different version of SMO. If you can't provide your own SMO implementation this provides a common interface across the one SSMS has loaded.
        /// </summary>
        new IServerManagementObjectsAdapter2 ServerManagementObjects { get; }
    }
}
