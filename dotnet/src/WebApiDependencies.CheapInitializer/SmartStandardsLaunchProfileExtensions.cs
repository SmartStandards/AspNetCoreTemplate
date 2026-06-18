using Logging.SmartStandards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore {

  public static class SmartStandardsLaunchProfileExtensions {

    public static void UseUrlsFromLaunchProfileIfRequested(this IWebHostBuilder webHost, string[] args) {

      string foundArg = args.Where(
        (a) => a.StartsWith("LaunchProfile=", StringComparison.CurrentCultureIgnoreCase)
      ).FirstOrDefault();

      if (foundArg == null || foundArg.IndexOf('=') < foundArg.Length - 2) {
        return;
      }

      string executingLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

      string[] potentialLaunchSettingsLocations = new string[] {
        "./", "./Properties/", "../Properties", "../../Properties"
      };

      string launchProfileFullFileName;

      foreach (string locationProbe in potentialLaunchSettingsLocations) {
        launchProfileFullFileName = Path.GetFullPath(Path.Combine(executingLocation, locationProbe, "launchSettings.json"));
        if (File.Exists(launchProfileFullFileName)) {
          string profileNameOrIndex = foundArg.Substring(foundArg.IndexOf('=') + 1);
          webHost.UseUrlsFromLaunchProfile(launchProfileFullFileName, profileNameOrIndex);
          return;
        }
      }
      throw new Exception($"Launch profile specified via command line argument, but no launchSettings.json file found at expected locations.");
    }

    public static void UseUrlsFromLaunchProfile(
      this IWebHostBuilder webHost,
      string launchProfileFullFileName,
      string profileNameOrIndex
    ) {

      using JsonDocument document = JsonDocument.Parse(
        File.ReadAllText(launchProfileFullFileName)
      );

      JsonElement root = document.RootElement;

      if (!root.TryGetProperty("profiles", out JsonElement profiles)) {
        throw new InvalidOperationException("The launchSettings.json file does not contain a valid profiles section.");
      }

      JsonProperty selectedProfile;

      int profileIndex;
      if (Int32.TryParse(profileNameOrIndex, out profileIndex)) {

        JsonProperty[] profileProperties = profiles.EnumerateObject().ToArray();

        if (profileIndex < 0 || profileIndex >= profileProperties.Length) {
          throw new IndexOutOfRangeException("The requested launch profile index is out of range.");
        }

        selectedProfile = profileProperties[profileIndex];
      }
      else {

        JsonProperty[] matchingProfiles = profiles.EnumerateObject().Where(
          (p) => p.Name.Equals(profileNameOrIndex, StringComparison.OrdinalIgnoreCase)
        ).ToArray();

        if (matchingProfiles.Length == 0) {
          throw new InvalidOperationException("The requested launch profile name was not found.");
        }

        selectedProfile = matchingProfiles[0];
      }

      JsonElement profile = selectedProfile.Value;

      if (
        !profile.TryGetProperty("applicationUrl", out JsonElement applicationUrlElement)
        || applicationUrlElement.ValueKind != JsonValueKind.String
      ) {
        throw new InvalidOperationException("The selected launch profile does not contain an applicationUrl.");
      }

      string applicationUrl = applicationUrlElement.GetString();

      if (String.IsNullOrWhiteSpace(applicationUrl)) {
        throw new InvalidOperationException("The selected launch profile does not contain an applicationUrl.");
      }

      string[] urlsFromLaunchProfile = applicationUrl.Split(';',StringSplitOptions.RemoveEmptyEntries);

      InsLogger.LogInformation(
        $"Using explicit listening-URLs from launch profile '{selectedProfile.Name}' ({launchProfileFullFileName}): {applicationUrl}"
      );

      webHost.UseUrls(urlsFromLaunchProfile);
    }

  }

}
