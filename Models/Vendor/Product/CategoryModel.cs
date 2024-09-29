namespace EAD_BE.Models.Vendor.Product;

public class CategoryModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; } = true;
}