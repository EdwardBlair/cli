﻿// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Tests
{
    public class GivenThatTheUserIsRunningDotNetForTheFirstTime : TestBase
    {
        private static CommandResult _firstDotnetNonVerbUseCommandResult;
        private static CommandResult _firstDotnetVerbUseCommandResult;
        private static DirectoryInfo _nugetFallbackFolder;

        static GivenThatTheUserIsRunningDotNetForTheFirstTime()
        {
            var testDirectory = TestAssets.CreateTestDirectory("Dotnet_first_time_experience_tests");
            var testNuGetHome = Path.Combine(testDirectory.FullName, "nuget_home");

            var command = new DotnetCommand()
                .WithWorkingDirectory(testDirectory);
            command.Environment["HOME"] = testNuGetHome;
            command.Environment["USERPROFILE"] = testNuGetHome;
            command.Environment["APPDATA"] = testNuGetHome;
            command.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "";
            command.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "";
            command.Environment["SkipInvalidConfigurations"] = "true";

            _firstDotnetNonVerbUseCommandResult = command.ExecuteWithCapturedOutput("--info");
            _firstDotnetVerbUseCommandResult = command.ExecuteWithCapturedOutput("new --debug:ephemeral-hive");

            _nugetFallbackFolder = new DirectoryInfo(Path.Combine(testNuGetHome, ".dotnet", "NuGetFallbackFolder"));
        }

        [Fact]
        public void UsingDotnetForTheFirstTimeSucceeds()
        {
            _firstDotnetVerbUseCommandResult
                .Should()
                .Pass();
        }

        [Fact]
        public void UsingDotnetForTheFirstTimeWithNonVerbsDoesNotPrintEula()
        {
            const string firstTimeNonVerbUseMessage = @".NET Command Line Tools";

            _firstDotnetNonVerbUseCommandResult.StdOut
                .Should()
                .StartWith(firstTimeNonVerbUseMessage);
        }

        [Fact]
        public void ItShowsTheAppropriateMessageToTheUser()
        {
            string firstTimeUseWelcomeMessage = NormalizeLineEndings(@"Welcome to .NET Core!
---------------------
Learn more about .NET Core @ https://aka.ms/dotnet-docs. Use dotnet --help to see available commands or go to https://aka.ms/dotnet-cli-docs.

Telemetry
--------------
The .NET Core tools collect usage data in order to improve your experience. The data is anonymous and does not include command-line arguments. The data is collected by Microsoft and shared with the community.
You can opt out of telemetry by setting a DOTNET_CLI_TELEMETRY_OPTOUT environment variable to 1 using your favorite shell.
You can read more about .NET Core tools telemetry @ https://aka.ms/dotnet-cli-telemetry.

Configuring...
-------------------
A command is running to initially populate your local package cache, to improve restore speed and enable offline access. This command will take up to a minute to complete and will only happen once.");

            // normalizing line endings as git is occasionally replacing line endings in this file causing this test to fail
            NormalizeLineEndings(_firstDotnetVerbUseCommandResult.StdOut)
                .Should().Contain(firstTimeUseWelcomeMessage)
                     .And.NotContain("Restore completed in");
        }

        [Fact]
        public void ItShowsTheAppropriateMessageToTheUserWhenEnvironmentVariablesAreSet()
        {
            string telemetryOptInMessage = NormalizeLineEndings(@"Telemetry
--------------
The .NET Core tools collect usage data in order to improve your experience. The data is anonymous and does not include command-line arguments. The data is collected by Microsoft and shared with the community.
You can opt out of telemetry by setting a DOTNET_CLI_TELEMETRY_OPTOUT environment variable to 1 using your favorite shell.
You can read more about .NET Core tools telemetry @ https://aka.ms/dotnet-cli-telemetry.");

            var telemetryOptInRootPath = TestAssets.CreateTestDirectory().FullName;
            var telemetryOptInNuGetHome = Path.Combine(telemetryOptInRootPath, "nuget_home");
            var telemetryOptInCommand = new DotnetCommand()
                .WithWorkingDirectory(telemetryOptInRootPath);
            telemetryOptInCommand.Environment["HOME"] = telemetryOptInNuGetHome;
            telemetryOptInCommand.Environment["USERPROFILE"] = telemetryOptInNuGetHome;
            telemetryOptInCommand.Environment["APPDATA"] = telemetryOptInNuGetHome;
            telemetryOptInCommand.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "";
            var telemetryOptInCommandResult = telemetryOptInCommand.ExecuteWithCapturedOutput("new --debug:ephemeral-hive");
            NormalizeLineEndings(telemetryOptInCommandResult.StdOut)
                .Should().Contain(telemetryOptInMessage);

            var telemetryOptOutRootPath = TestAssets.CreateTestDirectory().FullName;
            var telemetryOptOutNuGetHome = Path.Combine(telemetryOptOutRootPath, "nuget_home");
            var telemetryOptOutCommand = new DotnetCommand()
                .WithWorkingDirectory(telemetryOptOutRootPath);
            telemetryOptOutCommand.Environment["HOME"] = telemetryOptOutNuGetHome;
            telemetryOptOutCommand.Environment["USERPROFILE"] = telemetryOptOutNuGetHome;
            telemetryOptOutCommand.Environment["APPDATA"] = telemetryOptOutNuGetHome;
            telemetryOptOutCommand.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            var telemetryOptOutCommandResult = telemetryOptOutCommand.ExecuteWithCapturedOutput("new --debug:ephemeral-hive");
            NormalizeLineEndings(telemetryOptOutCommandResult.StdOut)
                .Should().NotContain(telemetryOptInMessage);
        }

        [Fact]
        public void ItCreatesASentinelFileUnderTheNuGetCacheFolder()
        {
            _nugetFallbackFolder
                .Should()
                .HaveFile($"{GetDotnetVersion()}.dotnetSentinel");
    	}

        [Fact]
        public void ItRestoresTheNuGetPackagesToTheNuGetCacheFolder()
        {
            List<string> expectedDirectories = new List<string>()
            {
                "microsoft.netcore.app",
                "microsoft.netcore.platforms",
                "netstandard.library",
                "microsoft.aspnetcore.diagnostics",
                "microsoft.aspnetcore.mvc",
                "microsoft.aspnetcore.routing",
                "microsoft.aspnetcore.server.iisintegration",
                "microsoft.aspnetcore.server.kestrel",
                "microsoft.aspnetcore.staticfiles",
                "microsoft.extensions.configuration.environmentvariables",
                "microsoft.extensions.configuration.json",
                "microsoft.extensions.logging",
                "microsoft.extensions.logging.console",
                "microsoft.extensions.logging.debug",
                "microsoft.extensions.options.configurationextensions",
                //BrowserLink has been temporarily disabled until https://github.com/dotnet/templating/issues/644 is resolved
                //"microsoft.visualstudio.web.browserlink",
            };

            _nugetFallbackFolder
                .Should()
                .HaveDirectories(expectedDirectories);
        }

        private string GetDotnetVersion()
        {
            return new DotnetCommand().ExecuteWithCapturedOutput("--version").StdOut
                .TrimEnd(Environment.NewLine.ToCharArray());
        }

        private static string NormalizeLineEndings(string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
