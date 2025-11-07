namespace MinIOCRUD.Dtos.Responses
{
    public class BreadcrumbItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url => $"/folders/{Id}";
    }

}
