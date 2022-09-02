﻿namespace ScriptsBase.Checks.FileTypes;

using System.Collections.Generic;
using System.IO;
using System.Text;

public class CsprojCheck : FileCheck
{
    private const string XML_IDENTIFIER = "<?xml";

    public CsprojCheck() : base(".csproj")
    {
    }

    public override async IAsyncEnumerable<string> Handle(string path)
    {
        // TODO: this check could be made into a generic check

        // TODO: this could read just a few of the first bytes
        var text = await File.ReadAllTextAsync(path, Encoding.UTF8);

        if (!text.StartsWith(XML_IDENTIFIER))
        {
            yield return $"File doesn't start with '{XML_IDENTIFIER}' like due to added BOM";
        }
    }
}
