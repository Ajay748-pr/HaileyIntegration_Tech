namespace HaileyIntegration.Tech.Models.Dto;

public sealed class VismaDimensionRequest
{
    public List<VismaSegmentValue> SegmentValues { get; set; } = [];
    public string DimensionId { get; set; } = string.Empty;
    public int SegementId { get; set; }
    public VismaLocalizedValue? Description { get; set; }
}

public sealed class VismaSegmentValue
{
    public string Operation { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public VismaLocalizedValue? Description { get; set; }
    public VismaBooleanValue? Active { get; set; }
}

public sealed class VismaLocalizedValue
{
    public string Value { get; set; } = string.Empty;
}

public sealed class VismaBooleanValue
{
    public bool Value { get; set; }
}
