namespace DMP.DataAccess.Models;

public class ProductCreationDAL
{
    public int ProductCreationId { get; set; }
    public int ProductId { get; set; }
    public string? CustomCategory { get; set; }
    public bool UseCustomCategory { get; set; }
    public string[]? Lines { get; set; }
    public string? MessageForSeller { get; set; }

    public virtual ProductDAL? Product { get; set; }
}
