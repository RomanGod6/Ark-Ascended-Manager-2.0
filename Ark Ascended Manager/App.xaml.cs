﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Pages;
using Ark_Ascended_Manager.ViewModels.Windows;
using Ark_Ascended_Manager.Views.Pages;
using Ark_Ascended_Manager.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Ark_Ascended_Manager.Services;

namespace Ark_Ascended_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();
                services.AddTransient<CreateServersPage>();
                services.AddTransient<ConfigPage>();
                services.AddSingleton<SchedulerService>();
                services.AddSingleton<BackupService>();
                services.AddSingleton<RconPanelPage>();
                services.AddSingleton<SyncConfigPage>();
                services.AddSingleton<ImportServersPage>();
                services.AddSingleton<IssueReportForm>();
                services.AddSingleton<IntegrationsPage>();
                services.AddTransient<PluginsPage>();
                services.AddTransient<PluginInstallPage>();
                services.AddSingleton<SteamVersionControl>();
                services.AddSingleton<ServerUpdateService>();
                services.AddSingleton<CreateSchedulePage>();
                services.AddSingleton<RestorePage>();

                

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ServersPage>();
                services.AddSingleton<ServersViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddTransient<CreateServersPageViewModel>();
                services.AddTransient<ConfigPageViewModel>();
                services.AddSingleton<RconPanelViewModel>();
                services.AddSingleton<SyncConfigViewModel>();
                services.AddSingleton<ImportServersPageViewModel>();
                services.AddSingleton<IntegrationsViewModel>();
                services.AddTransient<PluginsPageViewModel>();
                services.AddTransient<PluginInstallPageViewModel>();
            }).Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host.Start();
            var schedulerService = GetService<SchedulerService>();
            var BackupService = GetService<BackupService>();
            var steamVersionControl = GetService<SteamVersionControl>();
            steamVersionControl?.StartUpdateTimer();

            // Retrieve the ServerManager instance and start it
            // If ServerManager has a start method, call it here
            // serverManager.Start();
        }


        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
           

            var steamVersionControl = GetService<SteamVersionControl>();
            var BackupService = GetService<BackupService>();
            steamVersionControl?.StopUpdateTimer();

            await _host.StopAsync();
            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
