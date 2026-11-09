namespace Parley.Crm.Features.Contacts.Exports.Shared;

/// <summary>A verified export file on local storage, ready to stream to the requester.</summary>
public sealed record ContactExportFile(string Path, string DownloadName);
