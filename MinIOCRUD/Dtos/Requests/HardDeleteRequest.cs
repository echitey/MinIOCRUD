namespace MinIOCRUD.Dtos.Requests
{
    public class HardDeleteRequest
    {
        public List<Guid> Ids { get; set; } = new();
        public bool Force { get; set; } = false;
    }
}
