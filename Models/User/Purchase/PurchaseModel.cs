namespace EAD_BE.Models.User.Purchased;

public class PurchaseModel
{
    public Guid PurchaseId { get; set; }
    public string userEmail { get; set; }
    public DateTime PurchaseDate { get; set; }
    public List<PurchasedItem> Items { get; set; }
    public bool IsShipped { get; set; } = false;
    public bool IsDelivered { get; set; } = false;
}

public class PurchasedItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}