﻿namespace ScriptsBase.ToolBases;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Models;
using SharedBase.Utilities;
using Utilities;

/// <summary>
///   Base class for handling updating localization files
/// </summary>
/// <typeparam name="T">The type of the options class</typeparam>
public abstract class LocalizationUpdateBase<T>
    where T : LocalizationOptionsBase
{
    protected readonly T options;

    private readonly Dictionary<string, string> alreadyFoundTools = new();

    protected LocalizationUpdateBase(T options)
    {
        this.options = options;
    }

    /// <summary>
    ///   The locales to process
    /// </summary>
    protected abstract IReadOnlyList<string> Locales { get; }

    protected abstract string LocaleFolder { get; }

    /// <summary>
    ///   Weblate disagrees with gettext tools regarding where to wrap, so we have to disable it
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     https://github.com/WeblateOrg/weblate/issues/6350
    ///     https://github.com/Revolutionary-Games/Thrive/issues/2679
    ///   </para>
    /// </remarks>
    protected virtual IEnumerable<string> LineWrapSettings => new[] { "--no-wrap" };

    protected virtual string TranslationTemplateFileName => $"messages{PotSuffix}";

    protected virtual string TranslationTemplateFile => Path.Join(LocaleFolder, TranslationTemplateFileName);

    protected string PotSuffix => string.IsNullOrEmpty(options.PotSuffix) ? ".pot" : options.PotSuffix;
    protected string PoSuffix => string.IsNullOrEmpty(options.PoSuffix) ? ".po" : options.PoSuffix;

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        if (!options.Quiet)
            ColourConsole.WriteInfoLine($"Extracting translations template {TranslationTemplateFileName}");

        if (!await RunTemplateUpdate(cancellationToken))
            return false;

        if (!File.Exists(TranslationTemplateFile))
        {
            ColourConsole.WriteErrorLine(
                $"Failed to create the expected translations template at: {TranslationTemplateFile}");
            return false;
        }

        if (!options.Quiet)
        {
            ColourConsole.WriteSuccessLine("Template updated");
            ColourConsole.WriteInfoLine($"Updating individual languages ({PoSuffix} files)");
        }

        foreach (var locale in Locales)
        {
            if (!await UpdateLocale(locale, cancellationToken))
            {
                ColourConsole.WriteErrorLine($"Failed to process locale: {locale}");
                return false;
            }
        }

        await PostProcessTranslations(cancellationToken);

        if (!options.Quiet)
            ColourConsole.WriteSuccessLine("Done updating locales");
        return true;
    }

    protected virtual string GetTranslationFileNameForLocale(string locale)
    {
        return $"{locale}{PoSuffix}";
    }

    protected virtual Task<bool> UpdateLocale(string locale, CancellationToken cancellationToken)
    {
        var target = Path.Join(LocaleFolder, GetTranslationFileNameForLocale(locale));

        if (File.Exists(target))
        {
            if (!options.Quiet)
                ColourConsole.WriteNormalLine($"Updating locale {locale}");

            return RunTranslationUpdate(locale, target, cancellationToken);
        }

        // TODO: this is untested
        if (!options.Quiet)
            ColourConsole.WriteNormalLine($"Creating locale {locale}");
        return RunTranslationCreate(locale, target, cancellationToken);
    }

    protected abstract Task<bool> RunTranslationCreate(string locale, string targetFile,
        CancellationToken cancellationToken);

    protected abstract Task<bool> RunTranslationUpdate(string locale, string targetFile,
        CancellationToken cancellationToken);

    protected abstract ProcessStartInfo GetParametersToRunExtraction();

    protected virtual Task<bool> PostProcessTranslations(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    protected string? FindTranslationTool(string name)
    {
        if (alreadyFoundTools.TryGetValue(name, out var alreadyFound))
            return alreadyFound;

        var path = ExecutableFinder.Which(name);

        if (path == null)
        {
            ColourConsole.WriteErrorLine(
                $"Failed to find translation tool '{name}'. Please install it and make sure it is in PATH.");
        }
        else
        {
            alreadyFoundTools[name] = path;
        }

        return path;
    }

    protected void AddLineWrapSettings(ProcessStartInfo startInfo)
    {
        foreach (var setting in LineWrapSettings)
        {
            startInfo.ArgumentList.Add(setting);
        }
    }

    protected async Task<bool> RunTranslationTool(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine(
                $"Failed to run a localization tool (exit: {result.ExitCode}): {result.FullOutput}");
            return false;
        }

        return true;
    }

    private async Task<bool> RunTemplateUpdate(CancellationToken cancellationToken)
    {
        var startInfo = GetParametersToRunExtraction();

        // Showing the output about where it extracts stuff is good by default
        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, options.Quiet);

        if (result.ExitCode != 0)
        {
            if (options.Quiet)
            {
                ColourConsole.WriteErrorLine(
                    $"Failed to run text extraction (exit: {result.ExitCode}): {result.FullOutput}");
            }
            else
            {
                ColourConsole.WriteErrorLine(
                    $"Failed to run text extraction (exit: {result.ExitCode}) see messages above");
            }

            return false;
        }

        return true;
    }
}
