using Microsoft.Extensions.Options;
using Parley.Crm.Options;

namespace Parley.Crm.Exports;

/// <summary>
/// Local file storage of the export directory. Every path is the export directory plus a file
/// name that passed <see cref="ContactExportFiles.IsValidFileName"/>, and the resolved path must
/// stay inside the directory.
/// </summary>
public sealed class ContactExportStorage(IOptions<CrmExportOptions> options)
{
    private readonly string _root = Path.GetFullPath(options.Value.Directory);

    public string Root => _root;

    public string PathFor(string fileName)
    {
        if (!ContactExportFiles.IsValidFileName(fileName))
        {
            throw new InvalidOperationException("The export file name is not a valid export file name.");
        }

        var path = Path.GetFullPath(Path.Combine(_root, fileName));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The export path resolved outside the export directory.");
        }

        return path;
    }
}
