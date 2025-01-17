namespace DevCenterCommunication.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SharedBase.ModelVerifiers;

public class LauncherVersionInfo
{
    public LauncherVersionInfo(string latestVersion)
    {
        LatestVersion = latestVersion;
    }

    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string LatestVersion { get; }

    [Required]
    [ToStringMaxLength]
    public Uri DownloadsPage { get; set; } = new("https://revolutionarygamesstudio.com/");

    [Required]
    [MaxLength(50)]
    public Dictionary<LauncherAutoUpdateChannel, DownloadableInfo> AutoUpdateDownloads { get; set; } = new();
}
