/*
 * File: CategoryModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of Vendor Operations for Product Category Management
 */


namespace EAD_BE.Models.Vendor.Product;

public class CategoryModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; } = true;
}