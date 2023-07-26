﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using WixToolset.Mba.Core;

using mba = WixToolset.Mba.Core;

using PackageState = WixToolset.Mba.Core.PackageState;

[assembly: BootstrapperApplicationFactory(typeof(WixToolset.WixBA.WixBAFactory))]

namespace WixToolset.WixBA
{
    public class WixBAFactory : BaseBootstrapperApplicationFactory
    {
        protected override mba.IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand command)
        {
            return new ManagedBA(engine, command);
        }
    }
}

public class ManagedBA : mba.BootstrapperApplication
{
    public ManagedBA(mba.IEngine engine, mba.IBootstrapperCommand command) : base(engine)
    {
        this.Command = command;
    }

    public mba.IEngine Engine => base.engine;
    public mba.IBootstrapperCommand Command;

    /// <summary>
    /// Entry point that is called when the bootstrapper application is ready to run.
    /// </summary>
    protected override void Run()
    {
        new MainView(this).ShowDialog();
        engine.Quit(0);
    }
}

public class MainViewModel : INotifyPropertyChanged
{
    protected void OnPropertyChanged(string propertyName)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public IntPtr ViewHandle;

    public MainViewModel(ManagedBA bootstrapper)

    {
        this.IsBusy = false;

        this.Bootstrapper = bootstrapper;
        this.Bootstrapper.Error += this.OnError;
        this.Bootstrapper.ApplyComplete += this.OnApplyComplete;
        this.Bootstrapper.DetectPackageComplete += this.OnDetectPackageComplete;
        this.Bootstrapper.PlanComplete += this.OnPlanComplete;

        this.Bootstrapper.Engine.Detect();
    }

    void OnError(object sender, mba.ErrorEventArgs e)
    {
        MessageBox.Show(e.ErrorMessage);
    }

    bool installEnabled;

    public bool InstallEnabled
    {
        get { return installEnabled; }
        set
        {
            installEnabled = value;
            OnPropertyChanged("InstallEnabled");
        }
    }

    string userInput = "User input content...";

    public string UserInput
    {
        get => userInput;

        set
        {
            userInput = value;
            OnPropertyChanged("UserInput");
        }
    }

    bool uninstallEnabled;

    public bool UninstallEnabled
    {
        get { return uninstallEnabled; }
        set
        {
            uninstallEnabled = value;
            OnPropertyChanged("UninstallEnabled");
        }
    }

    bool isThinking;

    public bool IsBusy
    {
        get { return isThinking; }
        set
        {
            isThinking = value;
            OnPropertyChanged("IsBusy");
        }
    }

    public ManagedBA Bootstrapper { get; set; }

    public void InstallExecute()
    {
        IsBusy = true;

        Bootstrapper.Engine.SetVariableString("UserInput", UserInput, false);
        Bootstrapper.Engine.Plan(mba.LaunchAction.Install);
    }

    public void UninstallExecute()
    {
        IsBusy = true;
        Bootstrapper.Engine.Plan(mba.LaunchAction.Uninstall);
    }

    public void ExitExecute()
    {
        //Dispatcher.BootstrapperDispatcher.InvokeShutdown();
    }

    /// <summary>
    /// Method that gets invoked when the Bootstrapper ApplyComplete event is fired.
    /// This is called after a bundle installation has completed. Make sure we updated the view.
    /// </summary>
    void OnApplyComplete(object sender, mba.ApplyCompleteEventArgs e)
    {
        IsBusy = false;
        InstallEnabled = false;
        UninstallEnabled = false;
    }

    /// <summary>
    /// Method that gets invoked when the Bootstrapper DetectPackageComplete event is fired.
    /// Checks the PackageId and sets the installation scenario. The PackageId is the ID
    /// specified in one of the package elements (msipackage, exepackage, msppackage,
    /// msupackage) in the WiX bundle.
    /// </summary>
    void OnDetectPackageComplete(object sender, mba.DetectPackageCompleteEventArgs e)
    {
        // Debug.Assert(false);
        if (e.PackageId == "MyProductPackageId")
        {
            if (e.State == PackageState.Absent)
            {
                InstallEnabled = true;
            }
            // else if (e.State == PackageState.Present)
            else if (e.State == PackageState.Present || e.State == PackageState.Cached)
            {
                // need to add cache because of the bug in WiX https://github.com/wixtoolset/issues/issues/7399
                // interestingly enough WiX v4.0.1 marks `PackageState.Cached` as obsolete but...
                // still does not set e.State to present (to cached instead) if the product is installed
                UninstallEnabled = true;
            }
        }
    }

    /// <summary>
    /// Method that gets invoked when the Bootstrapper PlanComplete event is fired.
    /// If the planning was successful, it instructs the Bootstrapper Engine to
    /// install the packages.
    /// </summary>
    void OnPlanComplete(object sender, mba.PlanCompleteEventArgs e)
    {
        if (e.Status >= 0)
            Bootstrapper.Engine.Apply(ViewHandle);
    }
}