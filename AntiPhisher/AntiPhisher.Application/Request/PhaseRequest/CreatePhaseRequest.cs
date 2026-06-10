namespace AntiPhisher.Application.Request.PhaseRequest
{
    public class CreatePhaseRequest
    {
        public string PhaseName { get; set; } = null!;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon  { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdatePhaseRequest
    {
        public string? PhaseName  { get; set; }
        public string? Description { get; set; }
        public string? Color       { get; set; }
        public string? Icon        { get; set; }
        public int?    OrderIndex  { get; set; }
        public bool?   IsActive    { get; set; }
    }

    public class CreateModuleRequest
    {
        public string ModuleName  { get; set; } = null!;
        public int    OrderIndex  { get; set; }
    }
}
