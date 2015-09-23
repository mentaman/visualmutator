﻿namespace PiotrTrzpil.VisualMutator_VSPackage
{
    #region

    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows;
    using Infrastructure;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Ninject;
    using VisualMutator.Controllers;

    #endregion

    ///<summary>
    ///  This is the class that implements the package exposed by this assembly.
    ///
    ///  The minimum requirement for a class to be considered a valid package for Visual Studio
    ///  is to implement the IVsPackage interface and register itself with the shell.
    ///  This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///  to do it: it derives from the Package class that provides the implementation of the 
    ///  IVsPackage interface and uses the registration attributes defined in the framework to 
    ///  register itself and its components with the shell.
    ///</summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidVisualMutator_VSPackagePkgString)]
    [Infrastructure.ProvideBindingPath]
    public sealed class VisualMutator_VSPackagePackage : Package
    {
        public static object MainControl;

        private readonly Bootstrapper _bootstrapper;

        /// <summary>
        ///   Default constructor of the package.
        ///   Inside this method you can place any initialization code that does not require 
        ///   any Visual Studio service because at this point the package object is created but 
        ///   not sited yet inside Visual Studio environment. The place to do all the other 
        ///   initialization is the Initialize method.
        /// </summary>
        public VisualMutator_VSPackagePackage()
        {
            Trace.WriteLine(
                string.Format(
                    CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));

            
                _bootstrapper = new Bootstrapper(this);
            
        }

        private void CommandMutateAndTest(object sender, EventArgs e)
        {
            Trace.WriteLine("CommandMutateAndTest");
            _bootstrapper.RunMutationSessionForCurrentPosition();
        }

        /// <summary>
        ///   This function is called when the user clicks the menu item that shows the 
        ///   tool window. See the Initialize method to see how the menu item is associated to 
        ///   this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            var windowFrame = (IVsWindowFrame)window.Frame;
            window.Content = _bootstrapper.Shell;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        #region Package Members

        /// <summary>
        ///   Initialization of the package; this method is called right after the package is sited, so this is the place
        ///   where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(
                string.Format(
                    CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                var toolwndCommandID = new CommandID(
                    GuidList.guidVisualMutator_VSPackageCmdSet, (int)PkgCmdIDList.cmdidVisualMutator);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
              //  VsMenus.
             //   var ctxCommandID = new CommandID(
             //      GuidList.guidVisualMutator_VSPackageCmdSet, (int)PkgCmdIDList.cmdidVisualMutatorCtx);
             //   mcs.AddCommand(new OleMenuCommand(CommandMutateAndTest, ctxCommandID));
                CommandID id = new CommandID(GuidList.guidVisualMutator_VSPackageCmdSet, PkgCmdIDList.cmdidMyCommand);
                // Now create the OleMenuCommand object for this command. The EventHandler object is the
                // function that will be called when the user will select the command.
                OleMenuCommand command = new OleMenuCommand(CommandMutateAndTest, id);
                // Add the command to the command service.
                mcs.AddCommand(command);
            }

            try
            {
                _bootstrapper.InitializePackage(this);
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
    }
}