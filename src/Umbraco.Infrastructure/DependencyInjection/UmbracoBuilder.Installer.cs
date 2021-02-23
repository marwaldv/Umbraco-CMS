using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Install.InstallSteps;
using Umbraco.Cms.Core.Install.Models;
using Umbraco.Extensions;
using Umbraco.Web.Install;
using Umbraco.Web.Install.InstallSteps;

namespace Umbraco.Infrastructure.DependencyInjection
{
    public static partial class UmbracoBuilderExtensions
    {
        /// <summary>
        /// Adds the services for the Umbraco installer
        /// </summary>
        internal static IUmbracoBuilder AddInstaller(this IUmbracoBuilder builder)
        {
            // register the installer steps
            builder.Services.AddScoped<InstallSetupStep, NewInstallStep>();
            builder.Services.AddScoped<InstallSetupStep, UpgradeStep>();
            builder.Services.AddScoped<InstallSetupStep, FilePermissionsStep>();
            builder.Services.AddScoped<InstallSetupStep, TelemetryIdentifierStep>();
            builder.Services.AddScoped<InstallSetupStep, DatabaseConfigureStep>();
            builder.Services.AddScoped<InstallSetupStep, DatabaseInstallStep>();
            builder.Services.AddScoped<InstallSetupStep, DatabaseUpgradeStep>();

            // TODO: Add these back once we have a compatible Starter kit
            // composition.Services.AddScoped<InstallSetupStep,StarterKitDownloadStep>();
            // composition.Services.AddScoped<InstallSetupStep,StarterKitInstallStep>();
            // composition.Services.AddScoped<InstallSetupStep,StarterKitCleanupStep>();
            builder.Services.AddScoped<InstallSetupStep, CompleteInstallStep>();

            builder.Services.AddTransient<InstallStepCollection>();
            builder.Services.AddUnique<InstallHelper>();

            return builder;
        }
    }
}