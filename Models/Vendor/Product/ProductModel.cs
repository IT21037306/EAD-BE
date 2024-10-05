namespace EAD_BE.Models.Vendor.Product;

public class ProductModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public Guid Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string AddedByUserEmail { get; set; }

    public int Ranking { get; set; } = 0;
    
    public int RankingCount { get; set; } = 0;
    
    public List<Comment> Comments { get; set; } = new List<Comment>();
    
    public string ProductPicture { get; set; }
    
    public Notification? Notification { get; set; }

}

public class Notification
{
    public string Message { get; set; }
    public int currentStock { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Comment
{
    public Guid commentID { get; set; }
    public string UserEmail { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}