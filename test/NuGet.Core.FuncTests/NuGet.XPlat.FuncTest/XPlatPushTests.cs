// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.CommandLine.XPlat;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.XPlat.FuncTest
{
    public class XPlatPushTests
    {
        [Theory]
        [InlineData(TestServers.MyGet, nameof(TestServers.MyGet), false)]
        [InlineData(TestServers.ProGet, nameof(TestServers.ProGet), false)]
        [InlineData(TestServers.Nexus, nameof(TestServers.Nexus), true)]
        // [InlineData(TestServers.Artifactory, nameof(TestServers.Artifactory), false)] // Trial license expired!
        // [InlineData(TestServers.Klondike, nameof(TestServers.Klondike), false)] // 500 Internal Server Error pushing
        // [InlineData(TestServers.NuGetServer, nameof(TestServers.NuGetServer), false)] // doesn't work (credentials?)
        public async Task PushToServerSucceeds(string sourceUri, string feedName, bool mustDeleteFirst)
        {
            // Arrange
            using (var packageDir = TestFileSystemUtility.CreateRandomTestFolder())
            {
                var packageId = "XPlatPushTests.PushToServerSucceeds";
                var packageVersion = "1.0.0";
                var packageFile = await TestPackages.GetRuntimePackageAsync(packageDir, packageId, packageVersion);
                var configFile = XPlatTestUtils.CopyFuncTestConfig(packageDir);
                var log = new TestCommandOutputLogger();

                var apiKey = XPlatTestUtils.ReadApiKey(feedName);
                Assert.False(string.IsNullOrEmpty(apiKey));

                if (mustDeleteFirst)
                {
                    DeletePackageBeforePush(packageId, packageVersion, sourceUri, apiKey);
                }

                var pushArgs = new List<string>()
                {
                    "push",
                    packageFile.FullName,
                    "--source",
                    sourceUri,
                    "--api-key",
                    apiKey
                };

                // Act
                int exitCode = Program.MainInternal(pushArgs.ToArray(), log);

                // Assert
                Assert.Equal(string.Empty, log.ShowErrors());
                Assert.Equal(0, exitCode);
                Assert.Contains($@"PUT {sourceUri}", log.ShowMessages());
                Assert.Contains("Your package was pushed.", log.ShowMessages());
            }
        }

        /// <summary>
        /// This is called when the package must be deleted before being pushed. It's ok if this
        /// fails, maybe the package was never pushed.
        /// </summary>
        private void DeletePackageBeforePush(string packageId, string packageVersion, string sourceUri, string apiKey)
        {
            var log = new TestCommandOutputLogger();
            var args = new List<string>()
            {
                "delete",
                packageId,
                packageVersion,
                "--source",
                sourceUri,
                "--api-key",
                apiKey,
                "--non-interactive"
            };

            Program.MainInternal(args.ToArray(), log);
        }
    }
}
