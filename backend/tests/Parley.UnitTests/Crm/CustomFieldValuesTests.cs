using System.Text.Json;
using AwesomeAssertions;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Shared.CustomFields;
using Parley.SharedKernel.Results;
using Xunit;

namespace Parley.UnitTests.Crm;

/// <summary>Merge rules of custom field values: type, options, date format, required and archived fields.</summary>
public sealed class CustomFieldValuesTests
{
    private const string EmptyObject = "{}";
    private const string ScoreKey = "score";
    private const string SegmentKey = "segment";
    private const string ArchivedKey = "retired";
    private const string SegmentOptions = "[\"smb\",\"enterprise\"]";

    [Fact]
    public void MissingRequiredValue_IsReportedWithCode_OnCreate()
    {
        var definitions = new[] { Definition(ScoreKey, CustomFieldTypes.Number, isRequired: true) };

        var result = CustomFieldValues.Merge(definitions, CustomFieldValues.NoValues, EmptyObject);

        result.IsSuccess.Should().BeFalse();
        result.Error!.FieldErrors!.Should().ContainSingle()
            .Which.Should().Match<FieldError>(error => error.Field == "customFields.score" && error.Code == CrmErrorCodes.CustomFieldRequired);
    }

    [Fact]
    public void NullMap_KeepsStoredValues_WithoutRequiredCheck()
    {
        // A rename that sends no custom fields must not fail because a field was made required later.
        var definitions = new[] { Definition(ScoreKey, CustomFieldTypes.Number, isRequired: true) };

        var result = CustomFieldValues.Merge(definitions, null, EmptyObject);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(EmptyObject);
    }

    [Fact]
    public void UnknownKey_AndInvalidOption_AreReportedWithCodes()
    {
        var definitions = new[] { Definition(SegmentKey, CustomFieldTypes.Select, options: SegmentOptions) };
        var provided = new Dictionary<string, JsonElement>
        {
            [SegmentKey] = JsonSerializer.SerializeToElement("retail"),
            ["unknown"] = JsonSerializer.SerializeToElement(1),
        };

        var result = CustomFieldValues.Merge(definitions, provided, EmptyObject);

        result.IsSuccess.Should().BeFalse();
        result.Error!.FieldErrors!.Select(error => (error.Field, error.Code)).Should().BeEquivalentTo(
        [
            ("customFields.segment", CrmErrorCodes.CustomFieldOptionInvalid),
            ("customFields.unknown", CrmErrorCodes.CustomFieldUnknownKey),
        ]);
    }

    [Fact]
    public void ArchivedValues_AreKept_WhenActiveValuesAreReplaced()
    {
        var definitions = new[] { Definition(ScoreKey, CustomFieldTypes.Number) };
        var stored = "{\"retired\":\"kept\",\"score\":1}";
        var provided = new Dictionary<string, JsonElement> { [ScoreKey] = JsonSerializer.SerializeToElement(7) };

        var result = CustomFieldValues.Merge(definitions, provided, stored);

        result.IsSuccess.Should().BeTrue();
        var merged = CustomFieldValues.ToMap(result.Value!);
        merged[ArchivedKey].GetString().Should().Be("kept");
        merged[ScoreKey].GetInt32().Should().Be(7);
    }

    [Fact]
    public void Date_MustUseTheCalendarFormat()
    {
        var definitions = new[] { Definition("renewal", CustomFieldTypes.Date) };
        var provided = new Dictionary<string, JsonElement> { ["renewal"] = JsonSerializer.SerializeToElement("11/12/2026") };

        var result = CustomFieldValues.Merge(definitions, provided, EmptyObject);

        result.Error!.FieldErrors!.Should().ContainSingle()
            .Which.Code.Should().Be(CrmErrorCodes.CustomFieldValueInvalid);
    }

    private static CustomFieldDefinition Definition(string key, string fieldType, bool isRequired = false, string options = "[]") => new()
    {
        Key = key,
        EntityType = CustomFieldEntityTypes.Contact,
        FieldType = fieldType,
        Label = key,
        IsRequired = isRequired,
        Options = options,
    };
}
