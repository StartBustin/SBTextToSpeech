using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(SBTextToSpeech.BuildInfo.Description)]
[assembly: AssemblyDescription(SBTextToSpeech.BuildInfo.Description)]
[assembly: AssemblyCompany(SBTextToSpeech.BuildInfo.Company)]
[assembly: AssemblyProduct(SBTextToSpeech.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + SBTextToSpeech.BuildInfo.Author)]
[assembly: AssemblyTrademark(SBTextToSpeech.BuildInfo.Company)]
[assembly: AssemblyVersion(SBTextToSpeech.BuildInfo.Version)]
[assembly: AssemblyFileVersion(SBTextToSpeech.BuildInfo.Version)]
[assembly: MelonInfo(typeof(SBTextToSpeech.SBTextToSpeech), SBTextToSpeech.BuildInfo.Name, SBTextToSpeech.BuildInfo.Version, SBTextToSpeech.BuildInfo.Author, SBTextToSpeech.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame(null, null)]